# Order 15 — First run, Help, import/export, backup

**Priority:** P2
**Depends on:** 09, 10, 11, 13
**Findings:** F-79, F-80, F-81, F-82
**Files in scope:** `src/Core.cs` (schema version, backup/import/export services), `src/Windows.cs` (welcome/help/backup UI), `src/Program.cs` (first-run flag), `src/SelfTest.cs` (import/export/backup tests)
**Status:** Pending

---

## Context

A new user with no config sees a blank list. There is no Help, no About (order 13
adds About), no import/export, and no user-visible backup. The only config is a
single JSON file with a hidden `.bak` from order 11.

This is the final order: make the app explain itself on first run, give users a
way to recover and move their data, and document the behavior the previous
orders added.

---

## The ask

Add a first-run welcome flow, a real Help center, JSON import/export with merge
or replace, and automatic rotating backups with a restore UI. All of it must be
themed, accessible, and non-destructive.

---

## Required behavior

### 15.1 First-run welcome

- Show the welcome panel when:
  - there is no config file (or the library is empty), and
  - `Settings.HasCompletedOnboarding == false`.
- Panel content:
  - title "Welcome to XAppGroup"
  - one sentence: "Group the apps you use together, then launch them all at
    once."
  - three numbered steps:
    1. Create a group and give it a name.
    2. Add apps by dragging files or using Add.
    3. Drag the group icon to the taskbar or desktop to pin it, or assign a
       global hotkey.
  - primary **Create my first group** (opens New Group)
  - secondary **Import existing groups** (import flow)
  - secondary **Try a sample group** (creates a local sample with Notepad,
    Calculator, Paint, Explorer, and opens Edit; clearly labeled as sample)
  - text link **Skip** (dismisses onboarding)
- Actions:
  - Create → mark onboarding complete, open New Group.
  - Import → mark complete only after a successful import; cancel leaves the
    welcome.
  - Sample → mark complete, create the group with a unique name "Sample Group",
    select it.
  - Skip → mark complete, show the order 10 empty state.
- Do not show the welcome again after completion. Provide "Show welcome again"
  in Settings > Tray/Help (order 13's Settings window).
- The welcome panel must be keyboard reachable, focus the primary action on
  first display, and fit the order 01 window at 125%.
- Do not auto-create data without an explicit click.

### 15.2 Help center

- Add a themed **Help** window (or a Settings tab) opened by:
  - `F1` from the main window
  - a Help button in the title-bar overflow/menu
  - a "Help" item in the tray menu (order 12)
- Sections:
  1. **Getting started** — create, add apps, launch, pin.
  2. **Keyboard shortcuts** — the full list from order 07 (main, cards, popup,
     dialog).
  3. **Global hotkeys** — how to set, why they need the app running, conflict
     behavior.
  4. **Tray and closing** — close hides, how to quit, single instance.
  5. **Troubleshooting** — missing app files, shortcut creation failures,
     hotkey conflicts, config load errors, where the log lives.
  6. **Data and backup** — data folder, automatic backups, import/export.
  7. **About** — link to the About section (order 13).
- Content must match the shipped behavior. Do not document a feature that does
  not exist.
- No external network requirement; links to local folders are allowed.
- The window is scrollable, themed, keyboard accessible, and closable with Esc.
- A "Copy diagnostics" button may copy version, data path, log path, and counts;
  it must not include app paths unless the user opts in.

### 15.3 Export

- Command in Settings > Data and/or the title-bar menu: **Export groups…**
- `SaveFileDialog` with:
  - default name `XAppGroup-backup-YYYYMMDD.json`
  - JSON filter
  - overwrite confirmation (OS dialog)
- Export format:
  - top-level `SchemaVersion` (start at 1), `ExportedAtUtc`, `AppVersion`,
    `Settings` (theme, onboarding flag, tip flag), and `Groups`
  - preserve group order, names, paths, layout, icons, tray flags, hotkeys
  - do not include generated icon binaries; paths to custom icons are included
    as-is
- Validate the exported JSON by re-parsing it before reporting success.
- Success toast with "Open folder"; failure toast with the reason.
- Never include logs, credentials, or unrelated files.

### 15.4 Import

- Command: **Import groups…** in Settings > Data, the welcome panel, and the
  title-bar menu.
- `OpenFileDialog` for `.json`; reject files over a sane size (e.g. 20 MB) with
  a clear message.
- Parse and validate before modifying anything:
  - valid JSON object
  - `SchemaVersion` known (1; unknown newer versions rejected with a message)
  - groups array, names, paths, columns within range
  - count limits (e.g. ≤ 5,000 groups, ≤ 50,000 apps) to avoid a hang
- Show a summary dialog:
  - "Import N groups and M apps?"
  - choice: **Merge** (keep existing, add imported; rename duplicate names) or
    **Replace** (replace the current library)
  - **Cancel**
- Before import, create a timestamped backup of the current config (order 11
  helper).
- Merge:
  - keep existing groups and order
  - append imported groups in file order
  - rename duplicate names with order 11's helper
  - assign new Ids if an imported Id collides
  - preserve imported hotkeys only if free; otherwise clear them and report
    "N hotkeys could not be imported"
- Replace:
  - back up first
  - replace groups and app-level settings that are safe to import (theme may be
    included; onboarding/tip flags should reset or be preserved deliberately)
  - re-register hotkeys after load; clear conflicts
- After import:
  - save atomically
  - rebuild tray, hotkeys, stats, filter, selection
  - show a success toast with counts and "Undo" if feasible; at minimum the
    pre-import backup path
- Any validation/IO error leaves the current library untouched.
- No code execution from imported JSON; paths are data only.

### 15.5 Automatic backups

- On the first successful save of each day, write a backup to
  `%APPDATA%\XAppGroup\Backups\appgroups-YYYYMMDD-HHmmss.json`.
- Keep the 10 most recent automatic backups; prune older ones after a successful
  new backup.
- Do not back up on every keystroke/save; once per day plus before import,
  restore, and migration.
- Backup content is the same schema as export.
- Failures are logged and shown once as a warning; they must not block saving.
- Settings > Data > **Backups…** opens a themed list:
  - date/time, size, group count
  - **Restore** (confirmation; backs up the current state first)
  - **Open folder**
  - **Delete** (with confirmation)
  - empty state: "No backups yet."
- Restore:
  - validate the backup
  - back up the current config
  - replace groups/settings
  - reload tray/hotkeys/theme/stats
  - show success or a clear error
- The hidden `appgroups.bak.json` from order 11 remains the last-resort
  recovery; the Backups folder is the user-facing history.

### 15.6 Data and compatibility

- Add `SchemaVersion` to the serialized root with a default of 1. Existing files
  without it load as version 1.
- Keep `Store.Load`/`Save` backward compatible with the current JSON shape.
- Import/export and backup must round-trip without losing unknown future fields
  where practical; at minimum do not silently drop fields the current app knows.
- Never write an import/backup file into the app's live config path without the
  atomic save and backup rules.
- Tests:
  - export → import merge → names/counts correct
  - export → import replace → library matches
  - duplicate names renamed on merge
  - invalid JSON leaves the library untouched
  - backup rotation keeps 10
  - restore round-trip
  - schema version handling (missing, 1, unknown)

### 15.7 Accessibility and theme

- Welcome, Help, Import summary, and Backups windows follow orders 06 and 07:
  themed, keyboard accessible, visible focus, named controls, Esc closes.
- The welcome primary action receives focus once; do not steal focus again.
- Long help content scrolls; the close button stays visible.
- All new dialogs fit the 125% work area.

## Constraints

- Do not add a network account, cloud sync, or telemetry.
- Do not import executable code or run anything from a config file.
- Do not silently merge or replace; require an explicit choice.
- Do not auto-create a sample group without a click.
- Do not remove the order 11 `.bak` recovery.
- Do not add a database.
- Do not ship Help text for features that are not implemented.

---

## Inputs

- `src/Core.cs` `Store`, `Settings`, `GroupData`, order 11 helpers.
- `src/Windows.cs` empty state (order 10), Settings (order 13), toast (order 09).
- `src/Program.cs` startup and tray.
- `src/SelfTest.cs` store tests.
- `UX-REVIEW.md` F-79…F-82.
- `test-data/seed-appgroups.json` for import/export round-trips.

---

## Acceptance criteria

- [ ] With a clean `XAPPGROUP_DATA`, the welcome panel appears, fits the window,
      and focuses "Create my first group".
- [ ] Create, Import, Sample, and Skip each complete onboarding; the welcome does
      not return after restart.
- [ ] "Try a sample group" creates a deletable group with real Windows apps and
      opens Edit.
- [ ] `F1`, the title-bar Help, and the tray Help open the Help center; every
      documented shortcut/behavior is accurate.
- [ ] Export produces a valid JSON file that re-parses and contains all groups,
      paths, layout, and hotkeys.
- [ ] Import Merge adds groups, renames duplicates, and reports hotkey conflicts.
- [ ] Import Replace backs up the current config, replaces the library, and
      reloads tray/hotkeys/theme.
- [ ] Invalid/oversized/unknown-schema files are rejected without changing the
      current library.
- [ ] A backup is created once per day on first save and before import/restore;
      rotation keeps 10.
- [ ] Backups list shows date/size/count; Restore round-trips correctly.
- [ ] All new windows are themed, keyboard accessible, and fit 125%.
- [ ] Import/export/backup tests pass; self-test count does not decrease.
- [ ] Build and self-test pass.

---

## Verification

```powershell
dotnet build src/XAppGroup.csproj -c Release
$env:XAPPGROUP_DATA = "C:\Users\RedStone\AppData\Local\Temp\opencode\xappgroup-ux-fresh"
Remove-Item -Recurse -Force $env:XAPPGROUP_DATA -ErrorAction SilentlyContinue
out\XAppGroup.exe
out\XAppGroup.exe --selftest
```

Checks:

1. Clean first run: confirm welcome, each action, and no repeat after restart.
2. Export the seed, then import it into a fresh library with Merge and Replace.
3. Import a file with two duplicate names; verify renaming and counts.
4. Import invalid JSON and an unknown `SchemaVersion`; verify no change.
5. Create a backup, modify the library, restore; verify groups/settings/hotkeys.
6. Open Help from all three entry points; verify every shortcut works.
7. Tab through the welcome, Help, import summary, and backups windows.

---

## Handoff

Write `Docs/agent-report/20261108_ux-15-onboarding-help-import.md`. Include the
first-run flow, import/export round-trip counts, backup rotation/restore
evidence, and the new test names.

After this order, run the full re-test script in `UX-REVIEW.md` §6 and report
the final pass/fail table.
