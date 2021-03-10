/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2021 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System.Drawing;
using System.Windows.Forms;
using PaintDotNet.SystemLayer;

namespace HostTest
{
    /// <summary>
    /// This class stops the sub-menu from expanding when the ToolStripMenuItem is disabled.
    /// </summary>
    internal sealed class ToolStripMenuItemEx : ToolStripMenuItem
    {
        private static readonly int ArrowOffset2X = UI.ScaleWidth(2);
        private static readonly int ArrowOffset2Y = UI.ScaleHeight(2);
        private static readonly int ArrowOffset4Y = UI.ScaleHeight(4);

        public ToolStripMenuItemEx(string text, ToolStripItem dropDownItem) : base(text, null, dropDownItem)
        {
        }

        public override bool HasDropDownItems
        {
            get
            {
                if (!Enabled)
                {
                    return false;
                }

                return base.HasDropDownItems;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // The HasDropDownItems property is overridden to return false when the menu is disabled
            // and the drop down arrow will be drawn manually.
            // This is done to prevent the ToolStripMenuItem from expanding the child menus
            // of a disabled item when the mouse hovers over it.
            if (!Enabled && DropDownItems.Count > 0)
            {
                bool rightToLeft = RightToLeft == RightToLeft.Yes;

                const int arrowWidth = 10;
                ArrowDirection direction = rightToLeft ? ArrowDirection.Left : ArrowDirection.Right;

                // See the TextPadding field in the System.Windows.Forms.ToolStripDropDownMenu static constructor.
                int textPadding = direction == ArrowDirection.Right ? 8 : 9;

                int arrowX;

                if (rightToLeft)
                {
                    arrowX = textPadding;
                }
                else
                {
                    arrowX = Bounds.Width - arrowWidth - textPadding;
                }

                Rectangle arrowRect = new Rectangle(arrowX, 0, arrowWidth, Bounds.Height - Padding.Vertical);

                Point center = new Point(arrowRect.Left + (arrowRect.Width / 2), arrowRect.Top + (arrowRect.Height / 2));
                Point[] points = new Point[3];

                switch (direction)
                {
                    case ArrowDirection.Left:
                        points[0] = new Point(center.X + ArrowOffset2X, center.Y - ArrowOffset4Y);
                        points[1] = new Point(center.X + ArrowOffset2X, center.Y + ArrowOffset4Y);
                        points[2] = new Point(center.X - ArrowOffset2X, center.Y);
                        break;
                    case ArrowDirection.Right:
                        points[0] = new Point(center.X - ArrowOffset2X, center.Y - ArrowOffset4Y);
                        points[1] = new Point(center.X - ArrowOffset2X, center.Y + ArrowOffset4Y);
                        points[2] = new Point(center.X + ArrowOffset2X, center.Y);
                        break;
                    case ArrowDirection.Up:
                        points[0] = new Point(center.X - ArrowOffset2X, center.Y + 1);
                        points[1] = new Point(center.X + ArrowOffset2X + 1, center.Y + 1);
                        points[2] = new Point(center.X, center.Y - ArrowOffset2Y);
                        break;
                    default:
                        points[0] = new Point(center.X - ArrowOffset2X, center.Y - 1);
                        points[1] = new Point(center.X + ArrowOffset2X + 1, center.Y - 1);
                        points[2] = new Point(center.X, center.Y + ArrowOffset2Y);
                        break;
                }

                using (SolidBrush brush = new SolidBrush(SystemColors.ControlDark))
                {
                    e.Graphics.FillPolygon(brush, points);
                }
            }
        }
    }
}
