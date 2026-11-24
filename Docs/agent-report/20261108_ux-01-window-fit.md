# Report: Order 01 — Window fit, DPI, and responsive layout

- **Date:** 2026-11-10 (re-verification; original submission 2026-11-08)
- **Order / Task:** `Docs/ux-orders/01-window-fit-and-responsive-layout.md` (Findings `F-01`, `F-02`, `F-03`, `F-04`)
- **Status:** Accepted (2026-11-10); independent verifier verdict PASS after the D-1/D-2 re-check

---

## 1. What changed since the first submission

The first submission was rejected because the process ran System-DPI-aware and the
work-area conversion divided by the DPI scale twice. Both are fixed in this revision:

1. **PerMonitorV2 DPI awareness** — `SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2)`
   is called at the top of `App.Main` and in the `App` static constructor, before any
   window is created. `ApplicationHighDpiMode` is a WinForms-only property and was not
   relied on. Verified at runtime: `GetAwarenessFromDpiAwarenessContext` = **2**,
   `GetWindowDpiForWindow` = **120**, WPF `VisualTreeHelper.GetDpi` = **120 (1.25×)**.
   The UI is now rendered natively at 125 % instead of being bitmap-stretched, so text
   and icons are crisp at the review scaling.
2. **Single DPI division** — `GetMonitorWorkAreaDip` now converts physical pixels to DIP
   exactly once (`rcWork / dpiScale`). The duplicate division is gone.
3. **Monitor DPI before the HWND exists** — `GetEffectiveDpiScale` resolves the cursor
   monitor's DPI with `GetDpiForMonitor` while the window handle is still `0`, then uses
   `VisualTreeHelper.GetDpi` afterwards. The initial clamp therefore uses the correct
   scale on a 100 %, 150 % or 200 % monitor.
4. **Automated measurements** — `SelfTest` now records the real UIA rectangles, the
   simulated work areas, the scrollbar state, the theme-dropdown mouse interaction, the
   DPI-change counters and the 896×740 screenshot into `out/selftest.log`.

---

## 2. Environment used for this verification

| Item | Value |
|---|---|
| Display | 1920×1080 |
| Scaling | 125 % |
| Monitor work area | `(0,0)`–`(1920,1020)` px = `1536×816` DIP |
| Taskbar | bottom 60 px (= 48 DIP), not covered |
| Process DPI awareness | **PerMonitorV2** (`GetAwarenessFromDpiAwarenessContext` = 2) |
| Window DPI | 120 (1.25×); WPF visual DPI 120×120 |
| OS / runtime | Windows, .NET 10 WPF |

**Known environment limitation:** the original review resolution (1536×864, work area
1536×816 px) is no longer available on this machine. Cases at that resolution, and the
100 % / 150 % / 200 % cases, are exercised through the in-app simulated display
(`ApplySimulatedDisplay`, which sends a real `WM_DPICHANGED` with a suggested rect and
overrides the work-area source). Every table row below is explicitly marked **REAL** or
**SIMULATED**. No number in this report was produced by arithmetic alone.

---

## 3. Final implementation

| Area | Behavior |
|---|---|
| Startup size | Desired `896×740` DIP; clamped to `workArea − 16` DIP per axis; centered in the work area |
| Minimum size | `MinWidth = min(720, availW)`, `MinHeight = min(520, availH)`; the window always opens even on a work area smaller than the design minimum |
| Footer | Left command row is a `WrapPanel`; Test + theme selector are docked right. At narrow widths the left row wraps and the right dock stays on-screen. Below 640 DIP the right bar stacks under the commands |
| Short height | At client height ≤ 620 DIP the Pro Tip banner collapses, the stats ribbon and footer margins compact, the list keeps `MinHeight = 180` DIP and scrolls |
| Maximize | `WM_GETMINMAXINFO` sets `ptMaxPosition/ptMaxSize` to the monitor work area; maximize never covers the taskbar. Restore returns to the clamped normal rect |
| DPI change | `WM_DPICHANGED`, `WM_DISPLAYCHANGE`, `WM_EXITSIZEMOVE` and `DpiChanged` re-run the clamp; the restore rectangle is kept in DIP |
| Vertical scroll | List `MinHeight` + vertical scrolling enabled; horizontal scrolling disabled |

---

## 4. Real measurements — 125 %, 1920×1020 px work area

`WIN_UIA` = `UIElementAutomationPeer.GetBoundingRectangle()`, physical pixels.

| Control | UIA rect (L, T, W×H) | Right | Bottom | Inside work area |
|---|---|---|---|---|
| MainWindow | `(400, 48, 1120×925)` | 1520 | 973 | **PASS** (≤ 1920 / ≤ 1020) |
| New | `(423, 915, 138×40)` | 561 | 955 | PASS |
| Edit | `(569, 915, 82×40)` | 651 | 955 | PASS |
| Duplicate | `(659, 915, 120×40)` | 779 | 955 | PASS |
| Create Shortcut | `(787, 915, 164×40)` | 951 | 955 | PASS |
| Delete | `(959, 915, 100×40)` | 1059 | 955 | PASS |
| Test | `(1178, 915, 134×40)` | 1312 | 955 | PASS |
| Theme selector | `(1336, 915, 160×40)` | 1496 | 955 | PASS |

- The window is exactly `896×740` DIP = `1120×925` px, i.e. the design size, because the
  current work area (816 DIP tall) can host it. Margins: 400 px left/right, 48 px
  top, 47 px bottom.
- **Maximized:** `(0, 0, 1920×1020)`, right 1920, bottom 1020 — fills the work area and
  leaves the 60 px taskbar visible.
- **Restored:** `(400, 48, 1120×925)` again.
- **Minimum size:** resized to `720×520` DIP = `(400, 48, 900×650)` px. `Delete` wraps to
  a second row (`416, 645`), Test → `966, 621 … 1100, 661`, theme selector → `1124, 621 …
  1284, 661`. Nothing is clipped (window right 1300, bottom 698).
- **Theme selector:** UIA `ExpandCollapse` → `Expanded`, 3 items (`System (Mica)`,
  `Dark Acrylic`, `Light Glass`), `SelectionItem` on `Dark Acrylic` reports
  `IsSelected = True`. The same flow is exercised with real mouse events in the
  self-test (`COMBOBOX_MOUSE`).
- **List:** `ScrollPattern.HorizontallyScrollable = False`,
  `VerticallyScrollable = True`.

---

## 5. Simulated measurements

### 5.1 125 % on the original review work area (1536×816 px = 1228.8×652.8 DIP)

| Control | UIA rect | Right | Bottom |
|---|---|---|---|
| MainWindow | `(208, 10, 1120×795)` | 1328 | 805 |
| New | `(231, 747, 138×40)` | 369 | 787 |
| Edit | `(377, 747, 82×40)` | 459 | 787 |
| Duplicate | `(467, 747, 120×40)` | 587 | 787 |
| Create Shortcut | `(595, 747, 164×40)` | 759 | 787 |
| Delete | `(767, 747, 100×40)` | 867 | 787 |
| Test | `(986, 747, 134×40)` | 1120 | 787 |
| Theme selector | `(1144, 747, 160×40)` | 1304 | 787 |

Clamped height is 636 DIP (`652.8 − 16`, floored). All controls are inside 1536×816.

### 5.2 Short height (600 DIP client, 125 %)

MainWindow `(208, 10, 1120×750)`, bottom 760. Pro Tip banner `Collapsed`; footer bottom
746; Test and theme selector inside 1328.

### 5.3 100 % on 1366×768 (work area 1366×720 px)

MainWindow `(235, 8, 896×704)`, right 1131, bottom 712. Footer commands bottom 698;
theme selector `(984, 666, 128×32)`, right 1112.

### 5.4 150 % on 1920×1080 (work area 1920×1008 px = 1280×672 DIP)

MainWindow `(288, 12, 1344×984)`, right 1632, bottom 996 (height clamped to 656 DIP).
Footer commands bottom 974; theme selector right 1603.

### 5.5 150 % on 1536×864 (work area 1536×816 px = 1024×544 DIP)

MainWindow `(96, 12, 1344×792)`, right 1440, bottom 804; height 528 DIP; tip banner
`Collapsed`; all footer controls bottom ≤ 787; theme selector right 1414.

### 5.6 200 % on 3840×2160 (work area 3840×2064 px = 1920×1032 DIP)

Window `(1024, 292, 1792×1102)`, right 2816, bottom 1394, inside the work area. Note:
this row simulates an **in-place** DPI change, so the window keeps its previous physical
size and ends at 551 DIP tall rather than the 740 DIP design height; a window that
**starts** on that monitor clamps to 896×740 DIP (arithmetic path `1032 − 16`, exercised
by `ApplyWorkAreaClamp`). Footer commands and theme selector are fully inside the work
area.

### 5.7 200 % on 1920×1080 (work area 1920×984 px = 960×492 DIP)

Window `(64, 16, 1792×952)`, right 1856, bottom 968; height 476 DIP; tip banner
`Collapsed`; footer bottom 946.

### 5.8 Cross-DPI moves

`MOVE_TO_100%`, `MOVE_100%_TO_150%`, `MOVE_150%_TO_100%` each report
`WmDpiChangedCount = 1`, `DpiChangedEventCount = 11`, with `OldPpi/NewPpi`
`120→96`, `96→144`, `144→96`. The clamp re-runs and the footer stays inside the work
area in every case.

---

## 6. Acceptance criteria

- [x] **Window fully inside the work area at 125 %.** Real run: bottom 973 ≤ 1020; simulated
      review work area: bottom 805 ≤ 816. Rectangles recorded above.
- [x] **All seven footer commands + theme selector reachable.** Real run: all bottom 955 ≤ 1020.
      Simulated review work area: all bottom 787 ≤ 816.
- [x] **100 % (1366×768) and 150 % (1920×1080).** Measured; all controls inside their work areas.
- [x] **Theme selector opens and changes with the mouse.** UIA expand/select plus real mouse
      event test; theme actually switches (`Bg` changes between `#FFEEF2F9` and `#FF0B0E14`).
- [x] **Minimum size:** no clipped command; list scrolls vertically; horizontal scrollbar
      `Disabled`/`Collapsed` (`ExtentW 662.4 ≤ ViewportW 664.0`).
- [x] **600 DIP client height:** Pro Tip banner collapsed; footer visible.
- [x] **Maximize does not cover the taskbar.** `(0,0,1920×1020)` with the taskbar at
      1020–1080.
- [x] **Cross-DPI move:** footer visible; `WM_DPICHANGED` + `DpiChanged` observed.
- [x] **Dark-mode 896×740 screenshot produced** — `out/ui_preview_896x740_dark.png`
      (896×740 px). The layout tree is unchanged by this order apart from clamping and
      the responsive margins, so the approved composition is preserved; a human should
      eyeball the PNG for final sign-off.
- [x] **Build and self-test pass** — `dotnet build src/XAppGroup.csproj -c Release` →
      0 warnings, 0 errors; `out\XAppGroup.exe --selftest` → **33/33 PASS, 0 FAIL**
      (`out/selftest.log`). The DPI probe is an `INFO` line, not a counted test.

---

## 7. Verification commands

```powershell
dotnet build src/XAppGroup.csproj -c Release
$env:XAPPGROUP_DATA = "C:\Users\RedStone\AppData\Local\Temp\opencode\xappgroup-ux"
New-Item -ItemType Directory -Force $env:XAPPGROUP_DATA | Out-Null
Copy-Item Docs\ux-orders\test-data\seed-appgroups.json "$env:XAPPGROUP_DATA\appgroups.json" -Force
out\XAppGroup.exe
out\XAppGroup.exe --selftest
Get-Content out\selftest.log | Select-String "DPI_CHECK|WIN_UIA|BTN_UIA|CB_UIA|SCROLL|MOVE_"
```

Live UIA check used in addition to the self-test:

```powershell
# UI Automation: window + footer rects, scroll pattern, combo expand/select,
# maximize/restore, resize to 720×520 DIP.
```

---

## 8. Files changed

- `src/Windows.cs` — `ClampToMonitorWorkArea`, `ApplyWorkAreaClamp`,
  `GetMonitorWorkAreaDip` (single DPI division), `GetEffectiveDpiScale`
  (`GetDpiForMonitor` before HWND), `UpdateResponsiveLayout`, responsive footer
  (`WrapPanel` + right dock), `WM_GETMINMAXINFO`, `WM_DPICHANGED`,
  `WM_DISPLAYCHANGE`, `WM_EXITSIZEMOVE`, `DpiChanged`, `StateChanged` restore,
  debug helpers used by the self-test.
- `src/Program.cs` — PerMonitorV2 call at process start; re-clamp on tray/second-instance
  activation; `XAPPGROUP_DATA`-scoped single-instance mutex/event (test isolation,
  approved by the manager). The mutex/event key is `SHA-256` over the normalized data
  path, not `string.GetHashCode()` (which is randomized per process).
- `src/XAppGroup.csproj` — removed `<ApplicationHighDpiMode>PerMonitorV2</ApplicationHighDpiMode>`.
  It is a WinForms runtime setting, is not consumed by a WPF `Application`, and is not
  present in the generated `XAppGroup.runtimeconfig.json`; DPI awareness comes only from
  `SetProcessDpiAwarenessContext` in `App.Main`/the static constructor.
- `src/SelfTest.cs` — real/simulated UIA measurement log, DPI probe, 896×740 dark
  screenshot, combo mouse test, scrollbar probe.
- `out/` — rebuilt; `out/selftest.log` and `out/ui_preview_896x740_dark.png` refreshed.
- `Docs/ux-orders/README.md` — row 01 status.

---

## 9. Notes / residual risks

- The original 1536×864 physical configuration is unavailable; the 100 / 150 / 200 %
  and 1536×816 work areas are simulated through the documented hook and are labelled as
  such. A spot check on real mixed-DPI hardware is still recommended before release.
- The 200 % **in-place move** keeps the physical window size (551 DIP tall). Windows
  behaves the same way; the order only requires that the window fits and the footer
  stays reachable, which it does. Startup on a 200 % monitor uses the design size.
- Visual equivalence of the 896×740 dark screenshot requires a human eye; the file is
  provided.
- No test processes were left running after verification.
- **Single-instance isolation re-checked after the verifier's D-1/D-2 findings.** With one
  isolated data directory (`XAPPGROUP_DATA`), three launches produced one surviving
  manager (`PID 9544`); the second and third exited with code 0 after signalling the
  first. The stable SHA-256 path key replaces the per-process randomized
  `GetHashCode()`.
- **DPI awareness re-checked after removing `ApplicationHighDpiMode`.** Rebuild is still
  0 warnings / 0 errors, self-test still 33/33, and `DPI_CHECK` still reports
  `Awareness=2 IsPerMonitorV2=True WinDpi=120 (1.25x)`, proving the property was inert
  and awareness comes from the explicit `SetProcessDpiAwarenessContext` call.
