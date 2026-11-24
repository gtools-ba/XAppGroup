# Order: Desktop Keyboard Shortcuts & Access Keys

## Context
Project: `XAppGroup` — a WPF + WindowsForms .NET 10 desktop app at `D:\Programing\To-Do\XAppGroup`.
Code lives in `src/`. Build: `dotnet build src/XAppGroup.csproj -c Release`.

## Required Skills
Load before starting:
- `desktop-ui-design` (`~/.agents/skills/desktop-ui-design/SKILL.md`)
- `accessibility-a11y` (`~/.agents/skills/accessibility-a11y/SKILL.md`)

## The ask
Bring the desktop application into compliance with Windows desktop keyboard and accessibility standards:
1. **Access Keys (Mnemonics):** Add standard access keys to buttons: `_New`, `_Edit`, `_Duplicate`, `_Delete`, `_Test`, `_Save`, `_Cancel` so users can operate the UI via Alt keys.
2. **List Keyboard Navigation:** In `MainWindow`:
   - Pressing `Enter` on a selected group opens its flyout popup or edit window.
   - Pressing `Delete` triggers the delete prompt for the selected group.
3. **Popup Keyboard Navigation:** In `PopupWindow`:
   - Number keys 1–9 or arrow navigation launch the corresponding app.
   - `Escape` closes the popup (already present, preserve it).
4. **Tray minimize feedback:** First time the window is closed to the notification tray, display a balloon tip or clear toast explaining that XAppGroup is running in the background.

## What must change
| File | Change |
|---|---|
| `src/Windows.cs` | Add access keys to `Theme.Btn` / buttons. Wire `KeyDown` handlers on `_list` for `Enter` and `Delete`. Add tray balloon notification on first hide. |
| `src/SelfTest.cs` | Verify access keys and keyboard command dispatch. |

## Constraints
- Keep standard WPF styles and layout intact.
- Rebuild cleanly into `out/`.
- All self-tests must pass.

## Acceptance criteria
- Buttons support Alt-key mnemonics.
- Group list responds to `Enter` and `Delete` keys.
- All self-tests pass.
- Report delivered to `Docs/agent-report/20261103_desktop-keyboard-and-accessibility.md`.

