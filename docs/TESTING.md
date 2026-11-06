# Validation and release gates

## Automated suite

Run `TEST.cmd` from a Windows command prompt. Failures return a non-zero exit code. GitHub Actions runs the same command and retains the complete test log and UI rendering samples. Files under `artifacts/screenshots` contain only synthetic data.

Coverage:

- Percent validation, colour thresholds, copies and safe provider URLs.
- Exact reset boundary, independent active/weekly expiry, missing dates, clock rollback and countdown formatting.
- Manual XML persistence, atomic replacement, backups, corrupt files, old schema, prohibited DTDs, locked-target failure and temporary-file cleanup.
- Alert threshold, independent weekly warning, duplicate suppression, re-arming on a new period, disabled/demo/expired suppression.
- Tray icon rendering from 0 to 100%, unknown state and 500 icon creations with a bounded GDI count.
- Popup rendering at logical 100%, 150% and 200%, native button bounds/actions and accessible usage descriptions.
- Settings source enablement, provider change isolation and saving.
- Real NotifyIcon/menu construction, refresh and resource disposal with isolated readings.

Logical scale rendering is not equivalent to testing real monitors. Automated form bitmaps are reviewed for visible clipping but cannot establish native shell interaction correctness.

## Pending physical Windows 11 checks

These require a real Windows desktop and are not marked complete by CI:

- [ ] Icon readability at 100%, 125%, 150% and 200% scaling, including 0, 8, 72, 100 and `?`.
- [ ] Weekly strip recognisable at the actual taskbar size; icon visible outside overflow.
- [ ] Single-click opens and closes; double-click opens one service page without leaving a popup.
- [ ] Outside click and Escape dismiss; Tab/Enter reach all actions; Narrator announces readings and actions.
- [ ] Settings fit at supported scaling and Windows text-size settings; Save and Cancel behave correctly.
- [ ] Popup placement on secondary/negative-coordinate displays and taskbar on any supported edge.
- [ ] Mixed-DPI displays behave acceptably after restart on the target monitor.
- [ ] Explorer restart, sleep/resume and clock/time-zone changes do not leave misleading state or orphan icons.
- [ ] Start with Windows persists across sign-out/sign-in; disabling it and uninstall leave no Metraik startup entry.
- [ ] Notifications respect Windows Focus Assist; demo and expired readings never trigger warnings.
- [ ] Migration from the original ZIP works with real saved manual settings and the earlier process already running.
- [ ] Running from a path containing spaces works; a read-only installation folder reports build failure clearly.

Until these pass, label downloads as previews, do not call them fully tested, and do not publish a stable release.
