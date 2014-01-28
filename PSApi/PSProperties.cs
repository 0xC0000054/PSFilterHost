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

/* Adapted from PIProperties.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

namespace PSFilterLoad.PSApi
{
    internal static class PSProperties
    {
        /// <summary>
        /// The horizontal big nudge distance, 16.16 fixed point value - 'bndH'
        /// </summary>
        /// <remarks>The horizontal distance in pixels to move when using the shift key, default 10 pixels.</remarks>
        public const uint propBigNudgeH = 0x626e6448U;
        /// <summary>
        /// The vertical big nudge distance, 16.16 fixed point value - 'bndV'
        /// </summary>
        /// <remarks>The vertical distance in pixels to move when using the shift key, default 10 pixels.</remarks>
        public const uint propBigNudgeV = 0x626e6456U;
        /// <summary>
        /// The caption of the document as an IPTC-NAA record - 'capt'
        /// </summary>
        public const uint propCaption = 0x63617074U;
        /// <summary>
        /// Channel Name - 'nmch'
        /// </summary>
        public const uint propChannelName = 0x6e6d6368U;
        /// <summary>
        /// The copyright status of the document - 'cpyr'
        /// </summary>
        public const uint propCopyright = 0x63707972U;
        /// <summary>
        /// The new copyright property from 5.0, a get only version of propCopyright - 'cpyR'
        /// </summary>
        public const uint propCopyright2 = 0x63707952U;
        /// <summary>
        /// The document EXIF data - 'EXIF' 
        /// </summary>
        public const uint propEXIFData = 0x45584946U;
        /// <summary>
        /// The document XMP data - 'xmpd' 
        /// </summary>
        public const uint propXMPData = 0x786d7064U;
        /// <summary>
        /// Major grid size, 16.16 fixed point value - 'grmj'      
        /// </summary>
        /// <remarks>Measured in inches unless <see cref="propRulerUnits"/> is pixels, and then in pixels.</remarks>
        public const uint propGridMajor = 0x67726d6aU;
        /// <summary>
        /// Minor grid size, the number of subdivisions per <see cref="propGridMajor"/> - 'grmn'
        /// </summary>
        public const uint propGridMinor = 0x67726d6eU;
        /// <summary>
        /// Image mode - 'mode'
        /// </summary>
        public const uint propImageMode = 0x6d6f6465U;
        /// <summary>
        /// Interpolation Mode - 'intp'
        /// </summary>
        public const uint propInterpolationMethod = 0x696E7470U;
        /// <summary>
        /// Number of channels - 'nuch'
        /// </summary>
        public const uint propNumberOfChannels = 0x6e756368U;
        /// <summary>
        /// The number of paths = 'nupa'
        /// </summary>
        public const uint propNumberOfPaths = 0x6e757061U;
        /// <summary>
        /// The name of the path = 'nmpa'
        /// </summary>
        public const uint propPathName = 0x6e6d7061U;
        /// <summary>
        /// The index of the work path = 'wkpa'
        /// </summary>
        public const uint propWorkPathIndex = 0x776b7061U;
        /// <summary>
        /// The index of the clipping path = 'clpa'
        /// </summary>
        public const uint propClippingPathIndex = 0x636c7061U;
        /// <summary>
        /// The index of the target path = 'tgpa'
        /// </summary>
        public const uint propTargetPathIndex = 0x74677061U;
        /// <summary>
        /// Ruler Units - 'rulr'
        /// </summary>
        public const uint propRulerUnits = 0x72756c72U;
        /// <summary>
        /// Ruler origin horizontal, 16.16 fixed point value - 'rorH'
        /// </summary>
        public const uint propRulerOriginH = 0x726f7248U;
        /// <summary>
        /// Ruler origin vertical, 16.16 fixed point value - 'rorV'
        /// </summary>
        public const uint propRulerOriginV = 0x726f7256U;
        /// <summary>
        /// The host's serial number string - 'sstr' 
        /// </summary>
        public const uint propSerialString = 0x73737472U;
        /// <summary>
        /// The property that indicates if the file has a digital signature or watermark - 'watr'
        /// </summary>
        public const uint propWatermark = 0x77617472U;
        /// <summary>
        /// The URL of the document - 'URL '
        /// </summary>
        public const uint propURL = 0x55524c20U;
        /// <summary>
        /// The title of the document - 'titl'
        /// </summary>
        public const uint propTitle = 0x7469746cU;
        /// <summary>
        /// The watch suspension level - 'wtch'
        /// </summary>
        public const uint propWatchSuspension = 0x77746368U;
        /// <summary>
        /// The width of the current document in pixels - 'docW'  
        /// </summary>
        public const uint propDocumentWidth = 0x646f6357U;
        /// <summary>
        /// The height of the current document in pixels - 'docH'  
        /// </summary>
        public const uint propDocumentHeight = 0x646f6348U;
        /// <summary>
        /// Tool tip display - 'tltp'
        /// </summary>
        public const uint propToolTips = 0x746c7470U;
    }
}
