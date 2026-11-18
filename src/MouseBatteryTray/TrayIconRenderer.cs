using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace MouseBatteryTray;

internal static class TrayIconRenderer
{
    [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr icon);

    public static Icon Create(int? percent, bool muted)
    {
        const int size = 64;
        using var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        string text = percent?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "?";
        using var family = new FontFamily("Segoe UI");
        using var path = new GraphicsPath();
        path.AddString(text, family, (int)FontStyle.Bold, size, PointF.Empty, StringFormat.GenericTypographic);
        var bounds = path.GetBounds();
        // A single uniform scale keeps glyph proportions and baseline intact, including for 100.
        float scale = Math.Min(60 / bounds.Width, 56 / bounds.Height);
        using var transform = new Matrix(scale, 0, 0, scale,
            (size - bounds.Width * scale) / 2 - bounds.X * scale,
            (size - bounds.Height * scale) / 2 - bounds.Y * scale);
        path.Transform(transform);
        int rgb = TrayIconColor.ForReading(percent, muted);
        using var fill = new SolidBrush(Color.FromArgb((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255));
        // A narrow edge maintains contrast on light taskbars without a heavy outline.
        using var edge = new Pen(Color.FromArgb(210, 30, 30, 30), 2) { LineJoin = LineJoin.Round };
        graphics.DrawPath(edge, path);
        graphics.FillPath(fill, path);
        var handle = bitmap.GetHicon();
        try { using var temporary = Icon.FromHandle(handle); return (Icon)temporary.Clone(); }
        finally { DestroyIcon(handle); }
    }
}
