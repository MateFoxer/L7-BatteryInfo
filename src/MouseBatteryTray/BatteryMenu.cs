using System.ComponentModel;

namespace MouseBatteryTray;

/// <summary>A small reusable tray popup; closing it hides it until the next tray click.</summary>
internal sealed class BatteryMenu : Form
{
    private readonly Button refresh = CreateAction("↻   Refresh battery", "Refresh battery");
    private readonly Button startup = CreateAction("Start with Windows                       Off", "Start with Windows");
    private readonly Button alerts = CreateAction("Low battery alerts", "Low battery alerts");
    private bool alertsEnabled = true;
    private bool startupAvailable;
    private readonly Button exit = CreateAction("⏻   Exit", "Exit");
    private readonly Font titleFont = new("Segoe UI", 9.5f, FontStyle.Bold);
    private readonly Font secondaryFont = new("Segoe UI", 8.25f);
    private readonly Font batteryFont = new("Segoe UI", 13, FontStyle.Bold);
    private readonly Font actionFont = new("Segoe UI", 9.25f);
    private int? percent;

    internal event EventHandler? RefreshRequested;
    internal event EventHandler? ExitRequested;
    internal event EventHandler? StartupRequested;
    internal event EventHandler? AlertsRequested;

    public BatteryMenu()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(0x25, 0x27, 0x2B);
        ForeColor = Color.FromArgb(0xF2, 0xF2, 0xF2);
        AutoScaleMode = AutoScaleMode.None;
        KeyPreview = true;
        DoubleBuffered = true;
        AccessibleName = "MCHOSE L7 Ultra+ battery menu";
        refresh.Font = actionFont;
        exit.Font = actionFont;
        startup.Font = actionFont;
        alerts.Font = actionFont;
        alerts.Paint += (_, e) => DrawAlertState(e.Graphics);
        alerts.Click += (_, _) => AlertsRequested?.Invoke(this, EventArgs.Empty);
        Controls.Add(alerts);
        startup.Click += (_, _) => StartupRequested?.Invoke(this, EventArgs.Empty);
        Controls.Add(startup);
        refresh.Click += (_, _) => RefreshRequested?.Invoke(this, EventArgs.Empty);
        exit.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
        Controls.Add(refresh);
        Controls.Add(exit);
        Deactivate += (_, _) => Hide();
        ApplyLayout();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal int? Percent
    {
        get => percent;
        set
        {
            if (percent == value) return;
            percent = value;
            AccessibleDescription = value is int p ? $"Battery {p} percent" : "Battery unavailable";
            Invalidate();
        }
    }

    internal void SetStartup(bool available, bool enabled)
    {
        startupAvailable = available;
        startup.Visible = available;
        startup.Text = enabled ? "Start with Windows                       On" : "Start with Windows                       Off";
        startup.ForeColor = enabled ? Color.FromArgb(0x4A, 0xDE, 0x80) : ForeColor;
        ApplyLayout();
    }

    internal void SetAlerts(bool enabled)
    {
        alertsEnabled = enabled;
        alerts.AccessibleDescription = enabled ? "On" : "Off";
        alerts.Invalidate();
    }

    private void DrawAlertState(Graphics graphics)
    {
        int inset = (int)Math.Round(8 * DeviceDpi / 96.0);
        TextRenderer.DrawText(graphics, alertsEnabled ? "On" : "Off", actionFont,
            new Rectangle(0, 0, alerts.Width - inset, alerts.Height),
            alertsEnabled ? Color.FromArgb(0x4A, 0xDE, 0x80) : Color.FromArgb(0x9D, 0xA4, 0xAE),
            TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }

    internal void SetRefreshing(bool refreshing)
    {
        refresh.Enabled = !refreshing;
        refresh.Text = refreshing ? "↻   Refreshing…" : "↻   Refresh battery";
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
        Show();
        Activate();
        (refresh.Enabled ? refresh : exit).Focus();
    }

    private static Button CreateAction(string text, string accessibleName)
    {
        var button = new Button
        {
            Text = text,
            AccessibleName = accessibleName,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleLeft,
            UseVisualStyleBackColor = false,
            BackColor = Color.FromArgb(0x25, 0x27, 0x2B),
            ForeColor = Color.FromArgb(0xF2, 0xF2, 0xF2),
            TabStop = true
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(0x37, 0x39, 0x3E);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(0x40, 0x42, 0x48);
        return button;
    }

    private void ApplyLayout()
    {
        int Scale(int n) => (int)Math.Round(n * DeviceDpi / 96.0);
        ClientSize = new Size(Scale(260), Scale(startupAvailable ? 235 : 195));
        refresh.SetBounds(Scale(12), Scale(68), Scale(236), Scale(38));
        startup.SetBounds(Scale(12), Scale(108), Scale(236), Scale(38));
        alerts.SetBounds(Scale(12), Scale(startupAvailable ? 148 : 108), Scale(236), Scale(38));
        exit.SetBounds(Scale(12), Scale(startupAvailable ? 191 : 151), Scale(236), Scale(36));
        refresh.Padding = exit.Padding = new Padding(Scale(8), 0, 0, 0);
        Invalidate();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        ApplyLayout();
        Rectangle area = Screen.FromRectangle(Bounds).WorkingArea;
        Location = new Point(Math.Clamp(Left, area.Left, Math.Max(area.Left, area.Right - Width)),
            Math.Clamp(Top, area.Top, Math.Max(area.Top, area.Bottom - Height)));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        int Scale(int n) => (int)Math.Round(n * DeviceDpi / 96.0);
        Color primary = Color.FromArgb(0xF2, 0xF2, 0xF2);
        Color secondary = Color.FromArgb(0x9D, 0xA4, 0xAE);
        int rgb = TrayIconColor.ForReading(percent, percent is null);
        Color battery = Color.FromArgb((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);
        var left = new Rectangle(Scale(12), Scale(10), Width - Scale(90), Scale(24));
        TextRenderer.DrawText(e.Graphics, "MCHOSE L7 Ultra+", titleFont, left, primary,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(e.Graphics, "2.4 GHz wireless", secondaryFont,
            new Rectangle(Scale(12), Scale(34), Width - Scale(90), Scale(18)), secondary,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        TextRenderer.DrawText(e.Graphics, percent is int p ? $"{p}%" : "—", batteryFont,
            new Rectangle(Width - Scale(76), Scale(12), Scale(64), Scale(32)), battery,
            TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        // A missing or stale reading has no fill: Percent is null in both cases.
        var track = new Rectangle(Scale(12), Scale(55), Width - Scale(24), Math.Max(1, Scale(4)));
        using (var trackBrush = new SolidBrush(Color.FromArgb(0x41, 0x44, 0x49)))
            e.Graphics.FillRectangle(trackBrush, track);
        if (percent is int level)
        {
            using var fillBrush = new SolidBrush(battery);
            e.Graphics.FillRectangle(fillBrush, track.X, track.Y, track.Width * level / 100, track.Height);
        }
        using var pen = new Pen(Color.FromArgb(0x41, 0x44, 0x49));
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        e.Graphics.DrawLine(pen, Scale(12), Scale(62), Width - Scale(12), Scale(62));
        e.Graphics.DrawLine(pen, Scale(12), Scale(startupAvailable ? 188 : 148), Width - Scale(12), Scale(startupAvailable ? 188 : 148));
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape) { Hide(); return true; }
        if (keyData is Keys.Up or Keys.Down)
        {
            SelectNextControl(ActiveControl, keyData == Keys.Down, true, true, true);
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            titleFont.Dispose();
            secondaryFont.Dispose();
            batteryFont.Dispose();
            actionFont.Dispose();
        }
        base.Dispose(disposing);
    }
}
