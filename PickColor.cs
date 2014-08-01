/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2014 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

namespace PSFilterHostDll
{
    /// <summary>
    /// The callback used when the the filter wants the host application to show it's color picker dialog.
    /// </summary>
    /// <param name="prompt">The prompt for the user.</param>
    /// <param name="color">The color returned by the user.</param>
    /// <returns>True if the user chose a color; otherwise, false if the user canceled the dialog.</returns>
    public delegate bool PickColor(string prompt, ref ColorPickerResult color);
}
