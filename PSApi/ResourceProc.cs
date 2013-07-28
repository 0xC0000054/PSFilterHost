/////////////////////////////////////////////////////////////////////////////////
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

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short CountPIResourcesProc(uint type);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate IntPtr GetPIResourceProc(uint type, short index);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate void DeletePIResourceProc(uint type, short index);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
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
