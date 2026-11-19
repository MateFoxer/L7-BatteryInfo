using MouseBatteryTray.Hid;
using System.Diagnostics;
using Microsoft.Win32;
using HidSharp;

namespace MouseBatteryTray;

internal sealed class TrayApplication : ApplicationContext
{
    private readonly NotifyIcon tray;
    private readonly BatteryMenu menu = new();
    private readonly string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MouseBatteryTray", "alerts.json");
    private AlertSettings alerts = new();
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 1000 };
    private Icon? icon;
    private (int? Percent, bool Muted, bool Light)? rendered;
    private bool lightTheme;
    private long nextThemeCheck;
    private BatteryReading? last;
    private long lastTick;
    private long nextPoll;
    private bool busy, failed, closing;
    private readonly LifecycleState lifecycle = new();
    private static long Now() => (long)Stopwatch.GetElapsedTime(0).TotalMilliseconds;

    public TrayApplication()
    {
        lightTheme = WindowsSystemTheme.IsLight();
        nextThemeCheck = Stopwatch.GetTimestamp() + 15 * Stopwatch.Frequency;
        alerts = AlertSettings.Load(settingsPath);
        menu.SetAlerts(alerts.Enabled);
        menu.AlertsRequested += (_, _) => ToggleAlerts();
        menu.RefreshRequested += async (_, _) => await RefreshAsync();
        menu.ExitRequested += (_, _) => ExitThread();
        menu.StartupRequested += (_, _) => ToggleStartup();
        _ = menu.Handle; // A UI-thread handle allows safe dispatch from SystemEvents.
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        DeviceList.Local.Changed += OnDevicesChanged;
        tray = new NotifyIcon { Visible = true };
        tray.MouseUp += (_, e) =>
        {
            if (e.Button == MouseButtons.Right) { UpdateStartup(); menu.ShowAt(Cursor.Position); }
        };
        timer.Tick += async (_, _) =>
        {
            if (Stopwatch.GetTimestamp() >= nextThemeCheck)
            {
                lightTheme = WindowsSystemTheme.IsLight();
                nextThemeCheck = Stopwatch.GetTimestamp() + 15 * Stopwatch.Frequency;
            }
            UpdateDisplay();
            var work = lifecycle.TakeWork(Now(), busy);
            if (work == RecoveryWork.Verify) await VerifyReceiverAsync();
            else if (work == RecoveryWork.Refresh) await RefreshAsync();
            else if (!busy && !lifecycle.Pending && Stopwatch.GetTimestamp() >= nextPoll)
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

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode != PowerModes.Resume || menu.IsDisposed || !menu.IsHandleCreated) return;
        try { menu.BeginInvoke(() =>
        {
            if (closing) return;
            lifecycle.Resume(Now());
            UpdateDisplay();
        }); }
        catch (InvalidOperationException) { } // The UI may have been disposed during shutdown.
    }
    private void OnDevicesChanged(object? sender, DeviceListChangedEventArgs e)
    {
        if (menu.IsDisposed || !menu.IsHandleCreated) return;
        try { menu.BeginInvoke(() =>
        {
            if (closing) return;
            lifecycle.DeviceChanged(Now());
            UpdateDisplay();
        }); }
        catch (InvalidOperationException) { }
    }

    private async Task VerifyReceiverAsync()
    {
        busy = true;
        int generation = lifecycle.Generation;
        try
        {
            var state = await Task.Run(() =>
            {
                try { _ = BatteryReader.SelectDevice(); return ReceiverState.Present; }
                catch (ReceiverAbsentException) { return ReceiverState.Absent; }
                catch (Exception) { return ReceiverState.Unknown; }
            });
            lifecycle.Verified(state, generation, Now());
        }
        finally
        {
            busy = false;
            if (!closing) UpdateDisplay();
        }
    }
    private void ToggleAlerts()
    {
        bool previous = alerts.Enabled;
        alerts.Enabled = !previous;
        if (!alerts.Save(settingsPath)) { alerts.Enabled = previous; MessageBox.Show("Alert preference could not be saved.", "MouseBatteryTray"); }
        menu.SetAlerts(alerts.Enabled);
    }

    private void UpdateStartup()
    {
        try
        {
            string? path = WindowsStartup.PublishedExecutable();
            menu.SetStartup(path is not null, path is not null && WindowsStartup.IsEnabled(path));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            menu.SetStartup(false, false);
        }
    }

    private void ToggleStartup()
    {
        try
        {
            string? path = WindowsStartup.PublishedExecutable();
            if (path is null) return;
            WindowsStartup.SetEnabled(!WindowsStartup.IsEnabled(path), path);
            UpdateStartup();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or InvalidOperationException)
        {
            MessageBox.Show("Windows startup could not be updated. An existing entry may point to another application.", "MouseBatteryTray");
        }
    }
    private async void OnFirstIdle(object? sender, EventArgs e)
    {
        Application.Idle -= OnFirstIdle;
        if (!closing) await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (busy || closing || lifecycle.Pending) return;
        busy = true;
        int generation = lifecycle.Generation;
        menu.SetRefreshing(true);
        try
        {
            // Synchronous native HID calls run off the UI thread; they cannot be forcibly cancelled safely.
            var result = await Task.Run(BatteryReader.ReadOnce);
            if (!lifecycle.Accept(generation)) return;
            lifecycle.ReadingAccepted();
            last = result;
            lastTick = Stopwatch.GetTimestamp();
            failed = false;
            NotifyIfLow(result.Percent);
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

    private void NotifyIfLow(int percent)
    {
        int? threshold = alerts.ObserveAndPersist(percent, true, () => alerts.Save(settingsPath));
        if (threshold is null) return;
        try
        {
            tray.BalloonTipTitle = "MCHOSE L7 Ultra+";
            tray.BalloonTipText = threshold == 10 ? $"Mouse battery is critically low - {percent}% remaining." : $"Mouse battery is low - {percent}% remaining.";
            tray.BalloonTipIcon = ToolTipIcon.Warning;
            tray.ShowBalloonTip(5000);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception) { }
    }
    private void UpdateDisplay()
    {
        bool stale = last is not null && (lifecycle.Stale || failed || Stopwatch.GetElapsedTime(lastTick) > TimeSpan.FromSeconds(120));
        menu.Percent = stale || lifecycle.Receiver == ReceiverState.Absent ? null : last?.Percent;
        var tooltip = last is null || lifecycle.Receiver == ReceiverState.Absent
            ? $"MCHOSE L7 Ultra+\nBattery: unavailable\nStatus: {(busy ? "Reading" : failed ? "Unavailable / disconnected" : "Waiting")}"
            : $"MCHOSE L7 Ultra+\nBattery: {last.Percent}%\nStatus: {(stale ? "Stale" : "Recent candidate reading")}";
        tray.Text = tooltip.Length <= 127 ? tooltip : tooltip[..127];
        var appearance = (Percent: lifecycle.Receiver == ReceiverState.Absent ? null : last?.Percent, Muted: last is null || stale || lifecycle.Receiver == ReceiverState.Absent, Light: lightTheme);
        if (rendered != appearance)
        {
            var next = TrayIconRenderer.Create(appearance.Percent, appearance.Muted, appearance.Light);
            tray.Icon = next;
            icon?.Dispose();
            icon = next;
            rendered = appearance;
        }
    }

    protected override void ExitThreadCore()
    {
        Application.Idle -= OnFirstIdle;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        DeviceList.Local.Changed -= OnDevicesChanged;
        closing = true;
        lifecycle.Shutdown();
        timer.Stop();
        timer.Dispose();
        tray.Visible = false;
        tray.Dispose();
        icon?.Dispose();
        menu.Dispose();
        base.ExitThreadCore();
    }
}
