# UX order test data

## `seed-appgroups.json`

A deterministic library for manual verification:

- 14 groups / 120 launcher entries.
- Column counts 1, 2, 3, 4, 5, 8 (popup grid math).
- "Popup Stress" has 30 apps (scroll + placement).
- "A Very Long Group Name…" (title wrapping).
- "Missing Apps" (missing-file feedback).
- Two groups named "Duplicate Name" (order 11 migration).
- `ShowLabels` is mixed; one group has labels off.
- Paths point at standard Windows binaries and some intentionally missing
  files. No real user data is included.

### Load it into an isolated profile

```powershell
$env:XAPPGROUP_DATA = "C:\Users\RedStone\AppData\Local\Temp\opencode\xappgroup-ux"
New-Item -ItemType Directory -Force $env:XAPPGROUP_DATA | Out-Null
Copy-Item Docs\ux-orders\test-data\seed-appgroups.json "$env:XAPPGROUP_DATA\appgroups.json" -Force
out\XAppGroup.exe
```

Close the app before replacing the file. Never point `XAPPGROUP_DATA` at
`%APPDATA%\XAppGroup` while testing destructive flows.

## `generate-stress.ps1`

Creates `stress-appgroups.json` with 500 groups × 10 apps for order 14
(virtualization, scrolling, startup, memory). Run:

```powershell
powershell -ExecutionPolicy Bypass -File Docs\ux-orders\test-data\generate-stress.ps1
```

Then copy `stress-appgroups.json` over `$env:XAPPGROUP_DATA\appgroups.json` and
launch. Delete the file and folder when finished.
