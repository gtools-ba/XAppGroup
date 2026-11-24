# Order: Display group icons + taskbar button (simplest path)

## Context
Project: `XAppGroup` — a WPF + WindowsForms .NET 10 desktop app at `D:\Programing\To-Do\XAppGroup`.
Code lives in `src/`. Build: `dotnet build src/XAppGroup.csproj -c Release`.

## The ask
Make the app display each group's icon in three places, and add a "Add group to taskbar" button:

1. **Main window** — beside each group name in the list (currently shows only name + app count).
2. **Popup window** (flyout at cursor) — show the group icon beside the group name header.
3. **Taskbar integration** — add a button "Add group to taskbar" that creates a Windows taskbar group for the selected apps (creates an `explorer.exe` command that groups the apps).

### What must change
| File | Change |
|---|---|
| `Core.cs` | Expose the generated `.ico` path from `BuildGroupIcon` as a get-only property, and add an optional `IconPath` to `GroupData` so a user can set a custom icon file. |
| `MainWindow.xaml` / `.cs` | Add an `Image` control beside each group item in the `ListBox` that binds to the group's icon. |
| `PopupWindow.xaml` / `.cs` | Add an `Image` control beside the group name `TextBlock` that binds to the same icon. |
| `MainWindow.xaml` | Add a "Add group to taskbar" button (below the group list) that runs a PowerShell/command to pin the group to the taskbar. |

### Constraints
- Do NOT change the existing `BuildGroupIcon` logic — it already generates a valid 256×256 `.ico` with up to 4 app icons.
- Do NOT modify the `.lnk` shortcut creation (keep `ShortcutHelper.Create` as‑is).
- The taskbar button may use `Process.Start` with `explorer.exe` arguments or a PowerShell snippet — just needs to *attempt* the pin; if it fails, show a message box with the error details.

### Acceptance criteria
- **Report:** Markdown report in `Docs/agent-report/20261103_display-icons-and-taskbar.md`.
- **Main window** — each group in the list shows a 32×32 icon preview beside the group name; clicking a group still selects it.
- **Popup window** — the flyout at cursor shows the group icon beside the group name header; the icon matches the one shown in main window.
- **Taskbar button** — clicking "Add group to taskbar" either (a) successfully creates a taskbar group, or (b) shows a message box with the error details (so the user knows why it didn't work).
- **No regressions** — existing "Create Shortcut" button still works, existing group operations (new/edit/delete) still work.

### How to verify
1. Build: `dotnet build src/XAppGroup.csproj -c Release` — must succeed.
2. Run: `XAppGroup.exe` — open main window, verify icons appear.
3. Open a group's popup (`Test` button or double‑click a group) — verify icon appears.
4. Click "Add group to taskbar" — observe result (success or error message).
5. Run `XAppGroup.exe --selftest` — all self‑tests must still pass.

### How to verify (Coder)
The Coder will show the exact PowerShell/command used for the taskbar attempt, and the final report will include the build/test commands and results.