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
        Border BuildTrafficLight(string fillHex, string borderHex, string glyph, string tooltip, Action onClick)
        {
            var sym = new TextBlock
            {
                Text = glyph, FontSize = 7.5, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Hidden
            };
            var dot = new Border
            {
                Width = 12, Height = 12, CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fillHex)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(borderHex)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand, ToolTip = tooltip,
                Child = sym
            };
            dot.MouseEnter += (s, e) => sym.Visibility = Visibility.Visible;
            dot.MouseLeave += (s, e) => sym.Visibility = Visibility.Hidden;
            dot.MouseLeftButtonDown += (s, e) => e.Handled = true;
            dot.MouseLeftButtonUp += (s, e) => { e.Handled = true; onClick(); };
            return dot;
        }

        Border BuildHeaderVectorBtn(string pathData, string tooltip, Action onClick)
        {
            var b = new Border
            {
                Width = 28, Height = 28, CornerRadius = new CornerRadius(6),
                Background = Brushes.Transparent, Cursor = Cursors.Hand, ToolTip = tooltip,
                Margin = new Thickness(2, 0, 0, 0),
                Child = Lucide.Make(pathData, Theme.Sub, 14)
            };
            b.MouseEnter += (s, e) => b.Background = Theme.PillHover;
            b.MouseLeave += (s, e) => b.Background = Brushes.Transparent;
            b.MouseLeftButtonDown += (s, e) => e.Handled = true;
            b.MouseLeftButtonUp += (s, e) => { e.Handled = true; onClick(); };
            return b;
        }

        Button BuildHeaderIconButton(string pathData, string tooltip, string automationId, bool fill = false, Brush fixedBrush = null)
        {
            var bd = new FrameworkElementFactory(typeof(Border), "bd");
            bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            bd.SetValue(Border.BackgroundProperty, Brushes.Transparent);

            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            bd.AppendChild(cp);

            var ct = new ControlTemplate(typeof(Button)) { VisualTree = bd };
            var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(Border.BackgroundProperty, Theme.Bind("PillHover"), "bd"));
            ct.Triggers.Add(hover);
            var pressed = new Trigger { Property = ButtonBase.IsPressedProperty, Value = true };
            pressed.Setters.Add(new Setter(UIElement.OpacityProperty, 0.7));
            ct.Triggers.Add(pressed);

            var b = new Button
            {
                Width = 28, Height = 28, Padding = new Thickness(0),
                BorderThickness = new Thickness(0), Background = Brushes.Transparent,
                Cursor = Cursors.Hand, ToolTip = tooltip,
                Margin = new Thickness(2, 0, 0, 0),
                Template = ct,
                Content = Lucide.Make(pathData, fixedBrush ?? Theme.Sub, 14, fill)
            };
            AutomationProperties.SetName(b, tooltip);
            AutomationProperties.SetAutomationId(b, automationId);
            return b;
        }

        UIElement BuildTitleBar()
        {
            var grid = new Grid { Height = 48, Margin = new Thickness(16, 0, 14, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Left: macOS Traffic Lights + Divider + Brand Squircle + XAppGroup + v2.4 PRO
            var left = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            left.Children.Add(BuildTrafficLight("#FF5F56", "#E0443E", "✕", "Close to System Tray", () => Close()));
            left.Children.Add(BuildTrafficLight("#FFBD2E", "#DEA123", "−", "Minimize", () => WindowState = WindowState.Minimized));
            left.Children.Add(BuildTrafficLight("#27C93F", "#1AAB29", "+", "Expand / Restore", ToggleMaximize));

            left.Children.Add(new Border { Width = 1, Height = 16, Background = Theme.Divider, Margin = new Thickness(6, 0, 14, 0) });

            var brandRing = Icons.BuildBrandSquircle(20.0);
            brandRing.Margin = new Thickness(0, 0, 8, 0);
            left.Children.Add(brandRing);
            var titleBlock = new TextBlock
            {
                Text = "XAppGroup", FontSize = 13.5, FontWeight = FontWeights.Bold,
                Foreground = Theme.Fg, VerticalAlignment = VerticalAlignment.Center
            };
            titleBlock.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Fg"));
            left.Children.Add(titleBlock);
            var proBadge = new Border
            {
                CornerRadius = new CornerRadius(4), Padding = new Thickness(6, 1.5, 6, 1.5), Margin = new Thickness(8, 0, 0, 0),
                Background = Theme.CyanBadgeBg, BorderBrush = Theme.CyanBadgeBorder, BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center
            };
            proBadge.SetBinding(Border.BackgroundProperty, Theme.Bind("CyanBadgeBg"));
            proBadge.SetBinding(Border.BorderBrushProperty, Theme.Bind("CyanBadgeBorder"));
            var proBadgeTxt = new TextBlock
            {
                Text = $"v{Settings.AppVersion}", FontSize = 10, FontFamily = new FontFamily("Cascadia Mono, Consolas"),
                Foreground = Theme.CyanBadgeFg, FontWeight = FontWeights.Bold
            };
            proBadgeTxt.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("CyanBadgeFg"));
            proBadge.Child = proBadgeTxt;
            left.Children.Add(proBadge);
            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            // Center: Search Bar with Lucide Search Vector Icon and Ctrl+F KBD badge
            var searchHint = new TextBlock
            {
                Text = "Search groups or apps (Ctrl + F)...",
                Foreground = Theme.Sub, FontSize = 12, IsHitTestVisible = false,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(30, 0, 48, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            searchHint.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Sub"));
            _searchBox.TextChanged += (s, e) =>
            {
                searchHint.Visibility = string.IsNullOrEmpty(_searchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
                ApplySearchFilter(_searchBox.Text);
            };
            _searchBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    _searchBox.Text = "";
                    _list.Focus();
                    e.Handled = true;
                }
            };
            _searchBox.Margin = new Thickness(30, 0, 48, 0);

            var searchIcon = Lucide.Make(Lucide.Search, Theme.Sub, 13.5);
            searchIcon.HorizontalAlignment = HorizontalAlignment.Left;
            searchIcon.Margin = new Thickness(10, 0, 0, 0);
            searchIcon.IsHitTestVisible = false;

            var searchKbd = new Border
            {
                CornerRadius = new CornerRadius(4),
                Background = Theme.PillBg,
                BorderBrush = Theme.PillBorder,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4, 1, 4, 1),
                Margin = new Thickness(0, 0, 7, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            searchKbd.SetBinding(Border.BackgroundProperty, Theme.Bind("PillBg"));
            searchKbd.SetBinding(Border.BorderBrushProperty, Theme.Bind("PillBorder"));
            var searchKbdTxt = new TextBlock
            {
                Text = "Ctrl+F",
                FontSize = 9.5,
                FontFamily = new FontFamily("Cascadia Mono, Consolas"),
                Foreground = Theme.Sub
            };
            searchKbdTxt.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Sub"));
            searchKbd.Child = searchKbdTxt;

            var searchGrid = new Grid();
            searchGrid.Children.Add(searchIcon);
            searchGrid.Children.Add(searchHint);
            searchGrid.Children.Add(_searchBox);
            searchGrid.Children.Add(searchKbd);

            var searchBorder = new Border
            {
                MaxWidth = 360, MinWidth = 100, Height = 29, CornerRadius = new CornerRadius(8),
                Background = Theme.PillBg, BorderBrush = Theme.PillBorder, BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(12, 0, 12, 0),
                Child = searchGrid
            };
            searchBorder.SizeChanged += (s, e) =>
            {
                bool compact = e.NewSize.Width < 210;
                searchKbd.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
                var m = compact ? new Thickness(30, 0, 8, 0) : new Thickness(30, 0, 48, 0);
                _searchBox.Margin = m;
                searchHint.Margin = m;
            };
            searchBorder.MouseLeftButtonDown += (s, e) => e.Handled = true;
            Grid.SetColumn(searchBorder, 1);
            grid.Children.Add(searchBorder);

            // Right: Refresh + Settings + Divider + Donate (Coffee Cup) + GitHub (Official).
            var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            right.Children.Add(BuildHeaderVectorBtn(Lucide.RefreshCw, "Refresh Groups & Icons", () =>
            {
                foreach (var g in _s.Groups) g.InvalidateIcons();
                UpdateStats();
            }));
            right.Children.Add(BuildHeaderVectorBtn(Lucide.Settings, "Settings", () => OpenSettings()));
            right.Children.Add(new Border { Width = 1, Height = 16, Background = Theme.Divider, Margin = new Thickness(6, 0, 6, 0) });
            var donateBtn = BuildHeaderIconButton(Lucide.Coffee, "Donate", "HeaderDonateBtn", fill: false, fixedBrush: new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")));
            donateBtn.Click += (s, e) => SupportDialog.ShowPrompt(this);
            right.Children.Add(donateBtn);

            var githubBtn = BuildHeaderIconButton(Lucide.GitHub, "GitHub", "HeaderGitHubBtn", fill: true);
            githubBtn.Click += (s, e) => AppConfig.OpenUrl(AppConfig.Reload().GitHubUrl);
            right.Children.Add(githubBtn);
            Grid.SetColumn(right, 2);
            grid.Children.Add(right);

            var header = new Border
            {
                Background = Theme.TitleBg,
                BorderBrush = Theme.Divider,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Child = grid
            };
            header.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2) ToggleMaximize();
                else if (e.LeftButton == MouseButtonState.Pressed) { try { DragMove(); } catch { } }
            };
            return header;
        }

        void ToggleMaximize()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        UIElement BuildStatsRibbon()
        {
            _statsInnerDock = new DockPanel { Margin = new Thickness(24, 9, 24, 9) };

            var left = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            var groupBadge = new Border
            {
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(6, 1, 6, 1),
                Background = Theme.CyanBadgeBg,
                BorderBrush = Theme.CyanBadgeBorder,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 7, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = _groupCountText
            };
            groupBadge.SetBinding(Border.BackgroundProperty, Theme.Bind("CyanBadgeBg"));
            groupBadge.SetBinding(Border.BorderBrushProperty, Theme.Bind("CyanBadgeBorder"));
            _groupCountText.Foreground = Theme.CyanBadgeFg;
            _groupCountText.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("CyanBadgeFg"));
            _groupCountText.FontSize = 11;
            left.Children.Add(groupBadge);
            left.Children.Add(new TextBlock { Text = "App Groups", FontSize = 12, Foreground = Theme.Fg, Margin = new Thickness(0, 0, 16, 0), VerticalAlignment = VerticalAlignment.Center });

            left.Children.Add(new Border { Width = 1, Height = 14, Background = Theme.Divider, Margin = new Thickness(0, 0, 16, 0) });

            var cpuIcon = Lucide.Make(Lucide.Cpu, Theme.Cyan, 14);
            cpuIcon.Margin = new Thickness(0, 0, 6, 0);
            left.Children.Add(cpuIcon);
            _launcherCountText.Foreground = Theme.Fg;
            _launcherCountText.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Fg"));
            left.Children.Add(_launcherCountText);
            left.Children.Add(new TextBlock { Text = " Configured Launchers", FontSize = 12, Foreground = Theme.Sub, Margin = new Thickness(0, 0, 16, 0), VerticalAlignment = VerticalAlignment.Center });

            left.Children.Add(new Border { Width = 1, Height = 14, Background = Theme.Divider, Margin = new Thickness(0, 0, 16, 0) });

            var daemonDot = new Grid { Width = 8, Height = 8, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 7, 0) };
            daemonDot.Children.Add(new Border { Width = 8, Height = 8, CornerRadius = new CornerRadius(4), Background = Theme.CyanBadgeBg });
            daemonDot.Children.Add(new Border { Width = 6, Height = 6, CornerRadius = new CornerRadius(3), Background = Theme.Cyan, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
            left.Children.Add(daemonDot);

            left.Children.Add(new TextBlock
            {
                Text = "Background Daemon Active", FontSize = 12, FontWeight = FontWeights.Medium,
                Foreground = Theme.Cyan, VerticalAlignment = VerticalAlignment.Center
            });
            _statsInnerDock.Children.Add(left);

            _statsRibbon = new Border
            {
                Background = Theme.StatsBg,
                BorderBrush = Theme.Divider,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Child = _statsInnerDock
            };
            return _statsRibbon;
        }

        UIElement BuildProTipBanner()
        {
            _tipBanner = new Border
            {
                Background = Theme.TipBg,
                BorderBrush = Theme.Divider,
                BorderThickness = new Thickness(0, 1, 0, 1),
                Padding = new Thickness(24, 9, 20, 9)
            };
            var dock = new DockPanel();
            var dismiss = new Border
            {
                CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 3, 8, 3),
                Background = Brushes.Transparent, Cursor = Cursors.Hand, VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock { Text = "Dismiss", FontSize = 12, Foreground = Theme.Sub }
            };
            dismiss.MouseEnter += (s, e) => dismiss.Background = Theme.PillBg;
            dismiss.MouseLeave += (s, e) => dismiss.Background = Brushes.Transparent;
            dismiss.MouseLeftButtonUp += (s, e) => { _tipDismissedByUser = true; _tipBanner.Visibility = Visibility.Collapsed; };
            DockPanel.SetDock(dismiss, Dock.Right);
            dock.Children.Add(dismiss);

            var left = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            left.Children.Add(new Border
            {
                Width = 20, Height = 20, CornerRadius = new CornerRadius(10),
                Background = Theme.CyanBadgeBg, BorderBrush = Theme.CyanBadgeBorder, BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 10, 0),
                Child = Lucide.Make(Lucide.Lightbulb, Theme.Cyan, 12)
            });

            var tipText = new TextBlock { FontSize = 12, Foreground = Theme.Sub, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
            tipText.Inlines.Add(new System.Windows.Documents.Run("Pro Tip: ") { Foreground = Theme.Cyan, FontWeight = FontWeights.Bold });
            tipText.Inlines.Add(new System.Windows.Documents.Run("Drag a group's icon directly onto your ") { Foreground = Theme.Sub });
            tipText.Inlines.Add(new System.Windows.Documents.Run("Windows Taskbar") { Foreground = Theme.Fg, FontWeight = FontWeights.SemiBold, TextDecorations = TextDecorations.Underline });
            tipText.Inlines.Add(new System.Windows.Documents.Run(" or ") { Foreground = Theme.Sub });
            tipText.Inlines.Add(new System.Windows.Documents.Run("Desktop") { Foreground = Theme.Fg, FontWeight = FontWeights.SemiBold, TextDecorations = TextDecorations.Underline });
            tipText.Inlines.Add(new System.Windows.Documents.Run(" to create an instant 1-click launcher.") { Foreground = Theme.Sub });
            left.Children.Add(tipText);

            dock.Children.Add(left);
            _tipBanner.Child = dock;
            return _tipBanner;
        }

        ComboBox BuildThemedDropdown()
        {
            var cb = new ComboBox
            {
                Width = 128,
                Height = 32,
                FontSize = 12,
                Foreground = Theme.Fg,
                Background = Theme.PillBg,
                BorderBrush = Theme.PillBorder,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            AutomationProperties.SetName(cb, "Theme");
            AutomationProperties.SetAutomationId(cb, "ThemeSelector");

            // Custom dark/themed pill ControlTemplate so it matches screen.png instead of Windows 95 white ComboBox
            var rootGrid = new FrameworkElementFactory(typeof(Grid));
            var toggle = new FrameworkElementFactory(typeof(ToggleButton), "ToggleButton");
            toggle.SetValue(ToggleButton.FocusableProperty, false);
            toggle.SetValue(ToggleButton.ClickModeProperty, ClickMode.Press);
            toggle.SetBinding(ToggleButton.IsCheckedProperty, new System.Windows.Data.Binding("IsDropDownOpen")
            {
                RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent),
                Mode = System.Windows.Data.BindingMode.TwoWay
            });

            var toggleBd = new FrameworkElementFactory(typeof(Border), "ToggleBorder");
            toggleBd.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            toggleBd.SetBinding(Border.BackgroundProperty, Theme.Bind("PillBg"));
            toggleBd.SetBinding(Border.BorderBrushProperty, Theme.Bind("PillBorder"));
            toggleBd.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            toggleBd.SetValue(Border.PaddingProperty, new Thickness(12, 0, 10, 0));

            var toggleDock = new FrameworkElementFactory(typeof(DockPanel));
            var chev = Lucide.MakeFactory(Lucide.ChevronDown, "Sub", 12);
            chev.SetValue(DockPanel.DockProperty, Dock.Right);
            toggleDock.AppendChild(chev);

            var cp = new FrameworkElementFactory(typeof(TextBlock), "ContentSite");
            cp.SetValue(UIElement.IsHitTestVisibleProperty, false);
            cp.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("SelectedItem")
            {
                RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.FindAncestor, typeof(ComboBox), 1)
            });
            cp.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Fg"));
            cp.SetValue(TextBlock.FontSizeProperty, 12.0);
            cp.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            cp.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            toggleDock.AppendChild(cp);

            toggleBd.AppendChild(toggleDock);
            var toggleCt = new ControlTemplate(typeof(ToggleButton)) { VisualTree = toggleBd };
            var toggleHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            toggleHover.Setters.Add(new Setter(Border.BackgroundProperty, Theme.Bind("PillHover"), "ToggleBorder"));
            toggleHover.Setters.Add(new Setter(TextBlock.ForegroundProperty, Theme.Bind("HoverFg"), "ContentSite"));
            toggleCt.Triggers.Add(toggleHover);
            toggle.SetValue(Control.TemplateProperty, toggleCt);
            rootGrid.AppendChild(toggle);

            var popup = new FrameworkElementFactory(typeof(Popup), "PART_Popup");
            popup.SetValue(Popup.PlacementProperty, PlacementMode.Top);
            popup.SetValue(Popup.AllowsTransparencyProperty, true);
            popup.SetValue(Popup.FocusableProperty, false);
            popup.SetBinding(Popup.IsOpenProperty, new System.Windows.Data.Binding("IsDropDownOpen")
            {
                RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)
            });

            var dropBd = new FrameworkElementFactory(typeof(Border));
            dropBd.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            dropBd.SetBinding(Border.BackgroundProperty, Theme.Bind("PopupBg"));
            dropBd.SetBinding(Border.BorderBrushProperty, Theme.Bind("CardBorder"));
            dropBd.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            dropBd.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 4));
            dropBd.SetValue(Border.PaddingProperty, new Thickness(4));
            dropBd.SetValue(FrameworkElement.MinWidthProperty, 128.0);

            var itemsPresenter = new FrameworkElementFactory(typeof(ItemsPresenter));
            dropBd.AppendChild(itemsPresenter);
            popup.AppendChild(dropBd);
            rootGrid.AppendChild(popup);

            cb.Template = new ControlTemplate(typeof(ComboBox)) { VisualTree = rootGrid };

            var itemBd = new FrameworkElementFactory(typeof(Border), "ItemBorder");
            itemBd.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            itemBd.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            itemBd.SetValue(Border.PaddingProperty, new Thickness(10, 6, 10, 6));

            var itemCp = new FrameworkElementFactory(typeof(ContentPresenter));
            itemBd.AppendChild(itemCp);

            var itemCt = new ControlTemplate(typeof(ComboBoxItem)) { VisualTree = itemBd };
            var itemHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            itemHover.Setters.Add(new Setter(Border.BackgroundProperty, Theme.Bind("PillHover"), "ItemBorder"));
            itemHover.Setters.Add(new Setter(Control.ForegroundProperty, Theme.Bind("HoverFg")));
            itemCt.Triggers.Add(itemHover);

            var itemSel = new Trigger { Property = ComboBoxItem.IsSelectedProperty, Value = true };
            itemSel.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(40, 245, 158, 11)), "ItemBorder"));
            itemSel.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBBF24"))));
            itemCt.Triggers.Add(itemSel);

            var itemStyle = new Style(typeof(ComboBoxItem));
            itemStyle.Setters.Add(new Setter(Control.TemplateProperty, itemCt));
            itemStyle.Setters.Add(new Setter(Control.ForegroundProperty, Theme.Bind("Fg")));
            itemStyle.Setters.Add(new Setter(FrameworkElement.CursorProperty, Cursors.Hand));
            cb.ItemContainerStyle = itemStyle;

            cb.Items.Add("System (Mica)");
            cb.Items.Add("Dark Acrylic");
            cb.Items.Add("Light Glass");

            cb.SelectedItem = _s.Theme == "Light" ? "Light Glass" : (_s.Theme == "Dark" ? "Dark Acrylic" : "System (Mica)");
            cb.SelectionChanged += (a, b) =>
            {
                var label = cb.SelectedItem as string ?? "System (Mica)";
                string newTheme = label.StartsWith("Light") ? "Light" : (label.StartsWith("Dark") ? "Dark" : "System");
                if (_s.Theme != newTheme)
                {
                    _s.Theme = newTheme;
                    _changed();
                    Theme.Apply(newTheme);
                    if (_s?.Groups != null)
                    {
                        foreach (var g in _s.Groups) g.RefreshThemeBrushes();
                    }
                }
            };
            _themeSelector = cb;
            return cb;
        }
    }
}
