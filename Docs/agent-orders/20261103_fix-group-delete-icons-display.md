# Order: Fix group delete shortcuts, icon editing, icon display, and main screen effects

## Context
Project: `XAppGroup` — a WPF + WindowsForms .NET 10 desktop app at `D:\Programing\To-Do\XAppGroup`.
Code lives in `src/`. Build: `dotnet build src/XAppGroup.csproj -c Release`.

## The ask
Verify and fix four issues:

1. **Delete group does not remove desktop shortcut or unpinned from taskbar**  
   When a group is deleted, any `.lnk` shortcut created on the desktop and any taskbar pin should be removed.

2. **Edit group missing button to set a group icon**  
   The Edit window has no control to select/set a custom icon (`.ico`/`.exe` file) for the group. The group icon should be persistently settable.

3. **When apps are added to a group, icons don't show in the list — only app names**  
   The main window and popup list apps by name only; the extracted icons are not displayed beside each name, even though `IconSource` and the `BuildGroupIcon` infrastructure exists.

4. **Main screen does not reflect earlier changes**  
   The main window list, popup flyout, and taskbar button should already show icons (from the previous order), but apparently they don't. Need to ensure the data bindings are wired and the icon source is correctly computed.

### What must change

| File | Change |
|---|---|
| `Core.cs` | - Add `DeleteGroup` cleanup: remove the `.lnk` shortcut from `Store.ShortcutDir` (via `File.Delete` or `Shell.Application`).<br>- Ensure `IconPath` is persisted in `GroupData` and loaded on `Store.Load()`.<br>- Ensure `IconSource` is recomputed when `Paths` change (callback or refresh). |
| `Windows.cs` (EditWindow) | - Add an "Icon..." button / file picker to let the user select a custom `.ico`/`.exe` file for the group.<br>- Display the current `IconPath`/icon preview.<br>- When confirmed, update `GroupData.IconPath` and refresh the UI. |
| `Windows.cs` (MainWindow / Popup) | - Ensure each app item in the list box shows its extracted icon (32×32) beside the name.<br>- Verify `IconSource` for the group is refreshed when `Paths` collection changes (add/remove). |
| `Core.cs` / `Windows.cs` | - Ensure `BuildGroupIcon` / `Icons.GroupIcon()` is called when the group's icon needs updating (e.g., after edit, after adding/removing apps). |

### Constraints
- Do NOT change the existing `ShortcutHelper.Create` logic (it creates `.lnk` with the correct icon and arguments).
- Do NOT remove the "Create Shortcut" button functionality.
- The `IconPath` was introduced in the previous order — ensure it round-trips: set in Edit → saved in `appgroups.json` → displayed in Main/Popup.
- Delete must handle both desktop `.lnk` and taskbar pin cleanup (taskbar cleanup may just remove the `.lnk` and show a note that taskbar unpin is manual/Windows‑only).

### Acceptance criteria
- **Report:** Markdown report in `Docs/agent-report/20261103_fix-group-delete-icons-display.md`.
- **Delete group** — after deletion, the desktop `.lnk` file no longer exists; a message box confirms cleanup (or silently removes it). If taskbar pin was used, a note says "taskbar pin must be removed manually" (or attempts `Shell.Application` verb unpin).
- **Edit group icon** — the Edit window has a new "Icon..." button (or file picker). After selecting an icon file and confirming, the group displays that icon in Main window, Popup window, and group icon area. The `IconPath` is saved in `appgroups.json` and persists across app restart.
- **App icons in list** — each app item in the main window list and popup window shows a 32×32 icon extracted from the app executable, beside the app name. The list was already data‑bound to `IconSource`; the fix ensures the source is refreshed when paths change.
- **Main screen effects** — after the above fixes, the main window shows group icons, app icons in the list, and the popup header icon matches. The "Add group to taskbar" button still works.

### How to verify
1. Build: `dotnet build src/XAppGroup.csproj -c Release` → PASS.
2. Run: `XAppGroup.exe` — create a group with 2+ apps that have recognizable icons.
3. Edit the group → use the new "Icon..." button to select a custom icon (or leave default). Confirm the group now shows the custom icon in main window and popup.
4. Add/remove apps from the group → verify icons appear/disappear beside names in the list.
5. Delete the group → verify the desktop `.lnk` is gone (check `%USERPROFILE%\Desktop\`).
6. Run `XAppGroup.exe --selftest` → all 10 checks must still PASS.

### How to verify (Coder)
The Coder will show the exact commands used to build, the verification steps, and the final report. The report must include before/after behavior for each of the four issues.