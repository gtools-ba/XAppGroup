using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

namespace XAppGroup
{
    public class App : Application
    {
        private const string EventName = "XAppGroup.ShowEvent";
        private EventWaitHandle _showEvent;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_RESTORE = 9;

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr value);
        private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

        Settings _s;
        MainWindow _main;
        System.Windows.Forms.NotifyIcon _tray;

        static App()
        {
            try { SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2); } catch { }
        }

        [STAThread]
        public static void Main(string[] args)
        {
            try { SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2); } catch { }
            var app = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            app.Startup += (s, e) => app.Begin(args);
            app.Run();
        }

        // Stable per-data-directory key for the isolated single-instance mutex.
        // string.GetHashCode() is randomized per process, so it cannot be used here.
        static string StablePathKey(string path)
        {
            string normalized;
            try
            {
                normalized = System.IO.Path.GetFullPath(path.Trim())
                    .TrimEnd('\\', '/')
                    .ToLowerInvariant();
            }
            catch
            {
                normalized = path.Trim().ToLowerInvariant();
            }
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(normalized));
                return BitConverter.ToString(hash, 0, 8).Replace("-", "");
            }
        }

        GroupData ResolveGroup(string arg)
        {
            if (_s?.Groups == null || _s.Groups.Count == 0) return null;
            if (!string.IsNullOrWhiteSpace(arg))
            {
                var byId = _s.Groups.FirstOrDefault(x => string.Equals(x.Id, arg, StringComparison.OrdinalIgnoreCase));
                if (byId != null) return byId;
                var byName = _s.Groups.FirstOrDefault(x => string.Equals(x.GroupName, arg, StringComparison.OrdinalIgnoreCase));
                if (byName != null) return byName;
            }
            var lastId = JumpListHelper.LoadLastGroupId();
            if (!string.IsNullOrWhiteSpace(lastId))
            {
                var byLast = _s.Groups.FirstOrDefault(x => string.Equals(x.Id, lastId, StringComparison.OrdinalIgnoreCase));
                if (byLast != null) return byLast;
            }
            return _s.Groups[0];
        }

        void Begin(string[] args)
        {
            if (args.Contains("--selftest")) { SelfTest.Run(); Shutdown(); return; }
            if (args.Contains("--capture-docs")) { DocScreenshots.Capture(); Shutdown(); return; }
            _s = Store.Load();
            Settings.Current = _s;
            AppConfig.Load();
            Theme.Apply(_s.Theme);

            // Shortcut mode: XAppGroup.exe --launch <id>  → show the popup and exit when it closes.
            int iLaunch = Array.IndexOf(args, "--launch");
            if (iLaunch >= 0)
            {
                var g = ResolveGroup(iLaunch + 1 < args.Length ? args[iLaunch + 1] : null);
                if (g == null) { Shutdown(); return; }
                JumpListHelper.Update(g);
                var pop = new PopupWindow(g);
                pop.Closed += (s, e) => Shutdown();
                pop.Show();
                return;
            }

            // Taskbar Jump List: XAppGroup.exe --open-all [<id>]  → launch all apps in the group and exit.
            int iOpenAll = Array.IndexOf(args, "--open-all");
            if (iOpenAll >= 0)
            {
                var g = ResolveGroup(iOpenAll + 1 < args.Length ? args[iOpenAll + 1] : null);
                if (g != null)
                {
                    JumpListHelper.Update(g);
                    Launcher.RunAll(g);
                }
                Shutdown();
                return;
            }

            // Taskbar Jump List: XAppGroup.exe --edit [<id>]  → open Edit Group window in manager.
            int iEdit = Array.IndexOf(args, "--edit");
            if (iEdit >= 0)
            {
                var g = ResolveGroup(iEdit + 1 < args.Length ? args[iEdit + 1] : null);
                if (g != null)
                {
                    JumpListHelper.SaveEditRequest(g.Id);
                    JumpListHelper.Update(g);
                }
            }

            // Prevent a second manager instance for the same data directory; signal existing instance to activate.
            string customData = Environment.GetEnvironmentVariable("XAPPGROUP_DATA");
            string instanceKey = string.IsNullOrWhiteSpace(customData)
                ? ""
                : "." + StablePathKey(customData);
            string mutexName = "XAppGroup.Manager" + instanceKey;
            string eventName = EventName + instanceKey;

            var mutex = new Mutex(true, mutexName, out bool first);
            if (!first)
            {
                try
                {
                    if (EventWaitHandle.TryOpenExisting(eventName, out var signal))
                    {
                        signal.Set();
                    }
                }
                catch { }
                Shutdown();
                return;
            }
            GC.KeepAlive(mutex);

            try
            {
                _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);
                var listener = new Thread(() =>
                {
                    while (true)
                    {
                        if (!_showEvent.WaitOne()) break;
                        Dispatcher.InvokeAsync(ActivateMainWindow);
                    }
                }) { IsBackground = true };
                listener.Start();
            }
            catch { }

            bool firstHideNotice = true;
            if (_s?.Groups != null)
            {
                foreach (var g in _s.Groups)
                {
                    try { ShortcutHelper.GetValidShortcutIconLocation(g); } catch { }
                }
            }
            _main = new MainWindow(_s, () => { Store.Save(_s); RebuildTray(); });
            _main.Closing += (s, e) =>
            {
                e.Cancel = true;
                _main.Hide();
                if (firstHideNotice && _tray != null)
                {
                    firstHideNotice = false;
                    try
                    {
                        _tray.ShowBalloonTip(3000, "XAppGroup", "XAppGroup is running in the background. Click the tray icon to open.", System.Windows.Forms.ToolTipIcon.Info);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Tray] Balloon tip failed: {ex.Message}");
                    }
                }
            };
            _main.Show();
            BuildTray();
            JumpListHelper.ClearAppJumpList();
            foreach (var grp in _s.Groups)
            {
                TaskbarHelper.SyncPinnedShortcut(grp);
                JumpListHelper.Update(grp);
            }

            var initialEditId = JumpListHelper.ConsumeEditRequest();
            if (!string.IsNullOrEmpty(initialEditId))
            {
                Dispatcher.InvokeAsync(() => _main.EditGroupById(initialEditId));
            }
            else
            {
                MaybePromptSupport();
            }
        }

        void MaybePromptSupport()
        {
            try
            {
                var cfg = AppConfig.Current;
                _s.LaunchCount++;
                bool shouldShow = cfg.SupportPromptEnabled
                    && cfg.SupportPromptEveryNLaunches > 0
                    && _s.LaunchCount >= cfg.SupportPromptEveryNLaunches
                    && (_s.LaunchCount % cfg.SupportPromptEveryNLaunches == 0)
                    && (_s.LastSupportPromptUtc == null || (DateTime.UtcNow - _s.LastSupportPromptUtc.Value).TotalDays >= Math.Max(0, cfg.SupportPromptCooldownDays));

                if (shouldShow)
                {
                    _s.LastSupportPromptUtc = DateTime.UtcNow;
                }
                Store.Save(_s);

                if (shouldShow)
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        if (_main != null && _main.IsVisible)
                            SupportDialog.ShowPrompt(_main);
                    }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                }
            }
            catch { }
        }

        public void ActivateMainWindow()
        {
            if (_main == null) return;
            var s = Store.Load();
            _s.Groups.Clear();
            foreach (var g in s.Groups) _s.Groups.Add(g);

            if (!_main.IsVisible) _main.Show();
            if (_main.WindowState == WindowState.Minimized) _main.WindowState = WindowState.Normal;
            _main.ClampToMonitorWorkArea(recenter: false);
            var handle = new WindowInteropHelper(_main).Handle;
            if (handle != IntPtr.Zero)
            {
                ShowWindow(handle, SW_RESTORE);
                SetForegroundWindow(handle);
            }
            _main.Activate();
            _main.Focus();

            var editId = JumpListHelper.ConsumeEditRequest();
            if (!string.IsNullOrEmpty(editId))
            {
                _main.EditGroupById(editId);
            }
        }

        void BuildTray()
        {
            System.Drawing.Icon trayIcon = null;
            try
            {
                var icoPath = System.IO.Path.Combine(AppContext.BaseDirectory, "XAppGroup.ico");
                if (System.IO.File.Exists(icoPath))
                {
                    trayIcon = new System.Drawing.Icon(icoPath);
                }
            }
            catch { }

            _tray = new System.Windows.Forms.NotifyIcon
            {
                Icon = trayIcon ?? System.Drawing.SystemIcons.Application,
                Text = "XAppGroup",
                Visible = _s.ShowTrayIcon
            };
            _tray.MouseClick += (s, e) =>
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Left) ActivateMainWindow();
            };
            RebuildTray();
        }

        void RebuildTray()
        {
            if (_tray == null) return;
            Settings.Current = _s;
            _tray.Visible = _s.ShowTrayIcon;
            HotkeyManager.Sync(_s.Groups, g => new PopupWindow(g).Show());

            bool dark = Theme.Dark;
            var renderer = new ThemedTrayMenuRenderer(dark);
            var font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Regular);

            var m = new System.Windows.Forms.ContextMenuStrip
            {
                Renderer = renderer,
                Font = font,
                ShowImageMargin = true,
                ShowCheckMargin = false,
                DropShadowEnabled = true,
                Padding = new System.Windows.Forms.Padding(2, 4, 2, 4)
            };

            if (_s.ShowGroupsInTray)
            {
                foreach (var g in _s.Groups)
                {
                    var grp = g;
                    var item = new System.Windows.Forms.ToolStripMenuItem(grp.GroupName)
                    {
                        Font = font
                    };
                    item.DropDown.Renderer = renderer;
                    item.DropDown.Font = font;
                    item.DropDown.DropShadowEnabled = true;
                    item.DropDown.Padding = new System.Windows.Forms.Padding(2, 4, 2, 4);
                    if (item.DropDown is System.Windows.Forms.ToolStripDropDownMenu menu)
                    {
                        menu.ShowImageMargin = true;
                        menu.ShowCheckMargin = false;
                    }

                    try
                    {
                        var iconLoc = ShortcutHelper.GetValidShortcutIconLocation(grp);
                        if (!string.IsNullOrEmpty(iconLoc) && System.IO.File.Exists(iconLoc))
                        {
                            using (var ico = new System.Drawing.Icon(iconLoc, 16, 16))
                                item.Image = new System.Drawing.Bitmap(ico.ToBitmap(), 16, 16);
                        }
                    }
                    catch { }

                    var paths = grp.OrderedPaths().ToList();
                    if (_s.EnableLaunchAll)
                    {
                        var launchAllItem = new System.Windows.Forms.ToolStripMenuItem("Launch All", CreatePlayIcon(dark), (s, e) => Launcher.RunAll(grp))
                        {
                            Font = font
                        };
                        item.DropDownItems.Add(launchAllItem);
                        if (paths.Count > 0)
                            item.DropDownItems.Add(new System.Windows.Forms.ToolStripSeparator());
                    }

                    foreach (var p in paths)
                    {
                        var path = p;
                        var appName = System.IO.Path.GetFileNameWithoutExtension(path);
                        var appItem = new System.Windows.Forms.ToolStripMenuItem(appName, null, (s, e) => Launcher.Run(path))
                        {
                            Font = font
                        };
                        try
                        {
                            if (System.IO.File.Exists(path))
                            {
                                using (var appIco = System.Drawing.Icon.ExtractAssociatedIcon(path))
                                    if (appIco != null) appItem.Image = new System.Drawing.Bitmap(appIco.ToBitmap(), 16, 16);
                            }
                        }
                        catch { }
                        item.DropDownItems.Add(appItem);
                    }
                    m.Items.Add(item);
                }
            }

            if (m.Items.Count > 0)
            {
                m.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            }

            System.Drawing.Bitmap openIcon = null;
            try
            {
                if (_tray.Icon != null)
                    openIcon = new System.Drawing.Bitmap(_tray.Icon.ToBitmap(), 16, 16);
            }
            catch { }

            var openItem = new System.Windows.Forms.ToolStripMenuItem("Open XAppGroup", openIcon, (s, e) => ActivateMainWindow())
            {
                Font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold)
            };
            m.Items.Add(openItem);

            var exitItem = new System.Windows.Forms.ToolStripMenuItem("Exit", CreateExitIcon(dark), (s, e) =>
            {
                HotkeyManager.Dispose();
                _tray.Visible = false;
                _tray.Dispose();
                _showEvent?.Dispose();
                Shutdown();
            })
            {
                Font = font
            };
            m.Items.Add(exitItem);

            _tray.ContextMenuStrip = m;
        }

        private static System.Drawing.Bitmap CreateExitIcon(bool dark)
        {
            var bmp = new System.Drawing.Bitmap(16, 16);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var pen = new System.Drawing.Pen(dark ? System.Drawing.Color.FromArgb(248, 113, 113) : System.Drawing.Color.FromArgb(220, 38, 38), 1.6f))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    g.DrawArc(pen, 2.5f, 3f, 10.5f, 10.5f, -60, 300);
                    g.DrawLine(pen, 7.75f, 1.5f, 7.75f, 7f);
                }
            }
            return bmp;
        }

        private static System.Drawing.Bitmap CreatePlayIcon(bool dark)
        {
            var bmp = new System.Drawing.Bitmap(16, 16);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var brush = new System.Drawing.SolidBrush(dark ? System.Drawing.Color.FromArgb(245, 158, 11) : System.Drawing.Color.FromArgb(217, 119, 6)))
                {
                    g.FillPolygon(brush, new[]
                    {
                        new System.Drawing.PointF(5f, 3.5f),
                        new System.Drawing.PointF(12.5f, 8.0f),
                        new System.Drawing.PointF(5f, 12.5f)
                    });
                }
            }
            return bmp;
        }
    }
}
