# Order 02 — Selection, command states, and layout stability

**Priority:** P0
**Depends on:** 01
**Findings:** F-06, F-07, F-08, F-10, F-16, F-54
**Files in scope:** `src/Windows.cs` (main list, item container/template, footer, search filter, command methods)
**Status:** Accepted (2026-11-11)

---

## Context

The main list always selects the first group on launch. There is no coherent
selection model after that:

- Selecting a card grows it from 236 px to 276 px because the selected-only
  "Launch All" button and "+ Add App" pill appear, reflowing the list.
- With no selection, Edit, Duplicate, Create Shortcut, Delete, and Test silently
  return. The buttons still look enabled.
- Search replaces `_list.ItemsSource` with a new list, dropping selection;
  clearing search does not restore it.
- Delete removes the selected item and leaves nothing selected.
- Footer buttons never change enabled state.

This order establishes a selection model that survives filtering, deletion, and
reloads, and makes command availability visible. It does **not** redesign the
card actions; order 03 does that on top of this invariant.

---

## The ask

Make selection stable and observable: one clearly selected group, commands that
truthfully reflect whether they can run, and a card whose selected state never
changes its height.

---

## Required behavior

1. **Stable selection identity.**
   - Track selection by `GroupData.Id`, not by index or object identity in a
     temporary filtered list.
   - Filtering, clearing the filter, editing a group, reloading from disk, and
     duplicating preserve the selected group when it still exists.
   - After deleting the selected group, select the next group at the same index;
     if it was the last item, select the new last item. Only leave nothing
     selected when the library is empty.
   - After creating or duplicating a group, select the new group and scroll it
     into view.
2. **No layout jump on selection.**
   - The `ListBoxItem` height for selected and unselected cards must be equal
     (0 px difference, measured by UIA at the review resolution).
   - The primary launch action and the add-app affordance must occupy reserved
     space in every card, or be moved into a fixed-height row. Do not use
     `Visibility.Collapsed` for selected-only content that changes height.
   - Selection styling may change border, background, accent bar, and shadow;
     it must not change margin, padding, font size, or row count.
3. **Command enablement.**
   - Introduce one `UpdateCommandState()` path called on selection change,
     collection change, filter change, and after edit/delete/new/duplicate.
   - With no selected group: Edit, Duplicate, Create Shortcut, Delete, and Test
     are `IsEnabled = false`, rendered with reduced opacity (≈45%), cursor
     `Arrow`, and no hover/press response.
   - New is always enabled.
   - Disabled commands must expose the disabled state to UIA (`IsEnabled`
     false), not just look faded.
   - When no group is selected, show a short inline hint in the footer:
     "Select a group to edit, duplicate, or launch." The hint disappears when a
     selection exists. Do not use a MessageBox for this.
4. **Keyboard selection.**
   - Up/Down moves selection; Home/End jump to first/last; PageUp/PageDown move
     by a page.
   - The selected item is always brought into view.
   - Enter and Delete with no selection do nothing and do not open dialogs.
   - Space selects the focused item if focus is in the list but the item is not
     selected.
5. **Search interaction.**
   - Search must not destroy the underlying collection or selection identity.
   - If the selected group is filtered out, clear the visual selection and
     disable group-scoped commands; keep the remembered Id.
   - Clearing the search restores the remembered selection if that group still
     exists.
   - The footer hint and command states update on every keystroke.
6. **Empty library.**
   - All group-scoped commands are disabled; New remains enabled.
   - No exception when Delete/Edit/Duplicate/Test are invoked with zero groups.
   - The full empty-state visual is order 10; this order only guarantees no
     crash and correct disablement.

## Constraints

- Do not change the card action set or icons; that is order 03. Reserve space
  and keep the existing actions for now.
- Do not change the `GroupData` schema. A new non-persisted field for remembered
  selection is fine.
- Do not replace the `ListBox` with a custom control. Keep WPF selection,
  keyboard navigation, and UIA patterns working.
- Keep the current double-click-to-edit and Enter-to-edit behavior until order 03
  explicitly revisits card actions.

---

## Inputs

- `src/Windows.cs`:
  - `_list`, `BuildCardContainerStyle`, `ItemTemplate` (selected triggers).
  - `ApplySearchFilter`, `UpdateStats`, `Refresh`, `EditGroup`, `New`,
    `Duplicate`, `Delete`, `Test`.
- `UX-REVIEW.md` F-06…F-10, F-16, F-54.
- `test-data/seed-appgroups.json` — contains enough groups to scroll and filter.

---

## Acceptance criteria

- [ ] UIA measures the same `ListBoxItem` height before and after selecting it
      (difference 0 px) at 1536×864 / 125%.
- [ ] Selecting cards with Up/Down does not change the scroll offset or reflow
      the list; the selected row stays under the pointer/keyboard focus.
- [ ] With no selection, all five group-scoped footer commands are disabled and
      the inline hint is visible.
- [ ] Selecting a group immediately enables the correct commands; deleting it
      selects the nearest remaining group and updates the states.
- [ ] Search "dev" then clear restores the previously selected group; if that
      group was filtered out, no group is selected while filtering.
- [ ] Deleting the last group leaves an empty library with only New enabled and
      no exception.
- [ ] New and Duplicate select and reveal the new card.
- [ ] The disabled buttons report `IsEnabled = false` to UIA.
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

1. Record selected/unselected card heights with UIA (or Inspect) before and
   after the change. Put both numbers in the report.
2. Click a card near the bottom of the viewport; confirm it does not jump.
3. Select all groups with the keyboard, then delete the last one and confirm the
   new selection.
4. Type a filter that hides the selection, then clear it with Esc and the mouse.
5. Use the app with zero groups (empty seed) and press Enter/Delete.

---

## Handoff

Write `Docs/agent-report/20261108_ux-02-selection-states.md`. Include the
before/after card heights, the exact commands tested, and any behavior that
still depends on order 03.

Do not start order 03 until accepted.
