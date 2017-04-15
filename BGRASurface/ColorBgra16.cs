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

// Adapted from:
/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See License-pdn.txt for full licensing and attribution details.             //
//                                                                             //
/////////////////////////////////////////////////////////////////////////////////
using System;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.BGRASurface
{    

    [StructLayout(LayoutKind.Explicit)]
    internal struct ColorBgra16 
    {
        [FieldOffset(0)]
        public ushort B;
        [FieldOffset(2)]
        public ushort G;
        [FieldOffset(4)]
        public ushort R;
        [FieldOffset(6)]
        public ushort A;

        [FieldOffset(0)]
        public ulong Bgra;

    }
}