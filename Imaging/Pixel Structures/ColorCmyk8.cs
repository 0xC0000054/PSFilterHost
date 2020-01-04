/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2020 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.Imaging
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct ColorCmyk8
    {
        [FieldOffset(0)]
        public byte C;

        [FieldOffset(1)]
        public byte M;

        [FieldOffset(2)]
        public byte Y;

        [FieldOffset(3)]
        public byte K;

        /// <summary>
        /// Lets you change C, M, Y, and K at the same time.
        /// </summary>
        [NonSerialized]
        [FieldOffset(0)]
        public uint Cmyk;
    }
}
