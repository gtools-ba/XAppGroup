# Report: card-app-icons-and-footer-breakpoints
Order: `Docs/agent-orders/20261121_card-app-icons-and-footer-breakpoints.md`
Date: 2026-11-21

## What was done
- Corrected the previous card interpretation: app **icons are restored**, app **names are removed**. Cards now show up to 8 clickable icon-only app buttons with tooltips/UIA names carrying the app name; no dashed `+ Add App` pill, launcher-only button, or more-options button.
- Added a fixed `GroupData.CardVisibleAppCount = 8`. Hidden apps are summarized by a compact `+N` badge (for example `+4`), with no per-group setting.
- Popup app-label behavior is unchanged: the per-group `Show app labels in popup` checkbox still controls names in the launcher popup only.
- Extended footer icon-only mode from just the 720 DIP minimum to the 896 DIP default window width. Edit / Duplicate / Create Shortcut / Delete stay 32-DIP icon-only at 720 and 896 DIP, regain labels above 896 DIP, and switch back cleanly. New remains labeled throughout.

## Files changed
- `src/Core.cs` — added `CardVisibleAppCount = 8`; `AppPills` takes 8 apps; `OverflowCountText` now emits `+N`.
- `src/Windows.cs` — restored `CardAppPill` icon-only buttons (no text children), +N badge, and launch click handler; footer responsive check changed to `w <= DesignWidthDip` (896).
- `src/SelfTest.cs` — updated card checks for icons-only pills, added `CARD_OVERFLOW` (8 pills + `+4`, empty card = 0), rewrote `FOOTER_RESPONSIVE` for 720/896/1024/896-restore.
- `Docs/agent-orders/20261121_card-app-icons-and-footer-breakpoints.md` — two-item order, both checked, status Complete.
- `out/` — refreshed `XAppGroup.exe/.dll/.pdb/.deps.json/.runtimeconfig.json` and `selftest.log` (46 PASS / 0 FAIL). DLL hash matches `src\bin\Release\net10.0-windows\XAppGroup.dll`.

## How to verify
- `dotnet build src/XAppGroup.csproj -c Release` → 0 warnings, 0 errors.
- `XAPPGROUP_DATA=<isolated dir> out\XAppGroup.exe --selftest` → 46 PASS / 0 FAIL.
- Key log lines:
  - `INFO CARD_ACTIONS ... AppIcons=2 IconsOnly=True ...`
  - `INFO CARD_OVERFLOW DataPills=8 DataOverflow='+4' UIPills=8 UIBadge=1 EmptyPills=0`
  - `INFO FOOTER_RESPONSIVE Icons720=True Widths720='31,31,31,31' Icons896=True Widths896='31,31,31,31' Labeled1024=True Restored896=True Names='Edit,Duplicate,Create Shortcut,Delete'`
- Live UIA with isolated seed data:
  - Cards: 12-app groups show exactly 8 `Launch <app>` buttons, no text children, and a `+4` badge; badges `+1`, `+3`, `+8`, `+22` match the other group sizes.
  - Footer at 900 px (720 DIP), 1120 px (896 DIP), and 1120 px after 1280 px: four commands are 39 px wide, icon-only, with Names/HelpTexts intact and New labeled. At 1280 px (1024 DIP): labels return (82/120/164/100 px) with access keys.

## Open questions / decisions needed
- Donate/GitHub remain icons only because no URLs/actions were supplied (carried over from the previous order).
- Order 03 remains `In review`; its report section 8 is still pending, and its card sections are superseded by this order.
- Order 04 was not started, as instructed.

## Notes
- The earlier simplification report's statement that all per-app pills were removed is superseded by this report.
- Footer breakpoint is the default design width (`DesignWidthDip = 896.0`), chosen by the user as the size immediately above the 720 DIP minimum.
- No XAppGroup process was running at the end of verification; PID 9352 was not intentionally stopped by this work.
