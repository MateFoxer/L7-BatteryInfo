using System.Text.Json;

namespace MouseBatteryTray;

internal sealed class AlertSettings
{
    public bool Enabled { get; set; } = true;
    public bool LowSent { get; set; }
    public bool CriticalSent { get; set; }

    public int? Observe(int? percent, bool valid)
    {
        if (!valid || percent is null || percent < 0 || percent > 100) return null;
        if (percent > 25) { LowSent = false; CriticalSent = false; return null; }
        if (!Enabled) return null;
        if (percent <= 10 && !CriticalSent) { CriticalSent = true; LowSent = true; return 10; }
        if (percent <= 20 && !LowSent) { LowSent = true; return 20; }
        return null;
    }

    // Roll back on failure so a later reading can retry without issuing an unsaved alert.
    public int? ObserveAndPersist(int? percent, bool valid, Func<bool> save)
    {
        bool oldLow = LowSent, oldCritical = CriticalSent;
        int? threshold = Observe(percent, valid);
        if (oldLow == LowSent && oldCritical == CriticalSent) return null;
        if (save()) return threshold;
        LowSent = oldLow;
        CriticalSent = oldCritical;
        return null;
    }
    public static AlertSettings Load(string path)
    {
        try { return JsonSerializer.Deserialize<AlertSettings>(File.ReadAllText(path)) ?? new(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { return new(); }
    }

    public bool Save(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(this));
            File.Move(temp, path, true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return false; }
    }
}
