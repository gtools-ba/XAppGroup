# Order 06 — Theme correctness and contrast

**Priority:** P1
**Depends on:** 01, 02, 03, 04, 05
**Findings:** F-30, F-31, F-32, F-33, F-34, F-35, F-41, F-69
**Files in scope:** `src/Windows.cs` (`Theme`, `ThemePalette`, shared control styles), `src/Program.cs` only for OS theme-change plumbing
**Status:** Pending

---

## Context

The theme system has a good token structure but is not actually theme-correct:

- `Theme.Apply` treats everything except "Light" as dark. "System (Mica)" never
  reads the OS light/dark preference and never reacts when the user changes it.
- Hard-coded dark colors bypass the tokens: card hover `#D92C344A`, ComboBox item
  foreground `#F1F5F9`, scrollbar thumb `#28FFFFFF`, dark radial glows, and the
  dark tip gradient. In Light mode these remain dark.
- "Mica" and "Acrylic" are painted gradients, not real DWM backdrops.
- Measured contrast failures: white 12 px on the `#06B6D4` gradient endpoint
  ≈ 2.42:1; `Muted #64748B` on card `#202636` ≈ 3.1:1; scrollbar thumb ≈ 1.4:1
  at 6 px.
- Native context menus, tooltips, and checkbox/ComboBox controls do not follow
  the app theme.

This order makes themes honest and readable across all surfaces created by
orders 01–05.

---

## The ask

Make System mode follow the OS, remove every hard-coded dark color from themed
surfaces, bring text and non-text contrast to WCAG AA, and make Light mode a
complete, usable theme rather than a partial repaint.

---

## Required behavior

### 6.1 System mode

- `System` resolves at startup to the OS app theme:
  - Read `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\
    AppsUseLightTheme` (DWORD; 0 = dark, 1 = light) with a safe fallback to
    dark when the key is missing.
- When the OS theme changes while `System` is active, re-apply the palette
  without restart. Use `WM_SETTINGCHANGE` (`ImmersiveColorSet`) via an
  `HwndSource` hook, or `SystemEvents.UserPreferenceChanged` if available.
- When the user picks `Dark` or `Light`, OS changes must not override the
  explicit choice.
- The theme dropdown selection, persisted `Settings.Theme`, and rendered theme
  must stay in sync.

### 6.2 Token discipline

- Every themed surface and text color comes from `ThemePalette` tokens or a
  palette binding. Remove hard-coded hex colors from:
  - `BuildCardContainerStyle` hover (`#D92C344A`).
  - `BuildThemedDropdown` item foreground (`#F1F5F9`).
  - `BuildSleekScrollBarStyle` thumb/track (`#28FFFFFF`, etc.).
  - Main-window radial glows.
  - Pro-tip banner gradient.
  - Any other literal color in `Windows.cs` used on a themed surface.
- Brand gradient colors (`#06B6D4 → #6366F1 → #A855F7`) may remain literal, but
  they must not be used as text backgrounds without a passing contrast pair.
- Add explicit tokens if needed: `CardHover`, `ScrollThumb`, `ScrollTrack`,
  `MenuBg`, `MenuHover`, `FocusRing`, `AccentText`, `OnAccent`.
- Keep the shared-brush update model: changing the palette must update already
  open main/popup/edit windows and menus without recreating them.

### 6.3 Light theme completeness

- Main window: background, title bar, stats ribbon, cards, card hover, selected
  card, footer, tip banner, search field, divider, scrollbar.
- Popup: background, border, hover, labels, menu, footer link.
- Edit dialog: chrome, inputs, list, selected row, dropdowns, checkboxes,
  buttons, validation/error text.
- Context menus and tooltips app-wide.
- No dark hover, dark glow, white-on-white, or invisible border may remain in
  Light mode.
- The Light theme must not simply invert the dark theme; use the existing
  `#EEF2F9` background / white card language.

### 6.4 Contrast

Meet these minimums, measured against the actual composited background:

| Element | Minimum |
|---|---|
| Body text ≤ 18 px | 4.5:1 |
| Secondary text (`Sub`) | 4.5:1 |
| Metadata (`Muted`) | 4.5:1 when used for text |
| Text on accent/Launch gradient, both ends | 4.5:1 |
| Icons, borders, focus ring, scrollbar thumb | 3.0:1 |
| Disabled text | 3.0:1 and clearly disabled |

Concrete fixes required:

- Darken the `Launch All` gradient or switch to dark text; the current
  `#06B6D4` end with white 12 px is ≈2.42:1. A pair such as
  `#1D4ED8 → #0E7490` with white text is acceptable if measured ≥4.5:1.
- Raise `Muted` to a readable value in both themes (e.g. dark `#A8B3C4`,
  light `#475569`) or stop using it for text.
- Use `AccentText` (`#0369A1` in light, `#67E8F9` in dark) for small accent
  text such as "Configure Hotkeys"; keep `Cyan` for icons/large text.
- Scrollbar thumb: at least 10 DIP wide (8 DIP may be used only if contrast is
  ≥3:1 and a larger hit area is preserved), with a thumb ≥3:1 against the track.
- Focus ring ≥3:1 against both the control and the background.

### 6.5 Typography

- Minimum 11 px for metadata, 12 px for body/controls. Remove 10.5 px text.
- Use `Segoe UI Variable Display` only at ≥14 px. Use `Segoe UI` (or
  `Segoe UI Variable Text`) below 14 px.
- Do not use all-caps micro-labels below 11 px; the edit dialog's 11 px section
  labels are the floor.

### 6.6 Backdrop honesty

Choose one:

- **Option A (preferred if time allows):** implement a real DWM system backdrop
  for the main window (`DwmSetWindowAttribute` with
  `DWMWA_SYSTEMBACKDROP_TYPE` / Mica on Windows 11 22H2+), with a painted
  gradient fallback when unsupported. Keep text contrast valid over the
  backdrop.
- **Option B:** remove "Mica"/"Acrylic"/"Glass" from user-facing labels and use
  the painted gradients. Labels become "System", "Dark", "Light".

Either way, no label may claim a backdrop the app does not render. If Option A
is chosen, verify on Windows 11 and state the OS build; if not available, use
Option B.

### 6.7 High contrast

- When Windows High Contrast is enabled, do not override system colors for text,
  background, focus, or selection. The app must remain readable and operable.
- At minimum, detect `SystemParameters.HighContrast` and fall back to
  `SystemColors.WindowBrush`, `WindowTextBrush`, `HighlightBrush`, and
  `HighlightTextBrush` for the main surfaces.

## Constraints

- Do not change the brand palette, logo, or layout composition.
- Do not remove Dark mode or force Light mode.
- Do not add a theming NuGet package.
- Do not hard-code per-control colors as a shortcut; tokens only.
- Do not regress the live theme switch already covered by self-test.
- Do not change control behavior while restyling (order 05/03 behavior stays).

---

## Inputs

- `src/Windows.cs`: `ThemePalette`, `Theme.Apply`, `Theme.Btn`,
  `BuildSleekScrollBarStyle`, `BuildCardContainerStyle`,
  `BuildThemedDropdown`, `BuildProTipBanner`, `BuildTitleBar`,
  `BuildStatsRibbon`, `PopupWindow`, `EditWindow`.
- `src/Program.cs`: startup theme apply, window source hooks.
- `UX-REVIEW.md` F-30…F-35, F-41, F-69.
- `Docs/Design/main screen/code.html` for the intended dark palette.

---

## Acceptance criteria

- [ ] With `Theme = System` on a light OS, the app renders Light at startup; on
      a dark OS, it renders Dark.
- [ ] Toggling the OS app theme while the app runs switches the app within
      1 second, without restart, when `System` is selected.
- [ ] With `Theme = Dark` or `Light`, an OS theme change does not override the
      explicit choice.
- [ ] A source grep finds no hard-coded dark surface/text colors in the listed
      methods; any remaining literal is documented as brand-only.
- [ ] Light mode has no dark hover, dark glow, white-on-white, or invisible
      border on main, popup, edit, menus, or tooltips.
- [ ] White text on both ends of the Launch gradient measures ≥4.5:1.
- [ ] `Sub` and `Muted` text measure ≥4.5:1 on their actual backgrounds in both
      themes.
- [ ] Scrollbar thumb measures ≥3:1 and is at least 10 DIP wide (or has an
      equivalent hit target).
- [ ] Focus rings measure ≥3:1 in both themes.
- [ ] No text is smaller than 11 px; no `Segoe UI Variable Display` below 14 px.
- [ ] Theme labels match the implemented backdrop (no false Mica/Acrylic claim).
- [ ] High Contrast mode remains readable and focus is visible.
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

1. Switch Dark → Light → System using the footer dropdown; open main, popup, and
   edit in each mode.
2. Toggle the OS theme in Settings while the app is open in System mode.
3. Sample the Launch gradient, Sub text, Muted text, scrollbar, and focus ring;
   record ratios with a contrast checker.
4. Enable High Contrast and repeat the main-window tab path.
5. Check the context menu, tooltip, ComboBox dropdown, and checkbox in Light.

---

## Handoff

Write `Docs/agent-report/20261108_ux-06-theme-contrast.md`. Include a contrast
table with measured ratios, the backdrop option chosen and why, and the exact
list of hard-coded colors removed.

Do not start order 07 until accepted.
