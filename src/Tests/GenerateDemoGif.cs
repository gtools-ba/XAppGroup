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
    public static class GenerateDemoGif
    {
        const int W = 760;
        const int H = 428;
        const double TaskbarH = 46;

        public static void Run(string outFile)
        {
            string dir = IOPath.GetDirectoryName(outFile);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string sys32 = Environment.GetFolderPath(Environment.SpecialFolder.System);

            var gDev = new GroupData
            {
                Id = "dev-tools",
                GroupName = "Developer Tools",
                GroupCol = 2,
                Hotkey = "Ctrl + Alt + D",
                Paths = new List<string>
                {
                    IOPath.Combine(sys32, "WindowsPowerShell", "v1.0", "powershell.exe"),
                    IOPath.Combine(sys32, "cmd.exe"),
                    IOPath.Combine(sys32, "notepad.exe"),
                    IOPath.Combine(sys32, "Taskmgr.exe"),
                    IOPath.Combine(winDir, "regedit.exe")
                }
            };

            Settings.Current = new Settings
            {
                Theme = "Dark Acrylic",
                PopupTheme = "Dark Acrylic",
                AnimateWindow = false,
                AnimateContent = false,
                Groups = new ObservableCollection<GroupData> { gDev }
            };

            // 1. Capture the real PopupWindow visual once as a crisp frozen bitmap
            Theme.Apply("Dark");
            var pop = new PopupWindow(gDev);
            pop.Show();
            Pump(pop, 220);

            var popVisual = pop.Content as FrameworkElement;
            pop.Content = null;
            pop.Close();

            int popW = 244;
            int popH = 264;
            BitmapSource popBmp = null;
            if (popVisual != null)
            {
                popVisual.Measure(new Size(400, 450));
                popW = Math.Max(200, (int)Math.Ceiling(popVisual.DesiredSize.Width));
                popH = Math.Max(200, (int)Math.Ceiling(popVisual.DesiredSize.Height));
                popVisual.Arrange(new Rect(0, 0, popW, popH));
                popVisual.UpdateLayout();

                var prtb = new RenderTargetBitmap(popW, popH, 96, 96, PixelFormats.Pbgra32);
                prtb.Render(popVisual);
                prtb.Freeze();
                popBmp = prtb;
            }

            // 2. Build interactive scene graph
            var scene = BuildScene(popBmp, popW, popH);

            // Key coordinates on the 760x428 canvas
            double iconX = W / 2.0; // Pinned XAppGroup icon center X (380)
            double iconY = H - TaskbarH / 2.0; // Taskbar center Y
            double popLeft = iconX - popW / 2.0;
            double popTop = H - TaskbarH - popH - 10;

            // PowerShell tile center inside the 2-column flyout (top-left tile)
            double psTileX = popLeft + popW * 0.36;
            double psTileY = popTop + 44;

            var frames = new List<(byte[] GifBytes, ushort DelayCs)>();

            void CaptureFrame(int delayCs)
            {
                scene.Root.Measure(new Size(W, H));
                scene.Root.Arrange(new Rect(0, 0, W, H));
                scene.Root.UpdateLayout();

                var rtb = new RenderTargetBitmap(W, H, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(scene.Root);
                rtb.Freeze();

                var enc = new GifBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(rtb));
                using (var ms = new MemoryStream())
                {
                    enc.Save(ms);
                    frames.Add((ms.ToArray(), (ushort)delayCs));
                }
            }

            // Helper to update cursor position
            void SetCursor(double x, double y, bool pressed = false)
            {
                Canvas.SetLeft(scene.Cursor, x);
                Canvas.SetTop(scene.Cursor, y);
                scene.CursorScale.ScaleX = pressed ? 0.86 : 1.0;
                scene.CursorScale.ScaleY = pressed ? 0.86 : 1.0;
            }

            void SetStep(string badge, string title)
            {
                scene.StepBadgeText.Text = badge;
                scene.StepTitleText.Text = title;
            }

            void SetFlyout(double progress) // 0 = hidden, 1 = fully open (quintic ease)
            {
                double eased = 1.0 - Math.Pow(1.0 - Math.Clamp(progress, 0, 1), 5);
                scene.PopupHost.Opacity = eased;
                scene.PopupTranslate.Y = (1.0 - eased) * 26.0;
                scene.TaskbarIndicator.Width = 6 + 10 * eased;
                scene.TaskbarPill.Background = new SolidColorBrush(Color.FromArgb((byte)(18 + 32 * eased), 245, 158, 11));
                scene.TaskbarPill.BorderBrush = new SolidColorBrush(Color.FromArgb((byte)(40 + 80 * eased), 245, 158, 11));
            }

            void SetRipple(double x, double y, double t) // t: 0..1, <0 hidden
            {
                if (t < 0 || t > 1)
                {
                    scene.Ripple.Opacity = 0;
                    return;
                }
                double eased = 1.0 - Math.Pow(1.0 - t, 3);
                double r = 10 + 34 * eased;
                scene.Ripple.Width = r * 2;
                scene.Ripple.Height = r * 2;
                Canvas.SetLeft(scene.Ripple, x - r);
                Canvas.SetTop(scene.Ripple, y - r);
                scene.Ripple.Opacity = (1.0 - t) * 0.9;
            }

            void SetTerminal(double progress)
            {
                double eased = 1.0 - Math.Pow(1.0 - Math.Clamp(progress, 0, 1), 4);
                scene.TerminalCard.Opacity = eased;
                scene.TerminalScale.ScaleX = 0.92 + 0.08 * eased;
                scene.TerminalScale.ScaleY = 0.92 + 0.08 * eased;
            }

            void SetHotkeyHud(double progress, bool keysPressed)
            {
                double eased = 1.0 - Math.Pow(1.0 - Math.Clamp(progress, 0, 1), 3);
                scene.HotkeyHud.Opacity = eased;
                scene.HotkeyTranslate.Y = (1.0 - eased) * -14.0;
                var borderColor = keysPressed ? Color.FromRgb(245, 158, 11) : Color.FromRgb(64, 72, 86);
                var bgColor = keysPressed ? Color.FromArgb(65, 245, 158, 11) : Color.FromRgb(24, 28, 35);
                foreach (var k in scene.Keycaps)
                {
                    k.BorderBrush = new SolidColorBrush(borderColor);
                    k.Background = new SolidColorBrush(bgColor);
                }
            }

            // Initialize clean starting state
            double startX = 560, startY = 210;
            SetCursor(startX, startY);
            SetStep("STEP 1 / 4", "Click Pinned Taskbar Group");
            SetFlyout(0);
            SetRipple(0, 0, -1);
            SetTerminal(0);
            SetHotkeyHud(0, false);
            scene.HoverRing.Opacity = 0;

            // --- PHASE 1: Intro hold (0.6s) ---
            CaptureFrame(60);

            // --- PHASE 2: Cursor glides to pinned taskbar icon (0.68s @ 25 FPS = 17 frames) ---
            for (int i = 1; i <= 17; i++)
            {
                double t = i / 17.0;
                double e = EaseInOutCubic(t);
                SetCursor(Lerp(startX, iconX, e), Lerp(startY, iconY, e));
                if (t > 0.8)
                    scene.TaskbarPill.Background = new SolidColorBrush(Color.FromArgb(40, 245, 158, 11));
                CaptureFrame(4);
            }

            // --- PHASE 3: Click pinned icon + 260ms Quintic Flyout Opening (0.36s @ 25 FPS = 9 frames) ---
            SetStep("STEP 2 / 4", "260ms Quintic-Eased Flyout");
            for (int i = 0; i < 9; i++)
            {
                double t = i / 8.0;
                SetCursor(iconX, iconY, pressed: i < 2);
                SetRipple(iconX, iconY, t);
                double flyT = Math.Clamp((i - 1) / 6.5, 0, 1); // ~260ms quintic ease
                SetFlyout(flyT);
                CaptureFrame(4);
            }
            SetRipple(0, 0, -1);
            SetCursor(iconX, iconY, false);
            SetFlyout(1);

            // Hold on open flyout (0.9s)
            CaptureFrame(90);

            // --- PHASE 4: Cursor glides up to PowerShell tile inside flyout (0.56s @ 25 FPS = 14 frames) ---
            SetStep("STEP 3 / 4", "1-Click App Launch");
            for (int i = 1; i <= 14; i++)
            {
                double t = i / 14.0;
                double e = EaseInOutCubic(t);
                SetCursor(Lerp(iconX, psTileX, e), Lerp(iconY, psTileY, e));
                if (t > 0.72)
                {
                    double h = (t - 0.72) / 0.28;
                    Canvas.SetLeft(scene.HoverRing, psTileX - 28);
                    Canvas.SetTop(scene.HoverRing, psTileY - 26);
                    scene.HoverRing.Opacity = h;
                }
                CaptureFrame(4);
            }
            CaptureFrame(24); // Brief hover pause

            // --- PHASE 5: Click PowerShell -> Flyout dismisses & Terminal springs open (0.4s @ 25 FPS = 10 frames) ---
            for (int i = 0; i < 10; i++)
            {
                double t = i / 9.0;
                SetCursor(psTileX, psTileY, pressed: i < 2);
                SetRipple(psTileX, psTileY, Math.Min(1.0, t * 1.4));
                if (i >= 2)
                {
                    double ft = (i - 2) / 7.0;
                    scene.HoverRing.Opacity = Math.Max(0, 1.0 - ft * 2.5);
                    SetFlyout(1.0 - ft);
                    SetTerminal(ft);
                }
                CaptureFrame(4);
            }
            SetRipple(0, 0, -1);
            SetFlyout(0);
            SetTerminal(1);

            // Hold on launched PowerShell window (1.4s)
            CaptureFrame(140);

            // --- PHASE 6: Clear terminal & move cursor aside for Global Hotkey demo (0.44s @ 25 FPS = 11 frames) ---
            SetStep("STEP 4 / 4", "Global Hotkey  •  Ctrl + Alt + D");
            for (int i = 1; i <= 11; i++)
            {
                double t = i / 11.0;
                double e = EaseInOutCubic(t);
                SetTerminal(1.0 - e);
                SetCursor(Lerp(psTileX, startX, e), Lerp(psTileY, startY, e));
                SetHotkeyHud(e, false);
                CaptureFrame(4);
            }
            CaptureFrame(35); // Brief pause showing hotkey HUD ready

            // --- PHASE 7: Hotkey presses down (amber glow) -> 260ms Flyout pops open hands-free (0.36s @ 25 FPS = 9 frames) ---
            for (int i = 0; i < 9; i++)
            {
                bool pressed = i < 5;
                SetHotkeyHud(1.0, pressed);
                double flyT = Math.Clamp((i - 1) / 6.5, 0, 1);
                SetFlyout(flyT);
                CaptureFrame(4);
            }
            SetHotkeyHud(1.0, false);
            SetFlyout(1.0);

            // Hold showing hotkey-triggered flyout (1.5s)
            CaptureFrame(150);

            // --- PHASE 8: Smoothly dismiss flyout & hotkey HUD back to starting frame for seamless loop (0.36s @ 25 FPS = 9 frames) ---
            for (int i = 1; i <= 9; i++)
            {
                double t = i / 9.0;
                SetFlyout(1.0 - t);
                SetHotkeyHud(1.0 - t, false);
                CaptureFrame(4);
            }
            SetFlyout(0);
            SetHotkeyHud(0, false);
            CaptureFrame(50); // Clean loop tail

            // 3. Assemble final looping GIF89a with exact per-frame timing
            WriteAnimatedGif(outFile, frames);

            Console.WriteLine($"DEMO_GIF_GENERATED_SUCCESS: {outFile} ({frames.Count} frames)");
        }

        private class DemoScene
        {
            public Grid Root;
            public Canvas Overlay;
            public Border TaskbarPill;
            public Border TaskbarIndicator;
            public Border PopupHost;
            public TranslateTransform PopupTranslate;
            public Border HoverRing;
            public Ellipse Ripple;
            public Border TerminalCard;
            public ScaleTransform TerminalScale;
            public Border HotkeyHud;
            public TranslateTransform HotkeyTranslate;
            public List<Border> Keycaps = new List<Border>();
            public TextBlock StepBadgeText;
            public TextBlock StepTitleText;
            public Grid Cursor;
            public ScaleTransform CursorScale;
        }

        private static DemoScene BuildScene(BitmapSource popBmp, int popW, int popH)
        {
            var s = new DemoScene();

            s.Root = new Grid
            {
                Width = W,
                Height = H,
                Background = new SolidColorBrush(Color.FromRgb(13, 17, 23)),
                ClipToBounds = true
            };

            // Top-left step caption pill
            Border stepPill = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(22, 18, 0, 0),
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(215, 18, 22, 30)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 245, 158, 11)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 7, 14, 7)
            };
            StackPanel stepSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            Border badgeBg = new Border
            {
                CornerRadius = new CornerRadius(5),
                Background = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                Padding = new Thickness(7, 2, 7, 2),
                Margin = new Thickness(0, 0, 10, 0)
            };
            s.StepBadgeText = new TextBlock
            {
                Text = "STEP 1 / 4",
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(18, 20, 21)),
                FontFamily = new FontFamily("Segoe UI")
            };
            badgeBg.Child = s.StepBadgeText;
            s.StepTitleText = new TextBlock
            {
                Text = "Click Pinned Taskbar Group",
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(243, 244, 246)),
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI")
            };
            stepSp.Children.Add(badgeBg);
            stepSp.Children.Add(s.StepTitleText);
            stepPill.Child = stepSp;
            s.Root.Children.Add(stepPill);

            // Top-right subtle brand watermark
            StackPanel brandMark = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 22, 24, 0),
                Opacity = 0.85
            };
            brandMark.Children.Add(Icons.BuildBrandSquircle(20));
            brandMark.Children.Add(new TextBlock
            {
                Text = "XAppGroup v1.2",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0),
                FontFamily = new FontFamily("Segoe UI")
            });
            s.Root.Children.Add(brandMark);

            // Mock launched Windows PowerShell terminal window (centered)
            s.TerminalScale = new ScaleTransform(0.92, 0.92);
            s.TerminalCard = new Border
            {
                Width = 460,
                Height = 250,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromRgb(12, 16, 23)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, -10, 0, 30),
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = s.TerminalScale,
                Opacity = 0,
                Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 28, Opacity = 0.65, ShadowDepth = 8 }
            };
            Grid termGrid = new Grid();
            termGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(34) });
            termGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Border termTitle = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(23, 29, 40)),
                CornerRadius = new CornerRadius(10, 10, 0, 0),
                BorderBrush = new SolidColorBrush(Color.FromRgb(38, 47, 62)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(12, 0, 12, 0)
            };
            DockPanel termDock = new DockPanel();
            StackPanel termLeft = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            termLeft.Children.Add(new Border
            {
                Width = 14, Height = 14, CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                Margin = new Thickness(0, 0, 8, 0),
                Child = new TextBlock { Text = ">_", FontSize = 8, FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
            });
            termLeft.Children.Add(new TextBlock
            {
                Text = "Windows PowerShell — Launched from Developer Tools",
                FontSize = 11.5, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                FontFamily = new FontFamily("Segoe UI")
            });
            termDock.Children.Add(termLeft);
            termTitle.Child = termDock;
            Grid.SetRow(termTitle, 0);
            termGrid.Children.Add(termTitle);

            StackPanel termBody = new StackPanel { Margin = new Thickness(16, 14, 16, 14) };
            termBody.Children.Add(new TextBlock
            {
                Text = "Windows PowerShell",
                FontFamily = new FontFamily("Consolas"), FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184))
            });
            termBody.Children.Add(new TextBlock
            {
                Text = "Copyright (C) Microsoft Corporation. All rights reserved.",
                FontFamily = new FontFamily("Consolas"), FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                Margin = new Thickness(0, 2, 0, 14)
            });
            termBody.Children.Add(new TextBlock
            {
                Text = "PS C:\\Projects\\XAppGroup> dotnet --version",
                FontFamily = new FontFamily("Consolas"), FontSize = 12.5,
                Foreground = new SolidColorBrush(Color.FromRgb(248, 250, 252))
            });
            termBody.Children.Add(new TextBlock
            {
                Text = "10.0.100",
                FontFamily = new FontFamily("Consolas"), FontSize = 12.5,
                Foreground = new SolidColorBrush(Color.FromRgb(251, 191, 36)),
                Margin = new Thickness(0, 4, 0, 10)
            });
            termBody.Children.Add(new TextBlock
            {
                Text = "PS C:\\Projects\\XAppGroup> ▌",
                FontFamily = new FontFamily("Consolas"), FontSize = 12.5,
                Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248))
            });
            Grid.SetRow(termBody, 1);
            termGrid.Children.Add(termBody);
            s.TerminalCard.Child = termGrid;
            s.Root.Children.Add(s.TerminalCard);

            // Global Hotkey HUD (top-center)
            s.HotkeyTranslate = new TranslateTransform(0, -14);
            s.HotkeyHud = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 68, 0, 0),
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromArgb(230, 15, 18, 24)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(110, 245, 158, 11)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 10, 14, 10),
                RenderTransform = s.HotkeyTranslate,
                Opacity = 0,
                Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 20, Opacity = 0.55, ShadowDepth = 6 }
            };
            StackPanel hkRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            hkRow.Children.Add(new TextBlock
            {
                Text = "⚡ GLOBAL HOTKEY",
                FontSize = 10.5, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(251, 191, 36)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0),
                FontFamily = new FontFamily("Segoe UI")
            });
            string[] keys = { "Ctrl", "Alt", "D" };
            for (int k = 0; k < keys.Length; k++)
            {
                if (k > 0)
                {
                    hkRow.Children.Add(new TextBlock
                    {
                        Text = "+", FontSize = 12, FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(5, 0, 5, 0)
                    });
                }
                Border cap = new Border
                {
                    MinWidth = 34,
                    CornerRadius = new CornerRadius(6),
                    Background = new SolidColorBrush(Color.FromRgb(24, 28, 35)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(64, 72, 86)),
                    BorderThickness = new Thickness(1, 1, 1, 2),
                    Padding = new Thickness(10, 4, 10, 4),
                    Child = new TextBlock
                    {
                        Text = keys[k], FontSize = 12, FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(243, 244, 246)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontFamily = new FontFamily("Segoe UI")
                    }
                };
                s.Keycaps.Add(cap);
                hkRow.Children.Add(cap);
            }
            s.HotkeyHud.Child = hkRow;
            s.Root.Children.Add(s.HotkeyHud);

            // Real PopupWindow Flyout Host anchored above taskbar icon
            s.PopupTranslate = new TranslateTransform(0, 26);
            s.PopupHost = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, TaskbarH + 10),
                RenderTransform = s.PopupTranslate,
                Opacity = 0,
                Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 26, Opacity = 0.6, ShadowDepth = 8 },
                Child = popBmp != null
                    ? new Image { Source = popBmp, Width = popW, Height = popH, Stretch = Stretch.Uniform }
                    : (UIElement)new Border { Width = popW, Height = popH, Background = Brushes.Black }
            };
            s.Root.Children.Add(s.PopupHost);

            // Windows 11 Taskbar at bottom
            Border taskbar = new Border
            {
                Height = TaskbarH,
                VerticalAlignment = VerticalAlignment.Bottom,
                Background = new SolidColorBrush(Color.FromArgb(240, 18, 22, 30)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(40, 48, 62)),
                BorderThickness = new Thickness(0, 1, 0, 0)
            };
            Grid tbGrid = new Grid();

            // Centered taskbar icons (XAppGroup pinned icon sits cleanly at X = 440)
            StackPanel tbCenter = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            tbCenter.Children.Add(MakeWinStartIcon());
            tbCenter.Children.Add(MakeTaskbarDot("#94A3B8"));
            tbCenter.Children.Add(MakeTaskbarDot("#60A5FA"));

            // Pinned XAppGroup Icon (center element: 3 on left, 3 on right!)
            s.TaskbarPill = new Border
            {
                Width = 42, Height = 38,
                CornerRadius = new CornerRadius(7),
                Background = new SolidColorBrush(Color.FromArgb(18, 245, 158, 11)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 245, 158, 11)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(4, 0, 4, 0)
            };
            Grid pillGrid = new Grid();
            pillGrid.Children.Add(Icons.BuildBrandSquircle(24));
            s.TaskbarIndicator = new Border
            {
                Width = 6, Height = 2.5,
                CornerRadius = new CornerRadius(1.2),
                Background = new SolidColorBrush(Color.FromRgb(251, 191, 36)),
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 2)
            };
            pillGrid.Children.Add(s.TaskbarIndicator);
            s.TaskbarPill.Child = pillGrid;
            tbCenter.Children.Add(s.TaskbarPill);

            tbCenter.Children.Add(MakeTaskbarDot("#A78BFA"));
            tbCenter.Children.Add(MakeTaskbarDot("#34D399"));
            tbCenter.Children.Add(MakeTaskbarDot("#F472B6"));

            tbGrid.Children.Add(tbCenter);

            // System tray clock on the right
            StackPanel tray = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 18, 0)
            };
            tray.Children.Add(new TextBlock
            {
                Text = "12:00 PM", FontSize = 10.5, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                HorizontalAlignment = HorizontalAlignment.Right,
                FontFamily = new FontFamily("Segoe UI")
            });
            tray.Children.Add(new TextBlock
            {
                Text = "Windows 11", FontSize = 9.5,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                HorizontalAlignment = HorizontalAlignment.Right,
                FontFamily = new FontFamily("Segoe UI")
            });
            tbGrid.Children.Add(tray);
            taskbar.Child = tbGrid;
            s.Root.Children.Add(taskbar);

            // Top overlay canvas for hover ring, click ripple, and mouse cursor
            s.Overlay = new Canvas { Width = W, Height = H };

            s.HoverRing = new Border
            {
                Width = 56, Height = 56,
                CornerRadius = new CornerRadius(12),
                BorderBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                BorderThickness = new Thickness(2),
                Background = new SolidColorBrush(Color.FromArgb(30, 245, 158, 11)),
                Opacity = 0
            };
            s.Overlay.Children.Add(s.HoverRing);

            s.Ripple = new Ellipse
            {
                Width = 20, Height = 20,
                Stroke = new SolidColorBrush(Color.FromRgb(251, 191, 36)),
                StrokeThickness = 2.5,
                Fill = new SolidColorBrush(Color.FromArgb(45, 245, 158, 11)),
                Opacity = 0
            };
            s.Overlay.Children.Add(s.Ripple);

            // Crisp vector Windows mouse cursor
            s.CursorScale = new ScaleTransform(1.0, 1.0);
            s.Cursor = new Grid
            {
                RenderTransform = s.CursorScale,
                Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 6, Opacity = 0.65, ShadowDepth = 2 }
            };
            s.Cursor.Children.Add(new WpfPath
            {
                Data = Geometry.Parse("M 0,0 L 0,21 L 5.2,16.2 L 9.2,23.5 L 12.2,22.0 L 8.2,14.8 L 15.0,14.8 Z"),
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                StrokeThickness = 1.5,
                StrokeLineJoin = PenLineJoin.Round
            });
            s.Overlay.Children.Add(s.Cursor);

            s.Root.Children.Add(s.Overlay);
            return s;
        }

        private static Border MakeWinStartIcon()
        {
            Grid g = new Grid { Width = 16, Height = 16 };
            g.RowDefinitions.Add(new RowDefinition()); g.RowDefinitions.Add(new RowDefinition());
            g.ColumnDefinitions.Add(new ColumnDefinition()); g.ColumnDefinitions.Add(new ColumnDefinition());
            var blue = new SolidColorBrush(Color.FromRgb(56, 189, 248));
            for (int r = 0; r < 2; r++)
                for (int c = 0; c < 2; c++)
                {
                    var p = new Border { Background = blue, Margin = new Thickness(0.8), CornerRadius = new CornerRadius(1) };
                    Grid.SetRow(p, r); Grid.SetColumn(p, c);
                    g.Children.Add(p);
                }
            return new Border { Width = 36, Height = 36, Margin = new Thickness(2, 0, 2, 0), Child = g };
        }

        private static Border MakeTaskbarDot(string hex)
        {
            return new Border
            {
                Width = 36, Height = 36,
                Margin = new Thickness(2, 0, 2, 0),
                Child = new Border
                {
                    Width = 16, Height = 16, CornerRadius = new CornerRadius(5),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)),
                    Opacity = 0.85
                }
            };
        }

        private static double EaseInOutCubic(double t) =>
            t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;

        private static double Lerp(double a, double b, double t) => a + (b - a) * t;

        private static void Pump(Window win, int waitMs)
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

        /// <summary>
        /// Assembles individual WPF-encoded GIF frames into a single infinite-looping GIF89a
        /// with exact per-frame centisecond delays and per-frame Local Color Tables.
        /// </summary>
        private static void WriteAnimatedGif(string path, List<(byte[] GifBytes, ushort DelayCs)> frames)
        {
            if (frames.Count == 0) return;

            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                // Header (GIF89a) + Logical Screen Descriptor (no GCT, each frame carries its own LCT)
                byte[] header = new byte[13];
                Array.Copy(frames[0].GifBytes, 0, header, 0, 13);
                header[4] = (byte)'9'; // Ensure GIF89a
                header[10] = (byte)(header[10] & 0x70); // Clear Global Color Table flag (bit 7)
                fs.Write(header, 0, 13);

                // NETSCAPE2.0 Application Extension for infinite looping
                byte[] netscape =
                {
                    0x21, 0xFF, 0x0B,
                    (byte)'N', (byte)'E', (byte)'T', (byte)'S', (byte)'C', (byte)'A', (byte)'P', (byte)'E', (byte)'2', (byte)'.', (byte)'0',
                    0x03, 0x01, 0x00, 0x00, 0x00
                };
                fs.Write(netscape, 0, netscape.Length);

                foreach (var (buf, delayCs) in frames)
                {
                    // Graphic Control Extension (disposal=1, userInput=0, transparent=0)
                    byte[] gce =
                    {
                        0x21, 0xF9, 0x04, 0x04,
                        (byte)(delayCs & 0xFF),
                        (byte)((delayCs >> 8) & 0xFF),
                        0x00, 0x00
                    };
                    fs.Write(gce, 0, gce.Length);

                    // Parse single-frame GIF to extract its GCT (to use as LCT) and Image Descriptor + LZW data
                    int pos = 13;
                    bool hasGct = (buf[10] & 0x80) != 0;
                    int gctBits = buf[10] & 0x07;
                    int gctSize = hasGct ? 3 * (1 << (gctBits + 1)) : 0;
                    int gctOffset = pos;
                    pos += gctSize;

                    // Skip any extension blocks before 0x2C Image Descriptor
                    while (pos < buf.Length && buf[pos] == 0x21)
                    {
                        pos += 2; // skip 0x21 + label
                        while (pos < buf.Length && buf[pos] != 0)
                            pos += buf[pos] + 1;
                        pos++; // skip 0x00 terminator
                    }

                    if (pos >= buf.Length || buf[pos] != 0x2C) continue;

                    // Write 10-byte Image Descriptor with LCT flag set if GCT existed
                    byte[] desc = new byte[10];
                    Array.Copy(buf, pos, desc, 0, 10);
                    bool hasLct = (desc[9] & 0x80) != 0;
                    pos += 10;

                    if (!hasLct && hasGct)
                    {
                        desc[9] = (byte)(desc[9] | 0x80 | gctBits);
                        fs.Write(desc, 0, 10);
                        fs.Write(buf, gctOffset, gctSize);
                    }
                    else
                    {
                        fs.Write(desc, 0, 10);
                        if (hasLct)
                        {
                            int lctSize = 3 * (1 << ((desc[9] & 0x07) + 1));
                            fs.Write(buf, pos, lctSize);
                            pos += lctSize;
                        }
                    }

                    // Write LZW Minimum Code Size + sub-blocks up to 0x00 terminator
                    int dataStart = pos;
                    pos++; // LZW min code size
                    while (pos < buf.Length && buf[pos] != 0)
                        pos += buf[pos] + 1;
                    pos++; // include 0x00 terminator
                    fs.Write(buf, dataStart, pos - dataStart);
                }

                // GIF Trailer
                fs.WriteByte(0x3B);
            }
        }
    }
}
