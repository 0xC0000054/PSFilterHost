/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2017 Nicholas Hayes
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

namespace PSFilterHostDll.PSApi
{
#if USEIMAGESERVICES
    [StructLayout(LayoutKind.Sequential)]
    internal struct PSImagePlane
    {
        public IntPtr data;
        public Rect16 bounds;
        public int rowBytes;
        public int colBytes;
    } 
#endif
    
}
