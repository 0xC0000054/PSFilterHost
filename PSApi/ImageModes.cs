/////////////////////////////////////////////////////////////////////////////////
//
// Adobe(R) Photoshop(R) filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2013 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/* Adapted from PIGeneral.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

namespace PSFilterLoad.PSApi
{
    internal enum ImageModes : short
    {
        plugInModeBitmap = 0,
        plugInModeGrayScale = 1,
        plugInModeIndexedColor = 2,
        plugInModeRGBColor = 3,
        plugInModeCMYKColor = 4,
        plugInModeHSLColor = 5,
        plugInModeHSBColor = 6,
        plugInModeMultichannel = 7,
        plugInModeDuotone = 8,
        plugInModeLabColor = 9,
        plugInModeGray16 = 10,
        plugInModeRGB48 = 11
    }
}
