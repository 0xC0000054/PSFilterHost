/////////////////////////////////////////////////////////////////////////////////
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
    internal unsafe delegate short GetPropertyProc(uint signature, uint key, int index, IntPtr* simpleProperty, IntPtr* complexProperty);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short SetPropertyProc(uint signature, uint key, int index, IntPtr simpleProperty, IntPtr complexProperty);

    [StructLayout(LayoutKind.Sequential)]
    internal struct PropertyProcs
    {
        public short propertyProcsVersion;
        public short numPropertyProcs;
        public IntPtr getPropertyProc;
        public IntPtr setPropertyProc;
    }
}
