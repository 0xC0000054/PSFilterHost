/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2015 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System.Drawing;
using System.Windows.Forms; 

namespace HostTest
{
    internal sealed class ColorToolStripMenuItem : ToolStripMenuItem
    {
        private SolidBrush colorBrush;
        private Color rectangleColor;

        public ColorToolStripMenuItem()
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && colorBrush != null)
            {
                this.colorBrush.Dispose();
                this.colorBrush = null;
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Gets or sets the color of the rectangle.
        /// </summary>
        /// <value>
        /// The color of the rectangle.
        /// </value>
        public Color RectangleColor
        {
            get
            {
                return rectangleColor;
            }
            set
            {
                if (rectangleColor != value)
                {
                    this.rectangleColor = value;

                    if (colorBrush != null)
                    {
                        colorBrush.Dispose();
                        colorBrush = null;
                    }

                    colorBrush = new SolidBrush(value);
                }
            }

        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.FillRectangle(colorBrush, e.ClipRectangle);
        }
    }
}
