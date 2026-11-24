# Order 09 — Feedback, errors, and destructive actions

**Priority:** P1
**Depends on:** 05, 08
**Findings:** F-43, F-44, F-45, F-46, F-47, F-48, F-49, F-50, F-58
**Files in scope:** `src/Core.cs` (launch result, path validity, store backup), `src/Program.cs` (global handlers, store load), `src/Windows.cs` (toasts, delete/duplicate/shortcut flows, missing indicators, tip dismissal)
**Status:** Pending

---

## Context

The app fails silently and confirms too loudly:

- `Launcher.Run` catches every exception and writes to `Debug.WriteLine`.
  Clicking a missing app does nothing.
- `RunAll` launches sequentially with no result; partial failure is invisible.
- Delete asks with **Yes** as the default, then shows a second blocking
  "deleted and unpinned" MessageBox — even if the group was never pinned.
- Shortcut creation shows a blocking dialog with a raw path.
- A corrupt or unreadable `appgroups.json` is swallowed and replaced by an empty
  settings object, which looks like data loss.
- There is no undo, no toast, and no persistent dismissal for the Pro Tip.

This order gives the app one consistent feedback channel and makes destructive
actions safe.

---

## The ask

Add a non-blocking, accessible toast system; report launch and configuration
failures truthfully; replace double/blocking dialogs with one clear
confirmation plus undo; and mark missing files everywhere they appear.

---

## Required behavior

### 9.1 Toast/notification service

- A single app-level toast host, independent of the main window (works while the
  main window is hidden in the tray and while a popup is open).
- Placement: bottom-right of the active monitor's work area, stacked upward,
  never covering the taskbar or the popup's primary content.
- Behavior:
  - Queue, max 3 visible; older toasts collapse.
  - Auto-dismiss after 5 seconds; pause while hovered or keyboard-focused.
  - Manual close button with accessible name "Dismiss notification".
  - No focus stealing. Keyboard users can reach the toast via the notification
    live region/landmark; Undo/Details actions are buttons.
  - Optional action button (e.g. "Undo", "Open folder", "Details").
  - Deduplicate identical messages within 2 seconds.
- Types: info, success, warning, error. Error/warning use the theme's danger
  colors and an icon; do not rely on color alone.
- Live region: `AutomationProperties.LiveSetting = Assertive` for errors,
  `Polite` for success/info. The text must be announced once.
- Dark/Light themed and DPI-aware.
- No toast for routine navigation, selection, or opening a dialog.

### 9.2 Launch results

- Replace `Launcher.Run(string, bool)` internals with a result-returning path,
  e.g. `Launcher.TryRun(path, admin, out LaunchResult)`:
  - Missing file → `NotFound` with the path.
  - Shell failure → `Failed` with the exception message (short, sanitized).
  - Success → `Started`.
- `RunAll` returns a summary: attempted, started, failed, with per-app results.
- UI:
  - Single app failure: error toast "Could not launch {name}: file not found."
    with a "Locate…" action that opens the edit dialog (or Explorer at the
    parent folder).
  - Partial `RunAll`: warning toast "Launched 4 of 5 apps. 1 could not start."
    with "Details" opening a small list of failures.
  - Full success: success toast "Launched 5 apps." (suppress for a single app if
    it opens its own window).
- Launch must remain non-blocking; do not wait for the process to exit.
- Do not show a toast for an app that starts successfully and exits immediately
  (e.g. a launcher shim).

### 9.3 Missing-file indicators

- A path is missing when `File.Exists` is false (for `.lnk`, `.url`, `.exe`,
  `.bat`; do not treat a missing working directory as missing).
- Show a warning:
  - Card app pill: warning icon, muted/danger text, tooltip "File not found:
    {path}".
  - Card summary: "2 apps missing" badge or text.
  - Popup item: warning icon and tooltip; launching shows the error toast.
  - Edit list: warning icon and "File not found" text (order 05).
  - Hotkey manager: not applicable.
- Missing apps must not be silently removed or hidden.
- Re-check on refresh, on group edit, and when the popup opens; cache only for
  the duration of the operation.

### 9.4 Delete

- One confirmation only. No second "deleted" MessageBox.
- Confirmation must include:
  - group name
  - number of apps
  - what happens to shortcuts/pins
  - default button = **Cancel** (destructive action must not be the default)
- Use a themed dialog or `MessageBox` with `MessageBoxResult.No` as the default;
  either is acceptable if the default is safe and the dialog is keyboard
  operable.
- After confirming:
  - Remove the group from the collection and save the config immediately.
  - Show a success toast "Deleted '{name}'." with **Undo** for 8 seconds.
  - Defer deleting generated icons/shortcuts/pins until the undo window expires,
    so Undo can restore the group and its launcher.
  - If Undo is clicked, reinsert the group at its original index, restore
    selection, save, and cancel the cleanup.
  - If the app exits before the window expires, finalize the cleanup on exit.
- Do not claim the group was unpinned unless the unpin actually succeeded.
- Deleting the last group is allowed; the empty state appears (order 10).
- Keyboard `Delete` uses the same flow and the same safe default.

### 9.5 Duplicate and New

- Duplicate success: toast "Duplicated as '{new name}'." with "Edit" action.
- New group creation is silent unless it fails; then show an error toast.
- Do not use MessageBox for success.

### 9.6 Shortcut creation

- Success: toast "Desktop shortcut created." with "Open folder" action.
- Failure: error toast with the reason in plain language; include the target
  folder in the details.
- No blocking success dialog.

### 9.7 Configuration errors

- `Store.Load`:
  - If the file is unreadable or malformed, copy it to
    `appgroups.corrupt-{timestamp}.json` before doing anything else.
  - Show a prominent error toast/banner: "Could not load your groups. A backup
    was saved to {path}." with "Open folder".
  - Start with an empty library only after the backup succeeds; never overwrite
    the corrupt file silently.
- `Store.Save`:
  - Write to a temp file then replace the target atomically (order 11 adds
    rotation; this order at least prevents a half-written file).
  - On failure, show an error toast and keep the in-memory state; do not pretend
    the save succeeded.
- Add a small rolling log under `%APPDATA%\XAppGroup\logs\` (for example
  `2026-11-08.log`) capturing exceptions and store failures. No telemetry, no
  network.

### 9.8 Global exception safety

- Handle `Application.DispatcherUnhandledException`,
  `AppDomain.CurrentDomain.UnhandledException`, and
  `TaskScheduler.UnobservedTaskException`.
- Log the exception, show a friendly error toast or dialog ("Something went
  wrong. Details were written to the log."), and keep the app alive when it is
  safe to do so.
- Do not show raw stack traces to the user.

### 9.9 Pro Tip dismissal

- Persist `TipDismissed` in `Settings`.
- Dismiss hides the banner and saves immediately; it stays hidden after restart.
- A way to restore it belongs in Settings/Help (order 13/15); at minimum, the
  setting exists.

## Constraints

- Do not add a notification NuGet package.
- Do not use modal dialogs for success, launch failure, or non-blocking errors.
- Do not remove the destructive confirmation.
- Do not perform shortcut/icon cleanup before the undo window expires.
- Do not change the group data schema except for `TipDismissed` in `Settings`.
- Do not make toasts steal focus or block the UI thread.

---

## Inputs

- `src/Core.cs` `Launcher.Run`, `RunAll`, `Store.Load`, `Store.Save`,
  `ShortcutHelper.Create`, `Launcher.DeleteShortcut`.
- `src/Program.cs` startup, tray, exception surface.
- `src/Windows.cs` `Delete`, `Duplicate`, `Shortcut`, `BuildProTipBanner`,
  card/popup/edit item templates.
- `UX-REVIEW.md` F-43…F-50, F-58.

---

## Acceptance criteria

- [ ] Clicking a missing app shows an error toast with its name/path; the app
      does not silently do nothing.
- [ ] Launch All on a group with one missing app shows "Launched N-1 of N" with
      a working Details action.
- [ ] Missing files are visibly marked on the card, popup item, and edit row.
- [ ] Delete shows exactly one confirmation; the default is Cancel; no second
      MessageBox appears.
- [ ] After delete, Undo restores the group at its original position and the
      config persists.
- [ ] After the 8-second undo window, the group's shortcut/icon cleanup runs.
- [ ] Duplicate and desktop-shortcut success use toasts, not MessageBoxes.
- [ ] A malformed `appgroups.json` is backed up before the app starts empty, and
      the toast names the backup path.
- [ ] A failed save shows an error and does not lose the in-memory changes.
- [ ] Toasts are readable in Dark/Light, never steal focus, pause on hover, and
      expose a live region and a Dismiss button.
- [ ] The Pro Tip stays dismissed after restart.
- [ ] Unhandled exceptions are logged and surfaced without a raw stack trace.
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

Failure-path checks:

1. Use the "Missing Apps" seed group; launch each missing app and Launch All.
2. Delete a group, click Undo before 8 seconds; restart and verify it is back.
3. Delete a group and wait 8 seconds; verify no undo restores it and cleanup ran.
4. Corrupt a copy of `appgroups.json`; launch with `XAPPGROUP_DATA` pointing at
   it; verify the backup and message.
5. Make the config read-only and edit a group; verify the save error.
6. Dismiss the Pro Tip, restart, verify it stays hidden.
7. Trigger a test exception behind a debug flag (or a temporary throw) and verify
   the log and friendly message; remove the flag before reporting.

---

## Handoff

Write `Docs/agent-report/20261108_ux-09-feedback-errors.md`. Include the toast
placement, the delete/undo timing, the exact failure paths exercised, and the
log location. Attach the malformed-config backup path from the test.

Do not start order 10 until accepted.
