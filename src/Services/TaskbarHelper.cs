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
    /// <summary>
    /// Windows Taskbar integration: synchronization with User Pinned\TaskBar directory
    /// and shell change notifications.
    /// </summary>
    public static class TaskbarHelper
    {
        [DllImport("shell32.dll")]
        public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        public const uint SHCNE_RENAMEITEM   = 0x00000001;
        public const uint SHCNE_CREATE       = 0x00000002;
        public const uint SHCNE_DELETE       = 0x00000004;
        public const uint SHCNE_UPDATEDIR    = 0x00001000;
        public const uint SHCNE_UPDATEITEM   = 0x00002000;
        public const uint SHCNE_ASSOCCHANGED = 0x08000000;
        public const uint SHCNF_PATHW        = 0x0005;
        public const uint SHCNF_FLUSH        = 0x1000;

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        private static readonly Guid CLSID_TaskbandPin = new Guid("90AA3A4E-1CBA-4233-B8BB-535773D48449");

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("0DD79AE2-D156-45D4-9EEB-3B549769E940")]
        private interface IPinnedList3
        {
            [PreserveSig] int EnumObjects(out IEnumFullIDList ppv);
            [PreserveSig] int GetPinnableInfo(IntPtr ido, int pinnableFlag, out IntPtr psi2, out IntPtr psi1, out IntPtr ppsz, int i);
            [PreserveSig] int IsPinnable(IntPtr ido, int pinnableFlag);
            [PreserveSig] int Resolve(IntPtr hwnd, uint flags, IntPtr pidl, out IntPtr ppidlResolved);
            [PreserveSig] int LegacyModify(IntPtr unpin, IntPtr pin);
            [PreserveSig] int GetChangeCount(out uint count);
            [PreserveSig] int IsPinned(IntPtr pidl);
            [PreserveSig] int GetPinnedItem(IntPtr pidl, out IntPtr item);
            [PreserveSig] int GetAppIDForPinnedItem(IntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] out string appId);
            [PreserveSig] int ItemChangeNotify(IntPtr pidlOld, IntPtr pidlNew);
            [PreserveSig] int UpdateForRemovedItemsAsNecessary();
            [PreserveSig] int PinShellLink(uint flags, IntPtr psl);
            [PreserveSig] int GetPinnedItemForAppID([MarshalAs(UnmanagedType.LPWStr)] string appId, out IntPtr pidl);
            [PreserveSig] int Modify(IntPtr unpin, IntPtr pin, uint caller);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("D0191542-7954-4908-BC06-B2360BBE45BA")]
        private interface IEnumFullIDList
        {
            [PreserveSig] int Next(uint celt, out IntPtr rgelt, out uint pceltFetched);
            [PreserveSig] int Skip(uint celt);
            [PreserveSig] int Reset();
            [PreserveSig] int Clone(out IEnumFullIDList ppenum);
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr ILCreateFromPathW(string pszPath);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool SHGetPathFromIDListW(IntPtr pidl, System.Text.StringBuilder pszPath);

        [DllImport("shell32.dll")]
        private static extern void ILFree(IntPtr pidl);

        private static IPinnedList3 CreatePinnedList3()
        {
            try
            {
                var t = Type.GetTypeFromCLSID(CLSID_TaskbandPin);
                if (t == null) return null;
                return Activator.CreateInstance(t) as IPinnedList3;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TaskbarHelper] CreatePinnedList3 error: {ex.Message}");
                return null;
            }
        }

        private static bool UnpinPidl(IPinnedList3 pin, IntPtr pidl)
        {
            if (pin == null || pidl == IntPtr.Zero) return false;
            try
            {
                // PLMC_EXPLORER = 4
                int hr = pin.Modify(pidl, IntPtr.Zero, 4);
                if (hr >= 0) return true;
                hr = pin.Modify(pidl, IntPtr.Zero, 0);
                if (hr >= 0) return true;
                hr = pin.LegacyModify(pidl, IntPtr.Zero);
                return hr >= 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TaskbarHelper] UnpinPidl error: {ex.Message}");
                return false;
            }
        }

        private static bool UnpinPathViaCom(IPinnedList3 pin, string path)
        {
            if (pin == null || string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
            IntPtr pidl = IntPtr.Zero;
            try
            {
                pidl = ILCreateFromPathW(path);
                if (pidl == IntPtr.Zero) return false;
                return UnpinPidl(pin, pidl);
            }
            finally
            {
                if (pidl != IntPtr.Zero) ILFree(pidl);
            }
        }

        public static string UserPinnedTaskbarDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar"
        );

        public static void Unpin(GroupData g)
        {
            if (g == null) return;
            IPinnedList3 pin = null;
            try
            {
                object shell = null;
                try { shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")); } catch { }

                Func<string, bool> isGroupShortcut = f =>
                {
                    if (string.IsNullOrEmpty(f)) return false;
                    try
                    {
                        if (shell != null && File.Exists(f))
                        {
                            dynamic sc = ((dynamic)shell).CreateShortcut(f);
                            string args = sc.Arguments as string ?? "";
                            if (args.Contains("--launch " + g.Id)) return true;
                        }
                    }
                    catch { }
                    return string.Equals(Path.GetFileNameWithoutExtension(f), ShortcutHelper.Sanitize(g.GroupName), StringComparison.OrdinalIgnoreCase);
                };

                pin = CreatePinnedList3();
                if (pin != null)
                {
                    // 1. Enumerate Explorer's live Taskband pinned list and unpin exact PIDLs first
                    var toUnpin = new List<Tuple<IntPtr, string>>();
                    IEnumFullIDList en = null;
                    try
                    {
                        if (pin.EnumObjects(out en) == 0 && en != null)
                        {
                            IntPtr pidl;
                            uint fetched;
                            while (en.Next(1, out pidl, out fetched) == 0 && fetched == 1)
                            {
                                var sb = new System.Text.StringBuilder(1024);
                                string path = SHGetPathFromIDListW(pidl, sb) ? sb.ToString() : "";
                                if (isGroupShortcut(path))
                                {
                                    toUnpin.Add(Tuple.Create(pidl, path));
                                }
                                else
                                {
                                    ILFree(pidl);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[TaskbarHelper] EnumObjects error: {ex.Message}");
                    }
                    finally
                    {
                        if (en != null) Marshal.ReleaseComObject(en);
                    }

                    foreach (var item in toUnpin)
                    {
                        try
                        {
                            UnpinPidl(pin, item.Item1);
                        }
                        finally
                        {
                            ILFree(item.Item1);
                        }
                        if (!string.IsNullOrEmpty(item.Item2) && File.Exists(item.Item2))
                        {
                            try { File.Delete(item.Item2); } catch { }
                        }
                    }
                }

                // 2. Scan UserPinnedTaskbarDir for any remaining matching .lnk files
                if (Directory.Exists(UserPinnedTaskbarDir))
                {
                    foreach (var f in Directory.GetFiles(UserPinnedTaskbarDir, "*.lnk"))
                    {
                        try
                        {
                            if (isGroupShortcut(f))
                            {
                                UnpinPathViaCom(pin, f);
                                if (File.Exists(f)) File.Delete(f);
                                try
                                {
                                    IntPtr pItem = Marshal.StringToHGlobalUni(f);
                                    SHChangeNotify(SHCNE_DELETE, SHCNF_PATHW | SHCNF_FLUSH, pItem, IntPtr.Zero);
                                    Marshal.FreeHGlobal(pItem);
                                }
                                catch { }
                            }
                        }
                        catch { }
                    }
                }

                // 3. Force Explorer to purge any removed items from its Taskband cache
                try { pin?.UpdateForRemovedItemsAsNecessary(); } catch { }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TaskbarHelper] Unpin cleanup error: {ex.Message}");
            }
            finally
            {
                if (pin != null)
                {
                    try { Marshal.ReleaseComObject(pin); } catch { }
                }
            }
            NotifyShellChanged();
        }

        public static void Unpin(string shortcutPath)
        {
            IPinnedList3 pin = null;
            try
            {
                pin = CreatePinnedList3();
                if (!string.IsNullOrEmpty(shortcutPath))
                {
                    UnpinPathViaCom(pin, shortcutPath);
                    if (Directory.Exists(UserPinnedTaskbarDir))
                    {
                        var name = Path.GetFileName(shortcutPath);
                        var pinned = Path.Combine(UserPinnedTaskbarDir, name);
                        if (File.Exists(pinned))
                        {
                            UnpinPathViaCom(pin, pinned);
                            if (File.Exists(pinned)) File.Delete(pinned);
                            try
                            {
                                IntPtr pItem = Marshal.StringToHGlobalUni(pinned);
                                SHChangeNotify(SHCNE_DELETE, SHCNF_PATHW | SHCNF_FLUSH, pItem, IntPtr.Zero);
                                Marshal.FreeHGlobal(pItem);
                            }
                            catch { }
                        }
                    }
                }
                try { pin?.UpdateForRemovedItemsAsNecessary(); } catch { }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TaskbarHelper] Unpin cleanup error: {ex.Message}");
            }
            finally
            {
                if (pin != null)
                {
                    try { Marshal.ReleaseComObject(pin); } catch { }
                }
            }
            NotifyShellChanged();
        }

        /// <summary>Updates shortcut icon, name, and arguments if the group is pinned in User Pinned\TaskBar, Shortcuts folder, or Desktop.</summary>
        public static void SyncPinnedShortcut(GroupData g, string oldName = null)
        {
            try
            {
                var dirs = new List<string>();
                if (Directory.Exists(UserPinnedTaskbarDir)) dirs.Add(UserPinnedTaskbarDir);
                if (Directory.Exists(Store.ShortcutDir)) dirs.Add(Store.ShortcutDir);
                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                if (!string.IsNullOrEmpty(desktop) && Directory.Exists(desktop)) dirs.Add(desktop);

                var exe = Environment.ProcessPath;
                if (exe != null && exe.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
                {
                    var candidate = Path.Combine(AppContext.BaseDirectory, "XAppGroup.exe");
                    if (File.Exists(candidate)) exe = candidate;
                }

                // Guaranteed valid .ico path (converts .png to _custom.ico if needed, or generates 2x2 composite)
                string iconLocation = ShortcutHelper.GetValidShortcutIconLocation(g);

                var t = Type.GetTypeFromProgID("WScript.Shell");
                if (t == null) return;
                dynamic shell = Activator.CreateInstance(t);

                var targetName = ShortcutHelper.Sanitize(g.GroupName) + ".lnk";
                var oldFileName = !string.IsNullOrEmpty(oldName) ? ShortcutHelper.Sanitize(oldName) + ".lnk" : null;

                foreach (var dir in dirs)
                {
                    try
                    {
                        var files = Directory.GetFiles(dir, "*.lnk");
                        foreach (var file in files)
                        {
                            try
                            {
                                dynamic sc = shell.CreateShortcut(file);
                                string args = sc.Arguments as string ?? "";
                                string currentFileName = Path.GetFileName(file);
                                bool matches = args.Contains("--launch " + g.Id) ||
                                    (!string.IsNullOrEmpty(oldName) && (args.Contains(oldName) || (oldFileName != null && string.Equals(currentFileName, oldFileName, StringComparison.OrdinalIgnoreCase))));

                                if (matches)
                                {
                                    sc.TargetPath = exe;
                                    sc.Arguments = "--launch " + g.Id;
                                    sc.Description = g.GroupName;
                                    sc.IconLocation = iconLocation;
                                    sc.WorkingDirectory = Path.GetDirectoryName(exe);
                                    sc.Save();
                                    JumpListHelper.SetShortcutAppId(file, JumpListHelper.GetGroupAppId(g.Id));

                                    bool isTaskbarDir = string.Equals(dir, UserPinnedTaskbarDir, StringComparison.OrdinalIgnoreCase);

                                    if (!string.Equals(targetName, currentFileName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        var dest = Path.Combine(dir, targetName);
                                        if (isTaskbarDir)
                                        {
                                            // In User Pinned Taskbar, keep 'file' updated in place so Explorer's Taskband
                                            // registration never loses its file reference, and copy to dest.
                                            File.Copy(file, dest, true);
                                        }
                                        else
                                        {
                                            if (File.Exists(dest)) File.Delete(dest);
                                            File.Move(file, dest);
                                        }

                                        try
                                        {
                                            dynamic newSc = shell.CreateShortcut(dest);
                                            newSc.TargetPath = exe;
                                            newSc.Arguments = "--launch " + g.Id;
                                            newSc.Description = g.GroupName;
                                            newSc.IconLocation = iconLocation;
                                            newSc.WorkingDirectory = Path.GetDirectoryName(exe);
                                            newSc.Save();
                                            JumpListHelper.SetShortcutAppId(dest, JumpListHelper.GetGroupAppId(g.Id));
                                        }
                                        catch { }

                                        IntPtr pFrom = Marshal.StringToHGlobalUni(file);
                                        IntPtr pTo = Marshal.StringToHGlobalUni(dest);
                                        SHChangeNotify(SHCNE_RENAMEITEM, SHCNF_PATHW | SHCNF_FLUSH, pFrom, pTo);
                                        SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_PATHW | SHCNF_FLUSH, pTo, IntPtr.Zero);
                                        Marshal.FreeHGlobal(pFrom);
                                        Marshal.FreeHGlobal(pTo);
                                    }
                                    else
                                    {
                                        IntPtr pItem = Marshal.StringToHGlobalUni(file);
                                        SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_PATHW | SHCNF_FLUSH, pItem, IntPtr.Zero);
                                        Marshal.FreeHGlobal(pItem);
                                    }
                                }
                            }
                            catch { }
                        }

                        IntPtr pDir = Marshal.StringToHGlobalUni(dir);
                        SHChangeNotify(SHCNE_UPDATEDIR, SHCNF_PATHW | SHCNF_FLUSH, pDir, IntPtr.Zero);
                        Marshal.FreeHGlobal(pDir);
                    }
                    catch { }
                }

                NotifyShellChanged();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TaskbarHelper] SyncPinnedShortcut error: {ex.Message}");
            }
        }

        private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xFFFF);
        private const int WM_SETTINGCHANGE = 0x001A;
        private const uint SMTO_ABORTIFHUNG = 0x0002;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, int Msg, IntPtr wParam, string lParam, uint fuFlags, uint uTimeout, IntPtr lpdwResult);

        public static void NotifyShellChanged()
        {
            try
            {
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
                try
                {
                    SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, null, SMTO_ABORTIFHUNG, 100, IntPtr.Zero);
                }
                catch { }

                IntPtr taskbar = FindWindow("Shell_TrayWnd", null);
                if (taskbar != IntPtr.Zero)
                {
                    InvalidateRect(taskbar, IntPtr.Zero, true);
                }
                IntPtr secTaskbar = FindWindow("Shell_SecondaryTrayWnd", null);
                if (secTaskbar != IntPtr.Zero)
                {
                    InvalidateRect(secTaskbar, IntPtr.Zero, true);
                }
            }
            catch { }
        }
    }
}
