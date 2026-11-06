using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Metraik
{
    internal static class Ui
    {
        public static readonly Color Background = Color.FromArgb(22, 25, 29);
        public static readonly Color Panel = Color.FromArgb(33, 38, 43);
        public static readonly Color PanelHover = Color.FromArgb(45, 51, 57);
        public static readonly Color Border = Color.FromArgb(75, 82, 88);
        public static readonly Color Text = Color.FromArgb(241, 245, 245);
        public static readonly Color Muted = Color.FromArgb(173, 183, 184);
        public static readonly Color Dim = Color.FromArgb(123, 134, 137);
        public static readonly Color Green = Color.FromArgb(75, 232, 111);
        public static readonly Color Amber = Color.FromArgb(250, 192, 77);
        public static readonly Color Red = Color.FromArgb(255, 105, 112);
        public static readonly Color Track = Color.FromArgb(62, 69, 73);

        public static Font CanvasFont(float points, FontStyle style)
        {
            return new Font("Segoe UI", points * 96f / 72f, style, GraphicsUnit.Pixel);
        }

        public static Color RemainingColour(int percent)
        {
            if (percent <= 20) return Red;
            if (percent < 50) return Amber;
            return Green;
        }

        public static GraphicsPath RoundedRectangle(RectangleF rectangle, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = radius * 2f;
            if (diameter > rectangle.Width) diameter = rectangle.Width;
            if (diameter > rectangle.Height) diameter = rectangle.Height;
            if (diameter < 1)
            {
                path.AddRectangle(rectangle);
                return path;
            }
            path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void FillRounded(Graphics g, Brush brush, RectangleF rectangle, float radius)
        {
            using (GraphicsPath path = RoundedRectangle(rectangle, radius)) g.FillPath(brush, path);
        }

        public static void DrawRounded(Graphics g, Pen pen, RectangleF rectangle, float radius)
        {
            using (GraphicsPath path = RoundedRectangle(rectangle, radius)) g.DrawPath(pen, path);
        }

        public static void DrawText(Graphics g, string text, float x, float y, float width,
            float height, Font font, Color colour, StringAlignment alignment)
        {
            using (Brush brush = new SolidBrush(colour))
            using (StringFormat format = new StringFormat())
            {
                format.Alignment = alignment;
                format.LineAlignment = StringAlignment.Center;
                format.Trimming = StringTrimming.EllipsisCharacter;
                format.FormatFlags = StringFormatFlags.NoWrap;
                g.DrawString(text, font, brush, new RectangleF(x, y, width, height), format);
            }
        }

        public static string Countdown(DateTime local)
        {
            return UsageState.Countdown(local, DateTime.Now);
        }

        public static string ResetLine(DateTime local)
        {
            return local <= DateTime.Now ? "Reset time passed" : "Resets in " + Countdown(local);
        }
    }
}
