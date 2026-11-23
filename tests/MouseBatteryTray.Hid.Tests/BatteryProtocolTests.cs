using MouseBatteryTray.Hid;

namespace MouseBatteryTray.Hid.Tests;

public class BatteryProtocolTests
{
    private static byte[] Response(byte percent = 42, byte flags = 9, byte charge = 0)
    {
        byte[] body = [0x53, 0x52, 0x31, 0x00, 1, 2, 3, 4, flags, percent, charge];
        var result = new byte[65];
        result[0] = 0x11;
        result[1] = 0xF9;
        for (int i = 0; i < body.Length; i++) result[i + 2] = BatteryProtocol.Invert(body[i]);
        return result;
    }

    [Fact] public void RequestHasInvertedPayloadAndZeroPadding()
    {
        var request = BatteryProtocol.CreateRequest(65);
        Assert.Equal(0x11, request[0]); Assert.Equal(0xF9, request[1]);
        Assert.All(request[2..21], value => Assert.Equal(0xFF, value));
        Assert.All(request[21..], value => Assert.Equal(0, value));
        Assert.Throws<ArgumentOutOfRangeException>(() => BatteryProtocol.CreateRequest(20));
        Assert.Equal(0xA5, BatteryProtocol.Invert(BatteryProtocol.Invert(0xA5)));
    }
    [Theory] [InlineData(0)] [InlineData(100)] [InlineData(42)]
    public void ParsesValidBoundaries(byte value)
    {
        var r = BatteryProtocol.Parse(Response(value));
        Assert.NotNull(r); Assert.Equal(value, r.Percent); Assert.Equal(0x5253, r.VendorId);
        Assert.Equal(0x31, r.ModelId); Assert.Equal(0x04030201u, r.Firmware);
    }
    [Fact] public void UnknownFlagsArePreserved()
    {
        var r = BatteryProtocol.Parse(Response(flags: 0xFF, charge: 0x7F));
        Assert.NotNull(r); Assert.Equal(0xFF, r.Flags); Assert.Equal(0x7F, r.ChargeRaw);
    }
    [Fact] public void RejectsBadResponses()
    {
        Assert.Null(BatteryProtocol.Parse([]));
        Assert.Null(BatteryProtocol.Parse(new byte[65]));
        Assert.Null(BatteryProtocol.Parse(BatteryProtocol.CreateRequest(65)));
        Assert.Null(BatteryProtocol.Parse(Response(101)));
        var wrongId = Response(); wrongId[0] = 0x12; Assert.Null(BatteryProtocol.Parse(wrongId));
        var wrongCommand = Response(); wrongCommand[1] = 0xFA; Assert.Null(BatteryProtocol.Parse(wrongCommand));
        var wrongVendor = Response(); wrongVendor[2] = 0xFF; wrongVendor[3] = 0xFF; Assert.Null(BatteryProtocol.Parse(wrongVendor));
        var wrongModel = Response(); wrongModel[4] = 0xFF; wrongModel[5] = 0xFF; Assert.Null(BatteryProtocol.Parse(wrongModel));
        Assert.Null(BatteryProtocol.Parse(Response()[..12]));
    }

    [Fact] public void IdenticalValidResponsesCannotProveFreshness()
    {
        var cached = Response();
        Assert.Equal(BatteryProtocol.Parse(cached), BatteryProtocol.Parse(cached));
        Assert.True(BatteryProtocol.IsLikelyWirelessOnline(BatteryProtocol.Parse(cached)!));
        // The observed format has no verified sequence number or timestamp to reject this cache.
    }

    [Theory]
    [InlineData(0x09, true)]
    [InlineData(0x01, false)]
    [InlineData(0x0E, false)]
    [InlineData(0x0F, false)]
    [InlineData(0xFF, false)]
    public void RequiresKnownActiveWirelessFlags(byte flags, bool expected)
    {
        var reading = BatteryProtocol.Parse(Response(flags: flags));
        Assert.NotNull(reading);
        Assert.Equal(expected, BatteryProtocol.IsLikelyWirelessOnline(reading));
    }
}
