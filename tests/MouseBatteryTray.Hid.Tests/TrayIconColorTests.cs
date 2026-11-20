namespace MouseBatteryTray.Hid.Tests;

public class TrayIconColorTests
{
    [Theory]
    [InlineData(0, 0xFB5B58)]
    [InlineData(29, 0xFB5B58)]
    [InlineData(30, 0xFBBF24)]
    [InlineData(69, 0xFBBF24)]
    [InlineData(70, 0x4ADE80)]
    [InlineData(100, 0x4ADE80)]
    public void UsesThresholdColors(int percent, int expected) =>
        Assert.Equal(expected, TrayIconColor.ForReading(percent, false));

    [Theory]
    [InlineData(null, false)]
    [InlineData(85, true)]
    [InlineData(12, true)]
    public void UnavailableAndStaleAreMuted(int? percent, bool muted) =>
        Assert.Equal(0x9CA3AF, TrayIconColor.ForReading(percent, muted));
}
