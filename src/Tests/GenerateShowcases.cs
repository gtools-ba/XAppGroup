using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using IOPath = System.IO.Path;
using WpfPath = System.Windows.Shapes.Path;

namespace XAppGroup
{
    public static class GenerateShowcases
    {
        public static void Run(string outDir)
        {
            Directory.CreateDirectory(outDir);

            string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string sys32 = Environment.GetFolderPath(Environment.SpecialFolder.System);

            string notepad = IOPath.Combine(sys32, "notepad.exe");
            string cmd = IOPath.Combine(sys32, "cmd.exe");
            string powershell = IOPath.Combine(sys32, "WindowsPowerShell", "v1.0", "powershell.exe");
            string taskmgr = IOPath.Combine(sys32, "Taskmgr.exe");
            string regedit = IOPath.Combine(winDir, "regedit.exe");
            string mspaint = IOPath.Combine(sys32, "mspaint.exe");
            string calc = IOPath.Combine(sys32, "calc.exe");
            string write = IOPath.Combine(winDir, "write.exe");

            var gDev = new GroupData
            {
                Id = "dev-tools",
                GroupName = "Developer Tools",
                GroupCol = 2,
                Hotkey = "Ctrl + Alt + D",
                CustomSummary = "Integrated terminal environments, text editors, process manager, and registry tools.",
                Paths = new List<string> { powershell, cmd, notepad, taskmgr, regedit }
            };

            var gCreative = new GroupData
            {
                Id = "creative-studio",
                GroupName = "Design & Creative Studio",
                GroupCol = 2,
                Hotkey = "Ctrl + Alt + C",
                CustomSummary = "Digital illustration, graphics editing, quick document authoring, and color tools.",
                Paths = new List<string> { mspaint, write, calc, notepad }
            };

            var sampleSettings = new Settings
            {
                Theme = "Dark Acrylic",
                PopupTheme = "Dark Acrylic",
                AnimateWindow = false,
                AnimateContent = false,
                Groups = new ObservableCollection<GroupData> { gDev, gCreative }
            };
            Settings.Current = sampleSettings;

            // 1. Build and render Showcase 1: Popup Flyout with Windows 11 Desktop Context
            RenderPopupFlyoutShowcase(gDev, IOPath.Combine(outDir, "popup_flyout_preview.png"));

            // 2. Build and render Showcase 2: Dual Edit Window Showcase (Dark & Light)
            RenderEditShowcase(gDev, gCreative, IOPath.Combine(outDir, "edit_showcase_preview.png"));

            Console.WriteLine("SHOWCASES_GENERATED_SUCCESS");
        }

        private static void RenderPopupFlyoutShowcase(GroupData gDev, string outputPath)
        {
            int width = 1040;
            int height = 520;

            Grid root = new Grid
            {
                Width = width,
                Height = height,
                Background = new LinearGradientBrush(Color.FromRgb(12, 15, 20), Color.FromRgb(8, 10, 14), 135.0)
            };

            // Ambient background glow
            Border glow = new Border
            {
                Width = 450, Height = 450, CornerRadius = new CornerRadius(225),
                Background = new RadialGradientBrush(Color.FromArgb(30, 245, 158, 11), Color.FromArgb(0, 0, 0, 0)),
                HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(60, 0, 0, 0)
            };
            root.Children.Add(glow);

            // Left: Mock Desktop Screen with Taskbar
            Border screenFrame = new Border
            {
                Width = 540,
                Height = 440,
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(38, 44, 54)),
                Background = new LinearGradientBrush(Color.FromRgb(20, 25, 34), Color.FromRgb(14, 17, 24), 135.0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(40, 0, 0, 0),
                ClipToBounds = true,
                Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 30, Opacity = 0.6, ShadowDepth = 10 }
            };

            Grid screenGrid = new Grid();
            screenGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            screenGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(46) }); // Taskbar

            // Desktop wallpaper wallpaper texture
            Canvas wpCanvas = new Canvas();
            wpCanvas.Children.Add(new Ellipse
            {
                Width = 320, Height = 220,
                Fill = new RadialGradientBrush(Color.FromArgb(25, 245, 158, 11), Color.FromArgb(0, 0, 0, 0)),
                Margin = new Thickness(120, 80, 0, 0)
            });
            Grid.SetRow(wpCanvas, 0);
            screenGrid.Children.Add(wpCanvas);

            // Popup Flyout instance inside desktop
            Theme.Apply("Dark");
            var pop = new PopupWindow(gDev);
            pop.Show();
            Pump(pop, 200);

            var popVisual = pop.Content as FrameworkElement;
            pop.Content = null;
            pop.Close();

            if (popVisual != null)
            {
                popVisual.HorizontalAlignment = HorizontalAlignment.Left;
                popVisual.VerticalAlignment = VerticalAlignment.Bottom;
                popVisual.Margin = new Thickness(155, 0, 0, 4);
                Grid.SetRow(popVisual, 0);
                screenGrid.Children.Add(popVisual);
            }

            // Taskbar Border
            Border taskbar = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(235, 18, 22, 28)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(40, 46, 58)),
                BorderThickness = new Thickness(0, 1, 0, 0)
            };
            Grid.SetRow(taskbar, 1);

            StackPanel tbCenter = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(16, 0, 0, 0)
            };

            // Windows Logo
            tbCenter.Children.Add(CreateTaskbarIcon("#38BDF8", "Windows"));
            tbCenter.Children.Add(CreateTaskbarIcon("#94A3B8", "Search"));
            tbCenter.Children.Add(CreateTaskbarIcon("#60A5FA", "Explorer"));

            // Pinned XAppGroup Flyout item (Highlighted active)
            Border activeGroupPill = new Border
            {
                Width = 40, Height = 36,
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.FromArgb(40, 245, 158, 11)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 245, 158, 11)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(6, 0, 6, 0)
            };
            Grid agGrid = new Grid();
            agGrid.Children.Add(Icons.BuildBrandSquircle(22));
            Border ind = new Border
            {
                Width = 14, Height = 2.5, CornerRadius = new CornerRadius(1.2),
                Background = new SolidColorBrush(Color.FromRgb(251, 191, 36)),
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 2)
            };
            agGrid.Children.Add(ind);
            activeGroupPill.Child = agGrid;
            tbCenter.Children.Add(activeGroupPill);

            tbCenter.Children.Add(CreateTaskbarIcon("#A78BFA", "Terminal"));
            tbCenter.Children.Add(CreateTaskbarIcon("#34D399", "VSCode"));

            taskbar.Child = tbCenter;
            screenGrid.Children.Add(taskbar);

            screenFrame.Child = screenGrid;
            root.Children.Add(screenFrame);

            // Right: Rich Information Callout Panel
            StackPanel infoPanel = new StackPanel
            {
                Width = 380,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 48, 0)
            };

            Border tag = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 245, 158, 11)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 245, 158, 11)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 3, 8, 3),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            tag.Child = new TextBlock
            {
                Text = "TASKBAR INTEGRATION", FontSize = 11, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(251, 191, 36)), FontFamily = new FontFamily("Segoe UI")
            };
            infoPanel.Children.Add(tag);

            infoPanel.Children.Add(new TextBlock
            {
                Text = "Sleek Flyout Launcher",
                FontSize = 26, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(243, 244, 246)),
                Margin = new Thickness(0, 8, 0, 6), FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI")
            });

            infoPanel.Children.Add(new TextBlock
            {
                Text = "Clicks on your pinned taskbar icon summon an elevated flyout right above your taskbar. Launch any app individually or launch the whole group together.",
                FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 18), FontFamily = new FontFamily("Segoe UI")
            });

            // Feature points
            infoPanel.Children.Add(CreateFeatureBullet("⚡ 260ms Smooth Animation", "Physics-based quintic easing that feels native to Windows 11."));
            infoPanel.Children.Add(CreateFeatureBullet("🚀 1-Click Launch All", "Boot entire development or creative workflows at once."));
            infoPanel.Children.Add(CreateFeatureBullet("⌨️ Global Hotkeys", "Trigger flyouts anywhere in Windows using custom key combinations."));
            infoPanel.Children.Add(CreateFeatureBullet("🖱️ Native Jump Lists", "Right-click the taskbar icon to Open All or Edit Group."));

            root.Children.Add(infoPanel);

            SaveVisual(root, width, height, outputPath);
        }

        private static void RenderEditShowcase(GroupData gDev, GroupData gCreative, string outputPath)
        {
            int width = 1100;
            int height = 580;

            Grid root = new Grid
            {
                Width = width,
                Height = height,
                Background = new LinearGradientBrush(Color.FromRgb(12, 15, 20), Color.FromRgb(8, 10, 14), 135.0)
            };

            // Ambient background glow
            Border glow = new Border
            {
                Width = 500, Height = 500, CornerRadius = new CornerRadius(250),
                Background = new RadialGradientBrush(Color.FromArgb(25, 245, 158, 11), Color.FromArgb(0, 0, 0, 0)),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            root.Children.Add(glow);

            StackPanel mainStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(24, 0, 24, 0)
            };

            // Header Section
            StackPanel headerStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 18) };
            headerStack.Children.Add(new TextBlock
            {
                Text = "Comprehensive Group Customization",
                FontSize = 24, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(243, 244, 246)),
                HorizontalAlignment = HorizontalAlignment.Center,
                FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI")
            });
            headerStack.Children.Add(new TextBlock
            {
                Text = "Full control over group names, applications, layout columns, global hotkeys, and Obsidian Dark / Light Glass themes.",
                FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0), FontFamily = new FontFamily("Segoe UI")
            });
            mainStack.Children.Add(headerStack);

            // Dual Columns Grid
            Grid dualGrid = new Grid { Width = 1040, Height = 440 };
            dualGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            dualGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
            dualGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // 1. Dark Acrylic Card (rendered and snapshotted while Dark theme is active)
            Theme.Apply("Dark");
            var editDark = new EditWindow(gDev);
            var darkImage = SnapshotWindow(editDark, 540);

            Border darkContainer = CreateThemeShowcaseContainer("🌙 Dark Acrylic Theme", darkImage, true);
            Grid.SetColumn(darkContainer, 0);
            dualGrid.Children.Add(darkContainer);

            // 2. Light Glass Card (rendered and snapshotted while Light theme is active)
            Theme.Apply("Light Glass");
            var editLight = new EditWindow(gCreative);
            var lightImage = SnapshotWindow(editLight, 540);

            Border lightContainer = CreateThemeShowcaseContainer("☀️ Light Glass Theme", lightImage, false);
            Grid.SetColumn(lightContainer, 2);
            dualGrid.Children.Add(lightContainer);

            mainStack.Children.Add(dualGrid);
            root.Children.Add(mainStack);

            SaveVisual(root, width, height, outputPath);
        }

        private static Image SnapshotWindow(Window win, int width)
        {
            win.Show();
            Pump(win, 200);

            var visual = win.Content as FrameworkElement;
            win.Content = null;
            win.Close();

            if (visual == null) return new Image();

            visual.Measure(new Size(width, 1500));
            int height = (int)Math.Ceiling(visual.DesiredSize.Height);
            if (height < 600) height = 690;

            visual.Arrange(new Rect(0, 0, width, height));
            visual.UpdateLayout();

            var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);
            rtb.Freeze();

            return new Image
            {
                Source = rtb,
                Width = width,
                Height = height,
                Stretch = Stretch.Uniform
            };
        }

        private static Border CreateThemeShowcaseContainer(string label, FrameworkElement innerContent, bool isDark)
        {
            Border card = new Border
            {
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(isDark ? Color.FromRgb(42, 48, 58) : Color.FromRgb(215, 219, 226)),
                Background = new SolidColorBrush(isDark ? Color.FromRgb(15, 17, 18) : Color.FromRgb(247, 247, 248)),
                ClipToBounds = true,
                Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 24, Opacity = isDark ? 0.45 : 0.12, ShadowDepth = 6 }
            };

            Grid g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Top Bar
            Border bar = new Border
            {
                Background = new SolidColorBrush(isDark ? Color.FromRgb(24, 28, 35) : Color.FromRgb(255, 255, 255)),
                BorderBrush = new SolidColorBrush(isDark ? Color.FromRgb(38, 44, 54) : Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(14, 0, 14, 0)
            };
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            sp.Children.Add(new TextBlock
            {
                Text = label, FontSize = 12, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(isDark ? Color.FromRgb(229, 231, 235) : Color.FromRgb(17, 24, 39)),
                FontFamily = new FontFamily("Segoe UI")
            });
            bar.Child = sp;
            Grid.SetRow(bar, 0);
            g.Children.Add(bar);

            // Scaled Content Viewport
            if (innerContent != null)
            {
                Viewbox vb = new Viewbox { Stretch = Stretch.Uniform, Margin = new Thickness(10) };
                vb.Child = innerContent;
                Grid.SetRow(vb, 1);
                g.Children.Add(vb);
            }

            card.Child = g;
            return card;
        }

        private static Border CreateTaskbarIcon(string hex, string label)
        {
            Border b = new Border
            {
                Width = 34, Height = 34, CornerRadius = new CornerRadius(6),
                Background = Brushes.Transparent, Margin = new Thickness(2, 0, 2, 0)
            };
            b.Child = new Ellipse { Width = 16, Height = 16, Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)) };
            return b;
        }

        private static StackPanel CreateFeatureBullet(string title, string desc)
        {
            StackPanel sp = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            sp.Children.Add(new TextBlock
            {
                Text = title, FontSize = 13, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(243, 244, 246)), FontFamily = new FontFamily("Segoe UI")
            });
            sp.Children.Add(new TextBlock
            {
                Text = desc, FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)), FontFamily = new FontFamily("Segoe UI"),
                Margin = new Thickness(0, 2, 0, 0)
            });
            return sp;
        }

        private static void Pump(Window win, int waitMs = 100)
        {
            win.UpdateLayout();
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
                Application.Current.Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
            }
            Thread.Sleep(waitMs);
            win.UpdateLayout();
        }

        private static void SaveVisual(FrameworkElement el, int width, int height, string file)
        {
            el.Measure(new Size(width, height));
            el.Arrange(new Rect(0, 0, width, height));
            el.UpdateLayout();

            var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(el);

            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(rtb));
            using (var fs = new FileStream(file, FileMode.Create))
            {
                enc.Save(fs);
            }
        }
    }
}
