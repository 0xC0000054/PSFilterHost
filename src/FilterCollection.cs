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

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PSFilterHostDll
{
    /// <summary>
    /// A collection containing the results of searching a directory for filters.
    /// </summary>
    /// <threadsafety static="true" instance="false" />
    public sealed class FilterCollection : ReadOnlyCollection<PluginData>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FilterCollection"/> class.
        /// </summary>
        /// <param name="pluginData">The data.</param>
        internal FilterCollection(IEnumerable<PluginData> pluginData) : base(new List<PluginData>(pluginData))
        {
        }
    }
}
