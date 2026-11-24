# Report: fix-group-delete-icons-display
Order: Docs/agent-orders/20261103_fix-group-delete-icons-display.md
Date: 2026-11-03

This replaces the earlier report for this order. That report overstated the work: the Desktop `.lnk` was never deleted, there was no per-app icon in the lists, and the manual tests it listed were never run.

## What was done (before → after)
1. **Delete group**
   - Before: only `%APPDATA%\XAppGroup\Shortcuts\<name>.lnk` was removed. The Desktop `.lnk` made by "Create Shortcut" stayed, and there was no message.
   - After: `Launcher.DeleteShortcut(g)` now scans the app's Shortcuts folder, the user Desktop, the common Desktop and the taskbar "User Pinned" folder. It deletes every `.lnk` whose arguments are `--launch <group id>`, so renamed groups are handled too. It returns the list of removed files. A message box lists them and says "if still pinned to the taskbar, remove it manually (right-click → Unpin)".
2. **Edit group icon**
   - Before: an "Icon..." button set `IconPath` immediately, even if the user pressed Cancel. It was `.ico` only, had no preview, and `Save()` had a no-op line.
   - After:
     - The Edit window shows a 32×32 preview and the text "Group icon: automatic" or the file name.
     - "Icon..." accepts `.ico`, `.png`, `.exe` and `.dll`. It rejects files with no usable icon.
     - "Reset icon" goes back to the automatic icon.
     - `IconPath` is applied only on Save (Cancel discards it), saved to `appgroups.json`, and shown in the Main and Popup windows.
3. **App icons in lists**
   - Before: the Edit list was text only, and the Main list showed only the group icon.
   - After: the Edit window list shows a 32×32 icon beside each app name. The Main list shows a strip of up to 6 small (20px) app icons under each group. This is a new `GroupData.AppIcons` property, recomputed on every read, so it follows add/remove (the list rebinds after Edit). The Popup already showed app icons.
4. **Main screen reflects changes:** the group icon and app strip show in the Main list, the Popup header icon uses the same source, and "Add group to taskbar" is unchanged.
- Not touched: `ShortcutHelper.Create`, `BuildGroupIcon`, the "Create Shortcut" button.

## Files changed
- src/Core.cs — `DeleteShortcut` rewritten (scan + return list); `GroupData.AppIcons`; `Icons.FromFile` (ico/png/exe/dll); `GroupIcon` uses it.
- src/Windows.cs — Edit window icon preview, "Reset icon", deferred `IconPath`, per-app icons in the Edit list; Main item template app-icon strip; Delete message box.
- src/SelfTest.cs — 6 new checks (the suite now has 21).

## How to verify
- `dotnet build src/XAppGroup.csproj -c Release` → PASS (0 warnings, 0 errors).
- `XAppGroup.exe --selftest` (unsandboxed, `XAPPGROUP_DATA` set to a temp folder) → exit 0, 21/21 PASS. New checks:

| Check | Result |
|---|---|
| Delete cleanup removes Desktop + Shortcuts .lnk (2 files created, both removed) | PASS |
| IconPath round-trips via appgroups.json | PASS |
| Icon from .exe supported | PASS |
| Edit window: icon per app + preview | PASS |
| Main window list: per-app icons | PASS |
| AppIcons follows Paths changes | PASS |

- `out/` was cleared and rebuilt, so `out/XAppGroup.exe` is the new build (no running instance was blocking it).

## Open questions / decisions needed
- No interactive desktop was available, so clicking Icon.../Delete and the file dialog and message boxes were not tried by hand. Please do a quick manual run.
- Taskbar unpin cannot be done programmatically. The delete tries to remove the pinned `.lnk` from the User Pinned folder, but the button may stay visible until Explorer restarts. The message says to unpin manually.
- A custom `.exe`/`.dll` icon is shown in the app UI only. `ShortcutHelper.Create` still uses the generated icon, as the order required it not to change.

## Notes
- The self-test creates and removes a temporary `ZZ Delete Test` shortcut on the real Desktop.
