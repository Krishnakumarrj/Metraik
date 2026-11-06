using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Metraik.Tests
{
    internal static class Suite
    {
        private static int checks;
        private static readonly DateTime Now = new DateTime(2026, 11, 6, 12, 0, 0);
        private static string scratch;
        [DllImport("user32.dll")]
        private static extern int GetGuiResources(IntPtr process, int flags);

        [STAThread]
        private static int Main()
        {
            scratch = Path.Combine(Path.GetTempPath(), "Metraik.Tests." + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(scratch);
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                TestState();
                TestStorage();
                TestAlerts();
                TestDrawing();
                TestSettings();
                TestTrayLifecycle();
                Console.WriteLine("PASS: " + checks + " assertions. Windows build, storage, state, alerts, UI rendering and tray lifecycle.");
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
            finally { Directory.Delete(scratch, true); }
        }
        private static void Check(bool condition, string message)
        {
            checks++;
            if (!condition) throw new InvalidOperationException("FAIL: " + message);
        }
        private static AppSettings Manual()
        {
            return new AppSettings { Mode = "Manual", ActiveRemaining = 72, WeeklyRemaining = 48,
                LastEditedLocal = Now, ActiveResetLocal = Now.AddHours(2), WeeklyResetLocal = Now.AddDays(3) };
        }
        private static void TestState()
        {
            AppSettings s = Manual();
            Check(UsageState.ActiveKnown(s, Now), "manual reading before reset");
            Check(!UsageState.ActiveKnown(s, s.ActiveResetLocal), "exact reset becomes unknown");
            Check(UsageState.WeeklyKnown(s, s.ActiveResetLocal), "weekly stays known when active expires");
            Check(!UsageState.WeeklyKnown(s, s.WeeklyResetLocal), "weekly expires independently");
            s.LastEditedLocal = Now.AddHours(1);
            Check(!UsageState.ActiveKnown(s, Now), "clock rollback does not imply fresh data");
            s = Manual();
            s.ActiveRemaining = -20; s.WeeklyRemaining = 200; s.Mode = "unexpected"; s.Provider = "https://bad.example";
            s.Validate();
            Check(s.ActiveRemaining == 0 && s.WeeklyRemaining == 100 && s.IsDemo && s.Provider == "ChatGPT", "invalid values normalised");
            s.Mode = "Manual"; s.ActiveResetLocal = DateTime.MinValue; s.Validate();
            Check(!UsageState.ActiveKnown(s, Now), "missing reset not invented");
            AppSettings copy = s.Copy(); copy.ActiveRemaining = 99;
            Check(s.ActiveRemaining == 0, "copy does not edit original");
            Check(UsageState.Countdown(Now, Now) == "passed", "countdown expired");
            Check(UsageState.Countdown(Now.AddSeconds(1), Now) == "1m", "subminute countdown");
            Check(UsageState.Countdown(Now.AddHours(2).AddMinutes(15), Now) == "2h 15m", "hour countdown");
            Check(UsageState.Countdown(Now.AddDays(3).AddHours(4), Now) == "3d 4h", "day countdown");
            Check(Ui.RemainingColour(0) == Ui.Red && Ui.RemainingColour(20) == Ui.Red, "red boundary");
            Check(Ui.RemainingColour(21) == Ui.Amber && Ui.RemainingColour(49) == Ui.Amber, "amber boundaries");
            Check(Ui.RemainingColour(50) == Ui.Green && Ui.RemainingColour(100) == Ui.Green, "green boundaries");
            foreach (string provider in ProviderCatalog.Names)
                Check(new Uri(ProviderCatalog.Url(provider)).Scheme == "https", "HTTPS provider link");
            Check(ProviderCatalog.Url("unknown") == "https://chatgpt.com/", "unknown provider cannot open arbitrary URL");
            Point position = PopupForm.PopupPosition(new Rectangle(-1920, 0, 1920, 1040), new Size(416, 413));
            Check(position.X >= -1920 && position.X + 416 <= 0 && position.Y + 413 <= 1040, "negative monitor bounds");
            Check(PopupForm.PopupPosition(new Rectangle(0, 0, 100, 100), new Size(416, 413)) == Point.Empty, "small display anchored visibly");
        }
        private static void TestStorage()
        {
            string path = Path.Combine(scratch, "settings.xml");
            string absent = Path.Combine(scratch, "absent.xml");
            Check(SettingsStore.LoadFrom(path, absent).IsDemo, "first launch demo");
            AppSettings s = Manual();
            SettingsStore.SaveTo(path, s);
            AppSettings loaded = SettingsStore.LoadFrom(path, absent);
            Check(!loaded.IsDemo && loaded.ActiveRemaining == 72 && loaded.LastEditedLocal == Now, "manual XML round trip");
            s.ActiveRemaining = 17;
            SettingsStore.SaveTo(path, s);
            Check(File.Exists(path + ".bak"), "backup created");
            Check(SettingsStore.LoadFrom(path, absent).ActiveRemaining == 17, "atomic replacement contains new value");
            File.WriteAllText(path, "<broken");
            Check(SettingsStore.LoadFrom(path, absent).ActiveRemaining == 72 && SettingsStore.LoadWarning != null, "corrupt primary uses backup with warning");
            File.Delete(path + ".bak");
            Check(SettingsStore.LoadFrom(path, absent).IsDemo && SettingsStore.LoadWarning != null, "corrupt settings fallback visibly demo");
            Check(File.ReadAllText(path) == "<broken", "corrupt original retained");
            string legacy = Path.Combine(scratch, "legacy.xml");
            // Exact old XML schema, before the Provider and alert fields existed.
            File.WriteAllText(legacy, "<AppSettings><Mode>Manual</Mode><ActiveRemaining>33</ActiveRemaining><WeeklyRemaining>44</WeeklyRemaining><ActiveResetLocal>2026-11-07T12:00:00</ActiveResetLocal><WeeklyResetLocal>2026-11-09T12:00:00</WeeklyResetLocal><LastEditedLocal>2026-11-06T12:00:00</LastEditedLocal></AppSettings>");
            loaded = SettingsStore.LoadFrom(absent, legacy);
            Check(loaded.ActiveRemaining == 33 && loaded.Provider == "ChatGPT" && loaded.AlertThreshold == 20, "legacy schema migration");
            File.WriteAllText(path, "<!DOCTYPE x [<!ENTITY ext SYSTEM 'file:///not-allowed'>]><AppSettings>&ext;</AppSettings>");
            Check(SettingsStore.LoadFrom(path, absent).IsDemo && SettingsStore.LoadWarning != null, "DTD rejected");
            // A locked target makes replacement fail; its contents must survive.
            SettingsStore.SaveTo(legacy, s);
            byte[] before = File.ReadAllBytes(legacy);
            bool failed = false;
            using (FileStream locked = new FileStream(legacy, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                try { SettingsStore.SaveTo(legacy, Manual()); } catch (IOException) { failed = true; }
            }
            Check(failed && before.SequenceEqual(File.ReadAllBytes(legacy)), "failed save preserves previous data");
            Check(Directory.GetFiles(scratch, "*.tmp").Length == 0, "failed save cleans temporary file");
        }
        private static void TestAlerts()
        {
            AppSettings s = Manual(); s.AlertsEnabled = true; s.ActiveRemaining = 20;
            AlertTracker tracker = new AlertTracker();
            Check(tracker.Evaluate(s, Now) != null, "threshold alert");
            Check(tracker.Evaluate(s, Now) == null, "no repeated alert");
            s.WeeklyRemaining = 8;
            Check(tracker.Evaluate(s, Now).Contains("Weekly"), "weekly alert independently");
            s.ActiveResetLocal = Now.AddDays(1);
            Check(tracker.Evaluate(s, Now) != null, "new period rearms alert");
            s.Mode = "Demo";
            Check(new AlertTracker().Evaluate(s, Now) == null, "demo does not notify");
            s.Mode = "Manual"; s.AlertsEnabled = false;
            Check(new AlertTracker().Evaluate(s, Now) == null, "disabled alerts");
            s.AlertsEnabled = true; s.ActiveResetLocal = Now; s.WeeklyResetLocal = Now;
            Check(new AlertTracker().Evaluate(s, Now) == null, "expired data does not notify");
        }
        private static void TestDrawing()
        {
            Directory.CreateDirectory("artifacts/screenshots");
            int[] values = { 0, 1, 8, 20, 21, 49, 50, 72, 99, 100 };
            foreach (int value in values)
            {
                using (Icon icon = TrayIconFactory.Make(value, value))
                using (Bitmap image = icon.ToBitmap())
                {
                    Check(image.Width == 64 && image.Height == 64, "tray image dimensions " + value);
                    image.Save("artifacts/screenshots/tray-" + value + ".png", ImageFormat.Png);
                }
            }
            using (Icon icon = TrayIconFactory.Make(72, 48, false, false))
            using (Bitmap image = icon.ToBitmap()) image.Save("artifacts/screenshots/tray-unknown.png", ImageFormat.Png);
            // Handle ownership must remain stable after repeated dynamic icon replacement.
            int baseline = GetGuiResources(Process.GetCurrentProcess().Handle, 0);
            for (int i = 0; i < 500; i++) using (Icon icon = TrayIconFactory.Make(i % 101, i % 101)) { }
            Check(GetGuiResources(Process.GetCurrentProcess().Handle, 0) <= baseline + 8, "no GDI handle leak after 500 icons");
            foreach (float scale in new float[] { 1f, 1.5f, 2f })
            using (PopupForm popup = new PopupForm(scale))
            {
                popup.SetData(new AppSettings());
                popup.Show(); Application.DoEvents();
                Check(popup.Controls.OfType<Button>().Count() == 3, "keyboard-accessible popup actions");
                foreach (Control control in popup.Controls) Check(popup.ClientRectangle.Contains(control.Bounds), "scaled button stays inside popup");
                using (Bitmap image = new Bitmap(popup.Width, popup.Height))
                {
                    popup.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
                    image.Save("artifacts/screenshots/popup-" + scale.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ".png", ImageFormat.Png);
                }
                int refresh = 0;
                popup.RefreshRequested = delegate { refresh++; };
                popup.Controls.OfType<Button>().First(b => b.AccessibleName == "Refresh display only").PerformClick();
                Check(refresh == 1, "popup refresh button invokes callback");
                AppSettings expired = Manual(); expired.ActiveResetLocal = DateTime.Now.AddMinutes(-1); expired.WeeklyResetLocal = DateTime.Now.AddMinutes(-1);
                popup.SetData(expired);
                Check(popup.AccessibleDescription.Contains("unknown"), "unknown reading accessible description");
                using (Bitmap image = new Bitmap(popup.Width, popup.Height))
                {
                    popup.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
                    image.Save("artifacts/screenshots/popup-expired-" + scale.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ".png", ImageFormat.Png);
                }
                popup.Hide();
            }
        }
        private static T Find<T>(Control root, string name) where T : Control
        {
            return (T)root.Controls.Find(name, true)[0];
        }
        private static void TestSettings()
        {
            using (SettingsForm form = new SettingsForm(new AppSettings(), false))
            {
                form.Show(); Application.DoEvents();
                Check(!Find<NumericUpDown>(form, "active").Enabled, "demo fields disabled");
                Find<ComboBox>(form, "source").SelectedIndex = 1;
                Check(Find<NumericUpDown>(form, "active").Enabled, "manual fields enabled");
                Find<NumericUpDown>(form, "active").Value = 31;
                Find<ComboBox>(form, "provider").SelectedItem = "Claude";
                Check(Find<ComboBox>(form, "source").SelectedIndex == 0 && Find<NumericUpDown>(form, "active").Value == 72, "service switch does not re-label old readings");
                Find<ComboBox>(form, "source").SelectedIndex = 1;
                Find<NumericUpDown>(form, "active").Value = 35;
                using (Bitmap image = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
                    image.Save("artifacts/screenshots/settings.png", ImageFormat.Png);
                }
                Find<Button>(form, "save").PerformClick();
                Check(form.DialogResult == DialogResult.OK && form.ResultSettings.Provider == "Claude" && form.ResultSettings.ActiveRemaining == 35, "settings save captures manual service");
            }
        }
        private static void TestTrayLifecycle()
        {
            // Isolate the real application from user settings by injecting the already-tested data loader.
            using (TrayApplication context = new TrayApplication(new AppSettings()))
            {
                Application.DoEvents();
                typeof(TrayApplication).GetMethod("RefreshDisplay", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(context, null);
                NotifyIcon tray = (NotifyIcon)typeof(TrayApplication).GetField("tray", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(context);
                Check(tray.Visible && tray.Icon != null && tray.Text.Length <= 63, "real notification icon created");
                Check(tray.ContextMenuStrip.Items.Count > 5, "tray menu populated");
            }
            Check(true, "tray and timers dispose without exception");
        }
    }
}
