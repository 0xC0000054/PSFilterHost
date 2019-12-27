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

/* Adapted from PIErrorSuite.h
 * Copyright (c) 1997-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.PSApi.PICA
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ErrorSuiteSetErrorFromPString(IntPtr str);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ErrorSuiteSetErrorFromCString(IntPtr str);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ErrorSuiteSetErrorFromZString(ASZString str);

    [StructLayout(LayoutKind.Sequential)]
    internal struct PSErrorSuite1
    {
        public IntPtr SetErrorFromPString;
        public IntPtr SetErrorFromCString;
        public IntPtr SetErrorFromZString;
    }
}
