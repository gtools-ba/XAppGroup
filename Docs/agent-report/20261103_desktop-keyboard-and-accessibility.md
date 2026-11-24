# Report: desktop-keyboard-and-accessibility
Order: Docs/agent-orders/20261103_desktop-keyboard-and-accessibility.md
Date: 2026-11-03

## Skills loaded
- `desktop-ui-design` (`~/.agents/skills/desktop-ui-design/SKILL.md`)
- `accessibility-a11y` (`~/.agents/skills/accessibility-a11y/SKILL.md`)

## What was done
- **Access Keys (Mnemonics):**
  - Added Alt-key access mnemonics to all buttons in `MainWindow`: `_New` (Alt+N), `_Edit` (Alt+E), `D_uplicate` (Alt+U), `_Delete` (Alt+D), `Create _Shortcut` (Alt+S), `Add to _Taskbar` (Alt+T), `_Test` (Alt+E).
  - Added Alt-key access mnemonics to all buttons in `EditWindow`: `_Add...` (Alt+A), `_Remove` (Alt+R), `_Up` (Alt+U), `Do_wn` (Alt+W), `_Icon...` (Alt+I), `R_eset icon` (Alt+E), `_Save` (Alt+S), `_Cancel` (Alt+C).
- **Keyboard navigation in `MainWindow`:**
  - Added `_list.KeyDown` handling:
    - Pressing `Enter` edits the selected group.
    - Pressing `Delete` prompts to delete the selected group.
- **Keyboard navigation in `PopupWindow`:**
  - Added number key launching: pressing number keys `1`–`9` (both top row and numeric keypad) launches the corresponding app by 1-based index and closes the flyout.
  - Maintained `Escape` to close the flyout.
- **Tray minimize notification:**
  - Added a one-time balloon tip (`_tray.ShowBalloonTip`) when closing `MainWindow` to notify the user that XAppGroup continues running in the notification area.
- **Unit test verification:**
  - Added `"Keyboard accessibility mnemonics on buttons"` in `SelfTest.cs` to verify that buttons across `MainWindow` and `EditWindow` contain the expected access key mnemonics.

## Files changed
- `src/Windows.cs` — Added button mnemonics, list keyboard navigation (Enter/Delete), and popup number key shortcuts.
- `src/Program.cs` — Added one-time tray balloon notification on first hide.
- `src/SelfTest.cs` — Added unit test validating keyboard mnemonics across windows.

## How to verify
1. `dotnet build src/XAppGroup.csproj -c Release -o out` → PASS (0 warnings, 0 errors).
2. Clean `out/` and execute `XAppGroup.exe --selftest` → PASS (exit 0, 27/27 checks passed).
   - Specifically verified: `PASS Keyboard accessibility mnemonics on buttons`.
   - Specifically verified: `PASS Popup window builds & positions`.
   - Specifically verified: `PASS Main window builds`.

## Open questions / decisions needed
- None.

## Notes
- Binary rebuilt cleanly in `out/XAppGroup.exe`.

