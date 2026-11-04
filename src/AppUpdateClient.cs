using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace VsiCreativeSuite
{
    internal sealed class ApprovedApp
    {
        public string Id, Name, Detail, Executable, Symbol, Accent;
        public string Repository, AssetNameTemplate, Delivery;
    }

    internal sealed class AppUpdate
    {
        public string Id, Name, Executable, Repository, OldTag, Tag, AssetName, Sha256;
        public Uri Url;
        public long Size;
        public bool IsHew;
        public string DownloadedPath, StagePath;
    }

    internal sealed class AppUpdateScan
    {
        public readonly List<AppUpdate> Updates = new List<AppUpdate>();
        public readonly List<ApprovedApp> CatalogApps = new List<ApprovedApp>();
        public readonly List<string> Errors = new List<string>();
    }

    internal static class AppUpdates
    {
        private static bool checking;
        private const string UpstreamUserAgent = "VSI-Creative-Suite-Updater/0.3.1";

        private static string InstallRoot
        {
            get { return Path.GetDirectoryName(typeof(AppUpdates).Assembly.Location); }
        }

        public static void Check(Form owner, bool manual)
        {
            if (checking || owner.IsDisposed) return;
            checking = true;
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += delegate(object sender, DoWorkEventArgs e)
            {
                e.Result = Scan();
            };
            worker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
            {
                checking = false;
                if (owner.IsDisposed) return;
                if (e.Error != null)
                {
                    if (manual) MessageBox.Show(owner, "Could not check app updates:\n" + e.Error.Message,
                        "VSI Creative Suite", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                AppUpdateScan scan = e.Result as AppUpdateScan;
                if (scan == null) return;
                LauncherForm launcher = owner as LauncherForm;
                if (launcher != null) launcher.ApplyCatalog(scan.CatalogApps);
                if (scan.Updates.Count == 0)
                {
                    if (manual)
                    {
                        string message = scan.Errors.Count == 0
                            ? "All bundled apps are up to date with their official published releases."
                            : "No confirmed updates found. Some upstream sources could not be checked:\n\n" +
                              string.Join("\n", scan.Errors.ToArray());
                        MessageBox.Show(owner, message, "VSI Creative Suite",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    return;
                }
                List<AppUpdate> selected;
                using (AppUpdateSelectionForm picker = new AppUpdateSelectionForm(scan.Updates))
                {
                    if (picker.ShowDialog(owner) != DialogResult.OK) return;
                    selected = picker.Selected;
                }
                foreach (AppUpdate item in selected)
                {
                    try
                    {
                        if (Process.GetProcessesByName(item.Id).Length > 0)
                        {
                            MessageBox.Show(owner, "Close " + item.Name + " first, then check for updates again.\n" +
                                "Your application files were not changed.",
                                "VSI Creative Suite", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(owner, "Could not verify if " + item.Name + " is running:\n" + ex.Message,
                            "VSI Creative Suite", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                using (AppUpdatesDialog dialog = new AppUpdatesDialog(selected))
                    dialog.ShowDialog(owner);
            };
            worker.RunWorkerAsync();
        }

        internal static AppUpdateScan Scan()
        {
            string lockPath = Path.Combine(InstallRoot, "release-lock.json");
            if (!File.Exists(lockPath))
                throw new FileNotFoundException("Release version list is missing.", lockPath);
            Dictionary<string, string> installed = ReadVersions(lockPath);
            AppUpdateScan result = new AppUpdateScan();
            List<ApprovedApp> approved = LoadCatalog();
            result.CatalogApps.AddRange(approved);
            foreach (ApprovedApp app in approved)
            {
                string oldTag;
                if (!installed.TryGetValue(app.Repository, out oldTag))
                    oldTag = "not installed";
                try
                {
                    string json = Fetch("https://api.github.com/repos/" + app.Repository + "/releases/latest");
                    AppUpdate candidate = ParseCandidate(app, oldTag, json);
                    if (candidate != null) result.Updates.Add(candidate);
                }
                catch (Exception ex) { result.Errors.Add(app.Id + ": " + ex.Message); }
            }
            return result;
        }

        private static List<ApprovedApp> LoadCatalog()
        {
            // The public VSI GitHub catalog is the reviewed allowed-app list.
            // Cache validated metadata so previously discovered apps stay visible offline.
            string cache = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VSI Creative Suite", "approved-apps.json");
            try
            {
                string json = Fetch("https://raw.githubusercontent.com/vsiphils/vsi-creative-suite/main/catalog/apps.json");
                List<ApprovedApp> approved = ParseCatalog(json);
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(cache));
                    File.WriteAllText(cache, json, Encoding.UTF8);
                }
                catch { /* Cache failure cannot prevent online use. */ }
                return approved;
            }
            catch
            {
                try
                {
                    if (File.Exists(cache)) return ParseCatalog(File.ReadAllText(cache));
                }
                catch { /* Bad cache is ignored. */ }
                string local = Path.Combine(InstallRoot, "catalog", "apps.json");
                if (!File.Exists(local))
                    throw new InvalidDataException("No approved app catalog is available.");
                return ParseCatalog(File.ReadAllText(local));
            }
        }

        internal static List<ApprovedApp> ParseCatalog(string json)
        {
            Dictionary<string, object> root =
                new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string, object>;
            if (root == null || !root.ContainsKey("schemaVersion") ||
                Convert.ToInt32(root["schemaVersion"]) != 1)
                throw new InvalidDataException("Unsupported app catalog schema.");
            object raw;
            object[] items = root.TryGetValue("apps", out raw) ? raw as object[] : null;
            if (items == null || items.Length < 1 || items.Length > 24)
                throw new InvalidDataException("Catalog has an invalid number of apps.");
            List<ApprovedApp> result = new List<ApprovedApp>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (object rawApp in items)
            {
                Dictionary<string, object> fields = rawApp as Dictionary<string, object>;
                if (fields == null) throw new InvalidDataException("Invalid app catalog entry.");
                ApprovedApp a = new ApprovedApp
                {
                    Id = Value(fields, "id"), Name = Value(fields, "name"),
                    Detail = Value(fields, "description"), Executable = Value(fields, "executable"),
                    Symbol = Value(fields, "symbol"), Accent = Value(fields, "accent"),
                    Repository = Value(fields, "repository"),
                    AssetNameTemplate = Value(fields, "assetNameTemplate"),
                    Delivery = Value(fields, "delivery")
                };
                if (!Regex.IsMatch(a.Id, @"^[a-z][a-z0-9-]{1,31}$") || !seen.Add(a.Id) ||
                    !Regex.IsMatch(a.Executable, @"^[a-zA-Z0-9._-]+\.exe$") ||
                    !Regex.IsMatch(a.Accent, @"^#[0-9A-Fa-f]{6}$") ||
                    !Regex.IsMatch(a.Repository, @"^[a-zA-Z0-9-]+/[a-zA-Z0-9._-]+$") ||
                    string.IsNullOrWhiteSpace(a.Name) || a.Name.Length > 56 ||
                    string.IsNullOrWhiteSpace(a.Detail) || a.Detail.Length > 75 ||
                    string.IsNullOrWhiteSpace(a.Symbol) || a.Symbol.Length > 8 ||
                    a.AssetNameTemplate.IndexOf("{version}", StringComparison.Ordinal) < 0 ||
                    !Regex.IsMatch(a.AssetNameTemplate.Replace("{version}", "1.2.3"),
                        @"^[A-Za-z0-9._-]+\.(zip|exe)$") ||
                    (a.Delivery != "bundled-portable" &&
                     a.Delivery != "bundled-installer" && a.Delivery != "release-zip"))
                    throw new InvalidDataException("Unapproved or invalid catalog entry: " + a.Id);
                if (a.Id == "hew" && a.Delivery != "bundled-installer")
                    throw new InvalidDataException("Hew delivery method must be its official installer.");
                if (a.Id != "hew" && !a.AssetNameTemplate.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Only approved portable ZIP apps are supported.");
                result.Add(a);
            }
            return result;
        }

        private static Dictionary<string, string> ReadVersions(string lockPath)
        {
            object parsed = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(lockPath));
            object[] entries = parsed as object[];
            if (entries == null) throw new InvalidDataException("Invalid release-lock.json format.");
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (object entry in entries)
            {
                Dictionary<string, object> item = entry as Dictionary<string, object>;
                if (item == null) continue;
                string repo = Value(item, "repository");
                string tag = Value(item, "version");
                if (!string.IsNullOrEmpty(repo)) result[repo] = tag;
            }
            return result;
        }

        private static string Fetch(string url)
        {
            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = UpstreamUserAgent;
            request.Accept = "application/vnd.github+json";
            request.Timeout = 12000;
            request.ReadWriteTimeout = 12000;
            using (WebResponse response = request.GetResponse())
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                return reader.ReadToEnd();
        }

        private static string Value(Dictionary<string, object> data, string key)
        {
            object value;
            return data.TryGetValue(key, out value) && value != null ? Convert.ToString(value) : "";
        }

        internal static AppUpdate ParseCandidate(ApprovedApp app, string installedTag, string json)
        {
            Dictionary<string, object> release =
                new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string, object>;
            if (release == null) throw new InvalidDataException("Invalid upstream release response.");
            if (Value(release, "draft") == "True" || Value(release, "prerelease") == "True")
                return null;
            string tag = Value(release, "tag_name");
            Match match = Regex.Match(tag, @"^v(\d+\.\d+\.\d+)$");
            if (!match.Success) return null;
            Version remote, local;
            if (!Version.TryParse(match.Groups[1].Value, out remote))
                throw new InvalidDataException("Invalid upstream version.");
            if (installedTag == "not installed") local = new Version(0, 0, 0);
            else if (!Version.TryParse(installedTag.TrimStart('v', 'V'), out local))
                throw new InvalidDataException("Invalid installed version: " + installedTag);
            if (remote.CompareTo(local) <= 0) return null;

            string assetName = app.AssetNameTemplate.Replace("{version}", match.Groups[1].Value);
            object assetsRaw;
            if (!release.TryGetValue("assets", out assetsRaw))
                throw new InvalidDataException("Missing release assets.");
            object[] assets = assetsRaw as object[];
            if (assets == null) throw new InvalidDataException("Invalid release assets.");
            foreach (object item in assets)
            {
                Dictionary<string, object> asset = item as Dictionary<string, object>;
                if (asset == null || Value(asset, "name") != assetName) continue;
                string digest = Value(asset, "digest");
                if (!Regex.IsMatch(digest, @"^sha256:[0-9A-Fa-f]{64}$"))
                    throw new InvalidDataException("Release has no valid SHA256 digest.");
                Uri url;
                string exact = "https://github.com/" + app.Repository +
                    "/releases/download/" + tag + "/" + assetName;
                if (!Uri.TryCreate(Value(asset, "browser_download_url"), UriKind.Absolute, out url) ||
                    !string.Equals(url.AbsoluteUri, exact, StringComparison.Ordinal))
                    throw new InvalidDataException("Unexpected upstream download URL.");
                object bytes;
                long size = asset.TryGetValue("size", out bytes) ? Convert.ToInt64(bytes) : -1L;
                if (size < 1048576L || size > 2147483648L)
                    throw new InvalidDataException("Unexpected upstream package size.");
                return new AppUpdate
                {
                    Id = app.Id, Name = app.Name, Executable = app.Executable, Repository = app.Repository,
                    OldTag = installedTag, Tag = tag, AssetName = assetName,
                    Sha256 = digest.Substring(7), Size = size, Url = url, IsHew = app.Id == "hew"
                };
            }
            throw new InvalidDataException("Required Windows x64 package missing.");
        }

        public static bool Verify(string path, AppUpdate app)
        {
            return new FileInfo(path).Length == app.Size &&
                SuiteUpdater.MatchesSha256(path, app.Sha256);
        }

        public static void RecordInstalled(AppUpdate app)
        {
            string path = Path.Combine(InstallRoot, "release-lock.json");
            object[] items = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(path)) as object[];
            if (items == null) throw new InvalidDataException("Invalid local release lock.");
            List<object> entries = new List<object>(items);
            bool found = false;
            foreach (object item in entries)
            {
                Dictionary<string, object> entry = item as Dictionary<string, object>;
                if (entry == null || Value(entry, "repository") != app.Repository) continue;
                entry["version"] = app.Tag;
                entry["asset"] = app.AssetName;
                entry["sha256"] = app.Sha256;
                found = true;
            }
            if (!found)
                entries.Add(new Dictionary<string, object> {
                    { "repository", app.Repository }, { "version", app.Tag },
                    { "asset", app.AssetName }, { "sha256", app.Sha256 }
                });
            string temporary = path + ".updating";
            File.WriteAllText(temporary, new JavaScriptSerializer().Serialize(entries), Encoding.UTF8);
            // Same volume; preserve the original file if the new content fails.
            if (File.Exists(path)) File.Replace(temporary, path, path + ".previous", true);
            else File.Move(temporary, path);
        }
    }

    internal sealed class AppUpdateSelectionForm : Form
    {
        private readonly CheckedListBox options = new CheckedListBox();
        private readonly Button apply = new Button();
        public readonly List<AppUpdate> Selected = new List<AppUpdate>();

        public AppUpdateSelectionForm(List<AppUpdate> updates)
        {
            Text = "VSI Creative Suite — Select App Updates";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(565, 440);
            BackColor = Color.FromArgb(18, 21, 28);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10f);

            Label heading = new Label();
            heading.Text = "Choose apps to install or update";
            heading.Font = new Font("Segoe UI", 17f, FontStyle.Bold);
            heading.ForeColor = Color.White;
            heading.SetBounds(23, 18, 515, 42);
            Controls.Add(heading);

            Label hint = new Label();
            hint.Text = "Optional: select the apps you want. Close those apps before updating.";
            hint.ForeColor = Color.FromArgb(178, 187, 200);
            hint.SetBounds(25, 63, 520, 39);
            Controls.Add(hint);

            options.SetBounds(25, 107, 515, 232);
            options.CheckOnClick = true;
            options.IntegralHeight = false;
            options.BorderStyle = BorderStyle.FixedSingle;
            options.BackColor = Color.FromArgb(25, 30, 39);
            options.ForeColor = Color.White;
            for (int i = 0; i < updates.Count; i++)
                options.Items.Add(updates[i].Name + "   " +
                    (updates[i].OldTag == "not installed" ? "NEW APP" : updates[i].OldTag) +
                    "  →  " + updates[i].Tag, false);
            options.ItemCheck += delegate
            {
                BeginInvoke((MethodInvoker)delegate { apply.Enabled = options.CheckedItems.Count > 0; });
            };
            Controls.Add(options);

            Button selectAll = new Button();
            selectAll.Text = "Select all";
            selectAll.SetBounds(25, 355, 102, 32);
            selectAll.Click += delegate
            {
                for (int i = 0; i < options.Items.Count; i++) options.SetItemChecked(i, true);
                apply.Enabled = options.Items.Count > 0;
            };
            Controls.Add(selectAll);

            Button clear = new Button();
            clear.Text = "Clear";
            clear.SetBounds(135, 355, 87, 32);
            clear.Click += delegate
            {
                for (int i = 0; i < options.Items.Count; i++) options.SetItemChecked(i, false);
                apply.Enabled = false;
            };
            Controls.Add(clear);

            Button later = new Button();
            later.Text = "Later";
            later.SetBounds(282, 355, 97, 32);
            later.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(later);

            apply.Text = "Install / Update";
            apply.SetBounds(390, 355, 150, 32);
            apply.Enabled = false;
            apply.BackColor = Color.FromArgb(41, 103, 205);
            apply.ForeColor = Color.White;
            apply.Click += delegate
            {
                foreach (int index in options.CheckedIndices)
                    Selected.Add(updates[index]);
                if (Selected.Count == 0) return;
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(apply);
        }
    }

    internal sealed class AppUpdatesDialog : Form
    {
        private readonly List<AppUpdate> updates;
        private readonly ProgressBar bar = new ProgressBar();
        private readonly Label status = new Label();
        private readonly Button cancel = new Button();
        private readonly BackgroundWorker worker = new BackgroundWorker();
        private readonly List<string> downloaded = new List<string>();
        private readonly List<string> stages = new List<string>();

        public AppUpdatesDialog(List<AppUpdate> selected)
        {
            updates = selected;
            Text = "VSI Creative Suite — Updating Applications";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ClientSize = new Size(515, 150);
            MaximizeBox = false; MinimizeBox = false; ControlBox = false;
            BackColor = Color.FromArgb(19, 23, 31); ForeColor = Color.White;
            Font = new Font("Segoe UI", 10f);
            status.SetBounds(20, 20, 470, 35);
            status.Text = "Preparing verified downloads...";
            status.ForeColor = Color.White;
            Controls.Add(status);
            bar.SetBounds(20, 65, 473, 23);
            bar.Maximum = 100;
            Controls.Add(bar);
            cancel.SetBounds(393, 103, 100, 32);
            cancel.Text = "Cancel";
            cancel.Click += delegate
            {
                worker.CancelAsync();
                status.Text = "Cancelling after current download step...";
                cancel.Enabled = false;
            };
            Controls.Add(cancel);
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += InstallUpdates;
            worker.ProgressChanged += delegate(object sender, ProgressChangedEventArgs e)
            {
                bar.Value = Math.Max(0, Math.Min(100, e.ProgressPercentage));
                if (e.UserState != null) status.Text = Convert.ToString(e.UserState);
            };
            worker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
            {
                if (e.Cancelled)
                    MessageBox.Show(this, "Update cancelled. Installed applications were not changed.",
                        "VSI Creative Suite", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else if (e.Error != null)
                    MessageBox.Show(this, "Update could not complete:\n\n" + e.Error.Message +
                        "\n\nExisting application backups are retained for recovery.",
                        "VSI Creative Suite", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    MessageBox.Show(this, Convert.ToString(e.Result),
                        "VSI Creative Suite", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            };
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            worker.RunWorkerAsync();
        }

        private void InstallUpdates(object sender, DoWorkEventArgs args)
        {
            string root = Path.GetDirectoryName(typeof(AppUpdatesDialog).Assembly.Location);
            string appsRoot = Path.Combine(root, "apps");
            string cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VSI Creative Suite", "Updates", "AppPackages");
            Directory.CreateDirectory(cache);
            List<AppUpdate> crafts = new List<AppUpdate>();
            List<AppUpdate> hew = new List<AppUpdate>();
            foreach (AppUpdate u in updates) { if (u.IsHew) hew.Add(u); else crafts.Add(u); }
            try
            {
                int index = 0;
                foreach (AppUpdate app in updates)
                {
                    if (worker.CancellationPending) { args.Cancel = true; return; }
                    worker.ReportProgress(index * 65 / updates.Count, "Downloading " + app.Name + " (" + (index + 1) + "/" + updates.Count + ")...");
                    app.DownloadedPath = Path.Combine(cache, Guid.NewGuid().ToString("N") + (app.IsHew ? ".exe" : ".zip"));
                    downloaded.Add(app.DownloadedPath);
                    Download(app, index);
                    if (worker.CancellationPending) { args.Cancel = true; return; }
                    if (!AppUpdates.Verify(app.DownloadedPath, app))
                        throw new InvalidDataException(app.Name + " package failed SHA-256 or file size verification.");
                    if (!app.IsHew)
                    {
                        app.StagePath = Path.Combine(appsRoot, ".vsi-stage-" + app.Id + "-" + Guid.NewGuid().ToString("N"));
                        stages.Add(app.StagePath);
                        ExtractSafely(app.DownloadedPath, app.StagePath);
                        if (FindExecutable(app.StagePath, app.Executable) == null)
                            throw new InvalidDataException(app.Name + " executable missing in archive.");
                    }
                    index++;
                }
                if (worker.CancellationPending) { args.Cancel = true; return; }
                cancelSafeDisable();
                List<string> changed = new List<string>();
                foreach (AppUpdate app in crafts)
                {
                    worker.ReportProgress(70, "Installing " + app.Name + "...");
                    string target = Path.Combine(appsRoot, app.Id);
                    string backup = Path.Combine(appsRoot, ".vsi-backup-" + app.Id + "-" + Guid.NewGuid().ToString("N"));
                    if (Process.GetProcessesByName(app.Id).Length > 0)
                        throw new IOException(app.Name + " is still running. Close it and retry.");
                    bool moved = false;
                    try
                    {
                        if (Directory.Exists(target)) { Directory.Move(target, backup); moved = true; }
                        Directory.Move(app.StagePath, target);
                        AppUpdates.RecordInstalled(app);
                        changed.Add(app.Name + " " + app.Tag);
                        // Backups deliberately remain on disk for recoverability.
                    }
                    catch
                    {
                        if (moved)
                        {
                            // Never delete user files. Keep new files as a recoverable folder.
                            if (Directory.Exists(target))
                                Directory.Move(target, Path.Combine(appsRoot, ".vsi-unverified-" + app.Id + "-" + Guid.NewGuid().ToString("N")));
                            if (Directory.Exists(backup)) Directory.Move(backup, target);
                        }
                        throw;
                    }
                }
                foreach (AppUpdate app in hew)
                {
                    worker.ReportProgress(90, "Installing official Hew update...");
                    if (Process.GetProcessesByName("hew").Length > 0)
                        throw new IOException("Hew is still running. Close it and retry.");
                    string desktop = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Hew.lnk");
                    string commonDesktop = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Hew.lnk");
                    bool hadDesktop = File.Exists(desktop), hadCommonDesktop = File.Exists(commonDesktop);
                    try
                    {
                        ProcessStartInfo info = new ProcessStartInfo(app.DownloadedPath, "/S");
                        info.UseShellExecute = true;
                        using (Process process = Process.Start(info))
                        {
                            if (process == null || !process.WaitForExit(15 * 60 * 1000) || process.ExitCode != 0)
                                throw new IOException("Hew upstream installer did not complete successfully.");
                        }
                    }
                    finally
                    {
                        // Preserve shortcuts users already had. Remove only ones created by this update.
                        try { if (!hadDesktop && File.Exists(desktop)) File.Delete(desktop); } catch { }
                        try { if (!hadCommonDesktop && File.Exists(commonDesktop)) File.Delete(commonDesktop); } catch { }
                    }
                    AppUpdates.RecordInstalled(app);
                    changed.Add(app.Name + " " + app.Tag);
                }
                worker.ReportProgress(100, "Updates finished.");
                args.Result = "Updated applications:\n\n" + string.Join("\n", changed.ToArray()) +
                    "\n\nPlease reopen apps from VSI Creative Suite.";
            }
            finally
            {
                foreach (string file in downloaded)
                    try { if (File.Exists(file)) File.Delete(file); } catch { }
                foreach (string dir in stages)
                    try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch { }
            }
        }

        private void cancelSafeDisable()
        {
            worker.ReportProgress(67, "Downloads verified. Installing applications (do not close)...");
            if (InvokeRequired) BeginInvoke((MethodInvoker)delegate { cancel.Enabled = false; });
            else cancel.Enabled = false;
        }

        private void Download(AppUpdate app, int index)
        {
            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(app.Url);
            request.UserAgent = "VSI-Creative-Suite-Updater/0.3.1";
            request.Timeout = 30000; request.ReadWriteTimeout = 60000;
            using (WebResponse response = request.GetResponse())
            using (Stream input = response.GetResponseStream())
            using (FileStream output = new FileStream(app.DownloadedPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                byte[] buffer = new byte[65536];
                long received = 0;
                int count;
                while ((count = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (worker.CancellationPending) return;
                    received += count;
                    if (received > app.Size) throw new InvalidDataException("Oversized download.");
                    output.Write(buffer, 0, count);
                    int current = (int)Math.Min(65, (index * 65.0 + 65.0 * received / app.Size) / updates.Count);
                    worker.ReportProgress(current, "Downloading " + app.Name + " ...");
                }
            }
        }

        private static string FindExecutable(string folder, string executable)
        {
            if (!Directory.Exists(folder)) return null;
            string[] files = Directory.GetFiles(folder, executable, SearchOption.AllDirectories);
            return files.Length > 0 ? files[0] : null;
        }

        // Explicitly reject traversal, symlinks and zip bombs. Do not extract archives
        // directly into any existing installed app directory.
        internal static void ExtractSafely(string archivePath, string target)
        {
            Directory.CreateDirectory(target);
            string prefix = Path.GetFullPath(target).TrimEnd(Path.DirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            long total = 0L;
            using (FileStream stream = File.OpenRead(archivePath))
            using (ZipArchive zip = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                if (zip.Entries.Count > 20000) throw new InvalidDataException("Archive entry limit exceeded.");
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    string name = entry.FullName.Replace('/', Path.DirectorySeparatorChar)
                        .Replace('\\', Path.DirectorySeparatorChar);
                    if (string.IsNullOrEmpty(name)) continue;
                    if (name.IndexOf(':') >= 0 || name.IndexOf('\0') >= 0 ||
                        Path.IsPathRooted(name))
                        throw new InvalidDataException("Unsafe archive path.");
                    string full = Path.GetFullPath(Path.Combine(prefix, name));
                    if (!full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Archive path traversal blocked.");
                    int unixType = (entry.ExternalAttributes >> 16) & 0xF000;
                    if (unixType == 0xA000) throw new InvalidDataException("Archive symlink blocked.");
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(full);
                        continue;
                    }
                    total += entry.Length;
                    if (total > 3221225472L)
                        throw new InvalidDataException("Uncompressed archive too large.");
                    Directory.CreateDirectory(Path.GetDirectoryName(full));
                    using (Stream input = entry.Open())
                    using (FileStream output = new FileStream(full, FileMode.CreateNew, FileAccess.Write))
                    {
                        byte[] buffer = new byte[65536];
                        int n;
                        while ((n = input.Read(buffer, 0, buffer.Length)) > 0)
                            output.Write(buffer, 0, n);
                    }
                }
            }
        }
    }
}
