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

/* Adapted from PIGeneral.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.PSApi
{
    internal enum MaskDescription : int
    {
        kSimplePSMask = 0,
        kBlackMatPSMask = 1,
        kGrayMatPSMask = 2,
        kWhiteMatPSMask = 3,
        kInvertPSMask = 4
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PSPixelMask
    {
        public IntPtr next;
        public IntPtr maskData;
        public int rowBytes;
        public int colBytes;
        public MaskDescription maskDescription;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PSPixelMap
    {
        public int version;
        public VRect bounds;
        public int imageMode;
        public int rowBytes;
        public int colBytes;
        public int planeBytes;
        public IntPtr baseAddr;
        public IntPtr mat;
        public IntPtr masks;
        public int maskPhaseRow;
        public int maskPhaseCol;

#if DEBUG
        public override string ToString() => string.Format(
                "version = {0}, bounds = {1}, ImageMode = {2}, colBytes = {3}, rowBytes = {4},planeBytes = {5}, BaseAddress = 0x{6}, mat = 0x{7}, masks = 0x{8}",
                new object[]{ version, bounds, ((ImageModes)imageMode).ToString("G"), colBytes, rowBytes, planeBytes, baseAddr.ToHexString(),
                     mat.ToHexString(), masks.ToHexString() });
#endif
    }
}
