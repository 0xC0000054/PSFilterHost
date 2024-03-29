﻿/////////////////////////////////////////////////////////////////////////////////
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

using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace PSFilterHostDll
{
    /// <summary>
    /// Encapsulates the Pseudo–Resources used by the filters.
    /// </summary>
    /// <threadsafety static="true" instance="false" />
    [Serializable]
    public sealed class PSResource : IEquatable<PSResource>, ISerializable
    {
        private readonly uint key;
        private readonly int index;
        private byte[] data;

        /// <summary>
        /// Gets the resource key.
        /// </summary>
        public long Key => key;

        /// <summary>
        /// Gets the resource index.
        /// </summary>
        public int Index => index;

        /// <summary>
        /// Gets the resource data.
        /// </summary>
        /// <returns>The resource data byte array.</returns>
        public byte[] GetData()
        {
            return (byte[])data.Clone();
        }

        /// <summary>
        /// Gets the resource data without cloning the array.
        /// </summary>
        /// <returns>A direct reference to the resource data array.</returns>
        internal byte[] GetDataReadOnly()
        {
            return data;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PSResource"/> class.
        /// </summary>
        /// <param name="resourceKey">The resource key.</param>
        /// <param name="resourceIndex">The resource index.</param>
        /// <param name="resourceData">The resource data.</param>
        /// <exception cref="ArgumentNullException"><paramref name="resourceData"/> is null.</exception>
        internal PSResource(uint resourceKey, int resourceIndex, byte[] resourceData)
        {
            if (resourceData == null)
            {
                throw new ArgumentNullException(nameof(resourceData));
            }

            key = resourceKey;
            index = resourceIndex;
            data = (byte[])resourceData.Clone();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PSResource"/> class, from an existing instance with a new resource index.
        /// </summary>
        /// <param name="existing">The existing instance.</param>
        /// <param name="newIndex">The new resource index for the instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="existing"/> is null.</exception>
        internal PSResource(PSResource existing, int newIndex)
        {
            if (existing == null)
            {
                throw new ArgumentNullException(nameof(existing));
            }

            key = existing.key;
            index = newIndex;
            data = (byte[])existing.data.Clone();
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            PSResource other = obj as PSResource;
            if (other != null)
            {
                return Equals(other);
            }

            return false;
        }
        /// <summary>
        /// Determines whether the specified <see cref="PSResource"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="PSResource"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="PSResource"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(PSResource other)
        {
            if (other == null)
            {
                return false;
            }

            return key == other.key && index == other.index;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            return HashCodeHelper.GetHashCode(key, index);
        }

        /// <summary>
        /// Determines whether two PSResource instances have the same value.
        /// </summary>
        /// <param name="p1">The first object to compare.</param>
        /// <param name="p2">The second object to compare.</param>
        /// <returns>
        /// <c>true</c> if the PSResource instances are equal; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator ==(PSResource p1, PSResource p2)
        {
            if (((object)p1) == null || ((object)p2) == null)
            {
                return Object.Equals(p1, p2);
            }

            return p1.Equals(p2);
        }

        /// <summary>
        /// Determines whether two PSResource instances do not have the same value.
        /// </summary>
        /// <param name="p1">The first object to compare.</param>
        /// <param name="p2">The second object to compare.</param>
        /// <returns>
        /// <c>true</c> if the PSResource instances are not equal; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator !=(PSResource p1, PSResource p2)
        {
            return !(p1 == p2);
        }

        /// <summary>
        /// Compares this instance to the specified key and index for equality.
        /// </summary>
        /// <param name="otherKey">The key to compare.</param>
        /// <param name="otherIndex">The index to compare.</param>
        /// <returns>True if this instance is equal; otherwise false.</returns>
        internal bool Equals(uint otherKey, int otherIndex)
        {
            return key == otherKey && index == otherIndex;
        }

        /// <summary>
        /// Compares this instance to the specified key for equality.
        /// </summary>
        /// <param name="otherKey">The key to compare.</param>
        /// <returns>True if this instance is equal; otherwise false.</returns>
        internal bool Equals(uint otherKey)
        {
            return key == otherKey;
        }

        private PSResource(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            key = info.GetUInt32("key");
            index = info.GetInt32("index");
            data = (byte[])info.GetValue("data", typeof(byte[]));
        }

        /// <summary>
        /// Populates a <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> to populate with data.</param>
        /// <param name="context">The destination (see <see cref="T:System.Runtime.Serialization.StreamingContext"/>) for this serialization.</param>
        /// <exception cref="ArgumentNullException"><paramref name="info"/> is null.</exception>
        /// <exception cref="T:System.Security.SecurityException">
        /// The caller does not have the required permission.
        ///   </exception>
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue("key", key);
            info.AddValue("index", index);
            info.AddValue("data", data, typeof(byte[]));
        }
    }
}
