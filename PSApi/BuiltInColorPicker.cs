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

using System.Drawing;
using System.Windows.Forms;

namespace PSFilterHostDll.PSApi
{
    internal sealed class BuiltInColorPicker : IColorPicker
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BuiltInColorPicker"/> class.
        /// </summary>
        public BuiltInColorPicker()
        {
        }

        public bool ShowDialog(string prompt, ref byte red, ref byte green, ref byte blue)
        {
            bool colorPicked = false;

            using (ColorPicker picker = new ColorPicker(prompt))
            {
                picker.Color = Color.FromArgb(red, green, blue);

                if (picker.ShowDialog() == DialogResult.OK)
                {
                    Color color = picker.Color;
                    red = color.R;
                    green = color.G;
                    blue = color.B;
                    colorPicked = true;
                }
            }

            return colorPicked;
        }
    }
}
