# Report: check-structure
Order: Docs/agent-orders/20261103_check-structure.md
Date: 2026-11-03

## What was done
- Listed all files recursively under the project root and counted lines. No files were modified.
- Excluded `.git`, `obj`, `bin`, `out`, `node_modules` (build output / tooling).
- Found 5 source files in `src/` (4 `.cs`, 1 `.csproj`) and 13 files in total. **No file exceeds 600 lines, so nothing is flagged.**
- The build was not run (the order says only if needed).

## Line counts (threshold: 600)
| Lines | File | Flag |
|---|---|---|
| 213 | src/Core.cs | - |
| 87 | src/Program.cs | - |
| 92 | src/SelfTest.cs | - |
| 350 | src/Windows.cs | - |
| 14 | src/XAppGroup.csproj | - |
| 83 | skills-lock.json | - |
| 141 | .agents/skills/find-skills/SKILL.md | - |
| 16 | .claude/settings.local.json | - |
| 27 | Docs/agent-orders/20261103_check-structure.md | - |
| 62 | Docs/agent-orders/CODER_INSTRUCTIONS.md | - |
| 18 | Docs/agent-orders/README.md | - |
| 73 | Docs/agent-orders/SKILLS_INSTALLED.md | - |
| 39 | Docs/agent-report/README.md | - |

## Files changed
- Docs/agent-report/20261103_check-structure.md — this report (the only file created, as the acceptance criteria require)

## How to verify
Command run (PowerShell, from any folder):
```powershell
$r='D:\Programing\To-Do\XAppGroup\'; Get-ChildItem $r -Recurse -File -Force | Where-Object { $_.FullName -notmatch '\\(\.git|obj|bin|out|node_modules)\\' } | ForEach-Object { '{0}	{1}' -f [IO.File]::ReadAllLines($_.FullName).Length, $_.FullName.Substring($r.Length) }
```
- Result: PASS. 13 files listed, 0 over 600 lines.
- The count uses `ReadAllLines().Length`, which includes blank lines. `Measure-Object -Line` skips blank lines, so it gives lower numbers.
- The `out/` folder was not present or was excluded, so it holds no source files.

## Open questions / decisions needed
- None. The skill-load step was skipped because no listed skill applies to a read-only line count.

