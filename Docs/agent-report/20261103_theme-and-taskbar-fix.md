# Order Report: Real-Time Theme Switching and 1-Click Taskbar Custom Icon Pinning

## 1. Objectives Completed
1. **Real-Time Dynamic Theme Switching (No Restart Dialog)**:
   - Replaced static frozen brushes with mutable `SolidColorBrush` instances (`Bg`, `Panel`, `Fg`, `Sub`, `Accent`, `Hover`).
   - Removed the blocking popup ("Theme applies on next start").
   - In `MainWindow`, selecting Light, Dark, or System dynamically calls `Theme.Apply(newTheme)`.
   - In `DataTemplate`, bound template elements to `Theme.Fg` and `Theme.Sub` via `PropertyPath` to maintain brush mutability across WPF template sealing.
   - Tested real-time color mutation on live theme changes: Verified passing.

2. **Distinct AppUserModelID & Custom Icon Shortcuts**:
   - Implemented `IShellLinkW`, `IPropertyStore`, and `IPersistFile` COM interfaces.
   - Set `System.AppUserModel.ID` (`PKEY_AppUserModel_ID` `{9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3}, 5`) explicitly on all created `.lnk` shortcuts to `"XAppGroup.Group." + g.Id`.
   - Set `IconLocation` on the shortcut to `g.IconPath` when a custom icon is chosen (converting `.png` / images to `.ico` as needed, or targeting `.ico`/`.exe`), and only falling back to composite generation when no custom icon is set.
   - In `App.Begin()`, registered `SetCurrentProcessExplicitAppUserModelID("XAppGroup.Group." + g.Id)` upon `--launch <id>` so Windows taskbar groups the popup under its specific custom-pinned identity rather than collapsing it into the main manager app.

3. **1-Click Native Taskbar Pinning**:
   - Added `TaskbarHelper` using Windows Shell `CLSID_TaskbandPin` and `IPinnedList3.Modify` (slot 16) for programmatic 1-click pinning and unpinning.
   - Deleted groups are automatically unpinned from the taskbar and their shortcut files purged from both Desktop and Shortcuts directories.

## 2. Test Verification
- All 30 automated headless checks in `--selftest` passed:
  - `PASS Real-time theme brush update without restart`
  - `PASS Shortcut AppUserModelID assigned and persistent`
  - `PASS Shortcut uses custom IconPath when set`
  - `PASS Taskbar: shell verbs queryable (read-only)`
  - `PASS Delete cleanup removes Desktop + Shortcuts .lnk`
- Zero compiler errors or warnings during `dotnet build src/XAppGroup.csproj -c Release -o out`.

