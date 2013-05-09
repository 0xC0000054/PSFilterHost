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

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PSFilterHostDll
{
   
    /// <summary>
    /// The collection of Pseudo-Resources used by the filters.
    /// </summary>
    public sealed class PseudoResourceCollection : ReadOnlyCollection<PSResource>
    {
        internal PseudoResourceCollection(IList<PSResource> list) : base(list)
        {
        }
    }
}
