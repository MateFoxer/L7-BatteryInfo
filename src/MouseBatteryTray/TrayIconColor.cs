namespace MouseBatteryTray;

internal static class TrayIconColor
{
    public static bool IsLightTheme(object? value) => value is int n && n == 1;

    // The popup continues to use the existing dark palette by default.
    public static int ForReading(int? percent, bool muted, bool light = false) => muted || percent is null
        ? light ? 0x666666 : 0x9CA3AF
        : percent >= 70 ? light ? 0x1E8C3C : 0x4ADE80
        : percent >= 30 ? light ? 0xA66300 : 0xFBBF24
        : light ? 0xC0392B : 0xFB5B58;
}
