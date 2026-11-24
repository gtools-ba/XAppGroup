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
    public partial class EditWindow : Window
    {
        readonly GroupData _g;
        readonly ObservableCollection<string> _items;
        readonly TextBox _name = new TextBox
        {
            Margin = new Thickness(0, 6, 0, 14),
            Padding = new Thickness(12, 9, 12, 9),
            FontSize = 13,
            FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI")
        };
        readonly ComboBox _cols = new ComboBox();
        readonly ComboBox _sort = new ComboBox();
        readonly CheckBox _labels = new CheckBox { Content = "Show app labels in popup", Cursor = Cursors.Hand };
        readonly TextBox _hotkeyBox = new TextBox
        {
            IsReadOnly = true,
            Cursor = Cursors.Hand,
            Width = 155,
            Padding = new Thickness(10, 5, 10, 5),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            ToolTip = "Click and press a key combination (e.g. Ctrl + Alt + 1)"
        };
        string _hotkeyValue = "";
        readonly ListBox _list = new ListBox
        {
            AllowDrop = true, Height = 220,
            Padding = new Thickness(4)
        };
        readonly Image _prev = new Image { Width = 32, Height = 32 };
        readonly TextBlock _iconText = new TextBlock { VerticalAlignment = VerticalAlignment.Center, FontSize = 12 };
        readonly TextBlock _iconTitle = new TextBlock { FontWeight = FontWeights.SemiBold, FontSize = 13 };
        readonly TextBlock _iconSubtitle = new TextBlock { FontSize = 11 };
        readonly Border _autoBadge = new Border
        {
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(7, 1.5, 7, 1.5),
            Margin = new Thickness(8, 0, 0, 0)
        };
        readonly TextBlock _countText = new TextBlock { FontSize = 11, FontWeight = FontWeights.SemiBold };
        Button _addBtn;
        Button _removeBtn;
        Button _upBtn;
        Button _downBtn;
        Button _iconBtn;
        Button _resetBtn;
        Button _saveBtn;
        Button _cancelBtn;
        string _icon;

        public EditWindow(GroupData g, bool applicationsFocus = false)
        {
            _g = g;
            _icon = g.IconPath ?? "";
            _items = new ObservableCollection<string>(g.Paths);
            _items.CollectionChanged += (s, e) =>
            {
                UpdateCountBadge();
                if (string.IsNullOrWhiteSpace(_icon))
                {
                    UpdatePreview();
                }
            };

            Title = "Edit Group";
            Width = 560;
            SizeToContent = SizeToContent.Height;
            SetBinding(Window.BackgroundProperty, Theme.Bind("Bg"));
            SetBinding(Window.ForegroundProperty, Theme.Bind("Fg"));
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI");
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;

            try
            {
                var icoPath = IOPath.Combine(AppContext.BaseDirectory, "XAppGroup.ico");
                if (File.Exists(icoPath)) Icon = new BitmapImage(new Uri(icoPath, UriKind.Absolute));
            }
            catch { }
            SourceInitialized += (s, e) => JumpListHelper.SetWindowAppId(new System.Windows.Interop.WindowInteropHelper(this).Handle, "XAppGroup.Manager");

            // --- Section 1: Group Name ---
            var nameHeader = new DockPanel();
            var nameLbl = new TextBlock { Text = "GROUP NAME", FontSize = 11, FontWeight = FontWeights.SemiBold };
            nameLbl.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Sub"));
            var nameHint = new TextBlock { Text = "Max 32 chars", FontSize = 11 };
            nameHint.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Muted"));
            DockPanel.SetDock(nameHint, Dock.Right);
            nameHeader.Children.Add(nameHint);
            nameHeader.Children.Add(nameLbl);

            _name.Text = g.GroupName;
            _name.SetBinding(Control.BackgroundProperty, Theme.Bind("Card"));
            _name.SetBinding(Control.ForegroundProperty, Theme.Bind("Fg"));
            _name.CaretBrush = Theme.Cyan;
            _name.SetBinding(Control.BorderBrushProperty, Theme.Bind("CardBorder"));
            _name.BorderThickness = new Thickness(1);

            // --- Section 2: Group Icon ---
            var iconHeader = new TextBlock { Text = "GROUP ICON", FontSize = 11, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) };
            iconHeader.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Sub"));

            var previewSquircle = new Border
            {
                Width = 48, Height = 48, CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 12, 0), Padding = new Thickness(7),
                Child = _prev
            };
            previewSquircle.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("SquircleBg") { Source = _g });
            previewSquircle.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("SquircleBorder") { Source = _g });
            RenderOptions.SetBitmapScalingMode(_prev, BitmapScalingMode.HighQuality);

            _autoBadge.SetBinding(Border.BackgroundProperty, Theme.Bind("CyanBadgeBg"));
            _autoBadge.SetBinding(Border.BorderBrushProperty, Theme.Bind("CyanBadgeBorder"));
            _autoBadge.BorderThickness = new Thickness(1);
            var autoTxt = new TextBlock { Text = "Auto", FontSize = 10, FontWeight = FontWeights.Bold };
            autoTxt.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("CyanBadgeFg"));
            _autoBadge.Child = autoTxt;

            _iconTitle.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Fg"));
            _iconSubtitle.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Sub"));
            _iconText.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Sub"));

            var iconTitleRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            iconTitleRow.Children.Add(_iconTitle);
            iconTitleRow.Children.Add(_autoBadge);

            var iconInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            iconInfo.Children.Add(iconTitleRow);
            iconInfo.Children.Add(_iconSubtitle);

            var iconLeft = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            iconLeft.Children.Add(previewSquircle);
            iconLeft.Children.Add(iconInfo);

            _iconBtn = Theme.Btn("_Icon...", (s, e) => SelectIcon(), iconPath: Lucide.Image);
            _iconBtn.Margin = new Thickness(0, 0, 6, 0);
            _resetBtn = Theme.Btn("R_eset icon", (s, e) =>
            {
                _icon = "";
                _g.IconPath = "";
                _g.InvalidateIcons();
                UpdatePreview();
            }, iconPath: Lucide.RotateCcw);

            var iconBtns = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            iconBtns.Children.Add(_iconBtn);
            iconBtns.Children.Add(_resetBtn);

            var iconCardDock = new DockPanel();
            DockPanel.SetDock(iconBtns, Dock.Right);
            iconCardDock.Children.Add(iconBtns);
            iconCardDock.Children.Add(iconLeft);

            var iconCard = new Border
            {
                CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 14),
                Child = iconCardDock
            };
            iconCard.SetBinding(Border.BackgroundProperty, Theme.Bind("Card"));
            iconCard.SetBinding(Border.BorderBrushProperty, Theme.Bind("CardBorder"));

            // --- Section 3: Applications Manager ---
            var appsHeader = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
            var appsHeaderLeft = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var appsTitle = new TextBlock
            {
                Text = applicationsFocus ? "APPLICATIONS — ADD OR MANAGE" : "APPLICATIONS (DRAG & DROP FILES HERE)",
                FontSize = 11, FontWeight = FontWeights.SemiBold
            };
            appsTitle.Foreground = applicationsFocus ? Theme.Amber : Theme.Sub;
            appsHeaderLeft.Children.Add(appsTitle);

            var countBadge = new Border
            {
                CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(7, 1.5, 7, 1.5),
                Margin = new Thickness(8, 0, 0, 0),
                Child = _countText
            };
            countBadge.SetBinding(Border.BackgroundProperty, Theme.Bind("PillBg"));
            countBadge.SetBinding(Border.BorderBrushProperty, Theme.Bind("PillBorder"));
            _countText.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Sub"));
            appsHeaderLeft.Children.Add(countBadge);

            var appsHint = new TextBlock { Text = "Reorder by dragging rows", FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
            appsHint.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Muted"));
            DockPanel.SetDock(appsHint, Dock.Right);
            appsHeader.Children.Add(appsHint);
            appsHeader.Children.Add(appsHeaderLeft);

            // List drop hint
            var dropHint = new Border
            {
                BorderThickness = new Thickness(1, 0, 1, 1),
                CornerRadius = new CornerRadius(0, 0, 8, 8),
                Padding = new Thickness(8, 6, 8, 6)
            };
            dropHint.SetBinding(Border.BackgroundProperty, Theme.Bind("PillBg"));
            dropHint.SetBinding(Border.BorderBrushProperty, Theme.Bind("CardBorder"));
            var dropStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            var dropIcon = Lucide.Make(Lucide.FolderPlus, Theme.Amber, 13);
            dropStack.Children.Add(dropIcon);
            var dropText = new TextBlock
            {
                Text = "Drop executable shortcuts (.lnk, .exe, .url) here",
                FontSize = 10.5,
                Margin = new Thickness(6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            dropText.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Muted"));
            dropStack.Children.Add(dropText);
            dropHint.Child = dropStack;

            void HandleFileDrop(DragEventArgs e)
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
                    {
                        foreach (var f in files)
                        {
                            if (!string.IsNullOrWhiteSpace(f) && !_items.Contains(f))
                            {
                                _items.Add(f);
                            }
                        }
                    }
                    e.Handled = true;
                }
            }

            void HandleDragOver(DragEventArgs e)
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effects = DragDropEffects.Copy;
                    e.Handled = true;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }

            StyleListBox(_list);
            _list.ItemsSource = _items;
            _list.ItemTemplate = ItemTpl();
            _list.AllowDrop = true;
            _list.PreviewDragOver += (s, e) => HandleDragOver(e);
            _list.DragOver += (s, e) => HandleDragOver(e);
            _list.Drop += (s, e) => HandleFileDrop(e);
            AutomationProperties.SetName(_list, "Applications");
            AutomationProperties.SetAutomationId(_list, "EditApplicationsList");

            dropHint.AllowDrop = true;
            dropHint.PreviewDragOver += (s, e) => HandleDragOver(e);
            dropHint.DragOver += (s, e) => HandleDragOver(e);
            dropHint.DragEnter += (s, e) => dropHint.Background = Theme.PillHover;
            dropHint.DragLeave += (s, e) => dropHint.SetBinding(Border.BackgroundProperty, Theme.Bind("PillBg"));
            dropHint.Drop += (s, e) => { dropHint.SetBinding(Border.BackgroundProperty, Theme.Bind("PillBg")); HandleFileDrop(e); };
            dropHint.MouseLeftButtonUp += (s, e) => Add();

            AllowDrop = true;
            PreviewDragOver += (s, e) => HandleDragOver(e);
            DragOver += (s, e) => HandleDragOver(e);
            Drop += (s, e) => HandleFileDrop(e);

            var listColumn = new DockPanel();
            DockPanel.SetDock(dropHint, Dock.Bottom);
            listColumn.Children.Add(dropHint);
            listColumn.Children.Add(_list);

            var side = new StackPanel { Margin = new Thickness(10, 0, 0, 0), Width = 118 };
            _addBtn = Theme.Btn("_Add...", (s, e) => Add(), primary: true, iconPath: Lucide.Plus);
            AutomationProperties.SetName(_addBtn, "Add applications");
            AutomationProperties.SetAutomationId(_addBtn, "EditAddAppBtn");
            side.Children.Add(_addBtn);

            _removeBtn = Theme.Btn("_Remove", (s, e) => { if (_list.SelectedItem is string p) _items.Remove(p); }, variant: "danger", iconPath: Lucide.Trash2);
            side.Children.Add(_removeBtn);

            var sideDiv = new Border { Height = 1, Margin = new Thickness(0, 3, 0, 3) };
            sideDiv.SetBinding(Border.BackgroundProperty, Theme.Bind("Divider"));
            side.Children.Add(sideDiv);

            _upBtn = Theme.Btn("_Up", (s, e) => Move(-1), iconPath: Lucide.ChevronUp);
            _downBtn = Theme.Btn("Do_wn", (s, e) => Move(1), iconPath: Lucide.ChevronDown);
            side.Children.Add(_upBtn);
            side.Children.Add(_downBtn);
            foreach (UIElement child in side.Children)
            {
                if (child is Button b) b.Margin = new Thickness(0, 0, 0, 6);
            }

            var appsRow = new DockPanel { Margin = new Thickness(0, 0, 0, 14) };
            DockPanel.SetDock(side, Dock.Right);
            appsRow.Children.Add(side);
            appsRow.Children.Add(listColumn);

            // --- Section 4: Layout & Sorting (2-column cards) ---
            for (int i = 1; i <= 8; i++) _cols.Items.Add(i);
            _cols.SelectedItem = Math.Clamp(g.GroupCol, 1, 8);
            _sort.Items.Add("Manual"); _sort.Items.Add("Alphabetical"); _sort.SelectedItem = g.SortMode;
            StyleComboBox(_cols, 80);
            StyleComboBox(_sort, 140);

            var colsCard = new Border
            {
                CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 5, 0)
            };
            colsCard.SetBinding(Border.BackgroundProperty, Theme.Bind("Card"));
            colsCard.SetBinding(Border.BorderBrushProperty, Theme.Bind("CardBorder"));
            var colsCardStack = new StackPanel();
            var colsHead = new DockPanel();
            var colsLbl = new TextBlock { Text = "Grid Columns", FontSize = 12, FontWeight = FontWeights.SemiBold };
            colsLbl.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Fg"));
            var colsSub = new TextBlock { Text = "Popup layout", FontSize = 11 };
            colsSub.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Muted"));
            DockPanel.SetDock(colsSub, Dock.Right);
            colsHead.Children.Add(colsSub);
            colsHead.Children.Add(colsLbl);
            _cols.Margin = new Thickness(0, 6, 0, 0);
            colsCardStack.Children.Add(colsHead);
            colsCardStack.Children.Add(_cols);
            colsCard.Child = colsCardStack;

            var sortCard = new Border
            {
                CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(5, 0, 0, 0)
            };
            sortCard.SetBinding(Border.BackgroundProperty, Theme.Bind("Card"));
            sortCard.SetBinding(Border.BorderBrushProperty, Theme.Bind("CardBorder"));
            var sortCardStack = new StackPanel();
            var sortHead = new DockPanel();
            var sortLbl = new TextBlock { Text = "Sort Order", FontSize = 12, FontWeight = FontWeights.SemiBold };
            sortLbl.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Fg"));
            var sortSub = new TextBlock { Text = "Item sequence", FontSize = 11 };
            sortSub.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Muted"));
            DockPanel.SetDock(sortSub, Dock.Right);
            sortHead.Children.Add(sortSub);
            sortHead.Children.Add(sortLbl);
            _sort.Margin = new Thickness(0, 6, 0, 0);
            sortCardStack.Children.Add(sortHead);
            sortCardStack.Children.Add(_sort);
            sortCard.Child = sortCardStack;

            var colsSortGrid = new Grid { Margin = new Thickness(0, 0, 0, 14) };
            colsSortGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            colsSortGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(colsCard, 0);
            Grid.SetColumn(sortCard, 1);
            colsSortGrid.Children.Add(colsCard);
            colsSortGrid.Children.Add(sortCard);

            // --- Section 5: Presentation & Hotkey Options ---
            _labels.IsChecked = g.ShowLabels;
            _labels.SetBinding(Control.ForegroundProperty, Theme.Bind("Fg"));
            _labels.FontWeight = FontWeights.SemiBold;
            _labels.FontSize = 12.5;

            var optionsStack = new StackPanel();

            var labelPanel = new StackPanel();
            labelPanel.Children.Add(_labels);
            var labelSub = new TextBlock
            {
                Text = "Displays text caption beneath each application shortcut",
                FontSize = 11,
                Margin = new Thickness(22, 2, 0, 0)
            };
            labelSub.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Muted"));
            labelPanel.Children.Add(labelSub);
            optionsStack.Children.Add(labelPanel);

            var div = new Border { Height = 1, Margin = new Thickness(0, 10, 0, 10) };
            div.SetBinding(Border.BackgroundProperty, Theme.Bind("Divider"));
            optionsStack.Children.Add(div);

            _hotkeyValue = g.Hotkey ?? "";
            _hotkeyBox.SetBinding(Control.ForegroundProperty, Theme.Bind("Fg"));
            void RefreshHotkeyDisplay()
            {
                _hotkeyBox.Text = string.IsNullOrWhiteSpace(_hotkeyValue) ? "Click to set..." : _hotkeyValue;
            }
            RefreshHotkeyDisplay();

            var hotkeyBorder = new Border
            {
                CornerRadius = new CornerRadius(7),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                Child = _hotkeyBox
            };
            hotkeyBorder.SetBinding(Border.BackgroundProperty, Theme.Bind("PillBg"));
            hotkeyBorder.SetBinding(Border.BorderBrushProperty, Theme.Bind("PillBorder"));

            _hotkeyBox.GotKeyboardFocus += (s, e) =>
            {
                hotkeyBorder.SetBinding(Border.BorderBrushProperty, Theme.Bind("Accent"));
                if (string.IsNullOrWhiteSpace(_hotkeyValue)) _hotkeyBox.Text = "Press keys...";
            };
            _hotkeyBox.LostKeyboardFocus += (s, e) =>
            {
                hotkeyBorder.SetBinding(Border.BorderBrushProperty, Theme.Bind("PillBorder"));
                RefreshHotkeyDisplay();
            };
            _hotkeyBox.PreviewKeyDown += (s, e) =>
            {
                e.Handled = true;
                var key = e.Key == Key.System ? e.SystemKey : e.Key;
                if (key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt ||
                    key == Key.LeftShift || key == Key.RightShift || key == Key.LWin || key == Key.RWin)
                    return;

                var mods = Keyboard.Modifiers;
                if (mods == ModifierKeys.None && (key == Key.Back || key == Key.Delete || key == Key.Escape))
                {
                    _hotkeyValue = "";
                    RefreshHotkeyDisplay();
                    return;
                }
                if (mods == ModifierKeys.None) return;

                var parts = new List<string>();
                if ((mods & ModifierKeys.Windows) != 0) parts.Add("Win");
                if ((mods & ModifierKeys.Control) != 0) parts.Add("Ctrl");
                if ((mods & ModifierKeys.Alt) != 0) parts.Add("Alt");
                if ((mods & ModifierKeys.Shift) != 0) parts.Add("Shift");

                string keyName = key.ToString();
                if (key >= Key.D0 && key <= Key.D9) keyName = ((int)(key - Key.D0)).ToString();
                else if (key >= Key.NumPad0 && key <= Key.NumPad9) keyName = "Num" + ((int)(key - Key.NumPad0)).ToString();
                parts.Add(keyName);

                _hotkeyValue = string.Join(" + ", parts);
                RefreshHotkeyDisplay();
            };

            var clearHotkeyBtn = Theme.Btn("Clear", (s, e) =>
            {
                _hotkeyValue = "";
                RefreshHotkeyDisplay();
            });
            clearHotkeyBtn.Padding = new Thickness(9, 5, 9, 5);
            clearHotkeyBtn.Margin = new Thickness(6, 0, 0, 0);
            clearHotkeyBtn.VerticalAlignment = VerticalAlignment.Center;

            var hotkeyControls = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            hotkeyControls.Children.Add(hotkeyBorder);
            hotkeyControls.Children.Add(clearHotkeyBtn);
            DockPanel.SetDock(hotkeyControls, Dock.Right);

            var hotkeyTitleRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var zapIc = Lucide.Make(Lucide.Zap, Theme.Cyan, 13);
            zapIc.Margin = new Thickness(0, 0, 6, 0);
            hotkeyTitleRow.Children.Add(zapIc);
            var hotkeyTitle = new TextBlock
            {
                Text = "Configure Hotkey",
                FontWeight = FontWeights.SemiBold,
                FontSize = 12.5,
                VerticalAlignment = VerticalAlignment.Center
            };
            hotkeyTitle.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Fg"));
            hotkeyTitleRow.Children.Add(hotkeyTitle);

            var hotkeySub = new TextBlock
            {
                Text = "Assign a keyboard shortcut for this group",
                FontSize = 11,
                Margin = new Thickness(19, 2, 0, 0)
            };
            hotkeySub.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Muted"));

            var hotkeyInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            hotkeyInfo.Children.Add(hotkeyTitleRow);
            hotkeyInfo.Children.Add(hotkeySub);

            var hotkeyDock = new DockPanel { LastChildFill = true };
            hotkeyDock.Children.Add(hotkeyControls);
            hotkeyDock.Children.Add(hotkeyInfo);
            optionsStack.Children.Add(hotkeyDock);

            var optionsCard = new Border
            {
                CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 4),
                Child = optionsStack
            };
            optionsCard.SetBinding(Border.BackgroundProperty, Theme.Bind("Card"));
            optionsCard.SetBinding(Border.BorderBrushProperty, Theme.Bind("CardBorder"));

            // --- Section 6: Footer Actions ---
            var footerDock = new DockPanel();

            var statusStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var statusDot = new Border
            {
                Width = 8, Height = 8, CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 8, 0)
            };
            statusDot.SetBinding(Border.BackgroundProperty, Theme.Bind("EmeraldFg"));
            statusStack.Children.Add(statusDot);
            var statusText = new TextBlock
            {
                Text = "XAppGroup Daemon Ready",
                FontSize = 11.5,
                VerticalAlignment = VerticalAlignment.Center
            };
            statusText.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Sub"));
            statusStack.Children.Add(statusText);
            footerDock.Children.Add(statusStack);

            _cancelBtn = Theme.Btn("_Cancel", (s, e) => { DialogResult = false; });
            _cancelBtn.Margin = new Thickness(0, 0, 8, 0);
            _saveBtn = Theme.Btn("_Save", (s, e) => Save(), primary: true, iconPath: Lucide.Check);

            var footerBtns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            footerBtns.Children.Add(_cancelBtn);
            footerBtns.Children.Add(_saveBtn);
            DockPanel.SetDock(footerBtns, Dock.Right);
            footerDock.Children.Add(footerBtns);

            var footerBorder = new Border
            {
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(20, 12, 20, 12),
                Child = footerDock
            };
            footerBorder.SetBinding(Border.BackgroundProperty, Theme.Bind("Panel"));
            footerBorder.SetBinding(Border.BorderBrushProperty, Theme.Bind("Divider"));

            // Assemble main dialog
            var mainContent = new StackPanel { Margin = new Thickness(22, 18, 22, 14) };
            mainContent.Children.Add(nameHeader);
            mainContent.Children.Add(_name);
            mainContent.Children.Add(iconHeader);
            mainContent.Children.Add(iconCard);
            mainContent.Children.Add(appsHeader);
            mainContent.Children.Add(appsRow);
            mainContent.Children.Add(colsSortGrid);
            mainContent.Children.Add(optionsCard);

            var rootDock = new DockPanel();
            DockPanel.SetDock(footerBorder, Dock.Bottom);
            rootDock.Children.Add(footerBorder);
            rootDock.Children.Add(mainContent);

            Content = rootDock;

            UpdatePreview();
            UpdateCountBadge();

            if (applicationsFocus)
            {
                Title = "Edit Group — Applications";
                Loaded += (s, e) => Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_items.Count > 0)
                    {
                        _list.SelectedIndex = 0;
                        _list.ScrollIntoView(_items[0]);
                    }
                    _addBtn.Focus();
                    Keyboard.Focus(_addBtn);
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
        }
    }
}
