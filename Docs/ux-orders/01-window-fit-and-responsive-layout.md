# Order 01 — Window fit, DPI, and responsive layout

**Priority:** P0
**Depends on:** none
**Findings:** F-01, F-02, F-03, F-04
**Files in scope:** `src/Windows.cs` (main window construction, title bar, ribbons, footer), optionally `src/Program.cs` (startup placement)
**Status:** Accepted (2026-11-10)

---

## Context

At the review resolution (1536×864, work area 1536×816, 125% scaling), the main
window is 896×740 DIP, which becomes 1120×925 px. The bottom 109 px are below
the screen. The entire footer command bar — New, Edit, Duplicate, Create
Shortcut, Delete, Test, and the theme ComboBox — is unreachable. The theme
dropdown sits at approximately (1459, 880), outside the work area.

The window uses a fixed `Width = 896; Height = 740; MinWidth = 720; MinHeight = 520`
and `WindowStartupLocation = CenterScreen`, with no clamp to the monitor work
area. The layout is a `DockPanel` with title bar, stats ribbon, footer, tip
banner, and the list. None of the fixed-height bands collapse at short heights.

This order makes the window fit real screens. Do not redesign the visual
composition; make the existing composition survive DPI and small work areas.

---

## The ask

Make the main window size itself to the available monitor work area at startup
and on DPI change, keep the footer and theme selector always reachable, and
make the layout degrade predictably when the window is short.

---

## Required behavior

1. **Startup size is clamped to the work area.**
   - Desired size remains 896×740 DIP when it fits.
   - Actual startup height must never exceed `workAreaHeight - 16 DIP`; actual
     width must never exceed `workAreaWidth - 16 DIP`.
   - Use the monitor that will contain the window, not a hard-coded constant.
   - The window must start fully inside the work area, including at 100%, 125%,
     150%, and 200% scaling.
2. **Minimum size is also clamped.** `MinHeight`/`MinWidth` must not exceed the
   available work area. If the work area is smaller than the design minimum,
   the window still opens and the list scrolls; the footer remains visible.
3. **Footer is always visible.**
   - All seven footer commands (New, Edit, Duplicate, Create Shortcut, Delete,
     Test, theme selector) must be inside the window at startup at the review
     resolution.
   - If the command row runs out of width, it wraps to a second row; it must
     never clip horizontally or be pushed below the window.
   - The theme selector must remain reachable and must not be pushed off the
     right edge.
4. **Short-height behavior.** Add a height breakpoint (suggested ≤ 620 DIP
   client height): collapse the Pro Tip banner first and compact the stats
   ribbon. Do not collapse the footer or the title bar. The list must retain a
   usable minimum height (suggested 180 DIP) and scroll vertically.
5. **Maximize and restore.**
   - Maximizing must not cover the taskbar or extend under it.
   - Restoring returns to a size that still satisfies rules 1–3.
   - Double-click on the title bar and the caption buttons continue to work.
6. **DPI changes while running.**
   - Dragging the window between 100% and 150% monitors must not leave the footer
     clipped or the window larger than the target work area.
   - Re-evaluate the clamp on `WM_DPICHANGED` / `DpiChanged` and on monitor
     change.
7. **No horizontal scrollbar** on the main list at any supported width ≥ 720 DIP.
8. **Preserve the approved look** in dark mode at the design size. The fix must
   not add a visible scrollbar around the whole window or turn the custom chrome
   into a native title bar.

## Constraints

- Do not remove the custom window chrome or the traffic-light composition.
- Do not change the footer command set or their labels; order 13 handles copy.
- Do not add a dependency (no UI libraries, no MahApps, no Fluent theme package).
- Window size/position persistence is **not** part of this order; that is order 12.
  This order only guarantees a correct default and correct clamping.
- Do not edit `src/Core.cs` unless a new helper is genuinely required; keep data
  model changes out of this order.

---

## Inputs

- `Docs/Design/main screen/code.html` and `screen.png` — target composition.
- `src/Windows.cs`:
  - `MainWindow` constructor (window size, chrome, dock composition, glows).
  - `BuildTitleBar`, `BuildStatsRibbon`, `BuildProTipBanner`,
    `BuildFooterCommandBar`.
- `UX-REVIEW.md` findings F-01…F-04.
- Test data: `Docs/ux-orders/test-data/seed-appgroups.json`.

---

## Acceptance criteria

- [ ] At 1536×864 / 125%, the window's UIA bounding rectangle is fully inside
      (0, 0)–(1536, 816); the bottom is ≤ 816 px.
- [ ] At that resolution, UIA finds the New, Edit, Duplicate, Create Shortcut,
      Delete, Test buttons and the theme ComboBox, all with bottom ≤ 816 px.
- [ ] At 1366×768 / 100% and 1920×1080 / 150%, the same is true for the monitor
      work area.
- [ ] The theme ComboBox can be opened and changed with the mouse at the review
      resolution.
- [ ] At the minimum window size, no footer command is clipped; the list scrolls
      vertically; there is no horizontal scrollbar.
- [ ] At a client height of 600 DIP, the tip banner is gone/compacted and the
      footer is still visible.
- [ ] Maximize does not cover the taskbar.
- [ ] Moving the window to a different-DPI monitor leaves the footer visible.
- [ ] Dark-mode screenshot at 896×740 (when it fits) is visually equivalent to
      the approved reference; no new chrome or border.
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

1. Read the window and footer bounding rectangles with Accessibility Insights
   (or Inspect.exe) at 125%. Record the numbers in the report.
2. Repeat at 100% and 150%.
3. Resize the window to its minimum and confirm the list scrolls but the footer
   does not clip.
4. Toggle maximize/restore and confirm the taskbar is visible.
5. If a second monitor with different scaling is available, drag the window
   across and repeat step 1.

Report the exact before/after rectangles; "fixed" without measurements is not
acceptable for this order.

---

## Handoff

Write `Docs/agent-report/20261108_ux-01-window-fit.md` using the standard
template. Include:

- The measured window/work-area rectangles at each scaling.
- Which controls were confirmed reachable by UIA.
- Any breakpoint values chosen.
- Any display configuration you could not test.

Do not start order 02 until this report is accepted.
