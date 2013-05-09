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

/* Adapted from PIFilter.h
 * Copyright (c) 1990-1991, Thomas Knoll.
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

namespace PSFilterLoad.PSApi
{
    static class FilterCase
    {
        public const short filterCaseUnsupported = -1;
        public const short filterCaseFlatImageNoSelection = 1;
        public const short filterCaseFlatImageWithSelection = 2;
        public const short filterCaseFloatingSelection = 3;
        public const short filterCaseEditableTransparencyNoSelection = 4;
        public const short filterCaseEditableTransparencyWithSelection = 5;
        public const short filterCaseProtectedTransparencyNoSelection = 6;
        public const short filterCaseProtectedTransparencyWithSelection = 7;
    }
}