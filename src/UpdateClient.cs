using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace VsiCreativeSuite
{
    internal sealed class UpdateRelease
    {
        public Version Version;
        public string Tag;
        public Uri DownloadUrl;
        public string Sha256;
        public long Size;
    }

    internal static class SuiteUpdater
    {
        public const string CurrentVersion = "0.3.0";
        private const string ApiUrl = "https://api.github.com/repos/vsiphils/vsi-creative-suite/releases/latest";
        private const string AssetName = "VSI-Creative-Suite-Setup-Win10-11-x64.exe";
        private static readonly Version InstalledVersion = new Version(CurrentVersion);
        private static bool checking;

        // This performs no network request on the UI thread. Failures during the
        // automatic check are non-blocking: locally installed apps still work.
        public static void Check(Form owner, bool manual)
        {
            if (checking || owner.IsDisposed) return;
            checking = true;
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += delegate(object sender, DoWorkEventArgs e)
            {
                e.Result = FindNewRelease();
            };
            worker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
            {
                checking = false;
                if (owner.IsDisposed) return;
                if (e.Error != null)
                {
                    if (manual)
                        MessageBox.Show(owner, "Could not check for updates.\n\n" + e.Error.Message +
                            "\n\nYour installed apps will continue to work offline.",
                            "VSI Creative Suite", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                UpdateRelease release = e.Result as UpdateRelease;
                if (release == null)
                {
                    if (manual)
                        MessageBox.Show(owner, "VSI Creative Suite is up to date (v" +
                            CurrentVersion + ").", "VSI Creative Suite",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                DialogResult answer = MessageBox.Show(owner,
                    "VSI Creative Suite v" + release.Version + " is available.\n\n" +
                    "The update is a complete offline installer. It contains the app " +
                    "versions packaged in this VSI release, including any newly added apps.\n\n" +
                    "Close any open documents before updating.\n\n" +
                    "Do you want to download and install the update?",
                    "VSI Creative Suite — Update Available",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (answer != DialogResult.Yes) return;
                using (UpdateDownloadForm progress = new UpdateDownloadForm(release))
                {
                    if (progress.ShowDialog(owner) != DialogResult.OK) return;
                    try
                    {
                        // Verify a second time just before launching the installer.
                        if (!MatchesSha256(progress.VerifiedInstallerPath, release.Sha256))
                            throw new InvalidOperationException("Installer hash changed after download.");
                        ProcessStartInfo info = new ProcessStartInfo(progress.VerifiedInstallerPath);
                        info.UseShellExecute = true;
                        // Interactive Inno Setup lets users handle running apps safely.
                        Process.Start(info);
                        owner.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(owner, "The update could not start:\n" + ex.Message,
                            "VSI Creative Suite", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };
            worker.RunWorkerAsync();
        }

        private static UpdateRelease FindNewRelease()
        {
            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072; // TLS 1.2
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ApiUrl);
            request.UserAgent = "VSI-Creative-Suite-Updater/0.3.0";
            request.Accept = "application/vnd.github+json";
            request.Timeout = 12000;
            request.ReadWriteTimeout = 12000;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            string body;
            using (WebResponse response = request.GetResponse())
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                body = reader.ReadToEnd();
            return ParseRelease(body);
        }

        // Isolated parser makes update trust checks testable without Internet access.
        internal static UpdateRelease ParseRelease(string body)
        {
            Dictionary<string, object> data =
                new JavaScriptSerializer().DeserializeObject(body) as Dictionary<string, object>;
            if (data == null) throw new InvalidDataException("Invalid GitHub release response.");
            if (GetBool(data, "draft") || GetBool(data, "prerelease")) return null;

            string tag = GetString(data, "tag_name");
            Match match = Regex.Match(tag, @"^v(\d+\.\d+\.\d+)$", RegexOptions.CultureInvariant);
            if (!match.Success) return null;
            Version remote;
            if (!Version.TryParse(match.Groups[1].Value, out remote) ||
                remote.CompareTo(InstalledVersion) <= 0) return null;

            object assetsValue;
            if (!data.TryGetValue("assets", out assetsValue))
                throw new InvalidDataException("Release has no assets.");
            object[] assets = assetsValue as object[];
            if (assets == null) throw new InvalidDataException("Invalid release assets.");

            foreach (object item in assets)
            {
                Dictionary<string, object> asset = item as Dictionary<string, object>;
                if (asset == null || GetString(asset, "name") != AssetName) continue;
                string digest = GetString(asset, "digest");
                if (!Regex.IsMatch(digest, @"^sha256:[0-9a-fA-F]{64}$"))
                    throw new InvalidDataException("Release installer is missing a SHA256 digest.");
                string url = GetString(asset, "browser_download_url");
                Uri uri;
                if (!Uri.TryCreate(url, UriKind.Absolute, out uri) ||
                    uri.Scheme != Uri.UriSchemeHttps ||
                    !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
                    !uri.AbsolutePath.Equals(
                        "/vsiphils/vsi-creative-suite/releases/download/" + tag + "/" + AssetName,
                        StringComparison.Ordinal))
                    throw new InvalidDataException("Unexpected update download location.");
                long size = GetLong(asset, "size");
                if (size < 1048576L || size > 3221225472L)
                    throw new InvalidDataException("Unexpected update installer size.");
                return new UpdateRelease
                {
                    Version = remote, Tag = tag, DownloadUrl = uri,
                    Sha256 = digest.Substring(7), Size = size
                };
            }
            throw new InvalidDataException("Full offline installer is missing from the release.");
        }

        private static string GetString(Dictionary<string, object> data, string key)
        {
            object value;
            return data.TryGetValue(key, out value) && value != null ?
                Convert.ToString(value) : "";
        }
        private static bool GetBool(Dictionary<string, object> data, string key)
        {
            object value;
            return data.TryGetValue(key, out value) && value is bool && (bool)value;
        }
        private static long GetLong(Dictionary<string, object> data, string key)
        {
            object value;
            return data.TryGetValue(key, out value) ? Convert.ToInt64(value) : -1L;
        }

        public static bool MatchesSha256(string path, string expectedHex)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 sha = SHA256.Create())
            {
                string actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
                return string.Equals(actual, expectedHex, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    internal sealed class UpdateDownloadForm : Form
    {
        private readonly UpdateRelease release;
        private readonly ProgressBar progress = new ProgressBar();
        private readonly Label status = new Label();
        private readonly Button cancel = new Button();
        private WebClient client;
        private string partialPath;
        public string VerifiedInstallerPath { get; private set; }

        public UpdateDownloadForm(UpdateRelease update)
        {
            release = update;
            Text = "Downloading VSI Creative Suite v" + release.Version;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;
            ClientSize = new Size(480, 148);
            Font = new Font("Segoe UI", 10f);
            BackColor = Color.FromArgb(19, 23, 31);
            ForeColor = Color.White;

            status.Text = "Downloading full offline suite...";
            status.SetBounds(22, 18, 430, 30);
            status.ForeColor = Color.White;
            Controls.Add(status);

            progress.SetBounds(22, 60, 436, 24);
            progress.Minimum = 0;
            progress.Maximum = 100;
            Controls.Add(progress);

            cancel.Text = "Cancel";
            cancel.SetBounds(360, 101, 98, 32);
            cancel.Click += delegate
            {
                cancel.Enabled = false;
                status.Text = "Cancelling download...";
                if (client != null) client.CancelAsync();
            };
            Controls.Add(cancel);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try
            {
                string root = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "VSI Creative Suite", "Updates");
                Directory.CreateDirectory(root);
                string unique = Guid.NewGuid().ToString("N");
                partialPath = Path.Combine(root, "VSI-Update-" + unique + ".partial");
                VerifiedInstallerPath = Path.Combine(root, "VSI-Update-" + unique + ".exe");
                client = new WebClient();
                client.Headers.Add(HttpRequestHeader.UserAgent, "VSI-Creative-Suite-Updater/0.3.0");
                client.DownloadProgressChanged += delegate(object sender, DownloadProgressChangedEventArgs args)
                {
                    progress.Value = Math.Max(0, Math.Min(100, args.ProgressPercentage));
                    status.Text = "Downloading full offline suite... " + args.ProgressPercentage + "%";
                };
                client.DownloadFileCompleted += DownloadComplete;
                client.DownloadFileAsync(release.DownloadUrl, partialPath);
            }
            catch (Exception ex) { Fail("Could not start download: " + ex.Message); }
        }

        private void DownloadComplete(object sender, AsyncCompletedEventArgs e)
        {
            if (client != null) { client.Dispose(); client = null; }
            if (e.Cancelled) { Cleanup(); DialogResult = DialogResult.Cancel; Close(); return; }
            if (e.Error != null) { Fail("Download failed: " + e.Error.Message); return; }

            status.Text = "Verifying downloaded installer...";
            progress.Style = ProgressBarStyle.Marquee;
            cancel.Enabled = false;
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += delegate(object s, DoWorkEventArgs args)
            {
                FileInfo file = new FileInfo(partialPath);
                if (file.Length != release.Size)
                    throw new InvalidDataException("Downloaded file size does not match GitHub release.");
                if (!SuiteUpdater.MatchesSha256(partialPath, release.Sha256))
                    throw new InvalidDataException("Installer SHA256 verification failed.");
                File.Move(partialPath, VerifiedInstallerPath);
            };
            worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
            {
                if (args.Error != null) { Fail("Update verification failed: " + args.Error.Message); return; }
                DialogResult = DialogResult.OK;
                Close();
            };
            worker.RunWorkerAsync();
        }

        private void Cleanup()
        {
            try { if (!string.IsNullOrEmpty(partialPath) && File.Exists(partialPath)) File.Delete(partialPath); }
            catch { /* Never delete unrelated files or hide original failure. */ }
        }

        private void Fail(string message)
        {
            Cleanup();
            MessageBox.Show(this, message + "\n\nNo installed apps were changed.",
                "VSI Creative Suite", MessageBoxButtons.OK, MessageBoxIcon.Error);
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
