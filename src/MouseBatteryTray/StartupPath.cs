namespace MouseBatteryTray;

internal static class StartupPath
{
    internal static bool IsStable(string? path, string tempPath, bool exists)
    {
        if (!exists || string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path) ||
            !string.Equals(Path.GetFileName(path), "L7-BatteryInfo.exe", StringComparison.OrdinalIgnoreCase)) return false;
        try
        {
            string full = Path.GetFullPath(path);
            string temp = Path.GetFullPath(tempPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return !full.StartsWith(temp, StringComparison.OrdinalIgnoreCase) &&
                !full.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(p =>
                    p.Equals("bin", StringComparison.OrdinalIgnoreCase) || p.Equals("obj", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) { return false; }
    }

    // Reuse the legacy value name and refuse to replace any other command.
    internal static bool Conflicts(object? value, string executable) =>
        value is not null && (value is not string command || !Matches(command, executable));

    internal static string Command(string path) => $"\"{Path.GetFullPath(path)}\"";

    internal static bool Matches(string? command, string executable)
    {
        if (string.IsNullOrWhiteSpace(command)) return false;
        string value = command.Trim();
        if (value.Length < 2 || value[0] != '"' || value[^1] != '"') return false;
        try { return string.Equals(Path.GetFullPath(value[1..^1]), Path.GetFullPath(executable), StringComparison.OrdinalIgnoreCase); }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) { return false; }
    }
}
