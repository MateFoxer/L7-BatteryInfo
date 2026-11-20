namespace MouseBatteryTray.Hid.Tests;
public class AlertSettingsTests
{
    [Fact] public void ThresholdsAndRecovery()
    {
        var a = new AlertSettings(); Assert.Equal(20, a.Observe(20, true)); Assert.Null(a.Observe(19, true));
        Assert.Equal(10, a.Observe(10, true)); Assert.Null(a.Observe(9, true));
        Assert.Null(a.Observe(25, true)); Assert.Null(a.Observe(20, true));
        Assert.Null(a.Observe(26, true)); Assert.Equal(20, a.Observe(20, true));
    }
    [Fact] public void DirectCriticalAndInvalid()
    {
        var a = new AlertSettings(); Assert.Null(a.Observe(4, false)); Assert.Null(a.Observe(null, true));
        Assert.Null(a.Observe(101, true)); Assert.Equal(10, a.Observe(9, true)); Assert.Null(a.Observe(15, true));
    }
    [Fact] public void SettingsPersistAndDefault()
    {
        string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()); string path = Path.Combine(dir, "alerts.json");
        try { Assert.True(AlertSettings.Load(path).Enabled); var a = new AlertSettings { Enabled = false, LowSent = true }; Assert.True(a.Save(path)); var b = AlertSettings.Load(path); Assert.False(b.Enabled); Assert.True(b.LowSent); File.WriteAllText(path, "{"); Assert.True(AlertSettings.Load(path).Enabled); }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }
    [Fact] public void SavesOnlyChangedState()
    {
        var a = new AlertSettings(); int saves = 0;
        bool Save() { saves++; return true; }
        Assert.Null(a.ObserveAndPersist(90, true, Save));
        Assert.Null(a.ObserveAndPersist(20, false, Save));
        Assert.Equal(0, saves);
        Assert.Equal(20, a.ObserveAndPersist(20, true, Save));
        Assert.Null(a.ObserveAndPersist(19, true, Save));
        Assert.Equal(10, a.ObserveAndPersist(10, true, Save));
        Assert.Null(a.ObserveAndPersist(26, true, Save));
        Assert.Null(a.ObserveAndPersist(90, true, Save));
        Assert.Equal(3, saves);
    }
    [Fact] public void FailedSaveSuppressesAndAllowsRetry()
    {
        var a = new AlertSettings();
        Assert.Null(a.ObserveAndPersist(10, true, () => false));
        Assert.False(a.LowSent); Assert.False(a.CriticalSent);
        Assert.Equal(10, a.ObserveAndPersist(10, true, () => true));
        Assert.Null(a.ObserveAndPersist(30, true, () => false));
        Assert.True(a.LowSent); Assert.True(a.CriticalSent);
    }
}
