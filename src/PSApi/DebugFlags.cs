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

namespace PSFilterHostDll.PSApi
{
#if DEBUG
    [System.Flags]
    internal enum DebugFlags
    {
        None = 0,
        AdvanceState = 1 << 0,
        BufferSuite = 1 << 1,
        Call = 1 << 2,
        ChannelPorts = 1 << 3,
        ColorServices = 1 << 4,
        DescriptorParameters = 1 << 5,
        DisplayPixels = 1 << 6,
        Error = 1 << 7,
        HandleSuite = 1 << 8,
        ImageServices = 1 << 9,
        MiscCallbacks = 1 << 10,
        PiPL = 1 << 11,
        PropertySuite = 1 << 12,
        ResourceSuite = 1 << 13,
        SPBasicSuite = 1 << 14
    }
#endif
}
