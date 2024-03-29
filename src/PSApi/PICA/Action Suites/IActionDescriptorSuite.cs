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

namespace PSFilterHostDll.PSApi.PICA
{
    internal interface IActionDescriptorSuite
    {
        /// <summary>
        /// Gets the values associated with the specified descriptor.
        /// </summary>
        /// <param name="descriptor">The descriptor.</param>
        /// <param name="values">The values contained within the descriptor.</param>
        /// <returns><c>true</c> if the descriptor is valid; otherwise, <c>false</c>.</returns>
        bool TryGetDescriptorValues(PIActionDescriptor descriptor, out ReadOnlyDictionary<uint, AETEValue> values);

        /// <summary>
        /// Creates a descriptor that contains the specified values.
        /// </summary>
        /// <param name="values">The values to place in the descriptor.</param>
        /// <returns>The new descriptor.</returns>
        PIActionDescriptor CreateDescriptor(ReadOnlyDictionary<uint, AETEValue> values);
    }
}
