# Order 05 — Edit Group dialog: themed, validated, usable

**Priority:** P1
**Depends on:** 02, 03
**Findings:** F-13, F-24, F-25, F-26, F-27, F-28, F-29
**Files in scope:** `src/Windows.cs` (`EditWindow`), `src/Core.cs` only for validation helpers if needed
**Status:** Pending

---

## Context

The Edit Group dialog is where users actually build a group, but it is the least
finished surface in the app:

- It is a default WPF window with a bright native title bar while the main window
  uses custom chrome.
- Grid/sort dropdowns are unstyled Windows controls; checkboxes are default.
- Only an empty name is validated. Duplicate names are accepted.
- Cancel/Esc does not warn about unsaved changes.
- The apps list shows full paths to UIA, has no multi-select, no missing-file
  markers, and no keyboard delete.
- It is fixed at 500 DIP wide and non-resizable; long content can overflow.
- "Add App" from the card opens this same dialog with no visual cue or focus on
  the Applications section.

This dialog is also the future home of the hotkey control (order 08) and the
source of the "Add apps" path from order 03. Build it so those can be added
without another rewrite.

---

## The ask

Rebuild `EditWindow` as a themed, resizable, validated dialog with a clear
section structure, an application list that handles real-world paths, and
non-destructive cancel semantics. Add an "Applications focus" entry mode used by
the card's Add App action.

---

## Required behavior

### 5.1 Window and chrome

- Custom themed chrome consistent with the main window: same corner radius,
  background, border, title typography; no bright native title bar in dark mode.
- Title: "Edit Group" normally; "Edit Group — Applications" when opened from
  Add App / Add apps.
- Draggable by the title area; close button; `Esc` cancels; `Enter` does not
  accidentally save while typing in a text field.
- Resizable with `MinWidth = 520`, `MinHeight = 480`, `MaxWidth`/`MaxHeight`
  clamped to the current work area. Default ≈ 560×640 DIP, but never taller than
  the work area at 125%.
- The content area scrolls vertically when the window is shorter than the
  content; the Save/Cancel row stays pinned at the bottom and visible.
- Follows theme changes live (no restart).

### 5.2 Identity section

- Group name field with label, placeholder, and inline validation.
- Rules:
  - Required, trimmed.
  - Maximum 80 characters; show a counter or inline message when exceeded.
  - Case-insensitive uniqueness against all other groups. The current group's
    own name is allowed.
  - No path-invalid characters for shortcut names: `< > : " / \ | ? *`.
- Validation appears inline below the field; do not use a MessageBox for field
  errors.
- Save is disabled while the name is invalid.
- `AutomationProperties.Name` for the field is "Group name".

### 5.3 Icon section

- 44×44 preview, "Choose icon…", "Reset to automatic".
- Show "Automatic 2×2 composite" or the custom file name.
- Reject unusable icon files with an inline message, not a blocking MessageBox.
- Keep the existing icon generation fallback.

### 5.4 Applications section

- Header: "Applications" with the current count ("8 apps", singular "1 app").
- List supports:
  - Multi-select (Ctrl/Shift).
  - Add multiple files at once.
  - Remove selected (Delete key and button).
  - Move up/down (buttons and Alt+Up/Alt+Down).
  - Drag files from Explorer to add.
  - Duplicate paths are ignored case-insensitively.
- Each row shows the app icon, display name, and full path as a secondary line or
  tooltip. UIA name must be "`{name}`, `{path}`" (not just the full path and not
  just the file name).
- Missing files are marked with a warning icon and "File not found" text; they
  are allowed but must be visible. Do not silently remove them.
- Empty state in the list: "No apps yet. Add files or drag them here." with an
  Add button.
- The list scrolls; do not let a 100-app group grow the dialog past the work
  area.
- When opened in Applications mode, scroll this section into view and focus the
  Add button (or the list if non-empty). The user must not have to hunt for it.

### 5.5 Layout section

- Grid columns: 1–8, themed dropdown or segmented control.
- Sort order: Manual / Alphabetical, themed.
- Checkboxes: "Show app labels in launcher" and "Show group in system tray".
- All controls have visible labels and accessible names.
- Do not use default WPF ComboBox/CheckBox styling. Reuse the themed dropdown
  from the footer or a shared themed control.

### 5.6 Save / Cancel semantics

- Work on a local copy of the group. Do not mutate `GroupData` until Save
  succeeds.
- Save:
  1. Re-validate name and paths.
  2. Apply all fields to the model once.
  3. Refresh icons and pinned shortcut.
  4. Close with `DialogResult = true`.
- Cancel/Esc/close button:
  - If nothing changed, close immediately.
  - If anything changed, show a single themed confirmation: "Discard changes?"
    with **Discard** (destructive style) and **Keep editing** (default).
  - Cancel must leave the model and disk untouched.
- If applying the changes or syncing the pinned shortcut throws, keep the dialog
  open, show an inline error, and do not leave a half-applied group.
- Save button is the default action only when focus is not in the applications
  list; Enter in the list should not save unexpectedly.
- Show a busy state during save if icon regeneration takes more than ~150 ms;
  disable Save/Cancel to prevent double submission.

### 5.7 Accessibility (minimum for this order)

- Every input has a label bound via `AutomationProperties.LabeledBy` or an
  explicit accessible name.
- Tab order follows the visual order: name → icon actions → applications →
  layout → Save/Cancel.
- Focus starts in the name field (or the Applications list in Applications mode).
- No `FocusVisualStyle = null` in this dialog.
- Full audit is order 07; do not leave obvious gaps.

## Constraints

- Do not add the hotkey UI here; order 08 adds it in a dedicated section.
- Do not change the JSON schema.
- Do not change icon generation or shortcut formats.
- Do not rewrite the main-window card template; only share theme helpers if
  needed.
- Do not remove the ability to drag files from Explorer.
- Keep `TaskbarHelper.SyncPinnedShortcut` behavior after a successful save.

---

## Inputs

- `src/Windows.cs` `EditWindow` (all sections, `Save`, `Move`, `Add`,
  `SelectIcon`, `ItemTpl`).
- `src/Core.cs` `GroupData`, `Icons`, `ShortcutHelper`,
  `TaskbarHelper.SyncPinnedShortcut`.
- `Docs/Design/main screen/code.html` for visual language.
- `UX-REVIEW.md` F-24…F-29.
- `test-data/seed-appgroups.json` ("Missing Apps", "Productivity",
  "A Very Long Group Name…").

---

## Acceptance criteria

- [ ] The dialog has themed custom chrome in Dark and Light; no native white
      title bar appears.
- [ ] It fits inside the 1536×864 / 125% work area and scrolls its content; the
      Save/Cancel row is always visible.
- [ ] Duplicate names (case-insensitive) are rejected inline; Save is disabled.
- [ ] Empty, whitespace-only, over-80-character, and path-invalid names are
      rejected inline.
- [ ] Cancel after edits shows one "Discard changes?" prompt; Keep editing
      returns to the dialog; Discard leaves the model and `appgroups.json`
      unchanged.
- [ ] "Add App" from the card opens the dialog with the Applications section in
      view and Add focused; the title says "Applications".
- [ ] The apps list supports multi-select, Delete, Alt+Up/Down, drag-drop, and
      shows missing files with a warning.
- [ ] A 100-app group does not grow the dialog beyond the work area.
- [ ] All dropdowns/checkboxes are themed and have accessible names.
- [ ] There is no MessageBox for field validation.
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

Manual checks:

1. Open Edit from a card, from the footer, and from "Add App". Compare entry
   focus and title.
2. Try duplicate/empty/long/invalid names; confirm Save state.
3. Edit several fields, press Esc, choose Keep editing, then Save; verify the
   changes persist after restart.
4. Edit, press Cancel, choose Discard, restart; verify nothing changed.
5. Add 100 files (or simulate) and open the dialog at 125%; confirm scrolling.
6. Tab through every control; check focus rings and names with Inspect.

---

## Handoff

Write `Docs/agent-report/20261108_ux-05-edit-dialog.md`. Include which validation
rules are enforced where, the final dialog size, and the exact dirty-state flow
tested.

Do not start order 06 until accepted.
