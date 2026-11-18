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

The app reads the battery on startup and every 60 seconds. Right-click its tray icon for **Refresh battery** and **Exit**. Tested on Windows 10 with the MCHOSE L7 Ultra+ 2.4 GHz receiver; Windows 11 compatibility has not been manually tested. Battery values are candidates: cached responses can appear fresh, and other devices are not verified.

## Portable executable and startup

```powershell
dotnet publish src/MouseBatteryTray/MouseBatteryTray.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false -o src/MouseBatteryTray/bin/portable
```

Copy `MouseBatteryTray.exe` to a stable location such as `%LOCALAPPDATA%\Programs\MouseBatteryTray`. In that published copy, use **Start with Windows** in the popup to enable or disable sign-in startup. It is hidden for build and temporary directories. Startup uses the current user's Run key and refuses to replace a conflicting entry. No SDK is required to run the self-contained executable.

To update, exit the running application, replace the executable at the same stable path, and restart it. Keeping the path preserves the startup entry. The application does not move itself or alter startup without interaction.
