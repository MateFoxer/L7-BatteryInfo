using HidSharp;
using System.Text.RegularExpressions;

namespace MouseBatteryTray.Hid;

public sealed record HidInterfaceInfo(int VendorId, int ProductId, string? Manufacturer,
    string? Product, string? InterfaceNumber, string[] Usages,
    int? InputLength, int? OutputLength, int? FeatureLength)
{
    public bool IsCandidate => VendorId is 0x5253 or 0x3837 ||
        (Manufacturer?.Contains("MCHOSE", StringComparison.OrdinalIgnoreCase) ?? false) ||
        (Product?.Contains("MCHOSE", StringComparison.OrdinalIgnoreCase) ?? false);
}

public static class HidDiscovery
{
    public static IEnumerable<HidInterfaceInfo> Scan()
    {
        foreach (var device in DeviceList.Local.GetHidDevices())
        {
            // Windows device paths contain MI_nn for composite USB interfaces. Never print the path.
            var interfaceNumber = Read(() => Regex.Match(device.DevicePath, @"(?:^|&)mi_([0-9a-f]{2})(?:&|#)", RegexOptions.IgnoreCase)) is { Success: true } match
                ? $"0x{match.Groups[1].Value.ToUpperInvariant()}" : null;
            var usages = Read(() => device.GetReportDescriptor().DeviceItems
                .SelectMany(item => item.Usages.GetAllValues())
                .Select(usage => $"Page 0x{usage >> 16:X4}, Usage 0x{usage & 0xFFFF:X4}")
                .Distinct().ToArray()) ?? [];
            yield return new HidInterfaceInfo(device.VendorID, device.ProductID,
                Read(device.GetManufacturer), Read(device.GetProductName), interfaceNumber, usages,
                ReadValue(device.GetMaxInputReportLength), ReadValue(device.GetMaxOutputReportLength),
                ReadValue(device.GetMaxFeatureReportLength));
        }
    }

    private static T? Read<T>(Func<T> read) where T : class
    {
        try { return read(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or InvalidOperationException or ArgumentException) { return null; }
    }

    private static int? ReadValue(Func<int> read)
    {
        try { return read(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or InvalidOperationException or ArgumentException) { return null; }
    }
}
