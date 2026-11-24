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
    public class ThemedDialog : Window
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        public ThemedDialog(Window owner, string message, string title, MessageBoxButton buttons, MessageBoxImage image)
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            SizeToContent = SizeToContent.WidthAndHeight;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;

            if (owner != null && owner.IsVisible)
            {
                Owner = owner;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            var card = new Border
            {
                CornerRadius = new CornerRadius(14),
                BorderThickness = new Thickness(1),
                MinWidth = 360,
                MaxWidth = 440,
                Padding = new Thickness(24, 22, 24, 20),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 24,
                    Opacity = Theme.Dark ? 0.55 : 0.20,
                    ShadowDepth = 6
                }
            };
            card.SetBinding(Border.BackgroundProperty, Theme.Bind("PopupBg"));
            card.SetBinding(Border.BorderBrushProperty, Theme.Bind("CardBorder"));
            card.MouseLeftButtonDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };

            bool isDelete = title != null && (title.IndexOf("Delete", StringComparison.OrdinalIgnoreCase) >= 0 || title.IndexOf("Remove", StringComparison.OrdinalIgnoreCase) >= 0);
            bool isError = image == MessageBoxImage.Error || image == MessageBoxImage.Hand || image == MessageBoxImage.Stop || (title != null && title.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0);
            bool isWarning = image == MessageBoxImage.Warning || image == MessageBoxImage.Exclamation;
            bool isDanger = isDelete || isError;

            var badge = new Border
            {
                Width = 38,
                Height = 38,
                CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 14, 0)
            };

            string iconPath;
            string brushProp;
            if (isDanger)
            {
                badge.SetBinding(Border.BackgroundProperty, Theme.Bind("DangerBg"));
                badge.SetBinding(Border.BorderBrushProperty, Theme.Bind("DangerBorder"));
                iconPath = isDelete ? Lucide.Trash2 : Lucide.AlertTriangle;
                brushProp = "DangerFg";
            }
            else if (isWarning)
            {
                badge.SetBinding(Border.BackgroundProperty, Theme.Bind("CyanBadgeBg"));
                badge.SetBinding(Border.BorderBrushProperty, Theme.Bind("CyanBadgeBorder"));
                iconPath = Lucide.AlertTriangle;
                brushProp = "CyanBadgeFg";
            }
            else
            {
                badge.SetBinding(Border.BackgroundProperty, Theme.Bind("EmeraldBg"));
                badge.SetBinding(Border.BorderBrushProperty, Theme.Bind("EmeraldBorder"));
                iconPath = Lucide.Check;
                brushProp = "EmeraldFg";
            }

            var geom = Geometry.Parse(iconPath);
            geom.Freeze();
            var path = new WpfPath
            {
                Data = geom,
                Fill = Brushes.Transparent,
                StrokeThickness = 2.2,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round
            };
            path.SetBinding(Shape.StrokeProperty, Theme.Bind(brushProp));
            var canvas = new Canvas { Width = 24, Height = 24 };
            canvas.Children.Add(path);
            badge.Child = new Viewbox
            {
                Width = 18,
                Height = 18,
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Child = canvas
            };

            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 14) };
            headerPanel.Children.Add(badge);

            var headerTextPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var titleBlock = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(title) ? "XAppGroup" : title,
                FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI"),
                FontSize = 14.5,
                FontWeight = FontWeights.SemiBold
            };
            titleBlock.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Fg"));
            headerTextPanel.Children.Add(titleBlock);
            headerPanel.Children.Add(headerTextPanel);

            var msgBlock = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI"),
                FontSize = 12.5,
                LineHeight = 18,
                Margin = new Thickness(0, 0, 0, 20)
            };
            msgBlock.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Sub"));

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            if (buttons == MessageBoxButton.YesNo)
            {
                Result = MessageBoxResult.No;
                var noBtn = Theme.Btn("Cancel", (s, e) => { Result = MessageBoxResult.No; DialogResult = false; Close(); });
                noBtn.IsCancel = true;
                noBtn.Margin = new Thickness(0, 0, 8, 0);
                buttonPanel.Children.Add(noBtn);

                if (isDelete)
                {
                    var yesBtn = Theme.Btn("Delete", (s, e) => { Result = MessageBoxResult.Yes; DialogResult = true; Close(); }, variant: "danger", iconPath: Lucide.Trash2);
                    yesBtn.SetBinding(Control.BackgroundProperty, Theme.Bind("DangerBg"));
                    yesBtn.SetBinding(Control.BorderBrushProperty, Theme.Bind("DangerBorder"));
                    yesBtn.SetBinding(Control.ForegroundProperty, Theme.Bind("DangerFg"));
                    yesBtn.IsDefault = true;
                    buttonPanel.Children.Add(yesBtn);
                }
                else
                {
                    var yesBtn = Theme.Btn("Yes", (s, e) => { Result = MessageBoxResult.Yes; DialogResult = true; Close(); }, primary: true);
                    yesBtn.IsDefault = true;
                    buttonPanel.Children.Add(yesBtn);
                }
            }
            else if (buttons == MessageBoxButton.OKCancel)
            {
                Result = MessageBoxResult.Cancel;
                var cancelBtn = Theme.Btn("Cancel", (s, e) => { Result = MessageBoxResult.Cancel; DialogResult = false; Close(); });
                cancelBtn.IsCancel = true;
                cancelBtn.Margin = new Thickness(0, 0, 8, 0);
                buttonPanel.Children.Add(cancelBtn);

                var okBtn = Theme.Btn("OK", (s, e) => { Result = MessageBoxResult.OK; DialogResult = true; Close(); }, primary: true);
                okBtn.IsDefault = true;
                buttonPanel.Children.Add(okBtn);
            }
            else
            {
                Result = MessageBoxResult.OK;
                var okBtn = Theme.Btn("OK", (s, e) => { Result = MessageBoxResult.OK; DialogResult = true; Close(); }, primary: true);
                okBtn.IsDefault = true;
                okBtn.IsCancel = true;
                buttonPanel.Children.Add(okBtn);
            }

            var root = new StackPanel();
            root.Children.Add(headerPanel);
            root.Children.Add(msgBlock);
            root.Children.Add(buttonPanel);
            card.Child = root;
            Content = card;

            KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    if (buttons == MessageBoxButton.YesNo) Result = MessageBoxResult.No;
                    else if (buttons == MessageBoxButton.OKCancel) Result = MessageBoxResult.Cancel;
                    else Result = MessageBoxResult.OK;
                    Close();
                }
            };
        }

        public static MessageBoxResult Show(Window owner, string message, string title = "XAppGroup", MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
        {
            var dlg = new ThemedDialog(owner, message, title, buttons, image);
            dlg.ShowDialog();
            return dlg.Result;
        }

        public static MessageBoxResult Show(string message, string title = "XAppGroup", MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
        {
            return Show(null, message, title, buttons, image);
        }
    }
}
