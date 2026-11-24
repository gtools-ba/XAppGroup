# Skills installed in this session — handoff for an external AI agent (Coder)

Project: `XAppGroup` — a WPF + WindowsForms .NET 10 desktop app (`net10.0-windows`).
Install each skill with the Skills CLI. Run from any folder:

```bash
npx skills add <owner/repo@skill> -g -y
```

`-g` = user/global (goes to `~/.agents/skills/`, symlinked into Claude Code).
Omit `-g` to install into the current project's `.agents/skills/`.

---

## UX / UI / app design (platform-specific)

| # | Skill | Command | Why it's here |
|---|-------|---------|---------------|
| 1 | **Windows (Fluent / WinUI)** | `npx skills add microsoft/winappcli@winui-design -g -y` | WinUI 3 XAML guidance, Fluent Design, theming, accessibility, `winapp find-api`. |
| 2 | **macOS (HIG)** | `npx skills add ehmo/platform-design-skills@macos-design-guidelines -g -y` | Apple HIG for SwiftUI/AppKit: menu bars, toolbars, window mgmt, keyboard shortcuts. |
| 3 | **macOS HIG (designer)** | `npx skills add designnotdrum/skills@macos-hig-designer -g -y` | Designer-flavored macOS HIG guidance. |
| 4 | **Cross-platform desktop UI** | `npx skills add bateau84/skills@desktop-ui-design -g -y` | Desktop GUI design across Windows/macOS (native + hybrid). |
| 5 | **WPF best practices** | `npx skills add jcurbelo/skills@wpf-best-practices -g -y` | WPF/XAML, MVVM, system-tray apps — matches the XAppGroup codebase. |

## Design systems / aesthetics

| # | Skill | Command | Why it's here |
|---|-------|---------|---------------|
| 6 | **Design system** | `npx skills add nextlevelbuilder/ui-ux-pro-max-skill@design-system -g -y` | Token architecture (primitive→semantic→component), spacing/typography scales, component specs. |
| 7 | **Frontend design** | `npx skills add anthropics/skills@frontend-design -g -y` | Distinctive, intentional visual design direction. |
| 8 | **UI taste** | `npx skills add uizze.sh@ui-taste -g -y` | Aesthetic direction / taste calibration. |

## Accessibility

| # | Skill | Command | Why it's here |
|---|-------|---------|---------------|
| 9 | **Accessibility (a11y)** | `npx skills add mindrally/skills@accessibility-a11y -g -y` | WCAG-inclusive UI best practices. |

## Engineering (C# / .NET / packaging)

| # | Skill | Command | Why it's here |
|---|-------|---------|---------------|
| 10 | **C# expert** | `npx skills add dotnet/skills@csharp-expert -g -y` | C#/.NET routing, semantics, framework questions. |
| 11 | **C# refactoring** | `npx skills add dotnet/skills@csharp-refactoring -g -y` | Safe refactors, renames, modernization. |
| 12 | **MSIX packaging** | `npx skills add wshaddix/dotnet-skills@dotnet-msix -g -y` | MSIX creation, signing, Store, App Installer, auto-update. |

## Meta

| # | Skill | Command | Why it's here |
|---|-------|---------|---------------|
| 13 | **Find skills** | `npx skills add vercel-labs/skills --skill find-skills -g -y` | Discover/install more skills at runtime. |

---

## Reproducibility

`skills-lock.json` (project root) records every installed skill with its source repo
and the SHA-256 of its `SKILL.md`. To install exactly what is recorded here, run:

```bash
npx skills add <owner/repo@skill> -g -y
```

and verify the resulting hash matches the lock file.

## Notes for the external agent

- These skills are **instructions/context only** — they add no tooling. The app still needs the .NET SDK (`dotnet`) to build.
- `reverse-engineer-anything` is pre-existing in the environment and was **not** installed this session — it's intentionally omitted from the list above.
- **WPF vs WinUI scope:** `winui-design` and `wpf-best-practices` are mutually exclusive in scope. Use WinUI guidance for Windows App SDK / WinUI 3, WPF guidance for WPF. XAppGroup is **WPF**, so `wpf-best-practices` is the primary one; `winui-design` is reference for modern Windows visual language (Fluent rounded corners, accent colors, elevation) if the UI is ever ported.
- **macOS skills** (`macos-design-guidelines`, `macos-hig-designer`) apply only if a macOS build/variant is planned — XAppGroup is Windows-only today, but useful for cross-platform design decisions.
- **`desktop-ui-design`** covers platform conventions for both Windows Fluent and macOS HIG in one skill.
- To list what is installed at any time: `npx skills list` (or `ls ~/.agents/skills/`).