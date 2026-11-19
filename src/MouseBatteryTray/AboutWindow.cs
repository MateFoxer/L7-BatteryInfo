using System.Drawing.Drawing2D;
using System.Reflection;

namespace MouseBatteryTray;

internal sealed class AboutWindow : Form
{
    private readonly Font titleFont = new("Segoe UI", 12, FontStyle.Bold);
    private readonly Font bodyFont = new("Segoe UI", 9);
    private readonly Font valueFont = new("Segoe UI", 9, FontStyle.Bold);
    private readonly Button repositoryLink = new() { Text = "GitHub project", FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft, AccessibleName = "Open GitHub project" };
    private readonly RoundedCloseButton close = new() { Text = "Close", AccessibleName = "Close" };
    private readonly string version;
    private bool light;

    internal AboutWindow(bool isLight)
    {
        Text = "About L7-BatteryInfo";
        AccessibleName = Text;
        FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.None; KeyPreview = true; DoubleBuffered = true;
        version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
        close.Click += (_, _) => Close(); close.FlatAppearance.BorderSize = 0;
        repositoryLink.FlatAppearance.BorderSize = 0;
        repositoryLink.Click += (_, _) => OpenRepository();
        repositoryLink.Paint += (_, e) => DrawGitHubIcon(e.Graphics);
        Controls.Add(repositoryLink); Controls.Add(close);
        SetTheme(isLight); ApplyLayout();
    }

    internal void SetTheme(bool value)
    {
        light = value;
        BackColor = light ? Color.FromArgb(246, 247, 249) : Color.FromArgb(16, 17, 21);
        ForeColor = light ? Color.FromArgb(21, 23, 27) : Color.FromArgb(245, 245, 245);
        repositoryLink.BackColor = BackColor; repositoryLink.ForeColor = ForeColor;
        repositoryLink.FlatAppearance.MouseOverBackColor = light ? Color.FromArgb(230, 233, 238) : Color.FromArgb(40, 42, 48);
        close.BackColor = light ? Color.FromArgb(33, 35, 41) : Color.FromArgb(240, 242, 245);
        close.ForeColor = light ? Color.White : Color.FromArgb(21, 23, 27);
        close.SurfaceColor = BackColor;
        Invalidate();
    }

    private void ApplyLayout()
    {
        int S(int n) => (int)Math.Round(n * DeviceDpi / 96.0);
        ClientSize = new Size(S(400), S(250));
        repositoryLink.Font = bodyFont;
        repositoryLink.Padding = new Padding(S(22), 0, 0, 0);
        repositoryLink.SetBounds(S(14), S(210), S(155), S(28));
        close.SetBounds(S(310), S(210), S(72), S(28));

        using var path = BatteryMenu.Rounded(new Rectangle(0, 0, Width, Height), S(18));
        var old = Region; Region = new Region(path); old?.Dispose();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e) { base.OnDpiChanged(e); ApplyLayout(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        int S(int n) => (int)Math.Round(n * DeviceDpi / 96.0);
        var secondary = light ? Color.FromArgb(100, 108, 119) : Color.FromArgb(155, 163, 175);
        var divider = light ? Color.FromArgb(211, 215, 222) : Color.FromArgb(55, 58, 65);
        void TextAt(string text, Font font, Rectangle rect, Color color, bool right = false) =>
            TextRenderer.DrawText(g, text, font, rect, color, TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | (right ? TextFormatFlags.Right : TextFormatFlags.Left));
        TextAt("L7-BatteryInfo", titleFont, new Rectangle(S(18), S(10), S(240), S(28)), ForeColor);
        TextAt($"v{version}", bodyFont, new Rectangle(S(285), S(10), S(97), S(28)), secondary, true);
        TextAt("Mouse battery monitor for Windows", bodyFont, new Rectangle(S(18), S(36), S(364), S(22)), secondary);
        DrawInfoIcon(g, S(19), S(75), S(15), false, false);
        TextAt("Mouse", bodyFont, new Rectangle(S(41), S(72), S(102), S(23)), secondary);
        TextAt("MCHOSE L7 Ultra+", valueFont, new Rectangle(S(145), S(72), S(237), S(23)), ForeColor, true);
        DrawInfoIcon(g, S(19), S(104), S(15), true, false);
        TextAt("Receiver", bodyFont, new Rectangle(S(41), S(101), S(102), S(23)), secondary);
        TextAt("2.4 GHz USB", valueFont, new Rectangle(S(145), S(101), S(237), S(23)), ForeColor, true);
        using (var card = BatteryMenu.Rounded(new Rectangle(S(14), S(132), S(372), S(57)), S(8)))
        using (var fill = new SolidBrush(light ? Color.FromArgb(235, 237, 241) : Color.FromArgb(24, 26, 32)))
        using (var outline = new Pen(divider)) { g.FillPath(fill, card); g.DrawPath(outline, card); }
        DrawInfoIcon(g, S(23), S(140), S(15), false, true);
        TextAt("Developed by", bodyFont, new Rectangle(S(45), S(136), S(110), S(23)), secondary);
        TextAt("MateFoxer", valueFont, new Rectangle(S(155), S(136), S(222), S(23)), ForeColor, true);
        TextAt("Independent project. Not affiliated with MCHOSE.", bodyFont, new Rectangle(S(23), S(163), S(354), S(20)), secondary);
        using var line = new Pen(divider); foreach (int y in new[] { 64, 126, 198 }) g.DrawLine(line, S(18), S(y), Width - S(18), S(y));
        // A window Region clips to whole pixels; inset the antialiased stroke inside it.
        int inset = Math.Max(1, S(1));
        using var shape = BatteryMenu.Rounded(new Rectangle(inset, inset, Width - 2 * inset - 1, Height - 2 * inset - 1), Math.Max(1, S(18) - inset));
        using var gradient = new LinearGradientBrush(ClientRectangle, light ? Color.FromArgb(224, 227, 233) : Color.FromArgb(165, 168, 176), light ? Color.FromArgb(184, 189, 198) : Color.FromArgb(48, 51, 59), 90);
        using var border = new Pen(gradient, Math.Max(1, S(1))); g.DrawPath(border, shape);
    }

    private void DrawInfoIcon(Graphics graphics, int x, int y, int size, bool receiver, bool person)
    {
        var state = graphics.Save(); graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TranslateTransform(x, y); graphics.ScaleTransform(size / 16f, size / 16f);
        using var pen = new Pen(ForeColor, 1.3f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        if (person) { graphics.DrawEllipse(pen, 5, 1, 6, 6); graphics.DrawArc(pen, 2, 8, 12, 7, 180, 180); }
        else if (receiver) { graphics.DrawRectangle(pen, 4, 2, 8, 9); graphics.DrawRectangle(pen, 3, 11, 10, 3); graphics.DrawLine(pen, 6, 5, 6, 7); graphics.DrawLine(pen, 10, 5, 10, 7); }
        else { graphics.DrawArc(pen, 3, 1, 10, 14, 180, 180); graphics.DrawArc(pen, 3, 1, 10, 14, 0, 180); graphics.DrawLine(pen, 8, 2, 8, 6); }
        graphics.Restore(state);
    }
    private void DrawGitHubIcon(Graphics graphics)
    {
        var state = graphics.Save();
        int S(int n) => (int)Math.Round(n * DeviceDpi / 96.0);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TranslateTransform(S(5), (repositoryLink.Height - S(15)) / 2f);
        graphics.ScaleTransform(S(15) / 16f, S(15) / 16f);
        using var pen = new Pen(ForeColor, 1.15f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var path = new GraphicsPath();
        path.AddLines([new PointF(4, 12), new PointF(3, 10), new PointF(2.5f, 6), new PointF(4, 4.5f), new PointF(4, 2), new PointF(6.5f, 3), new PointF(9.5f, 3), new PointF(12, 2), new PointF(12, 4.5f), new PointF(13.5f, 6), new PointF(13, 10), new PointF(11, 12), new PointF(11, 14)]);
        graphics.DrawPath(pen, path); graphics.DrawLines(pen, [new PointF(5, 14), new PointF(5, 12), new PointF(3, 12), new PointF(1.5f, 10.5f)]);
        graphics.Restore(state);
    }
    private void OpenRepository()
    {
        string? url = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "RepositoryUrl")?.Value;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps) return;
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }); }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException) { }
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    { if (keyData == Keys.Escape) { Close(); return true; } return base.ProcessCmdKey(ref msg, keyData); }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { titleFont.Dispose(); bodyFont.Dispose(); valueFont.Dispose(); }
        base.Dispose(disposing);
    }
}

/// <summary>Painted button retains native Button keyboard and accessibility behavior.</summary>
internal sealed class RoundedCloseButton : Button
{
    private bool hovered, pressed;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal Color SurfaceColor { get; set; }

    internal RoundedCloseButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        FlatStyle = FlatStyle.Flat;
    }
    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hovered = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hovered = false; pressed = false; Invalidate(); }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { pressed = true; Invalidate(); } }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); pressed = false; Invalidate(); }
    protected override void OnKeyDown(KeyEventArgs e) { base.OnKeyDown(e); if (e.KeyCode is Keys.Space or Keys.Enter) { pressed = true; Invalidate(); } }
    protected override void OnKeyUp(KeyEventArgs e) { base.OnKeyUp(e); if (e.KeyCode is Keys.Space or Keys.Enter) { pressed = false; Invalidate(); } }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); pressed = false; Invalidate(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(SurfaceColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        int inset = Math.Max(1, (int)Math.Round(DeviceDpi / 96.0));
        int radius = Math.Max(2, (int)Math.Round(8 * DeviceDpi / 96.0));
        using var shape = BatteryMenu.Rounded(new Rectangle(inset, inset, Width - 2 * inset - 1, Height - 2 * inset - 1), radius);
        Color fill = pressed ? ControlPaint.Dark(BackColor, 0.18f) : hovered ? ControlPaint.Dark(BackColor, 0.08f) : BackColor;
        using (var brush = new SolidBrush(fill)) g.FillPath(brush, shape);
        TextRenderer.DrawText(g, Text, Font, ClientRectangle, ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        if (Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(g, Rectangle.Inflate(ClientRectangle, -4 * inset, -4 * inset), ForeColor, fill);
    }
}
