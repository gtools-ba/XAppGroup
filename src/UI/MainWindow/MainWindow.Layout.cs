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
        public string GetRawMonitorDebugInfo()
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                IntPtr hMon = hwnd != IntPtr.Zero
                    ? MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST)
                    : MonitorFromPoint(new POINT { x = 0, y = 0 }, MONITOR_DEFAULTTONEAREST);
                var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
                bool ok = GetMonitorInfo(hMon, ref mi);
                return $"hMon={hMon} ok={ok} rcMon=({mi.rcMonitor.Left},{mi.rcMonitor.Top},{mi.rcMonitor.Right},{mi.rcMonitor.Bottom}) rcWork=({mi.rcWork.Left},{mi.rcWork.Top},{mi.rcWork.Right},{mi.rcWork.Bottom})";
            }
            catch (Exception ex)
            {
                return $"ERR:{ex.GetType().Name}:{ex.Message}";
            }
        }

        public Rect GetMonitorWorkAreaDip()
        {
            var (dpiX, dpiY) = GetEffectiveDpiScale();
            if (_simulatedWorkAreaPx.HasValue)
            {
                var px = _simulatedWorkAreaPx.Value;
                return new Rect(px.Left / dpiX, px.Top / dpiY, px.Width / dpiX, px.Height / dpiY);
            }

            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                IntPtr hMon;
                if (hwnd != IntPtr.Zero)
                {
                    hMon = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                }
                else
                {
                    GetCursorPos(out var pt);
                    hMon = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
                }

                if (hMon != IntPtr.Zero)
                {
                    var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
                    if (GetMonitorInfo(hMon, ref mi))
                    {
                        double rawW = Math.Max(320, mi.rcWork.Right - mi.rcWork.Left);
                        double rawH = Math.Max(280, mi.rcWork.Bottom - mi.rcWork.Top);
                        // Divide physical pixels by DPI scale once (physical px / scale = DIP).
                        return new Rect(mi.rcWork.Left / dpiX, mi.rcWork.Top / dpiY, rawW / dpiX, rawH / dpiY);
                    }
                }
            }
            catch { }

            var wa = SystemParameters.WorkArea;
            return new Rect(wa.Left, wa.Top, wa.Width, wa.Height);
        }

        public void ApplySimulatedDisplay(Rect? workAreaPx, double? dpiScale)
        {
            _simulatedWorkAreaPx = workAreaPx;
            _simulatedDpiScale = dpiScale;
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                double targetScale = dpiScale ?? (GetDpiForWindow(hwnd) / 96.0);
                int targetDpi = (int)Math.Round(targetScale * 96.0);
                var waDip = GetMonitorWorkAreaDip();
                double wDip = Math.Max(DesignMinWidthDip, Math.Min(DesignWidthDip, Math.Floor(waDip.Width - WorkAreaMarginDip)));
                double hDip = Math.Max(DesignMinHeightDip, Math.Min(DesignHeightDip, Math.Floor(waDip.Height - WorkAreaMarginDip)));
                double lDip = Math.Round(waDip.Left + (waDip.Width - wDip) / 2.0);
                double tDip = Math.Round(waDip.Top + (waDip.Height - hDip) / 2.0);
                var suggested = new RECT
                {
                    Left = (int)Math.Round(lDip * targetScale),
                    Top = (int)Math.Round(tDip * targetScale),
                    Right = (int)Math.Round((lDip + wDip) * targetScale),
                    Bottom = (int)Math.Round((tDip + hDip) * targetScale)
                };
                IntPtr wParam = new IntPtr((targetDpi << 16) | (targetDpi & 0xFFFF));
                SendMessage(hwnd, WM_DPICHANGED, wParam, ref suggested);
            }
            Width = DesignWidthDip;
            Height = DesignHeightDip;
            ClampToMonitorWorkArea(recenter: true);
        }

        public void ClampToMonitorWorkArea(bool recenter = false)
        {
            var waDip = GetMonitorWorkAreaDip();
            ApplyWorkAreaClamp(waDip, recenter);
        }

        public void ApplyWorkAreaClamp(Rect workAreaDip, bool recenter = false)
        {
            double availW = Math.Max(320.0, Math.Floor(workAreaDip.Width - WorkAreaMarginDip));
            double availH = Math.Max(280.0, Math.Floor(workAreaDip.Height - WorkAreaMarginDip));

            MinWidth = Math.Min(DesignMinWidthDip, availW);
            MinHeight = Math.Min(DesignMinHeightDip, availH);

            if (WindowState == WindowState.Maximized)
            {
                MaxWidth = Math.Floor(workAreaDip.Width);
                MaxHeight = Math.Floor(workAreaDip.Height);
                UpdateResponsiveLayout(MaxWidth, MaxHeight);
                return;
            }

            double desiredW = (recenter || double.IsNaN(Width) || Width <= 0) ? DesignWidthDip : Width;
            double desiredH = (recenter || double.IsNaN(Height) || Height <= 0) ? DesignHeightDip : Height;

            Width = Math.Max(MinWidth, Math.Min(desiredW, availW));
            Height = Math.Max(MinHeight, Math.Min(desiredH, availH));

            double margin = WorkAreaMarginDip / 2.0;
            if (recenter || double.IsNaN(Left) || double.IsNaN(Top))
            {
                Left = Math.Round(workAreaDip.Left + (workAreaDip.Width - Width) / 2.0);
                Top = Math.Round(workAreaDip.Top + (workAreaDip.Height - Height) / 2.0);
            }
            else
            {
                if (Left + Width > workAreaDip.Right - margin) Left = Math.Round(workAreaDip.Right - margin - Width);
                if (Top + Height > workAreaDip.Bottom - margin) Top = Math.Round(workAreaDip.Bottom - margin - Height);
                if (Left < workAreaDip.Left + margin) Left = Math.Round(workAreaDip.Left + margin);
                if (Top < workAreaDip.Top + margin) Top = Math.Round(workAreaDip.Top + margin);
            }

            _restoreBoundsDip = new Rect(Left, Top, Width, Height);
            UpdateResponsiveLayout(Width, Height);
        }

        public void UpdateResponsiveLayout(double widthDip, double heightDip)
        {
            double w = widthDip > 0 && !double.IsNaN(widthDip) && !double.IsInfinity(widthDip) ? widthDip : Width;
            double h = heightDip > 0 && !double.IsNaN(heightDip) && !double.IsInfinity(heightDip) ? heightDip : Height;

            bool shortHeight = h <= ShortHeightBreakpointDip;
            if (_tipBanner != null)
            {
                _tipBanner.Visibility = (shortHeight || _tipDismissedByUser) ? Visibility.Collapsed : Visibility.Visible;
            }
            if (_statsInnerDock != null)
            {
                _statsInnerDock.Margin = shortHeight ? new Thickness(16, 4, 16, 4) : new Thickness(24, 9, 24, 9);
            }
            if (_footerDock != null)
            {
                _footerDock.Margin = shortHeight
                    ? new Thickness(w < 760 ? 12 : 16, 8, w < 760 ? 12 : 16, 8)
                    : new Thickness(w < 760 ? 12 : 18, 11, w < 760 ? 12 : 18, 11);
            }
            if (_footerRightBar != null)
            {
                if (w < 640)
                {
                    DockPanel.SetDock(_footerRightBar, Dock.Bottom);
                    _footerRightBar.HorizontalAlignment = HorizontalAlignment.Right;
                    _footerRightBar.Margin = new Thickness(0, 6, 0, 0);
                }
                else
                {
                    DockPanel.SetDock(_footerRightBar, Dock.Right);
                    _footerRightBar.HorizontalAlignment = HorizontalAlignment.Right;
                    _footerRightBar.Margin = new Thickness(0);
                }
            }
            // At 720 DIP and the 896 DIP default the four selection commands stay icon-only;
            // labels return only on wider windows. New stays labeled throughout.
            ApplyFooterIconMode(w <= DesignWidthDip);
            _list.Padding = shortHeight ? new Thickness(18, 10, 18, 8) : new Thickness(24, 16, 24, 10);
            _list.MinHeight = Math.Min(MinListHeightDip, Math.Max(80.0, h - 150.0));
        }

        IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_GETMINMAXINFO)
            {
                try
                {
                    var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                    var realDpi = VisualTreeHelper.GetDpi(this);
                    double dpiX = realDpi.DpiScaleX > 0 ? realDpi.DpiScaleX : 1.0;
                    double dpiY = realDpi.DpiScaleY > 0 ? realDpi.DpiScaleY : 1.0;
                    var waDip = GetMonitorWorkAreaDip();
                    mmi.ptMaxPosition.x = (int)Math.Round(waDip.Left * dpiX);
                    mmi.ptMaxPosition.y = (int)Math.Round(waDip.Top * dpiY);
                    mmi.ptMaxSize.x = (int)Math.Round(waDip.Width * dpiX);
                    mmi.ptMaxSize.y = (int)Math.Round(waDip.Height * dpiY);
                    mmi.ptMinTrackSize.x = (int)Math.Round(MinWidth * dpiX);
                    mmi.ptMinTrackSize.y = (int)Math.Round(MinHeight * dpiY);
                    Marshal.StructureToPtr(mmi, lParam, true);
                    handled = true;
                }
                catch { }
            }
            else if (msg == WM_DPICHANGED || msg == WM_DISPLAYCHANGE || msg == WM_EXITSIZEMOVE)
            {
                if (msg == WM_DPICHANGED) WmDpiChangedCount++;
                Dispatcher.InvokeAsync(() => ClampToMonitorWorkArea(recenter: false));
            }
            return IntPtr.Zero;
        }

        UIElement BuildFooterCommandBar()
        {
            _footerDock = new DockPanel { Margin = new Thickness(18, 11, 18, 11) };

            _footerRightBar = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _footerTestBtn = Theme.Btn("_Test", (a, b) => Test(), primary: false, iconPath: Lucide.FlaskConical, suffix: " Runner", variant: "emerald");
            AutomationProperties.SetAutomationId(_footerTestBtn, "FooterTestBtn");
            _footerRightBar.Children.Add(_footerTestBtn);
            _footerRightBar.Children.Add(new Border { Width = 1, Height = 16, Background = Theme.Divider, Margin = new Thickness(2, 0, 10, 0) });
            // Theme switch is now located in SettingsWindow (gear icon in header)
            DockPanel.SetDock(_footerRightBar, Dock.Right);
            _footerDock.Children.Add(_footerRightBar);

            _footerLeftBar = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
            _footerNewBtn = Theme.Btn("_New", (a, b) => New(), primary: true, iconPath: Lucide.Plus, suffix: " Group");
            _footerEditBtn = Theme.Btn("_Edit", (a, b) => Edit(), primary: false, iconPath: Lucide.Edit3, iconBrushProp: "Sub");
            _footerDuplicateBtn = Theme.Btn("D_uplicate", (a, b) => Duplicate(), primary: false, iconPath: Lucide.Copy, iconBrushProp: "Sub");
            _footerShortcutBtn = Theme.Btn("Create _Shortcut", (a, b) => Shortcut(), primary: false, iconPath: Lucide.ExternalLink, iconBrushProp: "Sub");
            _footerDeleteBtn = Theme.Btn("_Delete", (a, b) => Delete(), primary: false, iconPath: Lucide.Trash2, variant: "danger");

            AutomationProperties.SetAutomationId(_footerNewBtn, "FooterNewBtn");
            AutomationProperties.SetAutomationId(_footerEditBtn, "FooterEditBtn");
            AutomationProperties.SetAutomationId(_footerDuplicateBtn, "FooterDuplicateBtn");
            AutomationProperties.SetAutomationId(_footerShortcutBtn, "FooterShortcutBtn");
            AutomationProperties.SetAutomationId(_footerDeleteBtn, "FooterDeleteBtn");

            // New stays labeled at every width; the four selection commands collapse at the minimum width.
            SetupFooterIconMode(_footerEditBtn, "Edit", Lucide.Edit3, "Sub", null);
            SetupFooterIconMode(_footerDuplicateBtn, "Duplicate", Lucide.Copy, "Sub", null);
            SetupFooterIconMode(_footerShortcutBtn, "Create Shortcut", Lucide.ExternalLink, "Sub", null);
            SetupFooterIconMode(_footerDeleteBtn, "Delete", Lucide.Trash2, "DangerFg", "danger");

            _footerLeftBar.Children.Add(_footerNewBtn);
            _footerLeftBar.Children.Add(_footerEditBtn);
            _footerLeftBar.Children.Add(_footerDuplicateBtn);
            _footerLeftBar.Children.Add(_footerShortcutBtn);
            _footerLeftBar.Children.Add(_footerDeleteBtn);

            _footerSelectionHint = new TextBlock
            {
                Text = "Select a group to edit, duplicate, or launch.",
                Foreground = Theme.Muted,
                FontSize = 11.5,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            AutomationProperties.SetName(_footerSelectionHint, "Selection hint");
            AutomationProperties.SetAutomationId(_footerSelectionHint, "FooterSelectionHint");
            _footerLeftBar.Children.Add(_footerSelectionHint);
            _footerDock.Children.Add(_footerLeftBar);

            _footerBorder = new Border
            {
                Background = Theme.Panel,
                BorderBrush = Theme.Divider,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Child = _footerDock
            };
            return _footerBorder;
        }

        void SetupFooterIconMode(Button b, string label, string iconPath, string brushProp, string variant)
        {
            _footerIconModes[b] = new FooterIconMode
            {
                LabeledTemplate = b.Template,
                LabeledPadding = b.Padding,
                IconTemplate = BuildFooterIconOnlyTemplate(iconPath, variant == "danger" ? "DangerFg" : brushProp)
            };
            AutomationProperties.SetName(b, label);
            b.ToolTip = label;
        }

        static ControlTemplate BuildFooterIconOnlyTemplate(string iconPath, string brushProp)
        {
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
            sp.AppendChild(Lucide.MakeFactory(iconPath, brushProp, 13.5, fill: false, strokeWidth: 2.0));
            bd.AppendChild(sp);

            var ct = new ControlTemplate(typeof(Button)) { VisualTree = bd };
            var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(UIElement.OpacityProperty, 0.88));
            ct.Triggers.Add(hover);
            return ct;
        }

        void ApplyFooterIconMode(bool iconsOnly)
        {
            if (_footerIconsOnly == iconsOnly) return;
            _footerIconsOnly = iconsOnly;
            foreach (var pair in _footerIconModes)
            {
                var b = pair.Key;
                var mode = pair.Value;
                b.Template = iconsOnly ? mode.IconTemplate : mode.LabeledTemplate;
                b.Padding = iconsOnly ? new Thickness(8, 7, 8, 7) : mode.LabeledPadding;
            }
        }
    }
}
