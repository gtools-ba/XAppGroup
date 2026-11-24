using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

namespace XAppGroup
{
    public class ThemedTrayColorTable : System.Windows.Forms.ProfessionalColorTable
    {
        private readonly bool _dark;
        public ThemedTrayColorTable(bool dark)
        {
            _dark = dark;
            UseSystemColors = false;
        }

        public override System.Drawing.Color ToolStripDropDownBackground =>
            _dark ? System.Drawing.Color.FromArgb(28, 31, 32) : System.Drawing.Color.FromArgb(255, 255, 255);

        public override System.Drawing.Color MenuBorder =>
            _dark ? System.Drawing.Color.FromArgb(42, 46, 48) : System.Drawing.Color.FromArgb(229, 231, 235);

        public override System.Drawing.Color MenuItemBorder =>
            System.Drawing.Color.Transparent;

        public override System.Drawing.Color MenuItemSelected =>
            _dark ? System.Drawing.Color.FromArgb(37, 41, 43) : System.Drawing.Color.FromArgb(243, 244, 246);

        public override System.Drawing.Color MenuItemSelectedGradientBegin => MenuItemSelected;
        public override System.Drawing.Color MenuItemSelectedGradientEnd => MenuItemSelected;
        public override System.Drawing.Color MenuItemPressedGradientBegin => MenuItemSelected;
        public override System.Drawing.Color MenuItemPressedGradientEnd => MenuItemSelected;
        public override System.Drawing.Color MenuStripGradientBegin => ToolStripDropDownBackground;
        public override System.Drawing.Color MenuStripGradientEnd => ToolStripDropDownBackground;
        public override System.Drawing.Color ImageMarginGradientBegin => ToolStripDropDownBackground;
        public override System.Drawing.Color ImageMarginGradientMiddle => ToolStripDropDownBackground;
        public override System.Drawing.Color ImageMarginGradientEnd => ToolStripDropDownBackground;
        public override System.Drawing.Color SeparatorDark =>
            _dark ? System.Drawing.Color.FromArgb(42, 46, 48) : System.Drawing.Color.FromArgb(229, 231, 235);
        public override System.Drawing.Color SeparatorLight =>
            System.Drawing.Color.Transparent;
    }

    public class ThemedTrayMenuRenderer : System.Windows.Forms.ToolStripProfessionalRenderer
    {
        private readonly bool _dark;

        public ThemedTrayMenuRenderer(bool dark) : base(new ThemedTrayColorTable(dark))
        {
            _dark = dark;
        }

        protected override void OnRenderToolStripBackground(System.Windows.Forms.ToolStripRenderEventArgs e)
        {
            using (var brush = new System.Drawing.SolidBrush(_dark ? System.Drawing.Color.FromArgb(28, 31, 32) : System.Drawing.Color.FromArgb(255, 255, 255)))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }

        protected override void OnRenderToolStripBorder(System.Windows.Forms.ToolStripRenderEventArgs e)
        {
            using (var pen = new System.Drawing.Pen(_dark ? System.Drawing.Color.FromArgb(42, 46, 48) : System.Drawing.Color.FromArgb(229, 231, 235), 1))
            {
                var r = e.AffectedBounds;
                e.Graphics.DrawRectangle(pen, r.Left, r.Top, r.Width - 1, r.Height - 1);
            }
        }

        protected override void OnRenderImageMargin(System.Windows.Forms.ToolStripRenderEventArgs e)
        {
            // Do not paint gutter column - keep unified background
        }

        protected override void OnRenderMenuItemBackground(System.Windows.Forms.ToolStripItemRenderEventArgs e)
        {
            if (e.Item is System.Windows.Forms.ToolStripSeparator) return;

            if (e.Item.Selected && e.Item.Enabled)
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var r = new System.Drawing.Rectangle(4, 1, e.Item.Width - 8, e.Item.Height - 2);
                using (var path = GetRoundedPath(r, 6))
                using (var brush = new System.Drawing.SolidBrush(_dark ? System.Drawing.Color.FromArgb(37, 41, 43) : System.Drawing.Color.FromArgb(243, 244, 246)))
                using (var pen = new System.Drawing.Pen(_dark ? System.Drawing.Color.FromArgb(56, 61, 64) : System.Drawing.Color.FromArgb(209, 213, 219), 1))
                {
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }

        protected override void OnRenderItemText(System.Windows.Forms.ToolStripItemTextRenderEventArgs e)
        {
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            if (!e.Item.Enabled)
                e.TextColor = _dark ? System.Drawing.Color.FromArgb(107, 114, 128) : System.Drawing.Color.FromArgb(156, 163, 175);
            else if (e.Item.Selected)
                e.TextColor = _dark ? System.Drawing.Color.FromArgb(255, 255, 255) : System.Drawing.Color.FromArgb(17, 24, 39);
            else
                e.TextColor = _dark ? System.Drawing.Color.FromArgb(243, 244, 246) : System.Drawing.Color.FromArgb(31, 36, 41);

            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(System.Windows.Forms.ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using (var pen = new System.Drawing.Pen(_dark ? System.Drawing.Color.FromArgb(42, 46, 48) : System.Drawing.Color.FromArgb(229, 231, 235), 1))
            {
                e.Graphics.DrawLine(pen, 10, y, e.Item.Width - 10, y);
            }
        }

        protected override void OnRenderArrow(System.Windows.Forms.ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = _dark ? System.Drawing.Color.FromArgb(156, 163, 175) : System.Drawing.Color.FromArgb(107, 114, 128);
            base.OnRenderArrow(e);
        }

        protected override void OnRenderItemImage(System.Windows.Forms.ToolStripItemImageRenderEventArgs e)
        {
            if (e.Image != null)
            {
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            }
            base.OnRenderItemImage(e);
        }

        private static System.Drawing.Drawing2D.GraphicsPath GetRoundedPath(System.Drawing.Rectangle rect, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            float d = radius * 2f;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
