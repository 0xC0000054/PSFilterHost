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
        private Bitmap image;
        private Color color;

        private int imageWidth;
        private int imageHeight;

        public ColorToolStripMenuItem()
        {
            this.imageWidth = 16;
            this.imageHeight = 16;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && image != null)
            {
                this.image.Dispose();
                this.image = null;
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

                    DrawImage();
                }
            }

        }

        /// <summary>
        /// Gets or sets the size of the image in pixels.
        /// </summary>
        /// <value>
        /// The size of the image in pixels.
        /// </value>
        [Description("Specifies the size in pixels of the image displayed by the menu item.")]
        [DefaultValue(typeof(Size), "16,16")]
        public Size ImageSize
        {
            get
            {
                return new Size(imageWidth, imageHeight);
            }
            set
            {
                if (imageWidth != value.Width || imageHeight != value.Height)
                {
                    this.imageWidth = value.Width;
                    this.imageHeight = value.Height;
                    DrawImage();
                }
            }
        } 

        private void DrawImage()
        {
            if (image != null)
            {
                image.Dispose();
                image = null;
            }
            image = new Bitmap(imageWidth, imageHeight);

            using (Graphics gr = Graphics.FromImage(image))
            {
                using (Pen borderPen = new Pen(Color.Black))
                {
                    gr.DrawRectangle(borderPen, 0, 0, imageWidth - 1, imageHeight - 1);
                } 

                using (SolidBrush brush = new SolidBrush(color))
                {
                    gr.FillRectangle(brush, 1, 1, imageWidth - 2, imageHeight - 2);
                }
            }
            Invalidate();
        }

        public override Image BackgroundImage
        {
            get
            {
                return null;
            }
        }

        public override ToolStripItemDisplayStyle DisplayStyle
        {
            get
            {
                return ToolStripItemDisplayStyle.Image;
            }
        }

        public override Image Image
        {
            get
            {
                return image;
            }
        }
    }
}
