# MCHOSE-L7-Battery-Tray

MouseBatteryTray is a learning project for a lightweight Windows mouse-battery tray application. Phase 1 only discovers HID interfaces; the application, tray icon and battery polling are not implemented.

## Requirements

Windows 10 or 11 and the .NET 10 SDK. Connect the 2.4 GHz receiver before scanning. Administrator privileges should not normally be needed; individual descriptors may be unavailable.

## Build and scan

```powershell
dotnet restore
dotnet build
dotnet run --project tools/MouseBatteryTray.DeviceScanner
# Show all HID interfaces:
dotnet run --project tools/MouseBatteryTray.DeviceScanner -- --all
```

Candidates match VID `0x5253` or `0x3837`, or contain `MCHOSE` in an available manufacturer/product string. These are discovery hints, not proof of the mouse model or compatibility. The L7 Ultra+ battery protocol is not verified. No PID is assumed. One receiver can expose multiple HID interfaces. Interface numbers and usages may be unavailable, and report lengths include the report ID byte. The scanner does not print serial numbers or full device paths. Review output before sharing, especially when using `--all`.

The scanner enumerates HID interfaces and reads descriptor information only; it sends no HID output or feature reports and does not change device settings. HidSharp may enumerate non-USB HID transports too, depending on the operating system; the scanner does not claim to verify the transport.
