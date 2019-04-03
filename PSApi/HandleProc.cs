/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2019 Nicholas Hayes
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
    internal delegate void RecoverSpaceProc(int size);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr NewPIHandleProc(int size);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void DisposePIHandleProc(IntPtr h);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int GetPIHandleSizeProc(IntPtr h);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short SetPIHandleSizeProc(IntPtr h, int newSize);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr LockPIHandleProc(IntPtr h, byte moveHigh);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void UnlockPIHandleProc(IntPtr h);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
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
