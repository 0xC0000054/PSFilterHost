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
    internal interface IActionListSuite
    {
        /// <summary>
        /// Converts the list to a format that can be placed in an action descriptor.
        /// </summary>
        /// <param name="list">The list to convert.</param>
        /// <param name="descriptor">The <see cref="ActionDescriptorList"/> that contains the value of the specified list.</param>
        /// <returns><c>true</c> if the list was converted to an action descriptor; otherwise, <c>false</c> if the list is invalid.</returns>
        bool ConvertToActionDescriptor(PIActionList list, out ActionDescriptorList descriptor);

        /// <summary>
        /// Creates a list from an action descriptor.
        /// </summary>
        /// <param name="descriptor">The descriptor.</param>
        /// <returns>The new list.</returns>
        PIActionList CreateFromActionDescriptor(ActionDescriptorList descriptor);
    }
}
