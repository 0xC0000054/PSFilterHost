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

using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace PSFilterHostDll
{
    /// <summary>
    /// The class that encapsulates the filter's global parameter data.
    /// </summary>
    [Serializable]
    public sealed class GlobalParameters : ISerializable
    {
        private long parameterDataSize;
        private byte[] parameterDataBytes;
        private bool parameterDataIsPSHandle;
        private long pluginDataSize;
        private byte[] pluginDataBytes;
        private bool pluginDataIsPSHandle;
        private int storeMethod;

        /// <summary>
        /// Gets the parameter data bytes.
        /// </summary>
        /// <returns>The parameter data bytes.</returns>
        public byte[] GetParameterDataBytes()
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
        /// Gets the size of the parameter data.
        /// </summary>
        public long ParameterDataSize
        {
            get
            {
                return parameterDataSize;
            }
            internal set
            {
                parameterDataSize = value;
            }
        }

        /// <summary>
        /// Gets if the parameter data a PS Handle.
        /// </summary>
        public bool ParameterDataIsPSHandle
        {
            get
            {
                return parameterDataIsPSHandle;
            }
            internal set
            {
                parameterDataIsPSHandle = value;
            }
        }

        /// <summary>
        /// Gets the plugin data bytes.
        /// </summary>
        /// <returns>The plugin data bytes.</returns>
        public byte[] GetPluginDataBytes()
        {
            return pluginDataBytes;
        }

        /// <summary>
        /// Sets the plugin data bytes.
        /// </summary>
        /// <param name="data">The data.</param>
        internal void SetPluginDataBytes(byte[] data)
        {
            pluginDataBytes = data;
        }
        /// <summary>
        /// Gets the size of the plugin data.
        /// </summary>
        public long PluginDataSize
        {
            get
            {
                return pluginDataSize;
            }
            internal set
            {
                pluginDataSize = value;
            }
        }

        /// <summary>
        /// Gets if the plugin data is a PS Handle.
        /// </summary>
        public bool PluginDataIsPSHandle
        {
            get
            {
                return pluginDataIsPSHandle;
            }
            internal set
            {
                pluginDataIsPSHandle = value;
            }
        }

        /// <summary>
        /// Gets the store method.
        /// </summary>
        public int StoreMethod
        {
            get
            {
                return storeMethod;
            }
            internal set
            {
                storeMethod = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalParameters"/> class.
        /// </summary>
        public GlobalParameters()
        {
            this.parameterDataSize = 0;
            this.parameterDataBytes = null;
            this.parameterDataIsPSHandle = false;
            this.pluginDataSize = 0;
            this.pluginDataBytes = null;
            this.pluginDataIsPSHandle = false;
            this.storeMethod = 0;
        }
        private GlobalParameters(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new System.ArgumentNullException("info");

            this.parameterDataSize = info.GetInt64("parameterDataSize");
            this.parameterDataBytes = (byte[])info.GetValue("parameterDataBytes", typeof(byte[]));
            this.parameterDataIsPSHandle = info.GetBoolean("parameterDataIsPSHandle");
            this.pluginDataSize = info.GetInt64("pluginDataSize");
            this.pluginDataBytes = (byte[])info.GetValue("pluginDataBytes", typeof(byte[]));
            this.pluginDataIsPSHandle = info.GetBoolean("pluginDataIsPSHandle");
            this.storeMethod = info.GetInt32("storeMethod");
        }
        /// <summary>
        /// Populates a <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> to populate with data.</param>
        /// <param name="context">The destination (see <see cref="T:System.Runtime.Serialization.StreamingContext"/>) for this serialization.</param>
        /// <exception cref="T:System.Security.SecurityException">
        /// The caller does not have the required permission.
        ///   </exception>
        ///   <exception cref="T:System.ArgumentNullException">The SerializationInfo is null.</exception>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new System.ArgumentNullException("info");

            info.AddValue("parameterDataSize", this.parameterDataSize);
            info.AddValue("parameterDataBytes", this.parameterDataBytes, typeof(byte[]));
            info.AddValue("parameterDataIsPSHandle", this.parameterDataIsPSHandle);
            info.AddValue("pluginDataSize", this.pluginDataSize);
            info.AddValue("pluginDataBytes", this.pluginDataBytes, typeof(byte[]));
            info.AddValue("pluginDataIsPSHandle", this.pluginDataIsPSHandle);
            info.AddValue("storeMethod", this.storeMethod);
        }
    }
}
