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
    public class ThemePalette : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void NotifyAll() => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(string.Empty));

        public Brush Bg => new SolidColorBrush(Theme.Bg.Color);
        public Brush TitleBg => new SolidColorBrush(Theme.TitleBg.Color);
        public Brush StatsBg => new SolidColorBrush(Theme.StatsBg.Color);
        public Brush Panel => new SolidColorBrush(Theme.Panel.Color);
        public Brush Card => new SolidColorBrush(Theme.Card.Color);
        public Brush CardHover => new SolidColorBrush(Theme.CardHover.Color);
        public Brush CardBorder => new SolidColorBrush(Theme.CardBorder.Color);
        public Brush CardActiveBorder => new SolidColorBrush(Theme.CardActiveBorder.Color);
        public Brush Divider => new SolidColorBrush(Theme.Divider.Color);
        public Brush PillBg => new SolidColorBrush(Theme.PillBg.Color);
        public Brush PillHover => new SolidColorBrush(Theme.PillHover.Color);
        public Brush PillBorder => new SolidColorBrush(Theme.PillBorder.Color);
        public Brush AppPillBg => new SolidColorBrush(Theme.AppPillBg.Color);
        public Brush Fg => new SolidColorBrush(Theme.Fg.Color);
        public Brush Sub => new SolidColorBrush(Theme.Sub.Color);
        public Brush Muted => new SolidColorBrush(Theme.Muted.Color);
        public Brush Accent => new SolidColorBrush(Theme.Accent.Color);
        public Brush Cyan => new SolidColorBrush(Theme.Cyan.Color);
        public Brush CyanBadgeBg => new SolidColorBrush(Theme.CyanBadgeBg.Color);
        public Brush CyanBadgeBorder => new SolidColorBrush(Theme.CyanBadgeBorder.Color);
        public Brush CyanBadgeFg => new SolidColorBrush(Theme.CyanBadgeFg.Color);
        public Brush Indigo => new SolidColorBrush(Theme.Indigo.Color);
        public Brush Sky => new SolidColorBrush(Theme.Sky.Color);
        public Brush Emerald => new SolidColorBrush(Theme.Emerald.Color);
        public Brush EmeraldBg => new SolidColorBrush(Theme.EmeraldBg.Color);
        public Brush EmeraldBorder => new SolidColorBrush(Theme.EmeraldBorder.Color);
        public Brush EmeraldFg => new SolidColorBrush(Theme.EmeraldFg.Color);
        public Brush DangerBg => new SolidColorBrush(Theme.DangerBg.Color);
        public Brush DangerBorder => new SolidColorBrush(Theme.DangerBorder.Color);
        public Brush DangerFg => new SolidColorBrush(Theme.DangerFg.Color);
        public Brush DangerHoverBg => new SolidColorBrush(Theme.DangerHoverBg.Color);
        public Brush DangerHoverBorder => new SolidColorBrush(Theme.DangerHoverBorder.Color);
        public Brush DangerHoverFg => new SolidColorBrush(Theme.DangerHoverFg.Color);
        public Brush HoverFg => new SolidColorBrush(Theme.HoverFg.Color);
        public Brush HoverBorder => new SolidColorBrush(Theme.HoverBorder.Color);
        public Brush ScrollBarThumb => new SolidColorBrush(Theme.ScrollBarThumb.Color);
        public Brush IconSquircleBg => new SolidColorBrush(Theme.IconSquircleBg.Color);
        public Brush IconSquircleBorder => new SolidColorBrush(Theme.IconSquircleBorder.Color);
        public Brush TipBg => new SolidColorBrush(Theme.TipBg.Color);
        public Brush PopupBg => new SolidColorBrush(Theme.PopupBg.Color);
        public Brush Hover => new SolidColorBrush(Theme.Hover.Color);
        public Brush Amber => Cyan;
        public Brush AmberDim => CyanBadgeBg;
        public Brush AmberBorder => CyanBadgeBorder;
    }

    public static class Theme
    {
        public static readonly ThemePalette Palette = new ThemePalette();
        public static bool Dark = true;
        public static SolidColorBrush Amber => Cyan;
        public static SolidColorBrush Bg { get; } = new SolidColorBrush();
        public static SolidColorBrush TitleBg { get; } = new SolidColorBrush();
        public static SolidColorBrush StatsBg { get; } = new SolidColorBrush();
        public static SolidColorBrush Panel { get; } = new SolidColorBrush();
        public static SolidColorBrush Card { get; } = new SolidColorBrush();
        public static SolidColorBrush CardHover { get; } = new SolidColorBrush();
        public static SolidColorBrush CardBorder { get; } = new SolidColorBrush();
        public static SolidColorBrush CardActiveBorder { get; } = new SolidColorBrush();
        public static SolidColorBrush Divider { get; } = new SolidColorBrush();
        public static SolidColorBrush PillBg { get; } = new SolidColorBrush();
        public static SolidColorBrush PillHover { get; } = new SolidColorBrush();
        public static SolidColorBrush PillBorder { get; } = new SolidColorBrush();
        public static SolidColorBrush AppPillBg { get; } = new SolidColorBrush();
        public static SolidColorBrush Fg { get; } = new SolidColorBrush();
        public static SolidColorBrush Sub { get; } = new SolidColorBrush();
        public static SolidColorBrush Muted { get; } = new SolidColorBrush();
        public static SolidColorBrush Accent { get; } = new SolidColorBrush();
        public static SolidColorBrush Cyan { get; } = new SolidColorBrush();
        public static SolidColorBrush CyanBadgeBg { get; } = new SolidColorBrush();
        public static SolidColorBrush CyanBadgeBorder { get; } = new SolidColorBrush();
        public static SolidColorBrush CyanBadgeFg { get; } = new SolidColorBrush();
        public static SolidColorBrush Indigo { get; } = new SolidColorBrush();
        public static SolidColorBrush Sky { get; } = new SolidColorBrush();
        public static SolidColorBrush Emerald { get; } = new SolidColorBrush();
        public static SolidColorBrush EmeraldBg { get; } = new SolidColorBrush();
        public static SolidColorBrush EmeraldBorder { get; } = new SolidColorBrush();
        public static SolidColorBrush EmeraldFg { get; } = new SolidColorBrush();
        public static SolidColorBrush DangerBg { get; } = new SolidColorBrush();
        public static SolidColorBrush DangerBorder { get; } = new SolidColorBrush();
        public static SolidColorBrush DangerFg { get; } = new SolidColorBrush();
        public static SolidColorBrush DangerHoverBg { get; } = new SolidColorBrush();
        public static SolidColorBrush DangerHoverBorder { get; } = new SolidColorBrush();
        public static SolidColorBrush DangerHoverFg { get; } = new SolidColorBrush();
        public static SolidColorBrush HoverFg { get; } = new SolidColorBrush();
        public static SolidColorBrush HoverBorder { get; } = new SolidColorBrush();
        public static SolidColorBrush ScrollBarThumb { get; } = new SolidColorBrush();
        public static SolidColorBrush IconSquircleBg { get; } = new SolidColorBrush();
        public static SolidColorBrush IconSquircleBorder { get; } = new SolidColorBrush();
        public static SolidColorBrush TipBg { get; } = new SolidColorBrush();
        public static SolidColorBrush PopupBg { get; } = new SolidColorBrush();
        public static SolidColorBrush Hover { get; } = new SolidColorBrush();

        public static System.Windows.Data.Binding Bind(string prop) =>
            new System.Windows.Data.Binding(prop) { Source = Palette };

        static Theme()
        {
            Apply("System");
        }

        public static void Apply(string mode)
        {
            // "System" (System Mica) and "Dark" (Dark Acrylic) use the signature Obsidian Amber aesthetic from screen.png;
            // "Light" (Light Glass) provides a complete high-contrast Light Glass theme.
            bool light = string.Equals(mode, "Light", StringComparison.OrdinalIgnoreCase);
            bool dark = !light;
            Dark = dark;

            if (string.Equals(mode, "Dark", StringComparison.OrdinalIgnoreCase))
                SetColor(Bg, "#0F1112");
            else if (light)
                SetColor(Bg, "#F7F7F8");
            else
                SetColor(Bg, "#0F1112"); // System (Mica)

            SetColor(TitleBg, dark ? "#0F1112" : "#FFFFFF");
            SetColor(StatsBg, dark ? "#121415" : "#F3F4F6");
            SetColor(Panel, dark ? "#121415" : "#FFFFFF");
            SetColor(Card, dark ? "#1C1F20" : "#FFFFFF");
            SetColor(CardHover, dark ? "#222627" : "#F9FAFB");
            SetColor(CardBorder, dark ? "#2A2E30" : "#E5E7EB");
            SetColor(CardActiveBorder, dark ? "#F59E0B" : "#F59E0B");
            SetColor(Divider, dark ? "#2A2E30" : "#E5E7EB");
            SetColor(PillBg, dark ? "#1C1F20" : "#F3F4F6");
            SetColor(PillHover, dark ? "#25292B" : "#E5E7EB");
            SetColor(PillBorder, dark ? "#2A2E30" : "#E5E7EB");
            SetColor(AppPillBg, dark ? "#1C1F20" : "#F3F4F6");
            SetColor(Fg, dark ? "#F3F4F6" : "#111827");
            SetColor(Sub, dark ? "#9CA3AF" : "#4B5563");
            SetColor(Muted, dark ? "#6B7280" : "#9CA3AF");
            SetColor(Accent, "#F59E0B");
            SetColor(Cyan, dark ? "#F59E0B" : "#D97706");
            SetColor(CyanBadgeBg, dark ? "#26F59E0B" : "#FEF3C7");
            SetColor(CyanBadgeBorder, dark ? "#4DF59E0B" : "#FCD34D");
            SetColor(CyanBadgeFg, dark ? "#FBBF24" : "#B45309");
            SetColor(Indigo, dark ? "#D1D5DB" : "#4B5563");
            SetColor(Sky, dark ? "#D1D5DB" : "#4B5563");
            SetColor(Emerald, dark ? "#F59E0B" : "#D97706");
            SetColor(EmeraldBg, dark ? "#26F59E0B" : "#FEF3C7");
            SetColor(EmeraldBorder, dark ? "#4DF59E0B" : "#FCD34D");
            SetColor(EmeraldFg, dark ? "#FBBF24" : "#B45309");
            SetColor(DangerBg, dark ? "#26EF4444" : "#FEE2E2");
            SetColor(DangerBorder, dark ? "#4DEF4444" : "#FCA5A5");
            SetColor(DangerFg, dark ? "#FDA4AF" : "#DC2626");
            SetColor(DangerHoverBg, dark ? "#3B1219" : "#FEE2E2");
            SetColor(DangerHoverBorder, dark ? "#BE123C" : "#EF4444");
            SetColor(DangerHoverFg, dark ? "#FDA4AF" : "#B91C1C");
            SetColor(HoverFg, dark ? "#FFFFFF" : "#111827");
            SetColor(HoverBorder, dark ? "#383D40" : "#D1D5DB");
            SetColor(ScrollBarThumb, dark ? "#24FFFFFF" : "#24000000");
            SetColor(IconSquircleBg, dark ? "#28251E" : "#FEF3C7");
            SetColor(IconSquircleBorder, dark ? "#4DF59E0B" : "#FCD34D");
            SetColor(TipBg, dark ? "#171A1B" : "#FFFBEB");
            SetColor(PopupBg, dark ? "#1C1F20" : "#FFFFFF");
            SetColor(Hover, dark ? "#25292B" : "#E5E7EB");
            Palette.NotifyAll();
        }

        static void SetColor(SolidColorBrush brush, string hex)
        {
            var c = (Color)ColorConverter.ConvertFromString(hex);
            if (brush.Color != c)
            {
                brush.Color = c;
            }
        }

        public static Button Btn(string text, RoutedEventHandler click, bool primary = false, string iconPath = null, string suffix = null, string variant = null, string iconBrushProp = "Cyan")
        {
            var b = new Button
            {
                Content = text,
                Padding = primary ? new Thickness(13, 7, 13, 7) : new Thickness(11, 7, 11, 7),
                Margin = new Thickness(0, 2, 6, 2),
                BorderThickness = new Thickness(1),
                FontSize = 12,
                FontWeight = primary ? FontWeights.Bold : FontWeights.Medium,
                Cursor = Cursors.Hand
            };

            if (primary)
            {
                b.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                b.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#121415"));
                b.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                b.Effect = new DropShadowEffect
                {
                    Color = (Color)ColorConverter.ConvertFromString("#78350F"),
                    BlurRadius = 12,
                    Opacity = 0.35,
                    ShadowDepth = 2
                };
            }
            else if (variant == "danger")
            {
                b.SetBinding(Control.BackgroundProperty, Bind("PillBg"));
                b.SetBinding(Control.ForegroundProperty, Bind("Sub"));
                b.SetBinding(Control.BorderBrushProperty, Bind("PillBorder"));
            }
            else if (variant == "emerald")
            {
                b.SetBinding(Control.BackgroundProperty, Bind("PillBg"));
                b.SetBinding(Control.ForegroundProperty, Bind("EmeraldFg"));
                b.SetBinding(Control.BorderBrushProperty, Bind("PillBorder"));
            }
            else
            {
                b.SetBinding(Control.BackgroundProperty, Bind("PillBg"));
                b.SetBinding(Control.ForegroundProperty, Bind("Sub"));
                b.SetBinding(Control.BorderBrushProperty, Bind("PillBorder"));
            }

            var bd = new FrameworkElementFactory(typeof(Border), "BtnBorder");
            bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            bd.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            bd.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
            bd.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
            bd.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));

            var sp = new FrameworkElementFactory(typeof(StackPanel));
            sp.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            sp.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            sp.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

            if (!string.IsNullOrEmpty(iconPath))
            {
                Brush iconBrush = primary
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#121415"))
                    : (variant == "danger" ? null : (variant == "emerald" ? null : null));
                string prop = primary ? null : (variant == "danger" ? "DangerFg" : (variant == "emerald" ? "EmeraldFg" : iconBrushProp));
                var ic = Lucide.MakeFactory(iconPath, prop, 13.5, fill: false, fixedBrush: iconBrush, strokeWidth: 2.0);
                ic.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 7, 0));
                sp.AppendChild(ic);
            }

            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
            cp.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            sp.AppendChild(cp);

            if (!string.IsNullOrEmpty(suffix))
            {
                var sf = new FrameworkElementFactory(typeof(TextBlock));
                sf.SetValue(TextBlock.TextProperty, suffix);
                sf.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
                sp.AppendChild(sf);
            }

            bd.AppendChild(sp);
            var ct = new ControlTemplate(typeof(Button)) { VisualTree = bd };
            var hoverTrig = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            if (primary)
            {
                hoverTrig.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBBF24")), "BtnBorder"));
            }
            else if (variant == "danger")
            {
                hoverTrig.Setters.Add(new Setter(Border.BackgroundProperty, Theme.Bind("DangerHoverBg"), "BtnBorder"));
                hoverTrig.Setters.Add(new Setter(Border.BorderBrushProperty, Theme.Bind("DangerHoverBorder"), "BtnBorder"));
                hoverTrig.Setters.Add(new Setter(Control.ForegroundProperty, Theme.Bind("DangerHoverFg")));
            }
            else
            {
                hoverTrig.Setters.Add(new Setter(Border.BackgroundProperty, Theme.Bind("PillHover"), "BtnBorder"));
                hoverTrig.Setters.Add(new Setter(Border.BorderBrushProperty, Theme.Bind("HoverBorder"), "BtnBorder"));
                hoverTrig.Setters.Add(new Setter(Control.ForegroundProperty, Theme.Bind("HoverFg")));
            }
            ct.Triggers.Add(hoverTrig);
            b.Template = ct;

            b.Click += click;
            return b;
        }

        public static Style BuildSleekScrollBarStyle()
        {
            const string xaml = @"
<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
       TargetType='ScrollBar'>
    <Setter Property='Width' Value='2'/>
    <Setter Property='MinWidth' Value='2'/>
    <Setter Property='MaxWidth' Value='2'/>
    <Setter Property='Background' Value='Transparent'/>
    <Setter Property='Opacity' Value='0'/>
    <Setter Property='Template'>
        <Setter.Value>
            <ControlTemplate TargetType='ScrollBar'>
                <Grid x:Name='RootGrid' Background='Transparent' Width='2'>
                    <Track x:Name='PART_Track' IsDirectionReversed='True' Width='2'>
                        <Track.Thumb>
                            <Thumb Width='2'>
                                <Thumb.Template>
                                    <ControlTemplate TargetType='Thumb'>
                                        <Border CornerRadius='1' Width='2' Background='#35888888' Margin='0'/>
                                    </ControlTemplate>
                                </Thumb.Template>
                            </Thumb>
                        </Track.Thumb>
                    </Track>
                </Grid>
                <ControlTemplate.Triggers>
                    <Trigger Property='IsMouseOver' Value='True'>
                        <Setter Property='Opacity' Value='0.85'/>
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>";
            return (Style)System.Windows.Markup.XamlReader.Parse(xaml);
        }

        public static T FindChild<T>(DependencyObject parent) where T : DependencyObject
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
    }
}
