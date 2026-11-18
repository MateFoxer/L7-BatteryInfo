using MouseBatteryTray.Hid;

if (args.Any(arg => arg is not "--all"))
{
    Console.Error.WriteLine("Usage: dotnet run --project tools/MouseBatteryTray.DeviceScanner -- [--all]");
    return 2;
}

Console.WriteLine("MouseBatteryTray — Device Scanner v0.1\n\nScanning HID devices...\n");
try
{
    var devices = HidDiscovery.Scan().ToList();
    var candidates = devices.Count(device => device.IsCandidate);
    foreach (var device in devices.Where(device => args.Contains("--all") || device.IsCandidate))
    {
        Console.WriteLine(device.IsCandidate ? "Potential MCHOSE device (unverified)" : "HID interface");
        Console.WriteLine($"Vendor ID:      0x{device.VendorId:X4}");
        Console.WriteLine($"Product ID:     0x{device.ProductId:X4}");
        Console.WriteLine($"Manufacturer:   {Display(device.Manufacturer)}");
        Console.WriteLine($"Product:        {Display(device.Product)}");
        Console.WriteLine($"Interface:      {Display(device.InterfaceNumber)}");
        Console.WriteLine($"Usages:         {(device.Usages.Length == 0 ? "Unavailable" : string.Join("; ", device.Usages))}");
        Console.WriteLine($"Report lengths: input {Length(device.InputLength)}, output {Length(device.OutputLength)}, feature {Length(device.FeatureLength)} bytes (including report ID)");
        Console.WriteLine(device.IsCandidate ? "Status:         Candidate only; model and compatibility unverified\n" : "Status:         Not identified as a candidate\n");
    }
    if (candidates == 0) Console.WriteLine("No MCHOSE candidates found. Try --all to inspect other HID interfaces.\n");
    Console.WriteLine($"Interfaces scanned: {devices.Count}; candidates: {candidates}");
    Console.WriteLine("Battery: Not implemented\n\nScan complete.");
    return 0;
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or InvalidOperationException)
{
    Console.Error.WriteLine("HID enumeration failed or is unavailable. Reconnect the receiver and try again.");
    return 1;
}

static string Display(string? value) => string.IsNullOrWhiteSpace(value) ? "Unavailable" : value;
static string Length(int? value) => value?.ToString() ?? "unavailable";
