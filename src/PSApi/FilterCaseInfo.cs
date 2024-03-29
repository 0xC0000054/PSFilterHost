﻿/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2022 Nicholas Hayes
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

namespace PSFilterHostDll.PSApi
{
    /// <summary>
    /// The inputHandling and outputHandling constants for the FilterCaseInfo structure
    /// </summary>
    internal enum FilterDataHandling : byte
    {
        CantFilter = 0,
        None = 1,
        BlackMat = 2,
        GrayMat = 3,
        WhiteMat = 4,
        Defringe = 5,
        BlackZap = 6,
        GrayZap = 7,
        WhiteZap = 8,
        FillMask = 9,
        BackgroundZap = 10,
        ForegroundZap = 11,
    }
    /// <summary>
    /// The processing flags for the FilterCaseInfo structure.
    /// </summary>
    [Flags]
    internal enum FilterCaseInfoFlags : byte
    {
        None = 0,
        DontCopyToDestination = 1 << 0,
        WorksWithBlankData = 1 << 1,
        FiltersLayerMask = 1 << 2,
        WritesOutsideSelection = 1 << 3
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1), Serializable]
    internal readonly struct FilterCaseInfo
    {
        public readonly FilterDataHandling inputHandling;
        public readonly FilterDataHandling outputHandling;
        public readonly FilterCaseInfoFlags flags1;
        public readonly byte flags2;

        public const int SizeOf = 4;

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterCaseInfo"/> structure.
        /// </summary>
        /// <param name="inputHandling">The input handling.</param>
        /// <param name="outputHandling">The output handling.</param>
        /// <param name="flags1">The flags1.</param>
        /// <param name="flags2">The flags2.</param>
        public FilterCaseInfo(FilterDataHandling inputHandling, FilterDataHandling outputHandling, FilterCaseInfoFlags flags1, byte flags2)
        {
            this.inputHandling = inputHandling;
            this.outputHandling = outputHandling;
            this.flags1 = flags1;
            this.flags2 = flags2;
        }

        public bool IsSupported
        {
            get
            {
                return inputHandling != FilterDataHandling.CantFilter && outputHandling != FilterDataHandling.CantFilter;
            }
        }
    }
}
