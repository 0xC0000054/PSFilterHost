﻿/////////////////////////////////////////////////////////////////////////////////
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

namespace PSFilterHostDll
{
    /// <summary>
    /// The delegate the filter can call for the host to tell it to cancel any rendering currently in progress.
    /// </summary>
    /// <returns><c>true</c> if the filter should cancel rendering; otherwise <c>false</c>.</returns>
    public delegate bool AbortFunc();
}