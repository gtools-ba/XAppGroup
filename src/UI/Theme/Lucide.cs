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
    public static class Lucide
    {
        public const string Layers = "M 12,2 L 2,7 L 12,12 L 22,7 Z M 2,17 L 12,22 L 22,17 M 2,12 L 12,17 L 22,12";
        public const string Search = "M 19,11 A 8,8 0 1 1 3,11 A 8,8 0 1 1 19,11 M 21,21 L 16.65,16.65";
        public const string RefreshCw = "M 3,12 A 9,9 0 0 1 18.74,5.74 L 21,8 M 21,3 L 21,8 L 16,8 M 21,12 A 9,9 0 0 1 5.26,18.26 L 3,16 M 8,16 L 3,16 L 3,21";
        public const string Settings = "M 12,15 A 3,3 0 1 0 12,9 A 3,3 0 1 0 12,15 Z M 19.4,15 A 1.65,1.65 0 0 0 19.73,16.82 L 19.79,16.88 A 2,2 0 0 1 16.96,19.71 L 16.9,19.65 A 1.65,1.65 0 0 0 15.08,19.32 A 1.65,1.65 0 0 0 14,20.83 L 14,21 A 2,2 0 0 1 10,21 L 10,20.91 A 1.65,1.65 0 0 0 8.92,19.4 A 1.65,1.65 0 0 0 7.1,19.73 L 7.04,19.79 A 2,2 0 0 1 4.21,16.96 L 4.27,16.9 A 1.65,1.65 0 0 0 4.6,15.08 A 1.65,1.65 0 0 0 3.09,14 L 3,14 A 2,2 0 0 1 3,10 L 3.09,10 A 1.65,1.65 0 0 0 4.6,8.92 A 1.65,1.65 0 0 0 4.27,7.1 L 4.21,7.04 A 2,2 0 0 1 7.04,4.21 L 7.1,4.27 A 1.65,1.65 0 0 0 8.92,4.6 L 9,4.6 A 1.65,1.65 0 0 0 10,3.09 L 10,3 A 2,2 0 0 1 14,3 L 14,3.09 A 1.65,1.65 0 0 0 15,4.6 A 1.65,1.65 0 0 0 16.82,4.27 L 16.88,4.21 A 2,2 0 0 1 19.71,7.04 L 19.65,7.1 A 1.65,1.65 0 0 0 19.32,8.92 L 19.32,9 A 1.65,1.65 0 0 0 20.83,10 L 21,10 A 2,2 0 0 1 21,14 L 20.91,14 A 1.65,1.65 0 0 0 19.4,15 Z";
        public const string FolderKanban = "M 4,20 L 20,20 A 2,2 0 0 0 22,18 L 22,8 A 2,2 0 0 0 20,6 L 12.07,6 A 2,2 0 0 1 10.41,5.1 L 9.59,3.9 A 2,2 0 0 0 7.93,3 L 4,3 A 2,2 0 0 0 2,5 L 2,18 A 2,2 0 0 0 4,20 Z M 8,10 L 8,14 M 12,10 L 12,12 M 16,10 L 16,16";
        public const string Cpu = "M 6,4 L 18,4 A 2,2 0 0 1 20,6 L 20,18 A 2,2 0 0 1 18,20 L 6,20 A 2,2 0 0 1 4,18 L 4,6 A 2,2 0 0 1 6,4 Z M 9,9 L 15,9 L 15,15 L 9,15 Z M 9,2 L 9,4 M 15,2 L 15,4 M 9,20 L 9,22 M 15,20 L 15,22 M 20,9 L 22,9 M 20,14 L 22,14 M 2,9 L 4,9 M 2,14 L 4,14";
        public const string Zap = "M 13,2 L 3,14 L 12,14 L 11,22 L 21,10 L 12,10 Z";
        public const string Play = "M 6,3 L 20,12 L 6,21 Z";
        public const string LayoutGrid = "M 3,3 L 10,3 L 10,10 L 3,10 Z M 14,3 L 21,3 L 21,10 L 14,10 Z M 14,14 L 21,14 L 21,21 L 14,21 Z M 3,14 L 10,14 L 10,21 L 3,21 Z";
        public const string MoreHorizontal = "M 12,13 A 1,1 0 1 0 12,11 A 1,1 0 1 0 12,13 Z M 19,13 A 1,1 0 1 0 19,11 A 1,1 0 1 0 19,13 Z M 5,13 A 1,1 0 1 0 5,11 A 1,1 0 1 0 5,13 Z";
        public const string Plus = "M 5,12 L 19,12 M 12,5 L 12,19";
        public const string Lightbulb = "M 15,14 C 15.2,13 15.7,12.3 16.5,11.5 C 17.5,10.6 18,9.3 18,8 A 6,6 0 1 0 6,8 C 6,9 6.2,10.2 7.5,11.5 C 8.2,12.2 8.8,13.2 9,14 M 9,18 L 15,18 M 10,22 L 14,22";
        public const string FolderPlus = "M 12,10 L 12,16 M 9,13 L 15,13 M 20,20 A 2,2 0 0 0 22,18 L 22,8 A 2,2 0 0 0 20,6 L 12.1,6 A 2,2 0 0 1 10.4,5.1 L 9.6,3.9 A 2,2 0 0 0 7.9,3 L 4,3 A 2,2 0 0 0 2,5 L 2,18 A 2,2 0 0 0 4,20 Z";
        public const string Edit3 = "M 12,20 L 21,20 M 16.5,3.5 A 2.12,2.12 0 0 1 19.5,6.5 L 7,19 L 3,20 L 4,16 Z";
        public const string Copy = "M 10,8 L 20,8 A 2,2 0 0 1 22,10 L 22,20 A 2,2 0 0 1 20,22 L 10,22 A 2,2 0 0 1 8,20 L 8,10 A 2,2 0 0 1 10,8 Z M 4,16 A 2,2 0 0 1 2,14 L 2,4 A 2,2 0 0 1 4,2 L 14,2 A 2,2 0 0 1 16,4";
        public const string ExternalLink = "M 15,3 L 21,3 L 21,9 M 10,14 L 21,3 M 18,13 L 18,19 A 2,2 0 0 1 16,21 L 5,21 A 2,2 0 0 1 3,19 L 3,8 A 2,2 0 0 1 5,6 L 11,6";
        public const string Trash2 = "M 3,6 L 21,6 M 19,6 L 19,20 A 2,2 0 0 1 17,22 L 7,22 A 2,2 0 0 1 5,20 L 5,6 M 8,6 L 8,4 A 2,2 0 0 1 10,2 L 14,2 A 2,2 0 0 1 16,4 L 16,6 M 10,11 L 10,17 M 14,11 L 14,17";
        public const string FlaskConical = "M 10,2 L 10,9.5 L 4.5,20.5 A 1,1 0 0 0 5.4,22 L 18.6,22 A 1,1 0 0 0 19.5,20.5 L 14,9.5 L 14,2 M 8.5,2 L 15.5,2 M 7,15 L 17,15";
        public const string ChevronDown = "M 6,9 L 12,15 L 18,9";
        public const string ChevronUp = "M 18,15 L 12,9 L 6,15";
        public const string Image = "M 19,3 H 5 A 2,2 0 0 0 3,5 V 19 A 2,2 0 0 0 5,21 H 19 A 2,2 0 0 0 21,19 V 5 A 2,2 0 0 0 19,3 Z M 8.5,10 A 1.5,1.5 0 1 0 8.5,7 A 1.5,1.5 0 1 0 8.5,10 Z M 21,15 L 16,10 L 5,21";
        public const string RotateCcw = "M 3,12 A 9,9 0 1 0 5.3,6.7 L 3,9 M 3,3 V 9 H 9";
        public const string Check = "M 20,6 L 9,17 L 4,12";
        public const string AlertTriangle = "M 10.29,3.86 L 1.82,18 A 2,2 0 0 0 3.55,21 L 20.45,21 A 2,2 0 0 0 22.18,18 L 13.71,3.86 A 2,2 0 0 0 10.29,3.86 Z M 12,9 L 12,13 M 12,17 L 12.01,17";
        public const string Info = "M 12,22 A 10,10 0 1 0 12,2 A 10,10 0 1 0 12,22 Z M 12,16 L 12,12 M 12,8 L 12.01,8";
        public const string Heart = "M 19,14 C 20.49,12.54 22,10.79 22,8.5 A 5.5,5.5 0 0 0 16.5,3 C 14.74,3 14,3.5 12,5 C 10.5,3.5 9.74,3 8,3 A 5.5,5.5 0 0 0 2.5,8.5 C 2.5,10.8 4,12.55 5.5,14 L 12,20.5 Z";
        public const string Coffee = "M 10,2 L 10,4 M 14,2 L 14,4 M 6,2 L 6,4 M 17,8 L 18,8 A 3,3 0 0 1 21,11 L 21,12 A 3,3 0 0 1 18,15 L 17,15 M 3,8 L 17,8 L 17,16 A 4,4 0 0 1 13,20 L 7,20 A 4,4 0 0 1 3,16 Z";
        public const string GitHub = "M 12,2 C 6.477,2 2,6.484 2,12.017 C 2,16.442 4.865,20.197 8.839,21.521 C 9.339,21.613 9.521,21.304 9.521,21.038 C 9.521,20.801 9.513,20.17 9.508,19.335 C 6.726,19.94 6.139,17.997 6.139,17.997 C 5.685,16.839 5.029,16.531 5.029,16.531 C 4.121,15.911 5.098,15.923 5.098,15.923 C 6.101,15.993 6.628,16.955 6.628,16.955 C 7.52,18.485 8.969,18.043 9.538,17.787 C 9.63,17.14 9.888,16.699 10.174,16.449 C 7.954,16.196 5.619,15.336 5.619,11.498 C 5.619,10.405 6.009,9.51 6.648,8.81 C 6.545,8.557 6.202,7.538 6.746,6.16 C 6.746,6.16 7.586,5.89 9.496,7.186 C 10.294,6.964 11.144,6.853 11.994,6.849 C 12.844,6.853 13.699,6.964 14.498,7.186 C 16.407,5.89 17.245,6.16 17.245,6.16 C 17.791,7.538 17.447,8.557 17.347,8.81 C 17.987,9.51 18.375,10.405 18.375,11.498 C 18.375,15.347 16.036,16.193 13.809,16.441 C 14.168,16.75 14.487,17.361 14.487,18.296 C 14.487,19.634 14.475,20.715 14.475,21.043 C 14.475,21.311 14.655,21.623 15.163,21.525 C 19.137,20.197 22,16.442 22,12.017 C 22,6.484 17.522,2 12,2 Z";

        public static Viewbox Make(string pathData, Brush brush, double size = 14, bool fill = false, double strokeWidth = 2.0)
        {
            var geom = Geometry.Parse(pathData);
            geom.Freeze();
            var path = new WpfPath
            {
                Data = geom,
                Stroke = brush,
                Fill = fill ? brush : Brushes.Transparent,
                StrokeThickness = strokeWidth,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round
            };
            var canvas = new Canvas { Width = 24, Height = 24 };
            canvas.Children.Add(path);
            return new Viewbox
            {
                Width = size,
                Height = size,
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Child = canvas
            };
        }

        public static FrameworkElementFactory MakeFactory(string pathData, string themeBrushProp, double size = 14, bool fill = false, Brush fixedBrush = null, double strokeWidth = 2.0)
        {
            var geom = Geometry.Parse(pathData);
            geom.Freeze();
            var pathF = new FrameworkElementFactory(typeof(WpfPath));
            pathF.SetValue(WpfPath.DataProperty, geom);
            pathF.SetValue(Shape.StrokeThicknessProperty, strokeWidth);
            pathF.SetValue(Shape.StrokeStartLineCapProperty, PenLineCap.Round);
            pathF.SetValue(Shape.StrokeEndLineCapProperty, PenLineCap.Round);
            pathF.SetValue(Shape.StrokeLineJoinProperty, PenLineJoin.Round);

            if (fixedBrush != null)
            {
                pathF.SetValue(Shape.StrokeProperty, fixedBrush);
                pathF.SetValue(Shape.FillProperty, fill ? fixedBrush : Brushes.Transparent);
            }
            else if (!string.IsNullOrEmpty(themeBrushProp))
            {
                pathF.SetBinding(Shape.StrokeProperty, Theme.Bind(themeBrushProp));
                if (fill) pathF.SetBinding(Shape.FillProperty, Theme.Bind(themeBrushProp));
                else pathF.SetValue(Shape.FillProperty, Brushes.Transparent);
            }

            var canvasF = new FrameworkElementFactory(typeof(Canvas));
            canvasF.SetValue(FrameworkElement.WidthProperty, 24.0);
            canvasF.SetValue(FrameworkElement.HeightProperty, 24.0);
            canvasF.AppendChild(pathF);

            var vbF = new FrameworkElementFactory(typeof(Viewbox));
            vbF.SetValue(FrameworkElement.WidthProperty, size);
            vbF.SetValue(FrameworkElement.HeightProperty, size);
            vbF.SetValue(Viewbox.StretchProperty, Stretch.Uniform);
            vbF.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            vbF.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            vbF.AppendChild(canvasF);
            return vbF;
        }
    }
}
