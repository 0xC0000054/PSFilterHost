/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2014 Nicholas Hayes
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


namespace PSFilterLoad.PSApi
{ 
    [StructLayout(LayoutKind.Sequential)]
    internal struct PlugInMonitor
    {
        public int gamma;
        public int redX;
        public int redY;
        public int greenX;
        public int greenY;
        public int blueX;
        public int blueY;
        public int whiteX;
        public int whiteY;
        public int ambient;
    }

}
