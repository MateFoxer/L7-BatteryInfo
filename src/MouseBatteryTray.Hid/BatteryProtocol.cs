using System.Buffers.Binary;

namespace MouseBatteryTray.Hid;

public enum BatteryResponseError { None, TooShort, EchoedRequest, InvalidReportId, WrongCommand, InvalidVendor, InvalidModel, PercentageOutOfRange }

/// <summary>Candidate native-0x11 status format. No device I/O is performed here.</summary>
public static class BatteryProtocol
{
    public const byte ReportId = 0x11;
    public const byte Command = 0x06;
    public const int LogicalLength = 21;

    public static byte[] CreateRequest(int bufferLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(bufferLength, LogicalLength);
        var result = new byte[bufferLength];
        result[0] = ReportId;
        result.AsSpan(1, 20).Fill(0xFF);
        result[1] = Invert(Command);
        return result;
    }

    public static byte Invert(byte value) => (byte)(value ^ 0xFF);

    // A conservative interpretation from related devices, not proof of freshness.
    public static bool IsLikelyWirelessOnline(BatteryReading reading) =>
        (reading.Flags & 0x07) == 1 && (reading.Flags & 0x08) != 0;

    public static BatteryReading? Parse(ReadOnlySpan<byte> response) => Parse(response, out _);

    public static BatteryReading? Parse(ReadOnlySpan<byte> response, out BatteryResponseError error)
    {
        error = BatteryResponseError.None;
        if (response.Length < 13) { error = BatteryResponseError.TooShort; return null; }
        if (response[0] != ReportId) { error = BatteryResponseError.InvalidReportId; return null; }
        if (Invert(response[1]) != Command) { error = BatteryResponseError.WrongCommand; return null; }
        Span<byte> body = stackalloc byte[11];
        for (int i = 0; i < body.Length; i++) body[i] = Invert(response[i + 2]);
        ushort vendor = BinaryPrimitives.ReadUInt16LittleEndian(body);
        ushort model = BinaryPrimitives.ReadUInt16LittleEndian(body[2..]);
        uint firmware = BinaryPrimitives.ReadUInt32LittleEndian(body[4..]);
        if (vendor is not (0x5253 or 0x3837))
        {
            // Only call it an echo when the complete response matches the request, including padding.
            bool echo = response.Length >= LogicalLength && response[2..LogicalLength].IndexOfAnyExcept((byte)0xFF) < 0
                && response[LogicalLength..].IndexOfAnyExcept((byte)0) < 0;
            error = echo ? BatteryResponseError.EchoedRequest : BatteryResponseError.InvalidVendor;
            return null;
        }
        if (model == 0) { error = BatteryResponseError.InvalidModel; return null; }
        if (body[9] > 100) { error = BatteryResponseError.PercentageOutOfRange; return null; }
        // Status/charging semantics are unverified for this model. A parsed value is not proof of freshness.
        return new BatteryReading(vendor, model, firmware, body[8], body[9], body[10]);
    }
}

public sealed record BatteryReading(ushort VendorId, ushort ModelId, uint Firmware,
    byte Flags, byte Percent, byte ChargeRaw);

