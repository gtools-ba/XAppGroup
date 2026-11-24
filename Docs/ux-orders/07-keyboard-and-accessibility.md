# Order 07 — Keyboard, focus, and accessibility

**Priority:** P1
**Depends on:** 02, 03, 04, 05, 06
**Findings:** F-17, F-36, F-37, F-38, F-39, F-40, F-41
**Files in scope:** `src/Windows.cs` (main, popup, edit, shared controls), `src/Core.cs` for accessible display properties
**Status:** Pending

---

## Context

The app is effectively mouse-only:

- Close/minimize/maximize are 12×12 `Border`s with no Button pattern or name.
- Card actions and app pills are `Border` + mouse handlers with no Invoke pattern.
- `FocusVisualStyle = null` on list items; the custom button template has no
  focus visual.
- UIA names are CLR type names: `XAppGroup.GroupData`,
  `XAppGroup.AppPillItem`, and full file paths in the edit list.
- The search box, theme ComboBox, and other inputs have empty names.
- Keyboard users can select, edit, and delete, but cannot launch, duplicate,
  create a shortcut, or open the more menu.

This order closes the gap across all three windows. It assumes the controls from
orders 03–05 already exist; it converts the remaining custom surfaces and adds
the missing names, patterns, focus, and shortcuts.

---

## The ask

Make every action in the app reachable and understandable without a mouse, and
make the accessibility tree describe the UI instead of the CLR types.

---

## Required behavior

### 7.1 Real controls and patterns

- Every actionable element must expose the correct UIA control type and pattern:
  - Buttons: `Button` / Invoke.
  - Menu entries: `MenuItem` / Invoke.
  - List rows: `ListItem` / SelectionItem, plus Invoke or a documented child
    button.
  - Text inputs: `Edit` / Value.
  - Dropdowns: `ComboBox` / ExpandCollapse + Selection.
  - Checkboxes: `CheckBox` / Toggle.
- Convert the traffic lights, header icon buttons, banner Dismiss, popup close,
  and any remaining `Border`-as-button to `Button` (styled with a control
  template that preserves the visual).
- Do not fake a pattern with mouse handlers.

### 7.2 Accessible names

- List item name: `{GroupName}, {N apps}` — not `XAppGroup.GroupData`. Add a
  read-only, non-persisted `AccessibleName` (or equivalent) to `GroupData` and
  raise `PropertyChanged` when the name/app count changes.
- App pill name: `Launch {app name}`; help text includes the full path.
- Card actions:
  - "Launch all apps in {group name}"
  - "Open launcher for {group name}"
  - "More options for {group name}"
  - "Add apps to {group name}"
- Traffic lights: "Close to system tray", "Minimize window", "Maximize window"
  (order 12 may update the close behavior; keep the name truthful).
- Header icons: "Refresh app icons", "Open settings" (order 13) or the current
  truthful action; never just "Settings" if it edits a group.
- Search: "Search groups and apps"; help text "Type to filter. Press Escape to
  clear."
- Theme dropdown: "Theme"; each item is its visible label.
- Edit list row: `{app name}, {full path}`; missing rows append ", file not
  found".
- Footer: the visible label is the name; no duplicate punctuation.
- Icon-only controls also get `AutomationProperties.HelpText` with the tooltip
  text.

### 7.3 Focus

- Remove `FocusVisualStyle = null` from list items and any control that can
  receive focus.
- Add a focus ring that is visible on every background in both themes:
  - At least 2 DIP, ≥3:1 contrast, offset from the control edge.
  - Visible for list items, card action buttons, app pills, footer buttons,
    traffic lights, menu items, popup items, inputs, dropdowns, checkboxes.
- Focus must never be invisible or move without a visible indicator.
- After closing a popup/dialog, focus returns to the control that opened it.
- After deleting a group, focus moves to the newly selected group (order 02),
  with the ring visible.
- No focus traps. `Tab` leaves the popup; `Esc` closes it.
- Mouse click on a card sets keyboard focus to that card, not just selection.

### 7.4 Tab order and regions

- Main window tab order follows the visual order:
  1. Traffic lights / title-bar actions
  2. Search
  3. Group list
  4. Footer commands
  5. Theme selector
  6. Pro-tip dismiss (if visible)
- `Ctrl+F` focuses and selects the search text (already exists).
- `F6` / `Shift+F6` cycles between the title bar, list, and footer regions.
- `Tab` from the title bar does not jump into hidden/disabled controls.
- Disabled commands are skipped by Tab.

### 7.5 Keyboard shortcuts for card actions

With a card focused:

| Key | Action |
|---|---|
| `Enter` | Open launcher for the group (primary) |
| `Ctrl+E` or `F2` | Edit group |
| `Ctrl+D` | Duplicate group |
| `Ctrl+Shift+C` | Create desktop shortcut |
| `Delete` | Delete group (with confirmation) |
| `Shift+F10` / Menu key | Open the more-options menu |
| `Space` | Select the focused card |
| `Up/Down/Home/End/PageUp/PageDown` | Move selection |

- Do not use `Enter` for Edit anymore; `Enter` is the primary launch action.
- Shortcuts must not fire while a text field has focus.
- Add these to the Help/shortcut reference in order 15; the card tooltips should
  mention the primary one where useful.
- Footer buttons keep their access keys (`_New`, `_Edit`, etc.) and must work
  when the list has focus.

### 7.6 Target size and hit areas

- Every clickable target is at least 24×24 DIP, or has ≥24 DIP spacing from its
  neighbors.
- The 12 DIP traffic-light dot may stay visually 12 DIP, but its hit area must
  be at least 28×28 DIP.
- App pills and action chips meet the same minimum.
- Scrollbar width is handled in order 06; ensure the hit target is usable.

### 7.7 Window and system accessibility

- `Alt+Space` opens the window system menu (Restore/Move/Size/Minimize/Maximize/
  Close) even with custom chrome.
- `Alt+F4` behaves like the close button (order 12 defines tray behavior).
- The window has a title and an accessible help text describing the app.
- The tray icon has a keyboard-accessible menu (Windows handles this) with
  meaningful item names.
- Narrator announces selection changes, command enablement, and validation
  errors. Use `AutomationProperties.LiveSetting` for inline validation and the
  order 09 toast region.

### 7.8 Verification with assistive technology

- Run Accessibility Insights for Windows (Fast Pass) on main, popup, and edit.
- Fix every "missing name", "control type", "focus order", and "keyboard
  unreachable" issue it reports.
- Do a Narrator smoke test: open the app, navigate to a group, launch it, open
  the more menu, edit, save, and return focus.

## Constraints

- Do not change the visual design to add accessibility; style the real controls
  to look like the current design.
- Do not remove mouse behavior.
- Do not add a dependency or a UI automation framework.
- Do not change business logic or persistence.
- Do not regress the order 04 popup keyboard model or order 05 dialog flow.

---

## Inputs

- `src/Windows.cs`: traffic lights, header buttons, banner dismiss, card
  templates, popup items, edit list, footer, search, theme dropdown.
- `src/Core.cs`: `GroupData` (add accessible display property), `AppPillItem`.
- `UX-REVIEW.md` F-17, F-36…F-41.
- Current UIA observations: no Close/Minimize/Maximize buttons; item names are
  type names; ComboBoxes unnamed.

---

## Acceptance criteria

- [ ] UIA exposes Close, Minimize, and Maximize as buttons with the required
      names and Invoke patterns.
- [ ] No actionable element is reported with an empty name.
- [ ] No actionable element uses `XAppGroup.GroupData`,
      `XAppGroup.AppPillItem`, or a raw path as its only name.
- [ ] Card, pill, footer, popup, and dialog actions expose Invoke and are
      keyboard focusable.
- [ ] A 2 DIP visible focus ring appears on every focusable control in both
      themes, including list items.
- [ ] The full main window can be operated with Tab/Shift+Tab, arrows, Enter,
      Space, Esc, and the Menu key; no mouse is required.
- [ ] `Enter` opens the launcher; `Ctrl+E`/`F2` edits; `Ctrl+D` duplicates;
      `Ctrl+Shift+C` creates a shortcut; `Delete` deletes; Menu key opens the
      more menu.
- [ ] Focus returns to the opener after closing the popup and the edit dialog.
- [ ] `F6` cycles title/list/footer regions.
- [ ] `Alt+Space` opens the system menu.
- [ ] Accessibility Insights Fast Pass reports no keyboard/name/control-type
      failures on main, popup, and edit.
- [ ] Narrator can complete the smoke-test flow.
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

1. Unplug the mouse. Complete: open launcher, launch an app, edit a group,
   duplicate it, open the more menu, create a shortcut, delete a group, switch
   theme.
2. Tab through the main window and list every focus stop; attach the list to the
   report.
3. Run Accessibility Insights on all three windows; attach the summary.
4. Do the Narrator flow and note any announcement that is missing or wrong.
5. Check focus return after popup and dialog close.

---

## Handoff

Write `Docs/agent-report/20261108_ux-07-accessibility.md`. Include the UIA name
audit, the focus-stop list, Accessibility Insights results, and the Narrator
script/outcome. If Narrator is unavailable, say so and provide the UIA evidence
instead.

Do not start order 08 until accepted.
