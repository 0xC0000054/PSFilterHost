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
using System.Collections.Generic;

namespace PSFilterHostDll
{
    /// <summary>
    /// Represents settings that can be shared between plug-ins or saved and restored across host sessions.
    /// </summary>
    /// <remarks>
    /// Plug-ins can use this to communicate or store data that the host will save and restore between sessions.
    /// </remarks>
    /// <threadsafety static="true" instance="false"/>
    [Serializable]
    public sealed class PluginSettingsRegistry
    {
        private readonly ReadOnlyDictionary<string, PluginSettingsRegistryItem> persistedValues;
        [NonSerialized]
        private readonly ReadOnlyDictionary<string, PluginSettingsRegistryItem> sessionValues;
        [NonSerialized]
        private bool dirty;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginSettingsRegistry"/> class.
        /// </summary>
        /// <param name="values">The registry values.</param>
        /// <param name="persistentValuesChanged"><c>true</c> if the persistent values have been changed; otherwise, <c>false</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is null.</exception>
        internal PluginSettingsRegistry(Dictionary<string, PluginSettingsRegistryItem> values, bool persistentValuesChanged)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            Dictionary<string, PluginSettingsRegistryItem> persistentItems = new Dictionary<string, PluginSettingsRegistryItem>(StringComparer.Ordinal);
            Dictionary<string, PluginSettingsRegistryItem> sessionItems = new Dictionary<string, PluginSettingsRegistryItem>(StringComparer.Ordinal);

            foreach (var item in values)
            {
                if (item.Value.IsPersistent)
                {
                    persistentItems.Add(item.Key, item.Value);
                }
                else
                {
                    sessionItems.Add(item.Key, item.Value);
                }
            }

            this.persistedValues = new ReadOnlyDictionary<string, PluginSettingsRegistryItem>(persistentItems);
            this.sessionValues = new ReadOnlyDictionary<string, PluginSettingsRegistryItem>(sessionItems);
            // Mark the plug-in settings as dirty when the persisted items change.
            // The host can use this information to determine if it needs to save the plug-in settings.
            this.dirty = persistentValuesChanged;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the plug-in settings have been marked as changed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the plug-in settings have changed; otherwise, <c>false</c>.
        /// </value>
        public bool Dirty
        {
            get
            {
                return this.dirty;
            }
            set
            {
                this.dirty = value;
            }
        }

        /// <summary>
        /// Gets the values that are persisted between host sessions.
        /// </summary>
        /// <value>
        /// The values that are persisted between host sessions.
        /// </value>
        internal ReadOnlyDictionary<string, PluginSettingsRegistryItem> PersistedValues
        {
            get
            {
                return this.persistedValues;
            }
        }

        /// <summary>
        /// Gets the values that are stored for the current session.
        /// </summary>
        /// <value>
        /// The values that are stored for the current session.
        /// </value>
        internal ReadOnlyDictionary<string, PluginSettingsRegistryItem> SessionValues
        {
            get
            {
                return this.sessionValues;
            }
        }
    }
}
