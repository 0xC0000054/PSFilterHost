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
	/// The class containing the results of searching a directory for filters.
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
