# Report: Order 03 — Card actions: make the icon match the action

- **Date:** 2026-11-11
- **Order / Task:** `Docs/ux-orders/03-card-action-semantics.md` (Findings `F-11`, `F-12`, `F-13`, `F-14`, `F-15`, `F-16`, `F-17`, `F-72`)
- **Status:** In review (independent verification pending)

---

## 1. Environment used for verification

| Item | Value |
|---|---|
| Display | 1920×1080, 125 % scaling (work area `(0,0)`–`(1920,1020)` px) |
| Process DPI awareness | **PerMonitorV2** (`GetAwarenessFromDpiAwarenessContext` = 2) |
| Window DPI | 120 (1.25×) |
| Main window under test | `(400, 48, 1120×925)` px = **896×740 DIP** |
| OS / runtime | Windows, .NET 10 WPF |
| Test data | 14-group seed + one appended `Empty Card` group, isolated `XAPPGROUP_DATA` directory |

The original review resolution (1536×864) is unavailable on this machine, as
already documented in the accepted order 01/02 reports. All numbers below are
**REAL** measurements at 125 % on 1920×1080, taken from the live UI with UI
Automation on the final build. The window is the same 896×740 DIP design size
used by the review.

---

## 2. What changed

1. **Group icon (48×48) is now a real `Button`.**
   - Left click (no drag) opens the popup launcher.
   - A completed left drag past the 4 px threshold calls the existing
     `ShellDragDrop.DoDragLink` pin flow and suppresses the mouse-up click; no
     popup opens after a drag.
   - Tooltip `Open launcher. Drag to pin to the taskbar or desktop.`, UIA name
     `Open launcher for {group}. Drag to pin.`
2. **Dedicated launcher button** (32×32 DIP, `Lucide.LayoutGrid`) replaces the
   pin glyph. Tooltip `Open group launcher`, UIA name
   `Open launcher for {group}`. `Lucide.Pin` is deleted from the icon set; the
   card action row contains no pin glyph and no fake pin command.
3. **Ellipsis is a real menu**, themed `ContextMenu` anchored to the button with
   exactly (in order):
   1. `Open launcher`
   2. `Edit group…`
   3. `Add apps…`
   4. `Duplicate group`
   5. `Create desktop shortcut`
   6. separator
   7. `Delete group…` (danger styling)
   Opens on left click, `Enter`, `Shift+F10` and the context-menu key; `Esc`
   closes it and returns focus to the button that opened it.
4. **Primary launch button is always present** on every card, selected or not.
   Selected uses the filled gradient; unselected uses the glass/secondary style.
   The two variants occupy the same reserved cell, so card height is unchanged.
5. **Zero-app cards** replace the launch action with one primary dashed
   `Add apps` control in the launch slot. The redundant dashed `Add App` pill is
   hidden for empty groups, so each empty card shows exactly one Add apps control.
6. **“+ Add App” / “Add apps…”** open `EditWindow` with `applicationsFocus: true`:
   title `Edit Group — Applications`, section
   `APPLICATIONS — ADD OR MANAGE`, Applications list visible without scrolling,
   `Add applications` button focused. A re-entrancy guard prevents a second
   dialog when invoked twice.
7. **App pills are real `Button`s** with `Cursors.Hand`, full-path/missing-file
   tooltip, launch on click, and a themed four-item context menu:
   `Open`, `Run as administrator`, `Open file location`, `Copy path`.
   Removal stays in Edit.
8. **Card body** keeps selection and double-click/`Enter` to Edit; right-click
   and `Shift+F10` open the same six-item menu. Card `Buttons` are excluded from
   the card-body double-click/Enter path.
9. Pressed (`IsPressed`, opacity 0.7) triggers added to the icon, chrome and
   dashed-pill templates so pressed states are visible in both themes.

---

## 3. Real UIA measurements

### 3.1 Action row — selected `Productivity` card

Card rect `(432,174,1035,228)`; control sizes are DIP = px / 1.25.

| Control | UIA name | Rect (L, T, W×H) px | DIP | Invoke |
|---|---|---|---|---|
| Group icon | `Open launcher for Productivity. Drag to pin.` | `458,198,60×60` | 48×48 | **True** |
| Primary launch | `Launch all apps in Productivity` | `1210,198,131×40` | 104.8×32 | **True** |
| Launcher | `Open launcher for Productivity` | `1351,198,40×40` | 32×32 | **True** |
| More options | `More options for Productivity` | `1401,198,40×40` | 32×32 | **True** |
| Add app pill | `Add apps to Productivity` | `458,341,103×32` | 82.4×25.6 | **True** |
| App pill | `Launch notepad` | `458,293,118×40` | 94.4×32 | **True** |

All five card-action controls expose the UIA `Invoke` pattern and a name that
describes the action. Unselected card `Games` (`IsSelected=False`) shows the same
launch/launcher/more row (`Launch all apps in Games` `1231,1486,110×40`), proving
launch is not selection-only. The Add app pill is reserved (`Hidden`) on
unselected cards per order 02 and appears when the card is selected.

Full action-row dump: `C:\Users\RedStone\AppData\Local\Temp\opencode\ux03_final_row.ps1`.

### 3.2 Card menu — content, keyboard, dismissal

```
MENU Rect=1401,238,258,239
  MenuItem 'Open launcher'
  MenuItem 'Edit group…'
  MenuItem 'Add apps…'
  MenuItem 'Duplicate group'
  MenuItem 'Create desktop shortcut'
  Separator
  MenuItem 'Delete group…'
```

- Left click on `More options for Productivity` → this menu, **not** Edit.
- `Enter` on the focused More button → same 6-item menu.
- `Shift+F10` on the focused card body → same 6-item menu.
- Right click on the card body → same 6-item menu.
- `Esc` (with a menu item focused) → menu closed, focus returned to
  `More options for Productivity` (`menuOpen=False`).
- `Edit group…` → normal `Edit Group` dialog, section
  `APPLICATIONS (DRAG & DROP FILES HERE)`.
- `Open launcher` → launcher popup (492×502), no Edit window.
- `Enter` on the card body → normal `Edit Group` dialog (not Applications mode).

Screenshot: `out/ux03_card_menu.png`.

### 3.3 App-pill menu and activation

- Right click and `Shift+F10` both produce exactly:

```
MENU Items=4 Separators=0
  'Open | Run as administrator | Open file location | Copy path'
```

- `Esc` closes the menu and focus returns to `Launch notepad`.
- Left click on the pill launches the app (`Cursors.Hand`; pointer assigned in
  the template).
- No remove/delete entry exists in the pill menu.

Screenshot: `out/ux03_pill_menu.png`.

### 3.4 Group icon: click vs drag

| Test | Result |
|---|---|
| Drag from icon center `(488,228)` by 36×30 px (~29×24 DIP, > 4 px threshold) | **0 new windows** after mouse-up; process still `Responding=True`; shell drag ran |
| First click after the drag | Launcher popup `492×502` opened **once**, plus a transient tooltip `Popup` 71×32 |
| Popup closed | Only the main window remained (window count 1); no exception |

No popup is opened by the drag itself, and the drag does not break the
subsequent click. `Esc` during the shell drag cancels via the existing helper
(unchanged, no exception observed).

### 3.5 Add apps → Applications mode

| Entry point | Dialog title | Section | Applications list | Focus |
|---|---|---|---|---|
| Menu `Add apps…` | `Edit Group — Applications` | `APPLICATIONS — ADD OR MANAGE` | `Applications 682,390,401×275` visible/selected 0 | `Add applications` (`EditAddAppBtn`) |
| Card `Add apps to Productivity` | `Edit Group — Applications` | `APPLICATIONS — ADD OR MANAGE` | `Applications` visible | `Add applications` (`EditAddAppBtn`) |
| Empty card `Add apps to Empty Card` | `Edit Group — Applications` | `APPLICATIONS — ADD OR MANAGE` | `Applications` visible | `Add applications` |
| Double-invoke Add apps | exactly **one** `Edit Group — Applications` window | — | — | — |

### 3.6 Zero-app card

Card `Empty Card` rect `(432,676,1035,188)`:

| Control | Name | Rect px | Visible | Invoke |
|---|---|---|---|---|
| Group icon | `Open launcher for Empty Card. Drag to pin.` | `458,676,60×60` | yes | True |
| Empty primary | `Add apps to Empty Card` | `1234,676,107×40` | yes | True |
| Launcher | `Open launcher for Empty Card` | `1351,676,40×40` | yes | True |
| More options | `More options for Empty Card` | `1401,676,40×40` | yes | True |
| Launch all apps | — | — | **0 controls** | — |

`EMPTY_ADD_TOTAL=1`, `EMPTY_ADD_VISIBLE=1`, `EMPTY_LAUNCH_TOTAL=0`;
tooltip `Add applications to this group`. Clicking it opens Applications mode.

Screenshot: `out/ux03_empty_card.png`.

---

## 4. Acceptance criteria

- [x] No pin glyph remains in the card action row; the `Lucide.LayoutGrid`
      launcher glyph opens the popup and is labeled
      `Open group launcher` / `Open launcher for {group}`.
- [x] Clicking the group icon opens the popup; dragging it > 4 px starts the
      taskbar/desktop pin drag and does **not** open the popup on release
      (36×30 px drag → 0 new windows; next click opens one popup).
- [x] Clicking the ellipsis opens the six-item menu and does not open Edit;
      clicking `Edit group…` opens Edit.
- [x] The menu opens with `Shift+F10` and the context-menu key when a card or
      the More button is focused; `Esc` closes it and focus returns to the
      originating button.
- [x] `Add apps…` and the Add app pill open Edit with the Applications list
      focused and visible without scrolling the dialog first; title
      `Edit Group — Applications` distinguishes it from generic Edit.
- [x] Launch is visible on every card, including unselected cards, and does not
      change card height between states (`CARD_HEIGHT` diff 0.00 px, including
      an empty card: 144.00 px both states).
- [x] Clicking an app pill launches it; right-click/`Shift+F10` opens the
      four-item menu; cursor is `Hand`, not a drag cursor.
- [x] No action opens the Edit dialog except `Edit group…`, `Add apps…`, the Add
      app pill, and the pre-existing card double-click/`Enter`.
- [x] UIA exposes an Invoke pattern and a descriptive name for the launcher,
      ellipsis, launch, add-app and app-pill controls (table 3.1/3.6).
- [x] Build and self-test pass.

### Self-test evidence (final `out` build)

`out\XAppGroup.exe --selftest` → **44 PASS / 0 FAIL**.

```
INFO CARD_HEIGHT Item0_Selected=150.40 Item1_Unselected=150.40 Item0_Unselected=150.40 Item1_Selected=150.40 Empty_Selected=144.00 Empty_Unselected=144.00 Diff_SelectedVsUnselected=0.00 Diff_SameItem0=0.00 Diff_SameItem1=0.00 Diff_Empty=0.00
INFO CARD_ACTIONS Icon='Open launcher for Card A. Drag to pin.' Launch='Launch all apps in Card A' Launcher='Open launcher for Card A' More='More options for Card A' Pill='Launch notepad' Add='Add apps to Card A' Invoke=True NoPinConst=True Glyph=True IconClickPopup=True
INFO CARD_MENU Count=7 Names='Open launcher | Edit group… | Add apps… | Duplicate group | Create desktop shortcut | <separator> | Delete group…' OrderOk=True NoPin=True Danger=True Themed=True
INFO PILL_MENU ItemCount=4 Names='Open | Run as administrator | Open file location | Copy path' Matches=True
INFO EMPTY_CARD AddVisible=Visible LaunchHost=Hidden AddPill=Hidden VisibleAddButtons=1 AddName='Add apps to Empty Card' Invoke=True
INFO EDIT_APPS Title='Edit Group — Applications' AddName='Add applications' ListVisible=Visible ListHeight=220.0 ListSelected=0 AddFocused=True
```

Five new checks (`CARD_ACTIONS`, `CARD_MENU`, `PILL_MENU`, `EMPTY_CARD`,
`EDIT_APPS`) plus the empty card in `CARD_HEIGHT`.

---

## 5. Verification commands

```powershell
dotnet build src/XAppGroup.csproj -c Release
$env:XAPPGROUP_DATA = "C:\Users\RedStone\AppData\Local\Temp\opencode\xappgroup-ux03-selftest"
out\XAppGroup.exe --selftest
Get-Content out\selftest.log | Select-String "CARD_ACTIONS|CARD_MENU|PILL_MENU|EMPTY_CARD|EDIT_APPS|CARD_HEIGHT|^FAIL"
```

Live UIA scripts used (isolated `XAPPGROUP_DATA` directories, current machine):

- `...\ux03_verify_live.ps1` — action row, card menu, pill menu, Edit/Add apps,
  empty card, screenshots.
- `...\ux03_live2.ps1` — keyboard menus on card body/pill, Add apps focus,
  empty-card Applications mode.
- `...\ux03_live3.ps1` — `Enter` on More, `Esc` focus return, card-body `Enter`.
- `...\ux03_drag.ps1` — drag threshold and post-drag click.
- `...\ux03_extra.ps1` — card-body right click, double-invoke Add apps.
- `...\ux03_final_row.ps1` — final action-row and empty-card dump.
- `...\ux03_menu_actions.ps1` — menu `Edit group…` vs `Open launcher`.

Test instances were stopped after each run. The user's tray instance
(PID 18556) was left running.

---

## 6. Files changed

- `src/Windows.cs` — icon click/drag routing and drag-click suppression, group
  icon button, launcher button (`Lucide.LayoutGrid`), themed card menu and pill
  menu, Add apps → Applications mode, empty-card primary action, guarded
  card-body Edit path, pressed triggers.
- `src/SelfTest.cs` — five order-03 checks and their UIA/log evidence; empty
  card added to `CARD_HEIGHT`; `EMPTY_CARD` asserts a single visible Add apps
  button.
- `src/Core.cs` — no changes required.
- `out/` — rebuilt `XAppGroup.exe`, `.dll`, `.pdb`, `.deps.json`,
  `.runtimeconfig.json`; refreshed `selftest.log` and preview PNGs;
  `ux03_card_menu.png`, `ux03_pill_menu.png`, `ux03_empty_card.png`.
- `Docs/ux-orders/README.md` — row 03 set to `In review`.
- `Docs/ux-orders/03-card-action-semantics.md` — status `In review`.

No footer command, `GroupData` schema/persistence, shell-drag helper, shortcut
format, or popup behavior was changed.

---

## 7. Notes / residual risks

- **No pin affordance was added.** `Lucide.Pin` was removed; `Pin` is not used
  in the card action row. The only remaining "pin" strings are the honest
  drag-to-pin tooltip/UIA text and the pre-existing `TaskbarHelper` pin/unpin
  shortcut sync.
- **Drag verification is synthetic.** The 36×30 px drag was driven with Win32
  mouse events; on this machine the shell drop surface was not observed
  completing, but the app-side contract was verified: no popup on release and
  the drag helper was invoked. The physical desktop drop still needs a human
  check.
- **Unselected cards hide the dashed Add app pill** (order 02 reserved space,
  `Visibility.Hidden`); it is not a missing control. The empty card instead uses
  its primary Add apps action, so there is never a duplicate visible
  Add apps.
- **UIA list item name.** WPF still exposes each item's automation Name as
  `XAppGroup.GroupData`; the visible group label is the first `Text`
  descendant. Pre-existing gap, order 07 scope.
- **Running binary lock.** The user's tray instance (PID 18556) runs an earlier
  `out\XAppGroup.exe`, renamed to `.old-running` while locked. The current
  `out\XAppGroup.*` files are the new build; `.old-running` files can be deleted
  after PID 18556 exits.
- **Original 1536×864 physical review hardware** remains unavailable; all order
  03 evidence is REAL at 1920×1080 / 125 % with the same 896×740 DIP window.

---

## 8. Independent verification

- Pending.
