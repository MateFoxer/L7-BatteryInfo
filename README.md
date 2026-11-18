# MCHOSE-L7-Battery-Tray

MouseBatteryTray is a learning project for a lightweight Windows mouse-battery tray application. The Windows tray application provides an experimental battery monitor.

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

## Battery probe

The experimental one-shot probe targets only the `5253:1020` receiver's `FF01:0001` collection. The battery protocol has not been verified on the L7 Ultra+; a valid-looking response can be stale. The dry run reads descriptors only:

```powershell
dotnet run --project tools/MouseBatteryTray.BatteryProbe -- --dry-run
```

To explicitly send one status request and read one feature response:

```powershell
dotnet run --project tools/MouseBatteryTray.BatteryProbe -- --read-battery
```

The command does not retry or poll. HidSharp's synchronous feature calls may block and are not cancelled by a software timeout. Run `dotnet test` for synthetic tests, which do not access hardware.

## Windows tray app

```powershell
dotnet run --project src/MouseBatteryTray
```

The application starts in the notification area without a window. Right-click its icon and choose **Enable automatic monitoring** to begin 60-second polling; monitoring is off at every launch. **Refresh now** performs one read even when monitoring is off. Use **Exit** to close it. If the icon is hidden, open the taskbar's hidden-icons menu.

The reported percentage is a candidate until checked against MCHOSE HUB and tested with the mouse asleep, disconnected and reconnected. A recent successful response is not proof of fresh device data: the protocol has no verified transaction identifier. The icon turns muted and the tooltip says stale after a failed read or 120 seconds without a successful read. Last-known values and timestamps are kept only in memory. Synchronous native HID calls may block; disabling monitoring stops future requests but cannot interrupt an operation already in progress.
