/////////////////////////////////////////////////////////////////////////////////
//
// Adobe(R) Photoshop(R) filter host for .NET
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
    static class ColorServicesSelector
    {
        public const short plugIncolorServicesChooseColor = 0;
        public const short plugIncolorServicesConvertColor = 1;
        public const short plugIncolorServicesSamplePoint = 2;
        public const short plugIncolorServicesGetSpecialColor = 3; 
    }
}
