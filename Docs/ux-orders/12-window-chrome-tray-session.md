# Order 12 — Window chrome, tray, and session restore

**Priority:** P1
**Depends on:** 01, 07
**Findings:** F-05, F-61, F-62, F-63, F-64
**Files in scope:** `src/Windows.cs` (title bar, caption buttons, placement), `src/Core.cs` (`Settings` placement fields), `src/Program.cs` (tray, close-to-tray, quit, single instance)
**Status:** Pending

---

## Context

The main window is a tray-resident app with custom chrome, but the lifecycle is
implicit:

- Closing always hides to the tray (`e.Cancel = true`). There is no Quit in the
  main UI; the only Exit is in the tray menu.
- The first close shows a balloon once; after that the window can appear to
  vanish with no explanation.
- Window size/position/maximized state are not persisted. Every launch is
  centered at the fixed default.
- With `CaptionHeight = 0`, there is no right-click system menu and no
  `Alt+Space` menu.
- A window restored on a disconnected monitor could be off-screen.

This order makes the chrome behave like a real desktop window and makes the tray
relationship explicit.

---

## The ask

Persist and restore window placement safely, give the close button an honest
hide-to-tray behavior with an explicit Quit path, and make the tray menu and
single-instance activation complete.

---

## Required behavior

### 12.1 Window placement persistence

- Persist in `Settings`:
  - normal (restored) left, top, width, height
  - maximized state
  - optionally the monitor device name
- Save:
  - on move/resize end (debounced ~500 ms)
  - before hiding to tray
  - on exit
- Restore on launch:
  - if the saved rectangle intersects a connected monitor's work area by at
    least 100×100 DIP, restore it there
  - otherwise center on the primary monitor at the order 01 default/clamped size
  - clamp to the work area so no part is off-screen
  - if maximized, restore the normal bounds and then maximize
- Do not restore a size smaller than `MinWidth`/`MinHeight`.
- DPI changes between sessions must not shrink or blur the window; use
  DIP-consistent values and re-clamp.
- First run (no saved placement) uses order 01's centered, clamped default.

### 12.2 Close, minimize, quit

- **Minimize button:** minimizes to the taskbar (not the tray). Tooltip
  "Minimize window".
- **Close button (X):** hides the main window to the tray, keeping hotkeys and
  tray groups active. Tooltip and accessible name: "Hide to system tray".
- **Alt+F4:** same as the close button.
- First hide:
  - show one non-blocking toast (order 09): "XAppGroup is still running in the
    system tray. Right-click the tray icon to quit."
  - persist a `TrayNoticeShown` flag so it appears once per installation
  - do not use a blocking MessageBox or a balloon that disappears unreliably
- Add an explicit **Quit XAppGroup** command:
  - in the title-bar more/overflow menu
  - in the tray context menu (existing Exit)
  - `Ctrl+Q` from the main window
- Quit:
  - if an Edit dialog or other modal is open, bring it to the front instead of
    killing it silently
  - dispose the tray icon, unregister hotkeys, save placement/settings, and
    `Shutdown()`
  - no ghost tray icon remains
- Do not change the tray-resident model to exit-on-close.

### 12.3 Title-bar system menu

- Right-click on the title bar opens the standard system menu at the cursor:
  Restore, Move, Size, Minimize, Maximize, Close.
- `Alt+Space` opens the same menu.
- The menu items act on the window and respect the custom chrome.
- Double-click the title bar toggles maximize (keep).
- Dragging the title bar moves the window (keep), but never starts from a
  button, the search field, or any interactive child.
- Caption buttons must have the 28×28 hit target from order 07 and visible
  hover/pressed states.

### 12.4 Tray menu and icon

- Left click or double click the tray icon opens/restores the main window.
- Right-click menu:
  1. Open XAppGroup
  2. separator
  3. groups with `ShowOnTray` enabled, each with "Launch All" and its apps
  4. separator
  5. Settings… (order 13; until then, may be omitted)
  6. Quit XAppGroup
- Group submenu items use the unique group name and app count where practical;
  no duplicate labels after order 11.
- Tray tooltip: "XAppGroup — {N} groups" (update after add/delete/rename).
- Rebuild the menu after any group change, theme change, or show-on-tray toggle.
- No orphan icon after quit or crash recovery; call `Dispose()`.

### 12.5 Single instance and activation

- Keep the existing mutex + named event behavior.
- Launching a second instance must:
  - restore the existing window if hidden or minimized
  - bring it to the foreground
  - consume any pending `--edit` request
  - exit the second process
- Do not create a second tray icon.
- Verify with a normal launch, a shortcut launch, and `--launch <id>`.

### 12.6 Taskbar and z-order

- When the main window is visible, it appears in the taskbar with the app icon
  and title "XAppGroup".
- When hidden to the tray, it leaves the taskbar; the tray icon remains.
- The popup is topmost and does not appear in the taskbar (order 04).
- Activating from the tray brings the window to the foreground reliably; if
  Windows blocks foreground activation, flash the taskbar button or show the
  window without stealing focus.

## Constraints

- Do not add a startup shortcut or run-key entry; start-with-Windows is order 13.
- Do not change the tray-resident model or `ShutdownMode`.
- Do not persist window placement per group.
- Do not store placement in a separate file; use `Settings`.
- Do not use a native title bar.
- Keep the existing single-instance mechanism unless it is proven broken; if
  changed, document why.

---

## Inputs

- `src/Program.cs`: `Begin`, `_main.Closing`, `BuildTray`, `RebuildTray`,
  `ActivateMainWindow`, mutex/event.
- `src/Windows.cs`: `BuildTitleBar`, `BuildWindowCaptionBtn`, `ToggleMaximize`,
  `WindowChrome`.
- `src/Core.cs` `Settings`.
- `UX-REVIEW.md` F-05, F-61…F-64.

---

## Acceptance criteria

- [ ] Resize/move the window, quit, relaunch: the same normal size/position and
      maximized state are restored.
- [ ] Unplug/disable the saved monitor, relaunch: the window appears on the
      primary monitor, fully visible.
- [ ] The close button hides to the tray; the first hide shows one non-blocking
      notice; the second hide is silent and the notice does not return after
      restart.
- [ ] `Ctrl+Q`, the title-bar Quit, and the tray Quit all exit cleanly; no tray
      icon remains.
- [ ] `Alt+F4` hides to the tray, not exit.
- [ ] Minimize goes to the taskbar, not the tray.
- [ ] Right-click title bar and `Alt+Space` show the system menu; all items
      work.
- [ ] Tray left/double click restores the window; the menu lists the correct
      groups and apps.
- [ ] A second instance restores the first and exits; no second tray icon.
- [ ] The window appears/disappears from the taskbar correctly.
- [ ] Build and self-test pass.

---

## Verification

```powershell
dotnet build src/XAppGroup.csproj -c Release
$env:XAPPGROUP_DATA = "C:\Users\RedStone\AppData\Local\Temp\opencode\xappgroup-ux"
Copy-Item Docs\ux-orders\test-data\seed-appgroups.json "$env:XAPPGROUP_DATA\appgroups.json" -Force
out\XAppGroup.exe
out\XAppGroup.exe --selftest
```

Checks:

1. Move/resize/maximize, exit via tray, relaunch; compare rects.
2. Hide via X, Alt+F4, and minimize; compare taskbar/tray state.
3. Quit via Ctrl+Q, title bar, tray; check Task Manager for a remaining process
   and the notification area for a ghost icon.
4. Right-click title bar and press Alt+Space.
5. Launch a second `out\XAppGroup.exe` while hidden.
6. If possible, change the monitor arrangement and relaunch.

---

## Handoff

Write `Docs/agent-report/20261108_ux-12-chrome-tray-session.md`. Include the
saved/restored rectangles, the close/minimize/quit matrix, and the single-instance
evidence.

Do not start order 13 until accepted.
