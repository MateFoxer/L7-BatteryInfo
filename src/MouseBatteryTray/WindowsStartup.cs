using Microsoft.Win32;
using System.Reflection;

namespace MouseBatteryTray;

internal static class WindowsStartup
{
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "MouseBatteryTray";

    // Single-file publishing embeds the managed assembly; development builds have a DLL location.
    // Published binaries must also be moved out of build and temporary directories.
    public static string? PublishedExecutable()
    {
        string? path = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(Assembly.GetEntryAssembly()?.Location) || path is null ||
            !string.Equals(Path.GetFileName(path), "MouseBatteryTray.exe", StringComparison.OrdinalIgnoreCase)) return null;
        path = Path.GetFullPath(path);
        string temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (path.StartsWith(temp, StringComparison.OrdinalIgnoreCase) ||
            path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(part => part.Equals("bin", StringComparison.OrdinalIgnoreCase) || part.Equals("obj", StringComparison.OrdinalIgnoreCase))) return null;
        return path;
    }

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, false);
        return key?.GetValue(ValueName) is string value && !string.IsNullOrWhiteSpace(value);
    }

    public static void SetEnabled(bool enabled, string executable)
    {
        using var key = Registry.CurrentUser.CreateSubKey(KeyPath, true) ?? throw new InvalidOperationException("Startup settings are unavailable.");
        if (enabled) key.SetValue(ValueName, $"\"{executable}\"", RegistryValueKind.String);
        else key.DeleteValue(ValueName, false);
    }
}
