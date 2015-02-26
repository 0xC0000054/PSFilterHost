/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2015 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

namespace PSFilterHostDll
{
    /// <summary>
    /// The callback used when the filter wants the host application to show it's color picker dialog.
    /// </summary>
    /// <param name="prompt">The prompt for the user.</param>
    /// <param name="defaultRed">The red component of the color that the filter wants selected in the host's color dialog.</param>
    /// <param name="defaultGreen">The green component of the color that the filter wants selected in the host's color dialog.</param>
    /// <param name="defaultBlue">The blue component of the color that the filter wants selected in the host's color dialog.</param>
    /// <returns>A <see cref="ColorPickerResult" /> containing the user's chosen color; otherwise, null (Nothing in Visual Basic) if the user canceled the dialog.</returns>
    public delegate ColorPickerResult PickColor(string prompt, byte defaultRed, byte defaultGreen, byte defaultBlue);
}
