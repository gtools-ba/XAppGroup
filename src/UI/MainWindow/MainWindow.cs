using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using IOPath = System.IO.Path;
using WpfPath = System.Windows.Shapes.Path;

namespace XAppGroup
{
    public partial class MainWindow : Window
    {
        public const double DesignWidthDip = 896.0;
        public const double DesignHeightDip = 740.0;
        public const double DesignMinWidthDip = 720.0;
        public const double DesignMinHeightDip = 520.0;
        public const double WorkAreaMarginDip = 16.0;
        public const double ShortHeightBreakpointDip = 620.0;
        public const double MinListHeightDip = 180.0;

        const uint MONITOR_DEFAULTTONEAREST = 0x00000002;
        const int WM_GETMINMAXINFO = 0x0024;
        const int WM_DISPLAYCHANGE = 0x007E;
        const int WM_EXITSIZEMOVE = 0x0232;
        const int WM_DPICHANGED = 0x02E0;

        [StructLayout(LayoutKind.Sequential)]
        struct POINT { public int x, y; }

        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetProcessDpiAwarenessContext(IntPtr value);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDpiAwarenessContext(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern int GetAwarenessFromDpiAwarenessContext(IntPtr value);

        [DllImport("user32.dll")]
        public static extern bool AreDpiAwarenessContextsEqual(IntPtr dpiContextA, IntPtr dpiContextB);

        [DllImport("user32.dll")]
        public static extern uint GetDpiForSystem();

        [DllImport("user32.dll")]
        public static extern uint GetDpiForWindow(IntPtr hwnd);

        [DllImport("shcore.dll")]
        public static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, ref RECT lParam);

        public int WmDpiChangedCount { get; private set; }
        public int DpiChangedEventCount { get; private set; }
        public double LastDpiChangedOldPpi { get; private set; }
        public double LastDpiChangedNewPpi { get; private set; }

        static MainWindow()
        {
            try { SetProcessDpiAwarenessContext(new IntPtr(-4)); } catch { }
        }

        readonly Settings _s;
        readonly ListBox _list = new ListBox
        {
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(24, 18, 24, 12)
        };
        readonly Action _changed;
        readonly TextBlock _groupCountText = new TextBlock { FontWeight = FontWeights.Bold, Foreground = Theme.Fg, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
        readonly TextBlock _launcherCountText = new TextBlock { FontWeight = FontWeights.Bold, Foreground = Theme.Fg, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
        readonly TextBox _searchBox = new TextBox
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Foreground = Theme.Fg,
            CaretBrush = Theme.Cyan,
            FontSize = 12,
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(30, 0, 10, 0)
        };

        Border _outerBorder;
        Border _statsRibbon;
        DockPanel _statsInnerDock;
        Border _tipBanner;
        Border _footerBorder;
        DockPanel _footerDock;
        WrapPanel _footerLeftBar;
        StackPanel _footerRightBar;
        ComboBox _themeSelector;
        Button _footerNewBtn;
        Button _footerEditBtn;
        Button _footerDuplicateBtn;
        Button _footerShortcutBtn;
        Button _footerDeleteBtn;
        Button _footerTestBtn;
        TextBlock _footerSelectionHint;

        sealed class FooterIconMode
        {
            public ControlTemplate LabeledTemplate;
            public Thickness LabeledPadding;
            public ControlTemplate IconTemplate;
        }
        readonly Dictionary<Button, FooterIconMode> _footerIconModes = new Dictionary<Button, FooterIconMode>();
        bool _footerIconsOnly;

        readonly CollectionViewSource _groupsViewSource;
        readonly ICollectionView _groupsView;
        string _rememberedGroupId;
        bool _suppressSelectionMemory;
        bool _suppressIconClick;
        bool _tipDismissedByUser;
        Rect _restoreBoundsDip;
        double? _simulatedDpiScale;
        Rect? _simulatedWorkAreaPx;

        public Border TipBanner => _tipBanner;
        public Border StatsRibbon => _statsRibbon;
        public Border FooterBar => _footerBorder;
        public ComboBox ThemeSelector => _themeSelector;
        public ListBox GroupsList => _list;
        public TextBox SearchBox => _searchBox;
        public ICollectionView GroupsView => _groupsView;
        /// <summary>Id of the group that should be selected whenever it is visible in the current filter.</summary>
        public string RememberedSelectionId => _rememberedGroupId;

        public MainWindow(Settings s, Action changed)
        {
            _s = s; _changed = changed;
            Title = "XAppGroup";
            Width = DesignWidthDip; Height = DesignHeightDip;
            MinWidth = DesignMinWidthDip; MinHeight = DesignMinHeightDip;
            Background = Theme.Bg; Foreground = Theme.Fg;
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI");
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Ideal);
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
            WindowStartupLocation = WindowStartupLocation.Manual;
            WindowStyle = WindowStyle.None;

            System.Windows.Shell.WindowChrome.SetWindowChrome(this, new System.Windows.Shell.WindowChrome
            {
                CaptionHeight = 0,
                CornerRadius = new CornerRadius(16),
                GlassFrameThickness = new Thickness(0),
                ResizeBorderThickness = new Thickness(6)
            });

            try
            {
                var icoPath = IOPath.Combine(AppContext.BaseDirectory, "XAppGroup.ico");
                if (File.Exists(icoPath))
                {
                    Icon = new BitmapImage(new Uri(icoPath, UriKind.Absolute));
                }
            }
            catch { }

            SourceInitialized += (s2, e) =>
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                JumpListHelper.SetWindowAppId(hwnd, "XAppGroup.Manager");
                var src = HwndSource.FromHwnd(hwnd);
                src?.AddHook(WndProc);
                ClampToMonitorWorkArea(recenter: true);
            };

            DpiChanged += (s2, e) =>
            {
                DpiChangedEventCount++;
                LastDpiChangedOldPpi = e.OldDpi.PixelsPerInchX;
                LastDpiChangedNewPpi = e.NewDpi.PixelsPerInchX;
                ClampToMonitorWorkArea(recenter: false);
            };

            SizeChanged += (s2, e) =>
            {
                UpdateResponsiveLayout(e.NewSize.Width, e.NewSize.Height);
                if (WindowState == WindowState.Normal && IsLoaded)
                {
                    _restoreBoundsDip = new Rect(Left, Top, Width, Height);
                }
            };

            StateChanged += (s2, e) =>
            {
                if (_outerBorder != null)
                {
                    _outerBorder.CornerRadius = WindowState == WindowState.Maximized ? new CornerRadius(0) : new CornerRadius(16);
                }
                if (WindowState == WindowState.Maximized)
                {
                    var waDip = GetMonitorWorkAreaDip();
                    MaxWidth = waDip.Width;
                    MaxHeight = waDip.Height;
                    UpdateResponsiveLayout(waDip.Width, waDip.Height);
                }
                else if (WindowState == WindowState.Normal)
                {
                    MaxWidth = double.PositiveInfinity;
                    MaxHeight = double.PositiveInfinity;
                    if (_restoreBoundsDip.Width > 0 && _restoreBoundsDip.Height > 0)
                    {
                        Width = _restoreBoundsDip.Width;
                        Height = _restoreBoundsDip.Height;
                        Left = _restoreBoundsDip.Left;
                        Top = _restoreBoundsDip.Top;
                    }
                    ClampToMonitorWorkArea(recenter: false);
                }
            };

            ScrollViewer.SetHorizontalScrollBarVisibility(_list, ScrollBarVisibility.Disabled);
            ScrollViewer.SetCanContentScroll(_list, false);
            _list.Resources.Add(typeof(ScrollBar), Theme.BuildSleekScrollBarStyle());

            var scrollFadeTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
            ScrollBar cachedScrollBar = null;
            scrollFadeTimer.Tick += (s2, e) =>
            {
                scrollFadeTimer.Stop();
                if (cachedScrollBar == null) cachedScrollBar = FindChild<ScrollBar>(_list);
                if (cachedScrollBar != null && !cachedScrollBar.IsMouseOver)
                {
                    var anim = new System.Windows.Media.Animation.DoubleAnimation(0.0, TimeSpan.FromMilliseconds(300));
                    cachedScrollBar.BeginAnimation(UIElement.OpacityProperty, anim);
                }
            };

            _list.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler((s2, e) =>
            {
                if (Math.Abs(e.VerticalChange) > 0.05)
                {
                    if (cachedScrollBar == null) cachedScrollBar = FindChild<ScrollBar>(_list);
                    if (cachedScrollBar != null)
                    {
                        cachedScrollBar.BeginAnimation(UIElement.OpacityProperty, null);
                        cachedScrollBar.Opacity = 0.85;
                        scrollFadeTimer.Stop();
                        scrollFadeTimer.Start();
                    }
                }
            }));

            _list.ItemContainerStyle = BuildCardContainerStyle();
            _list.ItemTemplate = ItemTemplate();
            _list.ContextMenu = new ContextMenu();
            _list.ContextMenuOpening += OnListContextMenuOpening;
            _list.AddHandler(Button.ClickEvent, new RoutedEventHandler(OnCardButtonClick));
            AutomationProperties.SetName(_list, "Groups");
            AutomationProperties.SetAutomationId(_list, "GroupsList");
            AutomationProperties.SetName(_searchBox, "Search groups or apps");
            AutomationProperties.SetAutomationId(_searchBox, "SearchBox");

            _groupsViewSource = new CollectionViewSource { Source = _s.Groups };
            _groupsView = _groupsViewSource.View;

            _list.SelectionChanged += (s2, e) =>
            {
                var selected = Sel;
                if (selected != null)
                {
                    // A filter/view refresh can re-point the selected index at a different group. Do not let
                    // that transient selection overwrite the identity the user actually chose.
                    if (!_suppressSelectionMemory) _rememberedGroupId = selected.Id;
                    _list.ScrollIntoView(selected);
                    JumpListHelper.Update(selected);
                }
                UpdateCommandState();
            };
            _list.MouseDoubleClick += (s2, e) =>
            {
                // Double-click edits only the card body. Buttons inside the card own their own gestures.
                if (FindAncestor<Button>(e.OriginalSource as DependencyObject) == null) Edit();
            };
            _list.PreviewKeyDown += (s2, e) =>
            {
                // Space selects the focused card without changing the keyboard navigation model. Leave
                // Space alone when a card button owns keyboard focus so the button can be pressed.
                if (e.Key == Key.Space && FindAncestor<Button>(Keyboard.FocusedElement as DependencyObject) == null &&
                    TrySelectFocusedItem()) e.Handled = true;
            };
            _list.KeyDown += (s2, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    // Enter edits the card body, never a focused card button.
                    if (FindAncestor<Button>(Keyboard.FocusedElement as DependencyObject) == null && Sel != null)
                    {
                        Edit();
                        e.Handled = true;
                    }
                }
                else if (e.Key == Key.Delete)
                {
                    if (Sel != null) Delete();
                    e.Handled = true;
                }
            };

            PreviewKeyDown += (s2, e) =>
            {
                if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    _searchBox.Focus();
                    _searchBox.SelectAll();
                    e.Handled = true;
                }
            };

            Point dragStart = default;
            GroupData dragCandidate = null;
            bool dragFromIcon = false;

            _list.PreviewMouseLeftButtonDown += (s2, e) =>
            {
                dragStart = e.GetPosition(_list);
                dragFromIcon = false;
                dragCandidate = null;
                _suppressIconClick = false;

                var source = e.OriginalSource as DependencyObject ?? _list.InputHitTest(dragStart) as DependencyObject;
                var lbi = FindAncestor<ListBoxItem>(source);
                if (lbi != null) lbi.IsSelected = true;

                // The group icon is a real Button: a plain click raises Click (open launcher) while a
                // drag past the threshold starts the existing shell pin gesture and suppresses that click.
                if (FindAncestorByTag(source, "GroupIconHandle") != null && lbi?.DataContext is GroupData itemGroup)
                {
                    dragFromIcon = true;
                    dragCandidate = itemGroup;
                }
            };

            _list.PreviewMouseRightButtonDown += (s2, e) =>
            {
                // Right-click selects the card it lands on, so the more-options menu always acts on it.
                var lbi = FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject);
                if (lbi?.DataContext is GroupData)
                {
                    lbi.IsSelected = true;
                    lbi.Focus();
                }
            };

            _list.PreviewMouseMove += (s2, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed && dragCandidate != null && dragFromIcon)
                {
                    var pos = e.GetPosition(_list);
                    var diff = dragStart - pos;
                    if (Math.Abs(diff.X) > 4.0 || Math.Abs(diff.Y) > 4.0)
                    {
                        var groupToDrag = dragCandidate;
                        dragCandidate = null;
                        dragFromIcon = false;
                        _suppressIconClick = true;
                        Mouse.Capture(null);
                        var hwnd = new WindowInteropHelper(this).Handle;
                        ShellDragDrop.DoDragLink(hwnd, groupToDrag);
                    }
                }
            };

            _list.ItemsSource = _groupsView;
            if (_s.Groups.Count > 0)
            {
                _rememberedGroupId = _s.Groups[0].Id;
                _list.SelectedItem = _s.Groups[0];
            }
            _s.Groups.CollectionChanged += (s2, e) =>
            {
                UpdateStats();
                RestoreSelectionFromRemembered();
                UpdateCommandState();
            };
            UpdateStats();

            var titleBar = BuildTitleBar();
            var statsBar = BuildStatsRibbon();
            var tipBanner = BuildProTipBanner();
            var footer = BuildFooterCommandBar();
            UpdateCommandState();

            var dock = new DockPanel();
            DockPanel.SetDock(titleBar, Dock.Top);
            DockPanel.SetDock(statsBar, Dock.Top);
            DockPanel.SetDock(footer, Dock.Bottom);
            DockPanel.SetDock(tipBanner, Dock.Bottom);
            dock.Children.Add(titleBar);
            dock.Children.Add(statsBar);
            dock.Children.Add(footer);
            dock.Children.Add(tipBanner);
            dock.Children.Add(_list);

            // Multi-layered Mica radial ambient glow matching .mica-backdrop in code.html
            var rootGrid = new Grid();
            var topGlow = new Border
            {
                IsHitTestVisible = false,
                Background = new RadialGradientBrush
                {
                    GradientOrigin = new Point(0.10, 0.15),
                    Center = new Point(0.10, 0.15),
                    RadiusX = 0.55,
                    RadiusY = 0.55,
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb(40, 245, 158, 11), 0.0),
                        new GradientStop(Color.FromArgb(0, 0, 0, 0), 1.0)
                    }
                }
            };
            var bottomGlow = new Border
            {
                IsHitTestVisible = false,
                Background = new RadialGradientBrush
                {
                    GradientOrigin = new Point(0.90, 0.85),
                    Center = new Point(0.90, 0.85),
                    RadiusX = 0.50,
                    RadiusY = 0.50,
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb(30, 217, 119, 6), 0.0),
                        new GradientStop(Color.FromArgb(0, 0, 0, 0), 1.0)
                    }
                }
            };
            rootGrid.Children.Add(topGlow);
            rootGrid.Children.Add(bottomGlow);
            rootGrid.Children.Add(dock);

            _outerBorder = new Border
            {
                Background = Theme.Bg,
                BorderBrush = Theme.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                ClipToBounds = true,
                Child = rootGrid
            };
            Content = _outerBorder;

            // Perform initial work-area clamp prior to showing
            ClampToMonitorWorkArea(recenter: true);
        }

        public (double dpiX, double dpiY) GetEffectiveDpiScale()
        {
            if (_simulatedDpiScale.HasValue && _simulatedDpiScale.Value > 0)
                return (_simulatedDpiScale.Value, _simulatedDpiScale.Value);
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd == IntPtr.Zero)
                {
                    GetCursorPos(out var pt);
                    IntPtr hMon = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
                    if (hMon != IntPtr.Zero && GetDpiForMonitor(hMon, 0, out uint mdx, out uint mdy) == 0 && mdx > 0 && mdy > 0)
                    {
                        return (mdx / 96.0, mdy / 96.0);
                    }
                }
            }
            catch { }
            var dpi = VisualTreeHelper.GetDpi(this);
            double dx = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
            double dy = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;
            return (dx, dy);
        }
    }
}
