# Order 14 — Performance, virtualization, and motion

**Priority:** P2
**Depends on:** 01–13
**Findings:** F-74, F-75, F-76, F-77, F-78
**Files in scope:** `src/Windows.cs` (list virtualization, effects, selection plumbing, animations), `src/Core.cs` (jump-list debounce/API), `src/SelfTest.cs` only if a perf invariant is testable
**Status:** Pending

---

## Context

The app is fast enough for the live 17-group sample but pays avoidable costs:

- `ScrollViewer.CanContentScroll = false` disables item virtualization; every
  card and its effects are realized.
- Every card carries a 24 px blur `DropShadowEffect`; the selected card swaps in
  a 28 px colored shadow. Selection can cause visible GPU/CPU spikes.
- `SelectionChanged` calls `JumpListHelper.Update(Sel)` synchronously, which
  writes the last-group file and commits the jump list on every arrow-key
  repeat.
- There is no purposeful motion and no reduced-motion handling.
- Startup builds icons/jump lists synchronously; with many groups the first
  paint can be delayed.

This is the last polish order before first-run/onboarding. Optimize without
changing behavior or the approved look.

---

## The ask

Turn virtualization on, remove per-item shadow costs, debounce jump-list work,
add a small motion system that respects Windows animation settings, and measure
startup/selection/scroll/popup performance with evidence.

---

## Required behavior

### 14.1 List virtualization

- Enable virtualization on the main list:
  - `VirtualizingStackPanel.IsVirtualizing = true`
  - `VirtualizationMode = Recycling`
  - `ScrollViewer.CanContentScroll = true`
  - keep pixel scrolling if possible (`VirtualizingPanel.ScrollUnit = Pixel`) so
    the feel does not change
- Because order 02 made card heights constant, recycling must not produce
  visual glitches (stale icons, wrong selection styling, wrong action state).
- The list must still support smooth mouse-wheel scrolling, keyboard
  navigation, and `BringIntoView`.
- Do not virtualize the popup if the wrap layout cannot support it; instead cap
  and measure its open time (14.4).

### 14.2 Effects and brushes

- Remove the per-card `DropShadowEffect` and the selected-card colored shadow.
  Replace them with:
  - a 1 px themed border
  - background/border color changes for hover and selection
  - the 4 px accent bar for selection
  - optionally one shared, frozen shadow resource on the window/card container,
    but not one effect instance per item
- Freeze and reuse brushes, pens, gradients, and geometries where they are
  static.
- Replace the two full-window radial-glow borders with a single cached
  `DrawingBrush`/`VisualBrush` (or keep one static gradient) that does not
  re-render per item.
- The dark/light look must remain equivalent to the approved reference; if the
  shadow removal changes the composition noticeably, compensate with border and
  background contrast, not a per-item blur.
- Remove effects from popup items; one shadow on the popup border is enough.

### 14.3 Selection and jump-list work

- Split `JumpListHelper` into:
  - `RememberLastGroup(id)` — cheap, writes only the last-group file, debounced.
  - `UpdateJumpList(group)` — commits the shell jump list, only when the
    selection settles or the group changes.
- Debounce selection-driven work by ~300 ms (DispatcherTimer). Rapid Up/Down
  must not write per keystroke.
- Flush the final selection on window hide/close/exit so the next launch still
  restores the last group.
- Do not call `JumpListHelper.Update` from the list `SelectionChanged` directly.
- Icon caches (`IconSource`, `AppPills`, `AppIcons`) must not be invalidated by
  selection, hover, or scrolling. Verify.

### 14.4 Startup and popup timing

- Target: first usable main window ≤ 500 ms with 20 groups on the review
  machine; report the actual number.
- Target: popup open ≤ 250 ms for a 30-app group; report.
- Target: selection change to painted ≤ 50 ms; arrow-key repeat must stay
  smooth.
- If icon generation blocks first paint, make the group icon load lazily:
  - show a neutral placeholder immediately
  - generate/freeze the icon off the UI thread
  - assign it when ready
  - never touch WPF objects from the background thread without freezing
- Do not pre-generate icons for all groups at startup.
- Add opt-in timing logs (`XAPPGROUP_PERF=1` → `out/perf.log` or
  `%XAPPGROUP_DATA%\perf.log`) around startup, selection, filter, popup open,
  and jump-list commit. Remove or gate them so normal runs stay quiet.

### 14.5 Motion system

- Add a small, consistent motion vocabulary:
  - hover/press: 120 ms ease-out color/opacity change
  - popup: 140–160 ms fade + 0.98→1.0 scale from the anchor
  - toast: 160–200 ms fade/slide from the screen edge
  - menu: use the system default or a 100–120 ms fade
- Animate only opacity/transform (and color where cheap). Do not animate layout,
  width/height, or blur.
- Respect reduced motion:
  - `SystemParameters.ClientAreaAnimation == false` or
    `SystemParameters.MinimizeAnimation == false` → all custom animations are
    instant.
  - High Contrast → instant or system defaults.
- Do not animate list scrolling, selection changes, theme switches, or window
  resize.
- No animation may block input or delay the popup's keyboard readiness.
- Remove any animation that causes a visible flash on open.

### 14.6 Memory and large libraries

- With 500 groups / 5,000 launchers:
  - scrolling must remain responsive (no multi-second stalls)
  - memory must not grow unbounded while scrolling back and forth (recycling)
  - selecting groups repeatedly must not leak event handlers or icons
- Do not cache every full-size icon indefinitely if it causes excessive memory;
  a bounded cache or lazy release is acceptable.
- Tray menu rebuilds must not run on every selection; only on group changes
  (already order 12, verify).

## Constraints

- Do not change visible layout, spacing, or card composition.
- Do not remove the dark composition's depth entirely; use borders/contrast.
- Do not add a physics/animation library.
- Do not optimize by disabling accessibility, focus rings, or theme updates.
- Do not cache stale data; invalidate on group/app/icon changes.
- Do not make timing logs unconditional or noisy.

---

## Inputs

- `src/Windows.cs`: `_list`, `BuildCardContainerStyle`, `ItemTemplate`,
  `MainWindow` ctor (glows, `CanContentScroll`), `PopupWindow`, toasts.
- `src/Core.cs`: `JumpListHelper.Update`, `TryAddUserTasks`,
  `SaveLastGroupId`, icon caches.
- `UX-REVIEW.md` F-74…F-78.
- `test-data/seed-appgroups.json`; generate a 500-group variant for stress.

---

## Acceptance criteria

- [ ] Virtualization is active: with 500 groups, only the visible range (plus
      cache) is realized; scrolling reuses containers without stale visuals.
- [ ] No per-card `DropShadowEffect` remains; the selected card still reads as
      selected in Dark and Light.
- [ ] Rapid Up/Down through 20 groups produces at most one debounced jump-list
      commit, not 20.
- [ ] The last group is still restored after exit.
- [ ] Startup with 20 groups measures ≤ 500 ms (report actual).
- [ ] A 30-app popup opens in ≤ 250 ms (report actual).
- [ ] Selection paint is ≤ 50 ms and arrow-key repeat is smooth.
- [ ] With reduced motion enabled, popup/toast/menu transitions are instant.
- [ ] No animation runs during scrolling, selection, theme switch, or resize.
- [ ] 500 groups / 5,000 launchers scroll without a multi-second stall; memory
      does not grow continuously across repeated scroll cycles.
- [ ] Build and self-test pass; no warnings introduced.

---

## Verification

```powershell
dotnet build src/XAppGroup.csproj -c Release
$env:XAPPGROUP_DATA = "C:\Users\RedStone\AppData\Local\Temp\opencode\xappgroup-ux"
Copy-Item Docs\ux-orders\test-data\seed-appgroups.json "$env:XAPPGROUP_DATA\appgroups.json" -Force
$env:XAPPGROUP_PERF = "1"
out\XAppGroup.exe
out\XAppGroup.exe --selftest
Get-Content "$env:XAPPGROUP_DATA\perf.log" -Tail 50
```

Checks:

1. Generate a 500-group config (script or copy the seed with unique names/id).
2. Use PerfView or Visual Studio Diagnostic Tools to confirm virtualization and
   no repeated shadow/bitmap allocations while scrolling.
3. Hold Down for 5 seconds; count jump-list commits (log) and confirm one
   debounced write.
4. Measure startup, popup, and selection with the perf log.
5. Toggle "Animation effects" off in Windows Settings and repeat popup/toast.

---

## Handoff

Write `Docs/agent-report/20261108_ux-14-performance.md`. Include a before/after
table for startup, popup, selection, and scroll; the virtualization evidence;
the jump-list write count; and the reduced-motion result.

Do not start order 15 until accepted.
