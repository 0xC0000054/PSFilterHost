/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterHostDll
{
    [Serializable]
    internal sealed class PluginSettingsRegistryItem
    {
        private readonly ReadOnlyDictionary<uint, AETEValue> values;
        private readonly bool isPersistent;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginSettingsRegistryItem"/> class.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <param name="isPersistent"><c>true</c> if the item is persisted across host sessions; otherwise, <c>false</c>.</param>
        internal PluginSettingsRegistryItem(ReadOnlyDictionary<uint, AETEValue> values, bool isPersistent)
        {
            this.values = values;
            this.isPersistent = isPersistent;
        }

        /// <summary>
        /// Gets the values.
        /// </summary>
        /// <value>
        /// The values.
        /// </value>
        internal ReadOnlyDictionary<uint, AETEValue> Values
        {
            get
            {
                return this.values;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="PluginSettingsRegistryItem"/> is persisted across host sessions.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this item is persisted across host sessions; otherwise, <c>false</c>.
        /// </value>
        internal bool IsPersistent
        {
            get
            {
                return this.isPersistent;
            }
        }
    }
}
