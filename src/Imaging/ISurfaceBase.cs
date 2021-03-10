/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2021 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterHostDll.Imaging
{
    internal interface ISurfaceBase : IDisposable
    {
        int Width { get; }

        int Height { get; }

        MemoryBlock Scan0 { get; }

        long Stride { get; }

        int BitsPerChannel { get; }

        int ChannelCount { get; }

        unsafe byte* GetRowAddressUnchecked(int y);
    }
}