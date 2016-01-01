/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2016 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/* Adapted from PIFilter.h
 * Copyright (c) 1990-1991, Thomas Knoll.
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

namespace PSFilterHostDll.PSApi
{
    internal static class FilterCase
    {
        public const short Unsupported = -1;
        public const short FlatImageNoSelection = 1;
        public const short FlatImageWithSelection = 2;
        public const short FloatingSelection = 3;
        public const short EditableTransparencyNoSelection = 4;
        public const short EditableTransparencyWithSelection = 5;
        public const short ProtectedTransparencyNoSelection = 6;
        public const short ProtectedTransparencyWithSelection = 7;
    }
}