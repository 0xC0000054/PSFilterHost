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

/* Adapted from PIActions.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.PSApi
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int DescriptorRegistryRegister(IntPtr key, IntPtr descriptor, [MarshalAs(UnmanagedType.U1)] bool isPersistent);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int DescriptorRegistryErase(IntPtr key);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int DescriptorRegistryGet(IntPtr key, ref IntPtr descriptor);

    internal struct PSDescriptorRegistryProcs
    {
        public IntPtr Register;
        public IntPtr Erase;
        public IntPtr Get;
    }
}
