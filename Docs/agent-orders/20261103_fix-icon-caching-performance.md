# Order: Fix Icon Caching & UI Thread Disk I/O Performance

## Context
Project: `XAppGroup` — a WPF + WindowsForms .NET 10 desktop app at `D:\Programing\To-Do\XAppGroup`.
Code lives in `src/`. Build: `dotnet build src/XAppGroup.csproj -c Release`.

## Required Skills
Load before starting:
- `wpf-best-practices` (`~/.agents/skills/wpf-best-practices/SKILL.md`)
- `csharp-expert` (`~/.agents/skills/csharp-expert/SKILL.md`)

## The ask
Currently, `GroupData.IconSource` calls `Icons.GroupIcon(this)` on every getter evaluation. If `IconPath` is not set, this generates a 256×256 GDI+ bitmap, extracts app icons from disk, writes a `.ico` file to `%APPDATA%\...\Icons\<id>.ico`, and decodes it from disk on every single UI layout/render pass.

Fix this so:
1. `IconSource` and `AppIcons` are cached in memory on `GroupData`.
2. Icon generation and disk write only occur on demand (first access or when `Paths` / `IconPath` explicitly change).
3. Subsequent getter reads are instantaneous zero-cost memory lookups.
4. An explicit invalidation method (e.g. `InvalidateIcons()`) is provided and called when paths or icon path change.

## What must change
| File | Change |
|---|---|
| `src/Core.cs` | Back `IconSource` and `AppIcons` with cached private fields in `GroupData`. Add `InvalidateIcons()` to clear the cache. Only call `BuildGroupIcon` when cache is dirty or empty. |
| `src/Windows.cs` | Call `InvalidateIcons()` after editing a group or changing paths before refreshing. |
| `src/SelfTest.cs` | Add verification tests ensuring getter reads do not rewrite to disk repeatedly and cache invalidation functions properly. |

## Constraints
- Do NOT break existing `.ico` generation format or custom `IconPath` fallback.
- Must pass all existing self-tests.
- Clear `out/` and rebuild with `dotnet build src/XAppGroup.csproj -c Release -o out`.

## Acceptance criteria
- Property access to `IconSource` is an in-memory return without disk file writes when clean.
- When `Paths` or `IconPath` changes and is invalidated, the new icon is generated and cached.
- All self-tests pass (`XAppGroup.exe --selftest`).
- Report delivered to `Docs/agent-report/20261103_fix-icon-caching-performance.md`.

