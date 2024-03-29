﻿/////////////////////////////////////////////////////////////////////////////////
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

namespace PSFilterHostDll.PSApi.ColorConversion
{
    internal sealed class HSB
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HSB"/> class.
        /// </summary>
        /// <param name="hue">The hue component in the range of [0, 1].</param>
        /// <param name="saturation">The saturation component in the range of [0, 1].</param>
        /// <param name="brightness">The brightness component in the range of [0, 1].</param>
        public HSB(double hue, double saturation, double brightness)
        {
            Hue = hue;
            Saturation = saturation;
            Brightness = brightness;
        }

        /// <summary>
        /// Gets the hue component.
        /// </summary>
        /// <value>
        /// The hue.
        /// </value>
        public double Hue
        {
            get;
        }

        /// <summary>
        /// Gets the saturation component.
        /// </summary>
        /// <value>
        /// The saturation component.
        /// </value>
        public double Saturation
        {
            get;
        }

        /// <summary>
        /// Gets the brightness component.
        /// </summary>
        /// <value>
        /// The brightness component.
        /// </value>
        public double Brightness
        {
            get;
        }
    }
}
