# Order 11 — Group lifecycle and data integrity

**Priority:** P1
**Depends on:** 05, 09, 10
**Findings:** F-56, F-57, F-58, F-59, F-60
**Files in scope:** `src/Core.cs` (`GroupData`, `Store`, naming/normalization helpers), `src/Windows.cs` (New/Duplicate/Delete/reorder, list), `src/SelfTest.cs` (naming, normalization, store recovery)
**Status:** Pending

---

## Context

The live configuration shows the lifecycle is not managed:

- 17 groups / 198 launchers, but only 3 distinct names: 15 × `abdou - Copy`,
  1 × `abdou`, 1 × `Group 1`.
- Duplicate always appends ` - Copy`, with no uniqueness check or numbering.
- Groups cannot be reordered.
- Delete calls cleanup but does not verify it and cannot undo (order 09 adds
  undo; this order makes the cleanup correct).
- `Store.Save` writes directly to the only config file. A crash mid-write can
  destroy it.
- Duplicating does not guarantee a new identity/hotkey state, and there is no
  normalization of empty/duplicate paths.

This order makes groups behave like durable user data: unique names, stable
identity, reorderable order, and recoverable storage.

---

## The ask

Enforce unique names, provide predictable duplicate names, add reordering,
normalize group contents on load, and make config saves atomic with a recoverable
backup.

---

## Required behavior

### 11.1 Unique names

- A single naming helper is the only way New/Duplicate/rename produce a name:
  - trim
  - max 80 characters
  - case-insensitive comparison
  - if free, use as-is
  - if taken, append ` (2)`, ` (3)`, … (not ` - Copy`)
  - truncate the base if needed so the suffix fits within 80 characters
- New group default: `Group 1`, `Group 2`, … using the lowest free number.
- Duplicate: `"{name} (2)"` (or next free), not `"{name} - Copy"`.
- Rename validation in order 05 must call the same helper/comparison so the UI
  and programmatic paths agree.
- Names are compared case-insensitively and with Unicode normalization
  (e.g. composed vs decomposed accents should not create duplicates).
- The group's shortcut file name uses the sanitized unique name; renaming
  updates the pinned shortcut via `SyncPinnedShortcut` and does not leave the
  old `.lnk` behind.

### 11.2 Existing duplicate migration

- On load, detect duplicate names and duplicate/empty `Id` values.
- Before changing anything:
  - copy the config to a timestamped backup (`appgroups.pre-migration-*.json`)
  - keep the original file intact until the migrated save succeeds
- Migration:
  - keep the first group's name (in collection order)
  - rename subsequent duplicates with ` (2)`, ` (3)`, … using the helper
  - assign a new GUID to empty/duplicate Ids
  - do not merge, delete, or reorder groups
  - preserve paths, layout, icons, tray flags, and order
- Save once after migration.
- Show a single info toast: "Renamed N duplicate groups." with a "Details"
  action listing old → new names (order 09 toast).
- If migration/save fails, leave the original file untouched and show the error
  path; do not start with a silently modified library.
- Running migration twice must be a no-op.

### 11.3 Normalization

- On load and before save:
  - `Paths` null → empty list.
  - remove null/empty/whitespace paths
  - trim paths
  - de-duplicate paths case-insensitively, preserving first occurrence order
  - `GroupCol` clamp 1…8
  - `SortMode` must be `Manual` or `Alphabetical`; fall back to `Manual`
  - `GroupName` null/empty → a unique default name
  - `IconPath` missing file → keep the string but treat as automatic (order 05
    shows it); do not delete the user's setting silently
- Normalization must be idempotent and covered by headless tests.

### 11.4 Reordering

- Add reordering by:
  - `Alt+Up` / `Alt+Down` on a focused card
  - "Move up" / "Move down" in the more-options menu
  - an optional drag handle if it does not conflict with the icon drag-to-pin
- The selected group moves with its item; selection stays on the same group.
- The first item cannot move up; the last cannot move down. Disable/ignore
  accordingly.
- Save immediately after reorder. Order is the persisted collection order.
- Reordering must not change shortcuts, hotkeys, or group Ids.
- Announce the move to Narrator ("Moved Productivity to position 2 of 17") via
  an accessible status/live region.
- A drag-to-reorder implementation is optional; do not reuse the 48×48 icon
  drag, which is reserved for pinning.

### 11.5 Duplicate

- Deep-copy `Paths` (new list), layout fields, icon path, sort, tray flag.
- New `Id`.
- Unique name from 11.1.
- Hotkey cleared (order 08).
- Generated icons are regenerated from the new Id; do not share the old icon
  file path unless it is a user custom icon (which may be shared read-only).
- Select the copy and save.

### 11.6 Delete and cleanup

- Delete flow is order 09's single confirmation + undo.
- After the undo window:
  - delete the generated icon file for the group Id
  - delete the group shortcut under `Store.ShortcutDir`
  - remove the pinned shortcut if it exists (and verify)
  - do not delete user-supplied custom icon files
  - do not delete app files
- Report cleanup failures in the log and as a warning toast; do not claim
  success.
- If undo is used, cancel all cleanup and restore the group at its original
  index.

### 11.7 Atomic, recoverable storage

- `Store.Save`:
  - serialize to a temp file in the same directory
  - flush to disk
  - replace the target atomically (`File.Replace` where available)
  - keep the previous file as `appgroups.bak.json`
  - on failure, leave the previous file intact and return an error to the caller
- `Store.Load`:
  - if the main file is missing, use the `.bak` if present (with an info toast)
  - if the main file is corrupt, back it up, then try the `.bak`
  - if both fail, start empty only after both backups exist and the user sees
    the error (order 09)
- Never write a partially serialized file over the only copy.
- Add headless tests for save/load, backup fallback, and corrupt-file recovery
  using a temporary `XAPPGROUP_DATA` directory.

### 11.8 Empty and large libraries

- Zero groups is valid; order 10's empty state appears.
- Deleting the last group does not throw.
- 500 groups / 5,000 launchers load and save without data loss; report the
  timings.
- No hard cap is required, but do not allocate per-group work on the UI thread
  during load beyond what is needed (order 14 handles deeper performance).

## Constraints

- Do not merge duplicate groups automatically; rename only.
- Do not change existing group Ids except empty/duplicate ones.
- Do not reorder during migration.
- Do not delete user data without a backup and an undo path.
- Do not add a database or a new serialization library.
- Keep the JSON schema backward/forward compatible: old files load, new fields
  have safe defaults.

---

## Inputs

- `src/Core.cs` `GroupData`, `Settings`, `Store`.
- `src/Windows.cs` `New`, `Duplicate`, `Delete`, `EditGroup`, card menu,
  list keyboard handling.
- `src/SelfTest.cs` store tests.
- `UX-REVIEW.md` F-56…F-60.
- Live evidence: 17 groups, 3 names. `test-data/seed-appgroups.json` contains two
  `Duplicate Name` groups for migration testing.

---

## Acceptance criteria

- [ ] New groups are named `Group 1`, `Group 2`, … with the lowest free number.
- [ ] Duplicating `Productivity` twice produces `Productivity (2)` and
      `Productivity (3)`, never ` - Copy`.
- [ ] Renaming a group to an existing name (any case) is rejected by the same
      rule used by New/Duplicate.
- [ ] Loading the seed with two `Duplicate Name` groups creates a backup,
      renames the second to `Duplicate Name (2)`, and shows one info toast.
- [ ] Running the migration a second time changes nothing.
- [ ] Duplicate/empty Ids are repaired without merging groups.
- [ ] `Alt+Up/Down` and the menu move a group; the order persists after restart;
      selection follows the group.
- [ ] Duplicating does not copy the hotkey and uses a new Id.
- [ ] Delete cleanup removes the generated icon/shortcut and reports failures;
      Undo cancels cleanup and restores the group.
- [ ] A simulated crash/failed write leaves the previous config intact; the
      `.bak` is usable.
- [ ] Normalization removes empty/duplicate paths, clamps columns, and is
      idempotent (tests).
- [ ] 500 groups load/save without data loss; timings reported.
- [ ] Build and self-test pass with new tests.

---

## Verification

```powershell
dotnet build src/XAppGroup.csproj -c Release
$env:XAPPGROUP_DATA = "C:\Users\RedStone\AppData\Local\Temp\opencode\xappgroup-ux"
Copy-Item Docs\ux-orders\test-data\seed-appgroups.json "$env:XAPPGROUP_DATA\appgroups.json" -Force
out\XAppGroup.exe
out\XAppGroup.exe --selftest
```

Checks:

1. Open the seed; confirm the migration toast, backup file, and renamed
   duplicate. Restart; confirm no second migration.
2. Duplicate a group three times; inspect names and `appgroups.json`.
3. Rename to an existing name in different case; confirm rejection.
4. Move a group with Alt+Up/Down and the menu; restart; confirm order.
5. Delete and undo; inspect icons/shortcuts after 8 seconds vs. after undo.
6. Corrupt the main file with a valid `.bak`; verify fallback.
7. Generate 500 groups and measure load/save.

---

## Handoff

Write `Docs/agent-report/20261108_ux-11-group-lifecycle.md`. Include the
migration backup path, old → new name list, atomic-save test evidence, and
reorder persistence proof.

Do not start order 12 until accepted.
