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

using PSFilterHostDll.Interop;

namespace PSFilterHostDll.Imaging
{
    internal interface IColorManagedSurface : ISurfaceBase
    {
        NativeEnums.Mscms.BMFORMAT MscmsFormat { get; }
    }
}
