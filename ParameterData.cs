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
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace PSFilterHostDll
{
    /// <summary>
    /// Represents the parameters used to reapply a filter with the same settings.
    /// </summary>
    /// <threadsafety static="true" instance="false" />
    [Serializable]
    public sealed class ParameterData : ISerializable
    {
        private readonly GlobalParameters globalParameters;
        private readonly Dictionary<uint, AETEValue> scriptingData;

        /// <summary>
        /// Gets the filter's global parameters.
        /// </summary>
        internal GlobalParameters GlobalParameters
        {
            get
            {
                return this.globalParameters;
            }
        }

        /// <summary>
        /// Gets the filter's AETE scripting values.
        /// </summary>
        internal Dictionary<uint, AETEValue> ScriptingData
        {
            get
            {
                return this.scriptingData;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterData"/> class.
        /// </summary>
        /// <param name="globals">The globals.</param>
        /// <param name="aete">The dictionary containing the scripting parameters.</param>
        internal ParameterData(GlobalParameters globals, Dictionary<uint, AETEValue> aete)
        {
            this.globalParameters = globals;

            if ((aete != null) && aete.Count > 0)
            {
                this.scriptingData = aete;
            }
            else
            {
                this.scriptingData = null;
            }
        }

        private ParameterData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");

            this.globalParameters = (GlobalParameters)info.GetValue("globalParameters", typeof(GlobalParameters));
            this.scriptingData =  (Dictionary<uint, AETEValue>)info.GetValue("scriptingData", typeof(Dictionary<uint, AETEValue>));
        }

        /// <summary>
        /// Populates a <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> to populate with data.</param>
        /// <param name="context">The destination (see <see cref="T:System.Runtime.Serialization.StreamingContext"/>) for this serialization.</param>
        /// <exception cref="T:System.Security.SecurityException">
        /// The caller does not have the required permission.
        ///   </exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="info"/> is null.</exception>
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info", "info is null.");

            info.AddValue("globalParameters", this.globalParameters, typeof(GlobalParameters));
            info.AddValue("scriptingData", this.scriptingData, typeof(Dictionary<uint, AETEValue>));
        }
    }
}
