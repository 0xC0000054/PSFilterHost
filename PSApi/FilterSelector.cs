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

/* Adapted from PIFilter.h
 * Copyright (c) 1990-1991, Thomas Knoll.
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/


namespace PSFilterHostDll.PSApi
{
    internal enum FilterSelector : short
    {
        About = 0,
        Parameters = 1,
        Prepare = 2,
        Start = 3,
        Continue = 4,
        Finish = 5
    }
}
