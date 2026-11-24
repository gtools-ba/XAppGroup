# XAppGroup — UI/UX Remediation Orders

This folder is the execution queue for the external AI manager ("Coder").
It contains the runtime UX review and **15 numbered orders**. Each order is one
self-contained piece of work. Do not merge orders, and do not start the next
order until the current one has been built, tested, and accepted.

> Read `../agent-orders/CODER_INSTRUCTIONS.md` first. It defines the working
> rules, build commands, report template, and scope discipline. Everything in
> this folder is an addition to those rules, not a replacement.

---

## 1. Why these orders exist

The app builds and passes its headless self-test, but a live review at
1536×864 / 125% DPI found that core UI is unreachable, actions contradict their
icons, the popup grid is laid out incorrectly, the light theme is incomplete,
the keyboard/accessibility path is missing, and several labels promise features
that do not exist.

The full evidence is in [`UX-REVIEW.md`](UX-REVIEW.md). Every order below maps
back to findings in that document.

---

## 2. Execution rules

1. **One order at a time.** Work only the lowest-numbered order whose
   dependencies are accepted. Do not batch orders into one branch/commit/report.
2. **Read the whole order before coding.** If a requirement is ambiguous, stop
   and ask; do not invent a different design.
3. **Build and test after every order.**
   - `dotnet build src/XAppGroup.csproj -c Release`
   - `out\XAppGroup.exe --selftest` → `out/selftest.log`
   - All existing tests must stay green. Add tests for new logic where the order
     asks for it.
4. **Verify the real UI, not just the build.** Run the app at 1536×864 with
   125% scaling. Check both Dark and Light themes, mouse-only and
   keyboard-only. The runtime checks listed in each order are mandatory.
5. **Report to `Docs/agent-report/`** using the template in
   `../agent-report/README.md`. Name the report with the order number, e.g.
   `20261108_ux-01-window-fit.md`. State only what you actually verified.
6. **Update the status table below** in the same change as the report:
   `Pending → In review` (not `Accepted`; only the manager accepts).
7. **Do not modify files outside the order's stated scope.** Do not refactor
   unrelated code. Do not commit, push, or branch unless an order says so.
8. **Do not regress behavior that is not explicitly being changed.** If an
   existing behavior must change, the order says so.

### Test data safety

Live review used `%APPDATA%\XAppGroup\appgroups.json`, which contains real user
groups. Do **not** test destructive operations against it.

Use an isolated data folder for all manual and automated verification:

```powershell
$env:XAPPGROUP_DATA = "C:\Users\RedStone\AppData\Local\Temp\opencode\xappgroup-ux"
New-Item -ItemType Directory -Force $env:XAPPGROUP_DATA | Out-Null
out\XAppGroup.exe
```

`Store.Dir` already honors `XAPPGROUP_DATA` (`src/Core.cs`). A repeatable seed
file for layout/popup/scroll testing is provided in
[`test-data/seed-appgroups.json`](test-data/seed-appgroups.json); see
[`test-data/README.md`](test-data/README.md) for loading instructions and the
500-group stress generator.

---

## 3. Priority and status

| Priority | Meaning |
|---|---|
| **P0** | Blocks basic use or makes a feature unreachable; fix first. |
| **P1** | Serious usability, accessibility, or trust problem. |
| **P2** | Polish, clarity, and completeness. |

Status values: `Pending`, `In progress`, `In review`, `Accepted`, `Rejected`.
Update only your own order's row.

| # | Order | Priority | Depends on | Status |
|---|---|---|---|---|
| 01 | [Window fit, DPI, and responsive layout](01-window-fit-and-responsive-layout.md) | P0 | — | Accepted |
| 02 | [Selection, command states, and layout stability](02-selection-and-command-states.md) | P0 | 01 | Accepted |
| 03 | [Card actions: make the icon match the action](03-card-action-semantics.md) | P0 | 02 | In review |
| 04 | [Popup launcher: grid math, placement, scroll, keyboard](04-popup-launcher.md) | P0 | 03 | Pending |
| 05 | [Edit Group dialog: themed, validated, usable](05-edit-dialog.md) | P1 | 02, 03 | Pending |
| 06 | [Theme correctness and contrast](06-theme-and-contrast.md) | P1 | 01–05 | Pending |
| 07 | [Keyboard, focus, and accessibility](07-keyboard-and-accessibility.md) | P1 | 02–06 | Pending |
| 08 | [Real global hotkeys (or none)](08-global-hotkeys.md) | P1 | 05, 07 | Pending |
| 09 | [Feedback, errors, and destructive actions](09-feedback-errors-destructive.md) | P1 | 05, 08 | Pending |
| 10 | [Search, no-results, and empty library](10-search-and-empty-states.md) | P1 | 02, 09 | Pending |
| 11 | [Group lifecycle and data integrity](11-group-lifecycle-and-data.md) | P1 | 05, 09, 10 | Pending |
| 12 | [Window chrome, tray, and session restore](12-window-chrome-tray-session.md) | P1 | 01, 07 | Pending |
| 13 | [Copy, information architecture, Settings/About](13-copy-and-information-architecture.md) | P2 | 03, 04, 06, 08, 12 | Pending |
| 14 | [Performance, virtualization, and motion](14-performance-and-motion.md) | P2 | 01–13 | Pending |
| 15 | [First-run, Help, import/export, backup](15-onboarding-help-import-export.md) | P2 | 09–11, 13 | Pending |

---

## 4. Dependency chain

```
01 layout
 └─ 02 selection/states
     └─ 03 card actions
         ├─ 04 popup
         └─ 05 edit dialog
             ├─ 06 theme/contrast
             │   └─ 07 keyboard/a11y
             │       └─ 08 hotkeys
             │           └─ 09 feedback/errors
             │               ├─ 10 search/empty
             │               │   └─ 11 lifecycle/data
             │               │       └─ 15 onboarding/help/import
             │               └─ ...
             └─ 12 chrome/tray/session
                 └─ 13 copy/IA/settings
                     └─ 14 performance/motion
```

Orders 12–14 can be executed after 11 in numeric order; they do not depend on
15. The numbers are the required execution order.

---

## 5. Definition of done (applies to every order)

An order is accepted only when all of the following are true:

- [ ] `dotnet build src/XAppGroup.csproj -c Release` completes with **0 warnings,
      0 errors**.
- [ ] `out\XAppGroup.exe --selftest` reports **all PASS, 0 FAIL**; the count never
      goes down.
- [ ] The order's acceptance checklist is met item by item, with evidence.
- [ ] Verified at 1536×864 / 125% in Dark and Light, plus one additional
      scaling (100% or 150%) where layout is affected.
- [ ] Verified with mouse **and** keyboard (Tab/Shift+Tab, Enter, Space, Esc,
      arrow keys, context-menu key where applicable).
- [ ] No new unhandled exception, no new silent failure, no new blocking
      `MessageBox` for a routine action.
- [ ] The report lists exact commands, results, files changed, and any
      deviations. "Looks good" is not evidence.
- [ ] The status table row is set to `In review`.

---

## 6. Design and source references

| What | Where |
|---|---|
| Approved visual reference | `Docs/Design/main screen/code.html`, `screen.png` |
| Working rules and report format | `Docs/agent-orders/CODER_INSTRUCTIONS.md` |
| Report template and status folders | `Docs/agent-report/README.md` |
| Runtime evidence and finding IDs | [`UX-REVIEW.md`](UX-REVIEW.md) |
| UI construction | `src/Windows.cs` (main, popup, edit, theme, cards) |
| Data model, icons, launchers, shortcuts | `src/Core.cs` |
| Startup, tray, single instance, CLI modes | `src/Program.cs` |
| Headless tests | `src/SelfTest.cs`, `out/selftest.log` |

The HTML reference is the visual source of truth for spacing, palette, and
composition. Where an order explicitly changes behavior (for example, the pin
button), the order wins over the HTML mock.

Source line numbers cited in `UX-REVIEW.md` are from the 2026-11-08 build.
Anchor on method/control names rather than line numbers; the code will move as
orders land.

---

## 7. What is deliberately out of scope

- Rewriting the WPF app in another UI stack.
- Changing the JSON schema in a way that breaks existing `appgroups.json`
  without a migration and backup.
- Adding telemetry, network calls, or new third-party dependencies without an
  explicit order.
- Changing the existing app name, icon, or brand palette.
- Committing or releasing. The manager handles release decisions.

---

## 8. Reporting index

As reports arrive, they live in `Docs/agent-report/` and should be named:

```
20261108_ux-01-window-fit.md
20261108_ux-02-selection-states.md
...
```

One report per order. If an order is rejected, append a `## Re-review` section
to the same report after fixing; do not open a second report for the same order.
