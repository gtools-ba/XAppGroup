using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using IOPath = System.IO.Path;

namespace XAppGroup
{
    /// <summary>Flyout shown when a group's shortcut is clicked: matches MainWindow & EditWindow Obsidian Amber / Light Glass theme.</summary>
    public class PopupWindow : Window
    {
        private double _targetTop;
        private bool _fromTop;

        public PopupWindow(GroupData g)
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            Topmost = true;
            ResizeMode = ResizeMode.NoResize;
            SizeToContent = SizeToContent.WidthAndHeight;
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI");

            var cfg = Settings.Current;
            bool isDark = Theme.Dark;
            if (cfg != null)
            {
                if (string.Equals(cfg.PopupTheme, "Light Glass", StringComparison.OrdinalIgnoreCase)) isDark = false;
                else if (string.Equals(cfg.PopupTheme, "Dark Acrylic", StringComparison.OrdinalIgnoreCase)) isDark = true;
            }

            SolidColorBrush Hex(string h)
            {
                var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(h));
                b.Freeze();
                return b;
            }

            // Exact 1:1 color tokens from Theme.cs (MainWindow & EditWindow)
            Brush winBg       = Hex(isDark ? "#0F1112" : "#F7F7F8"); // Theme.Bg
            Brush cardBg      = Hex(isDark ? "#1C1F20" : "#FFFFFF"); // Theme.Card
            Brush footerBg    = Hex(isDark ? "#121415" : "#F3F4F6"); // Theme.StatsBg
            Brush cardBorder  = Hex(isDark ? "#2A2E30" : "#E5E7EB"); // Theme.CardBorder
            Brush dividerBg   = Hex(isDark ? "#2A2E30" : "#E5E7EB"); // Theme.Divider
            Brush pillBg      = Hex(isDark ? "#1C1F20" : "#F3F4F6"); // Theme.PillBg / AppPillBg
            Brush pillHover   = Hex(isDark ? "#25292B" : "#E5E7EB"); // Theme.PillHover
            Brush pillBorder  = Hex(isDark ? "#2A2E30" : "#E5E7EB"); // Theme.PillBorder
            Brush hoverBorder = Hex(isDark ? "#383D40" : "#D1D5DB"); // Theme.HoverBorder
            Brush fg          = Hex(isDark ? "#F3F4F6" : "#111827"); // Theme.Fg
            Brush sub         = Hex(isDark ? "#9CA3AF" : "#4B5563"); // Theme.Sub
            Brush hoverFg     = Hex(isDark ? "#FFFFFF" : "#111827"); // Theme.HoverFg
            Brush accent      = Hex("#F59E0B");                      // Theme.Accent
            Brush badgeBg     = Hex(isDark ? "#26F59E0B" : "#FEF3C7"); // Theme.CyanBadgeBg
            Brush badgeBorder = Hex(isDark ? "#4DF59E0B" : "#FCD34D"); // Theme.CyanBadgeBorder
            Brush badgeFg     = Hex(isDark ? "#FBBF24" : "#B45309"); // Theme.CyanBadgeFg

            // Optional subtle Amber accent glow matching MainWindow's ambient glow (never raw Windows blue)
            Brush outerBg = winBg;
            if (cfg != null && cfg.AccentColorBackground)
            {
                outerBg = new LinearGradientBrush(
                    (Color)ColorConverter.ConvertFromString(isDark ? "#1B1710" : "#FFFBEB"),
                    (Color)ColorConverter.ConvertFromString(isDark ? "#0F1112" : "#F7F7F8"),
                    new Point(0, 0), new Point(0, 1));
            }

            // --- App Icons Grid directly on clean outer shell (no inner card box) ---
            int cols = Math.Max(1, g.GroupCol);
            double itemWidth = g.ShowLabels ? 82 : 58;
            var panel = new WrapPanel { Width = cols * itemWidth + 2, Margin = new Thickness(12, 12, 12, 10) };
            var itemBorders = new List<Border>();

            foreach (var p in g.OrderedPaths())
            {
                var path = p;
                var sp = new StackPanel
                {
                    Width = itemWidth - 8,
                    Background = Brushes.Transparent,
                    Cursor = Cursors.Hand,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                var appImg = new Image
                {
                    Source = Icons.Get(path),
                    Width = 32,
                    Height = 32,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                RenderOptions.SetBitmapScalingMode(appImg, BitmapScalingMode.HighQuality);

                var iconTile = new Border
                {
                    Width = 46,
                    Height = 46,
                    CornerRadius = new CornerRadius(9),
                    Background = pillBg,
                    BorderBrush = pillBorder,
                    BorderThickness = new Thickness(1),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Child = appImg
                };
                sp.Children.Add(iconTile);

                if (g.ShowLabels)
                {
                    sp.Children.Add(new TextBlock
                    {
                        Text = IOPath.GetFileNameWithoutExtension(path),
                        Foreground = fg,
                        FontSize = 11,
                        Margin = new Thickness(0, 4, 0, 0),
                        TextAlignment = TextAlignment.Center,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    });
                }

                var itemBorder = new Border
                {
                    Width = itemWidth,
                    CornerRadius = new CornerRadius(10),
                    Background = Brushes.Transparent,
                    Padding = new Thickness(4, 4, 4, 4),
                    Cursor = Cursors.Hand,
                    ToolTip = IOPath.GetFileNameWithoutExtension(path),
                    Child = sp
                };

                itemBorder.MouseEnter += (s, e) =>
                {
                    iconTile.Background = pillHover;
                    iconTile.BorderBrush = hoverBorder;
                };
                itemBorder.MouseLeave += (s, e) =>
                {
                    iconTile.Background = pillBg;
                    iconTile.BorderBrush = pillBorder;
                };
                itemBorder.MouseLeftButtonUp += (s, e) => { Launcher.Run(path); Close(); };

                var menu = new ContextMenu();
                var open = new MenuItem { Header = "Open" }; open.Click += (s, e) => { Launcher.Run(path); Close(); };
                var adm = new MenuItem { Header = "Run as Administrator" }; adm.Click += (s, e) => { Launcher.Run(path, true); Close(); };
                var loc = new MenuItem { Header = "Open File Location" };
                loc.Click += (s, e) => { Process.Start("explorer.exe", "/select,\"" + path + "\""); Close(); };
                menu.Items.Add(open); menu.Items.Add(adm); menu.Items.Add(loc);
                itemBorder.ContextMenu = menu;

                panel.Children.Add(itemBorder);
                itemBorders.Add(itemBorder);
            }

            // --- Footer Bar with Card background (#1C1F20 dark / #FFFFFF light) ---
            var footerDock = new DockPanel { LastChildFill = true };

            var editBtn = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(8),
                Background = pillBg,
                BorderBrush = pillBorder,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                ToolTip = "Edit Group",
                VerticalAlignment = VerticalAlignment.Center,
                Child = Lucide.Make(Lucide.Edit3, sub, 13.5, strokeWidth: 2.0)
            };
            editBtn.MouseEnter += (s, e) =>
            {
                editBtn.Background = pillHover;
                editBtn.BorderBrush = hoverBorder;
                editBtn.Child = Lucide.Make(Lucide.Edit3, hoverFg, 13.5, strokeWidth: 2.0);
            };
            editBtn.MouseLeave += (s, e) =>
            {
                editBtn.Background = pillBg;
                editBtn.BorderBrush = pillBorder;
                editBtn.Child = Lucide.Make(Lucide.Edit3, sub, 13.5, strokeWidth: 2.0);
            };
            editBtn.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                OpenGroupEditor(g);
            };
            DockPanel.SetDock(editBtn, Dock.Right);
            footerDock.Children.Add(editBtn);

            var leftInfo = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            ImageSource groupIcon = g.IconSource;
            if (cfg != null && cfg.GrayscaleIcon && groupIcon is BitmapSource bs)
            {
                try { groupIcon = new FormatConvertedBitmap(bs, PixelFormats.Gray8, null, 0); } catch { }
            }
            leftInfo.Children.Add(new Image
            {
                Source = groupIcon,
                Width = 20,
                Height = 20,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            leftInfo.Children.Add(new TextBlock
            {
                Text = g.GroupName,
                Foreground = fg,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            var countBadge = new Border
            {
                CornerRadius = new CornerRadius(10),
                Background = badgeBg,
                BorderBrush = badgeBorder,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6, 1, 6, 1),
                Margin = new Thickness(8, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = g.Paths.Count == 1 ? "1 app" : $"{g.Paths.Count} apps",
                    Foreground = badgeFg,
                    FontSize = 10.5,
                    FontWeight = FontWeights.SemiBold
                }
            };
            leftInfo.Children.Add(countBadge);
            footerDock.Children.Add(leftInfo);

            var footerBorder = new Border
            {
                Background = cardBg,
                BorderBrush = dividerBg,
                BorderThickness = new Thickness(0, 1, 0, 0),
                CornerRadius = new CornerRadius(0, 0, 12, 12),
                Padding = new Thickness(12, 8, 10, 8),
                Child = footerDock
            };

            var root = new StackPanel();
            root.Children.Add(panel);
            root.Children.Add(footerBorder);

            bool animateWin = cfg == null || cfg.AnimateWindow;
            bool animateContent = cfg == null || cfg.AnimateContent;

            var outerBorder = new Border
            {
                Background = outerBg,
                CornerRadius = new CornerRadius(12),
                BorderBrush = cardBorder,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(12),
                Opacity = animateWin ? 0 : 1,
                Child = root,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 20,
                    Opacity = isDark ? 0.55 : 0.20,
                    ShadowDepth = 5
                }
            };
            Content = outerBorder;

            if (animateContent)
            {
                foreach (var ib in itemBorders)
                {
                    ib.Opacity = 0;
                    ib.RenderTransform = new TranslateTransform(0, 14);
                }
            }

            bool closing = false;
            Closing += (s, e) => closing = true;
            Deactivated += (s, e) => { if (!closing) { try { Close(); } catch { } } };
            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape) { Close(); return; }
                int idx = -1;
                if (e.Key >= Key.D1 && e.Key <= Key.D9) idx = e.Key - Key.D1;
                else if (e.Key >= Key.NumPad1 && e.Key <= Key.NumPad9) idx = e.Key - Key.NumPad1;
                if (idx >= 0)
                {
                    var paths = g.OrderedPaths().ToList();
                    if (idx < paths.Count) { Launcher.Run(paths[idx]); Close(); }
                }
            };

            Loaded += (s, e) =>
            {
                Position();
                if (animateWin)
                {
                    Top = _fromTop ? _targetTop - 38 : _targetTop + 38;
                }
                Activate();
            };

            ContentRendered += (s, e) =>
            {
                if (animateWin)
                {
                    var slideEase = new QuinticEase { EasingMode = EasingMode.EaseOut };
                    var fadeEase = new CubicEase { EasingMode = EasingMode.EaseOut };
                    var topAnim = new DoubleAnimation(Top, _targetTop, TimeSpan.FromMilliseconds(260)) { EasingFunction = slideEase };
                    var fadeAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)) { EasingFunction = fadeEase };
                    BeginAnimation(Window.TopProperty, topAnim);
                    outerBorder.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
                }
                if (animateContent)
                {
                    var itemEase = new CubicEase { EasingMode = EasingMode.EaseOut };
                    for (int i = 0; i < itemBorders.Count; i++)
                    {
                        var ib = itemBorders[i];
                        var tt = ib.RenderTransform as TranslateTransform ?? new TranslateTransform(0, 14);
                        ib.RenderTransform = tt;
                        var delay = TimeSpan.FromMilliseconds(Math.Min(i * 20, 140));
                        var yAnim = new DoubleAnimation(_fromTop ? -12 : 14, 0, TimeSpan.FromMilliseconds(220)) { BeginTime = delay, EasingFunction = itemEase };
                        var opAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)) { BeginTime = delay, EasingFunction = itemEase };
                        tt.BeginAnimation(TranslateTransform.YProperty, yAnim);
                        ib.BeginAnimation(UIElement.OpacityProperty, opAnim);
                    }
                }
            };
        }

        private void OpenGroupEditor(GroupData g)
        {
            Close();
            if (Application.Current?.MainWindow is MainWindow mw)
            {
                mw.Show();
                if (mw.WindowState == WindowState.Minimized) mw.WindowState = WindowState.Normal;
                mw.Activate();
                mw.EditGroupById(g.Id);
                return;
            }
            try
            {
                var exe = Environment.ProcessPath;
                if (exe != null && exe.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
                {
                    var candidate = IOPath.Combine(AppContext.BaseDirectory, "XAppGroup.exe");
                    if (File.Exists(candidate)) exe = candidate;
                }
                if (!string.IsNullOrEmpty(exe))
                {
                    Process.Start(new ProcessStartInfo(exe, "--edit " + g.Id) { UseShellExecute = true });
                }
            }
            catch { }
        }

        void Position()
        {
            var c = System.Windows.Forms.Cursor.Position;
            var src = PresentationSource.FromVisual(this);
            var m = src?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
            var pt = m.Transform(new Point(c.X, c.Y));
            var wa = SystemParameters.WorkArea;

            double left = Math.Min(Math.Max(wa.Left + 8, pt.X - ActualWidth / 2), wa.Right - ActualWidth - 8);
            double top;
            _fromTop = false;
            if (pt.Y > wa.Bottom - 100)
                top = wa.Bottom - ActualHeight - 4;
            else if (pt.Y < wa.Top + 100)
            {
                top = wa.Top + 4;
                _fromTop = true;
            }
            else
                top = Math.Min(Math.Max(wa.Top + 8, pt.Y - ActualHeight - 16), wa.Bottom - ActualHeight - 8);

            _targetTop = top;
            Left = left;
            Top = top;
        }
    }
}
