/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2017 Nicholas Hayes
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
    internal delegate short AllocateBufferProc(int size, ref IntPtr bufferID);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr LockBufferProc(IntPtr bufferID, byte moveHigh);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void UnlockBufferProc(IntPtr bufferID);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void FreeBufferProc(IntPtr bufferID);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int BufferSpaceProc();

    [StructLayout(LayoutKind.Sequential)]
    internal struct BufferProcs
    {
        public short bufferProcsVersion;
        public short numBufferProcs;
        public IntPtr allocateProc;
        public IntPtr lockProc;
        public IntPtr unlockProc;
        public IntPtr freeProc;
        public IntPtr spaceProc;
    }

}
