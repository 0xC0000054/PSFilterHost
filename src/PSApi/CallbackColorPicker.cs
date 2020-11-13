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

using System;

namespace PSFilterHostDll.PSApi
{
    internal sealed class CallbackColorPicker : IColorPicker
    {
        private readonly PickColor pickColor;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallbackColorPicker"/> class.
        /// </summary>
        /// <param name="pickColor">The callback delegate.</param>
        /// <exception cref="ArgumentNullException"><paramref name="pickColor"/> is null.</exception>
        public CallbackColorPicker(PickColor pickColor)
        {
            if (pickColor == null)
            {
                throw new ArgumentNullException(nameof(pickColor));
            }

            this.pickColor = pickColor;
        }

        public bool ShowDialog(string prompt, ref byte red, ref byte green, ref byte blue)
        {
            bool colorPicked = false;

            ColorPickerResult color = pickColor(prompt ?? string.Empty, red, green, blue);

            if (color != null)
            {
                red = color.R;
                green = color.G;
                blue = color.B;
                colorPicked = true;
            }

            return colorPicked;
        }
    }
}
