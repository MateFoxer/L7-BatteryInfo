using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace MouseBatteryTray;

internal static class TrayIconRenderer
{
    [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr icon);

    public static Icon Create(int? percent, bool muted)
    {
        // Fit bold glyph outlines to nearly the full canvas, then scale for the tray.
        using var bitmap = new Bitmap(64, 64);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        string text = percent?.ToString() ?? "?";
        using var family = new FontFamily("Segoe UI");
        using var path = new GraphicsPath();
        path.AddString(text, family, (int)FontStyle.Bold, 64, PointF.Empty, StringFormat.GenericTypographic);
        var bounds = path.GetBounds();
        float scale = Math.Min(58 / bounds.Width, 55 / bounds.Height);
        using var transform = new Matrix(scale, 0, 0, scale,
            (64 - bounds.Width * scale) / 2 - bounds.X * scale,
            (64 - bounds.Height * scale) / 2 - bounds.Y * scale);
        path.Transform(transform);
        using var outline = new Pen(Color.Black, 4) { LineJoin = LineJoin.Round };
        using var fill = new SolidBrush(muted ? Color.Silver : percent <= 20 ? Color.OrangeRed : Color.White);
        graphics.DrawPath(outline, path);
        graphics.FillPath(fill, path);
        var handle = bitmap.GetHicon();
        try { using var temporary = Icon.FromHandle(handle); return (Icon)temporary.Clone(); }
        finally { DestroyIcon(handle); }
    }
}
