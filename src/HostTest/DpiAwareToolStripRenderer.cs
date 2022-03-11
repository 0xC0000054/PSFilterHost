/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System.Drawing;
using System.Windows.Forms;
using PaintDotNet.SystemLayer;

namespace HostTest
{
    internal sealed class DpiAwareToolStripRenderer : ToolStripProfessionalRenderer
    {
        private static readonly int ArrowOffset2X = UI.ScaleWidth(2);
        private static readonly int ArrowOffset2Y = UI.ScaleHeight(2);
        private static readonly int ArrowOffset4Y = UI.ScaleHeight(4);

        private Point[] arrowPoints;

        public DpiAwareToolStripRenderer() : base()
        {
            arrowPoints = new Point[3];
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            Rectangle arrowRect = e.ArrowRectangle;

            using (Brush brush = new SolidBrush(e.ArrowColor))
            {
                Point center = new Point(arrowRect.Left + arrowRect.Width / 2, arrowRect.Top + arrowRect.Height / 2);

                switch (e.Direction)
                {
                    case ArrowDirection.Up:

                        arrowPoints[0] = new Point(center.X - ArrowOffset2X, center.Y + 1);
                        arrowPoints[1] = new Point(center.X + ArrowOffset2X + 1, center.Y + 1);
                        arrowPoints[2] = new Point(center.X, center.Y - ArrowOffset2Y);

                        break;
                    case ArrowDirection.Left:

                        arrowPoints[0] = new Point(center.X + ArrowOffset2X, center.Y - ArrowOffset4Y);
                        arrowPoints[1] = new Point(center.X + ArrowOffset2X, center.Y + ArrowOffset4Y);
                        arrowPoints[2] = new Point(center.X - ArrowOffset2X, center.Y);

                        break;
                    case ArrowDirection.Right:

                        arrowPoints[0] = new Point(center.X - ArrowOffset2X, center.Y - ArrowOffset4Y);
                        arrowPoints[1] = new Point(center.X - ArrowOffset2X, center.Y + ArrowOffset4Y);
                        arrowPoints[2] = new Point(center.X + ArrowOffset2X, center.Y);

                        break;
                    case ArrowDirection.Down:
                    default:

                        arrowPoints[0] = new Point(center.X - ArrowOffset2X, center.Y - 1);
                        arrowPoints[1] = new Point(center.X + ArrowOffset2X + 1, center.Y - 1);
                        arrowPoints[2] = new Point(center.X, center.Y + ArrowOffset2Y);
                        break;
                }

                e.Graphics.FillPolygon(brush, arrowPoints);
            }
        }

        /// <summary>
        /// Scales the image size with the system DPI.
        /// </summary>
        /// <param name="image">The image to scale.</param>
        /// <returns>The image scaled to the system DPI.</returns>
        private static Image ScaleImageWithDPI(Image image)
        {
            int scaledWidth = UI.ScaleWidth(image.Width);
            int scaledHeight = UI.ScaleHeight(image.Height);

            Bitmap scaledImage = new Bitmap(scaledWidth, scaledHeight, image.PixelFormat);

            using (Graphics g = Graphics.FromImage(scaledImage))
            {
                g.DrawImage(image, 0, 0, scaledWidth, scaledHeight);
            }

            return scaledImage;
        }

        /// <summary>
        /// Scales the MenuStrip and ContextMenuStrip scroll button arrows with the system DPI.
        /// </summary>
        /// <param name="menu">The menu.</param>
        internal static void ScaleScrollButtonArrows(ToolStripDropDownMenu menu)
        {
            if (menu != null)
            {
                var controls = menu.Controls;

                for (int i = 0; i < controls.Count; i++)
                {
                    // The ToolStripDropDownMenu scrolling is implemented by the internal ToolStripScrollButton class, which uses a StickyLabel class to hold the arrow image.
                    if (controls[i].GetType().Name == "StickyLabel")
                    {
                        Label label = controls[i] as Label;

                        if (label != null)
                        {
                            if (label.Image != null && label.Tag == null)
                            {
                                if (label.Image.Size != UI.ScaleSize(label.Image.Size))
                                {
                                    label.Image = ScaleImageWithDPI(label.Image);
                                }
                                label.Tag = string.Empty;
                            }
                        }
                    }
                }
            }
        }
    }
}
