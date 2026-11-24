using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace XAppGroup
{
    /// <summary>
    /// Periodic "Support Us" motivation popup styled to match Dark Acrylic and Light Glass themes.
    /// </summary>
    public class SupportDialog : Window
    {
        public SupportDialog(Window owner = null)
        {
            Title = "Support Us";
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

            var heartBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FB7185"));
            heartBrush.Freeze();

            // --- Top Body Section (matches Theme.Card / PopupBg) ---
            var titleRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            var heartIcon = Lucide.Make(Lucide.Heart, heartBrush, 20, fill: true, strokeWidth: 1.8);
            heartIcon.Margin = new Thickness(0, 0, 10, 0);
            titleRow.Children.Add(heartIcon);

            var titleTxt = new TextBlock
            {
                Text = "Support Us",
                FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI"),
                FontSize = 19,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            titleTxt.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Fg"));
            titleRow.Children.Add(titleTxt);

            var msgTxt = new TextBlock
            {
                Text = "Thanks for using XAppGroup!\nIf you find it useful, your support is greatly appreciated.",
                FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI"),
                FontSize = 13,
                LineHeight = 20,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 12, 0, 0)
            };
            msgTxt.SetBinding(TextBlock.ForegroundProperty, Theme.Bind("Sub"));

            var bodyStack = new StackPanel
            {
                Margin = new Thickness(24, 22, 24, 22)
            };
            bodyStack.Children.Add(titleRow);
            bodyStack.Children.Add(msgTxt);

            // --- Bottom Footer Section (matches Theme.StatsBg with 2 equal-width buttons) ---
            var laterBtn = Theme.Btn("Later", (s, e) => Close());
            laterBtn.IsCancel = true;
            laterBtn.Margin = new Thickness(0, 0, 6, 0);
            laterBtn.Padding = new Thickness(14, 9, 14, 9);
            laterBtn.FontSize = 13;

            var supportBtn = Theme.Btn("Support Us", (s, e) =>
            {
                var cfg = AppConfig.Reload();
                AppConfig.OpenUrl(cfg.SupportUrl);
                Close();
            }, primary: true);
            supportBtn.IsDefault = true;
            supportBtn.Margin = new Thickness(6, 0, 0, 0);
            supportBtn.Padding = new Thickness(14, 9, 14, 9);
            supportBtn.FontSize = 13;

            var btnGrid = new UniformGrid { Columns = 2 };
            btnGrid.Children.Add(laterBtn);
            btnGrid.Children.Add(supportBtn);

            var footerBorder = new Border
            {
                BorderThickness = new Thickness(0, 1, 0, 0),
                CornerRadius = new CornerRadius(0, 0, 14, 14),
                Padding = new Thickness(24, 16, 24, 16),
                Child = btnGrid
            };
            footerBorder.SetBinding(Border.BackgroundProperty, Theme.Bind("StatsBg"));
            footerBorder.SetBinding(Border.BorderBrushProperty, Theme.Bind("Divider"));

            var rootStack = new StackPanel();
            rootStack.Children.Add(bodyStack);
            rootStack.Children.Add(footerBorder);

            var card = new Border
            {
                CornerRadius = new CornerRadius(14),
                BorderThickness = new Thickness(1),
                MinWidth = 400,
                MaxWidth = 440,
                Margin = new Thickness(14),
                Child = rootStack,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 24,
                    Opacity = Theme.Dark ? 0.55 : 0.22,
                    ShadowDepth = 6
                }
            };
            card.SetBinding(Border.BackgroundProperty, Theme.Bind("Card"));
            card.SetBinding(Border.BorderBrushProperty, Theme.Bind("CardBorder"));
            card.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                    try { DragMove(); } catch { }
            };

            Content = card;
            KeyDown += (s, e) => { if (e.Key == Key.Escape) Close(); };
        }

        public static void ShowPrompt(Window owner = null)
        {
            var dlg = new SupportDialog(owner);
            dlg.ShowDialog();
        }
    }
}
