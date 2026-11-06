using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Metraik
{
    internal static class TrayIconFactory
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public static Icon Make(int active, int weekly)
        {
            return Make(active, weekly, true, true);
        }

        public static Icon Make(int active, int weekly, bool activeKnown, bool weeklyKnown)
        {
            // Draw at 64x64 so Windows can downscale cleanly for taskbar display.
            using (Bitmap image = new Bitmap(64, 64, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(image))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.Transparent);

                RectangleF tile = new RectangleF(2, 2, 60, 60);
                using (SolidBrush background = new SolidBrush(Color.FromArgb(19, 34, 30)))
                    Ui.FillRounded(g, background, tile, 13);
                using (Pen border = new Pen(Color.FromArgb(84, 127, 105), 2))
                    Ui.DrawRounded(g, border, new RectangleF(3, 3, 58, 58), 12);

                string number = activeKnown ? Math.Max(0, Math.Min(100, active)).ToString() : "?";
                float fontSize = active >= 100 ? 24f : 29f;
                using (Font font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel))
                    Ui.DrawText(g, number, 4, 4, 56, 42, font, activeKnown ? Ui.RemainingColour(active) : Ui.Muted, StringAlignment.Center);

                // The thin lower track is WEEKLY allowance; the number is ACTIVE allowance.
                using (SolidBrush baseBar = new SolidBrush(Color.FromArgb(69, 77, 76)))
                    Ui.FillRounded(g, baseBar, new RectangleF(11, 49, 42, 6), 3);
                float fill = 42f * Math.Max(0, Math.Min(100, weekly)) / 100f;
                if (weeklyKnown && fill > 0.1f)
                {
                    using (SolidBrush colour = new SolidBrush(Ui.RemainingColour(weekly)))
                        Ui.FillRounded(g, colour, new RectangleF(11, 49, fill, 6), Math.Min(3, fill / 2f));
                }

                IntPtr handle = image.GetHicon();
                try
                {
                    using (Icon temporary = Icon.FromHandle(handle)) return (Icon)temporary.Clone();
                }
                finally
                {
                    DestroyIcon(handle);
                }
            }
        }
    }
}
