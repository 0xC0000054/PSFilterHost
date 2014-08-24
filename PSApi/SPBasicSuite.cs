/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2014 Nicholas Hayes
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

namespace PSFilterLoad.PSApi
{
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate int SPBasicAcquireSuite(IntPtr name, int version, ref IntPtr suite);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate int SPBasicReleaseSuite(IntPtr name, int version);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate int SPBasicIsEqual(IntPtr token1, IntPtr token2);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate int SPBasicAllocateBlock(int size, ref IntPtr block);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate int SPBasicFreeBlock(IntPtr block);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate int SPBasicReallocateBlock(IntPtr block, int newSize, ref IntPtr newblock);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
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
