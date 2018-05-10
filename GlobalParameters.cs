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
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace PSFilterHostDll
{
    /// <summary>
    /// The class that encapsulates the filter's global parameter data.
    /// </summary>
    /// <threadsafety static="true" instance="false" />
    [Serializable]
    internal sealed class GlobalParameters : ISerializable
    {
        internal enum DataStorageMethod
        {
            HandleSuite,
            OTOFHandle,
            RawBytes
        }

        private byte[] parameterDataBytes;
        private DataStorageMethod parameterDataStorageMethod;
        private bool parameterDataExecutable;
        private byte[] pluginDataBytes;
        private DataStorageMethod pluginDataStorageMethod;
        private bool pluginDataExecutable;

        /// <summary>
        /// Gets the parameter data bytes.
        /// </summary>
        /// <returns>The parameter data bytes.</returns>
        internal byte[] GetParameterDataBytes()
        {
            return parameterDataBytes;
        }

        /// <summary>
        /// Sets the parameter data bytes.
        /// </summary>
        /// <param name="data">The data.</param>
        internal void SetParameterDataBytes(byte[] data)
        {
            parameterDataBytes = data;
        }

        /// <summary>
        /// Gets or sets the storage method of the parameter data.
        /// </summary>
        /// <value>
        /// The parameter data storage method.
        /// </value>
        internal DataStorageMethod ParameterDataStorageMethod
        {
            get
            {
                return parameterDataStorageMethod;
            }
            set
            {
                parameterDataStorageMethod = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter data memory must be executable.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the parameter data memory must be executable; otherwise, <c>false</c>.
        /// </value>
        internal bool ParameterDataExecutable
        {
            get
            {
                return parameterDataExecutable;
            }
            set
            {
                parameterDataExecutable = value;
            }
        }

        /// <summary>
        /// Gets the plug-in data bytes.
        /// </summary>
        /// <returns>The plug-in data bytes.</returns>
        internal byte[] GetPluginDataBytes()
        {
            return pluginDataBytes;
        }

        /// <summary>
        /// Sets the plug-in data bytes.
        /// </summary>
        /// <param name="data">The data.</param>
        internal void SetPluginDataBytes(byte[] data)
        {
            pluginDataBytes = data;
        }

        /// <summary>
        /// Gets or sets the storage method of the plug-in data.
        /// </summary>
        /// <value>
        /// The plug-in data storage method.
        /// </value>
        internal DataStorageMethod PluginDataStorageMethod
        {
            get
            {
                return pluginDataStorageMethod;
            }
            set
            {
                pluginDataStorageMethod = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether plugin data memory must be executable.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the plugin data memory must be executable; otherwise, <c>false</c>.
        /// </value>
        internal bool PluginDataExecutable
        {
            get
            {
                return pluginDataExecutable;
            }
            set
            {
                pluginDataExecutable = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalParameters"/> class.
        /// </summary>
        internal GlobalParameters()
        {
            parameterDataBytes = null;
            parameterDataStorageMethod = DataStorageMethod.HandleSuite;
            parameterDataExecutable = false;
            pluginDataBytes = null;
            pluginDataStorageMethod = DataStorageMethod.HandleSuite;
        }
        private GlobalParameters(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            parameterDataBytes = (byte[])info.GetValue("parameterDataBytes", typeof(byte[]));
            parameterDataStorageMethod = (DataStorageMethod)info.GetValue("parameterDataStorageMethod", typeof(DataStorageMethod));
            parameterDataExecutable = info.GetBoolean("parameterDataExecutable");

            pluginDataBytes = (byte[])info.GetValue("pluginDataBytes", typeof(byte[]));
            pluginDataStorageMethod = (DataStorageMethod)info.GetValue("pluginDataStorageMethod", typeof(DataStorageMethod));
            pluginDataExecutable = info.GetBoolean("pluginDataExecutable");
        }

        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue("parameterDataBytes", parameterDataBytes, typeof(byte[]));
            info.AddValue("parameterDataStorageMethod", parameterDataStorageMethod, typeof(DataStorageMethod));
            info.AddValue("parameterDataExecutable", parameterDataExecutable);

            info.AddValue("pluginDataBytes", pluginDataBytes, typeof(byte[]));
            info.AddValue("pluginDataStorageMethod", pluginDataStorageMethod, typeof(DataStorageMethod));
            info.AddValue("pluginDataExecutable", pluginDataExecutable);
        }
    }
}
