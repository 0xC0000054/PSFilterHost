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

namespace PSFilterHostDll.PSApi
{
    /// <summary>
    /// The callback that reports the render progress to the host.
    /// </summary>
    /// <param name="done">The amount of work done.</param>
    /// <param name="total">The total amount of work.</param>
    internal delegate void ProgressFunc(int done, int total);
}
