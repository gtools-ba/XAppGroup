# Report: Order 02 — Selection, command states, and layout stability

- **Date:** 2026-11-11
- **Order / Task:** `Docs/ux-orders/02-selection-and-command-states.md` (Findings `F-06`, `F-07`, `F-08`, `F-10`, `F-16`, `F-54`)
- **Status:** Accepted (2026-11-11); independent verifier verdict **PASS**

---

## 1. Environment used for verification

| Item | Value |
|---|---|
| Display | 1920×1080, 125 % scaling (work area `(0,0)`–`(1920,1020)` px) |
| Process DPI awareness | **PerMonitorV2** (`GetAwarenessFromDpiAwarenessContext` = 2) |
| Window DPI | 120 (1.25×) |
| Main window under test | `(400, 48, 1120×925)` px = **896×740 DIP** |
| Minimum-size run | `(400, 48, 900×650)` px = **720×520 DIP** |
| OS / runtime | Windows, .NET 10 WPF |

The original review resolution (1536×864) is unavailable on this machine; this is
already documented in the accepted order 01 report. All numbers below are **REAL**
measurements at 125 % on 1920×1080, taken from the live UI with UI Automation.
The window is the same 896×740 DIP design size that the review resolution would
use; card height is driven by DIP layout, not by the desktop resolution.

---

## 2. What was done

1. **Stable selection identity.**
   - The list now binds once to `CollectionViewSource.View` over the live
     `ObservableCollection<GroupData>` (line 1047). Filtering changes only the
     view predicate; it never replaces `ItemsSource`.
   - `_rememberedGroupId` stores `GroupData.Id` (not an index or a filtered-list
     object). Selection changes update it, except during a view refresh
     (`_suppressSelectionMemory`).
   - `RestoreSelectionFromRemembered()` re-selects the remembered Id after a
     filter change, a collection change, a reload, and an edit.
   - `SelectAndReveal()` selects a new/duplicated group and calls
     `ScrollIntoView` immediately and again at `DispatcherPriority.Loaded`.
2. **Delete selection.**
   - `RemoveGroupSelectNearest()` captures the visible index before removal and
     selects the group that takes that index; if the removed group was last it
     selects the new last group. The remembered Id is kept if an active filter
     hides all survivors. Nothing is selected only when the library is empty.
3. **No layout jump.**
   - The selected-only `Launch All` button and `+ Add App` pill are part of every
     card with `Visibility.Hidden`; the active/inactive launch variants share one
     fixed `Grid` cell. Selection changes border/background/accent/shadow only.
4. **One command-state path.**
   - `UpdateCommandState()` is called on selection change, collection change,
     filter change, and after edit/new/duplicate/delete. It sets
     `IsEnabled`, `Opacity = 0.45`, and `Cursor = Arrow` for Edit, Duplicate,
     Create Shortcut, Delete, and Test. New stays enabled.
   - The inline hint `"Select a group to edit, duplicate, or launch."` is visible
     only when nothing is selected.
5. **Keyboard rules.**
   - WPF Up/Down/Home/End/PageUp/PageDown navigation is retained; selection is
     brought into view. Space selects the focused card.
   - Enter and Delete are handled by the list and call Edit/Delete only when
     `Sel != null`; with no selection they are consumed and open no dialog.
6. **Search interaction.**
   - `ApplySearchFilter()` applies/clears a `Predicate` on the existing view,
     restores the remembered selection, and updates commands and hint on every
     keystroke. A filtered-out selection is visually cleared and remembered.

Existing card actions, icons, `GroupData` schema, double-click-to-edit, and
Enter-to-edit are unchanged.

---

## 3. Real UIA measurements

### 3.1 Same-item card height (criterion: 0 px difference)

| Card | Before | After | Difference |
|---|---|---|---|
| `Productivity` (selected → unselected) | 228 px | 228 px | **0.00** |
| `Dev Tools` (unselected → selected) | 188 px | 188 px | **0.00** |

`Productivity` and `Dev Tools` differ in height from each other (228 vs 188)
because they contain a different number of app pills. The criterion is the
same card before/after selection; both deltas are 0.

The self-test uses equal-content cards and measures
`Item0_Selected=150.40`, `Item1_Unselected=150.40`,
`Item0_Unselected=150.40`, `Item1_Selected=150.40`,
`Diff_SelectedVsUnselected=0.00`, `Diff_SameItem0=0.00`, `Diff_SameItem1=0.00`.

### 3.2 Keyboard navigation and scroll

| Action | Result | Scroll % |
|---|---|---|
| Start | index 0 selected | 0 |
| Down | index 0 → 1 | 0 (no jump) |
| End | index 13 | 100 (reveals the last card) |
| Home | index 0 | 0 |
| PageDown | index 2 | 0 |

Down/PageDown do not scroll or reflow when the next row is already visible;
End scrolls because it must reveal the off-screen last card.

### 3.3 Search "dev", clear, and duplicate

- Filter `dev`: **1 item**, selected index **−1**, Edit/Duplicate/Create
  Shortcut/Delete/Test all `IsEnabled=false`, New `IsEnabled=true`, hint visible.
- Clearing the filter restored index **13**, `Missing Apps` — the group selected
  before filtering (`matchesBefore=True`).
- Duplicate (from `Missing Apps`, index 13): group count **14 → 15**,
  `Missing Apps - Copy` selected at index **14**, `IsOffscreen=false`,
  scroll **99.24 %**. The new card is revealed.
- New: saved `Group 15` (dialog default), count **14 → 15** after the duplicate
  was removed, selected at index **14**, `IsOffscreen=false`, scroll **99.24 %**.

### 3.4 Delete selects the nearest survivor

- Selected `Group 15` at index **14** (last item), 15 groups.
- After Delete + confirmation: 14 groups, selected index **13**,
  `Missing Apps`, all five group commands enabled again (`nextSameIndex=True`).
- Empty library (one-group seed): Delete → **0 items**, all five group commands
  `IsEnabled=false`, New enabled, hint visible, **no exception**. With the empty
  list focused, Enter and Delete produced no dialog (window count stayed 1).

### 3.5 Edit preserves selection

- Selected `Design` at index **2**; opened Edit, renamed to `Design Renamed`,
  saved. Count stayed 14; selected index stayed **2**;
  `selectionPreserved=True`.

### 3.6 Minimum size, no selection (`900×650` px = `720×520` DIP)

Search `zzzznomatch`: 0 items, selected index −1, the five group commands
disabled, New enabled. The footer wraps instead of clipping:

| Control | UIA rect (L, T, W×H) | Right | Bottom | Inside window |
|---|---|---|---|---|
| Window | `(400, 48, 900×650)` | 1300 | 698 | — |
| Hint | `(534, 655, 269×19)` | 803 | 674 | **PASS** |
| New | `(416, 599, 138×40)` | 554 | 639 | PASS |
| Edit | `(562, 599, 82×40)` | 644 | 639 | PASS |
| Duplicate | `(652, 599, 120×40)` | 772 | 639 | PASS |
| Create Shortcut | `(780, 599, 164×40)` | 944 | 639 | PASS |
| Delete | `(416, 645, 100×40)` | 516 | 685 | PASS |
| Test | `(966, 621, 134×40)` | 1100 | 661 | PASS |
| Theme selector | `(1124, 621, 160×40)` | 1284 | 661 | PASS |

`Delete` wraps to a second footer row; the hint is fully visible below it and
nothing crosses the window edge (right 1300, bottom 698).

### 3.7 Disabled-state observability

Self-test `COMMAND_STATE`:
`EditEnabled=False`, `EditOpacity=0.45`, `EditUiaEnabled=False`,
`NewEnabled=True`, `HintVisible=Visible`,
`Hint='Select a group to edit, duplicate, or launch.'`. The same UIA
`IsEnabled=false` result was observed live for all five commands.

---

## 4. Acceptance criteria

- [x] Same `ListBoxItem` height before and after selection, difference 0 px.
      **REAL** at 1920×1080 / 125 %; the original 1536×864 hardware is
      unavailable (order 01 baseline).
- [x] Up/Down does not change the scroll offset or reflow the list; the selected
      row stays in view (Down 1→2, scroll 0; End scrolls only to reveal the last
      card).
- [x] With no selection, all five group-scoped commands are disabled
      (`IsEnabled=false`, opacity 0.45) and the inline hint is visible.
- [x] Selecting a group immediately enables the commands; deleting it selects the
      nearest remaining group and updates the state.
- [x] Search `dev` hides the selection (selected index −1, commands disabled);
      clearing the filter restores the remembered group. Filtering a group out
      leaves no visual selection.
- [x] Deleting the last group leaves an empty library with only New enabled and
      no exception (live one-group run + `EMPTY_LIBRARY` self-test).
- [x] New and Duplicate select and reveal the new card.
- [x] Disabled buttons report `IsEnabled = false` to UIA.
- [x] Build and self-test pass.

**Build:** `dotnet build src/XAppGroup.csproj -c Release` → **0 warnings,
0 errors**. **Self-test:** `out\XAppGroup.exe --selftest` → **39/39 PASS,
0 FAIL** (`out/selftest.log`). Six new checks: selection identity across
filter/clear/reload, delete-next selection, New/Duplicate reveal, footer command
state + UIA, equal card heights, empty library.

---

## 5. Verification commands

```powershell
dotnet build src/XAppGroup.csproj -c Release
out\XAppGroup.exe --selftest
Get-Content out\selftest.log | Select-String "SELECTION_FILTER|DELETE_SELECTION|NEW_DUPLICATE|COMMAND_STATE|CARD_HEIGHT|EMPTY_LIBRARY"
```

Live UIA scripts used (isolated `XAPPGROUP_DATA` directories, current machine):

- `C:\Users\RedStone\AppData\Local\Temp\opencode\verify-ux02-live2.ps1`
  — heights, keyboard, filter, duplicate.
- `...\verify-ux02-live3.ps1` — delete-nearest selection.
- `...\verify-ux02-minsize.ps1` — 900×650 footer/hint bounds.
- `...\verify-ux02-edit-nosel.ps1` — edit preserves selection; Enter/Delete
  no-op with no selection.
- `...\verify-ux02-empty.ps1` — empty-library delete and keyboard no-ops.

Test instances were stopped after each run. The user's tray instance
(PID 18556) was left running.

---

## 6. Files changed

- `src/Windows.cs` — `CollectionViewSource`/`ICollectionView` over the real
  collection, Id-based selection memory, `RestoreSelectionFromRemembered`,
  `SelectAndReveal`, `RemoveGroupSelectNearest`, `UpdateCommandState` /
  `SetCommandEnabled`, footer hint, list keyboard handling, Space selection,
  reserved card space (`Visibility.Hidden` launch/add cells).
- `src/SelfTest.cs` — six order-02 checks and their UIA/log evidence; the
  duplicate log line now captures the selection immediately after Duplicate
  instead of after the subsequent New call.
- `out/` — rebuilt `XAppGroup.exe`, `.dll`, `.pdb`, `.deps.json`,
  `.runtimeconfig.json`; refreshed `selftest.log` and preview PNGs.
- `Docs/ux-orders/README.md` — row 02 set to `In review`.
- `Docs/ux-orders/02-selection-and-command-states.md` — status `In review`.

---

## 7. Notes / residual risks

- **UIA list item name.** WPF still exposes each item's automation Name as
  `XAppGroup.GroupData`; the visible group label is the first `Text` descendant.
  This is a pre-existing accessibility gap outside order 02 (card/name
  accessibility belongs to order 07). Selection, `SelectionItemPattern`, scroll,
  and enabled states work correctly.
- **Different cards have different natural heights** (228 vs 188 px) because
  they contain different app-pill counts. Selection does not change an
  individual card's height (0.00 px both directions).
- **Selected trigger collapses the inactive launch button**, but the active
  launch button occupies the same fixed cell with identical padding/font
  metrics; the measured same-item height is unchanged. The add-app pill uses
  `Hidden`, not `Collapsed`.
- **Filter-hides-selection** intentionally leaves the list visually unselected
  while remembering the Id, per requirement 5; commands are disabled and the
  hint appears.
- **Running binary lock.** The user's tray instance (PID 18556) started from an
  earlier `out\XAppGroup.exe`. That file was renamed to
  `out\XAppGroup.exe.old-running` / `.dll.old-running` by an earlier update and
  is still locked while the instance runs. The current `out\XAppGroup.*` files
  are the new build. The `.old-running` files can be deleted after PID 18556
  exits.
- **Original 1536×864 physical review hardware** remains unavailable; all order
  02 evidence is REAL at 1920×1080 / 125 % with the same 896×740 DIP window.

---

## 8. Independent verification

- **Verdict: PASS** (2026-11-11). No defects raised.
- Clean rebuild (`dotnet build -t:Rebuild`) → 0 warnings / 0 errors; the rebuilt
  DLL was SHA-256-identical to `out\XAppGroup.dll`.
- Self-test re-run in a fresh isolated `XAPPGROUP_DATA` directory → 39 PASS /
  0 FAIL; all six order-02 `INFO` evidence lines present and consistent
  (`NEW_DUPLICATE` now shows `SelectedAfterDup == DupId`).
- Independent live UIA re-measured same-item heights (0.00 px on three cards),
  Down/End/Home/PageDown, filter/restore, Delete middle and last, New/Duplicate
  reveal, edit rename, empty library, and 900×650 no-selection footer bounds.
  All matched the acceptance criteria; all controls and the hint stayed inside
  the window.
- Constraints re-checked: `ListBox` and its patterns retained; card action tags,
  icons, and tooltips identical to the previous binary; `GroupData` persisted
  schema unchanged (selection memory lives only in `MainWindow`); double-click
  and Enter still open the edit dialog.
- Non-blocking pre-existing note confirmed: UIA item Name remains
  `XAppGroup.GroupData` (order 07 accessibility scope).
