# Order: Check project structure and file lengths

## Context
Project: `XAppGroup` — a WPF + WindowsForms .NET 10 desktop app at `D:\Programing\To-Do\XAppGroup`.
Code lives in `src/`. Output goes to `out/`.

## The ask
Inspect every source file under the project root and verify two things:
1. **File count** — list every `.cs` file (or all files) found recursively.
2. **Line count** — for each file, report the number of lines. Flag any file that exceeds **600 lines**.

Deliver a concise report listing each file and its line count, highlighting those >600 lines. Use the report template in `Docs/agent-report/README.md`.

## Constraints
- Do NOT modify any files.
- Do NOT create new files as part of this order.
- Use `dotnet build src/XAppGroup.csproj -c Release` only if needed to verify the build state — the primary task is reading and counting lines.

## Acceptance criteria
- A markdown report in `Docs/agent-report/20261103_check-structure.md` that lists each source file and its line count, with a "FLAG" marker next to any file >600 lines.
- The report must be verifiable: the Coder must show the exact command(s) used to count lines.

## How to verify
After the Coder writes the report, I will read it and confirm:
- All `.cs` files under the project are listed.
- Line counts are accurate (I'll spot-check a couple).
- Any file >600 lines is correctly flagged.