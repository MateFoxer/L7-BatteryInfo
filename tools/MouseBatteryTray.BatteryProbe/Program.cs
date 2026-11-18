using MouseBatteryTray.Hid;

Console.WriteLine("MouseBatteryTray — Battery Probe v0.1");
if (args.Length != 1 || args[0] is not ("--dry-run" or "--read-battery"))
{
    Console.WriteLine("Usage: dotnet run --project tools/MouseBatteryTray.BatteryProbe -- <--dry-run|--read-battery>");
    return args.Length == 0 ? 0 : 2;
}
try
{
    if (args[0] == "--dry-run")
    {
        var selected = BatteryReader.SelectDevice();
        Console.WriteLine("VID/PID: 5253:1020; interface: 02; usage: FF01:0001");
        Console.WriteLine($"Maximum feature buffer: {selected.BufferLength} bytes");
        Console.WriteLine($"Report 0x11 descriptor lengths: {string.Join(", ", selected.ReportLengths)}");
        Console.WriteLine($"Proposed request ({selected.BufferLength} bytes): {Convert.ToHexString(BatteryProtocol.CreateRequest(selected.BufferLength))}");
        Console.WriteLine("Dry run only: no communicating stream opened and no reports sent.");
    }
    else
    {
        var reading = BatteryReader.ReadOnce();
        Console.WriteLine($"Battery: {reading.Percent}% (candidate reading; freshness unverified)");
        Console.WriteLine("Charging: Unknown (not verified for this model)");
        Console.WriteLine($"Status flags: 0x{reading.Flags:X2}; charge byte: 0x{reading.ChargeRaw:X2}");
    }
    Console.WriteLine("Protocol compatibility with L7 Ultra+ is unverified.");
    return 0;
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or InvalidOperationException or ArgumentException or TimeoutException)
{
    Console.Error.WriteLine("The probe could not obtain a valid reading or inspect the collection. A feature request may have been sent if an active read was requested.");
    return 1;
}
