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
    static class ColorServicesConstants
    {
        public const short plugIncolorServicesRGBSpace = 0;
        public const short plugIncolorServicesHSBSpace = 1;
        public const short plugIncolorServicesCMYKSpace = 2;
        public const short plugIncolorServicesLabSpace = 3;
        public const short plugIncolorServicesGraySpace = 4;
        public const short plugIncolorServicesHSLSpace = 5;
        public const short plugIncolorServicesXYZSpace = 6;
        public const short plugIncolorServicesChosenSpace = -1;

        public const int plugIncolorServicesForegroundColor = 0;
        public const int plugIncolorServicesBackgroundColor = 1;
    }
}
