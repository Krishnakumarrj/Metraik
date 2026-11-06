# Metraik

**All your AI limits. One glance.**

A lightweight Windows system-tray allowance monitor. The main number shows active allowance remaining; the thin strip underneath shows weekly allowance. Click for a dark popup with one active ring and one weekly bar.

## Preview status

This is a **manual/demo preview**, not a live account integration. ChatGPT, Claude, Gemini, Cursor and Copilot are selectable labels with browser shortcuts. Metraik tracks **one selected service at a time**; switching service starts in Demo mode and replaces the previous snapshot when saved. It does not yet keep simultaneous provider profiles.

Numbers must be entered by you or generated in Demo mode. Refresh redraws the display and countdowns; it never fetches usage. Allowance labels are generic: do not assume every service offers both an active and weekly limit. Use Demo mode if your account does not expose suitable readings.

## Run

1. Download the `Metraik-Windows-preview` artifact from a successful [Windows build](../../actions), or download this source repository as a ZIP.
2. Extract it into a permanent folder.
3. Double-click `RUN_ME.cmd`. Source packages compile `Metraik.exe` first using the .NET Framework compiler on Windows. The tested binary artifact already includes the executable.
4. Look under the taskbar's `^` hidden-icons menu if necessary. Windows controls placement; show Metraik using the taskbar's system-tray settings.
5. Right-click the icon and choose **Edit usage and settings**. Select a service, choose Manual, and enter the remaining percentages and future reset dates.

Windows 10/11 with .NET Framework 4.8 is the target. No administrator rights or external packages are required. Keep the folder in place if enabling Start with Windows. After changing source files, exit Metraik and run `BUILD.cmd` again; the launcher does not detect source changes automatically.

## Behaviour

| Action or state | Result |
|---|---|
| Single left-click | Toggle the popup after the Windows double-click interval |
| Double-click | Open the selected service in your default browser |
| Right-click | Panel, settings, display refresh, service shortcut, startup or exit |
| Escape / click elsewhere | Dismiss the popup |
| Tab / Enter in popup | Focus and activate the native action buttons |
| Active reset passes | Main number becomes `?`; a new manual reading is required |
| Weekly reset passes | Weekly bar becomes an empty grey track; popup says Unknown |
| Manual clock moves behind the entry time | Readings become unknown until corrected |
| Alerts enabled | Notify at/below a chosen threshold, once per allowance period per app session |

The active number/ring and weekly bar use independent colours: green at 50–100%, amber at 21–49%, red at 0–20%. A grey weekly track can represent either 0% or unknown; hover or open the popup to distinguish them. Demo/manual source labels appear in the popup and tooltip. Manual readings are snapshots and can become outdated before the entered reset time; the popup shows when they were entered.

## Settings, updates and removal

Settings live at `%APPDATA%\Metraik\settings.xml`. Saving atomically replaces the XML and keeps the previous save as `settings.xml.bak`. If the primary file is corrupt, the app tries the backup and shows a warning. If neither is readable, it shows labelled Demo data and leaves both files intact.

If no Metraik settings exist, it can read the earlier monitor's `%APPDATA%\ChatGPTTaskbarMonitor\settings.xml`. The old file is left intact. On the first save, settings go to the new Metraik folder. Disable startup in the old application before updating: old startup entries are not silently removed. Old and new versions share a single-instance guard.

Start with Windows is opt-in. It stores a quoted executable path under the `Metraik` value in `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. To uninstall, untick Start with Windows, choose Exit, and delete the application folder. Optionally remove the Metraik settings folder too.

## Privacy

The app makes no network requests, gathers no telemetry and asks for no credentials. Opening a service explicitly launches its public website in your browser. No cookies, API keys or session tokens are read or stored. Account integration is future work and must be implemented per provider; service selection alone is not a connection.

## Build and test

Run `BUILD.cmd` to compile with warnings treated as errors. Run `TEST.cmd` for the Windows test suite. It covers countdown/expiry boundaries, independent weekly state, validation, legacy XML, atomic saves and failure recovery, alert suppression, provider changes, native popup actions, scaled rendering with a cap to fit the current display, icon handle ownership and creation/disposal of a real notification icon. Tests use temporary settings and do not register startup or launch service URLs.

GitHub Actions runs the build and tests on Windows Server 2022, saves rendered UI images and a test log, then packages a preview only after the suite succeeds. This verifies Windows compilation and automated behaviour; it does **not** establish Windows 11 physical-taskbar readability, Explorer restart recovery, startup after sign-in, assistive-technology support or mixed-DPI monitor behaviour. See [testing](docs/TESTING.md) for the remaining release checks. The app is system-DPI aware; moving between displays with different scaling may require a restart.

## Roadmap

- Independent saved profiles and simultaneous service monitoring.
- Provider-specific allowance schemas, including services without weekly limits.
- Official, supported usage sources where available, with explicit authorisation.
- Windows 11 physical-device verification, per-monitor DPI, signed packaging and installer.

This repository has no licence grant yet; a licence can be chosen separately.
