namespace MouseBatteryTray;

internal static class TrayIconColor
{
    // RGB values are kept independent of GDI+ so threshold behavior is testable without a UI.
    public static int ForReading(int? percent, bool muted) => muted || percent is null ? 0x9CA3AF
        : percent >= 70 ? 0x4ADE80
        : percent >= 30 ? 0xFBBF24
        : 0xFB5B58;
}
