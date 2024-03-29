﻿/////////////////////////////////////////////////////////////////////////////////
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

using System;

namespace PSFilterHostDll.Imaging
{
    [Flags]
    internal enum SurfaceCreationOptions
    {
        /// <summary>
        /// The default option.
        /// </summary>
        Default = 0,

        /// <summary>
        /// The stride is rounded to a multiple of four.
        /// </summary>
        GdiPlusCompatableStride = 1
    }
}
