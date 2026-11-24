# XAppGroup — Live UI/UX Review

**Date:** 2026-11-08
**Build:** Release, `src/XAppGroup.csproj` (`.NET 10`, WPF + Windows Forms)
**Artifact:** `out/XAppGroup.exe` (built 16:10, self-test 16:47)
**Self-test baseline:** 33 PASS / 0 FAIL (`out/selftest.log`)
**Reviewer role:** senior development manager / product designer

This document is the evidence base for the 15 orders in this folder. It records
what was observed at runtime, what was read in source, and what remains
unverified. It is not a wish list; every finding has an owner order.

---

## 1. Environment and method

| Item | Value |
|---|---|
| Screen | 1536 × 864 px |
| Windows work area | 1536 × 816 px (48 px taskbar) |
| Display scaling | 125% |
| OS theme during capture | Dark; `Theme` setting restored to `System` afterwards |
| Invocation | `out\XAppGroup.exe`, single instance |
| Live data | 17 groups, 198 launchers, 3 distinct group names |

Evidence was collected with:

- Windows UI Automation (UIA) tree and bounding rectangles.
- Windows OCR over screenshots.
- Screenshot pixel sampling for colors, borders, and hit areas.
- Headless self-test and source inspection.

**Limitation:** the reviewer could not view images directly. All visual claims
below are backed by UIA geometry, OCR text, or sampled pixel values. Anything
that could not be observed this way is marked **(code)** rather than
**(runtime)**. The light-theme hover state could not be confirmed at runtime
(a hover test produced no measurable change); light-theme findings are
code-based unless stated otherwise.

---

## 2. Executive summary

The app is functionally alive and visually close to the approved mock in dark
mode. The blocking problems are not cosmetic:

1. **The main window is taller than the screen at 125%.** The footer command bar
   — New, Edit, Duplicate, Create Shortcut, Delete, Test, theme selector — is
   entirely below the bottom edge. The theme dropdown cannot be reached with a
   mouse. This is a P0 release blocker.
2. **Selection changes card height by ~40 px**, so selecting a card reflows the
   whole list and moves the target under the pointer.
3. **The popup launcher lays out one fewer column than configured**, opens
   off-screen in common positions, and cannot scroll when a group is tall.
4. **Several controls do not do what their icon/tooltip says.** The pin icon
   opens the popup; the ellipsis opens Edit; "Configure Hotkeys" opens a dialog
   with no hotkey control; no hotkey is registered anywhere.
5. **The app is effectively mouse-only.** Custom `Border` controls expose no
   Button/Invoke patterns, focus visuals are removed, and UIA names are type
   names (`XAppGroup.GroupData`, `XAppGroup.AppPillItem`).
6. **Light theme and contrast are incomplete.** System mode does not follow the
   OS, several colors are hard-coded dark, and multiple text/background pairs
   fail WCAG AA.

The work is split so that layout and truthfulness are fixed before polish.
Do not start theming or motion work before the P0 layout/action orders are
accepted.

---

## 3. Findings

Severity: **S1** blocks use, **S2** serious, **S3** polish.

### 3.1 Layout and window fit

| ID | Sev | Finding | Evidence | Order |
|---|---|---|---|---|
| F-01 | S1 | Main window is 896×740 DIP = 1120×925 px at 125%. Work area is 816 px tall, so 109 px is off-screen. | Runtime UIA: window rect bottom 925 > 816. | 01 |
| F-02 | S1 | Footer commands sit at Y≈879–913 px, fully below the screen. Theme ComboBox at (1459, 880) is unreachable by mouse. | Runtime UIA bounding rects; OCR. | 01 |
| F-03 | S2 | The window uses a fixed `Height = 740` with `MinHeight = 520` and no work-area clamp; startup is always `CenterScreen`. | Code: `Windows.cs` `MainWindow` ctor. | 01 |
| F-04 | S2 | Footer is a `WrapPanel` inside a `DockPanel`, but the window is too tall to see it; there is no height breakpoint that collapses the tip banner or compacts the ribbon. | Runtime + code `BuildFooterCommandBar`, `BuildProTipBanner`. | 01 |
| F-05 | S3 | Window size/position/maximized state are not persisted; every launch is centered at the fixed size. | Code; no settings fields. | 12 |

### 3.2 Selection and command state

| ID | Sev | Finding | Evidence | Order |
|---|---|---|---|---|
| F-06 | S1 | Selecting a card changes its height from 236 px to 276 px (≈32 DIP), reflowing the list and moving subsequent cards. Cause: selected-only controls (`Launch All`, `Add App`) appear and add a row. | Runtime UIA: measured selected vs unselected item heights. | 02 |
| F-07 | S1 | With no selection, Edit/Duplicate/Create Shortcut/Delete/Test silently do nothing (all guard `Sel == null; return`). There is no disabled state or message. | Code `Edit`, `Duplicate`, `Shortcut`, `Delete`, `Test`. | 02 |
| F-08 | S2 | Search reassigns `_list.ItemsSource` to a new list, which drops selection; clearing search does not restore it. Delete also leaves no selection. | Code `ApplySearchFilter`, `Delete`. | 02 |
| F-09 | S2 | The list has no placeholder when it is empty; a first run shows a blank content area under the ribbons. | Code `ItemTemplate`; no empty-state visual. | 10 |
| F-10 | S2 | Footer command enabled state never updates with selection; buttons always look active. | Code; no `CanExecute`/`IsEnabled` binding. | 02 |

### 3.3 Card actions and affordances

| ID | Sev | Finding | Evidence | Order |
|---|---|---|---|---|
| F-11 | S1 | Pin icon (`Lucide.Pin`, tooltip "Open Group Popup Launcher") opens the popup. It does not pin. The mock's pin means "Pin to Windows Taskbar". | Runtime click + code `Tag = "CardPopup"`. | 03 |
| F-12 | S1 | Ellipsis (`MoreHorizontal`, tooltip "Edit this Group") opens the Edit dialog directly. A "more" affordance is expected to open a menu. | Runtime click + code `Tag = "CardEdit"`. | 03 |
| F-13 | S2 | "Add App" opens the full Edit Group dialog with no focus/scroll to Applications; it is indistinguishable from Edit. | Code `Tag = "CardAddApp"` → `EditGroup`. | 03, 05 |
| F-14 | S2 | App pills are chips with `Cursor = Hand` that launch immediately on click; the mock shows `cursor-grab` draggable chips. Affordance and behavior disagree. | Code `CardAppPill`; runtime click. | 03 |
| F-15 | S2 | Only the 48×48 group icon starts drag-to-taskbar. The banner says "Drag any group card", which is false. | Code `GroupIconHandle`; runtime. | 03, 13 |
| F-16 | S3 | "Launch All" appears only on the selected card, causing F-06 and making the primary action invisible at rest. | Runtime + templates. | 02, 03 |
| F-17 | S3 | Icon-only buttons have tooltips but no accessible names/patterns; they are `Border` + mouse handlers. | Runtime UIA: no Invoke pattern. | 03, 07 |

### 3.4 Popup launcher

| ID | Sev | Finding | Evidence | Order |
|---|---|---|---|---|
| F-18 | S1 | Configured 4 columns renders 3 per row. `WrapPanel.Width = cols × 88`, but each item is 96 wide (80 StackPanel + 8 margin + 8 border padding). Affects every column count. | Runtime UIA: item X = 582/702/822; next row starts Y=187. | 04 |
| F-19 | S1 | Popup always opens above the cursor; if there is no room it clamps to the top instead of flipping below. No work-area height clamp. | Code `Position()`; runtime. | 04 |
| F-20 | S1 | No `ScrollViewer`; tall groups extend past the screen and "Launch All" is clipped. | Code `PopupWindow`; runtime with 17+ app group. | 04 |
| F-21 | S2 | Labels are constrained to 80 DIP and rendered "ChatGPT - Sho..." for a normal shortcut name. | Runtime OCR/UIA text. | 04 |
| F-22 | S2 | Keyboard support is limited to Esc and 1–9; no arrow navigation, no initial focus, no visible focus ring. | Code `KeyDown`; runtime. | 04, 07 |
| F-23 | S3 | Popup closes on `Deactivated`; clicking the parent window or another app dismisses it, which is fine, but there is no explicit close affordance for touch. | Code. | 04 |

### 3.5 Edit Group dialog

| ID | Sev | Finding | Evidence | Order |
|---|---|---|---|---|
| F-24 | S2 | Standard OS title bar on a `WindowStyle` default window, while the main window uses custom chrome; in dark mode this is a bright white bar. | Runtime + code `EditWindow` (no `WindowStyle`). | 05 |
| F-25 | S2 | Default WPF `ComboBox` and `CheckBox` controls appear inside the themed dialog; the columns/sort dropdowns are unstyled. | Runtime screenshot/OCR + code `_cols`, `_sort`. | 05 |
| F-26 | S2 | No duplicate-name validation; empty name only. Save mutates the model. No dirty-state prompt on Cancel/Esc. | Code `EditWindow.Save`. | 05 |
| F-27 | S2 | Apps list exposes full file paths as item names to UIA; missing files are not marked; no multi-select; no keyboard delete. | Runtime UIA names; code `NameConv`/`ItemTpl`. | 05, 07 |
| F-28 | S3 | Fixed `Width = 500`, `ResizeMode = NoResize`, `SizeToContent.Height`; long icon paths and large app counts can overflow vertically on small screens. | Code. | 05 |
| F-29 | S3 | No hotkey UI, yet the main window links here from "Configure Hotkeys". | Code; runtime click. | 05, 08 |

### 3.6 Theme and contrast

| ID | Sev | Finding | Evidence | Order |
|---|---|---|---|---|
| F-30 | S2 | "System (Mica)" is not system-aware: `Theme.Apply` treats anything except "Light" as dark, and `Dark = !light`. It never reads the OS light/dark setting and never reacts to changes. | Code `Theme.Apply`; runtime with OS light. | 06 |
| F-31 | S2 | Hard-coded dark colors bypass tokens: card hover `#D92C344A`, ComboBox item foreground `#F1F5F9`, scrollbar thumb `#28FFFFFF`, dark radial glows, dark tip gradient. | Code `BuildCardContainerStyle`, `BuildThemedDropdown`, `BuildSleekScrollBarStyle`, `MainWindow` ctor. | 06 |
| F-32 | S2 | No real Mica/Acrylic backdrop; the "Mica" label is a painted gradient. | Code; runtime pixel sampling. | 06 |
| F-33 | S2 | Contrast failures: white 12 px on `Launch All` gradient endpoint `#06B6D4` ≈ 2.42:1; `Muted #64748B` on card `#202636` ≈ 3.1:1; scrollbar thumb ≈ 1.4:1 at 6 px. | Pixel sampling + WCAG calculation. | 06 |
| F-34 | S3 | Scrollbar thumb is 6 px wide; hard to acquire with a mouse and fails non-text contrast. | Code `BuildSleekScrollBarStyle`. | 06 |
| F-35 | S3 | Native context menus, tooltips, and file dialogs do not follow the app theme. | Runtime + code. | 06 |

### 3.7 Keyboard and accessibility

| ID | Sev | Finding | Evidence | Order |
|---|---|---|---|---|
| F-36 | S1 | Traffic-light close/minimize/maximize are 12×12 `Border`s with no Button pattern, no accessible name, no keyboard focus. | Runtime UIA: no Close/Minimize/Maximize buttons. | 07 |
| F-37 | S1 | Card action chips and app pills expose no Button/Invoke pattern; they are mouse-only. | Runtime UIA. | 03, 07 |
| F-38 | S2 | `FocusVisualStyle = null` on ListBoxItem; custom button template has no focus visual. Keyboard users cannot see focus. | Code. | 07 |
| F-39 | S2 | UIA names are CLR type names: `XAppGroup.GroupData` for cards, `XAppGroup.AppPillItem` for pills, full path for edit items. ComboBoxes and the search box have empty names. | Runtime UIA. | 07 |
| F-40 | S2 | No keyboard path to card actions; Enter edits, Delete deletes, but Launch/Duplicate/Shortcut/popup require the mouse. | Code/runtime. | 07 |
| F-41 | S3 | Text as small as 10.5–11.5 px, with `Segoe UI Variable Display` at those sizes; legibility suffers at 100%. | Code; OCR. | 06, 07 |

### 3.8 Truthfulness, feedback, and destructive actions

| ID | Sev | Finding | Evidence | Order |
|---|---|---|---|---|
| F-42 | S1 | The hotkey badge shows `Win + Shift + {GroupCol}` for every group, "Configure Hotkeys" opens Edit, and there is no `RegisterHotKey` anywhere. The feature is fake. | Code `HotkeyBadge`, no P/Invoke; runtime. | 08 |
| F-43 | S2 | `Launcher.Run` swallows every exception into `Debug.WriteLine`; a missing/broken app produces no user feedback. | Code `Core.cs` `Launcher.Run`. | 09 |
| F-44 | S2 | No missing-file indicators on cards, pills, popup items, or the edit list. | Code; UI has no state. | 09 |
| F-45 | S2 | Delete confirmation defaults to **Yes**; after deleting it shows a second blocking MessageBox claiming the group was "unpinned from taskbar" even if it was never pinned. | Code `Delete`; runtime. | 09 |
| F-46 | S2 | `RunAll` launches sequentially with no summary; partial failure is invisible. | Code. | 09 |
| F-47 | S2 | Shortcut creation uses a blocking success MessageBox with a raw path; failures show only `ex.Message`. | Code `Shortcut`. | 09 |
| F-48 | S2 | Config load failure is swallowed and the app starts with an empty settings object, which can look like data loss. | Code `Store.Load`. | 09, 11 |
| F-49 | S3 | No undo/toast system for delete, duplicate, or shortcut creation. | Code. | 09 |
| F-50 | S3 | The "Dismiss" on the Pro Tip banner is not persisted; it returns every launch. | Code `banner.Visibility`. | 09 |

### 3.9 Search and empty states

| ID | Sev | Finding | Evidence | Order |
|---|---|---|---|---|
| F-51 | S2 | No clear (×) button; Esc clears but is undiscoverable. | Runtime. | 10 |
| F-52 | S2 | No no-results state; the list simply goes blank. | Runtime with a nonsense query. | 10 |
| F-53 | S2 | Search only matches group name and file name; no result count, no "showing X of Y". | Code `ApplySearchFilter`. | 10 |
| F-54 | S3 | `_list.ItemsSource` is replaced on every filter change, defeating the collection view and selection. | Code. | 02, 10 |
| F-55 | S3 | Stats always show global totals while the list is filtered; users cannot tell what the numbers refer to. | Code `UpdateStats`; runtime. | 10 |

### 3.10 Group lifecycle and data integrity

| ID | Sev | Finding | Evidence | Order |
|---|---|---|---|---|
| F-56 | S1 | Live config: 17 groups, 198 launchers, but only 3 distinct names: 15 × `abdou - Copy`, 1 × `abdou`, 1 × `Group 1`. Duplicate names are allowed and Duplicate appends ` - Copy` indefinitely. | `%APPDATA%\XAppGroup\appgroups.json`; runtime stats. | 11 |
| F-57 | S2 | No group reordering. The list order is persisted but cannot be changed in the UI. | Code; no drag/reorder handlers. | 11 |
| F-58 | S2 | Delete removes the group and calls `Unpin`/`DeleteShortcut`, but does not clean generated icons or verify results; failures are silent. | Code `Delete`; `Launcher.DeleteShortcut`. | 09, 11 |
| F-59 | S2 | `Store.Save` writes directly to the live file (no temp + atomic replace, no backup). A crash mid-write can corrupt the only config. | Code `Store.Save`. | 11 |
| F-60 | S3 | Duplicate copies everything including layout and paths but cannot produce a unique name; a duplicated group with a hotkey would need the hotkey cleared (after order 08). | Code `Duplicate`. | 11 |

### 3.11 Window chrome, tray, and session

| ID | Sev | Finding | Evidence | Order |
|---|---|---|---|---|
| F-61 | S2 | Closing the main window always hides to tray (`e.Cancel = true`). There is no Quit command in the main UI; the only Exit is the tray context menu. | Code `Program.cs`; runtime. | 12 |
| F-62 | S2 | The close tooltip says "Close to System Tray", but the green and yellow traffic lights are 12 px and easy to miss; the title bar has no right-click system menu (`CaptionHeight = 0`). | Code/runtime. | 12 |
| F-63 | S2 | First close shows a balloon tip only once; after that the app can appear to vanish. | Code `firstHideNotice`. | 12 |
| F-64 | S3 | Minimize/maximize/close state and last monitor are not restored; a window restored on a disconnected monitor could be off-screen. | Code. | 12 |

### 3.12 Copy and information architecture

| ID | Sev | Finding | Evidence | Order |
|---|---|---|---|---|
| F-65 | S2 | Hard-coded "v2.4 PRO" badge with no relationship to the assembly version. | Code `BuildTitleBar`. | 13 |
| F-66 | S2 | "Background Daemon Active" claims a daemon that does not exist; the app is a tray-resident WPF process. | Code `BuildStatsRibbon`. | 13 |
| F-67 | S2 | "Refresh Groups & Icons" only calls `InvalidateIcons()`; it does not reload groups from disk. | Code. | 13 |
| F-68 | S2 | Gear icon tooltip "Edit Selected Group" opens Edit; it is a settings icon and users expect app settings. | Code/runtime. | 13 |
| F-69 | S2 | Theme names "System (Mica)" / "Dark Acrylic" / "Light Glass" promise backdrops that are not implemented (see F-32). | Code. | 06, 13 |
| F-70 | S2 | "Test Runner" opens a popup launcher; it is not a test runner. | Code `Test()`. | 13 |
| F-71 | S3 | "Create Shortcut" only creates a desktop shortcut; the label implies a choice. | Code `Shortcut`. | 13 |
| F-72 | S3 | "Pro Tip: Drag any group card" is false (only the icon drags). | Code; runtime. | 03, 13 |
| F-73 | S3 | Empty stats read "0 App Groups" / "0 Configured Launchers" without singular/plural handling. | Code `UpdateStats`. | 13 |

### 3.13 Performance and motion

| ID | Sev | Finding | Evidence | Order |
|---|---|---|---|---|
| F-74 | S2 | `ScrollViewer.CanContentScroll = false` disables item virtualization; every card (and its `DropShadowEffect`) is realized. With 198 launchers this is unnecessary work. | Code `MainWindow` ctor. | 14 |
| F-75 | S2 | Every card carries a 24 px blur `DropShadowEffect`, and the selected card swaps in a 28 px colored shadow; selection can cause GPU/CPU spikes. | Code `BuildCardContainerStyle`. | 14 |
| F-76 | S2 | `SelectionChanged` calls `JumpListHelper.Update(Sel)` synchronously, which writes jump-list state on every arrow-key repeat. | Code. | 14 |
| F-77 | S3 | No reduced-motion handling; there are currently no purposeful transitions, but any added hover/popup animation must honor system settings. | Code. | 14 |
| F-78 | S3 | Startup rebuilds icons and jump lists synchronously; no lazy/placeholder path for many groups. | Code; runtime startup. | 14 |

### 3.14 First run, help, import/export

| ID | Sev | Finding | Evidence | Order |
|---|---|---|---|---|
| F-79 | S2 | First run with no config shows a blank list and no explanation, sample, or onboarding. | Runtime with empty `XAPPGROUP_DATA`. | 15 |
| F-80 | S2 | No Help, About, or keyboard-shortcut reference in the UI. | Runtime; no commands. | 13, 15 |
| F-81 | S2 | No import/export or backup/restore. The only config is a single JSON file with no recovery UI. | Code `Store`; runtime. | 15 |
| F-82 | S3 | No link to the data folder, so users cannot inspect or back up their groups. | Code. | 13, 15 |

---

## 4. Verified vs. code-based

| Claim | Status |
|---|---|
| Footer is off-screen; theme dropdown unreachable | **Runtime verified** (UIA rects) |
| Selected card grows and reflows list | **Runtime verified** (236 → 276 px) |
| Popup renders 3 columns for a 4-column setting | **Runtime verified** (UIA X positions) |
| Popup opens above cursor and clips tall groups | **Runtime verified** |
| Pin/ellipsis/Add App actions differ from their labels | **Runtime verified** (click behavior) |
| Hotkey badge is fake; no registration | **Runtime + source verified** |
| UIA lacks traffic-light buttons and card Invoke patterns | **Runtime verified** |
| Duplicate-name flood | **Runtime/config verified** (17 groups, 3 names) |
| Contrast failures | **Pixel sampling + WCAG math** |
| System mode does not follow OS | **Source verified**; OS-light runtime check attempted |
| Light-theme hover is still dark | **Source verified**; runtime hover test inconclusive |
| Virtualization disabled | **Source verified** |
| No import/export/backup | **Runtime + source verified** |

---

## 5. Baseline metrics to preserve or improve

| Metric | Baseline | Target |
|---|---|---|
| Build warnings/errors | 0 / 0 | 0 / 0 |
| Self-test | 33 PASS / 0 FAIL | ≥33 PASS / 0 FAIL |
| Main window bottom at 125% | 925 px (off-screen) | ≤816 px work area |
| Footer reachability | 0 of 7 commands | 7 of 7 |
| Selected vs unselected card height | +40 px | 0 px |
| Popup columns for `GroupCol = 4` | 3 | 4 |
| Popup item area clipped for tall group | yes | never (scrolls) |
| Actionable UIA elements without name/pattern | many | 0 |
| Contrast (body text) | 3.1:1 in places | ≥4.5:1 |
| Config writes per selection | 1 jump-list write | 0 on repeat, debounced |

---

## 6. Re-test script (used to accept the final state)

Run this after order 15, and spot-check it after each order.

1. Set `XAPPGROUP_DATA` to a clean folder, launch the app. Confirm the window is
   fully on-screen, the footer is visible, and the empty/first-run state is
   understandable.
2. Create a group with 10 apps, including one missing path and one very long
   name. Save. Confirm no duplicate name can be created.
3. Select cards with mouse and keyboard. Confirm no reflow, visible focus, and
   correct enabled/disabled command states.
4. Open the popup with columns 1, 2, 4, and 8 from the top, middle, and bottom
   of the screen. Confirm exact column counts, no clipping, and scrolling.
5. Launch an app, a missing app, and Launch All. Confirm toasts and a partial
   failure summary.
6. Delete a group with the keyboard. Confirm the default is Cancel, only one
   confirmation appears, and Undo works.
7. Switch Dark → Light → System, and toggle the OS theme while running. Confirm
   every window and menu follows; sample the previously failing contrast pairs.
8. Tab through the entire main window and popup. Confirm every action is
   reachable, named, and has a focus ring. Run Accessibility Insights.
9. Bind a hotkey to a group, minimize to tray, and trigger it. Confirm the popup
   opens; create a conflict and confirm a clear error.
10. Restart the app. Confirm size, position, theme, selection-independent data,
    and hotkeys survive.
11. Import a config, export it, restore a backup. Confirm a pre-import backup and
    a readable validation error for malformed JSON.
12. Run `out\XAppGroup.exe --selftest` and confirm ≥33 PASS / 0 FAIL.

---

## 7. Notes for the Coder

- Fix the P0 chain (01 → 02 → 03 → 04) before touching theme or motion. A
  beautiful window that is still off-screen is not progress.
- Do not "fix" an action mismatch by changing the tooltip to match the wrong
  behavior. The order states which behavior is correct.
- Do not remove a feature to make a test pass. If a feature cannot be delivered
  honestly, remove its promise and say so in the report.
- Runtime evidence beats source assumptions. If the order's expected geometry
  differs from what you measure, report the measurement and ask before changing
  the acceptance target.
