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

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
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
    }
}
