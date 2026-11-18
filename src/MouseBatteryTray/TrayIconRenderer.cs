using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace MouseBatteryTray;

internal static class TrayIconRenderer
{
    [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr icon);

    public static Icon Create(int? percent, bool muted)
    {
        // Render at Windows' current small-icon size instead of downscaling a vector path.
        int size = Math.Max(16, SystemInformation.SmallIconSize.Width);
        using var bitmap = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        string text = percent?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "?";
        float emSize = size * (text.Length == 3 ? 0.58f : text.Length == 2 ? 0.76f : 0.88f);
        using var font = new Font("Segoe UI", emSize, FontStyle.Bold, GraphicsUnit.Pixel);
        using var format = new StringFormat(StringFormat.GenericTypographic)
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoClip
        };
        int rgb = TrayIconColor.ForReading(percent, muted);
        using var fill = new SolidBrush(Color.FromArgb((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255));
        graphics.DrawString(text, font, fill, new RectangleF(0, 0, size, size), format);
        var handle = bitmap.GetHicon();
        try { using var temporary = Icon.FromHandle(handle); return (Icon)temporary.Clone(); }
        finally { DestroyIcon(handle); }
    }
}
