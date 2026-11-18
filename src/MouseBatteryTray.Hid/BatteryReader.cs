using HidSharp;
using System.Text.RegularExpressions;

namespace MouseBatteryTray.Hid;

/// <summary>One guarded, synchronous status exchange. A native feature call can block.</summary>
public static class BatteryReader
{
    public static BatteryDevice SelectDevice()
    {
        var candidates = new List<BatteryDevice>();
        foreach (var device in DeviceList.Local.GetHidDevices())
        {
            if (device.VendorID != 0x5253 || device.ProductID != 0x1020 ||
                !Regex.IsMatch(device.DevicePath, @"(?:^|&)mi_02(?:&|#)", RegexOptions.IgnoreCase)) continue;
            var descriptor = device.GetReportDescriptor();
            var items = descriptor.DeviceItems.Where(item => item.Usages.GetAllValues().Contains(0xFF010001u)).ToArray();
            if (items.Length == 0) continue;
            var lengths = items.SelectMany(item => item.FeatureReports).Where(report => report.ReportID == BatteryProtocol.ReportId)
                .Select(report => report.Length).Distinct().ToArray();
            candidates.Add(new BatteryDevice(device, device.GetMaxFeatureReportLength(), lengths));
        }
        if (candidates.Count != 1) throw new InvalidOperationException($"Expected one matching collection; found {candidates.Count}.");
        var selected = candidates[0];
        if (selected.BufferLength != 65 || selected.ReportLengths.Length != 1 ||
            selected.ReportLengths[0] < BatteryProtocol.LogicalLength || selected.ReportLengths[0] > selected.BufferLength)
            throw new InvalidOperationException("Incompatible or unavailable feature descriptor.");
        return selected;
    }

    public static BatteryReading ReadOnce()
    {
        var selected = SelectDevice();
        using var stream = selected.Device.Open();
        stream.SetFeature(BatteryProtocol.CreateRequest(selected.BufferLength));
        Thread.Sleep(120);
        var response = new byte[selected.BufferLength];
        response[0] = BatteryProtocol.ReportId;
        stream.GetFeature(response);
        var reading = BatteryProtocol.Parse(response) ?? throw new InvalidDataException("Invalid or echoed status response.");
        if (!BatteryProtocol.IsLikelyWirelessOnline(reading)) throw new InvalidDataException("Status does not indicate an active wireless link.");
        return reading;
    }
}

public sealed record BatteryDevice(HidDevice Device, int BufferLength, int[] ReportLengths);
