namespace MouseBatteryTray.Hid.Tests;

public class StartupPathTests
{
    private const string Stable = @"C:\Users\Test\App Data\MouseBatteryTray.exe";
    [Theory]
    [InlineData(@"C:\Users\Test\App Data\MouseBatteryTray.exe", true)]
    [InlineData(@"C:\project\bin\portable\MouseBatteryTray.exe", false)]
    [InlineData(@"C:\project\obj\MouseBatteryTray.exe", false)]
    [InlineData(@"C:\temp\MouseBatteryTray.exe", false)]
    [InlineData(@"C:\Program Files\dotnet\dotnet.exe", false)]
    [InlineData(@"relative\MouseBatteryTray.exe", false)]
    public void ValidatesPaths(string path, bool expected) => Assert.Equal(expected, StartupPath.IsStable(path, @"C:\temp", true));
    [Fact] public void RequiresFile() => Assert.False(StartupPath.IsStable(Stable, @"C:\temp", false));
    [Fact] public void QuotesAndMatchesOnlySameExecutable()
    {
        var command = StartupPath.Command(Stable);
        Assert.Equal("\"" + Stable + "\"", command);
        Assert.True(StartupPath.Matches(command, Stable));
        Assert.False(StartupPath.Matches(null, Stable));
        Assert.False(StartupPath.Matches("\"C:\\old\\MouseBatteryTray.exe\"", Stable));
        Assert.False(StartupPath.Matches(command + " --extra", Stable));
    }
}
