using System;
using System.Threading;
using System.Windows.Forms;

namespace Metraik
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool createdNew;
            // Retain the previous mutex name to avoid running old and new versions together.
            using (Mutex mutex = new Mutex(true, @"Local\ChatGPTTaskbarMonitor_9B26CC17", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("Metraik (or the earlier monitor) is already running in the system tray.",
                        "Metraik", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                using (TrayApplication context = new TrayApplication()) Application.Run(context);
            }
        }
    }
}
