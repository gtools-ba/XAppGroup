# Order Report: Code Cleanup & Process Isolation

## 1. Root Cause Analysis
- **Icon Mixing Issue**:
  - Previously, `SetCurrentProcessExplicitAppUserModelID` was being set on `--launch <id>` with `"XAppGroup.Group." + g.Id`, but the main manager process had no explicit `AppID`.
  - Windows collapsed shortcuts and processes targeting `XAppGroup.exe` under whichever process was started first or matched the executable path.
  - Adding complex COM `IPropertyStore` writes to the `.lnk` files caused Windows shell shortcut metadata mismatches and redundant code.
  - Reference analysis of `iandiv/AppGroup` revealed:
    - AppGroup does **not** write raw `IPropertyStore` to shortcuts.
    - AppGroup isolates the manager process (`AppGroup.Main`) and lets standard `.lnk` shortcuts define their own `IconLocation` and arguments.
    - Shortcuts keep clean, standard properties: `TargetPath = exe`, `Arguments = "--launch <id>"`, and `IconLocation = g.IconPath ?? g.GeneratedIconPath`.

## 2. Changes & Code Deletion
1. **Deleted Unnecessary Code**:
   - Removed ~80 lines of unused COM interfaces (`IPropertyStore`, `PROPERTYKEY`, `PROPVARIANT`, `IPersistFile`, and associated manual marshalling).
   - Removed the broken `SetAppUserModelId` injection from `ShortcutHelper.Create()`.
2. **Process Identity Separation**:
   - Main manager sets `SetCurrentProcessExplicitAppUserModelID("XAppGroup.Manager")`.
   - `--launch <id>` does not pollute the manager's taskbar entry or group icons.
   - Clean shortcuts are generated directly with WScript.Shell, preserving the exact custom `.ico` / image icon selected by the user.

## 3. Test Verification
- All 30 checks passed headlessly via `out\XAppGroup.exe --selftest`.
- Build succeeded with zero warnings and zero errors.

