using MouseBatteryTray.Hid;
using System.Diagnostics;

namespace MouseBatteryTray;

internal sealed class TrayApplication : ApplicationContext
{
    private readonly NotifyIcon tray;
    private readonly ContextMenuStrip menu = new();
    private readonly ToolStripMenuItem statusItem = new("Battery: unavailable") { Enabled = false };
    private readonly ToolStripMenuItem refreshItem = new("Refresh now");
    private readonly ToolStripMenuItem monitorItem = new("Enable automatic monitoring");
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 1000 };
    private Icon? icon;
    private (int? Percent, bool Muted)? rendered;
    private BatteryReading? last;
    private DateTimeOffset lastAt;
    private long lastTick;
    private long nextPoll;
    private bool monitoring, busy, failed, closing;
    private int generation;

    public TrayApplication()
    {
        menu.Items.Add(new ToolStripMenuItem("MCHOSE L7 Ultra+") { Enabled = false });
        menu.Items.Add(statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(refreshItem);
        menu.Items.Add(monitorItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());
        refreshItem.Click += async (_, _) => await RefreshAsync();
        monitorItem.Click += async (_, _) =>
        {
            monitoring = !monitoring;
            generation++;
            monitorItem.Text = monitoring ? "Disable automatic monitoring" : "Enable automatic monitoring";
            nextPoll = Stopwatch.GetTimestamp() + 60 * Stopwatch.Frequency;
            UpdateDisplay();
            if (monitoring) await RefreshAsync();
        };
        tray = new NotifyIcon { ContextMenuStrip = menu, Visible = true };
        timer.Tick += async (_, _) =>
        {
            UpdateDisplay();
            if (monitoring && !busy && Stopwatch.GetTimestamp() >= nextPoll)
            {
                nextPoll = Stopwatch.GetTimestamp() + 60 * Stopwatch.Frequency;
                await RefreshAsync();
            }
        };
        timer.Start();
        UpdateDisplay();
    }

    private async Task RefreshAsync()
    {
        if (busy || closing) return;
        busy = true;
        int startGeneration = generation;
        refreshItem.Enabled = false;
        try
        {
            // Synchronous native HID calls run off the UI thread; they cannot be forcibly cancelled safely.
            var result = await Task.Run(BatteryReader.ReadOnce);
            if (closing || startGeneration != generation) return;
            last = result;
            lastAt = DateTimeOffset.Now;
            lastTick = Stopwatch.GetTimestamp();
            failed = false;
        }
        catch (Exception)
        {
            // A failed in-flight read invalidates recency even if monitoring was toggled.
            if (!closing) failed = true;
        }
        finally
        {
            busy = false;
            if (!closing)
            {
                refreshItem.Enabled = true;
                if (monitoring) nextPoll = Stopwatch.GetTimestamp() + 60 * Stopwatch.Frequency;
                UpdateDisplay();
            }
        }
    }

    private void UpdateDisplay()
    {
        bool stale = last is not null && (failed || Stopwatch.GetElapsedTime(lastTick) > TimeSpan.FromSeconds(120));
        string status = last is null ? (failed ? "Unavailable / disconnected" : "Unavailable")
            : stale ? $"{last.Percent}% — stale (last {lastAt:HH:mm:ss})"
            : $"{last.Percent}% — recent candidate ({lastAt:HH:mm:ss})";
        statusItem.Text = $"Battery: {status}";
        var tooltip = $"MCHOSE L7 Ultra+\n{status}";
        tray.Text = tooltip.Length <= 127 ? tooltip : tooltip[..127];
        // No protocol transaction ID exists: even a recent candidate's freshness cannot be proven.
        var appearance = (Percent: last?.Percent, Muted: last is null || stale);
        if (rendered != appearance)
        {
            var next = TrayIconRenderer.Create(appearance.Percent, appearance.Muted);
            tray.Icon = next;
            icon?.Dispose();
            icon = next;
            rendered = appearance;
        }
    }

    protected override void ExitThreadCore()
    {
        closing = true;
        generation++;
        monitoring = false;
        timer.Stop();
        timer.Dispose();
        tray.Visible = false;
        tray.Dispose();
        icon?.Dispose();
        menu.Dispose();
        base.ExitThreadCore();
    }
}
