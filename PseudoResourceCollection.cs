/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2018 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PSFilterHostDll
{

    /// <summary>
    /// The collection of Pseudo-Resources used by the filters.
    /// </summary>
    /// <threadsafety static="true" instance="false" />
    [Serializable]
    public sealed class PseudoResourceCollection : ReadOnlyCollection<PSResource>
    {
        internal PseudoResourceCollection(IList<PSResource> list) : base(list)
        {
        }
    }
}
