using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Metraik
{
    internal sealed class TrayApplication : ApplicationContext
    {
        private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RegistryName = "Metraik";
        private AppSettings settings;
        private NotifyIcon tray;
        private Icon trayImage;
        private PopupForm popup;
        private Timer clock;
        private Timer clickTimer;
        private ContextMenuStrip menu;
        private readonly AlertTracker alerts = new AlertTracker();
        private ToolStripMenuItem startAtLoginItem;
        private bool isExiting;

        public TrayApplication() : this(null) { }

        internal TrayApplication(AppSettings suppliedSettings)
        {
            settings = suppliedSettings ?? SettingsStore.Load();
            popup = new PopupForm();
            popup.SetData(settings);
            popup.RefreshRequested = RefreshDisplay;
            popup.OpenServiceRequested = OpenService;
            popup.SettingsRequested = OpenSettings;

            menu = new ContextMenuStrip();
            menu.RenderMode = ToolStripRenderMode.System;
            menu.Items.Add("Open usage panel", null, delegate { TogglePopup(); });
            menu.Items.Add("Edit usage and settings...", null, delegate { OpenSettings(); });
            menu.Items.Add("Refresh display", null, delegate { RefreshDisplay(); });
            menu.Items.Add("Open selected service", null, delegate { OpenService(); });
            menu.Items.Add(new ToolStripSeparator());
            startAtLoginItem = new ToolStripMenuItem("Start with Windows");
            startAtLoginItem.Checked = IsStartingAtLogin();
            startAtLoginItem.Click += delegate
            {
                bool desired = !IsStartingAtLogin();
                try
                {
                    SetStartAtLogin(desired);
                    startAtLoginItem.Checked = IsStartingAtLogin();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not change Windows startup: " + ex.Message,
                        "Metraik", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            menu.Items.Add(startAtLoginItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, delegate { Quit(); });

            tray = new NotifyIcon();
            tray.ContextMenuStrip = menu;
            clickTimer = new Timer();
            clickTimer.Interval = SystemInformation.DoubleClickTime;
            clickTimer.Tick += delegate { clickTimer.Stop(); TogglePopup(); };
            tray.MouseClick += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button != MouseButtons.Left) return;
                if (popup.Visible || (DateTime.UtcNow - popup.LastDeactivatedUtc).TotalMilliseconds < 300)
                {
                    clickTimer.Stop();
                    popup.Hide();
                    return;
                }
                clickTimer.Stop();
                clickTimer.Start();
            };
            tray.MouseDoubleClick += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    clickTimer.Stop();
                    popup.Hide();
                    OpenService();
                }
            };
            RefreshDisplay();
            tray.Visible = true;
            if (suppliedSettings == null && SettingsStore.LoadWarning != null)
                tray.ShowBalloonTip(10000, "Metraik settings", SettingsStore.LoadWarning, ToolTipIcon.Warning);
            CheckAlerts();

            clock = new Timer();
            clock.Interval = 10000;
            clock.Tick += delegate
            {
                if (!isExiting)
                {
                    RefreshDisplay();
                    CheckAlerts();
                }
            };
            clock.Start();
        }

        private void UpdateTooltip()
        {
            if (tray == null) return;
            DateTime now = DateTime.Now;
            string text = String.Format("{0} {1}: Active {2} / Week {3}",
                settings.IsDemo ? "DEMO" : "MANUAL", settings.Provider,
                UsageState.ActiveKnown(settings, now) ? settings.ActiveRemaining + "% " + Ui.Countdown(settings.ActiveResetLocal) : "? update",
                UsageState.WeeklyKnown(settings, now) ? settings.WeeklyRemaining + "%" : "? update");
            // Older NotifyIcon implementations limit the tool-tip to 63 characters.
            tray.Text = text.Length > 63 ? text.Substring(0, 63) : text;
        }

        private void RefreshDisplay()
        {
            if (isExiting) return;
            if (tray != null)
            {
                Icon next = TrayIconFactory.Make(settings.ActiveRemaining, settings.WeeklyRemaining,
                    UsageState.ActiveKnown(settings, DateTime.Now), UsageState.WeeklyKnown(settings, DateTime.Now));
                tray.Icon = next;
                Icon old = trayImage;
                trayImage = next;
                if (old != null) old.Dispose();
                UpdateTooltip();
            }
            if (popup != null) popup.SetData(settings);
        }

        private void TogglePopup()
        {
            if (popup.Visible)
            {
                popup.Hide();
            }
            else
            {
                RefreshDisplay();
                popup.ShowNearTray();
            }
        }

        private void OpenSettings()
        {
            if (popup != null) popup.Hide();
            using (SettingsForm dialog = new SettingsForm(settings, IsStartingAtLogin()))
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                AppSettings updated = dialog.ResultSettings;
                bool wantStartup = dialog.StartWithWindows;
                try
                {
                    // User-edited percentages are never described as live account data.
                    updated.LastEditedLocal = DateTime.Now;
                    updated.Validate();
                    if (updated.IsDemo) updated.ResetDemoReadings();
                    SettingsStore.Save(updated);
                    settings = updated;
                    if (wantStartup != IsStartingAtLogin()) SetStartAtLogin(wantStartup);
                    startAtLoginItem.Checked = IsStartingAtLogin();
                    RefreshDisplay();
                    CheckAlerts();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not save every setting: " + ex.Message,
                        "Metraik", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    RefreshDisplay();
                }
            }
        }

        private void CheckAlerts()
        {
            string message = alerts.Evaluate(settings, DateTime.Now);
            if (message != null) tray.ShowBalloonTip(8000, "Metraik allowance alert", message, ToolTipIcon.Warning);
        }

        private void OpenService()
        {
            try
            {
                Process.Start(new ProcessStartInfo(ProviderCatalog.Url(settings.Provider))
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open the selected service in your browser: " + ex.Message,
                    "Metraik", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private bool IsStartingAtLogin()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath, false))
                {
                    if (key == null) return false;
                    string registered = key.GetValue(RegistryName) as string;
                    if (String.IsNullOrEmpty(registered)) return false;
                    return String.Equals(registered, "\"" + Application.ExecutablePath + "\"",
                        StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void SetStartAtLogin(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath))
            {
                if (key == null) throw new InvalidOperationException("Startup settings are unavailable.");
                if (enabled) key.SetValue(RegistryName, "\"" + Application.ExecutablePath + "\"");
                else key.DeleteValue(RegistryName, false);
            }
        }

        private void Quit()
        {
            if (isExiting) return;
            isExiting = true;
            if (clock != null) { clock.Stop(); clock.Dispose(); clock = null; }
            if (clickTimer != null) { clickTimer.Stop(); clickTimer.Dispose(); clickTimer = null; }
            if (tray != null)
            {
                tray.Visible = false;
                tray.Dispose();
                tray = null;
            }
            if (menu != null) { menu.Dispose(); menu = null; }
            if (trayImage != null) { trayImage.Dispose(); trayImage = null; }
            if (popup != null) { popup.Hide(); popup.Dispose(); popup = null; }
            ExitThread();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Quit();
            base.Dispose(disposing);
        }
    }
}
