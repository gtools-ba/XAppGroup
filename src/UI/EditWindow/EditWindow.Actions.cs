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
        void StyleListBox(ListBox list)
        {
            list.SetBinding(Control.BackgroundProperty, Theme.Bind("Card"));
            list.SetBinding(Control.ForegroundProperty, Theme.Bind("Fg"));
            list.SetBinding(Control.BorderBrushProperty, Theme.Bind("CardBorder"));
            list.BorderThickness = new Thickness(1, 1, 1, 0);

            ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Disabled);
            list.Resources.Add(typeof(ScrollBar), Theme.BuildSleekScrollBarStyle());

            var scrollFadeTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
            ScrollBar cachedScrollBar = null;
            scrollFadeTimer.Tick += (s2, e) =>
            {
                scrollFadeTimer.Stop();
                if (cachedScrollBar == null) cachedScrollBar = Theme.FindChild<ScrollBar>(list);
                if (cachedScrollBar != null && !cachedScrollBar.IsMouseOver)
                {
                    var anim = new System.Windows.Media.Animation.DoubleAnimation(0.0, TimeSpan.FromMilliseconds(300));
                    cachedScrollBar.BeginAnimation(UIElement.OpacityProperty, anim);
                }
            };

            list.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler((s2, e) =>
            {
                if (Math.Abs(e.VerticalChange) > 0.05)
                {
                    if (cachedScrollBar == null) cachedScrollBar = Theme.FindChild<ScrollBar>(list);
                    if (cachedScrollBar != null)
                    {
                        cachedScrollBar.BeginAnimation(UIElement.OpacityProperty, null);
                        cachedScrollBar.Opacity = 0.85;
                        scrollFadeTimer.Stop();
                        scrollFadeTimer.Start();
                    }
                }
            }));

            var listCt = new ControlTemplate(typeof(ListBoxItem));
            var listBd = new FrameworkElementFactory(typeof(Border), "ListItemBorder");
            listBd.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            listBd.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            listBd.SetValue(Border.BorderThicknessProperty, new Thickness(2, 0, 0, 0));
            listBd.SetValue(Border.BorderBrushProperty, Brushes.Transparent);
            listBd.SetValue(Border.PaddingProperty, new Thickness(6, 3, 8, 3));
            listBd.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 1, 0, 1));
            var listCp = new FrameworkElementFactory(typeof(ContentPresenter));
            listBd.AppendChild(listCp);
            listCt.VisualTree = listBd;

            var hoverTrig = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverTrig.Setters.Add(new Setter(Border.BackgroundProperty, Theme.Bind("PillHover"), "ListItemBorder"));
            listCt.Triggers.Add(hoverTrig);

            var selTrig = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
            selTrig.Setters.Add(new Setter(Border.BackgroundProperty, Theme.Bind("AmberDim"), "ListItemBorder"));
            selTrig.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")), "ListItemBorder"));
            listCt.Triggers.Add(selTrig);

            var listItemStyle = new Style(typeof(ListBoxItem));
            listItemStyle.Setters.Add(new Setter(Control.TemplateProperty, listCt));
            listItemStyle.Setters.Add(new Setter(FrameworkElement.CursorProperty, Cursors.Hand));
            list.ItemContainerStyle = listItemStyle;
        }

        static void StyleComboBox(ComboBox cb, double minWidth = 100)
        {
            cb.Height = 32;
            cb.Cursor = Cursors.Hand;
            cb.FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI");

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
            popup.SetValue(Popup.PlacementProperty, PlacementMode.Bottom);
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
            dropBd.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 4, 0, 0));
            dropBd.SetValue(Border.PaddingProperty, new Thickness(4));
            dropBd.SetValue(FrameworkElement.MinWidthProperty, minWidth);

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
        }

        static DataTemplate ItemTpl()
        {
            var dock = new FrameworkElementFactory(typeof(DockPanel));
            dock.SetValue(FrameworkElement.MarginProperty, new Thickness(2, 3, 2, 3));

            var grip = new FrameworkElementFactory(typeof(TextBlock));
            grip.SetValue(TextBlock.TextProperty, "⠿");
            grip.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Muted"));
            grip.SetValue(TextBlock.FontSizeProperty, 11.0);
            grip.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            grip.SetValue(FrameworkElement.MarginProperty, new Thickness(4, 0, 8, 0));
            dock.AppendChild(grip);

            var iconBox = new FrameworkElementFactory(typeof(Border));
            iconBox.SetValue(Border.CornerRadiusProperty, new CornerRadius(7));
            iconBox.SetBinding(Border.BackgroundProperty, Theme.Bind("PillBg"));
            iconBox.SetBinding(Border.BorderBrushProperty, Theme.Bind("PillBorder"));
            iconBox.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            iconBox.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 10, 0));
            iconBox.SetValue(Border.PaddingProperty, new Thickness(3));

            var img = new FrameworkElementFactory(typeof(Image));
            img.SetBinding(Image.SourceProperty, new System.Windows.Data.Binding { Converter = new IconConv() });
            img.SetValue(FrameworkElement.WidthProperty, 32.0);
            img.SetValue(FrameworkElement.HeightProperty, 32.0);
            img.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);
            iconBox.AppendChild(img);
            dock.AppendChild(iconBox);

            var textStack = new FrameworkElementFactory(typeof(StackPanel));
            textStack.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

            var nameTxt = new FrameworkElementFactory(typeof(TextBlock));
            nameTxt.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding { Converter = new NameConv() });
            nameTxt.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Fg"));
            nameTxt.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            nameTxt.SetValue(TextBlock.FontSizeProperty, 12.5);

            var pathTxt = new FrameworkElementFactory(typeof(TextBlock));
            pathTxt.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding());
            pathTxt.SetBinding(TextBlock.ToolTipProperty, new System.Windows.Data.Binding());
            pathTxt.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Muted"));
            pathTxt.SetValue(TextBlock.FontSizeProperty, 10.5);
            pathTxt.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
            pathTxt.SetValue(FrameworkElement.MaxWidthProperty, 260.0);

            textStack.AppendChild(nameTxt);
            textStack.AppendChild(pathTxt);
            dock.AppendChild(textStack);

            return new DataTemplate { VisualTree = dock };
        }

        class IconConv : System.Windows.Data.IValueConverter
        {
            public object Convert(object v, Type t, object p, System.Globalization.CultureInfo c) => Icons.Get((string)v);
            public object ConvertBack(object v, Type t, object p, System.Globalization.CultureInfo c) => throw new NotSupportedException();
        }

        class NameConv : System.Windows.Data.IValueConverter
        {
            public object Convert(object v, Type t, object p, System.Globalization.CultureInfo c) => IOPath.GetFileNameWithoutExtension((string)v);
            public object ConvertBack(object v, Type t, object p, System.Globalization.CultureInfo c) => throw new NotSupportedException();
        }

        void Add()
        {
            var dlg = new OpenFileDialog { Multiselect = true, Filter = "Apps and shortcuts|*.exe;*.lnk;*.url;*.bat|All files|*.*" };
            if (dlg.ShowDialog(this) == true)
                foreach (var f in dlg.FileNames) if (!_items.Contains(f)) _items.Add(f);
        }

        void SelectIcon()
        {
            var dlg = new OpenFileDialog { Filter = "Icons and apps|*.ico;*.png;*.exe;*.dll|All files|*.*" };
            if (dlg.ShowDialog(this) != true || !File.Exists(dlg.FileName)) return;
            if (Icons.FromFile(dlg.FileName) == null) { ThemedDialog.Show(this, "That file has no usable icon.", "Icon", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            _icon = dlg.FileName; UpdatePreview();
        }

        void UpdateCountBadge()
        {
            _countText.Text = $"{_items.Count} items";
        }

        void UpdatePreview()
        {
            if (!string.IsNullOrWhiteSpace(_icon) && File.Exists(_icon))
            {
                _prev.Source = Icons.FromFile(_icon);
            }
            else
            {
                _prev.Source = Icons.CreateCompositeBitmap(_items);
            }
            bool isCustom = !string.IsNullOrWhiteSpace(_icon) && File.Exists(_icon);
            _iconTitle.Text = isCustom ? IOPath.GetFileName(_icon) : "Automatic 2×2 composite";
            _iconSubtitle.Text = isCustom ? "Custom group icon file" : "Dynamically generates icon preview from first 4 items";
            _autoBadge.Visibility = isCustom ? Visibility.Collapsed : Visibility.Visible;
            _iconText.Text = isCustom ? "Custom icon: " + IOPath.GetFileName(_icon) : "Group icon: automatic 2×2 composite";
        }

        void Move(int d)
        {
            int i = _list.SelectedIndex, j = i + d;
            if (i < 0 || j < 0 || j >= _items.Count) return;
            _items.Move(i, j); _list.SelectedIndex = j;
        }

        void Save()
        {
            if (string.IsNullOrWhiteSpace(_name.Text)) { ThemedDialog.Show(this, "Please enter a group name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            string oldName = _g.GroupName;
            _g.GroupName = _name.Text.Trim();
            _g.GroupCol = (int)_cols.SelectedItem;
            _g.SortMode = (string)_sort.SelectedItem;
            _g.ShowLabels = _labels.IsChecked == true;
            _g.Hotkey = _hotkeyValue ?? "";
            _g.Paths = _items.ToList();
            _g.IconPath = _icon ?? "";
            _g.InvalidateIcons();
            ShortcutHelper.GetValidShortcutIconLocation(_g);
            TaskbarHelper.SyncPinnedShortcut(_g, oldName);
            JumpListHelper.Update(_g);
            DialogResult = true;
        }
    }
}
