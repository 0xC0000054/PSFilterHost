/////////////////////////////////////////////////////////////////////////////////
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

namespace PSFilterHostDll.PSApi
{
    internal interface IColorPicker
    {
        /// <summary>
        /// Displays a dialog allowing the user to select a color.
        /// </summary>
        /// <param name="prompt">The prompt for the user.</param>
        /// <param name="red">The red component of the color that the filter wants selected in the host's color dialog.</param>
        /// <param name="green">The green component of the color that the filter wants selected in the host's color dialog.</param>
        /// <param name="blue">The blue component of the color that the filter wants selected in the host's color dialog.</param>
        /// <returns>
        ///  <c>true</c> if a color was chosen; otherwise, <c>false</c> if the dialog was canceled.
        /// </returns>
        bool ShowDialog(string prompt, ref byte red, ref byte green, ref byte blue);
    }
}
