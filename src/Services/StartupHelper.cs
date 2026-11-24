using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace XAppGroup
{
    public static class StartupHelper
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "XAppGroup";

        public static bool IsEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false))
                {
                    var val = key?.GetValue(AppName) as string;
                    return !string.IsNullOrWhiteSpace(val);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StartupHelper] IsEnabled error: {ex.Message}");
                return false;
            }
        }

        public static bool SetEnabled(bool enable)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                                 ?? Registry.CurrentUser.CreateSubKey(RunKeyPath))
                {
                    if (key == null) return false;
                    if (enable)
                    {
                        var exe = Environment.ProcessPath;
                        if (exe != null && exe.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
                        {
                            var candidate = Path.Combine(AppContext.BaseDirectory, "XAppGroup.exe");
                            if (File.Exists(candidate)) exe = candidate;
                        }
                        if (string.IsNullOrWhiteSpace(exe)) return false;
                        key.SetValue(AppName, $"\"{exe}\"");
                    }
                    else
                    {
                        if (key.GetValue(AppName) != null)
                            key.DeleteValue(AppName, throwOnMissingValue: false);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StartupHelper] SetEnabled({enable}) error: {ex.Message}");
                return false;
            }
        }
    }
}
