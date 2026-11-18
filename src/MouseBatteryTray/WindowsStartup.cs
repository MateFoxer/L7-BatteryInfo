using Microsoft.Win32;

namespace MouseBatteryTray;

internal static class WindowsStartup
{
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "MouseBatteryTray";

    public static string? PublishedExecutable()
    {
        string? path = Environment.ProcessPath;
        // A stable deployed exe must be outside build and temporary directories.
        return StartupPath.IsStable(path, Path.GetTempPath(), path is not null && File.Exists(path)) &&
            !File.Exists(Path.ChangeExtension(path, ".dll")) && !File.Exists(Path.ChangeExtension(path, ".runtimeconfig.json")) ? path : null;
    }

    private static string? ReadCommand(RegistryKey? key) => key?.GetValue(ValueName) as string;
    public static bool IsEnabled(string executable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, false);
        return StartupPath.Matches(ReadCommand(key), executable);
    }

    public static void SetEnabled(bool enabled, string executable)
    {
        using var key = Registry.CurrentUser.CreateSubKey(KeyPath, true) ?? throw new InvalidOperationException("Startup settings are unavailable.");
        object? value = key.GetValue(ValueName);
        if (value is not null && value is not string) throw new InvalidOperationException("The startup entry is not a command string.");
        var existing = value as string;
        if (existing is not null && !StartupPath.Matches(existing, executable))
            throw new InvalidOperationException("An existing startup entry belongs to a different executable.");
        if (enabled) key.SetValue(ValueName, StartupPath.Command(executable), RegistryValueKind.String);
        else key.DeleteValue(ValueName, false);
    }
}
