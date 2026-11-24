# Order 13 — Copy, information architecture, Settings/About

**Priority:** P2
**Depends on:** 03, 04, 06, 08, 12
**Findings:** F-65, F-66, F-67, F-68, F-69, F-70, F-71, F-72, F-73, F-80, F-82
**Files in scope:** `src/Windows.cs` (all user-facing strings, title bar, ribbon, footer, Settings/About window), `src/Core.cs` (`Settings` fields if needed), `src/Program.cs` (`--tray` startup, Run key), `src/XAppGroup.csproj` (version metadata)
**Status:** Pending

---

## Context

The interface is visually convincing but says things that are not true:

- "v2.4 PRO" is hard-coded and unrelated to the assembly version.
- "Background Daemon Active" claims a daemon; the app is a tray-resident WPF
  process.
- "Refresh Groups & Icons" only invalidates icons.
- The gear icon says "Edit Selected Group" and opens Edit; users expect app
  settings.
- "Test Runner" opens the popup launcher.
- "Create Shortcut" only creates a desktop shortcut.
- "System (Mica)" / "Dark Acrylic" claim backdrops that may not exist.
- "Pro Tip: Drag any group card" is false; only the group icon drags.

There is no Settings window, no About, and no way to open the data folder. This
order makes the words match the product and gives the app a proper settings
surface.

---

## The ask

Do a complete copy and information-architecture pass, replace the gear's
behavior with a real Settings/About window, make "start with Windows" work, and
remove every claim the app cannot back up.

---

## Required behavior

### 13.1 Version and About

- Add `<Version>` (and optionally `<FileVersion>` /
  `<InformationalVersion>`) to `src/XAppGroup.csproj`. Use `2.4.0` unless the
  manager says otherwise; the badge becomes "v2.4", not "v2.4 PRO".
- Read the version from the assembly at runtime. Do not hard-code it in XAML or
  C#.
- About panel/window:
  - app name and icon
  - version from the assembly
  - "Runs in the system tray" one-liner
  - data folder path + "Open data folder" button
  - a "Documentation" button that opens `Docs/` or the project page if one is
    configured; otherwise omit it (do not ship a dead link)
  - no fake company, copyright, or update claims
- If the version cannot be read, show "Version unavailable" rather than a fake
  number.

### 13.2 Settings window

- The gear button opens a themed, keyboard-accessible **Settings** window (not
  the Edit Group dialog). Tooltip/accessible name: "Settings".
- Sections:
  1. **Appearance** — theme: System / Dark / Light. Must stay in sync with the
     footer dropdown and persist immediately.
  2. **Startup** — "Start XAppGroup with Windows" checkbox.
     - Writes/removes `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\
       XAppGroup` with the quoted exe path and `--tray`.
     - Handles access/registry errors with an inline message; do not claim
       success when the write fails.
  3. **Tray** — "Show the Pro Tip banner" toggle (restores after a dismiss), and
     a read-only note that closing hides to the tray (order 12).
  4. **Hotkeys** — a button that opens the Hotkeys manager from order 08.
  5. **Data** — data folder path, "Open data folder", and the current config
     file size/last-modified time.
  6. **About** — the version block above.
- Settings changes apply immediately and save; no Apply/OK required. Close with
  Esc or a close button.
- The window fits the 125% work area and scrolls if needed.
- No unimplemented options. Remove or hide `CheckUpdates` if there is no update
  feature.
- `--tray` startup mode:
  - loads settings, applies theme, builds the tray and hotkeys
  - does not show the main window
  - a second normal launch still restores it
  - `--tray` must not break `--launch`, `--open-all`, `--edit`, or `--selftest`

### 13.3 Copy table (must be applied)

| Current | New | Why |
|---|---|---|
| `v2.4 PRO` | `v2.4` from assembly | version must be real |
| `Background Daemon Active` | `Tray resident` | no daemon exists |
| `App Groups` (stat) | `groups` | plain language |
| `Configured Launchers` | `apps` | plain language |
| `Configure Hotkeys` | `Configure hotkeys` | sentence case; still opens manager |
| `Refresh Groups & Icons` | `Refresh app icons` | it only rebuilds icons |
| `Edit Selected Group` (gear) | `Settings` | gear now opens Settings |
| `Test` / `Test Runner` | `Preview launcher` | it opens the popup |
| `Create Shortcut` | `Create desktop shortcut` | only desktop is supported |
| `Group icon: automatic 2×2 composite` | `Automatic group icon` | remove implementation detail |
| `Pro Tip: Drag any group card…` | `Pro Tip: Drag the group icon onto the taskbar or desktop to pin it.` | only the icon drags |
| `Close to System Tray` | `Hide to system tray` | match order 12 |
| `Open Group Popup Launcher` | `Open group launcher` | consistent term |
| `Edit this Group` (ellipsis) | `More options` | menu, not direct edit |
| `System (Mica)` / `Dark Acrylic` / `Light Glass` | match order 06's decision | no false backdrop |

- Fix singular/plural: "1 group" / "2 groups", "1 app" / "2 apps".
- Commands that open a dialog end with `…` (e.g. "Edit group…", "Add apps…",
  "Settings…").
- Destructive commands are explicit: "Delete group…" not "Delete".
- Tooltips start with the action: "Open launcher", "Refresh app icons",
  "Settings", "More options".
- The status dot text must reflect actual state. If the tray icon failed to
  create, do not show "Tray resident".
- Remove the underlined "Windows Taskbar"/"Desktop" text in the tip unless it is
  a real link; do not style static text as a link.

### 13.4 Information architecture

- **Title bar:** traffic lights · brand/version · search · refresh icons ·
  settings · window controls. No action that belongs in the footer.
- **Stats ribbon:** groups count · apps count · tray state · "Configure hotkeys"
  link. No daemon claim.
- **List:** cards only; no global commands.
- **Footer:** New · Edit · Duplicate · Create desktop shortcut · Delete ·
  Preview launcher · Theme. Group destructive `Delete` apart from the primary
  actions with spacing/divider; keep the existing order if it tests better, but
  document it.
- **Tray:** open · groups · settings · quit (order 12).
- **Settings:** app-level only. Group-level fields stay in Edit Group.
- The gear must never open a group dialog again.
- "Preview launcher" opens the selected group's popup; if no selection, it is
  disabled per order 02.

### 13.5 Constant audit

- Centralize user-facing strings that are repeated (app name, version, action
  names, toast messages) if practical; at minimum, make them consistent.
- Remove dead labels and tooltips for removed controls (pin, test runner, fake
  daemon).
- Search the source for: `PRO`, `Daemon`, `Mica`, `Acrylic`, `Test Runner`,
  `Unpinned`, `Copy`, `any group card`, `Configured Launchers`, `App Groups`.
  Document each remaining occurrence and why it is correct.
- Do not leave commented-out fake features.

## Constraints

- Do not rename the product.
- Do not add a marketing/update-check feature.
- Do not add a network call.
- Do not remove the footer theme selector; Settings and footer must stay in sync.
- Do not implement a second settings store; use `Settings`.
- Do not break `--selftest` or the CLI modes.
- Do not reorganize the visual composition beyond what the copy table requires.

---

## Inputs

- `src/Windows.cs`: title bar (version, gear, refresh), stats ribbon, footer,
  tip banner, Edit labels, popup labels.
- `src/Program.cs`: startup, tray, Run key, CLI.
- `src/Core.cs` `Settings`.
- `src/XAppGroup.csproj`.
- `UX-REVIEW.md` F-65…F-73, F-80, F-82.
- `Docs/Design/main screen/code.html` for the intended labels.

---

## Acceptance criteria

- [ ] The badge and About show the same real assembly version; no "PRO".
- [ ] The gear opens Settings; Edit Group is still reachable from cards/footer.
- [ ] Settings contains working Appearance, Startup, Tray, Hotkeys, Data, and
      About sections; no dead options.
- [ ] "Start with Windows" writes/removes the Run key with `--tray`, and
      `--tray` starts hidden with a working tray icon and hotkeys.
- [ ] "Refresh app icons" only rebuilds icons and says so.
- [ ] "Preview launcher" opens the popup and is disabled without a selection.
- [ ] "Create desktop shortcut" says desktop and only does desktop.
- [ ] The Pro Tip copy matches the actual drag area and stays dismissed.
- [ ] No "Daemon", "PRO", "Mica", "Acrylic", "Test Runner", or "any group card"
      remains unless order 06 implemented a real backdrop (then the label must
      name it accurately).
- [ ] Singular/plural and ellipsis rules are followed in the visible UI.
- [ ] The data folder button opens Explorer at the correct path.
- [ ] Build and self-test pass; all CLI modes still work.

---

## Verification

```powershell
dotnet build src/XAppGroup.csproj -c Release
$env:XAPPGROUP_DATA = "C:\Users\RedStone\AppData\Local\Temp\opencode\xappgroup-ux"
Copy-Item Docs\ux-orders\test-data\seed-appgroups.json "$env:XAPPGROUP_DATA\appgroups.json" -Force
out\XAppGroup.exe
out\XAppGroup.exe --selftest
out\XAppGroup.exe --tray
```

Checks:

1. Open Settings from the gear; change theme; verify the footer dropdown syncs.
2. Toggle "Start with Windows"; inspect the Run key; sign out/in or simulate
   `--tray` and verify no main window appears.
3. Open Hotkeys and Data from Settings.
4. Compare every visible string against the copy table; attach a current → new
   list.
5. Grep for the forbidden terms; attach the output.
6. Verify Preview launcher with and without a selection.

---

## Handoff

Write `Docs/agent-report/20261108_ux-13-copy-settings.md`. Include the full copy
table as applied, the Run-key value, the `--tray` behavior, and the grep output
for forbidden terms.

Do not start order 14 until accepted.
