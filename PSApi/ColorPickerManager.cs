/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2018 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System.Drawing;
using System.Windows.Forms;

namespace PSFilterHostDll.PSApi
{
    internal static class ColorPickerManager
    {
        private static PickColor pickColor;

        public static void SetPickColorCallback(PickColor value)
        {
            pickColor = value;
        }

        public static bool ShowColorPickerDialog(string prompt, ref byte red, ref byte green, ref byte blue)
        {
            bool colorPicked = false;

            if (pickColor != null)
            {
                ColorPickerResult color = pickColor(prompt == null ? string.Empty : prompt, red, green, blue);

                if (color != null)
                {
                    red = color.R;
                    green = color.G;
                    blue = color.B;
                    colorPicked = true;
                }
            }
            else
            {
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
            }

            return colorPicked;
        }
    }
}
