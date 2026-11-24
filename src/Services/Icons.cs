using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace XAppGroup
{
    public static class Icons
    {
        static readonly ConcurrentDictionary<string, BitmapSource> Cache = new ConcurrentDictionary<string, BitmapSource>(StringComparer.OrdinalIgnoreCase);

        public static Bitmap ExtractBitmap(string path)
        {
            try
            {
                if (Directory.Exists(path) || File.Exists(path))
                {
                    using (var ico = Icon.ExtractAssociatedIcon(path))
                        if (ico != null) return ico.ToBitmap();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Icons] ExtractBitmap failed for '{path}': {ex.Message}");
            }
            return SystemIcons.Application.ToBitmap();
        }

        public static BitmapSource Get(string path)
        {
            if (string.IsNullOrEmpty(path)) path = Environment.ProcessPath ?? "";
            return Cache.GetOrAdd(path, p =>
            {
                BitmapSource bs;
                try
                {
                    using (var ico = Icon.ExtractAssociatedIcon(p))
                    {
                        if (ico != null)
                            bs = Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        else
                            bs = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Application.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    }
                    bs.Freeze();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Icons] Get failed for '{p}': {ex.Message}");
                    bs = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Application.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    bs.Freeze();
                }
                return bs;
            });
        }

        /// <summary>Group icon for display: custom IconPath if valid, otherwise the generated .ico.</summary>
        public static BitmapSource GroupIcon(GroupData g)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(g.IconPath) && File.Exists(g.IconPath))
                {
                    var c = FromFile(g.IconPath);
                    if (c != null) return c;
                }
                return FromFile(BuildGroupIcon(g)) ?? Get(Environment.ProcessPath ?? "");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Icons] GroupIcon failed for '{g.GroupName}': {ex.Message}");
                return Get(Environment.ProcessPath ?? "");
            }
        }

        /// <summary>Loads an icon/image file (.ico/.png/...) or extracts the icon of an .exe/.dll/.lnk. Returns null on failure.</summary>
        public static BitmapSource FromFile(string file)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(file) || !File.Exists(file)) return null;
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext == ".exe" || ext == ".dll" || ext == ".lnk") return Get(file);
                byte[] bytes = File.ReadAllBytes(file);
                using var ms = new MemoryStream(bytes);
                var dec = BitmapDecoder.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                var bs = dec.Frames.OrderByDescending(f => f.PixelWidth).FirstOrDefault();
                bs?.Freeze();
                return bs;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Icons] FromFile failed for '{file}': {ex.Message}");
                return null;
            }
        }

        /// <summary>Renders up to four app icons as a 2x2 grid and writes a PNG-based .ico file.</summary>
        public static string BuildGroupIcon(GroupData g)
        {
            Directory.CreateDirectory(Store.IconDir);
            const int size = 256;
            using (var bmp = new Bitmap(size, size))
            using (var gr = Graphics.FromImage(bmp))
            {
                gr.SmoothingMode = SmoothingMode.AntiAlias;
                gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gr.Clear(Color.Transparent);
                var bgColor = Theme.Dark ? Color.FromArgb(200, 32, 32, 36) : Color.FromArgb(245, 255, 255, 255);
                using (var bg = new SolidBrush(bgColor))
                using (var path = RoundRect(8, 8, size - 16, size - 16, 44))
                {
                    gr.FillPath(bg, path);
                    if (!Theme.Dark)
                    {
                        using var borderPen = new Pen(Color.FromArgb(40, 0, 0, 0), 3);
                        gr.DrawPath(borderPen, path);
                    }
                }
                var items = g.OrderedPaths().Take(4).ToList();
                int cell = 100, gap = 12, off = (size - (cell * 2 + gap)) / 2;
                for (int i = 0; i < items.Count; i++)
                {
                    using (var ib = ExtractBitmap(items[i]))
                    {
                        if (ib != null)
                            gr.DrawImage(ib, off + (i % 2) * (cell + gap), off + (i / 2) * (cell + gap), cell, cell);
                    }
                }
                var file = Path.Combine(Store.IconDir, g.Id + ".ico");
                WriteIco(bmp, file);
                return file;
            }
        }

        /// <summary>Generates in-memory BitmapSource for 2x2 composite group icon in real time.</summary>
        public static BitmapSource CreateCompositeBitmap(IEnumerable<string> paths)
        {
            try
            {
                const int size = 256;
                using var bmp = new Bitmap(size, size);
                using (var gr = Graphics.FromImage(bmp))
                {
                    gr.SmoothingMode = SmoothingMode.AntiAlias;
                    gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    gr.Clear(Color.Transparent);

                    var bgColor = Theme.Dark ? Color.FromArgb(200, 32, 32, 36) : Color.FromArgb(245, 255, 255, 255);
                    using (var bg = new SolidBrush(bgColor))
                    using (var path = RoundRect(8, 8, size - 16, size - 16, 44))
                    {
                        gr.FillPath(bg, path);
                        if (!Theme.Dark)
                        {
                            using var borderPen = new Pen(Color.FromArgb(40, 0, 0, 0), 3);
                            gr.DrawPath(borderPen, path);
                        }
                    }

                    var items = (paths ?? Enumerable.Empty<string>()).Take(4).ToList();
                    int cell = 100, gap = 12, off = (size - (cell * 2 + gap)) / 2;
                    for (int i = 0; i < items.Count; i++)
                    {
                        using var ib = ExtractBitmap(items[i]);
                        if (ib != null)
                            gr.DrawImage(ib, off + (i % 2) * (cell + gap), off + (i / 2) * (cell + gap), cell, cell);
                    }
                }

                using var ms = new MemoryStream();
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                var dec = BitmapDecoder.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                var bs = dec.Frames.FirstOrDefault();
                bs?.Freeze();
                return bs;
            }
            catch
            {
                return Get(Environment.ProcessPath ?? "");
            }
        }

        static GraphicsPath RoundRect(int x, int y, int w, int h, int r)
        {
            var p = new GraphicsPath();
            p.AddArc(x, y, r, r, 180, 90);
            p.AddArc(x + w - r, y, r, r, 270, 90);
            p.AddArc(x + w - r, y + h - r, r, r, 0, 90);
            p.AddArc(x, y + h - r, r, r, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void WriteIco(Bitmap bmp, string file)
        {
            byte[] png;
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                png = ms.ToArray();
            }
            using (var fs = new FileStream(file, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write((short)0); bw.Write((short)1); bw.Write((short)1);
                bw.Write((byte)0); bw.Write((byte)0); bw.Write((byte)0); bw.Write((byte)0);
                bw.Write((short)1); bw.Write((short)32);
                bw.Write(png.Length); bw.Write(22);
                bw.Write(png);
            }
        }

        /// <summary>Creates the unified brand squircle UIElement (Obsidian container with amber gradient border and 2x2 mini app matrix).</summary>
        public static System.Windows.Controls.Border BuildBrandSquircle(double size = 20.0)
        {
            var brandGrad = new System.Windows.Media.LinearGradientBrush
            {
                StartPoint = new System.Windows.Point(0, 0),
                EndPoint = new System.Windows.Point(1, 1)
            };
            brandGrad.GradientStops.Add(new System.Windows.Media.GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FBBF24"), 0.0));
            brandGrad.GradientStops.Add(new System.Windows.Media.GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#D97706"), 1.0));

            var squircle = new System.Windows.Controls.Border
            {
                Width = size,
                Height = size,
                CornerRadius = new System.Windows.CornerRadius(Math.Max(3, size * 0.25)),
                Background = new System.Windows.Media.LinearGradientBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#242830"),
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#121418"),
                    90.0),
                BorderBrush = brandGrad,
                BorderThickness = new System.Windows.Thickness(Math.Max(1.0, size * 0.05)),
                SnapsToDevicePixels = true,
                UseLayoutRounding = true
            };

            var grid = new System.Windows.Controls.Grid
            {
                Width = size * 0.65,
                Height = size * 0.65,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });

            double m = Math.Max(0.5, size * 0.025);
            double cr = Math.Max(1.2, size * 0.08);

            // Tile 1: Top-Left (Glowing Amber Accent)
            var t1 = new System.Windows.Controls.Border
            {
                Margin = new System.Windows.Thickness(m),
                CornerRadius = new System.Windows.CornerRadius(cr),
                Background = brandGrad
            };
            System.Windows.Controls.Grid.SetRow(t1, 0);
            System.Windows.Controls.Grid.SetColumn(t1, 0);
            grid.Children.Add(t1);

            // Tile 2: Top-Right (Slate Glass)
            var t2 = new System.Windows.Controls.Border
            {
                Margin = new System.Windows.Thickness(m),
                CornerRadius = new System.Windows.CornerRadius(cr),
                Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3A4250")),
                BorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#556070")),
                BorderThickness = new System.Windows.Thickness(Math.Max(0.5, size * 0.02))
            };
            System.Windows.Controls.Grid.SetRow(t2, 0);
            System.Windows.Controls.Grid.SetColumn(t2, 1);
            grid.Children.Add(t2);

            // Tile 3: Bottom-Left (Charcoal Glass)
            var t3 = new System.Windows.Controls.Border
            {
                Margin = new System.Windows.Thickness(m),
                CornerRadius = new System.Windows.CornerRadius(cr),
                Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2F3642")),
                BorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#455060")),
                BorderThickness = new System.Windows.Thickness(Math.Max(0.5, size * 0.02))
            };
            System.Windows.Controls.Grid.SetRow(t3, 1);
            System.Windows.Controls.Grid.SetColumn(t3, 0);
            grid.Children.Add(t3);

            // Tile 4: Bottom-Right (Amber Accent Outline)
            var t4 = new System.Windows.Controls.Border
            {
                Margin = new System.Windows.Thickness(m),
                CornerRadius = new System.Windows.CornerRadius(cr),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 245, 158, 11)),
                BorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F59E0B")),
                BorderThickness = new System.Windows.Thickness(Math.Max(0.6, size * 0.025))
            };
            System.Windows.Controls.Grid.SetRow(t4, 1);
            System.Windows.Controls.Grid.SetColumn(t4, 1);
            grid.Children.Add(t4);

            squircle.Child = grid;
            return squircle;
        }
    }
}
