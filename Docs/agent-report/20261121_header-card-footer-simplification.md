# Report: header-card-footer-simplification
Order: `Docs/agent-orders/20261121_header-card-footer-simplification.md`
Date: 2026-11-21

## What was done
- Header right side: removed minimize/maximize/restore caption controls; added icon-only **Donate** and **GitHub** buttons with tooltips and UIA names, no click action. Left traffic lights, refresh, settings, search, brand unchanged.
- Home cards: removed the open-group-launcher button, more-options button and its menu builders, the dashed `+ Add App` pill, and all per-app shortcut pills. The group icon (click = launcher, drag = pin) and the primary Launch/Add-apps action remain. The list context menu is suppressed.
- App names: kept the per-group `Show app labels in popup` checkbox in Edit Group; verified it controls names in the launcher popup and persists. No new card checkbox, since cards no longer show app names.
- Footer: at the app minimum width (**720 DIP**) Edit / Duplicate / Create Shortcut / Delete become 32-DIP icon-only buttons with tooltips and stable accessible names; above 720 DIP the original labeled buttons return. New remains labeled at all widths. Switching is reversible.

## Files changed
- `src/Windows.cs` — Lucide Heart/GitHub paths; header icon-button helper; removed card launcher/more/app-pill/add-pill UI and dead menu helpers; footer icon-mode templates and `ApplyFooterIconMode` driven by `UpdateResponsiveLayout`.
- `src/SelfTest.cs` — updated card checks for the simplified card, removed obsolete menu checks, added `HEADER_ACTIONS`, `CARD_ACTIONS`, `POPUP_LABELS`, `FOOTER_RESPONSIVE` checks.
- `Docs/agent-orders/20261121_header-card-footer-simplification.md` — to-do list with decisions and per-item verification results; status Complete.
- `out/` — refreshed `XAppGroup.exe/.dll/.pdb/.deps.json/.runtimeconfig.json` and `selftest.log` (45 PASS / 0 FAIL). Removed stale `.old-running` copies.

## How to verify
- `dotnet build src/XAppGroup.csproj -c Release` → 0 warnings, 0 errors.
- `XAPPGROUP_DATA=<isolated dir> out\XAppGroup.exe --selftest` → 45 PASS / 0 FAIL.
- Live UIA checks with isolated seed data:
  - Header: `HeaderDonateBtn` / `HeaderGitHubBtn`, both Invoke=True; no minimize/maximize controls.
  - Cards: zero launcher-only / more-options / app-pill / add-pill buttons; right-click opens no menu; card heights stable across selection.
  - Popup labels: checkbox Off removes app names from popup; On restores them; JSON keeps the flag.
  - Footer at 900 px (720 DIP): four commands 39 px, no text children, Names/HelpTexts intact, New labeled. At 1120 px (896 DIP): all four labels return.

## Open questions / decisions needed
- Donate/GitHub URLs were not supplied; per decision the buttons are icons only.
- Order 03 remains `In review`; its report section 8 is still pending, and its card sections are superseded by this order.
- Order 04 was not started, as instructed.

## Notes
- Footer breakpoint is the app minimum window width (`DesignMinWidthDip = 720`), chosen because the current small width was unknown.
- No XAppGroup process was running at the end of verification; PID 9352 was not intentionally stopped by this work. Restart from `out\XAppGroup.exe` if it was open before.
