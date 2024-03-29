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
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short CountPIResourcesProc(uint type);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr GetPIResourceProc(uint type, short index);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void DeletePIResourceProc(uint type, short index);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short AddPIResourceProc(uint type, IntPtr data);

    [StructLayout(LayoutKind.Sequential)]
    internal struct ResourceProcs
    {
        public short resourceProcsVersion;
        public short numResourceProcs;
        public IntPtr countProc;
        public IntPtr getProc;
        public IntPtr deleteProc;
        public IntPtr addProc;
    }
}
