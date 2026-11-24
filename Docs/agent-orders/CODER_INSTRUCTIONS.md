# Order 0 — Working instructions for the Coder agent

Read this **first**, before any task order. Then work orders one at a time.

---

## 1. How to work

- **One order at a time.** Do not start a second order until the current one is tested and confirmed. If an order is unclear, ask — do not guess and do not proceed.
- **Test before reporting.** After every change, build and run the verification the order specifies. Report only what you actually verified, not what you intended.
- **Short, factual reports.** Use the template in `Docs/agent-report/README.md`. No prose, no speculation. Say what you changed, the files, the commands you ran, and the result.

## 2. Skills

- Load the relevant skill **before** starting any order. The full install list and commands are in `Docs/agent-orders/SKILLS_INSTALLED.md`.
- The `api-anything` skill is available for fetching data from websites (Goodreads, etc.). Use it instead of web search or a browser when the order involves site data.
- If a needed skill is not installed, install it with `npx skills add <owner/repo@skill> -g -y` and record it in `skills-lock.json`.

## 3. Project context

- **Project:** `XAppGroup` — a WPF + WindowsForms .NET 10 desktop app.
- **Build:** `dotnet build src/XAppGroup.csproj -c Release` (output lands in `out/`).
- **Self-test:** `XAppGroup.exe --selftest` writes `out/selftest.log`. Use it to verify functional behavior headlessly.
- **Auto-rebuild:** After each order's code changes, the Coder **must** run `dotnet build src/XAppGroup.csproj -c Release` to update `out/`. If `out/` is locked by a running instance, close it first or use the `src/bin/Release/net10.0-windows/` output directly.
- **Source:** `src/` — `Core.cs`, `Windows.cs`, `Program.cs`, `SelfTest.cs`, `XAppGroup.csproj`.
- **Config data:** `%APPDATA%\XAppGroup\appgroups.json` (created by the app at runtime).

## 4. Where things go

| Direction | Path |
|---|---|
| Your orders (from the user) | `Docs/agent-orders/YYYYMMDD_<slug>.md` |
| Your reports (to the user) | `Docs/agent-report/YYYYMMDD_<slug>.md` |
| Skill install list | `Docs/agent-orders/SKILLS_INSTALLED.md` |
| Skill lock (reproducibility) | `skills-lock.json` |

## 5. Report template

```markdown
# Report: <slug>
Order: Docs/agent-orders/<order file>
Date: YYYY-MM-DD

## What was done
- <concrete changes>

## Files changed
- <path> — <one line>

## How to verify
- <exact commands run>
- <result: PASS / FAIL>

## Open questions / decisions needed
- <anything you could not decide>
```

## 6. Rules

- Do not commit, push, or create branches unless the order explicitly says so.
- Do not modify files outside the scope of the current order.
- If a build or test fails, fix it within the same order — do not report failure and move on.
- Preserve existing behavior unless the order asks to change it.