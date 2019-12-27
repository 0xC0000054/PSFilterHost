﻿/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2019 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterHostDll.PSApi.PICA
{
    internal interface IASZStringSuite
    {
        /// <summary>
        /// Converts a ZString to a format that can be placed in an action descriptor.
        /// </summary>
        /// <param name="zstring">The ZString to convert.</param>
        /// <param name="descriptor">The <see cref="ActionDescriptorZString"/> that contains the value of the specified ZString.</param>
        /// <returns><c>true</c> if the ZString was converted to an action descriptor; otherwise, <c>false</c> if the ZString is invalid.</returns>
        bool ConvertToActionDescriptor(ASZString zstring, out ActionDescriptorZString descriptor);

        /// <summary>
        /// Creates a ZString from an action descriptor.
        /// </summary>
        /// <param name="descriptor">The descriptor.</param>
        /// <returns>The new ZString value.</returns>
        ASZString CreateFromActionDescriptor(ActionDescriptorZString descriptor);

        /// <summary>
        /// Gets the value of the specified ZString.
        /// </summary>
        /// <param name="zstring">The ZString to convert.</param>
        /// <param name="value">The <see cref="string"/> that contains the value of the specified ZString.</param>
        /// <returns><c>true</c> if the ZString was converted to a string; otherwise, <c>false</c> if the ZString is invalid.</returns>
        bool ConvertToString(ASZString zstring, out string value);

        /// <summary>
        /// Creates a ZString from the specified string.
        /// </summary>
        /// <param name="value">The string to use.</param>
        /// <returns>The new ZString value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        ASZString CreateFromString(string value);
    }
}
