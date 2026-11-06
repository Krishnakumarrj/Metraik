# Metraik 0.2 preview plan

The first milestone preserves the agreed layout: active number plus weekly strip in one tray icon; one active ring plus a secondary weekly bar in the popup.

| Priority | Change | Acceptance |
|---|---|---|
| P0 | Rebrand executable, UI, startup value and data directory | Metraik visible throughout; legacy settings readable |
| P0 | Avoid misleading readings | Demo/manual labels; expired periods unknown; no invented resets |
| P0 | Preserve settings | Atomic replacement, backup recovery, no destructive corruption fallback |
| P1 | Use accessible popup actions | Native focusable buttons, Escape, usage summary description |
| P1 | Correct tray colours and resource ownership | Independent colours, boundary renders, repeated icon creation without GDI growth |
| P1 | Add manual provider labels | Safe fixed browser URLs; switching providers clears the old manual snapshot |
| P1 | Add optional allowance alerts | Configurable threshold; no demo/expired/repeated alerts |
| P1 | Automate Windows validation | Strict compile, regression suite, rendered artifacts, packaged preview after pass |
| Release gate | Verify Windows 11 shell and device behaviour | Complete the pending physical checks in TESTING.md |

A validation branch carries source needed by the Windows runner. A green automated result permits a preview PR, not a claim of full device validation. No stable release is published until the device checks pass.
