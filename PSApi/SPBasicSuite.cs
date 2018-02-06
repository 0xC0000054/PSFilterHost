/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2018 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/* Adapted from SPBasic.h
 * Copyright 1986-1998 Adobe Systems Incorporated.
 * All Rights Reserved.
 */

using System;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.PSApi
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPBasicAcquireSuite(IntPtr name, int version, ref IntPtr suite);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPBasicReleaseSuite(IntPtr name, int version);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal delegate bool SPBasicIsEqual(IntPtr token1, IntPtr token2);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPBasicAllocateBlock(int size, ref IntPtr block);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPBasicFreeBlock(IntPtr block);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPBasicReallocateBlock(IntPtr block, int newSize, ref IntPtr newblock);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPBasicUndefined();

    [StructLayout(LayoutKind.Sequential)]
    internal struct SPBasicSuite
    {
        public IntPtr acquireSuite;
        public IntPtr releaseSuite;
        public IntPtr isEqual;
        public IntPtr allocateBlock;
        public IntPtr freeBlock;
        public IntPtr reallocateBlock;
        public IntPtr undefined;
    }
}
