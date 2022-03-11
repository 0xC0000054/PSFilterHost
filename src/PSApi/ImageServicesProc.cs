/////////////////////////////////////////////////////////////////////////////////
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
    [StructLayout(LayoutKind.Sequential)]
    internal struct PSImagePlane
    {
        public IntPtr data;
        public Rect16 bounds;
        public int rowBytes;
        public int colBytes;

#if DEBUG
        public override string ToString() => bounds.ToString();
#endif
    }

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal unsafe delegate short PIResampleProc(PSImagePlane* source, PSImagePlane* destination, Rect16* area, IntPtr coords, short method);

    [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal struct ImageServicesProcs
    {
        public short imageServicesProcsVersion;
        public short numImageServicesProcs;
        public IntPtr interpolate1DProc;
        public IntPtr interpolate2DProc;
    }

    internal enum InterpolationModes
    {
        PointSampling = 0,
        Bilinear,
        Bicubic
    }
}
