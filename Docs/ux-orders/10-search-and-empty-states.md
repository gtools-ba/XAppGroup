# Order 10 — Search, no-results, and empty library

**Priority:** P1
**Depends on:** 02, 09
**Findings:** F-09, F-51, F-52, F-53, F-54, F-55
**Files in scope:** `src/Windows.cs` (search box, title bar, list area, empty states), `src/SelfTest.cs` (filter predicate tests)
**Status:** Pending

---

## Context

Search exists but is incomplete:

- No clear (×) button; Esc clears but is undiscoverable.
- A query with no matches leaves a blank list with no explanation.
- There is no result count and no indication that the stats ribbon is global
  while the list is filtered.
- Filtering replaces `_list.ItemsSource` with a new list, which drops selection
  and defeats collection-view behavior.
- A first run with zero groups is a blank content area.

Order 02 fixed selection identity; this order fixes the search experience and
adds the two empty states.

---

## The ask

Make search a first-class, non-destructive filter with a clear action, a result
count, a no-results state, and a proper empty-library state.

---

## Required behavior

### 10.1 Filtering model

- Keep `_list.ItemsSource` bound to the live `_s.Groups` collection (or a single
  `ICollectionView` over it). Do not replace the source with a new `List<T>` on
  every keystroke.
- Use a filter predicate over:
  - group name
  - app display name (file name without extension)
  - full app path (optional but useful for power users)
- Match case-insensitively, trim leading/trailing whitespace, treat empty/space
  as no filter.
- Do not use regex; literal `Contains` semantics.
- Re-evaluate the filter when:
  - the query changes
  - groups are added/edited/deleted
  - the app reloads from disk (order 13)
- Preserve the order of `_s.Groups`; do not sort results by default.
- The predicate must be unit-testable without a window. Add headless tests for:
  empty query, group-name match, app-name match, path match, no match, and
  case-insensitivity.

### 10.2 Search box

- Clear button (`×`) inside the field, visible only when text is non-empty.
  - 24×24 hit target, accessible name "Clear search", keyboard reachable.
  - Clicking clears the query and returns focus to the search field.
- `Esc`:
  - first press clears a non-empty query and keeps focus in the field;
  - second press (already empty) moves focus to the selected group in the list.
- `Ctrl+F` focuses the field and selects all text.
- Placeholder: "Search groups or apps (Ctrl + F)…" and it disappears when the
  field has text.
- Accessible name "Search groups and apps"; help text "Type to filter. Press
  Escape to clear."
- The field must not lose focus when the list refreshes.

### 10.3 Result count

- When a filter is active, show "Showing {visible} of {total} groups" in or next
  to the search area.
- When no filter is active, hide the count (or show nothing extra).
- The stats ribbon stays global (total groups/launchers). It must be visually
  clear that the count belongs to the search, not the ribbon.
- Use singular/plural correctly ("1 of 1 group", "3 of 17 groups").

### 10.4 No-results state

When the library has groups but the filter matches none:

- Show a centered panel inside the list area (not a modal):
  - search/empty icon
  - "No groups match \"{query}\"."
  - a short hint: "Try a group name or an app name."
  - **Clear search** button (primary/secondary)
  - **New group** button (secondary) that opens the New Group dialog with the
    query prefilled as the name, if valid; otherwise "New Group".
- The footer commands follow order 02: no selection → group-scoped commands
  disabled.
- Do not show the no-results panel while the query is empty.
- The panel must be keyboard reachable and announced when it appears
  (`AutomationProperties.LiveSetting = Polite` on the message).

### 10.5 Empty library state

When `_s.Groups.Count == 0` (and no filter):

- Show a centered onboarding panel:
  - app/group icon
  - "No app groups yet"
  - one sentence: "Group the apps you use together, then launch them all at
    once."
  - **Create your first group** primary button (same action as New)
  - a secondary "Learn how it works" that opens Help (order 15; until then, it
    may focus the Pro Tip or be hidden)
- Stats show 0 correctly; no fake launchers.
- The empty state must not appear underneath the no-results state.
- The list area must not show a scrollbar when empty.
- The state is themed and accessible; the primary action is the default focus
  when the window opens with zero groups (but do not steal focus repeatedly).

### 10.6 Search + empty interaction

- If the library is empty, searching still works but the empty-library state
  remains (do not show "No groups match" for an empty library).
- If a group is created while a filter is active, it appears only if it matches;
  otherwise show a toast "Group created. Clear search to see it." (order 09
  toast).
- If the selected group is filtered out, selection is cleared per order 02 and
  the footer hint explains why only if needed.
- Clearing the search restores the previous selection and scrolls it into view.

### 10.7 Performance

- Debounce filter application by 100–150 ms for typing; apply immediately on
  clear/Esc/Enter.
- Filtering 500 groups / 5,000 apps should not block the UI thread for more than
  ~50 ms; if it does, cache a lowercase search index per group and invalidate it
  on change.
- Do not rebuild the visual tree for every keystroke; the collection view
  handles it.

## Constraints

- Do not add a search dependency or a fuzzy-search library.
- Do not change the footer commands.
- Do not filter the stats ribbon.
- Do not persist the query across restarts.
- Do not replace the `ListBox` with a custom control.
- Keep the approved title-bar composition; the clear button lives inside the
  existing search field.

---

## Inputs

- `src/Windows.cs`: `_searchBox`, `searchHint`, `ApplySearchFilter`, `_list`,
  title bar, list area, `UpdateStats`.
- `src/Core.cs`: `GroupData.GroupName`, `Paths`, `OrderedPaths`.
- `src/SelfTest.cs`: add predicate tests.
- `UX-REVIEW.md` F-09, F-51…F-55.
- `test-data/seed-appgroups.json`: "Productivity", "Dev Tools", "Missing Apps",
  duplicate names for filter tests.

---

## Acceptance criteria

- [ ] Typing filters without replacing `_list.ItemsSource`; selection survives a
      clear (order 02).
- [ ] A clear button appears only with text, clears the query, and is keyboard
      reachable.
- [ ] `Esc` clears first, then moves focus to the list on a second press.
- [ ] The result count reads correctly for 0, 1, and many matches and is hidden
      when no filter is active.
- [ ] A nonsense query shows the no-results panel with the query, Clear search,
      and New group; no blank list.
- [ ] An empty library shows the empty-library panel with a working Create
      button; no no-results panel.
- [ ] The stats ribbon still shows global totals while filtered.
- [ ] Filter predicate tests cover the cases in 10.1 and pass.
- [ ] Typing in a 500-group library does not visibly lag; report the measured
      filter time.
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

1. Search "dev", then "notepad", then a path fragment, then "zzzz".
2. Clear via the ×, Esc, and Ctrl+F; confirm focus behavior.
3. Select a group, filter it out, clear; confirm selection restoration.
4. Launch with an empty `XAPPGROUP_DATA`; confirm the empty-library panel.
5. Create a group from the empty state and from the no-results state.
6. Test with a generated 500-group config if available; record keystroke latency.

---

## Handoff

Write `Docs/agent-report/20261108_ux-10-search-empty.md`. Include the predicate
test names, measured filter time, and screenshots/OCR of both empty states.

Do not start order 11 until accepted.
