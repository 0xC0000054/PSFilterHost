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

using System.Runtime.InteropServices;

namespace PSFilterHostDll.PSApi
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct PlugInMonitor
    {
        public Fixed16 gamma;
        public Fixed16 redX;
        public Fixed16 redY;
        public Fixed16 greenX;
        public Fixed16 greenY;
        public Fixed16 blueX;
        public Fixed16 blueY;
        public Fixed16 whiteX;
        public Fixed16 whiteY;
        public Fixed16 ambient;
    }
}
