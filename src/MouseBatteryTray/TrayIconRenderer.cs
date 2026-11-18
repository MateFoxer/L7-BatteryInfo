using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MouseBatteryTray;

internal static class TrayIconRenderer
{
    [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr icon);

    public static Icon Create(int? percent, bool muted)
    {
        int size = Math.Max(16, SystemInformation.SmallIconSize.Width);
        string text = percent?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "?";
        // Draw a monochrome GDI text mask at the actual icon resolution. Measuring the
        // painted pixels removes font layout padding without stretching the glyphs.
        using var mask = new Bitmap(size * 4, size * 4, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(mask);
        Rectangle bounds = Rectangle.Empty;
        for (int fontSize = size; fontSize >= 1; fontSize--)
        {
            graphics.Clear(Color.Black);
            using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
            TextRenderer.DrawText(graphics, text, font, new Point(size, size), Color.White,
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
            bounds = PaintedBounds(mask);
            if (!bounds.IsEmpty && bounds.Width <= size - 2 && bounds.Height <= size - 2) break;
        }

        using var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        int rgb = TrayIconColor.ForReading(percent, muted);
        int left = (size - bounds.Width) / 2;
        int top = (size - bounds.Height) / 2;
        for (int y = 0; y < bounds.Height; y++)
            for (int x = 0; x < bounds.Width; x++)
            {
                int alpha = mask.GetPixel(bounds.X + x, bounds.Y + y).R;
                if (alpha != 0) bitmap.SetPixel(left + x, top + y,
                    Color.FromArgb(alpha, (rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255));
            }
        var handle = bitmap.GetHicon();
        try { using var temporary = Icon.FromHandle(handle); return (Icon)temporary.Clone(); }
        finally { DestroyIcon(handle); }
    }

    private static Rectangle PaintedBounds(Bitmap bitmap)
    {
        int left = bitmap.Width, top = bitmap.Height, right = -1, bottom = -1;
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
                if (bitmap.GetPixel(x, y).R != 0)
                {
                    left = Math.Min(left, x); top = Math.Min(top, y);
                    right = Math.Max(right, x); bottom = Math.Max(bottom, y);
                }
        return right < left ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }
}
