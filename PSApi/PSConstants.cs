/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2016 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/* Adapted from PIGeneral.h and PIProperties.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

namespace PSFilterHostDll.PSApi
{
    internal static class PSConstants
    {
        /// <summary>
        /// The signature of Adobe(R) Photoshop(R) - '8BIM'
        /// </summary>
        public const uint kPhotoshopSignature = 0x3842494dU;

        /// <summary>
        /// The signature used when a plug-in works with any host.
        /// </summary>
        public const uint AnyHostSignature = 0x20202020U;

        /// <summary>
        /// The filter type code - '8BFM'
        /// </summary>
        public const uint filterKind = 0x3842464dU;

        public const int latestPIPLVersion = 0;

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

        internal static class PICA
        {
            public const string ActionDescriptorSuite = "df135115-c769-11d0-8079-00c04fd7ec47";
            public const string ASZStringSuite = "AS ZString Suite";
            public const string BufferSuite = "Photoshop Buffer Suite for Plug-ins";
            public const string ColorSpaceSuite = "Photoshop ColorSpace Suite for Plug-ins";
            public const string HandleSuite = "Photoshop Handle Suite for Plug-ins";
            public const string PropertySuite = "Photoshop Property Suite for Plug-ins";
            public const string UIHooksSuite = "Photoshop UIHooks Suite for Plug-ins";

#if PICASUITEDEBUG
            public const string ASZStringDictonarySuite = "AS ZString Dictionary Suite";
            public const string SPPluginsSuite = "SP Plug-ins Suite";
#endif
        }

        public const int LatestTerminologyVersion = 0;
        public const int AETEMajorVersion = 1;
        public const int AETEMinorVersion = 0;
        public const short AETESuiteLevel = 1;
        public const short AETESuiteVersion = 1;

        internal static class ChannelPorts
        {
            /// <summary>
            /// The index of the gray channel.
            /// </summary>
            public const int Gray = 0;
            /// <summary>
            /// The index of the red channel.
            /// </summary>
            public const int Red = 1;
            /// <summary>
            /// The index of the green channel.
            /// </summary>
            public const int Green = 2;
            /// <summary>
            /// The index of the blue channel.
            /// </summary>
            public const int Blue = 3;
            /// <summary>
            /// The index of the alpha channel.
            /// </summary>
            public const int Alpha = 4;
            /// <summary>
            /// The index of the selection mask.
            /// </summary>
            public const int SelectionMask = 5;
        }

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
        /// The constants used by the Property suite.
        /// </summary>
        internal static class Properties
        {
            /// <summary>
            /// The default big nudge distance, 10 pixels.
            /// </summary>
            public const int BigNudgeDistance = 10;
            /// <summary>
            /// The default major grid size.
            /// </summary>
            public const int GridMajor = 1;
            /// <summary>
            /// The default minor grid size.
            /// </summary>
            public const int GridMinor = 4;
            /// <summary>
            /// The index that is used when a document does not contain any paths.
            /// </summary>
            public const int NoPathIndex = -1;

            internal static class InterpolationMethod
            {
                public const int NearestNeghbor = 1;
                public const int Bilinear = 2;
                public const int Bicubic = 3;
            }
        }

        /// <summary>
        /// The padding constants used by the FilterRecord input, output and mask padding fields.
        /// </summary>
        internal static class Padding
        {
            public const short plugInWantsEdgeReplication = -1;
            public const short plugInDoesNotWantPadding = -2;
            public const short plugInWantsErrorOnBoundsException = -3;
        }

        /// <summary>
        /// The layout constants for the data presented to the plug-ins.
        /// </summary>
        internal static class Layout
        {
            /// <summary>
            /// Rows, columns, planes with colbytes = # planes
            /// </summary>
            public const short Traditional = 0;
            public const short RowsColumnsPlanes = 1;
            public const short RowsPlanesColumns = 2;
            public const short ColumnsRowsPlanes = 3;
            public const short ColumnsPlanesRows = 4;
            public const short PlanesRowsColumns = 5;
            public const short PlanesColumnsRows = 6;
        }
    }
}
