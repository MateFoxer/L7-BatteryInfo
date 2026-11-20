# Development

Development requires the .NET 10 SDK. The self-contained portable executable does not. Administrator privileges should not normally be needed, though individual HID descriptors may be inaccessible.

## Build and tests

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project src/MouseBatteryTray
```

The automated tests use synthetic data and do not access the mouse.

## HID device scanner

Connect the 2.4 GHz receiver, then run:

```powershell
dotnet run --project tools/MouseBatteryTray.DeviceScanner
# Show all HID interfaces:
dotnet run --project tools/MouseBatteryTray.DeviceScanner -- --all
```

Candidates match VID `0x5253` or `0x3837`, or contain `MCHOSE` in an available manufacturer or product string. These are discovery hints, not proof of a model or compatibility. The scanner does not assume a PID; the probe and app target PID `0x1020`. A receiver can expose multiple HID interfaces. Interface numbers and usages may be unavailable, and report lengths include the report ID byte.

The scanner enumerates HID interfaces and reads descriptors only: it sends no output or feature reports and does not change device settings. HidSharp may enumerate non-USB HID transports depending on the operating system. Serial numbers and full device paths are not printed by default; review output before sharing, especially with `--all`.

## Battery probe

The experimental one-shot probe targets only the `5253:1020` receiver's `FF01:0001` collection. The L7 Ultra+ battery protocol remains unverified, and a valid-looking response can be cached. The dry run reads descriptors only:

```powershell
dotnet run --project tools/MouseBatteryTray.BatteryProbe -- --dry-run
```

To explicitly send one status request and read one feature response:

```powershell
dotnet run --project tools/MouseBatteryTray.BatteryProbe -- --read-battery
```

This active command does not retry or poll. HidSharp's synchronous feature calls may block and are not cancelled by a software timeout.

## Portable publish

```powershell
dotnet publish src/MouseBatteryTray/MouseBatteryTray.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false -o src/MouseBatteryTray/bin/portable
```

Copy the executable to a stable location before enabling startup; the app does not move itself or change startup settings without user interaction. Keep the executable at the same path when updating to preserve its startup entry.

## Recovery notes

Resume and HID device-list events invalidate the previous reading and trigger delayed recovery work. An unrelated HID event may also make a reading stale because the notification does not identify the changed device. Inaccessible descriptors are treated as unknown and retried conservatively. Cached receiver responses cannot prove freshness. Alert preferences and suppression state are stored locally.

See the [manual release QA checklist](release-qa.md); unchecked cases have not been completed.
