/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2016 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms; 

namespace HostTest
{
    internal sealed class ColorToolStripMenuItem : ToolStripMenuItem
    {
        private SolidBrush colorBrush;
        private Color color;

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
        /// Gets or sets the displayed color.
        /// </summary>
        /// <value>
        /// The color displayed by the <see cref="ColorToolStripMenuItem"/>.
        /// </value>
        [Description("Specifies the color displayed as the image of the menu item.")]
        public Color Color
        {
            get
            {
                return color;
            }
            set
            {
                if (color != value)
                {
                    this.color = value;

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
