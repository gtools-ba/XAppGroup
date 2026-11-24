# Order: MVVM Property Change Notifications & Clean List Binding

## Context
Project: `XAppGroup` — a WPF + WindowsForms .NET 10 desktop app at `D:\Programing\To-Do\XAppGroup`.
Code lives in `src/`. Build: `dotnet build src/XAppGroup.csproj -c Release`.

## Required Skills
Load before starting:
- `wpf-best-practices` (`~/.agents/skills/wpf-best-practices/SKILL.md`)

## The ask
`GroupData` does not implement `INotifyPropertyChanged`. Whenever a group is edited or renamed, `MainWindow` has to force a full reset of the list via `_list.ItemsSource = null; _list.ItemsSource = _s.Groups;`, destroying and recreating all visual containers, losing scroll position, and flickering.

Fix this so:
1. `GroupData` implements `INotifyPropertyChanged`.
2. Property setters for `GroupName`, `GroupCol`, `ShowLabels`, `ShowOnTray`, `SortMode`, `IconPath`, and icon caches raise `PropertyChanged`.
3. `Settings.Groups` is represented as an `ObservableCollection<GroupData>` (or properly bound) so adding/removing groups notifies the UI without crude `ItemsSource = null` rebinding.

## What must change
| File | Change |
|---|---|
| `src/Core.cs` | Make `GroupData` implement `INotifyPropertyChanged`. Add `SetProperty` helper and raise notifications on property updates. Change `Settings.Groups` to `ObservableCollection<GroupData>`. |
| `src/Windows.cs` | Remove `_list.ItemsSource = null` resets in `MainWindow.Refresh()`. Rely on collection and property change notifications. |
| `src/SelfTest.cs` | Add test verifying `PropertyChanged` events fire on `GroupName` and `IconPath` modification. |

## Constraints
- JSON serialization and deserialization of `Settings` and `GroupData` must remain backwards compatible.
- All self-tests must pass.
- Output lands in `out/`.

## Acceptance criteria
- Editing group properties updates the main list items automatically via WPF data-binding.
- No `_list.ItemsSource = null` workarounds in `MainWindow`.
- Self-test suite passes cleanly.
- Report delivered to `Docs/agent-report/20261103_mvvm-property-change-notifications.md`.

