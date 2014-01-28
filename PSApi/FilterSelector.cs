/////////////////////////////////////////////////////////////////////////////////
//
// Adobe(R) Photoshop(R) filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2014 Nicholas Hayes
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
    static class FilterSelector
    {
        public const int filterSelectorAbout = 0;
        public const int filterSelectorParameters = 1;
        public const int filterSelectorPrepare = 2;
        public const int filterSelectorStart = 3;
        public const int filterSelectorContinue = 4;
        public const int filterSelectorFinish = 5;
    }
}
