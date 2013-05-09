﻿/////////////////////////////////////////////////////////////////////////////////
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

/* Adapted from PIGeneral.h and PIProperties.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

namespace PSFilterLoad.PSApi
{
    internal static class PSConstants
    {
        /// <summary>
        /// Photoshop's host signature - '8BIM'
        /// </summary>
        public const uint kPhotoshopSignature = 0x3842494dU;

        /// <summary>
        /// The filter type code - '8BFM'
        /// </summary>
        public const uint filterKind = 0x3842464dU; 

        public const short kCurrentBufferProcsVersion = 2;
        public const short kCurrentBufferProcsCount = 5; 
        
        public const short kCurrentHandleProcsVersion = 1;
        public const short kCurrentHandleProcsCount = 8;

#if USEIMAGESERVICES
        public const short kCurrentImageServicesProcsVersion = 1;
        public const short kCurrentImageServicesProcsCount = 2;
#endif
        public const short kCurrentPropertyProcsVersion = 1;
        public const short kCurrentPropertyProcsCount = 2;

        public const short kCurrentDescriptorParametersVersion = 0;

        public const short kCurrentReadDescriptorProcsVersion = 0;
        public const short kCurrentReadDescriptorProcsCount = 18;

        public const short kCurrentWriteDescriptorProcsVersion = 0;
        public const short kCurrentWriteDescriptorProcsCount = 16;

        public const short kCurrentChannelPortProcsVersion = 1;
        public const short kCurrentChannelPortProcsCount = 3;

        public const short kCurrentMinVersReadChannelDesc = 0;
        public const short kCurrentMaxVersReadChannelDesc = 0;

        public const short kCurrentMinVersWriteChannelDesc = 0;
        public const short kCurrentMaxVersWriteChannelDesc = 0;

        public const short kCurrentMinVersReadImageDocDesc = 0;
        public const short kCurrentMaxVersReadImageDocDesc = 0;  

        public const short kCurrentResourceProcsVersion = 3;
        public const short kCurrentResourceProcsCount = 4;

        public const short latestFilterVersion = 4;
        public const short latestFilterSubVersion = 0;

        public const int plugInModeRGBColor = 3;
        public const int plugInModeGrayScale = 1;

        public const int flagSupportsRGBColor = 16;
        public const int flagSupportsGrayScale = 64;
        public const int flagSupportsRGB48 = 4096;
        public const int flagSupportsGray16 = 8192;
       
        public const int supportsRGBColor = 8;
        public const int supportsGrayScale = 2;

#if PICASUITES
        public const string PICABufferSuite = "Photoshop Buffer Suite for Plug-ins";
        public const string PICAColorSpaceSuite = "Photoshop ColorSpace Suite for Plug-ins";
        public const string PICAHandleSuite = "Photoshop Handle Suite for Plug-ins";
        public const string PICAPropertySuite = "Photoshop Property Suite for Plug-ins";
        public const string PICAUIHooksSuite = "Photoshop UIHooks Suite for Plug-ins";
        public const string PICAZStringSuite = "AS ZString Suite";
        public const string PICAZStringDictonarySuite = "AS ZString Dictionary Suite";
        public const string PICAPluginsSuite = "SP Plug-ins Suite";
#endif

        /// <summary>
        /// The host sampling support constants 
        /// </summary>
        internal static class SamplingSupport
        {
            public const byte hostDoesNotSupportSampling = 0;
            public const byte hostSupportsIntegralSampling = 1;
            public const byte hostSupportsFractionalSampling = 2;
        }

        /// <summary>
        /// The RulerUnit constants used by PSProperties.propRulerUnits 
        /// </summary>
        internal static class RulerUnits
        {
            public const int Pixels = 0;
            public const int Inches = 1;
            public const int Centimeters = 2;
        }

        /// <summary>
        /// The InterpolationMethod constants used by PSProperties.propInterpolationMethod 
        /// </summary>
        internal static class InterpolationMethod
        {
            public const int NearestNeghbor = 1;
            public const int Bilinear = 2;
            public const int Bicubic = 3;
        }
    }

  

}
