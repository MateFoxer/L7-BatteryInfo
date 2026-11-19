namespace MouseBatteryTray.Hid.Tests;

public class ThemeTests
{
    [Theory]
    [InlineData(0, false)] [InlineData(1, true)] [InlineData(2, false)] [InlineData(-1, false)]
    public void AcceptsOnlyValidLightSetting(int value, bool expected) => Assert.Equal(expected, TrayIconColor.IsLightTheme(value));
    [Fact] public void MissingAndWrongTypeDefaultDark()
    {
        Assert.False(TrayIconColor.IsLightTheme(null));
        Assert.False(TrayIconColor.IsLightTheme("1"));
    }
    [Theory]
    [InlineData(0, 0xC0392B)] [InlineData(29, 0xC0392B)]
    [InlineData(30, 0xA66300)] [InlineData(69, 0xA66300)]
    [InlineData(70, 0x1E8C3C)] [InlineData(100, 0x1E8C3C)]
    public void LightThresholds(int value, int expected) => Assert.Equal(expected, TrayIconColor.ForReading(value, false, true));
    [Fact] public void MutedAndThemeChange()
    {
        Assert.Equal(0x666666, TrayIconColor.ForReading(null, false, true));
        Assert.Equal(0x666666, TrayIconColor.ForReading(85, true, true));
        Assert.Equal(0x9CA3AF, TrayIconColor.ForReading(85, true, false));
        var dark = (Percent: (int?)85, Muted: false, Light: false);
        var light = (Percent: (int?)85, Muted: false, Light: true);
        Assert.NotEqual(dark, light);
        Assert.NotEqual(TrayIconColor.ForReading(85, false, false), TrayIconColor.ForReading(85, false, true));
    }
}
