# Order 04 — Popup launcher: grid math, placement, scroll, keyboard

**Priority:** P0
**Depends on:** 03
**Findings:** F-18, F-19, F-20, F-21, F-22, F-23
**Files in scope:** `src/Windows.cs` (`PopupWindow` only, plus any shared menu/theme helper it needs), `src/Program.cs` only if the `--launch` path needs a size hint
**Status:** Pending

---

## Context

The popup is the fast launcher: a flyout with the group's app grid and a
"Launch All" footer. Three runtime defects make it unreliable:

1. **Column count is wrong.** With `GroupCol = 4`, the UIA tree shows three
   items per row (X = 582, 702, 822; next row Y = 187). The panel width is
   `4 × 88 = 352`, but each item occupies 96 DIP (80 StackPanel + 4+4 margin +
   4+4 border padding). This affects every column count.
2. **Placement is above-only.** `Position()` computes the popup above the cursor
   and clamps to the top when there is no room, instead of flipping below. It
   uses the primary work area; multi-monitor and DPI are not handled.
3. **Tall groups clip.** There is no `ScrollViewer` and no work-area height
   clamp, so the footer and lower rows can be off-screen.

Labels are also truncated to "ChatGPT - Sho..." and keyboard support is limited
to digits 1–9.

---

## The ask

Rebuild the popup layout so the configured column count is exact, the popup
always opens entirely inside the current monitor's work area, tall groups scroll,
long names remain usable, and the whole grid is keyboard navigable.

---

## Required behavior

### 4.1 Column math

- For `GroupCol = N` (1…8), the grid must render exactly **N items per row**
  (except the final partial row), for both labeled and unlabeled modes.
- Item width must include the item's margin, border, and padding. Do not size the
  panel with one number and the item with another.
- The popup width must be the sum of the actual item widths plus the container
  padding. It must not be one item short.
- Suggested implementation: `UniformGrid` with `Columns = N`, or a `WrapPanel`
  with an explicit `ItemWidth`/`ItemHeight`. Either is acceptable if the measured
  result is exact.
- `GroupCol` out of range must be clamped to 1…8, not crash.

### 4.2 Placement

- Use the monitor that contains the cursor (`MonitorFromPoint` +
  `GetMonitorInfo`), converted to WPF DIPs for the popup's DPI.
- Preferred position: **below-right** of the cursor with a 12 DIP gap.
- If there is not enough room below, flip **above** the cursor.
- If neither side fits, clamp the popup inside the work area and let the grid
  scroll; the header must remain visible.
- Never place any part of the popup outside the work area (left/right/top/bottom).
- Apply a 8 DIP margin from work-area edges.
- If the popup is wider/taller than the work area, cap it and scroll; do not
  shrink items below their minimum.
- On a multi-monitor setup with different scaling, the popup must stay on the
  cursor's monitor and remain crisp.

### 4.3 Height and scrolling

- The popup's total height must be ≤ `workAreaHeight - 16 DIP`.
- Header (icon + group name + count) and footer ("Launch All") stay fixed.
- The app grid is inside a vertical `ScrollViewer` with `Auto` vertical
  scrollbar; horizontal scrolling is disabled.
- Mouse wheel scrolls the grid, not the page behind it.
- Keyboard navigation scrolls the focused item into view.
- "Launch All" is always visible, even with 50 apps.
- A group with zero apps shows a simple empty state: "No apps in this group"
  plus an "Edit group" button; no empty scroll area.

### 4.4 Labels and header

- Labeled mode: each label gets at least 84 DIP of usable width and up to two
  lines. Use `TextTrimming = CharacterEllipsis` only after two lines.
- Full app name is always available in the tooltip and the accessible name.
- Do not render a one-character fragment followed by an ellipsis; a long name
  must show a meaningful prefix.
- The header shows the group icon and full group name, wrapping or trimming to
  two lines; it must not overlap the close area or the grid.
- Show the app count ("12 apps") next to the name.
- Add an explicit close button (`×`) with accessible name "Close launcher", in
  addition to Esc and click-away.

### 4.5 Keyboard and focus

- On open, focus the first app item and show a visible focus ring.
- Arrow keys move focus spatially: Left/Right within a row, Up/Down by one row;
  wrap at edges predictably.
- Enter or Space launches the focused app.
- Esc closes the popup and returns focus to the control that opened it (card
  launcher or tray/shortcut path).
- Digits 1–9 keep launching the first nine apps.
- `Tab`/`Shift+Tab` cycles app items and the "Launch All"/close buttons; do not
  trap focus in a way that makes the popup impossible to leave.
- The close button is reachable by Tab and Space/Enter.

### 4.6 Mouse and context menu

- Left click launches and closes.
- Right click opens the existing themed menu: Open, Run as Administrator, Open
  File Location.
- Opening the context menu must **not** trigger the `Deactivated` close handler.
  Track whether the menu is open, or use a `Popup`/`ContextMenu` with the correct
  owner; the popup must stay alive while its menu is open.
- Hover state is visible in Dark and Light; do not hard-code a dark-only color.
- Clicking outside closes. Clicking the parent app or another window is allowed
  to close.

### 4.7 Launch behavior

- A missing app path must not close the popup with no result; order 09 adds the
  toast, but this order must at least keep the popup open and not crash.
- "Launch All" closes after attempting all apps (order 09 adds the summary).
- Do not block the UI thread while launching; launch is fire-and-forget.

## Constraints

- Keep the popup borderless, topmost, `ShowInTaskbar = false`, with the existing
  rounded card look.
- Do not replace the popup with a full window or a modal dialog.
- Do not change the group data schema or `GroupCol` semantics.
- Do not add a dependency.
- Do not regress the `--launch <id>` CLI path in `Program.cs`.

---

## Inputs

- `src/Windows.cs` `PopupWindow` (panel sizing, `Position`, `KeyDown`, menus).
- `src/Core.cs` `GroupData.GroupCol`, `OrderedPaths`, `Launcher`.
- `UX-REVIEW.md` F-18…F-23.
- `test-data/seed-appgroups.json`: "Popup Stress" (30 apps, 4 cols), "Single
  Column" (1 col), "Eight Columns" (8 cols), "A Very Long Group Name…" (5 cols).

---

## Acceptance criteria

- [ ] For a group with `GroupCol = 4`, UIA reports exactly 4 items in row 1,
      and the fifth item starts the second row.
- [ ] Same for 1, 2, 3, 5, 6, 7, and 8 columns.
- [ ] The popup opens below the cursor when there is room; above when there is
      not; and fully inside the work area in both cases.
- [ ] Opening at the bottom-right corner of the screen keeps the whole popup
      visible and scrollable; "Launch All" is visible.
- [ ] The 30-app "Popup Stress" group scrolls by mouse wheel and keyboard; no
      content is clipped.
- [ ] "ChatGPT - Shortcut" (or an equally long name) shows a meaningful label
      and the full name in the tooltip.
- [ ] Arrow keys move focus; Enter launches; Esc closes; the focus ring is
      visible; the close button works by mouse and keyboard.
- [ ] Right-click opens the menu and the popup does not disappear when the menu
      opens.
- [ ] Empty group shows the empty state, not a blank border.
- [ ] Multi-monitor (if available): the popup opens on the cursor's monitor and
      is not scaled blurry.
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

Manual/UIA checks:

1. For each seed group, open the popup from the card launcher and record the
   row X positions and the first item of row 2. Put them in the report.
2. Open at top-left, top-right, bottom-left, bottom-right, and center.
3. Open "Popup Stress" near the bottom edge; scroll to the last row and click
   "Launch All".
4. Right-click an item; confirm the menu and popup coexist.
5. Use only the keyboard from open to launch.

---

## Handoff

Write `Docs/agent-report/20261108_ux-04-popup-launcher.md`. Include the measured
column counts (configured vs. rendered) and the placement rectangles for each
screen corner. If a multi-monitor setup was unavailable, say so.

Do not start order 05 until accepted.
