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
    public static class Launcher
    {
        public static void Run(string path, bool admin = false)
        {
            try
            {
                var psi = new ProcessStartInfo(path) { UseShellExecute = true };
                if (admin) psi.Verb = "runas";
                try { psi.WorkingDirectory = Path.GetDirectoryName(path); } catch { }
                Process.Start(psi);
            }
            catch (Exception ex) { Debug.WriteLine(ex.Message); }
        }

        public static void RunAll(GroupData g)
        {
            foreach (var p in g.OrderedPaths()) Run(p);
        }

        /// <summary>
        /// Removes every .lnk that launches this group (matched by "--launch &lt;id&gt;"): the app's Shortcuts folder, the Desktop,
        /// and the taskbar "User Pinned" folder. Returns the removed files. A taskbar button may stay visible until Explorer restarts.
        /// </summary>
        public static List<string> DeleteShortcut(GroupData g)
        {
            var removed = new List<string>();
            var dirs = new[]
            {
                Store.ShortcutDir,
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar")
            };
            object shell = null;
            try { shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")); } catch { }
            foreach (var d in dirs)
            {
                try
                {
                    if (string.IsNullOrEmpty(d) || !Directory.Exists(d)) continue;
                    foreach (var f in Directory.GetFiles(d, "*.lnk"))
                    {
                        try
                        {
                            bool mine = false;
                            if (shell != null)
                            {
                                dynamic sc = ((dynamic)shell).CreateShortcut(f);
                                string args = sc.Arguments as string ?? "";
                                mine = args.Contains("--launch " + g.Id);
                            }
                            if (!mine)
                            {
                                mine = string.Equals(Path.GetFileNameWithoutExtension(f), Sanitize(g.GroupName), StringComparison.OrdinalIgnoreCase);
                            }
                            if (mine) { File.Delete(f); removed.Add(f); }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[DeleteShortcut] Error checking '{f}': {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DeleteShortcut] Error scanning directory '{d}': {ex.Message}");
                }
            }
            return removed;
        }

        public static string Sanitize(string n)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) n = n.Replace(c, '_');
            return string.IsNullOrWhiteSpace(n) ? "Group" : n;
        }
    }
}
