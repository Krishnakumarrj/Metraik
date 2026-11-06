using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Metraik
{
    internal sealed class PopupForm : Form
    {
        public Action RefreshRequested;
        public Action OpenServiceRequested;
        public Action SettingsRequested;

        internal DateTime LastDeactivatedUtc { get; private set; }
        private AppSettings settings;
        private readonly float scale;
        private readonly Button refreshButton;
        private readonly Button openButton;
        private readonly Button settingsButton;

        public PopupForm() : this(0) { }

        internal PopupForm(float requestedScale)
        {
            AutoScaleMode = AutoScaleMode.None;
            using (Graphics graphics = CreateGraphics())
                scale = requestedScale > 0 ? requestedScale : graphics.DpiX / 96f;
            ClientSize = new Size((int)Math.Round(416 * scale), (int)Math.Round(413 * scale));
            Text = "Metraik usage";
            AccessibleName = "Metraik usage overview";
            BackColor = Ui.Background;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            KeyPreview = true;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            refreshButton = ActionButton("Refresh", "Refresh display only", new Rectangle(201, 365, 84, 34), 0);
            refreshButton.Click += delegate { if (RefreshRequested != null) RefreshRequested(); };
            openButton = ActionButton("Open service", "Open selected service in browser", new Rectangle(293, 365, 105, 34), 1);
            openButton.Click += delegate { Hide(); if (OpenServiceRequested != null) OpenServiceRequested(); };
            settingsButton = ActionButton("...", "Settings", new Rectangle(370, 18, 29, 29), 2);
            settingsButton.Click += delegate { Hide(); if (SettingsRequested != null) SettingsRequested(); };
            using (GraphicsPath rounded = Ui.RoundedRectangle(new RectangleF(0, 0, Width, Height), 18 * scale))
                Region = new Region(rounded);
        }

        private Button ActionButton(string text, string accessibleName, Rectangle logical, int order)
        {
            Button button = new Button();
            button.Text = text;
            button.AccessibleName = accessibleName;
            button.TabIndex = order;
            button.SetBounds((int)(logical.X * scale), (int)(logical.Y * scale),
                (int)(logical.Width * scale), (int)(logical.Height * scale));
            button.BackColor = Ui.Panel;
            button.ForeColor = Ui.Text;
            button.Font = Ui.CanvasFont(9 * scale, FontStyle.Regular);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Ui.Border;
            Controls.Add(button);
            return button;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams p = base.CreateParams;
                p.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW: no separate Alt+Tab window
                p.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return p;
            }
        }

        public void SetData(AppSettings data)
        {
            settings = data.Copy();
            DateTime now = DateTime.Now;
            AccessibleDescription = String.Format("{0}, {1}. Active {2}. Weekly {3}. Entered {4:g}.",
                settings.Provider, settings.IsDemo ? "demo data" : "manual data",
                UsageState.ActiveKnown(settings, now) ? settings.ActiveRemaining + " percent remaining" : "unknown; update needed",
                UsageState.WeeklyKnown(settings, now) ? settings.WeeklyRemaining + " percent remaining" : "unknown; update needed",
                settings.LastEditedLocal);
            Invalidate();
        }

        internal static Point PopupPosition(Rectangle area, Size size)
        {
            return new Point(Math.Max(area.Left, area.Right - size.Width - 12),
                Math.Max(area.Top, area.Bottom - size.Height - 12));
        }

        public void ShowNearTray()
        {
            // On tray click the pointer is on the correct monitor, including multi-monitor setups.
            Rectangle area = Screen.FromPoint(Cursor.Position).WorkingArea;
            Location = PopupPosition(area, Size);
            Show();
            BringToFront();
            Activate();
        }

        protected override void OnDeactivate(EventArgs e)
        {
            LastDeactivatedUtc = DateTime.UtcNow;
            base.OnDeactivate(e);
            Hide();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) Hide();
            base.OnKeyDown(e);
        }

        private void Divider(Graphics g, int y)
        {
            using (Pen pen = new Pen(Color.FromArgb(58, 67, 70), 1f))
                g.DrawLine(pen, 22, y, 416 - 22, y);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (settings == null) return;
            Graphics g = e.Graphics;
            g.ScaleTransform(scale, scale);
            bool activeKnown = UsageState.ActiveKnown(settings, DateTime.Now);
            bool weeklyKnown = UsageState.WeeklyKnown(settings, DateTime.Now);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(Ui.Background);
            using (Pen border = new Pen(Ui.Border, 1))
                Ui.DrawRounded(g, border, new RectangleF(.5f, .5f, 415, 412), 18);

            using (Brush logo = new SolidBrush(Color.FromArgb(19, 55, 37)))
                Ui.FillRounded(g, logo, new RectangleF(20, 19, 44, 44), 11);
            using (Pen logoBorder = new Pen(Color.FromArgb(82, 131, 99), 1.5f))
                Ui.DrawRounded(g, logoBorder, new RectangleF(20.5f, 19.5f, 43, 43), 11);
            using (Font smallLogo = Ui.CanvasFont(11f, FontStyle.Bold))
                Ui.DrawText(g, "M", 23, 25, 38, 31, smallLogo, Ui.Text, StringAlignment.Center);

            using (Font title = Ui.CanvasFont(16, FontStyle.Bold))
                Ui.DrawText(g, "Metraik", 78, 15, 270, 34, title, Ui.Text, StringAlignment.Near);
            using (Font subtitle = Ui.CanvasFont(10, FontStyle.Regular))
                Ui.DrawText(g, settings.Provider + " usage", 79, 48, 240, 22, subtitle, Ui.Muted, StringAlignment.Near);
            Divider(g, 86);

            // Single primary allowance ring. No duplicate hourly / active usage block.
            RectangleF ring = new RectangleF(29, 104, 138, 138);
            using (Pen track = new Pen(Ui.Track, 12f))
            {
                track.StartCap = LineCap.Round;
                track.EndCap = LineCap.Round;
                g.DrawArc(track, ring, -90, 359.9f);
            }
            if (activeKnown && settings.ActiveRemaining > 0)
            {
                using (Pen progress = new Pen(Ui.RemainingColour(settings.ActiveRemaining), 12f))
                {
                    progress.StartCap = LineCap.Round;
                    progress.EndCap = LineCap.Round;
                    g.DrawArc(progress, ring, -90, 359.9f * settings.ActiveRemaining / 100f);
                }
            }
            using (Font percent = Ui.CanvasFont(26f, FontStyle.Bold))
                Ui.DrawText(g, activeKnown ? settings.ActiveRemaining + "%" : "?", 37, 143, 122, 55,
                    percent, Ui.Text, StringAlignment.Center);
            using (Font bigLabel = Ui.CanvasFont(16f, FontStyle.Bold))
                Ui.DrawText(g, activeKnown ? "Remaining" : "Update needed", 185, 128, 208, 32,
                    bigLabel, Ui.Text, StringAlignment.Near);
            using (Font secondary = Ui.CanvasFont(10.5f, FontStyle.Regular))
            {
                Ui.DrawText(g, "Active allowance", 186, 161, 208, 25,
                    secondary, Ui.Muted, StringAlignment.Near);
                Ui.DrawText(g, activeKnown ? Ui.ResetLine(settings.ActiveResetLocal) : "Enter a new reading",
                    186, 189, 213, 25, secondary, Ui.Muted, StringAlignment.Near);
            }
            using (Font badge = Ui.CanvasFont(8f, FontStyle.Bold))
                Ui.DrawText(g, settings.IsDemo ? "DEMO DATA" : "MANUAL DATA",
                    188, 220, 197, 20, badge,
                    settings.IsDemo ? Ui.Amber : Ui.Green, StringAlignment.Near);

            Divider(g, 257);
            using (Font weeklyLabel = Ui.CanvasFont(11.6f, FontStyle.Bold))
                Ui.DrawText(g, "Weekly allowance", 24, 269, 210, 27,
                    weeklyLabel, Ui.Text, StringAlignment.Near);
            using (Font weeklyText = Ui.CanvasFont(10.5f, FontStyle.Regular))
            {
                Ui.DrawText(g, weeklyKnown ? settings.WeeklyRemaining + "% remaining" : "Unknown", 245, 270, 150, 27,
                    weeklyText, Ui.Text, StringAlignment.Far);
                Ui.DrawText(g, weeklyKnown ? Ui.ResetLine(settings.WeeklyResetLocal) : "Update readings in Settings",
                    24, 330, 280, 24, weeklyText, Ui.Muted, StringAlignment.Near);
            }
            using (Brush bar = new SolidBrush(Ui.Track))
                Ui.FillRounded(g, bar, new RectangleF(24, 310, 372, 9), 4.5f);
            float barWidth = 372f * settings.WeeklyRemaining / 100f;
            if (weeklyKnown && barWidth > 0f)
            {
                using (Brush progress = new SolidBrush(Ui.RemainingColour(settings.WeeklyRemaining)))
                    Ui.FillRounded(g, progress, new RectangleF(24, 310, barWidth, 9), Math.Min(4.5f, barWidth / 2f));
            }

            Divider(g, 355);
            string stamp = settings.IsDemo ? "Demo values" : "Entered " + settings.LastEditedLocal.ToString("dd MMM HH:mm");
            using (Font footer = Ui.CanvasFont(9f, FontStyle.Regular))
                Ui.DrawText(g, stamp, 24, 367, 167, 31, footer, Ui.Muted, StringAlignment.Near);
        }
    }
}
