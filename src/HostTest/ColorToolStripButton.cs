/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2020 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace HostTest
{
    internal sealed class ColorToolStripButton : ToolStripButton
    {
        private Bitmap image;
        private Color color;

        private int imageWidth;
        private int imageHeight;

        public ColorToolStripButton()
        {
            imageWidth = 16;
            imageHeight = 16;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && image != null)
            {
                image.Dispose();
                image = null;
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Gets or sets the displayed color.
        /// </summary>
        /// <value>
        /// The color displayed by the <see cref="ColorToolStripButton"/>.
        /// </value>
        [Description("Specifies the color displayed as the image of the button.")]
        public Color Color
        {
            get => color;
            set
            {
                if (color != value)
                {
                    color = value;

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
        [Description("Specifies the size in pixels of the image displayed by the button.")]
        [DefaultValue(typeof(Size), "16,16")]
        public Size ImageSize
        {
            get => new Size(imageWidth, imageHeight);
            set
            {
                if (imageWidth != value.Width || imageHeight != value.Height)
                {
                    imageWidth = value.Width;
                    imageHeight = value.Height;
                    DrawImage();
                }
            }
        }

        private void DrawImage()
        {
            if (image == null || image.Width != imageWidth || image.Height != imageHeight)
            {
                if (image != null)
                {
                    image.Dispose();
                    image = null;
                }
                image = new Bitmap(imageWidth, imageHeight);
            }

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

        public override Image BackgroundImage => null;

        public override ToolStripItemDisplayStyle DisplayStyle => ToolStripItemDisplayStyle.Image;

        public override Image Image => image;
    }
}
