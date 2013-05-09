/////////////////////////////////////////////////////////////////////////////////
//
// Adobe(R) Photoshop(R) filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2013 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterLoad.PSApi
{
#if DEBUG
    [Flags]
    enum DebugFlags
    {
        None = 0,
        AdvanceState = 1,
        BufferSuite = 2,
        Call = 4,
        ChannelPorts = 8,
        ColorServices = 16,        
        DescriptorParameters = 32,
        DisplayPixels = 64,
        Error = 128,
        HandleSuite = 256,
        ImageServices = 512,
        MiscCallbacks = 1024,
        PiPL = 2048,
        SPBasicSuite = 4096
    }
#endif
}
