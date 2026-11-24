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
    public static partial class SelfTest
    {
        static void RunUiVisualTests(List<string> log, string[] apps, GroupData g, Action<string, Func<bool>> Check)
        {
            Check("Render UI visual preview PNG", () =>
            {
                Theme.Apply("System");
                var previewSettings = new Settings
                {
                    Theme = "System",
                    Groups = new System.Collections.ObjectModel.ObservableCollection<GroupData>
                    {
                        new GroupData
                        {
                            GroupName = "Group 1 — Productivity & Dev",
                            GroupCol = 1,
                            CustomSummary = "Primary workspace stack: code editor, terminal, messaging & AI assistants.",
                            Paths = new List<string> { apps[0], apps[1], apps[2] }
                        },
                        new GroupData
                        {
                            GroupName = "abdou — Development",
                            GroupCol = 2,
                            CustomSummary = "Containers, API debuggers, Git GUIs, DB viewers, and localhost proxies.",
                            Paths = new List<string> { apps[0], apps[1] }
                        },
                        new GroupData
                        {
                            GroupName = "Design & Creative Suite",
                            GroupCol = 3,
                            CustomSummary = "Vector editing, screen recording, Figma desktop, and assets catalog.",
                            Paths = new List<string> { apps[2], apps[0] }
                        }
                    }
                };
                var m = new MainWindow(previewSettings, () => { });
                m.Show();
                m.UpdateLayout();
                Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);

                var hwnd = new System.Windows.Interop.WindowInteropHelper(m).Handle;
                var ctx = MainWindow.GetWindowDpiAwarenessContext(hwnd);
                int awareness = MainWindow.GetAwarenessFromDpiAwarenessContext(ctx);
                bool isPmV2 = MainWindow.AreDpiAwarenessContextsEqual(ctx, new IntPtr(-4));
                uint sysDpi = MainWindow.GetDpiForSystem();
                uint winDpi = MainWindow.GetDpiForWindow(hwnd);
                var vDpi = System.Windows.Media.VisualTreeHelper.GetDpi(m);
                var waDip = m.GetMonitorWorkAreaDip();
                var rawMon = m.GetRawMonitorDebugInfo();
                log.Add($"INFO DPI_CHECK Awareness={awareness} IsPerMonitorV2={isPmV2} SysDpi={sysDpi} WinDpi={winDpi} VisualDpi={vDpi.PixelsPerInchX}x{vDpi.PixelsPerInchY} ({vDpi.DpiScaleX:F2}x{vDpi.DpiScaleY:F2}) RawMon={rawMon} WorkAreaDip=({waDip.Left:F1},{waDip.Top:F1},{waDip.Width:F1}x{waDip.Height:F1})");

                void DumpRects(string tag)
                {
                    m.UpdateLayout();
                    Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
                    var curDpi = System.Windows.Media.VisualTreeHelper.GetDpi(m);
                    var winPeer = System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(m);
                    var wr = winPeer.GetBoundingRectangle();
                    log.Add($"INFO [{tag}] VisualDpi={curDpi.PixelsPerInchX}({curDpi.DpiScaleX:F2}x) WIN_UIA Rect=({wr.Left:F1},{wr.Top:F1},{wr.Width:F1}x{wr.Height:F1}) Right={wr.Right:F1} Bottom={wr.Bottom:F1} DIP=({m.Left:F1},{m.Top:F1},{m.ActualWidth:F1}x{m.ActualHeight:F1}) TipVis={m.TipBanner?.Visibility}");
                    foreach (var btn in FindButtons(m))
                    {
                        var peer = System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(btn);
                        var r = peer.GetBoundingRectangle();
                        log.Add($"INFO [{tag}] BTN_UIA '{btn.Content}' Rect=({r.Left:F1},{r.Top:F1},{r.Width:F1}x{r.Height:F1}) Right={r.Right:F1} Bottom={r.Bottom:F1}");
                    }
                    if (m.ThemeSelector != null)
                    {
                        var cbPeer = System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(m.ThemeSelector);
                        var cbr = cbPeer.GetBoundingRectangle();
                        log.Add($"INFO [{tag}] CB_UIA 'ThemeSelector({m.ThemeSelector.SelectedItem})' Rect=({cbr.Left:F1},{cbr.Top:F1},{cbr.Width:F1}x{cbr.Height:F1}) Right={cbr.Right:F1} Bottom={cbr.Bottom:F1}");
                    }
                }

                // 1. Real monitor run at 125% (rcWork = 1920x1020 px = 1536x816 DIP -> 896x740 DIP = 1120x925 px fits!)
                Theme.Apply("Dark");
                if (m.ThemeSelector != null) m.ThemeSelector.SelectedItem = "Dark Acrylic";
                DumpRects("REAL_125%_1920x1020px_896x740DIP");

                // Save 896x740 dark-mode visual equivalence screenshot at native 120 DPI (1120x925 px) and 96 DPI (896x740 px)
                {
                    var rv = m.Content as FrameworkElement ?? m;
                    var rtb96 = new RenderTargetBitmap((int)m.ActualWidth, (int)m.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtb96.Render(rv);
                    var p896 = Path.Combine(AppContext.BaseDirectory, "ui_preview_896x740_dark.png");
                    var enc896 = new PngBitmapEncoder();
                    enc896.Frames.Add(BitmapFrame.Create(rtb96));
                    using (var fs = new FileStream(p896, FileMode.Create)) enc896.Save(fs);
                    log.Add($"INFO DARK_SCREENSHOT_896x740 Path={p896} PixelSize={rtb96.PixelWidth}x{rtb96.PixelHeight} DIP={m.ActualWidth}x{m.ActualHeight} Bytes={new FileInfo(p896).Length}");
                }

                // 2. Theme ComboBox opened and changed by mouse
                if (m.ThemeSelector != null)
                {
                    var cb = m.ThemeSelector;
                    cb.Focus();
                    var downArgs = new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice, Environment.TickCount, System.Windows.Input.MouseButton.Left)
                    {
                        RoutedEvent = UIElement.MouseLeftButtonDownEvent,
                        Source = cb
                    };
                    cb.RaiseEvent(downArgs);
                    cb.IsDropDownOpen = true;
                    m.UpdateLayout();
                    Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
                    var itemLight = cb.ItemContainerGenerator.ContainerFromItem("Light Glass") as System.Windows.Controls.ComboBoxItem;
                    Rect itemLightRect = Rect.Empty;
                    if (itemLight != null)
                    {
                        var p = System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(itemLight);
                        itemLightRect = p.GetBoundingRectangle();
                        itemLight.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice, Environment.TickCount, System.Windows.Input.MouseButton.Left) { RoutedEvent = UIElement.MouseLeftButtonDownEvent, Source = itemLight });
                        itemLight.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice, Environment.TickCount, System.Windows.Input.MouseButton.Left) { RoutedEvent = UIElement.MouseLeftButtonUpEvent, Source = itemLight });
                        itemLight.IsSelected = true;
                    }
                    m.UpdateLayout();
                    var lightBg = Theme.Bg.Color;
                    string afterLight = previewSettings.Theme;

                    cb.IsDropDownOpen = true;
                    m.UpdateLayout();
                    Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
                    var itemDark = cb.ItemContainerGenerator.ContainerFromItem("Dark Acrylic") as System.Windows.Controls.ComboBoxItem;
                    Rect itemDarkRect = Rect.Empty;
                    if (itemDark != null)
                    {
                        var p = System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(itemDark);
                        itemDarkRect = p.GetBoundingRectangle();
                        itemDark.IsSelected = true;
                    }
                    cb.IsDropDownOpen = false;
                    m.UpdateLayout();
                    var darkBg = Theme.Bg.Color;
                    string afterDark = previewSettings.Theme;
                    log.Add($"INFO COMBOBOX_MOUSE LightItemUIA=({itemLightRect.Left:F1},{itemLightRect.Top:F1},{itemLightRect.Width:F1}x{itemLightRect.Height:F1},Bottom={itemLightRect.Bottom:F1}) DarkItemUIA=({itemDarkRect.Left:F1},{itemDarkRect.Top:F1},{itemDarkRect.Width:F1}x{itemDarkRect.Height:F1},Bottom={itemDarkRect.Bottom:F1}) ThemeAfterLight={afterLight}(Bg={lightBg}) ThemeAfterDark={afterDark}(Bg={darkBg})");
                }

                // 3. 125% at 1536x816 px work area (ApplySimulatedDisplay: 1536x864 panel @ 125% -> 1120x795 px)
                m.ApplySimulatedDisplay(new Rect(0, 0, 1536, 816), 1.25);
                DumpRects("SIM_125%_1536x816px");

                // 4. Short height 600 DIP at 125%
                m.Height = 600;
                DumpRects("SIM_125%_SHORT_600DIP");

                // 5. Minimum size 720x520 DIP at 125% + ScrollViewer horizontal scrollbar check
                m.Width = m.MinWidth;
                m.Height = m.MinHeight;
                DumpRects("SIM_125%_MINSIZE_720x520DIP");
                {
                    System.Windows.Controls.ScrollViewer FindSv(DependencyObject d)
                    {
                        if (d is System.Windows.Controls.ScrollViewer s) return s;
                        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(d); i++)
                        {
                            var r = FindSv(System.Windows.Media.VisualTreeHelper.GetChild(d, i));
                            if (r != null) return r;
                        }
                        return null;
                    }
                    var sv = FindSv(m.GroupsList);
                    if (sv != null)
                    {
                        log.Add($"INFO SCROLL_720DIP HScrollVis={sv.HorizontalScrollBarVisibility} ComputedHScroll={sv.ComputedHorizontalScrollBarVisibility} ExtentW={sv.ExtentWidth:F1} ViewportW={sv.ViewportWidth:F1} ComputedVScroll={sv.ComputedVerticalScrollBarVisibility} ExtentH={sv.ExtentHeight:F1} ViewportH={sv.ViewportHeight:F1}");
                    }
                }

                // 6. Maximize & Restore on real monitor (1920x1020 px @ 125%) and 1536x816 px @ 125%
                m.ApplySimulatedDisplay(new Rect(0, 0, 1536, 816), 1.25);
                m.WindowState = WindowState.Maximized;
                DumpRects("SIM_125%_1536x816_MAXIMIZED");
                m.WindowState = WindowState.Normal;
                DumpRects("SIM_125%_1536x816_RESTORED");

                m.ApplySimulatedDisplay(null, null);
                m.WindowState = WindowState.Maximized;
                DumpRects("REAL_125%_1920x1020_MAXIMIZED");
                m.WindowState = WindowState.Normal;
                DumpRects("REAL_125%_1920x1020_RESTORED");

                // 7. 100% scaling (1366x768, work area 1366x720 px) via ApplySimulatedDisplay
                int wmBeforeMove = m.WmDpiChangedCount;
                int evBeforeMove = m.DpiChangedEventCount;
                m.ApplySimulatedDisplay(new Rect(0, 0, 1366, 720), 1.0);
                log.Add($"INFO MOVE_TO_100% WmDpiChangedCount={m.WmDpiChangedCount - wmBeforeMove} DpiChangedEventCount={m.DpiChangedEventCount - evBeforeMove} OldPpi={m.LastDpiChangedOldPpi} NewPpi={m.LastDpiChangedNewPpi}");
                DumpRects("SIM_100%_1366x720px");

                // 8. Move from 100% monitor to 150% monitor (1920x1080, work area 1920x1008 px)
                wmBeforeMove = m.WmDpiChangedCount;
                evBeforeMove = m.DpiChangedEventCount;
                m.ApplySimulatedDisplay(new Rect(0, 0, 1920, 1008), 1.5);
                log.Add($"INFO MOVE_100%_TO_150% WmDpiChangedCount={m.WmDpiChangedCount - wmBeforeMove} DpiChangedEventCount={m.DpiChangedEventCount - evBeforeMove} OldPpi={m.LastDpiChangedOldPpi} NewPpi={m.LastDpiChangedNewPpi}");
                DumpRects("SIM_150%_1920x1008px");

                // 9. 150% scaling on 1536x864 (work area 1536x816 px) via ApplySimulatedDisplay
                m.ApplySimulatedDisplay(new Rect(0, 0, 1536, 816), 1.5);
                DumpRects("SIM_150%_1536x816px");

                // 10. Move from 150% monitor back to 100% monitor (1366x720 px)
                wmBeforeMove = m.WmDpiChangedCount;
                evBeforeMove = m.DpiChangedEventCount;
                m.ApplySimulatedDisplay(new Rect(0, 0, 1366, 720), 1.0);
                log.Add($"INFO MOVE_150%_TO_100% WmDpiChangedCount={m.WmDpiChangedCount - wmBeforeMove} DpiChangedEventCount={m.DpiChangedEventCount - evBeforeMove} OldPpi={m.LastDpiChangedOldPpi} NewPpi={m.LastDpiChangedNewPpi}");

                // 11. 200% scaling (3840x2160, work area 3840x2064 px = 1920x1032 DIP) via ApplySimulatedDisplay
                m.ApplySimulatedDisplay(new Rect(0, 0, 3840, 2064), 2.0);
                DumpRects("SIM_200%_3840x2064px");

                // 12. 200% scaling on 1920x1080 (work area 1920x984 px = 960x492 DIP) via ApplySimulatedDisplay
                m.ApplySimulatedDisplay(new Rect(0, 0, 1920, 984), 2.0);
                DumpRects("SIM_200%_1920x984px");

                // Reset to real monitor
                m.ApplySimulatedDisplay(null, null);

                var rootVisual = m.Content as FrameworkElement ?? m;
                int w = Math.Max(1, (int)m.ActualWidth);
                int h = Math.Max(1, (int)m.ActualHeight);
                var rtb = new RenderTargetBitmap(w, h, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtb.Render(rootVisual);
                var pngPath = Path.Combine(AppContext.BaseDirectory, "ui_preview.png");
                var enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(rtb));
                using (var fs = new FileStream(pngPath, FileMode.Create))
                    enc.Save(fs);
                m.Close();
                return File.Exists(pngPath) && new FileInfo(pngPath).Length > 1000;
            });

            Check("Selection identity survives filter, clear, and collection reload", () =>
            {
                Theme.Apply("Dark");
                var paths = new List<string> { apps[0], apps[1] };
                var s = new Settings
                {
                    Groups =
                    {
                        new GroupData { GroupName = "Productivity", Paths = paths.ToList() },
                        new GroupData { GroupName = "Dev Tools", Paths = paths.ToList() },
                        new GroupData { GroupName = "Design", Paths = paths.ToList() }
                    }
                };
                var m = new MainWindow(s, () => { });
                m.Show();
                Pump(m);

                var prod = s.Groups[0];
                var dev = s.Groups[1];
                string prodId = prod.Id;
                string devId = dev.Id;
                m.GroupsList.SelectedItem = prod;
                Pump(m);
                bool startOk = (m.GroupsList.SelectedItem as GroupData)?.Id == prodId && m.RememberedSelectionId == prodId;

                m.SearchBox.Text = "dev";
                Pump(m);
                var editBtn = FindButtonByAutomationId(m, "FooterEditBtn");
                var newBtn = FindButtonByAutomationId(m, "FooterNewBtn");
                var hint = FindTextByAutomationId(m, "FooterSelectionHint");
                string hiddenSelected = (m.GroupsList.SelectedItem as GroupData)?.Id ?? "<none>";
                bool hiddenOk = m.GroupsList.SelectedItem == null && m.GroupsList.Items.Count == 1 && m.RememberedSelectionId == prodId;
                bool disabledOk = editBtn != null && !editBtn.IsEnabled && newBtn != null && newBtn.IsEnabled;
                bool hintOk = hint != null && hint.Visibility == Visibility.Visible && hint.Text == "Select a group to edit, duplicate, or launch.";

                m.SearchBox.Text = string.Empty;
                Pump(m);
                string restoredSelected = (m.GroupsList.SelectedItem as GroupData)?.Id ?? "<none>";
                bool restoredOk = restoredSelected == prodId;
                bool enabledOk = editBtn != null && editBtn.IsEnabled && hint != null && hint.Visibility == Visibility.Collapsed;

                m.GroupsList.SelectedItem = dev;
                Pump(m);
                m.SearchBox.Text = "dev";
                Pump(m);
                bool keptOk = (m.GroupsList.SelectedItem as GroupData)?.Id == devId;
                m.SearchBox.Text = string.Empty;
                Pump(m);

                // Simulate a reload from disk: new GroupData instances with the same Ids.
                s.Groups.Clear();
                foreach (var ng in new[]
                {
                    new GroupData { Id = prodId, GroupName = "Productivity", Paths = paths.ToList() },
                    new GroupData { Id = devId, GroupName = "Dev Tools", Paths = paths.ToList() },
                    new GroupData { GroupName = "Design", Paths = paths.ToList() }
                }) s.Groups.Add(ng);
                Pump(m);
                string reloadSelected = (m.GroupsList.SelectedItem as GroupData)?.Id ?? "<none>";
                bool reloadOk = reloadSelected == devId && m.RememberedSelectionId == devId;

                log.Add($"INFO SELECTION_FILTER StartId={prodId} HiddenSelected={hiddenSelected} Remembered={m.RememberedSelectionId} RestoredId={restoredSelected} KeptId={(keptOk ? devId : "<mismatch>")} ReloadId={reloadSelected} FilteredItems=1");
                m.Close();
                return startOk && hiddenOk && disabledOk && hintOk && restoredOk && enabledOk && keptOk && reloadOk;
            });

            Check("Delete selects the next group at the same index, else the new last", () =>
            {
                Theme.Apply("Dark");
                var paths = new List<string> { apps[0] };
                var s = new Settings
                {
                    Groups =
                    {
                        new GroupData { GroupName = "Alpha", Paths = paths.ToList() },
                        new GroupData { GroupName = "Beta", Paths = paths.ToList() },
                        new GroupData { GroupName = "Gamma", Paths = paths.ToList() }
                    }
                };
                var m = new MainWindow(s, () => { });
                m.Show();
                Pump(m);

                var a = s.Groups[0];
                var b = s.Groups[1];
                var c = s.Groups[2];
                m.GroupsList.SelectedItem = b;
                Pump(m);

                var next1 = (GroupData)InvokePrivate(m, "RemoveGroupSelectNearest", b);
                bool nextOk = ReferenceEquals(next1, c) && ReferenceEquals(m.GroupsList.SelectedItem, c) && s.Groups.Count == 2;

                var next2 = (GroupData)InvokePrivate(m, "RemoveGroupSelectNearest", c);
                bool lastOk = ReferenceEquals(next2, a) && ReferenceEquals(m.GroupsList.SelectedItem, a) && s.Groups.Count == 1;

                var next3 = (GroupData)InvokePrivate(m, "RemoveGroupSelectNearest", a);
                var editBtn = FindButtonByAutomationId(m, "FooterEditBtn");
                var newBtn = FindButtonByAutomationId(m, "FooterNewBtn");
                var hint = FindTextByAutomationId(m, "FooterSelectionHint");
                bool emptyOk = next3 == null && m.GroupsList.SelectedItem == null && s.Groups.Count == 0 &&
                               editBtn != null && !editBtn.IsEnabled && newBtn != null && newBtn.IsEnabled &&
                               hint != null && hint.Visibility == Visibility.Visible;

                log.Add($"INFO DELETE_SELECTION Removed={b.Id} Next={next1?.Id ?? "<none>"} ThenRemoved={c.Id} LastSelected={(next2?.Id ?? "<none>")} EmptySelected={(m.GroupsList.SelectedItem as GroupData)?.Id ?? "<none>"} RemainingGroups={s.Groups.Count}");
                m.Close();
                return nextOk && lastOk && emptyOk;
            });

            Check("New and Duplicate select and scroll the new card into view", () =>
            {
                Theme.Apply("Dark");
                var s = new Settings();
                for (int i = 0; i < 18; i++) s.Groups.Add(new GroupData { GroupName = "Group " + (i + 1), Paths = new List<string>() });
                var m = new MainWindow(s, () => { });
                m.Show();
                Pump(m);
                m.GroupsList.SelectedItem = s.Groups[0];
                Pump(m);

                var sv = FindVisual<ScrollViewer>(m.GroupsList).FirstOrDefault();
                InvokePrivate(m, "Duplicate");
                Pump(m);
                var copy = s.Groups[s.Groups.Count - 1];
                bool dupOk = ReferenceEquals(m.GroupsList.SelectedItem, copy) && m.RememberedSelectionId == copy.Id;
                // Capture the duplicate's post-action evidence before New() changes the selection.
                string dupSelectedId = (m.GroupsList.SelectedItem as GroupData)?.Id ?? "<none>";
                double dupOffset = sv?.VerticalOffset ?? -1;
                bool dupScroll = dupOffset > 0;

                using (var timer = new System.Threading.Timer(_ =>
                {
                    m.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var ew = Application.Current.Windows.OfType<EditWindow>().LastOrDefault();
                        if (ew != null) ew.DialogResult = true;
                    }));
                }, null, 200, 200))
                {
                    InvokePrivate(m, "New");
                }
                Pump(m);
                var added = s.Groups[s.Groups.Count - 1];
                bool newOk = ReferenceEquals(m.GroupsList.SelectedItem, added) && m.RememberedSelectionId == added.Id;
                double newOffset = sv?.VerticalOffset ?? -1;
                bool newScroll = newOffset >= dupOffset;

                log.Add($"INFO NEW_DUPLICATE DupId={copy.Id} SelectedAfterDup={dupSelectedId} OffsetAfterDup={dupOffset:F1} NewId={added.Id} SelectedAfterNew={(m.GroupsList.SelectedItem as GroupData)?.Id ?? "<none>"} OffsetAfterNew={newOffset:F1} Count={s.Groups.Count}");
                m.Close();
                return dupOk && dupScroll && newOk && newScroll;
            });

            Check("Footer commands disable with no selection and expose UIA state", () =>
            {
                Theme.Apply("Dark");
                var s = new Settings { Groups = { new GroupData { GroupName = "Only", Paths = new List<string> { apps[0] } } } };
                var m = new MainWindow(s, () => { });
                m.Show();
                Pump(m);

                var g = s.Groups[0];
                var newBtn = FindButtonByAutomationId(m, "FooterNewBtn");
                var editBtn = FindButtonByAutomationId(m, "FooterEditBtn");
                var dupBtn = FindButtonByAutomationId(m, "FooterDuplicateBtn");
                var shortBtn = FindButtonByAutomationId(m, "FooterShortcutBtn");
                var delBtn = FindButtonByAutomationId(m, "FooterDeleteBtn");
                var testBtn = FindButtonByAutomationId(m, "FooterTestBtn");
                var hint = FindTextByAutomationId(m, "FooterSelectionHint");
                var groupButtons = new[] { editBtn, dupBtn, shortBtn, delBtn, testBtn };

                m.GroupsList.SelectedItem = g;
                Pump(m);
                bool selectedOk = groupButtons.All(b => b != null && b.IsEnabled && Math.Abs(b.Opacity - 1.0) < 0.001) &&
                                  newBtn != null && newBtn.IsEnabled && hint != null && hint.Visibility == Visibility.Collapsed;

                m.GroupsList.SelectedItem = null;
                Pump(m);
                bool noneOk = groupButtons.All(b => b != null && !b.IsEnabled && Math.Abs(b.Opacity - 0.45) < 0.001 &&
                                                      b.Cursor == System.Windows.Input.Cursors.Arrow && !PeerIsEnabled(b)) &&
                              newBtn != null && newBtn.IsEnabled && PeerIsEnabled(newBtn) &&
                              hint != null && hint.Visibility == Visibility.Visible &&
                              hint.Text == "Select a group to edit, duplicate, or launch.";

                log.Add($"INFO COMMAND_STATE OneSelected={selectedOk} NoneSelected={noneOk} EditEnabled={editBtn?.IsEnabled} EditOpacity={editBtn?.Opacity:F2} EditUiaEnabled={(editBtn != null && PeerIsEnabled(editBtn))} NewEnabled={newBtn?.IsEnabled} HintVisible={hint?.Visibility} Hint='{hint?.Text}'");
                m.Close();
                return selectedOk && noneOk;
            });

            Check("Selected and unselected ListBoxItem heights are identical", () =>
            {
                Theme.Apply("Dark");
                var paths = new List<string> { apps[0], apps[1], apps[2] };
                var s = new Settings
                {
                    Groups =
                    {
                        new GroupData { GroupName = "Same A", Paths = paths.ToList() },
                        new GroupData { GroupName = "Same B", Paths = paths.ToList() },
                        new GroupData { GroupName = "Same Empty", Paths = new List<string>() }
                    }
                };
                var m = new MainWindow(s, () => { });
                m.Show();
                Pump(m);

                var item0 = GetItemContainer(m.GroupsList, s.Groups[0]);
                var item1 = GetItemContainer(m.GroupsList, s.Groups[1]);
                var item2 = GetItemContainer(m.GroupsList, s.Groups[2]);

                m.GroupsList.SelectedItem = s.Groups[0];
                Pump(m);
                double item0Selected = item0?.ActualHeight ?? -1;
                double item1Unselected = item1?.ActualHeight ?? -1;

                m.GroupsList.SelectedItem = s.Groups[1];
                Pump(m);
                double item0Unselected = item0?.ActualHeight ?? -1;
                double item1Selected = item1?.ActualHeight ?? -1;

                m.GroupsList.SelectedItem = s.Groups[2];
                Pump(m);
                double item1AfterEmpty = item1?.ActualHeight ?? -1;
                double item2Selected = item2?.ActualHeight ?? -1;

                m.GroupsList.SelectedItem = null;
                Pump(m);
                double item2Unselected = item2?.ActualHeight ?? -1;

                double diffSelectedUnselected = Math.Abs(item0Selected - item1Unselected);
                double diffSameItem0 = Math.Abs(item0Selected - item0Unselected);
                double diffSameItem1 = Math.Abs(item1Selected - item1AfterEmpty);
                double diffEmpty = Math.Abs(item2Selected - item2Unselected);
                bool ok = item0 != null && item1 != null && item2 != null &&
                          diffSelectedUnselected < 0.01 && diffSameItem0 < 0.01 && diffSameItem1 < 0.01 &&
                          diffEmpty < 0.01;

                log.Add($"INFO CARD_HEIGHT Item0_Selected={item0Selected:F2} Item1_Unselected={item1Unselected:F2} Item0_Unselected={item0Unselected:F2} Item1_Selected={item1Selected:F2} Empty_Selected={item2Selected:F2} Empty_Unselected={item2Unselected:F2} Diff_SelectedVsUnselected={diffSelectedUnselected:F2} Diff_SameItem0={diffSameItem0:F2} Diff_SameItem1={diffSameItem1:F2} Diff_Empty={diffEmpty:F2}");
                m.Close();
                return ok;
            });

        }
    }
}
