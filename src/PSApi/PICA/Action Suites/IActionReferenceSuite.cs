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

namespace PSFilterHostDll.PSApi.PICA
{
    internal interface IActionReferenceSuite
    {
        /// <summary>
        /// Converts the reference to a format that can be placed in an action descriptor.
        /// </summary>
        /// <param name="reference">The reference to convert.</param>
        /// <param name="descriptor">The <see cref="ActionDescriptorReference"/> that contains the value of the specified reference.</param>
        /// <returns><c>true</c> if the reference was converted to an action descriptor; otherwise, <c>false</c> if the reference is invalid.</returns>
        bool ConvertToActionDescriptor(PIActionReference reference, out ActionDescriptorReference descriptor);

        /// <summary>
        /// Creates a reference from an action descriptor.
        /// </summary>
        /// <param name="descriptor">The descriptor.</param>
        /// <returns>The new reference.</returns>
        PIActionReference CreateFromActionDescriptor(ActionDescriptorReference descriptor);
    }
}
