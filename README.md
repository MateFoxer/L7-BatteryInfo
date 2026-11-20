# L7-BatteryInfo

**Battery monitoring, without the clutter.**

L7-BatteryInfo puts the battery percentage of your MCHOSE L7 Ultra+ in the Windows notification area, with a compact menu for refresh, alerts and startup settings.

![Windows 10 tested](https://img.shields.io/badge/Windows_10-tested-555555) ![.NET 10](https://img.shields.io/badge/.NET-10-555555) ![Version 0.1.0](https://img.shields.io/badge/version-0.1.0-398b62)

**[Download v0.1.0 for Windows x64](https://github.com/MateFoxer/MCHOSE-L7-Battery-Tray/releases/download/v0.1.0/L7-BatteryInfo-v0.1.0-win-x64.zip)** · [Release notes](https://github.com/MateFoxer/MCHOSE-L7-Battery-Tray/releases/tag/v0.1.0)

## At a glance

- **Always within reach** — a numeric battery indicator in the system tray and a manual refresh action.
- **Automatic monitoring** — requests a reading at startup and normally refreshes every 60 seconds.
- **Low-battery alerts** — optional Windows notifications at 20% and 10%.
- **At home on Windows** — optional startup at sign-in, light/dark theme adaptation, and recovery attempts after sleep or receiver reconnection.

## Installation

The portable release is self-contained for Windows x64; no separate .NET runtime installation is needed.

1. [Download the v0.1.0 ZIP](https://github.com/MateFoxer/MCHOSE-L7-Battery-Tray/releases/download/v0.1.0/L7-BatteryInfo-v0.1.0-win-x64.zip).
2. Extract it to a permanent folder, for example `%LOCALAPPDATA%\Programs\L7-BatteryInfo`.
3. Connect the 2.4 GHz receiver and run `L7-BatteryInfo.exe`.
4. Find the battery indicator in the Windows notification area; it may be under **Show hidden icons** (`^`).
5. Optionally, right-click the icon and enable **Start with Windows**.

To update, exit the app and replace the executable in the same folder before restarting it. The startup entry remains tied to its path.

## Compatibility

| | Supported setup |
| --- | --- |
| Mouse | MCHOSE L7 Ultra+ |
| Connection | Supported 2.4 GHz USB receiver (VID `5253`, PID `1020`) |
| Windows | Windows 10 tested; Windows 11 not manually verified |
| Other models | Not verified |

## Troubleshooting

- **The icon shows `?` or battery is unavailable:** Wake the mouse and ensure it is turned on in 2.4 GHz mode. Allow time for a reading, or right-click the icon and choose **Refresh battery**. A sleeping mouse, inaccessible receiver or invalid response can prevent a reading.
- **The receiver was disconnected:** Reconnect it and allow time for detection and a recovery attempt. If the value remains unavailable, try **Refresh battery**.
- **Start with Windows is missing:** Extract the standalone executable to a stable folder outside development, build and temporary directories, then run it from there. A conflicting legacy startup entry is not replaced automatically.
- **The tray icon is missing:** Open **Show hidden icons** (`^`) in the notification area.

## Limitations

The battery protocol is experimental. A receiver can return a cached, valid-looking response, so freshness and exact accuracy cannot be guaranteed. Windows controls notification delivery, and native HID calls can block. Sleep/wake and reconnect behavior need further hardware testing; see the [manual QA checklist](docs/release-qa.md).

## Development

Build instructions, tests, HID discovery, the battery probe and protocol limitations are in the [development guide](docs/development.md). Development requires the .NET 10 SDK.

---

Developed by **MateFoxer**. Independent project; not affiliated with MCHOSE. No project license is currently included; third-party dependencies and assets may have their own terms.
