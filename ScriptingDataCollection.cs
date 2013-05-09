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

namespace PSFilterHostDll
{
    using System;
    using System.Collections.Generic;
    using PSFilterLoad.PSApi;

    /// <summary>
    /// The collection that contains the filter's scripting data.
    /// </summary>
    [Serializable]
    public sealed class ScriptingDataCollection
    {
        private readonly IDictionary<uint, AETEValue> dictionary;

        internal ScriptingDataCollection(IDictionary<uint, AETEValue> data)
        {
            this.dictionary = data;
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        public int Count
        {
            get 
            {
                return dictionary.Count;
            }
        }

        internal Dictionary<uint, AETEValue> ToDictionary()
        {
            return new Dictionary<uint, AETEValue>(dictionary);
        }
    }
}
