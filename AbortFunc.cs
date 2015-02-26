/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2015 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

namespace PSFilterHostDll
{ 
    /// <summary>
    /// The delegate the filter can call for the host to tell it to abort.
    /// </summary>
    /// <returns>True if the filter should abort; otherwise false.</returns>
    public delegate bool AbortFunc();
}