using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace VsiCreativeSuite
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LauncherForm());
        }
    }

    internal sealed class SuiteApp
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Detail;
        public readonly string ExeName;
        public readonly string Symbol;
        public readonly Color Accent;

        public SuiteApp(string id, string name, string detail, string exe, string symbol, Color accent)
        {
            Id = id; Name = name; Detail = detail; ExeName = exe;
            Symbol = symbol; Accent = accent;
        }
    }

    internal sealed class LauncherForm : Form
    {
        private static readonly Color Background = Color.FromArgb(10, 13, 19);
        private static readonly Color Muted = Color.FromArgb(149, 157, 171);
        private readonly List<SuiteApp> apps = new List<SuiteApp>
        {
            new SuiteApp("hew", "Hew 3D", "3D Modeling", "hew.exe", "3D", Color.FromArgb(230, 65, 36)),
            new SuiteApp("pdfcraft", "PdfCraft", "PDF Tools", "pdfcraft.exe", "PDF", Color.FromArgb(0, 143, 129)),
            new SuiteApp("photocraft", "PhotoCraft", "Image Editing", "photocraft.exe", "P", Color.FromArgb(0, 117, 195)),
            new SuiteApp("gridcraft", "GridCraft", "Spreadsheets", "gridcraft.exe", "X", Color.FromArgb(43, 156, 76)),
            new SuiteApp("wordcraft", "WordCraft", "Documents", "wordcraft.exe", "W", Color.FromArgb(41, 103, 205)),
            new SuiteApp("cadcraft", "CADCraft", "CAD Drafting", "cadcraft.exe", "CAD", Color.FromArgb(133, 72, 210))
        };

        private readonly List<AppCard> cards = new List<AppCard>();
        private readonly Panel cardHost = new Panel();
        private readonly Panel top = new Panel();
        private readonly Label heading = new Label();
        private readonly Label subheading = new Label();
        private readonly Panel footer = new Panel();
        private bool companyHover;
        private bool updateHover;
        private readonly string installRoot;
        private readonly bool modernWindows;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct OsVersion
        {
            public int Size;
            public int Major;
            public int Minor;
            public int Build;
            public int Platform;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string ServicePack;
        }

        [DllImport("ntdll.dll")]
        private static extern int RtlGetVersion(ref OsVersion version);

        private static bool SupportsModernApps()
        {
            try
            {
                OsVersion version = new OsVersion();
                version.Size = Marshal.SizeOf(typeof(OsVersion));
                return RtlGetVersion(ref version) == 0 && version.Major >= 10;
            }
            catch { return false; }
        }

        public LauncherForm()
        {
            installRoot = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            modernWindows = SupportsModernApps();
            Text = "VSI Creative Suite";
            BackColor = Background;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10f);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            MinimumSize = new Size(1050, 720);
            ClientSize = new Size(1180, 760);
            DoubleBuffered = true;
            KeyPreview = true;
            KeyDown += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Escape) Close(); };

            top.Height = 116;
            top.Dock = DockStyle.Top;
            top.BackColor = Color.FromArgb(17, 17, 21);
            top.Paint += PaintHeader;
            top.MouseDown += DragWindow;
            Controls.Add(top);

            PictureBox logo = new PictureBox();
            logo.SetBounds(39, 14, 78, 88);
            logo.SizeMode = PictureBoxSizeMode.Zoom;
            logo.BackColor = Color.Transparent;
            string logoPath = Path.Combine(installRoot, "assets", "vsi-logo.png");
            if (File.Exists(logoPath))
            {
                try { logo.Image = Image.FromFile(logoPath); }
                catch { /* Broken logo must not prevent app launch. */ }
            }
            logo.MouseDown += DragWindow;
            top.Controls.Add(logo);

            Label title = NewLabel("VSI Creative Suite", 133, 19, 650, 53,
                new Font("Segoe UI", 27f, FontStyle.Bold), Color.White);
            title.MouseDown += DragWindow;
            top.Controls.Add(title);
            Label corporate = NewLabel("V E R T I C A L   S P A C E   I N T E R I O R S ,   I N C .", 136, 72, 660, 25,
                new Font("Segoe UI", 8.2f), Color.FromArgb(184, 189, 200));
            corporate.MouseDown += DragWindow;
            top.Controls.Add(corporate);

            Button minimize = WindowButton("—", 51);
            minimize.Click += delegate { WindowState = FormWindowState.Minimized; };
            top.Controls.Add(minimize);
            Button close = WindowButton("×", 5);
            close.Click += delegate { Close(); };
            top.Controls.Add(close);

            heading.Text = "Create without limits.";
            heading.Font = new Font("Segoe UI", 30f, FontStyle.Bold);
            heading.ForeColor = Color.White;
            heading.AutoSize = false;
            heading.SetBounds(42, 149, 1040, 67);
            Controls.Add(heading);

            subheading.Text = modernWindows
                ? "Your essential workspace, ready offline."
                : "Legacy launcher mode. Applications require Windows 10 or newer.";
            subheading.Font = new Font("Segoe UI", 14f);
            subheading.ForeColor = Muted;
            subheading.AutoSize = false;
            subheading.SetBounds(44, 215, 1030, 38);
            Controls.Add(subheading);

            cardHost.SetBounds(43, 284, ClientSize.Width - 86, ClientSize.Height - 366);
            cardHost.Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                AnchorStyles.Left | AnchorStyles.Right;
            cardHost.AutoScroll = true;
            cardHost.BackColor = Background;
            Controls.Add(cardHost);
            for (int i = 0; i < apps.Count; i++)
            {
                AppCard card = new AppCard(apps[i], modernWindows, FindExecutable, Launch);
                cards.Add(card);
                cardHost.Controls.Add(card);
            }

            footer.Dock = DockStyle.Bottom;
            footer.Height = 64;
            footer.BackColor = Color.FromArgb(18, 21, 28);
            footer.Paint += delegate(object sender, PaintEventArgs e)
            {
                using (Pen p = new Pen(Color.FromArgb(54, 62, 75)))
                    e.Graphics.DrawLine(p, 0, 0, footer.Width, 0);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle link = CompanyLinkBounds();
                using (GraphicsPath shape = UiShapes.Rounded(link, 9))
                using (SolidBrush fill = new SolidBrush(companyHover
                    ? Color.FromArgb(69, 37, 34) : Color.FromArgb(29, 29, 37)))
                using (Pen border = new Pen(companyHover
                    ? Color.FromArgb(252, 120, 70) : Color.FromArgb(172, 77, 55), 1f))
                {
                    e.Graphics.FillPath(fill, shape);
                    e.Graphics.DrawPath(border, shape);
                }
                using (Font linkFont = new Font("Segoe UI", 9f, FontStyle.Bold))
                    TextRenderer.DrawText(e.Graphics, "COMPANY EDITION   ↗", linkFont, link,
                        Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                Rectangle updateButton = UpdateLinkBounds();
                using (GraphicsPath shape = UiShapes.Rounded(updateButton, 9))
                using (SolidBrush fill = new SolidBrush(updateHover
                    ? Color.FromArgb(42, 59, 54) : Color.FromArgb(27, 38, 38)))
                using (Pen line = new Pen(updateHover
                    ? Color.FromArgb(64, 190, 128) : Color.FromArgb(65, 100, 85), 1f))
                {
                    e.Graphics.FillPath(fill, shape);
                    e.Graphics.DrawPath(line, shape);
                }
                using (Font updateFont = new Font("Segoe UI", 9f, FontStyle.Bold))
                    TextRenderer.DrawText(e.Graphics, "CHECK FOR UPDATES", updateFont,
                        updateButton, Color.FromArgb(112, 227, 160),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            footer.MouseMove += delegate(object sender, MouseEventArgs e)
            {
                bool hovering = CompanyLinkBounds().Contains(e.Location);
                bool overUpdate = UpdateLinkBounds().Contains(e.Location);
                footer.Cursor = hovering || overUpdate ? Cursors.Hand : Cursors.Default;
                if (hovering != companyHover || overUpdate != updateHover)
                {
                    companyHover = hovering; updateHover = overUpdate; footer.Invalidate();
                }
            };
            footer.MouseLeave += delegate
            {
                if (companyHover || updateHover)
                {
                    companyHover = false; updateHover = false; footer.Invalidate();
                }
            };
            footer.MouseClick += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button != MouseButtons.Left) return;
                if (CompanyLinkBounds().Contains(e.Location))
                    OpenCompanyWebsite();
                else if (UpdateLinkBounds().Contains(e.Location))
                {
                    if (modernWindows) SuiteUpdater.Check(this, true);
                    else MessageBox.Show(this, "Updates require Windows 10 or newer.",
                        "VSI Creative Suite", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            Controls.Add(footer);
            Label help = NewLabel("There's issue? Contact VSI I.T", ClientSize.Width - 338, 16, 300, 32,
                new Font("Segoe UI", 10f), Color.FromArgb(206, 212, 224));
            help.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            help.TextAlign = ContentAlignment.MiddleRight;
            footer.Controls.Add(help);

            Shown += delegate { if (modernWindows) SuiteUpdater.Check(this, false); };
            Resize += delegate { LayoutCards(); };
            Activated += delegate { RefreshCards(); };
            LayoutCards();
            RefreshCards();
        }

        private Button WindowButton(string text, int rightMargin)
        {
            Button b = new Button();
            b.Text = text;
            b.SetBounds(ClientSize.Width - rightMargin - 38, 26, 38, 36);
            b.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Color.FromArgb(24, 26, 35);
            b.FlatAppearance.BorderColor = Color.FromArgb(54, 61, 75);
            b.FlatAppearance.BorderSize = 1;
            b.ForeColor = Color.White;
            b.Font = new Font("Segoe UI", 17f);
            return b;
        }

        private static Label NewLabel(string text, int x, int y, int width, int height, Font font, Color color)
        {
            Label l = new Label();
            l.Text = text; l.Location = new Point(x, y); l.Size = new Size(width, height);
            l.ForeColor = color; l.BackColor = Color.Transparent; l.Font = font;
            l.TextAlign = ContentAlignment.MiddleLeft;
            return l;
        }

        private void DragWindow(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, 0xA1, 0x2, 0);
            }
        }

        private void PaintHeader(object sender, PaintEventArgs e)
        {
            Rectangle rect = top.ClientRectangle;
            using (LinearGradientBrush brush = new LinearGradientBrush(rect,
                Color.FromArgb(17, 20, 29), Color.FromArgb(70, 20, 19), 0f))
                e.Graphics.FillRectangle(brush, rect);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (Brush accent = new SolidBrush(Color.FromArgb(220, 164, 32, 20)))
                e.Graphics.FillPolygon(accent, new Point[]
                {
                    new Point(top.Width - 470, 0), new Point(top.Width - 369, 0),
                    new Point(top.Width - 445, 115), new Point(top.Width - 546, 115)
                });
            using (Pen highlight = new Pen(Color.FromArgb(230, 243, 74, 34), 2f))
                e.Graphics.DrawLine(highlight, top.Width - 469, 0, top.Width - 545, 115);
            using (Pen separator = new Pen(Color.FromArgb(50, 61, 76)))
                e.Graphics.DrawLine(separator, 0, top.Height - 1, top.Width, top.Height - 1);
            Rectangle badge = new Rectangle(top.Width - 315, 36, 183, 34);
            using (GraphicsPath shape = UiShapes.Rounded(badge, 17))
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(19, 28, 27)))
            using (Pen line = new Pen(Color.FromArgb(54, 115, 85), 1f))
            {
                e.Graphics.FillPath(fill, shape);
                e.Graphics.DrawPath(line, shape);
            }
            using (Font badgeFont = new Font("Segoe UI", 8.8f, FontStyle.Bold))
                TextRenderer.DrawText(e.Graphics, modernWindows ? "●  LOCAL & OFFLINE" : "●  LEGACY MODE",
                    badgeFont, badge, Color.FromArgb(78, 225, 145),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private Rectangle CompanyLinkBounds()
        {
            return new Rectangle(40, 14, 234, 37);
        }

        private Rectangle UpdateLinkBounds()
        {
            return new Rectangle(288, 14, 188, 37);
        }

        private void LayoutCards()
        {
            int gap = 18;
            int columns = 3;
            int rows = (cards.Count + columns - 1) / columns;
            int scrollbar = rows > 2 ? SystemInformation.VerticalScrollBarWidth : 0;
            int width = Math.Max(210, (cardHost.ClientSize.Width - gap * 2 - scrollbar - 2) / columns);
            int height = Math.Max(164, (ClientSize.Height - 366 - gap) / 2);
            for (int i = 0; i < cards.Count; i++)
                cards[i].SetBounds((i % 3) * (width + gap),
                    (i / 3) * (height + gap), width, height);
            cardHost.AutoScrollMinSize = new Size(0,
                rows > 2 ? rows * (height + gap) - gap : 0);
        }

        private void RefreshCards()
        {
            for (int i = 0; i < cards.Count; i++) cards[i].RefreshState();
        }

        internal void ApplyCatalog(List<ApprovedApp> catalog)
        {
            if (catalog == null || IsDisposed) return;
            foreach (ApprovedApp item in catalog)
            {
                AppCard existing = null;
                for (int i = 0; i < apps.Count; i++)
                    if (apps[i].Id == item.Id) { existing = cards[i]; break; }
                if (existing != null)
                {
                    existing.SetInstallable(true);
                    continue;
                }
                try
                {
                    Color accent = ColorTranslator.FromHtml(item.Accent);
                    SuiteApp app = new SuiteApp(item.Id, item.Name, item.Detail,
                        item.Executable, item.Symbol, accent);
                    AppCard card = new AppCard(app, modernWindows, FindExecutable, Launch);
                    card.SetInstallable(true);
                    apps.Add(app);
                    cards.Add(card);
                    cardHost.Controls.Add(card);
                }
                catch { /* Invalid optional catalog tile is ignored. */ }
            }
            LayoutCards();
            RefreshCards();
        }

        private void OpenCompanyWebsite()
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://vsiphils.com/") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not open the VSI website:\n" + ex.Message,
                    "VSI Creative Suite", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void Launch(SuiteApp app)
        {
            string executable = FindExecutable(app);
            if (!modernWindows)
            {
                MessageBox.Show(this, "Application is unavailable on this PC.",
                    "VSI Creative Suite", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (executable == null)
            {
                AppUpdates.Check(this, true);
                return;
            }
            try
            {
                ProcessStartInfo info = new ProcessStartInfo(executable);
                info.WorkingDirectory = Path.GetDirectoryName(executable);
                info.UseShellExecute = true;
                Process.Start(info);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not launch " + app.Name + ":\n" + ex.Message,
                    "VSI Creative Suite", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string FindExecutable(SuiteApp app)
        {
            string result = FindInside(Path.Combine(installRoot, "apps", app.Id), app.ExeName);
            if (result != null || app.Id != "hew") return result;
            // Hew is distributed by its upstream native NSIS installer.
            foreach (string location in new string[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Hew"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Hew"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Hew"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Hew")
            })
            {
                result = FindInside(location, "hew.exe");
                if (result != null) return result;
            }
            return FindHewFromRegistry();
        }

        private static string FindInside(string folder, string exe)
        {
            try
            {
                if (!Directory.Exists(folder)) return null;
                string direct = Path.Combine(folder, exe);
                if (File.Exists(direct)) return direct;
                string[] matches = Directory.GetFiles(folder, exe, SearchOption.AllDirectories);
                return matches.Length > 0 ? matches[0] : null;
            }
            catch { return null; }
        }

        private static string FindHewFromRegistry()
        {
            RegistryKey[] hives = new RegistryKey[] { Registry.CurrentUser, Registry.LocalMachine };
            string[] subkeys = new string[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };
            foreach (RegistryKey hive in hives)
            foreach (string path in subkeys)
            {
                try
                {
                    using (RegistryKey parent = hive.OpenSubKey(path))
                    {
                        if (parent == null) continue;
                        foreach (string child in parent.GetSubKeyNames())
                        {
                            using (RegistryKey entry = parent.OpenSubKey(child))
                            {
                                if (entry == null) continue;
                                string name = Convert.ToString(entry.GetValue("DisplayName", ""));
                                if (!name.Equals("Hew", StringComparison.OrdinalIgnoreCase) &&
                                    !name.StartsWith("Hew ", StringComparison.OrdinalIgnoreCase)) continue;
                                string install = Convert.ToString(entry.GetValue("InstallLocation", ""));
                                string found = FindInside(install, "hew.exe");
                                if (found != null) return found;
                                string icon = Convert.ToString(entry.GetValue("DisplayIcon", "")).Trim('"');
                                if (icon.EndsWith("hew.exe", StringComparison.OrdinalIgnoreCase) && File.Exists(icon))
                                    return icon;
                            }
                        }
                    }
                }
                catch { /* Some system registry entries cannot be read by the current user. */ }
            }
            return null;
        }

    }

    internal sealed class AppCard : Panel
    {
        private readonly SuiteApp app;
        private readonly bool supported;
        private readonly Func<SuiteApp, string> locate;
        private readonly Action<SuiteApp> launch;
        private bool available;
        private bool installable;
        private bool hovering;
        private string statusText;
        private string buttonText;
        private Color statusColor;

        public AppCard(SuiteApp definition, bool isSupported,
            Func<SuiteApp, string> fileLocator, Action<SuiteApp> openAction)
        {
            app = definition; supported = isSupported; locate = fileLocator; launch = openAction;
            BackColor = Color.FromArgb(10, 13, 19);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            TabStop = true;
            AccessibleRole = AccessibleRole.PushButton;
            AccessibleName = "Open " + app.Name;
            RefreshState();
        }

        private Rectangle ButtonBounds()
        {
            return new Rectangle(22, Height - 62, Math.Max(90, Width - 44), 43);
        }

        public void SetInstallable(bool value)
        {
            installable = value;
            RefreshState();
        }

        public void RefreshState()
        {
            if (!supported)
            {
                available = false;
                statusText = "●  Windows 10+ required";
                buttonText = "UNAVAILABLE";
            }
            else
            {
                available = locate(app) != null;
                statusText = available ? "●  Installed   •   Offline" :
                    installable ? "●  Available to install" : "●  App missing";
                buttonText = available ? "OPEN" : installable ? "INSTALL" : "MISSING";
            }
            statusColor = available ? Color.FromArgb(74, 218, 116) :
                installable ? Color.FromArgb(105, 188, 242) : Color.FromArgb(226, 168, 91);
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            bool overButton = (available || installable) && ButtonBounds().Contains(e.Location);
            Cursor = overButton ? Cursors.Hand : Cursors.Default;
            if (overButton != hovering) { hovering = overButton; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (hovering) { hovering = false; Invalidate(); }
            Cursor = Cursors.Default;
            base.OnMouseLeave(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if ((available || installable) && e.Button == MouseButtons.Left && ButtonBounds().Contains(e.Location))
                launch(app);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if ((available || installable) && (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space))
            {
                launch(app);
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle card = new Rectangle(1, 1, Width - 3, Height - 3);
            if (card.Width <= 0 || card.Height <= 0) return;
            using (GraphicsPath shape = UiShapes.Rounded(card, 15))
            using (LinearGradientBrush surface = new LinearGradientBrush(card,
                Color.FromArgb(29, 33, 44), Color.FromArgb(18, 21, 28), LinearGradientMode.Vertical))
            using (Pen border = new Pen(Color.FromArgb(66, 76, 92), 1f))
            {
                e.Graphics.FillPath(surface, shape);
                e.Graphics.DrawPath(border, shape);
            }
            using (Pen accent = new Pen(Color.FromArgb(198, app.Accent), 2f))
                e.Graphics.DrawLine(accent, 23, 2, Math.Min(67, Width - 23), 2);

            // All card graphics are rendered on this ONE surface. No child controls
            // means no opaque square backgrounds or native WinForms button artifacts.
            bool compact = Height < 200;
            int tileSize = compact ? 57 : 62;
            Rectangle icon = new Rectangle(22, compact ? 18 : 21, tileSize, tileSize);
            using (GraphicsPath shape = UiShapes.Rounded(icon, 11))
            using (LinearGradientBrush fill = new LinearGradientBrush(icon,
                ControlPaint.Light(app.Accent, 0.08f), ControlPaint.Dark(app.Accent, 0.36f), 105f))
            using (Pen border = new Pen(ControlPaint.Light(app.Accent, 0.30f), 1f))
            {
                e.Graphics.FillPath(fill, shape);
                e.Graphics.DrawPath(border, shape);
            }
            using (Font markFont = new Font("Segoe UI", app.Symbol.Length > 2 ? 12f : 21f, FontStyle.Bold))
                TextRenderer.DrawText(e.Graphics, app.Symbol, markFont, icon, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);

            int textX = icon.Right + 17;
            int textWidth = Math.Max(120, Width - textX - 14);
            using (Font nameFont = new Font("Segoe UI", 18f, FontStyle.Bold))
                TextRenderer.DrawText(e.Graphics, app.Name, nameFont,
                    new Rectangle(textX, compact ? 15 : 20, textWidth, 33), Color.White,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            using (Font detailFont = new Font("Segoe UI", 10f))
                TextRenderer.DrawText(e.Graphics, app.Detail, detailFont,
                    new Rectangle(textX + 1, compact ? 50 : 57, textWidth, 22),
                    Color.FromArgb(171, 181, 196), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            using (Font statusFont = new Font("Segoe UI", 9f))
                TextRenderer.DrawText(e.Graphics, statusText ?? "", statusFont,
                    new Rectangle(textX + 1, compact ? 76 : 85, textWidth, 22), statusColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            Rectangle button = ButtonBounds();
            Color first = (available || installable) ? (hovering ? Color.FromArgb(250, 56, 31) : Color.FromArgb(231, 37, 30)) : Color.FromArgb(66, 73, 84);
            Color last = (available || installable) ? (hovering ? Color.FromArgb(255, 172, 45) : Color.FromArgb(252, 149, 27)) : Color.FromArgb(86, 92, 102);
            using (GraphicsPath shape = UiShapes.Rounded(button, 9))
            using (LinearGradientBrush fill = new LinearGradientBrush(button, first, last, 0f))
            {
                e.Graphics.FillPath(fill, shape);
            }
            using (Font buttonFont = new Font("Segoe UI", 10f, FontStyle.Bold))
                TextRenderer.DrawText(e.Graphics, buttonText ?? "OPEN", buttonFont, button,
                    Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            if (available || installable)
            {
                using (Pen arrow = new Pen(Color.White, 1.7f))
                {
                    int x = button.Right - 22;
                    int y = button.Top + button.Height / 2;
                    e.Graphics.DrawLine(arrow, x - 3, y - 5, x + 3, y);
                    e.Graphics.DrawLine(arrow, x + 3, y, x - 3, y + 5);
                }
            }
            if (Focused)
            {
                using (Pen focus = new Pen(Color.FromArgb(210, 245, 141, 56), 1f))
                using (GraphicsPath focusShape = UiShapes.Rounded(new Rectangle(3, 3, Width - 7, Height - 7), 13))
                    e.Graphics.DrawPath(focus, focusShape);
            }
        }
    }

    internal static class UiShapes
    {
        public static GraphicsPath Rounded(Rectangle area, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(area.Left, area.Top, diameter, diameter, 180f, 90f);
            path.AddArc(area.Right - diameter, area.Top, diameter, diameter, 270f, 90f);
            path.AddArc(area.Right - diameter, area.Bottom - diameter, diameter, diameter, 0f, 90f);
            path.AddArc(area.Left, area.Bottom - diameter, diameter, diameter, 90f, 90f);
            path.CloseFigure();
            return path;
        }
    }

}
