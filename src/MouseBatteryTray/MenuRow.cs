using System.Drawing.Drawing2D;

namespace MouseBatteryTray;

internal enum MenuIcon { Refresh, Monitor, Bell, Info, Power }

internal sealed class MenuRow : Button
{
    private readonly MenuIcon icon;
    private readonly Font rowFont = new("Segoe UI", 9.5f);
    private bool light, hovered;
    private bool? status;

    internal MenuRow(string label, MenuIcon icon)
    {
        Text = label; AccessibleName = label; AccessibleRole = AccessibleRole.PushButton;
        this.icon = icon; TabStop = true; FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }
    internal void SetTheme(bool value) { light = value; Invalidate(); }
    internal void SetStatus(bool value) { status = value; AccessibleDescription = value ? "On" : "Off"; Invalidate(); }
    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hovered = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hovered = false; Invalidate(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        int S(int n) => (int)Math.Round(n * DeviceDpi / 96.0);
        Color bg = light ? Color.FromArgb(246, 247, 249) : Color.FromArgb(16, 17, 21);
        Color fg = light ? Color.FromArgb(21, 23, 27) : Color.FromArgb(245, 245, 245);
        g.Clear(bg);
        if (hovered || Focused)
        {
            using var hover = new SolidBrush(light ? Color.FromArgb(230, 233, 238) : Color.FromArgb(40, 42, 48));
            using var path = BatteryMenu.Rounded(new Rectangle(0, S(2), Width - 1, Height - S(4)), S(8));
            g.FillPath(hover, path);
        }
        if (!Enabled) fg = light ? Color.Gray : Color.FromArgb(140, 143, 151);
        DrawIcon(g, fg, S(9), (Height - S(16)) / 2, S(16));
        TextRenderer.DrawText(g, Text, rowFont, new Rectangle(S(40), 0, Width - S(status.HasValue ? 104 : 46), Height), fg,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        if (status is bool on)
            TextRenderer.DrawText(g, on ? "On" : "Off", rowFont, new Rectangle(Width - S(55), 0, S(44), Height),
                on ? (light ? Color.FromArgb(30, 140, 60) : Color.FromArgb(74, 222, 128)) : (light ? Color.FromArgb(113, 119, 130) : Color.FromArgb(150, 153, 161)),
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(g, new Rectangle(S(3), S(3), Width - S(6), Height - S(6)), fg, bg);
    }
    private void DrawIcon(Graphics g, Color color, int x, int y, int size)
    {
        var state = g.Save(); g.TranslateTransform(x, y); g.ScaleTransform(size / 16f, size / 16f);
        using var pen = new Pen(color, 1.45f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        switch (icon)
        {
            case MenuIcon.Refresh:
                g.DrawArc(pen, 2, 2, 12, 12, 205, 130); g.DrawLines(pen, [new PointF(12, 2), new PointF(14, 2), new PointF(14, 5)]);
                g.DrawArc(pen, 2, 2, 12, 12, 25, 130); g.DrawLines(pen, [new PointF(4, 14), new PointF(2, 14), new PointF(2, 11)]); break;
            case MenuIcon.Monitor:
                g.DrawRectangle(pen, 1.5f, 2.5f, 13, 9); g.DrawLine(pen, 8, 11.5f, 8, 14); g.DrawLine(pen, 5, 14, 11, 14); break;
            case MenuIcon.Bell:
                g.DrawArc(pen, 3, 2, 10, 11, 180, 180); g.DrawLines(pen, [new PointF(3, 7), new PointF(3, 11), new PointF(1.5f, 12.5f), new PointF(14.5f, 12.5f), new PointF(13, 11), new PointF(13, 7)]); g.DrawArc(pen, 6, 12, 4, 3, 0, 180); break;
            case MenuIcon.Info:
                g.DrawEllipse(pen, 1.5f, 1.5f, 13, 13); g.DrawLine(pen, 8, 7, 8, 11); g.DrawEllipse(pen, 7.5f, 4, 1, 1); break;
            case MenuIcon.Power:
                g.DrawArc(pen, 2, 3, 12, 12, -45, 270); g.DrawLine(pen, 8, 1, 8, 8); break;
        }
        g.Restore(state);
    }
    protected override void Dispose(bool disposing) { if (disposing) rowFont.Dispose(); base.Dispose(disposing); }
}
