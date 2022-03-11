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

/* Adapted from PIFilter.h
 * Copyright (c) 1990-1991, Thomas Knoll.
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

namespace PSFilterHostDll.PSApi
{
    internal enum FilterCase : short
    {
        FlatImageNoSelection = 1,
        FlatImageWithSelection = 2,
        FloatingSelection = 3,
        EditableTransparencyNoSelection = 4,
        EditableTransparencyWithSelection = 5,
        ProtectedTransparencyNoSelection = 6,
        ProtectedTransparencyWithSelection = 7
    }
}