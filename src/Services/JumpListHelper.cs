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
    public static class JumpListHelper
    {
        private static readonly Guid CLSID_DestinationList = new Guid("77f10cf0-3db5-4966-b520-b7c54fd35ed6");
        private static readonly Guid CLSID_EnumerableObjectCollection = new Guid("2d3468c1-36a7-43b6-ac24-d3f02fd9607a");
        private static readonly Guid CLSID_ShellLink = new Guid("00021401-0000-0000-C000-000000000046");
        private static readonly Guid IID_IObjectArray = new Guid("92CA9DCD-5622-4bba-A805-5E9F541BD8C9");
        private static readonly Guid IID_IPropertyStore = new Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99");
        private static readonly Guid FMTID_AppUserModel = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");

        [DllImport("shell32.dll")]
        private static extern int SHGetPropertyStoreForWindow(
            IntPtr hwnd,
            ref Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IPropertyStore ppv);

        [DllImport("ole32.dll")]
        private static extern int PropVariantClear(ref PROPVARIANT pvar);

        [ComImport, Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellLinkW
        {
            [PreserveSig] int GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);
            [PreserveSig] int GetIDList(out IntPtr ppidl);
            [PreserveSig] int SetIDList(IntPtr pidl);
            [PreserveSig] int GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszName, int cch);
            [PreserveSig] int SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            [PreserveSig] int GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszDir, int cch);
            [PreserveSig] int SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            [PreserveSig] int GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszArgs, int cch);
            [PreserveSig] int SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            [PreserveSig] int GetHotkey(out short pwHotkey);
            [PreserveSig] int SetHotkey(short wHotkey);
            [PreserveSig] int GetShowCmd(out int piShowCmd);
            [PreserveSig] int SetShowCmd(int iShowCmd);
            [PreserveSig] int GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszIconPath, int cch, out int piIcon);
            [PreserveSig] int SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            [PreserveSig] int SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            [PreserveSig] int Resolve(IntPtr hwnd, uint fFlags);
            [PreserveSig] int SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct PROPERTYKEY
        {
            public Guid fmtid;
            public uint pid;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROPVARIANT
        {
            public ushort vt;
            public ushort wReserved1;
            public ushort wReserved2;
            public ushort wReserved3;
            public IntPtr p;
            public int p2;
        }

        [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyStore
        {
            [PreserveSig] int GetCount(out uint cProps);
            [PreserveSig] int GetAt(uint iProp, out PROPERTYKEY pkey);
            [PreserveSig] int GetValue(ref PROPERTYKEY key, out PROPVARIANT pv);
            [PreserveSig] int SetValue(ref PROPERTYKEY key, ref PROPVARIANT propvar);
            [PreserveSig] int Commit();
        }

        [ComImport, Guid("5632B1A4-E38A-400A-928A-D4CD63230295"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IObjectCollection
        {
            [PreserveSig] int GetCount(out uint pcObjects);
            [PreserveSig] int GetAt(uint uiIndex, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);
            [PreserveSig] int AddObject([MarshalAs(UnmanagedType.IUnknown)] object punk);
            [PreserveSig] int AddFromArray(IntPtr poaSource);
            [PreserveSig] int RemoveObjectAt(uint uiIndex);
            [PreserveSig] int Clear();
        }

        [ComImport, Guid("6332debf-87b5-4670-90c0-5e57b408a49e"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ICustomDestinationList
        {
            [PreserveSig] int SetAppID([MarshalAs(UnmanagedType.LPWStr)] string pszAppID);
            [PreserveSig] int BeginList(out uint pcMinSlots, ref Guid riid, out IntPtr ppv);
            [PreserveSig] int AppendCategory([MarshalAs(UnmanagedType.LPWStr)] string pszCategory, IntPtr poa);
            [PreserveSig] int AppendKnownCategory(int category);
            [PreserveSig] int AddUserTasks([MarshalAs(UnmanagedType.IUnknown)] object poa);
            [PreserveSig] int CommitList();
            [PreserveSig] int GetRemovedDestinations(ref Guid riid, out IntPtr ppv);
            [PreserveSig] int DeleteList([MarshalAs(UnmanagedType.LPWStr)] string pszAppID);
            [PreserveSig] int AbortList();
        }

        public static string LastGroupFile => Path.Combine(Store.Dir, "last_group.txt");
        public static string EditRequestFile => Path.Combine(Store.Dir, "edit_request.txt");

        public static void SaveLastGroupId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            try
            {
                Directory.CreateDirectory(Store.Dir);
                File.WriteAllText(LastGroupFile, id.Trim());
            }
            catch { }
        }

        public static string LoadLastGroupId()
        {
            try
            {
                if (File.Exists(LastGroupFile))
                {
                    var text = File.ReadAllText(LastGroupFile).Trim();
                    if (!string.IsNullOrEmpty(text)) return text;
                }
            }
            catch { }
            return null;
        }

        public static void SaveEditRequest(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            try
            {
                Directory.CreateDirectory(Store.Dir);
                File.WriteAllText(EditRequestFile, id.Trim());
            }
            catch { }
        }

        public static string ConsumeEditRequest()
        {
            try
            {
                if (File.Exists(EditRequestFile))
                {
                    var text = File.ReadAllText(EditRequestFile).Trim();
                    try { File.Delete(EditRequestFile); } catch { }
                    if (!string.IsNullOrEmpty(text)) return text;
                }
            }
            catch { }
            return null;
        }

        private static object CreateTaskLink(string exePath, string arguments, string title, string description)
        {
            var link = (IShellLinkW)Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_ShellLink));
            link.SetPath(exePath);
            link.SetArguments(arguments ?? "");
            link.SetWorkingDirectory(Path.GetDirectoryName(exePath) ?? AppContext.BaseDirectory);
            if (!string.IsNullOrEmpty(description)) link.SetDescription(description);
            link.SetIconLocation(exePath, 0);

            var ps = (IPropertyStore)link;
            var pkeyTitle = new PROPERTYKEY { fmtid = new Guid("F29F85E0-4FF9-1068-AB91-08002B27B3D9"), pid = 2 };
            var pv = new PROPVARIANT { vt = 31, p = Marshal.StringToCoTaskMemUni(title) };
            try
            {
                ps.SetValue(ref pkeyTitle, ref pv);
                ps.Commit();
            }
            finally
            {
                if (pv.p != IntPtr.Zero) Marshal.FreeCoTaskMem(pv.p);
            }
            return link;
        }

        public static string GetGroupAppId(string groupId) =>
            string.IsNullOrWhiteSpace(groupId) ? null : ("XAppGroup.Group." + groupId.Trim());

        public static bool SetShortcutAppId(string lnkPath, string appId)
        {
            if (string.IsNullOrWhiteSpace(lnkPath) || !File.Exists(lnkPath) || string.IsNullOrWhiteSpace(appId))
                return false;
            try
            {
                var link = Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_ShellLink));
                var pf = (System.Runtime.InteropServices.ComTypes.IPersistFile)link;
                pf.Load(lnkPath, 2); // STGM_READWRITE = 2
                var ps = (IPropertyStore)link;
                SetPropString(ps, FMTID_AppUserModel, 5, appId); // PKEY_AppUserModel_ID
                ps.Commit();
                pf.Save(lnkPath, true);
                Marshal.ReleaseComObject(link);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[JumpListHelper] SetShortcutAppId error: {ex.Message}");
                return false;
            }
        }

        public static int TryAddUserTasks(GroupData g, bool commit, out int taskCount, string appId = null)
        {
            taskCount = 0;
            try
            {
                string targetId = g?.Id ?? LoadLastGroupId();
                if (!string.IsNullOrWhiteSpace(targetId)) SaveLastGroupId(targetId);

                string exe = Process.GetCurrentProcess().MainModule?.FileName
                    ?? Path.Combine(AppContext.BaseDirectory, "XAppGroup.exe");

                string openAllArgs = string.IsNullOrWhiteSpace(targetId) ? "--open-all" : ("--open-all " + targetId);
                string editArgs = string.IsNullOrWhiteSpace(targetId) ? "--edit" : ("--edit " + targetId);

                var col = (IObjectCollection)Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_EnumerableObjectCollection));
                bool includeOpenAll = !commit || (Settings.Current?.EnableLaunchAll ?? true);
                if (includeOpenAll)
                {
                    col.AddObject(CreateTaskLink(exe, openAllArgs, "Open All", "Open all apps in this group"));
                }
                col.AddObject(CreateTaskLink(exe, editArgs, "Edit this Group", "Edit this group"));
                col.GetCount(out uint count);
                taskCount = (int)count;

                var cdl = (ICustomDestinationList)Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_DestinationList));
                if (!string.IsNullOrWhiteSpace(appId))
                {
                    cdl.SetAppID(appId);
                }
                Guid iidArr = IID_IObjectArray;
                int hr = cdl.BeginList(out _, ref iidArr, out IntPtr pRemoved);
                if (pRemoved != IntPtr.Zero) Marshal.Release(pRemoved);
                if (hr != 0) return hr;

                hr = cdl.AddUserTasks(col);
                if (hr != 0)
                {
                    cdl.AbortList();
                    return hr;
                }

                if (commit)
                {
                    return cdl.CommitList();
                }
                else
                {
                    cdl.AbortList();
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[JumpListHelper] TryAddUserTasks error: {ex.Message}");
                return -1;
            }
        }

        private static void SetPropString(IPropertyStore ps, Guid fmtid, uint pid, string value)
        {
            if (ps == null || string.IsNullOrEmpty(value)) return;
            var pkey = new PROPERTYKEY { fmtid = fmtid, pid = pid };
            var pv = new PROPVARIANT { vt = 31, p = Marshal.StringToCoTaskMemUni(value) };
            try
            {
                ps.SetValue(ref pkey, ref pv);
            }
            finally
            {
                if (pv.p != IntPtr.Zero) Marshal.FreeCoTaskMem(pv.p);
            }
        }

        public static bool SetWindowAppId(IntPtr hwnd, string appId = "XAppGroup.Manager")
        {
            if (hwnd == IntPtr.Zero || string.IsNullOrWhiteSpace(appId)) return false;
            try
            {
                Guid iid = IID_IPropertyStore;
                int hr = SHGetPropertyStoreForWindow(hwnd, ref iid, out IPropertyStore ps);
                if (hr != 0 || ps == null) return false;

                string exe = Process.GetCurrentProcess().MainModule?.FileName
                    ?? Path.Combine(AppContext.BaseDirectory, "XAppGroup.exe");
                string ico = Path.Combine(AppContext.BaseDirectory, "XAppGroup.ico");
                string iconRes = File.Exists(ico) ? (ico + ",0") : (exe + ",0");

                // PKEY_AppUserModel_RelaunchCommand (pid 2), RelaunchIconResource (pid 3),
                // RelaunchDisplayNameResource (pid 4), ID (pid 5)
                SetPropString(ps, FMTID_AppUserModel, 2, "\"" + exe + "\"");
                SetPropString(ps, FMTID_AppUserModel, 3, iconRes);
                SetPropString(ps, FMTID_AppUserModel, 4, "XAppGroup");
                SetPropString(ps, FMTID_AppUserModel, 5, appId);
                ps.Commit();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string GetWindowAppId(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return null;
            try
            {
                Guid iid = IID_IPropertyStore;
                int hr = SHGetPropertyStoreForWindow(hwnd, ref iid, out IPropertyStore ps);
                if (hr != 0 || ps == null) return null;
                var pkey = new PROPERTYKEY { fmtid = FMTID_AppUserModel, pid = 5 };
                if (ps.GetValue(ref pkey, out PROPVARIANT pv) == 0)
                {
                    try
                    {
                        if (pv.vt == 31 && pv.p != IntPtr.Zero)
                            return Marshal.PtrToStringUni(pv.p);
                    }
                    finally
                    {
                        PropVariantClear(ref pv);
                    }
                }
            }
            catch { }
            return null;
        }

        public static void ClearAppJumpList()
        {
            try
            {
                var cdl = (ICustomDestinationList)Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_DestinationList));
                try { cdl.DeleteList("XAppGroup.Manager"); } catch { }
                try { cdl.DeleteList(null); } catch { }
                Marshal.ReleaseComObject(cdl);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[JumpListHelper] ClearAppJumpList error: {ex.Message}");
            }
        }

        public static void DeleteGroupJumpList(string groupId)
        {
            var appId = GetGroupAppId(groupId);
            if (string.IsNullOrWhiteSpace(appId)) return;
            try
            {
                var cdl = (ICustomDestinationList)Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_DestinationList));
                try { cdl.DeleteList(appId); } catch { }
                Marshal.ReleaseComObject(cdl);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[JumpListHelper] DeleteGroupJumpList error: {ex.Message}");
            }
        }

        public static void Update(GroupData g = null)
        {
            // Keep the main XAppGroup app icon Jump List clean (app name only)
            ClearAppJumpList();

            // Commit "Open All" + "Edit this Group" exclusively to this group's own AppUserModelID
            if (g != null && !string.IsNullOrWhiteSpace(g.Id))
            {
                SaveLastGroupId(g.Id);
                TryAddUserTasks(g, commit: true, out _, appId: GetGroupAppId(g.Id));
            }
        }
    }
}
