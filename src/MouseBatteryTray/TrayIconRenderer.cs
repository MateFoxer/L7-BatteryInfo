using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace MouseBatteryTray;

internal static class TrayIconRenderer
{
    [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr icon);

    public static Icon Create(int? percent, bool muted)
    {
        // Render at a larger size for Windows to scale at different taskbar DPIs.
        using var bitmap = new Bitmap(64, 64);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        var color = muted ? Color.DimGray : percent <= 20 ? Color.DarkRed : Color.FromArgb(24, 100, 65);
        using var brush = new SolidBrush(color);
        graphics.FillEllipse(brush, 1, 1, 62, 62);
        string text = percent?.ToString() ?? "?";
        using var font = new Font("Segoe UI", text.Length == 3 ? 21 : 27, FontStyle.Bold, GraphicsUnit.Pixel);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        graphics.DrawString(text, font, Brushes.White, new RectangleF(0, 0, 64, 64), format);
        var handle = bitmap.GetHicon();
        try { using var temporary = Icon.FromHandle(handle); return (Icon)temporary.Clone(); }
        finally { DestroyIcon(handle); }
    }
}
