# Order: Thread Safety, Resource Hygiene, and Error Diagnostics

## Context
Project: `XAppGroup` — a WPF + WindowsForms .NET 10 desktop app at `D:\Programing\To-Do\XAppGroup`.
Code lives in `src/`. Build: `dotnet build src/XAppGroup.csproj -c Release`.

## Required Skills
Load before starting:
- `wpf-best-practices` (`~/.agents/skills/wpf-best-practices/SKILL.md`)
- `csharp-expert` (`~/.agents/skills/csharp-expert/SKILL.md`)

## The ask
1. **Thread safety:** `Icons.Cache` is currently a non-thread-safe `Dictionary<string, BitmapSource>`. Make it thread-safe using `ConcurrentDictionary` or proper locking.
2. **Error hygiene:** Over 12 empty `catch { }` blocks silently swallow errors in `Core.cs` and `Windows.cs`. Replace blind empty `catch` blocks with explicit exception types, debug logging (`Debug.WriteLine`), or structured fallback handling as prescribed in `wpf-best-practices` ("Don't leave empty catch { } blocks").
3. **Handle cleanup:** Ensure Win32 icon handles or GDI resources are cleanly disposed.

## What must change
| File | Change |
|---|---|
| `src/Core.cs` | Replace `Dictionary` with `ConcurrentDictionary<string, BitmapSource>`. Audit and replace blind `catch { }` with logged/safe fallbacks. |
| `src/Windows.cs` | Audit and replace blind `catch { }` blocks in UI operations. |
| `src/SelfTest.cs` | Verify concurrent access to `Icons.Get()` does not throw or corrupt state. |

## Constraints
- Do NOT crash on expected harmless errors (such as missing files or unregistered verbs); handle them gracefully with diagnostics instead of silent suppression.
- All self-tests must pass.
- Rebuild cleanly into `out/`.

## Acceptance criteria
- `Icons.Cache` is thread-safe under concurrent access.
- Critical paths log error diagnostic information in debug mode instead of completely swallowing exceptions.
- All self-tests pass.
- Report delivered to `Docs/agent-report/20261103_thread-safety-and-error-logging.md`.

