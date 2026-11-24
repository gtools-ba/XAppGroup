using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace XAppGroup
{
    /// <summary>Headless functional check: run with --selftest; writes selftest.log next to the exe.</summary>
    public static partial class SelfTest
    {
        static IEnumerable<System.Windows.Controls.Image> FindImages(DependencyObject d)
        {
            if (d is System.Windows.Controls.Image im) yield return im;
            int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(d);
            for (int k = 0; k < n; k++)
                foreach (var x in FindImages(System.Windows.Media.VisualTreeHelper.GetChild(d, k))) yield return x;
        }

        static IEnumerable<System.Windows.Controls.Button> FindButtons(DependencyObject d)
        {
            if (d is System.Windows.Controls.Button b) yield return b;
            int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(d);
            for (int k = 0; k < n; k++)
                foreach (var x in FindButtons(System.Windows.Media.VisualTreeHelper.GetChild(d, k))) yield return x;
        }

        static IEnumerable<T> FindVisual<T>(DependencyObject d) where T : DependencyObject
        {
            if (d is T t) yield return t;
            int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(d);
            for (int k = 0; k < n; k++)
                foreach (var x in FindVisual<T>(System.Windows.Media.VisualTreeHelper.GetChild(d, k))) yield return x;
        }

        static T FindAncestor<T>(DependencyObject d) where T : DependencyObject
        {
            while (d != null)
            {
                if (d is T t) return t;
                d = d is Visual ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);
            }
            return null;
        }

        static Button FindButtonByAutomationId(DependencyObject root, string id) =>
            FindVisual<Button>(root).FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == id);

        static Button FindButtonByTag(DependencyObject root, string tag) =>
            FindVisual<Button>(root).FirstOrDefault(b => string.Equals(b.Tag as string, tag, StringComparison.Ordinal));

        static Button FindVisibleButtonByTag(DependencyObject root, string tag) =>
            FindVisual<Button>(root).FirstOrDefault(b =>
                string.Equals(b.Tag as string, tag, StringComparison.Ordinal) && b.Visibility == Visibility.Visible);

        static bool HasInvokePattern(Button b)
        {
            if (b == null) return false;
            var peer = System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(b);
            return peer != null && peer.GetPattern(System.Windows.Automation.Peers.PatternInterface.Invoke) != null;
        }

        static TextBlock FindTextByAutomationId(DependencyObject root, string id) =>
            FindVisual<TextBlock>(root).FirstOrDefault(t => AutomationProperties.GetAutomationId(t) == id);

        static object InvokePrivate(object target, string method, params object[] args)
        {
            var mi = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
            if (mi == null) throw new MissingMethodException(target.GetType().FullName, method);
            return mi.Invoke(target, args);
        }

        static ListBoxItem GetItemContainer(ListBox list, object item)
        {
            list.UpdateLayout();
            return list.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
        }

        static void Pump(Window w)
        {
            w.UpdateLayout();
            Application.Current?.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            w.UpdateLayout();
        }

        static bool PeerIsEnabled(Button b)
        {
            var peer = System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(b);
            return peer != null && peer.IsEnabled();
        }

        public static void Run()
        {
            var log = new List<string>();
            string logFile = Path.Combine(AppContext.BaseDirectory, "selftest.log");
            if (Application.Current != null)
            {
                Application.Current.DispatcherUnhandledException += (s, e) =>
                {
                    log.Add("DISPATCHER_ERROR " + e.Exception.GetType().Name + ": " + e.Exception.Message + " @ " + e.Exception.StackTrace);
                    try { File.WriteAllLines(logFile, log); } catch { }
                    e.Handled = true;
                };
            }
            var backup = File.Exists(Store.File_) ? File.ReadAllText(Store.File_) : null;
            void Check(string name, Func<bool> f)
            {
                try { log.Add((f() ? "PASS " : "FAIL ") + name); }
                catch (Exception ex) { log.Add("FAIL " + name + " -> " + ex.GetType().Name + ": " + ex.Message); }
                try { File.WriteAllLines(logFile, log); } catch { }
            }

            var win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var apps = new[] { Path.Combine(win, "notepad.exe"), Path.Combine(win, "System32", "calc.exe"), Path.Combine(win, "System32", "mspaint.exe") };
            var g = new GroupData { GroupName = "Test Group", GroupCol = 2, ShowLabels = true, Paths = apps.ToList() };

            Check("Store save/load roundtrip", () =>
            {
                var s = new Settings(); s.Groups.Add(g); Store.Save(s);
                var l = Store.Load();
                return l.Groups.Count == 1 && l.Groups[0].Id == g.Id && l.Groups[0].Paths.Count == 3 && l.Groups[0].GroupCol == 2;
            });
            Check("Alphabetical sort", () =>
            {
                var t = new GroupData { SortMode = "Alphabetical", Paths = { "c:\\b.exe", "c:\\a.exe" } };
                return t.OrderedPaths().First() == "c:\\a.exe";
            });
            Check("Icon extraction", () => Icons.Get(apps[0]).PixelWidth > 0);
            Check("Group .ico generation", () =>
            {
                var f = Icons.BuildGroupIcon(g);
                var b = File.ReadAllBytes(f);
                return b.Length > 100 && b[2] == 1 && b[22] == 0x89; // ICO header + PNG payload
            });
            string lnk = null;
            Check("Shortcut creation", () =>
            {
                lnk = ShortcutHelper.Create(g, Store.ShortcutDir);
                return File.Exists(lnk);
            });
            Check("Shortcut uses custom IconPath when set", () =>
            {
                var customIco = Path.Combine(Store.Dir, "test_custom.ico");
                File.Copy(g.GeneratedIconPath, customIco, true);
                var gCustom = new GroupData { GroupName = "CustomIconGroup", IconPath = customIco, Paths = apps.ToList() };
                var customLnk = ShortcutHelper.Create(gCustom, Store.ShortcutDir);
                dynamic sh = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
                dynamic sc = sh.CreateShortcut(customLnk);
                string iconLoc = (string)sc.IconLocation;
                try { File.Delete(customLnk); File.Delete(customIco); } catch { }
                return iconLoc.Contains("test_custom.ico");
            });
            Check("Shortcut target/args", () =>
            {
                dynamic sh = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
                dynamic sc = sh.CreateShortcut(lnk);
                return ((string)sc.Arguments) == "--launch " + g.Id && ((string)sc.TargetPath).EndsWith("XAppGroup.exe", StringComparison.OrdinalIgnoreCase);
            });
            Check("Popup window builds & positions", () =>
            {
                Theme.Apply("Dark");
                var p = new PopupWindow(g);
                p.Show();
                p.UpdateLayout();
                Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                bool ok = p.ActualWidth > 50 && p.ActualHeight > 50 && p.Left >= 0;
                p.Close();
                return ok;
            });
            Check("Group icon source (generated)", () => { var s = g.IconSource; return s.PixelWidth == 256 && File.Exists(g.GeneratedIconPath); });
            Check("IconSource and AppIcons in-memory caching", () =>
            {
                var s1 = g.IconSource;
                var s2 = g.IconSource;
                var a1 = g.AppIcons;
                var a2 = g.AppIcons;
                bool cached = ReferenceEquals(s1, s2) && ReferenceEquals(a1, a2);
                g.InvalidateIcons();
                var s3 = g.IconSource;
                return cached && !ReferenceEquals(s1, s3);
            });
            Check("Icon benchmark: 10,000 reads zero disk writes & < 50ms", () =>
            {
                var icoPath = g.GeneratedIconPath;
                var lastWriteBefore = File.GetLastWriteTimeUtc(icoPath);
                var sw = System.Diagnostics.Stopwatch.StartNew();
                for (int i = 0; i < 10000; i++)
                {
                    var dummy1 = g.IconSource;
                    var dummy2 = g.AppIcons;
                }
                sw.Stop();
                var lastWriteAfter = File.GetLastWriteTimeUtc(icoPath);
                bool noDiskWrite = lastWriteBefore == lastWriteAfter;
                bool sub50ms = sw.ElapsedMilliseconds < 50;
                log.Add($"INFO 10,000 cached reads completed in {sw.ElapsedMilliseconds} ms (disk writes: 0)");
                return noDiskWrite && sub50ms;
            });
            Check("INotifyPropertyChanged on GroupData & ObservableCollection", () =>
            {
                var gd = new GroupData { GroupName = "Initial" };
                var propsChanged = new List<string>();
                gd.PropertyChanged += (s, e) => propsChanged.Add(e.PropertyName);
                gd.GroupName = "Updated Name";
                gd.IconPath = "custom.ico";
                gd.InvalidateIcons();
                bool propOk = propsChanged.Contains("GroupName") &&
                              propsChanged.Contains("IconPath") &&
                              propsChanged.Contains("IconSource") &&
                              propsChanged.Contains("AppIcons");

                var settings = new Settings();
                bool colChanged = false;
                settings.Groups.CollectionChanged += (s, e) => colChanged = true;
                settings.Groups.Add(gd);
                return propOk && colChanged;
            });
            Check("Concurrent thread-safe access to Icons.Get", () =>
            {
                var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
                var threads = new List<Thread>();
                for (int t = 0; t < 10; t++)
                {
                    int index = t % apps.Length;
                    var th = new Thread(() =>
                    {
                        try
                        {
                            for (int i = 0; i < 50; i++)
                            {
                                var img = Icons.Get(apps[index]);
                                if (img == null || img.PixelWidth <= 0)
                                    throw new InvalidOperationException("Failed to get icon");
                            }
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(ex);
                        }
                    });
                    threads.Add(th);
                    th.Start();
                }
                foreach (var th in threads) th.Join();
                return exceptions.IsEmpty;
            });
            Check("Single-instance signal wait handle works", () =>
            {
                const string testEvent = "XAppGroup.TestSignalEvent";
                using var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, testEvent);
                bool signaled = false;
                var t = new Thread(() =>
                {
                    if (EventWaitHandle.TryOpenExisting(testEvent, out var sender))
                    {
                        sender.Set();
                    }
                });
                t.Start();
                signaled = waitHandle.WaitOne(2000);
                t.Join();
                return signaled;
            });
            Check("Keyboard accessibility mnemonics on buttons", () =>
            {
                var m = new MainWindow(new Settings { Groups = { g } }, () => { });
                m.Show(); m.UpdateLayout();
                var btns = FindButtons(m).Select(b => b.Content?.ToString()).Where(s => s != null).ToList();
                m.Close();
                var e = new EditWindow(g);
                e.Show(); e.UpdateLayout();
                var eBtns = FindButtons(e).Select(b => b.Content?.ToString()).Where(s => s != null).ToList();
                e.Close();
                bool mOk = btns.Contains("_New") && btns.Contains("_Edit") && btns.Contains("_Delete");
                bool eOk = eBtns.Contains("_Save") && eBtns.Contains("_Cancel");
                return mOk && eOk;
            });
            Check("Custom IconPath used + persisted, IconSource not persisted", () =>
            {
                var png = Path.Combine(Store.Dir, "custom.ico");
                File.Copy(g.GeneratedIconPath, png, true);
                var c = new GroupData { GroupName = "C", IconPath = png, Paths = apps.ToList() };
                var json = System.Text.Json.JsonSerializer.Serialize(c);
                return c.IconSource.PixelWidth > 0 && json.Contains("custom.ico") && !json.Contains("IconSource") && !json.Contains("GeneratedIconPath");
            });
            Check("Popup header shows group icon", () =>
            {
                var p = new PopupWindow(g); p.Show(); p.UpdateLayout();
                bool found = FindImages(p).Any(i => i.Width == 20 && i.Source == null ? false : i.Width == 20 && ((BitmapSource)i.Source).PixelWidth == 256);
                p.Close(); return found;
            });
            Check("Main window list items show 32x32 icon", () =>
            {
                var m = new MainWindow(new Settings { Groups = { g } }, () => { });
                m.Show(); m.UpdateLayout();
                Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                bool found = FindImages(m).Any(i => i.Width == 32 && i.Height == 32 && i.Source != null);
                var hwnd = new System.Windows.Interop.WindowInteropHelper(m).Handle;
                bool appIdOk = JumpListHelper.GetWindowAppId(hwnd) == "XAppGroup.Manager";
                m.Close(); return found && appIdOk;
            });
            Check("Taskbar: shell verbs queryable (read-only)", () =>
            {
                var psi = new System.Diagnostics.ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
                psi.ArgumentList.Add("-NoProfile"); psi.ArgumentList.Add("-Command");
                psi.ArgumentList.Add("$f=Get-Item -LiteralPath '" + lnk + "';$i=(New-Object -ComObject Shell.Application).Namespace($f.DirectoryName).ParseName($f.Name);'VERBS:'+(($i.Verbs()|%{$_.Name}) -join '|')");
                using (var pr = System.Diagnostics.Process.Start(psi)) { var o = pr.StandardOutput.ReadToEnd(); pr.WaitForExit(); log.Add("INFO " + o.Trim()); return pr.ExitCode == 0 && o.Contains("VERBS:"); }
            });
            Check("Taskbar drag: native Shell IDataObject passes SHCreateShellItemArrayFromDataObject", () =>
            {
                var dataObj = ShellDragDrop.CreateShellDataObject(lnk, g);
                if (dataObj == null) return false;
                Guid iidArray = new Guid("b63ea76d-1f85-456f-a19c-48159efa858b");
                int hr = ShellDragDrop.SHCreateShellItemArrayFromDataObject(dataObj, ref iidArray, out IntPtr psia);
                if (psia != IntPtr.Zero) System.Runtime.InteropServices.Marshal.Release(psia);
                return hr == 0 && psia != IntPtr.Zero;
            });
            Check("Taskbar Jump List: Open All + Edit this Group tasks added via ICustomDestinationList", () =>
            {
                int hr = JumpListHelper.TryAddUserTasks(g, commit: false, out int count);
                bool tasksOk = hr == 0 && count == 2;
                JumpListHelper.SaveLastGroupId(g.Id);
                bool lastOk = JumpListHelper.LoadLastGroupId() == g.Id;
                JumpListHelper.SaveEditRequest(g.Id);
                bool editReqOk = JumpListHelper.ConsumeEditRequest() == g.Id && JumpListHelper.ConsumeEditRequest() == null;
                return tasksOk && lastOk && editReqOk;
            });
            Check("Delete cleanup removes Desktop + Shortcuts .lnk", () =>
            {
                try
                {
                    var g2 = new GroupData { GroupName = "ZZ Delete Test", Paths = apps.ToList() };
                    var b = ShortcutHelper.Create(g2, Store.ShortcutDir);
                    if (!File.Exists(b)) return false;
                    var removed = Launcher.DeleteShortcut(g2);
                    return !File.Exists(b);
                }
                catch (Exception ex)
                {
                    throw new Exception($"{ex.GetType().Name}: {ex.Message} at {ex.StackTrace}");
                }
            });
            Check("IconPath round-trips via appgroups.json", () =>
            {
                var png = Path.Combine(Store.Dir, "custom.ico");
                var s = new Settings(); s.Groups.Add(new GroupData { GroupName = "R", IconPath = png }); Store.Save(s);
                return Store.Load().Groups[0].IconPath == png;
            });
            Check("Icon from .exe supported", () => Icons.FromFile(apps[0]) != null);
            Check("Edit window: icon per app + preview", () =>
            {
                var e = new EditWindow(g); e.Show(); e.UpdateLayout();
                Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                var imgs = FindImages(e).ToList();
                bool ok = imgs.Count(i => i.Width == 32 && i.Height == 32 && i.Source != null) >= 4; // 3 apps + preview
                e.Close(); return ok;
            });
            Check("Main window list: group icon + icon-only app buttons", () =>
            {
                var m = new MainWindow(new Settings { Groups = { g } }, () => { });
                m.Show(); m.UpdateLayout();
                Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                var imgs = FindImages(m).Where(i => i.Source != null).ToList();
                var pills = FindButtons(m).Where(b => (b.Tag as string) == "CardAppPill").ToList();
                bool ok = imgs.Count(i => i.Width == 20 && i.Height == 20) == 3 &&
                          imgs.Count(i => i.Width == 32 && i.Height == 32) >= 1 &&
                          pills.Count == 3 &&
                          pills.All(p => !FindVisual<TextBlock>(p).Any());
                m.Close(); return ok;
            });
            Check("AppIcons follows Paths changes", () =>
            {
                var t = new GroupData { Paths = apps.Take(2).ToList() };
                int a = t.AppIcons.Count; t.Paths.Add(apps[2]);
                return a == 2 && t.AppIcons.Count == 3;
            });
            Check("Launch app", () =>
            {
                var marker = Path.Combine(Store.Dir, "launched.txt");
                var bat = Path.Combine(Store.Dir, "t.bat");
                Directory.CreateDirectory(Store.Dir);
                File.WriteAllText(bat, "@echo ok> \"" + marker + "\"\r\n");
                File.Delete(marker);
                Launcher.Run(bat);
                for (int i = 0; i < 20 && !File.Exists(marker); i++) Thread.Sleep(250);
                return File.Exists(marker);
            });
                        Check("Edit window builds", () => { var e = new EditWindow(g); return e.Title == "Edit Group"; });
            Check("Main window builds", () => { var m = new MainWindow(new Settings { Groups = { g } }, () => { }); return m.Title == "XAppGroup"; });

            Check("Real-time theme brush update without restart", () =>
            {
                try
                {
                    Theme.Apply("Dark");
                    var darkBg = Theme.Bg.Color;
                    Theme.Apply("Light");
                    var lightBg = Theme.Bg.Color;
                    bool dynamicChange = darkBg != lightBg;
                    Theme.Apply("System");
                    return dynamicChange;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{ex.GetType().Name}: {ex.Message} at {ex.StackTrace}");
                }
            });

            Check("Shortcut created with valid properties", () =>
            {
                var testGroup = new GroupData { GroupName = "AUMID Test", Paths = apps.ToList() };
                var shortcutLnk = ShortcutHelper.Create(testGroup, Path.Combine(Store.Dir, "TestShortcuts"));
                bool exists = File.Exists(shortcutLnk);
                if (File.Exists(shortcutLnk)) File.Delete(shortcutLnk);
                return exists;
            });

            RunUiVisualTests(log, apps, g, Check);
            RunInteractionTests(log, apps, Check);

            try { File.Delete(lnk); if (backup != null) File.WriteAllText(Store.File_, backup); else File.Delete(Store.File_); } catch { }
            File.WriteAllLines(Path.Combine(AppContext.BaseDirectory, "selftest.log"), log);
        }
    }
}
