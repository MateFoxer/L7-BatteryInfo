using Microsoft.Win32;

namespace MouseBatteryTray;

internal static class WindowsSystemTheme
{
    public static bool IsLight()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", false);
            return TrayIconColor.IsLightTheme(key?.GetValue("SystemUsesLightTheme"));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return false;
        }
    }
}
