# Generates a 500-group / 5,000-launcher library for order 14 stress testing.
# Output: Docs\ux-orders\test-data\stress-appgroups.json
# Do not use this against a real %APPDATA%\XAppGroup profile.

$ErrorActionPreference = "Stop"

$apps = @(
    "C:\Windows\System32\notepad.exe",
    "C:\Windows\System32\mspaint.exe",
    "C:\Windows\System32\calc.exe",
    "C:\Windows\System32\cmd.exe",
    "C:\Windows\System32\control.exe",
    "C:\Windows\System32\taskmgr.exe",
    "C:\Windows\System32\mstsc.exe",
    "C:\Windows\System32\magnify.exe",
    "C:\Windows\System32\osk.exe",
    "C:\Windows\System32\charmap.exe"
)

$groups = for ($i = 1; $i -le 500; $i++) {
    [pscustomobject]@{
        Id         = [guid]::NewGuid().ToString("N")
        GroupName  = "Stress Group $i"
        GroupCol   = (($i - 1) % 8) + 1
        ShowLabels = ($i % 2 -eq 0)
        ShowOnTray = ($i -le 5)
        SortMode   = if ($i % 3 -eq 0) { "Alphabetical" } else { "Manual" }
        Paths      = $apps
        IconPath   = ""
    }
}

$root = [pscustomobject]@{
    Theme        = "Dark"
    CheckUpdates = $false
    Groups       = $groups
}

$out = Join-Path $PSScriptRoot "stress-appgroups.json"
$root | ConvertTo-Json -Depth 6 | Set-Content -Path $out -Encoding UTF8
Write-Host "Wrote $out ($($groups.Count) groups, $($groups.Count * $apps.Count) launchers)"
