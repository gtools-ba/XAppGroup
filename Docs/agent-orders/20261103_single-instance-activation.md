# Order: Single-Instance Window Activation

## Context
Project: `XAppGroup` — a WPF + WindowsForms .NET 10 desktop app at `D:\Programing\To-Do\XAppGroup`.
Code lives in `src/`. Build: `dotnet build src/XAppGroup.csproj -c Release`.

## Required Skills
Load before starting:
- `desktop-ui-design` (`~/.agents/skills/desktop-ui-design/SKILL.md`)
- `wpf-best-practices` (`~/.agents/skills/wpf-best-practices/SKILL.md`)

## The ask
Currently in `src/Program.cs`:
```csharp
var mutex = new Mutex(true, "XAppGroup.Manager", out bool first);
if (!first) { Shutdown(); return; }
```
When `XAppGroup` is already running (e.g., hidden in the system tray) and the user launches the app again, it exits silently. The user is left confused because "nothing happened."

Fix this so:
1. When a secondary instance starts without arguments, it signals the primary running instance to restore, un-hide, and bring `MainWindow` to the foreground (`SetForegroundWindow`).
2. The secondary instance then terminates cleanly.
3. If `--launch <id>` or `--selftest` is passed, existing argument handling continues to function as designed.

## What must change
| File | Change |
|---|---|
| `src/Program.cs` | Implement cross-process window activation (using a Win32 registered window message, event wait handle, or named pipe). If not the first instance, signal the existing instance to `Show()`, `WindowState = Normal`, and `Activate()` / `SetForegroundWindow()`. |
| `src/SelfTest.cs` | Add test validating single-instance detection logic. |

## Constraints
- Do NOT break `--launch <id>` popup behavior (popups are allowed to launch even while manager runs).
- Do NOT break `--selftest`.
- Must pass all self-tests.
- Rebuild cleanly into `out/`.

## Acceptance criteria
- Launching a second instance of `XAppGroup.exe` causes the already running instance's `MainWindow` to show and become the active foreground window.
- All self-tests pass.
- Report delivered to `Docs/agent-report/20261103_single-instance-activation.md`.

