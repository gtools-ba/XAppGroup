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
        Style BuildCardContainerStyle()
        {
            var st = new Style(typeof(ListBoxItem));
            st.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
            st.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 16)));
            st.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
            st.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            st.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
            st.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty, null));

            var cardBorder = new FrameworkElementFactory(typeof(Border), "CardOuter");
            cardBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(12));
            cardBorder.SetBinding(Border.BackgroundProperty, Theme.Bind("Card"));
            cardBorder.SetBinding(Border.BorderBrushProperty, Theme.Bind("CardBorder"));
            cardBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            cardBorder.SetValue(Border.PaddingProperty, new Thickness(20, 18, 20, 16));

            var grid = new FrameworkElementFactory(typeof(Grid));
            var accentBar = new FrameworkElementFactory(typeof(Border), "ActiveBar");
            accentBar.SetValue(FrameworkElement.WidthProperty, 5.0);
            accentBar.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            accentBar.SetValue(FrameworkElement.MarginProperty, new Thickness(-20, -6, 0, -4));
            accentBar.SetValue(Border.CornerRadiusProperty, new CornerRadius(0, 3, 3, 0));
            accentBar.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
            accentBar.SetValue(Border.BackgroundProperty, new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#FBBF24"),
                (Color)ColorConverter.ConvertFromString("#D97706"),
                new Point(0, 0), new Point(0, 1)));
            accentBar.SetValue(UIElement.EffectProperty, new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#F59E0B"),
                BlurRadius = 12,
                Opacity = 0.65,
                ShadowDepth = 0
            });
            grid.AppendChild(accentBar);

            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            grid.AppendChild(cp);
            cardBorder.AppendChild(grid);

            var ct = new ControlTemplate(typeof(ListBoxItem)) { VisualTree = cardBorder };

            var hoverTrig = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverTrig.Setters.Add(new Setter(Border.BackgroundProperty, Theme.Bind("CardHover"), "CardOuter"));
            hoverTrig.Setters.Add(new Setter(Border.BorderBrushProperty, Theme.Bind("HoverBorder"), "CardOuter"));
            ct.Triggers.Add(hoverTrig);

            var selTrig = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
            selTrig.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")), "CardOuter"));
            selTrig.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Visible, "ActiveBar"));
            ct.Triggers.Add(selTrig);

            st.Setters.Add(new Setter(Control.TemplateProperty, ct));
            return st;
        }

        static ControlTemplate ChromeTemplate(FrameworkElementFactory content, double corner)
        {
            var bd = new FrameworkElementFactory(typeof(Border));
            bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(corner));
            bd.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            bd.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
            bd.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
            bd.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));
            bd.AppendChild(content);
            var ct = new ControlTemplate(typeof(Button)) { VisualTree = bd };
            var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(UIElement.OpacityProperty, 0.86));
            ct.Triggers.Add(hover);
            var pressed = new Trigger { Property = ButtonBase.IsPressedProperty, Value = true };
            pressed.Setters.Add(new Setter(UIElement.OpacityProperty, 0.7));
            ct.Triggers.Add(pressed);
            return ct;
        }

        static ControlTemplate DashedPillTemplate(string text)
        {
            var g = new FrameworkElementFactory(typeof(Grid));
            var rect = new FrameworkElementFactory(typeof(Rectangle));
            rect.SetValue(Rectangle.RadiusXProperty, 8.0);
            rect.SetValue(Rectangle.RadiusYProperty, 8.0);
            rect.SetBinding(Shape.StrokeProperty, Theme.Bind("PillBorder"));
            rect.SetValue(Shape.StrokeThicknessProperty, 1.0);
            rect.SetValue(Shape.StrokeDashArrayProperty, new DoubleCollection { 3.0, 2.0 });
            rect.SetValue(Shape.FillProperty, Brushes.Transparent);
            g.AppendChild(rect);

            var inner = new FrameworkElementFactory(typeof(StackPanel));
            inner.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            inner.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            inner.SetValue(FrameworkElement.MarginProperty, new Thickness(10, 5, 10, 5));
            var plus = Lucide.MakeFactory(Lucide.Plus, "Sub", 11.5);
            plus.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 5, 0));
            inner.AppendChild(plus);
            var txt = new FrameworkElementFactory(typeof(TextBlock));
            txt.SetValue(TextBlock.TextProperty, text);
            txt.SetValue(TextBlock.FontSizeProperty, 12.0);
            txt.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            txt.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Sub"));
            inner.AppendChild(txt);
            g.AppendChild(inner);

            var ct = new ControlTemplate(typeof(Button)) { VisualTree = g };
            var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(UIElement.OpacityProperty, 0.86));
            ct.Triggers.Add(hover);
            var pressed = new Trigger { Property = ButtonBase.IsPressedProperty, Value = true };
            pressed.Setters.Add(new Setter(UIElement.OpacityProperty, 0.7));
            ct.Triggers.Add(pressed);
            return ct;
        }

        DataTemplate ItemTemplate()
        {
            var cardStack = new FrameworkElementFactory(typeof(StackPanel));

            // Top Row Grid: Col 0 = 48x48 Icon Squircle, Col 1 = Title/Badges/Summary (*), Col 2 = Action Buttons (Auto)
            var topGrid = new FrameworkElementFactory(typeof(Grid));
            var col0 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col0.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
            var col1 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col1.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            var col2 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col2.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
            topGrid.AppendChild(col0);
            topGrid.AppendChild(col1);
            topGrid.AppendChild(col2);

            // Col 0: 48x48 group icon. A real Button: click opens the launcher, drag pins (see list routing).
            var iconBtn = new FrameworkElementFactory(typeof(Button));
            iconBtn.SetValue(Grid.ColumnProperty, 0);
            iconBtn.SetValue(FrameworkElement.WidthProperty, 48.0);
            iconBtn.SetValue(FrameworkElement.HeightProperty, 48.0);
            iconBtn.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 16, 0));
            iconBtn.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Top);
            iconBtn.SetValue(FrameworkElement.CursorProperty, Cursors.Hand);
            iconBtn.SetValue(Control.PaddingProperty, new Thickness(0));
            iconBtn.SetValue(Control.BorderThicknessProperty, new Thickness(0));
            iconBtn.SetValue(Control.MinWidthProperty, 0.0);
            iconBtn.SetValue(Control.MinHeightProperty, 0.0);
            iconBtn.SetValue(Control.BackgroundProperty, Brushes.Transparent);
            iconBtn.SetValue(FrameworkElement.ToolTipProperty, "Open launcher. Drag to pin to the taskbar or desktop.");
            iconBtn.SetValue(FrameworkElement.TagProperty, "GroupIconHandle");
            iconBtn.SetValue(AutomationProperties.NameProperty, new System.Windows.Data.Binding("GroupName")
            {
                StringFormat = "Open launcher for {0}. Drag to pin."
            });

            var iconFace = new FrameworkElementFactory(typeof(Border));
            iconFace.SetValue(Border.CornerRadiusProperty, new CornerRadius(12));
            iconFace.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("SquircleBg"));
            iconFace.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("SquircleBorder"));
            iconFace.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            iconFace.SetValue(Border.PaddingProperty, new Thickness(7));
            var img = new FrameworkElementFactory(typeof(Image));
            img.SetBinding(Image.SourceProperty, new System.Windows.Data.Binding("IconSource"));
            img.SetValue(FrameworkElement.WidthProperty, 32.0);
            img.SetValue(FrameworkElement.HeightProperty, 32.0);
            iconFace.AppendChild(img);
            var iconTemplate = new ControlTemplate(typeof(Button)) { VisualTree = iconFace };
            var iconHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            iconHover.Setters.Add(new Setter(UIElement.OpacityProperty, 0.86));
            iconTemplate.Triggers.Add(iconHover);
            var iconPressed = new Trigger { Property = ButtonBase.IsPressedProperty, Value = true };
            iconPressed.Setters.Add(new Setter(UIElement.OpacityProperty, 0.7));
            iconTemplate.Triggers.Add(iconPressed);
            iconBtn.SetValue(Control.TemplateProperty, iconTemplate);
            topGrid.AppendChild(iconBtn);

            // Col 1: Group Title + App Count Badge + Hotkey Badge + Summary Subtitle
            var tx = new FrameworkElementFactory(typeof(StackPanel));
            tx.SetValue(Grid.ColumnProperty, 1);
            tx.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            tx.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 16, 0));

            var titleRow = new FrameworkElementFactory(typeof(WrapPanel));
            titleRow.SetValue(WrapPanel.OrientationProperty, Orientation.Horizontal);
            titleRow.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

            var n = new FrameworkElementFactory(typeof(TextBlock));
            n.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("GroupName"));
            n.SetValue(TextBlock.FontSizeProperty, 16.0);
            n.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            n.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            n.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Fg"));
            titleRow.AppendChild(n);

            var countBadge = new FrameworkElementFactory(typeof(Border));
            countBadge.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            countBadge.SetValue(Border.PaddingProperty, new Thickness(8, 2, 8, 2));
            countBadge.SetValue(FrameworkElement.MarginProperty, new Thickness(10, 0, 0, 0));
            countBadge.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            countBadge.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("BadgeBg"));
            countBadge.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BadgeBorder"));
            countBadge.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            var c = new FrameworkElementFactory(typeof(TextBlock));
            c.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("AppCountBadge"));
            c.SetValue(TextBlock.FontSizeProperty, 11.0);
            c.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            c.SetBinding(TextBlock.ForegroundProperty, new System.Windows.Data.Binding("BadgeFg"));
            countBadge.AppendChild(c);
            titleRow.AppendChild(countBadge);

            var hotkeyBadge = new FrameworkElementFactory(typeof(Border));
            hotkeyBadge.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            hotkeyBadge.SetValue(Border.PaddingProperty, new Thickness(8, 2, 8, 2));
            hotkeyBadge.SetValue(FrameworkElement.MarginProperty, new Thickness(8, 0, 0, 0));
            hotkeyBadge.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            hotkeyBadge.SetBinding(Border.BackgroundProperty, Theme.Bind("PillBg"));
            hotkeyBadge.SetBinding(Border.BorderBrushProperty, Theme.Bind("PillBorder"));
            hotkeyBadge.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            var hbTxt = new FrameworkElementFactory(typeof(TextBlock));
            hbTxt.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("HotkeyBadge"));
            hbTxt.SetValue(TextBlock.FontSizeProperty, 11.0);
            hbTxt.SetValue(TextBlock.FontFamilyProperty, new FontFamily("Cascadia Mono, Consolas"));
            hbTxt.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Sub"));
            hotkeyBadge.AppendChild(hbTxt);
            titleRow.AppendChild(hotkeyBadge);

            tx.AppendChild(titleRow);

            var desc = new FrameworkElementFactory(typeof(TextBlock));
            desc.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("SummaryText"));
            desc.SetValue(TextBlock.FontSizeProperty, 12.0);
            desc.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 4, 0, 0));
            desc.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
            desc.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Sub"));
            tx.AppendChild(desc);

            topGrid.AppendChild(tx);

            // Col 2: Top-right action row — primary launch, launcher, more options.
            // Every control is a real Button so UIA exposes Invoke for keyboard and screen readers.
            var actions = new FrameworkElementFactory(typeof(StackPanel));
            actions.SetValue(Grid.ColumnProperty, 2);
            actions.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            actions.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Top);

            // Active card "Launch All" (Solid warm amber background, black text and play icon).
            // It shares a fixed-size cell with the inactive "Launch" button so selection never changes row geometry.
            var activeLaunchBtn = new FrameworkElementFactory(typeof(Button), "ActiveLaunchBtn");
            activeLaunchBtn.SetValue(Control.PaddingProperty, new Thickness(14, 6, 14, 6));
            activeLaunchBtn.SetValue(Control.BorderThicknessProperty, new Thickness(1));
            activeLaunchBtn.SetValue(Control.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")));
            activeLaunchBtn.SetValue(Control.MinWidthProperty, 0.0);
            activeLaunchBtn.SetValue(Control.MinHeightProperty, 0.0);
            activeLaunchBtn.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right);
            activeLaunchBtn.SetValue(Control.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")));
            activeLaunchBtn.SetValue(UIElement.EffectProperty, new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#78350F"),
                BlurRadius = 14,
                Opacity = 0.35,
                ShadowDepth = 2
            });
            activeLaunchBtn.SetValue(FrameworkElement.CursorProperty, Cursors.Hand);
            activeLaunchBtn.SetValue(FrameworkElement.TagProperty, "CardLaunchAll");
            activeLaunchBtn.SetValue(FrameworkElement.ToolTipProperty, "Launch all applications in this group");
            activeLaunchBtn.SetValue(AutomationProperties.NameProperty, new System.Windows.Data.Binding("GroupName")
            {
                StringFormat = "Launch all apps in {0}"
            });
            activeLaunchBtn.SetValue(UIElement.VisibilityProperty, Visibility.Hidden);

            var activeLaunchRow = new FrameworkElementFactory(typeof(StackPanel));
            activeLaunchRow.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            var activePlayIcon = Lucide.MakeFactory(Lucide.Play, null, 12.5, fill: true, fixedBrush: new SolidColorBrush((Color)ColorConverter.ConvertFromString("#121415")));
            activePlayIcon.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 6, 0));
            activeLaunchRow.AppendChild(activePlayIcon);
            var activeLaunchTxt = new FrameworkElementFactory(typeof(TextBlock));
            activeLaunchTxt.SetValue(TextBlock.TextProperty, "Launch All");
            activeLaunchTxt.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#121415")));
            activeLaunchTxt.SetValue(TextBlock.FontSizeProperty, 12.0);
            activeLaunchTxt.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            activeLaunchTxt.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            activeLaunchRow.AppendChild(activeLaunchTxt);
            activeLaunchBtn.SetValue(Control.TemplateProperty, ChromeTemplate(activeLaunchRow, 8));

            // Secondary card "Launch" (dark button with amber text and amber play icon).
            var inactiveLaunchBtn = new FrameworkElementFactory(typeof(Button), "InactiveLaunchBtn");
            inactiveLaunchBtn.SetValue(Control.PaddingProperty, new Thickness(14, 6, 14, 6));
            inactiveLaunchBtn.SetBinding(Control.BackgroundProperty, Theme.Bind("PillBg"));
            inactiveLaunchBtn.SetBinding(Control.BorderBrushProperty, Theme.Bind("PillBorder"));
            inactiveLaunchBtn.SetValue(Control.BorderThicknessProperty, new Thickness(1));
            inactiveLaunchBtn.SetValue(Control.MinWidthProperty, 0.0);
            inactiveLaunchBtn.SetValue(Control.MinHeightProperty, 0.0);
            inactiveLaunchBtn.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right);
            inactiveLaunchBtn.SetValue(FrameworkElement.CursorProperty, Cursors.Hand);
            inactiveLaunchBtn.SetValue(FrameworkElement.TagProperty, "CardLaunchAll");
            inactiveLaunchBtn.SetValue(FrameworkElement.ToolTipProperty, "Launch all applications in this group");
            inactiveLaunchBtn.SetValue(AutomationProperties.NameProperty, new System.Windows.Data.Binding("GroupName")
            {
                StringFormat = "Launch all apps in {0}"
            });

            var inactiveLaunchRow = new FrameworkElementFactory(typeof(StackPanel));
            inactiveLaunchRow.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            var inactivePlayIcon = Lucide.MakeFactory(Lucide.Play, "Cyan", 12.5, fill: true);
            inactivePlayIcon.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 6, 0));
            inactiveLaunchRow.AppendChild(inactivePlayIcon);
            var inactiveLaunchTxt = new FrameworkElementFactory(typeof(TextBlock));
            inactiveLaunchTxt.SetValue(TextBlock.TextProperty, "Launch");
            inactiveLaunchTxt.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Cyan"));
            inactiveLaunchTxt.SetValue(TextBlock.FontSizeProperty, 12.0);
            inactiveLaunchTxt.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            inactiveLaunchTxt.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            inactiveLaunchRow.AppendChild(inactiveLaunchTxt);
            inactiveLaunchBtn.SetValue(Control.TemplateProperty, ChromeTemplate(inactiveLaunchRow, 8));

            // Zero-app groups replace Launch with a dashed "Add apps" primary action.
            var emptyAddBtn = new FrameworkElementFactory(typeof(Button), "EmptyAddAppsBtn");
            emptyAddBtn.SetValue(Control.PaddingProperty, new Thickness(0));
            emptyAddBtn.SetValue(Control.BorderThicknessProperty, new Thickness(0));
            emptyAddBtn.SetValue(Control.BackgroundProperty, Brushes.Transparent);
            emptyAddBtn.SetValue(Control.MinWidthProperty, 0.0);
            emptyAddBtn.SetValue(Control.MinHeightProperty, 0.0);
            emptyAddBtn.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right);
            emptyAddBtn.SetValue(FrameworkElement.CursorProperty, Cursors.Hand);
            emptyAddBtn.SetValue(FrameworkElement.TagProperty, "CardAddAppsEmpty");
            emptyAddBtn.SetValue(FrameworkElement.ToolTipProperty, "Add applications to this group");
            emptyAddBtn.SetValue(AutomationProperties.NameProperty, new System.Windows.Data.Binding("GroupName")
            {
                StringFormat = "Add apps to {0}"
            });
            emptyAddBtn.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
            emptyAddBtn.SetValue(Control.TemplateProperty, DashedPillTemplate("Add apps"));

            // Both launch variants occupy the same fixed cell. Visibility.Hidden (not Collapsed) keeps
            // the cell measured, so selecting a card can never change the card or row height.
            var launchHost = new FrameworkElementFactory(typeof(Grid), "LaunchHost");
            launchHost.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right);
            launchHost.AppendChild(activeLaunchBtn);
            launchHost.AppendChild(inactiveLaunchBtn);

            var launchCell = new FrameworkElementFactory(typeof(Grid));
            launchCell.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right);
            launchCell.AppendChild(launchHost);
            launchCell.AppendChild(emptyAddBtn);
            actions.AppendChild(launchCell);

            topGrid.AppendChild(actions);
            cardStack.AppendChild(topGrid);

            // Divider + icon-only app row. App names are intentionally omitted;
            // hidden apps are summarized by the +N badge instead.
            var divider = new FrameworkElementFactory(typeof(Border));
            divider.SetValue(FrameworkElement.HeightProperty, 1.0);
            divider.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 15, 0, 12));
            divider.SetBinding(Border.BackgroundProperty, Theme.Bind("Divider"));
            cardStack.AppendChild(divider);

            var bottomWrap = new FrameworkElementFactory(typeof(WrapPanel));
            bottomWrap.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

            var strip = new FrameworkElementFactory(typeof(ItemsControl));
            strip.SetBinding(ItemsControl.ItemsSourceProperty, new System.Windows.Data.Binding("AppPills"));
            var panelF = new FrameworkElementFactory(typeof(WrapPanel));
            panelF.SetValue(WrapPanel.OrientationProperty, Orientation.Horizontal);
            strip.SetValue(ItemsControl.ItemsPanelProperty, new ItemsPanelTemplate(panelF));

            var pillBtn = new FrameworkElementFactory(typeof(Button));
            pillBtn.SetBinding(Control.BackgroundProperty, Theme.Bind("PillBg"));
            pillBtn.SetBinding(Control.BorderBrushProperty, Theme.Bind("PillBorder"));
            pillBtn.SetValue(Control.BorderThicknessProperty, new Thickness(1));
            pillBtn.SetValue(Control.PaddingProperty, new Thickness(0));
            pillBtn.SetValue(FrameworkElement.WidthProperty, 36.0);
            pillBtn.SetValue(FrameworkElement.HeightProperty, 36.0);
            pillBtn.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 6));
            pillBtn.SetValue(FrameworkElement.CursorProperty, Cursors.Hand);
            pillBtn.SetValue(FrameworkElement.TagProperty, "CardAppPill");
            pillBtn.SetBinding(FrameworkElement.ToolTipProperty, new System.Windows.Data.Binding("ToolTipText"));
            pillBtn.SetValue(AutomationProperties.NameProperty, new System.Windows.Data.Binding("Name")
            {
                StringFormat = "Launch {0}"
            });

            var pillGrid = new FrameworkElementFactory(typeof(Grid));
            var pillIcon = new FrameworkElementFactory(typeof(Image));
            pillIcon.SetBinding(Image.SourceProperty, new System.Windows.Data.Binding("Icon"));
            pillIcon.SetValue(FrameworkElement.WidthProperty, 20.0);
            pillIcon.SetValue(FrameworkElement.HeightProperty, 20.0);
            pillIcon.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            pillIcon.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            pillGrid.AppendChild(pillIcon);

            var arrowPath = new FrameworkElementFactory(typeof(System.Windows.Shapes.Path));
            arrowPath.SetValue(System.Windows.Shapes.Path.DataProperty, Geometry.Parse("M 1.5 5.5 L 5.5 1.5 M 2.5 1.5 L 5.5 1.5 L 5.5 4.5"));
            arrowPath.SetValue(System.Windows.Shapes.Path.StrokeProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")));
            arrowPath.SetValue(System.Windows.Shapes.Path.StrokeThicknessProperty, 1.2);
            arrowPath.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right);
            arrowPath.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Bottom);
            arrowPath.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 2.5, 2.5));
            pillGrid.AppendChild(arrowPath);

            pillBtn.SetValue(Control.TemplateProperty, ChromeTemplate(pillGrid, 8));
            strip.SetValue(ItemsControl.ItemTemplateProperty, new DataTemplate { VisualTree = pillBtn });
            bottomWrap.AppendChild(strip);

            var overflowTxt = new FrameworkElementFactory(typeof(TextBlock));
            overflowTxt.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("OverflowCountText"));
            overflowTxt.SetValue(TextBlock.FontSizeProperty, 12.0);
            overflowTxt.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            overflowTxt.SetValue(TextBlock.FontFamilyProperty, new FontFamily("Cascadia Mono, Consolas"));
            overflowTxt.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            overflowTxt.SetValue(FrameworkElement.MarginProperty, new Thickness(2, 0, 10, 6));
            overflowTxt.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Muted"));
            bottomWrap.AppendChild(overflowTxt);

            cardStack.AppendChild(bottomWrap);

            var dt = new DataTemplate { VisualTree = cardStack };
            var isSelectedTrigger = new DataTrigger
            {
                Binding = new System.Windows.Data.Binding("IsSelected")
                {
                    RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.FindAncestor, typeof(ListBoxItem), 1)
                },
                Value = true
            };
            isSelectedTrigger.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Visible, "ActiveLaunchBtn"));
            isSelectedTrigger.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed, "InactiveLaunchBtn"));
            dt.Triggers.Add(isSelectedTrigger);

            // Zero-app cards replace Launch with a dashed "Add apps" primary action.
            var emptyTrigger = new DataTrigger
            {
                Binding = new System.Windows.Data.Binding("Paths.Count"),
                Value = 0
            };
            emptyTrigger.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Hidden, "LaunchHost"));
            emptyTrigger.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Visible, "EmptyAddAppsBtn"));
            dt.Triggers.Add(emptyTrigger);

            return dt;
        }

        void OnCardButtonClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is not Button btn) return;
            string tag = btn.Tag as string;
            var group = FindCardGroup(btn);

            switch (tag)
            {
                case "GroupIconHandle":
                    // The icon is click-to-open and drag-to-pin. A completed drag suppresses only this click.
                    if (_suppressIconClick) { _suppressIconClick = false; return; }
                    ShowGroupPopup(group);
                    break;
                case "CardLaunchAll":
                    if (group != null)
                    {
                        _list.SelectedItem = group;
                        Launcher.RunAll(group);
                    }
                    break;
                case "CardAddAppsEmpty":
                    if (group != null)
                    {
                        _list.SelectedItem = group;
                        EditGroup(group, true);
                    }
                    break;
                case "CardAppPill":
                    if (btn.DataContext is AppPillItem pill && !string.IsNullOrEmpty(pill.FullPath))
                    {
                        if (group != null) _list.SelectedItem = group;
                        Launcher.Run(pill.FullPath);
                    }
                    break;
            }
        }

        void ShowGroupPopup(GroupData g)
        {
            if (g == null) return;
            _list.SelectedItem = g;
            new PopupWindow(g).Show();
        }

        void OnListContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // Cards no longer expose a more-options menu; suppress the default context menu.
            e.Handled = true;
        }

        static DependencyObject ParentOf(DependencyObject d)
        {
            if (d == null) return null;
            if (d is Visual || d is System.Windows.Media.Media3D.Visual3D) return VisualTreeHelper.GetParent(d);
            return LogicalTreeHelper.GetParent(d);
        }

        static T FindAncestor<T>(DependencyObject d) where T : DependencyObject
        {
            while (d != null)
            {
                if (d is T t) return t;
                d = ParentOf(d);
            }
            return null;
        }

        static T FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed) return typed;
                var found = FindChild<T>(child);
                if (found != null) return found;
            }
            return null;
        }

        static FrameworkElement FindAncestorByTag(DependencyObject d, string tag)
        {
            while (d != null)
            {
                if (d is FrameworkElement fe && string.Equals(fe.Tag as string, tag, StringComparison.Ordinal))
                    return fe;
                d = ParentOf(d);
            }
            return null;
        }

        static GroupData FindCardGroup(DependencyObject d) => FindAncestor<ListBoxItem>(d)?.DataContext as GroupData;
    }
}
