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

/* Adapted from PIGeneral.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

namespace PSFilterLoad.PSApi
{
    internal enum ImageModes : short
    {
        Bitmap = 0,
        GrayScale = 1,
        IndexedColor = 2,
        RGBColor = 3,
        CMYKColor = 4,
        HSLColor = 5,
        HSBColor = 6,
        Multichannel = 7,
        Duotone = 8,
        LabColor = 9,
        Gray16 = 10,
        RGB48 = 11
    }
}
