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
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate void RecoverSpaceProc(int size);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate IntPtr NewPIHandleProc(int size);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate void DisposePIHandleProc(System.IntPtr h);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate int GetPIHandleSizeProc(System.IntPtr h);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short SetPIHandleSizeProc(System.IntPtr h, int newSize);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate IntPtr LockPIHandleProc(System.IntPtr h, byte moveHigh);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate void UnlockPIHandleProc(System.IntPtr h);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate void DisposeRegularPIHandleProc(IntPtr h);

    [StructLayoutAttribute(LayoutKind.Sequential)]
    internal struct HandleProcs
    {
        public short handleProcsVersion;
        public short numHandleProcs;
        public IntPtr newProc;
        public IntPtr disposeProc;
        public IntPtr getSizeProc;
        public IntPtr setSizeProc;
        public IntPtr lockProc;
        public IntPtr unlockProc;
        public IntPtr recoverSpaceProc;
        public IntPtr disposeRegularHandleProc;
    }

}
