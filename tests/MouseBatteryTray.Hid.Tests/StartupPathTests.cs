namespace MouseBatteryTray.Hid.Tests;

public class StartupPathTests
{
    private const string Stable = @"C:\Users\Test\App Data\L7-BatteryInfo.exe";
    [Theory]
    [InlineData(@"C:\Users\Test\App Data\L7-BatteryInfo.exe", true)]
    [InlineData(@"C:\project\bin\portable\L7-BatteryInfo.exe", false)]
    [InlineData(@"C:\project\obj\L7-BatteryInfo.exe", false)]
    [InlineData(@"C:\temp\L7-BatteryInfo.exe", false)]
    [InlineData(@"C:\Program Files\dotnet\dotnet.exe", false)]
    [InlineData(@"relative\L7-BatteryInfo.exe", false)]
    public void ValidatesPaths(string path, bool expected) => Assert.Equal(expected, StartupPath.IsStable(path, @"C:\temp", true));
    [Fact] public void RequiresFile() => Assert.False(StartupPath.IsStable(Stable, @"C:\temp", false));
    [Fact] public void QuotesAndMatchesOnlySameExecutable()
    {
        var command = StartupPath.Command(Stable);
        Assert.Equal("\"" + Stable + "\"", command);
        Assert.True(StartupPath.Matches(command, Stable));
        Assert.False(StartupPath.Matches(null, Stable));
        Assert.False(StartupPath.Matches("\"C:\\old\\L7-BatteryInfo.exe\"", Stable));
        Assert.False(StartupPath.Matches(command + " --extra", Stable));
    }
    [Fact] public void LegacyEntriesAreNeverOverwrittenOrRemoved()
    {
        string old = @"C:\Users\Test\App Data\MouseBatteryTray.exe";
        Assert.True(StartupPath.Conflicts(StartupPath.Command(old), Stable));
        Assert.False(StartupPath.IsStable(old, @"C:\temp", true));
        Assert.False(StartupPath.Conflicts(null, Stable));
        Assert.False(StartupPath.Conflicts(StartupPath.Command(Stable), Stable));
        Assert.True(StartupPath.Conflicts(42, Stable));
    }
}
