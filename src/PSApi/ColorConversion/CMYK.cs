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
    internal sealed class CMYK
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CMYK"/> class.
        /// </summary>
        /// <param name="cyan">The cyan component in the range of [0, 1].</param>
        /// <param name="magenta">The magenta component in the range of [0, 1]..</param>
        /// <param name="yellow">The yellow component in the range of [0, 1]..</param>
        /// <param name="black">The black component in the range of [0, 1]..</param>
        public CMYK(double cyan, double magenta, double yellow, double black)
        {
            Cyan = cyan;
            Magenta = magenta;
            Yellow = yellow;
            Black = black;
        }

        /// <summary>
        /// Gets the cyan component.
        /// </summary>
        /// <value>
        /// The cyan component.
        /// </value>
        public double Cyan
        {
            get;
        }

        /// <summary>
        /// Gets the magenta component.
        /// </summary>
        /// <value>
        /// The magenta component.
        /// </value>
        public double Magenta
        {
            get;
        }

        /// <summary>
        /// Gets the yellow component.
        /// </summary>
        /// <value>
        /// The yellow component.
        /// </value>
        public double Yellow
        {
            get;
        }

        /// <summary>
        /// Gets the black component.
        /// </summary>
        /// <value>
        /// The black component.
        /// </value>
        public double Black
        {
            get;
        }
    }
}
