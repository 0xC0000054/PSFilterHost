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
	/// The class encapsulates the Pseudo–Resources used by the filters. 
	/// </summary>
	/// <threadsafety static="true" instance="false" />
	[Serializable]
	public sealed class PSResource : IEquatable<PSResource>, ISerializable
	{
		private uint key;
		private int index;
		private byte[] data;

		/// <summary>
		/// Gets the resource key.
		/// </summary>
		public long Key
		{
			get
			{
				return key;
			}
		}

		/// <summary>
		/// Gets the resource index.
		/// </summary>
		public int Index
		{
			get
			{
				return index;
			}
			internal set
			{
				index = value;
			}
		}

		/// <summary>
		/// Gets the resource data.
		/// </summary>
		/// <returns>The resource data byte array.</returns>
		public byte[] GetData()
		{
			return data;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PSResource"/> class.
		/// </summary>
		/// <param name="key">The resource key.</param>
		/// <param name="index">The resource index.</param>
		/// <param name="data">The resource data.</param>
		internal PSResource(uint key, int index, byte[] data)
		{
			this.key = key;
			this.index = index;
			this.data = data;
		}

		/// <summary>
		/// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
		/// </summary>
		/// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
		/// <returns>
		///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
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
				return false;

			return (this.key == other.key && this.index == other.index);
		}

		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		/// <returns>
		/// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
		/// </returns>
		public override int GetHashCode()
		{
			return (key.GetHashCode() ^ index.GetHashCode());
		}

		/// <summary>
		/// Compares this instance to the specified key and index for equality.
		/// </summary>
		/// <param name="otherKey">The key to compare.</param>
		/// <param name="otherIndex">The index to compare.</param>
		/// <returns>True if this instance is equal; otherwise false.</returns>
		internal bool Equals(uint otherKey, int otherIndex)
		{
			return (this.key == otherKey && this.index == otherIndex);
		}

		/// <summary>
		/// Compares this instance to the specified key for equality.
		/// </summary>
		/// <param name="otherKey">The key to compare.</param>
		/// <returns>True if this instance is equal; otherwise false.</returns>
		internal bool Equals(uint otherKey)
		{
			return (this.key == otherKey);
		}

		private PSResource(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
				throw new ArgumentNullException("info", "info is null.");

			this.key = info.GetUInt32("key");
			this.index = info.GetInt32("index");
			this.data = (byte[])info.GetValue("data", typeof(byte[]));
		}

        /// <summary>
        /// Populates a <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> to populate with data.</param>
        /// <param name="context">The destination (see <see cref="T:System.Runtime.Serialization.StreamingContext"/>) for this serialization.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="info"/> is null.</exception>
        /// <exception cref="T:System.Security.SecurityException">
        /// The caller does not have the required permission.
        ///   </exception>
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
				throw new ArgumentNullException("info", "info is null.");

			info.AddValue("key", this.key);
			info.AddValue("index", this.index);
			info.AddValue("data", this.data, typeof(byte[]));
		}
	}
}
