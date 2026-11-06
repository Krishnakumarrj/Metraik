using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Metraik
{
    public class AppSettings
    {
        public string Mode { get; set; }
        public string Provider { get; set; }
        public int ActiveRemaining { get; set; }
        public int WeeklyRemaining { get; set; }
        public DateTime ActiveResetLocal { get; set; }
        public DateTime WeeklyResetLocal { get; set; }
        public DateTime LastEditedLocal { get; set; }
        public bool AlertsEnabled { get; set; }
        public int AlertThreshold { get; set; }
        public AppSettings()
        {
            Mode = "Demo";
            Provider = "ChatGPT";
            ActiveRemaining = 72;
            WeeklyRemaining = 48;
            ActiveResetLocal = DateTime.Now.AddMinutes(135);
            WeeklyResetLocal = DateTime.Now.AddDays(3);
            LastEditedLocal = DateTime.Now;
            AlertThreshold = 20;
        }
        public bool IsDemo { get { return Mode != "Manual"; } }
        public AppSettings Copy() { return (AppSettings)MemberwiseClone(); }
        public void Validate()
        {
            if (Mode != "Manual") Mode = "Demo";
            Provider = ProviderCatalog.Normalise(Provider);
            ActiveRemaining = Math.Max(0, Math.Min(100, ActiveRemaining));
            WeeklyRemaining = Math.Max(0, Math.Min(100, WeeklyRemaining));
            AlertThreshold = Math.Max(1, Math.Min(50, AlertThreshold));
            // Missing dates become unknown, never an invented future reset.
            if (ActiveResetLocal.Year < 2000) ActiveResetLocal = new DateTime(2000, 1, 1);
            if (WeeklyResetLocal.Year < 2000) WeeklyResetLocal = new DateTime(2000, 1, 1);
            if (LastEditedLocal.Year < 2000) LastEditedLocal = new DateTime(2000, 1, 1);
        }
        public void ResetDemoReadings()
        {
            if (!IsDemo) return;
            ActiveRemaining = 72;
            WeeklyRemaining = 48;
            ActiveResetLocal = DateTime.Now.AddMinutes(135);
            WeeklyResetLocal = DateTime.Now.AddDays(3);
            LastEditedLocal = DateTime.Now;
        }
    }
    internal static class SettingsStore
    {
        public static string LoadWarning { get; private set; }
        public static string DirectoryPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Metraik"); }
        }
        public static string FilePath { get { return Path.Combine(DirectoryPath, "settings.xml"); } }
        public static AppSettings Load()
        {
            string legacy = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ChatGPTTaskbarMonitor", "settings.xml");
            return LoadFrom(FilePath, legacy);
        }
        internal static AppSettings LoadFrom(string path, string legacyPath)
        {
            LoadWarning = null;
            string selected = File.Exists(path) ? path : (File.Exists(legacyPath) ? legacyPath : null);
            if (selected == null) return new AppSettings();
            try { return Read(selected); }
            catch (Exception ex)
            {
                if (!ExpectedReadError(ex)) throw;
                try
                {
                    AppSettings backup = Read(selected + ".bak");
                    LoadWarning = "Settings were unreadable. Previous saved readings were recovered; please check them.";
                    return backup;
                }
                catch (Exception backupError)
                {
                    if (!ExpectedReadError(backupError)) throw;
                    LoadWarning = "Settings could not be read. Showing DEMO data. Your original file has been kept.";
                    return new AppSettings();
                }
            }
        }
        private static bool ExpectedReadError(Exception ex)
        {
            return ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException || ex is XmlException;
        }
        private static AppSettings Read(string path)
        {
            XmlReaderSettings options = new XmlReaderSettings();
            options.DtdProcessing = DtdProcessing.Prohibit;
            options.XmlResolver = null;
            options.MaxCharactersInDocument = 65536;
            using (XmlReader reader = XmlReader.Create(path, options))
            {
                AppSettings settings = (AppSettings)new XmlSerializer(typeof(AppSettings)).Deserialize(reader);
                settings.Validate();
                settings.ResetDemoReadings();
                return settings;
            }
        }
        public static void Save(AppSettings settings) { SaveTo(FilePath, settings); }
        internal static void SaveTo(string path, AppSettings settings)
        {
            AppSettings copy = settings.Copy();
            copy.Validate();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (FileStream stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    new XmlSerializer(typeof(AppSettings)).Serialize(stream, copy);
                    stream.Flush(true);
                }
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
