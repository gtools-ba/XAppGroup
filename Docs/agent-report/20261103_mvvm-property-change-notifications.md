# Report: mvvm-property-change-notifications
Order: Docs/agent-orders/20261103_mvvm-property-change-notifications.md
Date: 2026-11-03

## Skills loaded
- `wpf-best-practices` (`~/.agents/skills/wpf-best-practices/SKILL.md`)

## What was done
- **Implemented `INotifyPropertyChanged` on `GroupData`:**
  - Added standard WPF MVVM property change infrastructure (`SetProperty`, `OnPropertyChanged`) to `GroupData`.
  - Properties (`GroupName`, `GroupCol`, `ShowLabels`, `ShowOnTray`, `SortMode`, `IconPath`, `Paths`) now notify WPF bindings automatically on change.
  - `InvalidateIcons()` raises property change notifications for `IconSource`, `AppIcons`, `GeneratedIconPath`, and `Paths` to notify bound UI controls immediately without full list resets.
- **Switched `Settings.Groups` to `ObservableCollection<GroupData>`:**
  - `Settings.Groups` is now an `ObservableCollection<GroupData>`.
  - Adding or deleting groups automatically updates the `ListBox` through WPF's built-in collection change notification channels.
- **Cleaned up `MainWindow` data-binding:**
  - Removed the `_list.ItemsSource = null` / reset workaround in `MainWindow.Refresh()`.
  - Bound `_list.ItemsSource = _s.Groups` once in the constructor.
  - Adding a new group (`New`), editing a group in place (`Edit`), duplicating a group (`Duplicate`), or deleting a group (`Delete`) now modifies the collection / models directly, and the UI responds natively without visual flicker or losing scroll position.
- **Unit test verification:**
  - Added `"INotifyPropertyChanged on GroupData & ObservableCollection"` in `SelfTest.cs` to test event dispatching for property modifications and collection changes.

## Files changed
- `src/Core.cs` — Implemented `INotifyPropertyChanged`, `SetProperty`, and `ObservableCollection<GroupData>` on `Settings.Groups`.
- `src/Windows.cs` — Bound `_list.ItemsSource` once, removed `ItemsSource = null` rebinding workarounds.
- `src/SelfTest.cs` — Added test validating property change and collection change events.

## How to verify
1. `dotnet build src/XAppGroup.csproj -c Release -o out` → PASS (0 warnings, 0 errors).
2. Clean `out/` and execute `XAppGroup.exe --selftest` → PASS (exit 0, 24/24 checks passed).
   - Specifically verified: `PASS INotifyPropertyChanged on GroupData & ObservableCollection`.
   - Specifically verified: `PASS Edit window builds`.
   - Specifically verified: `PASS Main window builds`.

## Open questions / decisions needed
- None.

## Notes
- Binary rebuilt in `out/XAppGroup.exe`.

