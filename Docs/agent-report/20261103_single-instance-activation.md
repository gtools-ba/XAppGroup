# Report: single-instance-activation
Order: Docs/agent-orders/20261103_single-instance-activation.md
Date: 2026-11-03

## Skills loaded
- `desktop-ui-design` (`~/.agents/skills/desktop-ui-design/SKILL.md`)
- `wpf-best-practices` (`~/.agents/skills/wpf-best-practices/SKILL.md`)

## What was done
- **Single-instance window activation:**
  - Implemented cross-process event signaling using an auto-reset `EventWaitHandle` (`XAppGroup.ShowEvent`).
  - When a second instance of `XAppGroup.exe` is launched without arguments, it signals the event and exits cleanly instead of dying silently.
  - The running primary instance listens on a background thread. When signaled, it dispatches `ActivateMainWindow()` on the WPF UI thread.
  - `ActivateMainWindow()` ensures:
    - If the window was hidden (minimized to system tray), `_main.Show()` is called.
    - If the window was minimized, `_main.WindowState = WindowState.Normal` and Win32 `ShowWindow(hWnd, SW_RESTORE)` restore it.
    - Win32 `SetForegroundWindow(hWnd)` brings it above all other windows.
    - Focus and keyboard activation are restored.
  - Unified `ActivateMainWindow()` across tray icon left-click, tray context menu "Open XAppGroup", and second-instance activations.
- **Unit test verification:**
  - Added `"Single-instance signal wait handle works"` in `SelfTest.cs` to test cross-thread event handle creation and signaling.

## Files changed
- `src/Program.cs` — Added `EventWaitHandle` listener and Win32 `SetForegroundWindow` / `ShowWindow` activation helper.
- `src/SelfTest.cs` — Added test for event signaling.

## How to verify
1. `dotnet build src/XAppGroup.csproj -c Release -o out` → PASS (0 warnings, 0 errors).
2. Clean `out/` and execute `XAppGroup.exe --selftest` → PASS (exit 0, 25/25 checks passed).
   - Specifically verified: `PASS Single-instance signal wait handle works`.
   - Specifically verified: `PASS Launch app`.

## Open questions / decisions needed
- None.

## Notes
- Binary rebuilt cleanly in `out/XAppGroup.exe`.

