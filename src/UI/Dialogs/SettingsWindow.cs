using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace XAppGroup
{
    public class SettingsWindow : Window
    {
        private readonly Settings _s;
        private readonly Action _onSettingsChanged;
        private TextBlock _updateStatusText;

        public SettingsWindow(Settings settings, Action onSettingsChanged)
        {
            _s = settings;
            _onSettingsChanged = onSettingsChanged;

            Title = "Settings";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Width = 540;
            MaxHeight = 660;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;

            try
            {
                var icoPath = System.IO.Path.Combine(AppContext.BaseDirectory, "XAppGroup.ico");
                if (System.IO.File.Exists(icoPath)) Icon = new BitmapImage(new Uri(icoPath, UriKind.Absolute));
            }
            catch { }

            // Sync startup toggle with actual registry state
            _s.RunAtStartup = StartupHelper.IsEnabled();

            var outer = new Border
            {
                CornerRadius = new CornerRadius(14),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(14),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 24,
                    Opacity = Theme.Dark ? 0.55 : 0.20,
                    ShadowDepth = 6
                }
            };
            outer.SetBinding(Border.BackgroundProperty, Theme.Bind("Bg"));
            outer.SetBinding(Border.BorderBrushProperty, Theme.Bind("CardBorder"));

            var rootDock = new DockPanel { LastChildFill = true };

            // --- Header ---
            var headerDock = new DockPanel
            {
                Margin = new Thickness(22, 18, 16, 12),
                Background = Brushes.Transparent
            };
            headerDock.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                    try { DragMove(); } catch { }
            };

            var closeBtn = new Border
            {
                Width = 30,
                Height = 30,
                CornerRadius = new CornerRadius(8),
                Cursor = Cursors.Hand,
                Background = Brushes.Transparent,
                ToolTip = "Close"
            };
            var closeTxt = new TextBlock
            {
                Text = "✕",
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            closeTxt.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Sub"));
            closeBtn.Child = closeTxt;
            closeBtn.MouseEnter += (s, e) =>
            {
                closeBtn.SetBinding(Border.BackgroundProperty, Theme.Bind("PillHover"));
                closeTxt.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Fg"));
            };
            closeBtn.MouseLeave += (s, e) =>
            {
                closeBtn.Background = Brushes.Transparent;
                closeTxt.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Sub"));
            };
            closeBtn.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                Close();
            };
            DockPanel.SetDock(closeBtn, Dock.Right);
            headerDock.Children.Add(closeBtn);

            var titleTxt = new TextBlock
            {
                Text = "Settings",
                FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI"),
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            titleTxt.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Fg"));
            headerDock.Children.Add(titleTxt);

            DockPanel.SetDock(headerDock, Dock.Top);
            rootDock.Children.Add(headerDock);

            // --- Scrollable Content ---
            var contentStack = new StackPanel { Margin = new Thickness(22, 2, 22, 22) };

            // 1. General
            contentStack.Children.Add(BuildSettingCard(
                "Run at startup",
                "Automatically start the application when Windows starts",
                BuildToggleSwitch(_s.RunAtStartup, val =>
                {
                    _s.RunAtStartup = val;
                    StartupHelper.SetEnabled(val);
                    NotifyChanged();
                })
            ));

            contentStack.Children.Add(BuildSettingCard(
                "Show system tray icon",
                "Display the application icon in the system tray",
                BuildToggleSwitch(_s.ShowTrayIcon, val =>
                {
                    _s.ShowTrayIcon = val;
                    NotifyChanged();
                })
            ));

            contentStack.Children.Add(BuildSettingCard(
                "Show groups in system tray menu",
                "Make your app groups accessible from the system tray right-click menu",
                BuildToggleSwitch(_s.ShowGroupsInTray, val =>
                {
                    _s.ShowGroupsInTray = val;
                    NotifyChanged();
                })
            ));

            // 2. Appearance
            contentStack.Children.Add(BuildSectionHeader("Appearance"));

            var appThemeCombo = BuildComboBox(
                new[] { "Use system setting", "Dark Acrylic", "Light Glass" },
                _s.Theme == "Light" ? "Light Glass" : (_s.Theme == "Dark" ? "Dark Acrylic" : "Use system setting"),
                sel =>
                {
                    string mode = sel.StartsWith("Light") ? "Light" : (sel.StartsWith("Dark") ? "Dark" : "System");
                    if (_s.Theme != mode)
                    {
                        _s.Theme = mode;
                        Theme.Apply(mode);
                        if (_s.Groups != null)
                            foreach (var g in _s.Groups) g.RefreshThemeBrushes();
                        NotifyChanged();
                    }
                }
            );
            contentStack.Children.Add(BuildSettingCard("App theme", "Select which app theme to display", appThemeCombo));

            var popupThemeCombo = BuildComboBox(
                new[] { "Windows mode", "Dark Acrylic", "Light Glass" },
                string.IsNullOrWhiteSpace(_s.PopupTheme) ? "Windows mode" : _s.PopupTheme,
                sel =>
                {
                    _s.PopupTheme = sel;
                    NotifyChanged();
                }
            );
            contentStack.Children.Add(BuildSettingCard("Popup theme", "Select the theme for the popup window", popupThemeCombo));

            contentStack.Children.Add(BuildSettingCard(
                "Accent color background",
                "Use Windows accent color as popup background",
                BuildToggleSwitch(_s.AccentColorBackground, val =>
                {
                    _s.AccentColorBackground = val;
                    NotifyChanged();
                })
            ));

            // 3. Animation
            contentStack.Children.Add(BuildSectionHeader("Animation"));

            contentStack.Children.Add(BuildSettingCard(
                "Window",
                "Animate window when opening and closing",
                BuildToggleSwitch(_s.AnimateWindow, val =>
                {
                    _s.AnimateWindow = val;
                    NotifyChanged();
                })
            ));

            contentStack.Children.Add(BuildSettingCard(
                "Content",
                "Animate items inside the group",
                BuildToggleSwitch(_s.AnimateContent, val =>
                {
                    _s.AnimateContent = val;
                    NotifyChanged();
                })
            ));

            // 4. Others
            contentStack.Children.Add(BuildSectionHeader("Others"));

            contentStack.Children.Add(BuildSettingCard(
                "Enable \"Launch All\"",
                "Show 'Launch All' option in the popup and right-click menu",
                BuildToggleSwitch(_s.EnableLaunchAll, val =>
                {
                    _s.EnableLaunchAll = val;
                    NotifyChanged();
                })
            ));

            contentStack.Children.Add(BuildSettingCard(
                "Grayscale icon",
                "Show the icon in grayscale when the group is showing",
                BuildToggleSwitch(_s.GrayscaleIcon, val =>
                {
                    _s.GrayscaleIcon = val;
                    NotifyChanged();
                })
            ));

            // 5. About
            contentStack.Children.Add(BuildSectionHeader("About"));

            contentStack.Children.Add(BuildSettingCard(
                "XAppGroup",
                $"Version {Settings.AppVersion}",
                Icons.BuildBrandSquircle(32.0)
            ));

            contentStack.Children.Add(BuildSettingCard(
                "Check for updates on startup",
                "Automatically check for new versions when the app starts",
                BuildToggleSwitch(_s.CheckUpdates, val =>
                {
                    _s.CheckUpdates = val;
                    NotifyChanged();
                })
            ));

            var checkBtn = Theme.Btn("Check Now", (s, e) =>
            {
                if (_updateStatusText != null)
                    _updateStatusText.Text = $"You're up to date! (v{Settings.AppVersion})";
            });
            checkBtn.Margin = new Thickness(0);
            checkBtn.Padding = new Thickness(14, 7, 14, 7);

            var updateCard = BuildSettingCard(
                "Check for updates",
                $"You're up to date! (v{Settings.AppVersion})",
                checkBtn,
                out _updateStatusText
            );
            contentStack.Children.Add(updateCard);

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = contentStack
            };
            scroll.Resources.Add(typeof(ScrollBar), Theme.BuildSleekScrollBarStyle());

            rootDock.Children.Add(scroll);
            outer.Child = rootDock;
            Content = outer;

            KeyDown += (s, e) => { if (e.Key == Key.Escape) Close(); };
        }

        private void NotifyChanged()
        {
            _onSettingsChanged?.Invoke();
        }

        private static TextBlock BuildSectionHeader(string text)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI"),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(2, 16, 0, 8)
            };
            tb.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Fg"));
            return tb;
        }

        private static Border BuildSettingCard(string title, string subtitle, UIElement rightControl)
        {
            return BuildSettingCard(title, subtitle, rightControl, out _);
        }

        private static Border BuildSettingCard(string title, string subtitle, UIElement rightControl, out TextBlock subtitleBlock)
        {
            var dock = new DockPanel { LastChildFill = true };

            if (rightControl != null)
            {
                if (rightControl is FrameworkElement fe)
                {
                    fe.VerticalAlignment = VerticalAlignment.Center;
                    fe.Margin = new Thickness(14, 0, 0, 0);
                }
                DockPanel.SetDock(rightControl, Dock.Right);
                dock.Children.Add(rightControl);
            }

            var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var titleBlock = new TextBlock
            {
                Text = title,
                FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI"),
                FontSize = 13.5,
                FontWeight = FontWeights.SemiBold
            };
            titleBlock.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Fg"));

            subtitleBlock = new TextBlock
            {
                Text = subtitle,
                FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI"),
                FontSize = 11.5,
                Margin = new Thickness(0, 3, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            subtitleBlock.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Sub"));

            textStack.Children.Add(titleBlock);
            textStack.Children.Add(subtitleBlock);
            dock.Children.Add(textStack);

            var card = new Border
            {
                CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(18, 14, 18, 14),
                Margin = new Thickness(0, 0, 0, 8),
                Child = dock
            };
            card.SetBinding(Border.BackgroundProperty, Theme.Bind("Card"));
            card.SetBinding(Border.BorderBrushProperty, Theme.Bind("CardBorder"));
            return card;
        }

        private static CheckBox BuildToggleSwitch(bool initialValue, Action<bool> onChanged)
        {
            var amberBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
            var amberHoverBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBBF24"));
            var darkKnobBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#121415"));
            amberBrush.Freeze();
            amberHoverBrush.Freeze();
            darkKnobBrush.Freeze();

            var cb = new CheckBox
            {
                IsChecked = initialValue,
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };

            var track = new FrameworkElementFactory(typeof(Border), "Track");
            track.SetValue(FrameworkElement.WidthProperty, 44.0);
            track.SetValue(FrameworkElement.HeightProperty, 22.0);
            track.SetValue(Border.CornerRadiusProperty, new CornerRadius(11));
            track.SetValue(Border.BorderThicknessProperty, new Thickness(1.2));
            track.SetValue(Border.PaddingProperty, new Thickness(3));
            track.SetBinding(Border.BackgroundProperty, Theme.Bind("PillBg"));
            track.SetBinding(Border.BorderBrushProperty, Theme.Bind("HoverBorder"));

            var knob = new FrameworkElementFactory(typeof(Border), "Knob");
            knob.SetValue(FrameworkElement.WidthProperty, 14.0);
            knob.SetValue(FrameworkElement.HeightProperty, 14.0);
            knob.SetValue(Border.CornerRadiusProperty, new CornerRadius(7));
            knob.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            knob.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            knob.SetBinding(Border.BackgroundProperty, Theme.Bind("Sub"));

            track.AppendChild(knob);

            var tpl = new ControlTemplate(typeof(CheckBox)) { VisualTree = track };

            var checkedTrig = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true };
            checkedTrig.Setters.Add(new Setter(Border.BackgroundProperty, amberBrush, "Track"));
            checkedTrig.Setters.Add(new Setter(Border.BorderBrushProperty, amberBrush, "Track"));
            checkedTrig.Setters.Add(new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right, "Knob"));
            checkedTrig.Setters.Add(new Setter(Border.BackgroundProperty, darkKnobBrush, "Knob"));
            tpl.Triggers.Add(checkedTrig);

            var hoverCheckedTrig = new MultiTrigger();
            hoverCheckedTrig.Conditions.Add(new Condition(ToggleButton.IsCheckedProperty, true));
            hoverCheckedTrig.Conditions.Add(new Condition(UIElement.IsMouseOverProperty, true));
            hoverCheckedTrig.Setters.Add(new Setter(Border.BackgroundProperty, amberHoverBrush, "Track"));
            hoverCheckedTrig.Setters.Add(new Setter(Border.BorderBrushProperty, amberHoverBrush, "Track"));
            tpl.Triggers.Add(hoverCheckedTrig);

            cb.Template = tpl;
            cb.Checked += (s, e) => onChanged?.Invoke(true);
            cb.Unchecked += (s, e) => onChanged?.Invoke(false);
            return cb;
        }

        private static ComboBox BuildComboBox(string[] items, string selected, Action<string> onChanged)
        {
            var cb = new ComboBox
            {
                MinWidth = 165,
                Height = 32,
                FontSize = 12,
                Cursor = Cursors.Hand,
                FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI")
            };
            foreach (var it in items) cb.Items.Add(it);
            cb.SelectedItem = selected;

            var grid = new FrameworkElementFactory(typeof(Grid));
            var toggle = new FrameworkElementFactory(typeof(ToggleButton), "ToggleButton");
            toggle.SetValue(ToggleButton.FocusableProperty, false);
            toggle.SetValue(ToggleButton.ClickModeProperty, ClickMode.Press);
            toggle.SetBinding(ToggleButton.IsCheckedProperty, new System.Windows.Data.Binding("IsDropDownOpen")
            {
                RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent),
                Mode = System.Windows.Data.BindingMode.TwoWay
            });

            var tbBd = new FrameworkElementFactory(typeof(Border), "ComboBd");
            tbBd.SetValue(Border.CornerRadiusProperty, new CornerRadius(7));
            tbBd.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            tbBd.SetValue(Border.PaddingProperty, new Thickness(11, 0, 10, 0));
            tbBd.SetBinding(Border.BackgroundProperty, Theme.Bind("PillBg"));
            tbBd.SetBinding(Border.BorderBrushProperty, Theme.Bind("PillBorder"));

            var tbDock = new FrameworkElementFactory(typeof(DockPanel));
            var chev = Lucide.MakeFactory(Lucide.ChevronDown, "Sub", 12, strokeWidth: 2.0);
            chev.SetValue(DockPanel.DockProperty, Dock.Right);
            chev.SetValue(FrameworkElement.MarginProperty, new Thickness(8, 0, 0, 0));
            tbDock.AppendChild(chev);

            var selTxt = new FrameworkElementFactory(typeof(TextBlock), "ContentSite");
            selTxt.SetValue(UIElement.IsHitTestVisibleProperty, false);
            selTxt.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("SelectedItem")
            {
                RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.FindAncestor, typeof(ComboBox), 1)
            });
            selTxt.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Fg"));
            selTxt.SetValue(TextBlock.FontSizeProperty, 12.0);
            selTxt.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            selTxt.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            tbDock.AppendChild(selTxt);
            tbBd.AppendChild(tbDock);

            var tbTpl = new ControlTemplate(typeof(ToggleButton)) { VisualTree = tbBd };
            var tbHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            tbHover.Setters.Add(new Setter(Border.BackgroundProperty, Theme.Bind("PillHover"), "ComboBd"));
            tbHover.Setters.Add(new Setter(Border.BorderBrushProperty, Theme.Bind("HoverBorder"), "ComboBd"));
            tbHover.Setters.Add(new Setter(TextBlock.ForegroundProperty, Theme.Bind("HoverFg"), "ContentSite"));
            tbTpl.Triggers.Add(tbHover);
            toggle.SetValue(Control.TemplateProperty, tbTpl);
            grid.AppendChild(toggle);

            var popup = new FrameworkElementFactory(typeof(Popup), "PART_Popup");
            popup.SetValue(Popup.PlacementProperty, PlacementMode.Bottom);
            popup.SetValue(Popup.AllowsTransparencyProperty, true);
            popup.SetValue(Popup.FocusableProperty, false);
            popup.SetValue(Popup.StaysOpenProperty, false);
            popup.SetBinding(Popup.IsOpenProperty, new System.Windows.Data.Binding("IsDropDownOpen")
            {
                RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)
            });

            var popBd = new FrameworkElementFactory(typeof(Border));
            popBd.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            popBd.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            popBd.SetValue(Border.PaddingProperty, new Thickness(4));
            popBd.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 4, 0, 0));
            popBd.SetValue(FrameworkElement.MinWidthProperty, 165.0);
            popBd.SetBinding(Border.BackgroundProperty, Theme.Bind("PopupBg"));
            popBd.SetBinding(Border.BorderBrushProperty, Theme.Bind("CardBorder"));

            var itemsPresenter = new FrameworkElementFactory(typeof(ItemsPresenter));
            popBd.AppendChild(itemsPresenter);
            popup.AppendChild(popBd);
            grid.AppendChild(popup);

            cb.Template = new ControlTemplate(typeof(ComboBox)) { VisualTree = grid };

            var itemBd = new FrameworkElementFactory(typeof(Border), "ItemBd");
            itemBd.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            itemBd.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            itemBd.SetValue(Border.PaddingProperty, new Thickness(10, 6, 10, 6));
            var itemCp = new FrameworkElementFactory(typeof(ContentPresenter));
            itemCp.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            itemBd.AppendChild(itemCp);

            var itemTpl = new ControlTemplate(typeof(ComboBoxItem)) { VisualTree = itemBd };
            var hoverTrig = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverTrig.Setters.Add(new Setter(Border.BackgroundProperty, Theme.Bind("PillHover"), "ItemBd"));
            hoverTrig.Setters.Add(new Setter(Control.ForegroundProperty, Theme.Bind("HoverFg")));
            itemTpl.Triggers.Add(hoverTrig);
            var selTrig = new Trigger { Property = ComboBoxItem.IsSelectedProperty, Value = true };
            selTrig.Setters.Add(new Setter(Border.BackgroundProperty, Theme.Bind("PillHover"), "ItemBd"));
            selTrig.Setters.Add(new Setter(Control.ForegroundProperty, Theme.Bind("Fg")));
            itemTpl.Triggers.Add(selTrig);

            var itemStyle = new Style(typeof(ComboBoxItem));
            itemStyle.Setters.Add(new Setter(Control.ForegroundProperty, Theme.Bind("Fg")));
            itemStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            itemStyle.Setters.Add(new Setter(FrameworkElement.CursorProperty, Cursors.Hand));
            itemStyle.Setters.Add(new Setter(Control.TemplateProperty, itemTpl));
            cb.ItemContainerStyle = itemStyle;

            cb.SelectionChanged += (s, e) =>
            {
                if (cb.SelectedItem is string val) onChanged?.Invoke(val);
            };
            return cb;
        }
    }
}
