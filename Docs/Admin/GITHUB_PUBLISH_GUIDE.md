# 🚀 XAppGroup — Developer's GitHub Publishing & Release Guide

This private guide is for **you (the developer)**. It walks you through setting your official support links, pushing your source code and documentation to GitHub, and creating a public release for users to download.

---

## 📋 Table of Contents
1. [Step 1: Set Your Official Support & Repository Links](#step-1-set-your-official-support--repository-links)
2. [Step 2: Initialize Git & Push to GitHub (First Time)](#step-2-initialize-git--push-to-github-first-time)
3. [Step 3: Create the GitHub Release & Upload the Zip](#step-3-create-the-github-release--upload-the-zip)
4. [Step 4: Pushing Future Updates](#step-4-pushing-future-updates)
5. [📦 Ready-to-Copy GitHub Release Notes](#-ready-to-copy-github-release-notes)

---

## Step 1: Set Your Official Support & Repository Links

Because XAppGroup is 100% free, the in-app **Support Us** dialog and header buttons are designed to direct users to **your personal donation page (Ko-fi)** and **your GitHub repository**.

Before releasing, open:
📁 `Docs/Admin/realise-v1.2.0/appconfig.json`

Replace the placeholders with your actual username and Ko-fi handle:
```json
{
  "GitHubUrl": "https://github.com/gtools-ba/XAppGroup",
  "SupportUrl": "https://ko-fi.com/<YOUR_KOFI_HANDLE>",
  "SupportPromptEnabled": true,
  "SupportPromptEveryNLaunches": 5,
  "SupportPromptCooldownDays": 3
}
```

> [!TIP]
> Also update `src/Models/AppConfig.cs` (lines 14–15) with the same links so the hardcoded fallback inside the `.exe` matches if `appconfig.json` is ever moved. Then re-zip `Docs/Admin/realise-v1.2.0/XAppGroup-v1.2.0-win-x64.zip`.

---

## Step 2: Initialize Git & Push to GitHub (First Time)

If you haven't uploaded your project to GitHub yet, follow these commands in PowerShell or Command Prompt from your project root (`D:\Programing\To-Do\XAppGroup`):

### 1. Create your new repository on GitHub:
1. Open [github.com/new](https://github.com/new).
2. Name the repository: `XAppGroup`.
3. Choose **Public**.
4. **Do NOT** check "Add a README file" or ".gitignore" (we have already created them for you).
5. Click **Create repository**.

### 2. Push your project from your PC:
Open PowerShell in `D:\Programing\To-Do\XAppGroup` and run:

```powershell
# 1. Initialize local git repository
git init

# 2. Stage all project files (ignoring build caches via .gitignore)
git add .

# 3. Create your first commit
git commit -m "feat: XAppGroup v1.2.0 release - modern taskbar flyouts, global hotkeys, and unified squircle icon"

# 4. Set the default branch to main
git branch -M main

# 5. Connect your local project to your new GitHub repository:
git remote add origin https://github.com/gtools-ba/XAppGroup.git

# 6. Push everything to GitHub
git push -u origin main
```

---

## Step 3: Create the GitHub Release & Upload the Zip

Once your code is pushed to GitHub, publish the executable package so users can download it with one click:

1. Go to your repository page on GitHub: `https://github.com/gtools-ba/XAppGroup`.
2. On the right-hand sidebar, click **Releases** (or click **"Create a new release"**).
3. Click **Draft a new release**.
4. Fill in the release details:
   - **Choose a tag:** Type `v1.2.0` and click **Create new tag: v1.2.0 on main**.
   - **Release title:** `XAppGroup v1.2.0 — Smart App Grouping & Taskbar Flyout Launcher`
   - **Attach binaries:** In the box that says *"Attach binaries by dropping them here or selecting them"*, drag and drop this file from your PC:
     📂 `Docs/Admin/realise-v1.2.0/XAppGroup-v1.2.0-win-x64.zip`
5. Paste the **Release Notes** from the section below into the description box.
6. Click **Publish release**.

Users will now see a green download button for `XAppGroup-v1.2.0-win-x64.zip`!

---

## Step 4: Pushing Future Updates

When you make changes or release a new version in the future:

```powershell
# 1. Stage modified files
git add .

# 2. Commit your changes
git commit -m "fix/feat: description of what was changed"

# 3. Push to GitHub
git push origin main
```

---

## 📦 Ready-to-Copy GitHub Release Notes

*(Copy and paste the text below into GitHub's release description box when publishing v1.2.0)*

```markdown
# 🚀 XAppGroup v1.2.0 Release

We are excited to launch **XAppGroup v1.2.0** — the smart, modern application grouping and taskbar flyout launcher for Windows 10 and Windows 11!

### 🌟 Highlights
- 📌 **Taskbar & Desktop Group Pinning**: Pin multi-application suites directly to your Windows Taskbar as a single high-definition icon.
- ⚡ **Elevated Flyout Launcher**: Smooth 260ms quintic-eased bottom-up animation for launching individual apps or the entire group with one click.
- 🖱️ **Contextual Taskbar Jump Lists**: Right-click any pinned taskbar group to quickly "Open All Applications" or "Edit this Group".
- ⌨️ **Global Win32 Hotkeys**: Bind custom system-wide shortcuts (e.g. `Ctrl + Alt + D`) to summon any group flyout from anywhere in Windows.
- 🎨 **Obsidian Dark & Light Glass**: Handcrafted modern UI with subtle Amber Gold (`#F59E0B`) accents, customizable backdrop blur, and smooth switches.
- 🧹 **Clean Lifecycle & Unpin Automation**: Deleting a group automatically unpins it from the Taskbar, deletes desktop shortcuts, and cleans up Jump Lists without orphaned files.
- 🚀 **100% Portable**: No installer needed, zero registry bloat. Download, unzip, and run!

---

### 📥 Download & Quick Start
1. Download **`XAppGroup-v1.2.0-win-x64.zip`** below.
2. Extract the zip to any folder on your PC (e.g. `C:\Tools\XAppGroup` or `Desktop`).
3. Double-click **`XAppGroup.exe`** to run.

*Requires 64-bit Windows 10 (Build 19041+) or Windows 11 and [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0).*

---

### 💖 Support Continued Development
XAppGroup is 100% free and open-source. If it helps organize your workspace, please consider starring the repository ⭐ and [supporting our work on Ko-fi](https://ko-fi.com/)!
```
