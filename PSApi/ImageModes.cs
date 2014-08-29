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
        Indexed = 2,
        RGB = 3,
        CMYK = 4,
        HSL = 5,
        HSB = 6,
        Multichannel = 7,
        Duotone = 8,
        Lab = 9,
        Gray16 = 10,
        RGB48 = 11
    }
}
