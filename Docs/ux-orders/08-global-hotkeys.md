# Order 08 — Real global hotkeys (or none)

**Priority:** P1
**Depends on:** 05, 07
**Findings:** F-29, F-42
**Files in scope:** `src/Core.cs` (model + hotkey service), `src/Windows.cs` (Edit dialog section, badge, hotkey manager, main window hook), `src/Program.cs` (registration lifetime), `src/SelfTest.cs` (parser/validator tests)
**Status:** Pending

---

## Context

The app currently advertises a global hotkey on every card:

- `GroupData.HotkeyBadge` returns `Win + Shift + {GroupCol}`.
- Every group with the same column shows the same fake shortcut.
- "Configure Hotkeys" opens the Edit dialog, which has no hotkey control.
- There is no `RegisterHotKey` P/Invoke anywhere.

This is a trust defect, not a missing nicety. This order either ships real,
working global hotkeys or removes the promise entirely. The decision is to
**ship them**, because the card badge and the ribbon link already occupy the
space and users will try them.

---

## The ask

Add one optional global hotkey per group, persisted across restarts, registered
with Windows while the app runs in the tray, editable in the Edit dialog and a
small hotkey manager, with conflict detection and honest badges.

---

## Required behavior

### 8.1 Data model and migration

- Add a persisted `Hotkey` string property to `GroupData` (canonical format such
  as `Ctrl+Alt+1`, `Win+Shift+G`, `Ctrl+Shift+F12`; empty = none).
- Existing `appgroups.json` files load with `Hotkey = ""`. No migration write is
  required until the user sets one.
- Remove `HotkeyBadge`'s computed `Win + Shift + N` fallback. A badge is shown
  only when a hotkey is actually bound.
- Duplicating a group copies name/layout/apps but **not** the hotkey.
- Deleting a group unregisters its hotkey before removing the group.

### 8.2 Registration service

- Use `RegisterHotKey` / `UnregisterHotKey` on the main window's HWND (or a
  dedicated message-only window). Handle `WM_HOTKEY` in an `HwndSource` hook.
- Register only while the app process is running (tray resident). No Windows
  service, scheduled task, or startup shortcut is created in this order.
- Re-register when:
  - the app starts and finishes loading settings;
  - a group is created, edited, duplicated, deleted, or imported;
  - a hotkey is cleared or changed.
- Unregister on exit and before re-registering. Do not leak registrations.
- Use a stable numeric id per group (do not reuse a live id).
- If the main window is hidden/minimized, the hotkey still works and shows the
  popup at the cursor on the active monitor.
- Do not register duplicates of the same combo.
- Registration must not run during `--selftest`; parser/validator logic must be
  testable headlessly.

### 8.3 Hotkey format and validation

- At least one modifier is required: Ctrl, Alt, Shift, or Win.
- A non-modifier key is required: A–Z, 0–9, F1–F24, arrows, Home/End, PageUp/
  PageDown, Insert/Delete, or another RegisterHotKey-supported key.
- Reject bare letters/digits and modifier-only combos.
- Normalize the display: `Ctrl+Alt+1`, `Win+Shift+G` (fixed order:
  Ctrl, Alt, Shift, Win; stable key casing).
- Reject a combo already bound to another group (case-insensitive, normalized).
- If Windows rejects the combo (already registered by the OS/another app), do
  not save it. Show an inline error: "That shortcut is already in use by Windows
  or another app." Keep the previous binding active.
- Do not persist a binding that failed to register.
- Clearing the field unregisters and saves an empty value.

### 8.4 Edit dialog section

- Add a "Global hotkey" section to `EditWindow` (after Layout, before Save).
- A read-only capture field:
  - Click/Focus enters capture mode; the next key combination is recorded.
  - Shows the normalized combo or "Not set".
  - `Backspace`/`Delete` clears; `Esc` cancels capture and restores the previous
    value.
  - While capturing, temporarily unregister the app's hotkeys so pressing an
    existing one does not launch a popup behind the dialog; restore on exit.
- Inline validation for modifier/key/conflict.
- A "Clear" button with accessible name "Clear global hotkey".
- Help text: "Works while XAppGroup is running in the system tray."
- Save registers the new combo before closing. If registration fails, keep the
  dialog open and show the error.
- Cancel restores the previous registration (unregister the temporary one).

### 8.5 Hotkey manager

- The "Configure Hotkeys" link in the stats ribbon opens a small themed
  **Hotkeys** window, not the Edit dialog.
- The manager lists every group with:
  - group name
  - current hotkey or "Not set"
  - a conflict/invalid marker if applicable
  - buttons: "Set…" (opens Edit at the hotkey section), "Clear"
- `Set…` and `Clear` update the list immediately.
- Empty state: "No groups yet. Create a group to assign a hotkey."
- The window is themed, keyboard accessible, and closes with Esc.
- No hotkey is assigned to the manager itself.

### 8.6 Trigger behavior

- Pressing a group's hotkey opens that group's popup launcher, positioned at the
  cursor, using order 04's placement rules.
- Do not launch all apps directly from the hotkey; the popup is the confirmation
  surface and matches the `--launch <id>` behavior.
- If the group has zero apps, show the popup's empty state with "Edit group".
- If the group was deleted while its hotkey was registered, do not open a stale
  popup; unregister on delete.
- Bring the popup to the foreground, but do not steal focus from a full-screen
  exclusive app if Windows refuses; fail quietly with a tray balloon once.
- Do not fire while the hotkey capture field is active (see 8.4).

### 8.7 Badge and copy

- Card badge: show the real hotkey only. Hide the badge when unset.
- Do not show `Win + Shift + N` defaults derived from `GroupCol`.
- The badge's accessible name is "Global hotkey: {combo}" or it is hidden.
- "Configure Hotkeys" remains as the ribbon link (order 13 may refine the label).

## Constraints

- Do not add a Windows service, driver, or background scheduled task.
- Do not use low-level keyboard hooks (`WH_KEYBOARD_LL`) to simulate global
  hotkeys. Use `RegisterHotKey`.
- Do not add a NuGet dependency.
- Do not change app startup behavior; start-with-Windows is order 13.
- Do not lose hotkeys during unrelated edits or saves.
- If registration is impossible in the test environment, the tests must cover
  parsing/validation and the report must state what was manually verified on a
  real desktop session.

---

## Inputs

- `src/Core.cs`: `GroupData`, `HotkeyBadge`, `Settings`.
- `src/Windows.cs`: `EditWindow` (insert section), `BuildStatsRibbon`
  ("Configure Hotkeys"), card `hotkeyBadge` factory.
- `src/Program.cs`: startup, main window lifetime, tray residency.
- `src/SelfTest.cs`: add parser/validator tests.
- `UX-REVIEW.md` F-42.

---

## Acceptance criteria

- [ ] No fake `Win + Shift + {GroupCol}` badge exists; unset groups show no
      hotkey badge.
- [ ] Setting `Ctrl+Alt+1` on a group and pressing it while the app is minimized
      opens that group's popup at the cursor.
- [ ] The binding survives an app restart and still works.
- [ ] Changing the binding unregisters the old combo; the old combo no longer
      opens anything and the new one does.
- [ ] Assigning a combo already used by another group is rejected inline.
- [ ] Assigning a combo already owned by Windows/another app is rejected with a
      clear message; the previous binding still works.
- [ ] Clearing a hotkey removes the badge and unregisters it.
- [ ] Deleting a group unregisters its hotkey; pressing the old combo does
      nothing.
- [ ] Duplicating a group does not copy the hotkey.
- [ ] The Hotkeys manager opens from "Configure Hotkeys", lists groups, and can
      set/clear bindings.
- [ ] No hotkey fires while the capture field is recording.
- [ ] Parser/validator tests pass headlessly; the total self-test count does not
      decrease.
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

Manual checks on a real desktop session:

1. Bind Ctrl+Alt+1 to "Productivity"; minimize to tray; press it; verify the
   popup.
2. Restart the app; press it again.
3. Bind the same combo to another group; verify the rejection.
4. Bind a combo likely owned by the OS (e.g. Win+L) and verify the error.
5. Clear, delete, and duplicate; verify unregistration.
6. Open the manager from the ribbon; set and clear from there.
7. Inspect `%XAPPGROUP_DATA%\appgroups.json` and confirm only real bindings are
   stored.

---

## Handoff

Write `Docs/agent-report/20261108_ux-08-global-hotkeys.md`. Include the
registration lifecycle, parser/validator test names, manual combos tested, and
the exact conflict behavior observed. State the Windows build used.

Do not start order 09 until accepted.
