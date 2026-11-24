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
    /// Native Windows Shell drag-and-drop for .lnk shortcuts using IShellItem(BHID_DataObject),
    /// IDragSourceHelper, and SHDoDragDrop so Windows 11 Taskbar accepts pinning via drag.
    /// </summary>
    public static class ShellDragDrop
    {
        static readonly Guid BHID_DataObject = new Guid("b8c0bd9f-ed24-455c-83e6-d5390c4fe8c4");
        static readonly Guid IID_IDataObject = new Guid("0000010e-0000-0000-C000-000000000046");
        static readonly Guid IID_IShellItem = new Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe");
        static readonly Guid CLSID_DragDropHelper = new Guid("4657278A-411B-11d2-839A-00C04FD918D0");

        public const int DROPEFFECT_COPY = 1;
        public const int DROPEFFECT_LINK = 4;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int x, y; }

        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE { public int cx, cy; }

        [StructLayout(LayoutKind.Sequential)]
        public struct SHDRAGIMAGE
        {
            public SIZE sizeDragImage;
            public POINT ptOffset;
            public IntPtr hbmpDragImage;
            public int crColorKey;
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
        interface IShellItem
        {
            [PreserveSig]
            int BindToHandler(IntPtr pbc, [In] ref Guid bhid, [In] ref Guid riid, out System.Runtime.InteropServices.ComTypes.IDataObject ppv);
        }

        [ComImport, Guid("DE5BF786-477A-11d2-839D-00C04FD918D0"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IDragSourceHelper
        {
            [PreserveSig]
            int InitializeFromBitmap(ref SHDRAGIMAGE pshdi, System.Runtime.InteropServices.ComTypes.IDataObject pDataObject);
            [PreserveSig]
            int InitializeFromWindow(IntPtr hwnd, ref POINT ppt, System.Runtime.InteropServices.ComTypes.IDataObject pDataObject);
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath, IntPtr pbc, [In] ref Guid riid, out IShellItem ppv);

        [DllImport("shell32.dll")]
        static extern int SHDoDragDrop(IntPtr hwnd, System.Runtime.InteropServices.ComTypes.IDataObject pdtobj, IntPtr pdsrc, int dwEffect, out int pdwEffect);

        [DllImport("shell32.dll")]
        public static extern int SHCreateShellItemArrayFromDataObject(System.Runtime.InteropServices.ComTypes.IDataObject pdo, [In] ref Guid riid, out IntPtr ppv);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern uint RegisterClipboardFormat(string lpszFormat);

        [DllImport("user32.dll")]
        static extern bool ReleaseCapture();

        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);

        public static System.Runtime.InteropServices.ComTypes.IDataObject CreateShellDataObject(string lnkPath, GroupData g)
        {
            string fullPath = Path.GetFullPath(lnkPath);
            Guid iidItem = IID_IShellItem;
            int hr = SHCreateItemFromParsingName(fullPath, IntPtr.Zero, ref iidItem, out IShellItem item);
            if (hr != 0 || item == null) return null;

            Guid bhid = BHID_DataObject;
            Guid iidData = IID_IDataObject;
            hr = item.BindToHandler(IntPtr.Zero, ref bhid, ref iidData, out var dataObj);
            if (hr != 0 || dataObj == null) return null;

            // Set Preferred DropEffect = DROPEFFECT_LINK (4) just like WinRT DataPackageOperation.Link
            try
            {
                uint cfPref = RegisterClipboardFormat("Preferred DropEffect");
                if (cfPref != 0)
                {
                    IntPtr hGlobal = Marshal.AllocHGlobal(4);
                    Marshal.WriteInt32(hGlobal, DROPEFFECT_LINK);
                    var fmt = new System.Runtime.InteropServices.ComTypes.FORMATETC
                    {
                        cfFormat = unchecked((short)cfPref),
                        dwAspect = System.Runtime.InteropServices.ComTypes.DVASPECT.DVASPECT_CONTENT,
                        lindex = -1,
                        ptd = IntPtr.Zero,
                        tymed = System.Runtime.InteropServices.ComTypes.TYMED.TYMED_HGLOBAL
                    };
                    var medium = new System.Runtime.InteropServices.ComTypes.STGMEDIUM
                    {
                        tymed = System.Runtime.InteropServices.ComTypes.TYMED.TYMED_HGLOBAL,
                        unionmember = hGlobal,
                        pUnkForRelease = null
                    };
                    dataObj.SetData(ref fmt, ref medium, true);
                }
            }
            catch { }

            // Attach the group icon bitmap as the drag visual so ONLY the group icon is dragged
            try
            {
                string icoPath = ShortcutHelper.GetValidShortcutIconLocation(g);
                if (!string.IsNullOrEmpty(icoPath) && File.Exists(icoPath))
                {
                    const int dragSize = 48;
                    using var bmp = new Bitmap(dragSize, dragSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using (var gr = Graphics.FromImage(bmp))
                    {
                        gr.SmoothingMode = SmoothingMode.AntiAlias;
                        gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        gr.Clear(Color.Transparent);
                        try
                        {
                            using var srcIco = new Icon(icoPath, dragSize, dragSize);
                            using var srcBmp = srcIco.ToBitmap();
                            gr.DrawImage(srcBmp, 0, 0, dragSize, dragSize);
                        }
                        catch
                        {
                            using var srcBmp = Icons.ExtractBitmap(icoPath);
                            gr.DrawImage(srcBmp, 0, 0, dragSize, dragSize);
                        }
                    }
                    IntPtr hbmp = bmp.GetHbitmap(Color.FromArgb(0, 0, 0, 0));
                    if (hbmp != IntPtr.Zero)
                    {
                        var helperType = Type.GetTypeFromCLSID(CLSID_DragDropHelper);
                        if (helperType != null)
                        {
                            var helper = (IDragSourceHelper)Activator.CreateInstance(helperType);
                            var shdi = new SHDRAGIMAGE
                            {
                                sizeDragImage = new SIZE { cx = dragSize, cy = dragSize },
                                ptOffset = new POINT { x = dragSize / 2, y = dragSize / 2 },
                                hbmpDragImage = hbmp,
                                crColorKey = -1
                            };
                            int hrImg = helper.InitializeFromBitmap(ref shdi, dataObj);
                            if (hrImg != 0) DeleteObject(hbmp);
                        }
                        else
                        {
                            DeleteObject(hbmp);
                        }
                    }
                }
            }
            catch { }

            return dataObj;
        }

        public static void DoDragLink(IntPtr hwnd, GroupData g)
        {
            if (g == null) return;
            try
            {
                JumpListHelper.Update(g);
                var srcLnk = ShortcutHelper.Create(g, Store.ShortcutDir);
                var tempDir = Path.Combine(Path.GetTempPath(), "XAppGroup", "DragTemp");
                Directory.CreateDirectory(tempDir);
                var tempLnk = Path.Combine(tempDir, ShortcutHelper.Sanitize(g.GroupName) + ".lnk");
                File.Copy(srcLnk, tempLnk, true);

                var dataObj = CreateShellDataObject(tempLnk, g);
                if (dataObj != null)
                {
                    ReleaseCapture();
                    SHDoDragDrop(IntPtr.Zero, dataObj, IntPtr.Zero, DROPEFFECT_COPY | DROPEFFECT_LINK, out _);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShellDragDrop] DoDragLink error: {ex.Message}");
            }
        }
    }
}
