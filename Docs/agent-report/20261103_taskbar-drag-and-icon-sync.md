# Order Report: Taskbar Pinning UX, Drag-to-Pin, and App Icon

## 1. Summary of Changes
1. **Application Icon**:
   - Converted `Docs/XAppGroup.png` into `src/XAppGroup.ico` and copied to `out/XAppGroup.ico`.
   - Embedded `<ApplicationIcon>XAppGroup.ico</ApplicationIcon>` in `src/XAppGroup.csproj`.
   - Window and taskbar icon dynamically set to `XAppGroup.ico`.

2. **Drag-to-Taskbar Pinning**:
   - Added OLE Drag & Drop support to `_list` in `MainWindow` using `PreviewMouseLeftButtonDown` + `PreviewMouseMove`.
   - Initiates `DragDrop.DoDragDrop` with `DataFormats.FileDrop` linking the prepared `.lnk` shortcut.
   - Users can drag any group card directly onto the Windows Taskbar to pin it natively.

3. **Pinned Taskbar Synchronization**:
   - Implemented `TaskbarHelper.SyncPinnedShortcut()` targeting `%APPDATA%\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar`.
   - When saving group edits (name, icon, or apps), existing pinned taskbar shortcuts are updated in-place with the new icon and arguments.
   - Triggered `SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH)` and `InvalidateRect("Shell_TrayWnd")` to flush Windows Shell cache immediately.

4. **Taskbar-Aware Snapping**:
   - Updated `PopupWindow.Position()` to detect monitor work area and taskbar edge, aligning the flyout directly above bottom taskbars or below top taskbars.

## 2. Test Verification
- All 30 checks in `--selftest` passed headlessly.
- Release binary cleanly built at `out/XAppGroup.exe`.

