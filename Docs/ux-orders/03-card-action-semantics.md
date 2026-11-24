# Order 03 — Card actions: make the icon match the action

**Priority:** P0
**Depends on:** 02
**Findings:** F-11, F-12, F-13, F-14, F-15, F-16, F-17, F-72
**Files in scope:** `src/Windows.cs` (card data template, card mouse routing, action menu, popup entry), `src/Core.cs` only if an action helper is missing
**Status:** In review

---

## Context

The card presents three icon-only actions, and two of them do the wrong thing:

| Control | Icon / tooltip | What it actually does |
|---|---|---|
| Pin | pin / "Open Group Popup Launcher" | Opens the popup; never pins |
| Ellipsis | ⋯ / "Edit this Group" | Opens the Edit dialog; no menu |
| "+ Add App" | dashed pill | Opens the full Edit dialog, same as Edit |

App pills look draggable but launch immediately. Only the 48×48 group icon can
start the shell drag that pins to the taskbar, while the banner says "Drag any
group card". Keyboard and screen-reader users cannot invoke any card action.

Windows does not provide a reliable, supported API to programmatically pin an
arbitrary app shortcut to the Windows 11 taskbar. Shipping a pin button that
does something else is worse than not shipping it. This order therefore makes
the pin promise honest: **drag is the pin affordance**, and the card's third
button becomes an explicit launcher button.

---

## The ask

Make every card action truthful, discoverable, and keyboard reachable:

1. The group icon is click-to-open-launcher and drag-to-pin.
2. A dedicated launcher button opens the popup.
3. The ellipsis opens a real action menu.
4. "Add App" opens Edit focused on applications, not the generic dialog.
5. App pills have one clear primary action and a useful context menu.

---

## Required behavior

### 3.1 Group icon (48×48)

- **Left click without drag** opens the popup launcher for the group.
- **Left drag past 4 px** starts the existing `ShellDragDrop.DoDragLink` pin
  flow; no popup opens on mouse-up after a drag.
- `Esc` during drag cancels the shell drag; no popup and no exception.
- Tooltip: "Open launcher. Drag to pin to the taskbar or desktop."
- The icon must have an accessible name: "Open launcher for {group name}.
  Drag to pin."
- Do not remove the drag capability. It is the only honest pin path until order
  15 documents it.

### 3.2 Primary launch button

- **Always visible** on every card, selected or not.
- Clicking launches all apps in the group.
- Unselected: glass/secondary style. Selected: filled gradient style (the
  existing `#2563EB → #0891B2`). The style change must not change size.
- Label "Launch" on unselected cards and "Launch All" on selected cards is
  acceptable; the hit area and row height must not change.
- If the group has zero apps, replace the button with "Add apps" (dashed
  secondary) that opens Edit focused on Applications.
- If every app path is missing, keep the button enabled but order 09 adds the
  failure feedback. Do not silently no-op.

### 3.3 Launcher button (replaces the pin button)

- Replace the pin glyph with a grid/launcher glyph (add a `Lucide.LayoutGrid`
  const; do not reuse the pin path).
- Opens the popup launcher. Tooltip: "Open group launcher".
- Accessible name: "Open launcher for {group name}".
- Do not use `Pin` anywhere in the card action row.
- Keep the 30×30 hit target at minimum; 32×32 preferred.

### 3.4 More options menu (ellipsis)

- Opens a themed `ContextMenu` (or a themed popup panel) anchored to the button.
- Menu items, in this order:
  1. **Open launcher** (default; Enter)
  2. **Edit group…**
  3. **Add apps…**
  4. **Duplicate group**
  5. **Create desktop shortcut**
  6. separator
  7. **Delete group…** (danger style)
- "Create desktop shortcut" must say "desktop" because that is all it does.
- Do not add "Pin to taskbar" unless it actually pins. Rely on drag.
- Menu opens on left click, `Shift+F10`, and the context-menu key when the card
  is focused. Esc closes it and returns focus to the button.
- Opening the menu must never open the Edit dialog.

### 3.5 "+ Add App" pill

- Opens the Edit dialog **scrolled to the Applications section**, with the
  "Add…" button focused. The dialog title should read "Edit Group — Applications"
  (or an equivalent visible cue). It must not look identical to the generic
  Edit action.
- If the group is empty, the same control is the primary empty-card action.
- Do not open the file picker twice or stack two dialogs.

### 3.6 App pills on the card

- **Left click** launches that app. Cursor = `Hand`.
- **Right click** opens a themed context menu:
  - Open
  - Run as administrator
  - Open file location
  - Copy path
- Tooltip: full path (and a "Missing file" warning when applicable).
- Pills must be keyboard reachable and expose an Invoke pattern; order 07 will
  verify the accessibility tree, but build on real `Button` controls now.
- Do not use `Cursors.Hand` on a draggable-looking chip unless drag is actually
  implemented. Since this order chooses click-to-launch, use `Hand`, not grab.
- Do not remove apps directly from the pill menu; removal stays in Edit.

### 3.7 Card body

- Single click selects (order 02 invariant).
- Double click opens Edit (unchanged).
- Right click on the card body opens the same more-options menu.
- The reserved action row from order 02 stays; no card may change height when
  hovered, selected, or when a menu opens.

### 3.8 General

- Every actionable card control must be a real `Button`/`MenuItem` (or expose
  the equivalent UIA Invoke pattern). Do not use bare `Border` + mouse handlers
  for new work.
- Every icon-only control gets both `ToolTip` and
  `AutomationProperties.Name`; the name must describe the action, not the icon.
- Hover/pressed states must be visible in Dark and Light; order 06 fixes final
  contrast, but do not hard-code new dark colors here.

## Constraints

- No new NuGet/dependency.
- Do not change the shell drag helper's behavior or the shortcut format.
- Do not add a fake pin command. If a future Windows API makes pinning
  reliable, a later order can add it.
- Do not change the footer commands in this order.
- Do not change group data or persistence.
- The card must remain a `ListBoxItem` so selection/keyboard navigation keeps
  working.

---

## Inputs

- `Docs/Design/main screen/code.html` — card composition and menu intent.
- `src/Windows.cs`:
  - `ItemTemplate` (action row, pills, add pill).
  - `MainWindow` preview mouse routing (`GroupIconHandle`, `CardLaunchAll`,
    `CardPopup`, `CardEdit`, `CardAddApp`, `CardAppPill`).
  - `BuildCardContainerStyle`.
- `src/Core.cs`: `ShellDragDrop.DoDragLink`, `ShortcutHelper.Create`,
  `TaskbarHelper`.
- `UX-REVIEW.md` F-11…F-17, F-72.

---

## Acceptance criteria

- [ ] No pin glyph remains in the card action row; the launcher glyph opens the
      popup and is labeled "Open group launcher".
- [ ] Clicking the group icon opens the popup; dragging it 10 px or more starts
      the taskbar/desktop pin drag and does **not** open the popup on release.
- [ ] Clicking the ellipsis opens the six-item menu; it does not open Edit.
      Clicking "Edit group…" opens Edit.
- [ ] The menu opens with `Shift+F10` and the context-menu key when a card is
      focused; Esc closes it and focus returns to the card/button.
- [ ] "Add apps…" and the "+ Add App" pill open Edit with the Applications list
      focused and visible without scrolling the dialog first.
- [ ] Launch is visible on every card, including unselected cards, and does not
      change card height between states.
- [ ] Clicking an app pill launches it; right-click opens the four-item menu;
      the cursor is `Hand`, not a grab cursor.
- [ ] No action opens the Edit dialog except Edit group, Add apps, or the
      Add App pill.
- [ ] UIA exposes an Invoke pattern and a descriptive name for the launcher,
      ellipsis, launch, add-app, and app-pill controls.
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

1. Click each action on three cards: selected, unselected, and zero-app.
2. Drag the icon onto the desktop, then click it; confirm the two paths do not
   interfere.
3. Open the more menu by mouse and keyboard; check every item.
4. Right-click a card and an app pill; confirm the correct menu appears.
5. Use Inspect/Accessibility Insights to read the names and patterns of the five
   card controls.

---

## Handoff

Write `Docs/agent-report/20261108_ux-03-card-actions.md`. Include a screenshot or
UIA dump of the action row, the final menu item list, and confirmation that no
pin glyph or fake pin command remains.

Do not start order 04 until accepted.
