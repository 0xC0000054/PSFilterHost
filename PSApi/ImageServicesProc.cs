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

/* Adapted from PIGeneral.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System;
using System.Runtime.InteropServices;
namespace PSFilterLoad.PSApi
{
#if USEIMAGESERVICES

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PIResampleProc(ref PSImagePlane source, ref PSImagePlane destination, ref Rect16 area, IntPtr coords, short method);

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
    
#endif


}
