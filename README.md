# L7-BatteryInfo

A lightweight Windows tray utility for monitoring the battery of the MCHOSE L7 Ultra+ wireless mouse.

![Windows 10](https://img.shields.io/badge/Windows-10%20tested-0078D4) ![.NET 10](https://img.shields.io/badge/C%23%20%2F%20.NET-10-512BD4) ![Version 0.1.0](https://img.shields.io/badge/version-0.1.0-4ADE80)

## Download

**[Download L7-BatteryInfo v0.1.0](https://github.com/MateFoxer/MCHOSE-L7-Battery-Tray/releases/tag/v0.1.0)**

Choose the Windows x64 portable executable from the release. It is self-contained and does not require a .NET installation.

## Features

- Battery percentage in the notification area, with a manual refresh action.
- Automatic monitoring, normally every 60 seconds, and recovery attempts after sleep or receiver changes.
- Optional low-battery alerts, with the preference saved locally.
- Optional startup at Windows sign-in and light/dark theme support.

## Quick installation

1. Download the Windows x64 executable from the release page.
2. Move it to a stable location, such as `%LOCALAPPDATA%\Programs\L7-BatteryInfo\L7-BatteryInfo.exe`.
3. Connect the mouse's 2.4 GHz receiver and run the executable. The icon may appear under the taskbar's hidden icons.
4. Right-click the tray icon to refresh the reading or exit.

To update the executable, exit the application first, replace the file at the same location, and start it again.

## Supported hardware and compatibility

The project targets the **MCHOSE L7 Ultra+** using its 2.4 GHz USB receiver (VID `5253`, PID `1020`). Other models and receivers are not verified. Windows 10 has been tested with this setup; Windows 11 has not been manually verified.

This is an independent project and is not affiliated with MCHOSE.

## Start with Windows

In the tray menu, use **Start with Windows** to enable or disable startup for the current user. The option is available for a standalone executable in a stable location; it is hidden for development, build, and temporary paths. No administrator privileges are normally required.

The application will not silently replace a conflicting startup entry. If a legacy MouseBatteryTray entry exists, disable it before enabling the new one.

## Known limitations

- The battery protocol is experimental. A receiver may return a cached but valid-looking response, so neither freshness nor exact accuracy can be guaranteed. Compare readings with MCHOSE HUB when needed.
- A sleeping mouse or an unavailable receiver may prevent a reading. Native HID calls can block, and recovery after sleep or reconnect still needs further hardware testing.
- Windows controls whether low-battery notifications are shown. Unrelated HID device changes may temporarily invalidate a reading.
- Manual release testing is incomplete; see the [release QA checklist](docs/release-qa.md).

## Build from source

Install the .NET 10 SDK on Windows, then run:

```powershell
dotnet restore
dotnet build
dotnet run --project src/MouseBatteryTray
```

See [development documentation](docs/development.md) for tests, portable publishing, HID discovery, and the experimental battery probe.

## Development and licensing

Developed by MateFoxer. This repository does not currently include a project license; do not assume permission to redistribute or reuse its contents. Third-party dependencies and assets may have their own terms.
