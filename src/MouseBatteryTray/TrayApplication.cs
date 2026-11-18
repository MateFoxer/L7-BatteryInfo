using MouseBatteryTray.Hid;
using System.Diagnostics;

namespace MouseBatteryTray;

internal sealed class TrayApplication : ApplicationContext
{
    private readonly NotifyIcon tray;
    private readonly BatteryMenu menu = new();
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 1000 };
    private Icon? icon;
    private (int? Percent, bool Muted)? rendered;
    private BatteryReading? last;
    private long lastTick;
    private long nextPoll;
    private bool busy, failed, closing;

    public TrayApplication()
    {
        menu.RefreshRequested += async (_, _) => await RefreshAsync();
        menu.ExitRequested += (_, _) => ExitThread();
        tray = new NotifyIcon { Visible = true };
        tray.MouseUp += (_, e) =>
        {
            if (e.Button == MouseButtons.Right) menu.ShowAt(Cursor.Position);
        };
        timer.Tick += async (_, _) =>
        {
            UpdateDisplay();
            if (!busy && Stopwatch.GetTimestamp() >= nextPoll)
            {
                nextPoll = Stopwatch.GetTimestamp() + 60 * Stopwatch.Frequency;
                await RefreshAsync();
            }
        };
        nextPoll = Stopwatch.GetTimestamp() + 60 * Stopwatch.Frequency;
        Application.Idle += OnFirstIdle;
        timer.Start();
        UpdateDisplay();
    }

    private async void OnFirstIdle(object? sender, EventArgs e)
    {
        Application.Idle -= OnFirstIdle;
        if (!closing) await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (busy || closing) return;
        busy = true;
        menu.SetRefreshing(true);
        try
        {
            // Synchronous native HID calls run off the UI thread; they cannot be forcibly cancelled safely.
            var result = await Task.Run(BatteryReader.ReadOnce);
            if (closing) return;
            last = result;
            lastTick = Stopwatch.GetTimestamp();
            failed = false;
        }
        catch (Exception)
        {
            if (!closing) failed = true;
        }
        finally
        {
            busy = false;
            if (!closing)
            {
                menu.SetRefreshing(false);
                nextPoll = Stopwatch.GetTimestamp() + 60 * Stopwatch.Frequency;
                UpdateDisplay();
            }
        }
    }

    private void UpdateDisplay()
    {
        bool stale = last is not null && (failed || Stopwatch.GetElapsedTime(lastTick) > TimeSpan.FromSeconds(120));
        menu.Percent = stale ? null : last?.Percent;
        var tooltip = last is null
            ? $"MCHOSE L7 Ultra+\nBattery: unavailable\nStatus: {(busy ? "Reading" : failed ? "Unavailable / disconnected" : "Waiting")}"
            : $"MCHOSE L7 Ultra+\nBattery: {last.Percent}%\nStatus: {(stale ? "Stale" : "Recent candidate reading")}";
        tray.Text = tooltip.Length <= 127 ? tooltip : tooltip[..127];
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
        Application.Idle -= OnFirstIdle;
        closing = true;
        timer.Stop();
        timer.Dispose();
        tray.Visible = false;
        tray.Dispose();
        icon?.Dispose();
        menu.Dispose();
        base.ExitThreadCore();
    }
}
