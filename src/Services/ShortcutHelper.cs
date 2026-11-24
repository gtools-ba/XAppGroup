using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace XAppGroup
{
    public static class ShortcutHelper
    {
        /// <summary>
        /// Returns an absolute file path to a valid .ico file for this group, guaranteed to exist on disk.
        /// If a custom icon is set (.png, .jpg, etc.), converts and caches it as <Id>_custom.ico.
        /// If an .ico is set, returns it directly.
        /// If an .exe or .dll is set, returns path,0.
        /// If no custom icon or missing, generates and returns the 2x2 composite .ico via BuildGroupIcon(g).
        /// </summary>
        public static string GetValidShortcutIconLocation(GroupData g)
        {
            if (g == null) return null;
            try
            {
                Directory.CreateDirectory(Store.IconDir);
                if (!string.IsNullOrWhiteSpace(g.IconPath) && File.Exists(g.IconPath))
                {
                    var ext = Path.GetExtension(g.IconPath).ToLowerInvariant();
                    if (ext == ".ico")
                    {
                        return g.IconPath;
                    }
                    if (ext == ".exe" || ext == ".dll")
                    {
                        return g.IconPath + ",0";
                    }

                    // Convert any other image (.png, .jpeg, etc.) to a proper Windows .ico
                    var icoFile = Path.Combine(Store.IconDir, g.Id + "_custom.ico");
                    try
                    {
                        using var bmp = new Bitmap(g.IconPath);
                        Icons.WriteIco(bmp, icoFile);
                        if (File.Exists(icoFile)) return icoFile;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ShortcutHelper] Custom icon conversion failed: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShortcutHelper] GetValidShortcutIconLocation error: {ex.Message}");
            }

            // Fallback: guaranteed generated 2x2 composite .ico
            return Icons.BuildGroupIcon(g);
        }

        /// <summary>Creates a clean Windows shortcut (.lnk) that launches this group.</summary>
        public static string Create(GroupData g, string folder)
        {
            Directory.CreateDirectory(folder);
            var lnk = Path.Combine(folder, Sanitize(g.GroupName) + ".lnk");
            string iconLocation = GetValidShortcutIconLocation(g);

            var exe = Environment.ProcessPath;
            if (exe != null && exe.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
            {
                var candidate = Path.Combine(AppContext.BaseDirectory, "XAppGroup.exe");
                if (File.Exists(candidate)) exe = candidate;
            }
            var t = Type.GetTypeFromProgID("WScript.Shell");
            dynamic shell = Activator.CreateInstance(t);
            dynamic sc = shell.CreateShortcut(lnk);
            sc.TargetPath = exe;
            sc.Arguments = "--launch " + g.Id;
            sc.WorkingDirectory = Path.GetDirectoryName(exe);
            sc.IconLocation = iconLocation;
            sc.Description = g.GroupName;
            sc.Save();

            JumpListHelper.SetShortcutAppId(lnk, JumpListHelper.GetGroupAppId(g.Id));
            return lnk;
        }

        public static string Sanitize(string n)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) n = n.Replace(c, '_');
            return string.IsNullOrWhiteSpace(n) ? "Group" : n;
        }
    }
}
