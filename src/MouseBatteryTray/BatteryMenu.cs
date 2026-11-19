using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Reflection;

namespace MouseBatteryTray;

internal sealed class BatteryMenu : Form
{
    private readonly MenuRow refresh = new("Refresh battery", MenuIcon.Refresh);
    private readonly MenuRow startup = new("Start with Windows", MenuIcon.Monitor);
    private readonly MenuRow alerts = new("Low battery alerts", MenuIcon.Bell);
    private readonly MenuRow about = new("About", MenuIcon.Info);
    private readonly MenuRow exit = new("Exit", MenuIcon.Power);
    private readonly Font titleFont = new("Segoe UI", 10, FontStyle.Bold);
    private readonly Font secondaryFont = new("Segoe UI", 8.5f);
    private readonly Font batteryFont = new("Segoe UI", 15, FontStyle.Bold);
    private readonly Image? mouseImage;
    private bool startupAvailable, light;
    private int? percent;
    private AboutWindow? aboutWindow;

    internal event EventHandler? RefreshRequested;
    internal event EventHandler? ExitRequested;
    internal event EventHandler? StartupRequested;
    internal event EventHandler? AlertsRequested;

    public BatteryMenu()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        KeyPreview = true;
        DoubleBuffered = true;
        AccessibleName = "MCHOSE L7 Ultra+ battery menu";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MouseBatteryTray.Assets.mouse.png");
        if (stream is not null) { using var loaded = Image.FromStream(stream); mouseImage = new Bitmap(loaded); }
        refresh.Click += (_, _) => RefreshRequested?.Invoke(this, EventArgs.Empty);
        startup.Click += (_, _) => StartupRequested?.Invoke(this, EventArgs.Empty);
        alerts.Click += (_, _) => AlertsRequested?.Invoke(this, EventArgs.Empty);
        about.Click += (_, _) => ShowAbout();
        exit.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
        Controls.AddRange([refresh, startup, alerts, about, exit]);
        Deactivate += (_, _) => Hide();
        VisibleChanged += (_, _) => { if (!Visible) TopMost = false; };
        SetTheme(false);
        ApplyLayout();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal int? Percent
    {
        get => percent;
        set { if (percent == value) return; percent = value; AccessibleDescription = value is int p ? $"Battery {p} percent" : "Battery unavailable"; Invalidate(); }
    }

    internal void SetTheme(bool isLight)
    {
        light = isLight;
        BackColor = isLight ? ColorTranslator.FromHtml("#F6F7F9") : ColorTranslator.FromHtml("#101115");
        ForeColor = isLight ? ColorTranslator.FromHtml("#15171B") : ColorTranslator.FromHtml("#F5F5F5");
        foreach (MenuRow row in Controls.OfType<MenuRow>()) row.SetTheme(isLight);
        if (aboutWindow is { IsDisposed: false }) aboutWindow.SetTheme(isLight);
        Invalidate();
    }

    internal void SetStartup(bool available, bool enabled)
    {
        startupAvailable = available;
        startup.Visible = available;
        startup.SetStatus(enabled);
        ApplyLayout();
    }
    internal void SetAlerts(bool enabled) => alerts.SetStatus(enabled);
    internal void SetRefreshing(bool refreshing)
    {
        refresh.Enabled = !refreshing;
        refresh.Text = refreshing ? "Refreshing…" : "Refresh battery";
    }

    internal void ShowAt(Point anchor)
    {
        if (Visible) { Hide(); return; }
        ApplyLayout();
        Rectangle area = Screen.FromPoint(anchor).WorkingArea;
        int x = Math.Clamp(anchor.X - Width, area.Left, Math.Max(area.Left, area.Right - Width));
        int y = anchor.Y - Height;
        if (y < area.Top) y = anchor.Y;
        y = Math.Clamp(y, area.Top, Math.Max(area.Top, area.Bottom - Height));
        Location = new Point(x, y);
        // The notification overflow is topmost; elevate only while this popup is visible.
        TopMost = true;
        Show();
        Activate();
        (refresh.Enabled ? refresh : alerts).Focus();
    }

    private void ApplyLayout()
    {
        int S(int n) => (int)Math.Round(n * DeviceDpi / 96.0);
        int count = startupAvailable ? 5 : 4;
        ClientSize = new Size(S(340), S(119 + count * 37 + 19));
        int y = 116;
        foreach (var row in new[] { refresh, startup, alerts, about, exit })
        {
            if (row == startup && !startupAvailable) continue;
            if (row == about) y += 8;
            row.SetBounds(S(12), S(y), S(316), S(36));
            y += 37;
        }
        using var path = Rounded(new Rectangle(0, 0, Width, Height), S(18));
        var previous = Region;
        Region = new Region(path);
        previous?.Dispose();
        Invalidate();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e); ApplyLayout();
        Rectangle area = Screen.FromRectangle(Bounds).WorkingArea;
        Location = new Point(Math.Clamp(Left, area.Left, Math.Max(area.Left, area.Right - Width)),
            Math.Clamp(Top, area.Top, Math.Max(area.Top, area.Bottom - Height)));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        int S(int n) => (int)Math.Round(n * DeviceDpi / 96.0);
        var primary = ForeColor;
        var secondary = ColorTranslator.FromHtml(light ? "#717782" : "#9699A1");
        int rgb = TrayIconColor.ForReading(percent, percent is null, light);
        Color battery = Color.FromArgb((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);
        TextRenderer.DrawText(g, "MCHOSE L7 Ultra+", titleFont, new Rectangle(S(18), S(16), S(190), S(25)), primary,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(g, "2.4 GHz wireless", secondaryFont, new Rectangle(S(18), S(43), S(185), S(20)), secondary,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        if (mouseImage is not null)
        {
            float scale = Math.Min((float)S(112) / mouseImage.Width, (float)S(56) / mouseImage.Height);
            float w = mouseImage.Width * scale, h = mouseImage.Height * scale;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(mouseImage, new RectangleF(S(211) + (S(112) - w) / 2, S(5), w, h));
        }
        TextRenderer.DrawText(g, percent is int p ? $"{p}%" : "—", batteryFont,
            new Rectangle(S(230), S(64), S(92), S(30)), battery,
            TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        var track = new Rectangle(S(18), S(98), S(304), Math.Max(2, S(5)));
        FillPill(g, track, light ? Color.FromArgb(215, 219, 226) : Color.FromArgb(54, 56, 61));
        if (percent is int level) FillPill(g, new Rectangle(track.X, track.Y, track.Width * level / 100, track.Height), battery);
        using var divider = new Pen(light ? Color.FromArgb(214, 217, 223) : Color.FromArgb(65, 67, 72));
        g.DrawLine(divider, S(18), S(111), Width - S(18), S(111));
        int secondDivider = 116 + (startupAvailable ? 3 : 2) * 37 + 3;
        g.DrawLine(divider, S(18), S(secondDivider), Width - S(18), S(secondDivider));
        using var borderPath = Rounded(new Rectangle(0, 0, Width - 1, Height - 1), S(18));
        using var borderBrush = new LinearGradientBrush(ClientRectangle, light ? Color.FromArgb(224, 227, 233) : Color.FromArgb(165, 168, 176), light ? Color.FromArgb(184, 189, 198) : Color.FromArgb(48, 51, 59), 90);
        using var border = new Pen(borderBrush, Math.Max(1, S(1)));
        g.DrawPath(border, borderPath);
    }

    private static void FillPill(Graphics graphics, Rectangle bounds, Color color)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        using var brush = new SolidBrush(color);
        if (bounds.Width < 2 || bounds.Height < 2) { graphics.FillRectangle(brush, bounds); return; }
        using var path = Rounded(bounds, Math.Min(bounds.Width, bounds.Height) / 2);
        graphics.FillPath(brush, path);
    }
    internal static GraphicsPath Rounded(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath(); int d = Math.Max(2, radius * 2);
        path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure(); return path;
    }

    private void ShowAbout()
    {
        Hide();
        if (aboutWindow is { IsDisposed: false }) { aboutWindow.Activate(); return; }
        var dialog = new AboutWindow(light);
        aboutWindow = dialog;
        dialog.FormClosed += (_, _) => { aboutWindow = null; dialog.Dispose(); };
        dialog.Show();
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape) { Hide(); return true; }
        if (keyData is Keys.Up or Keys.Down) { SelectNextControl(ActiveControl, keyData == Keys.Down, true, true, true); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { aboutWindow?.Dispose(); mouseImage?.Dispose(); titleFont.Dispose(); secondaryFont.Dispose(); batteryFont.Dispose(); }
        base.Dispose(disposing);
    }
}
