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

/* Adapted from PIFilter.h
 * Copyright (c) 1990-1991, Thomas Knoll.
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/


using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// The inputHandling and outputHandling constants for the FilterCaseInfo structure
    /// </summary>
    internal enum FilterDataHandling : byte
    {
        filterDataHandlingCantFilter = 0,
        filterDataHandlingNone = 1,
        filterDataHandlingBlackMat = 2,
        filterDataHandlingGrayMat = 3,
        filterDataHandlingWhiteMat = 4,
        filterDataHandlingDefringe = 5,
        filterDataHandlingBlackZap = 6,
        filterDataHandlingGrayZap = 7,
        filterDataHandlingWhiteZap = 8,
        filterDataHandlingFillMask = 9,
        filterDataHandlingBackgroundZap = 10,
        filterDataHandlingForegroundZap = 11,
    }
    /// <summary>
    /// The processing flags for the FilterCaseInfo structure.
    /// </summary>
    [Flags]
    internal enum FilterCaseInfoFlags : byte
    {
        None = 0,
        PIFilterDontCopyToDestinationBit = (1 << 0),
        PIFilterWorksWithBlankDataBit = (1 << 1),
        PIFilterFiltersLayerMaskBit = (1 << 2),
        PIFilterWritesOutsideSelectionBit = (1 << 3)
    }

    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1), Serializable]
    struct FilterCaseInfo
    {
        public FilterDataHandling inputHandling;
        public FilterDataHandling outputHandling;
        public FilterCaseInfoFlags flags1;
        public byte flags2; 
    }

}
