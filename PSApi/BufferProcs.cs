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
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
    internal delegate short AllocateBufferProc(int size, ref System.IntPtr bufferID);
    
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
    internal delegate IntPtr LockBufferProc(IntPtr bufferID, byte moveHigh);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
    internal delegate void UnlockBufferProc(IntPtr bufferID);

   
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
    internal delegate void FreeBufferProc(IntPtr bufferID);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
    internal delegate int BufferSpaceProc();

    [StructLayout(LayoutKind.Sequential)]
    struct BufferProcs
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
