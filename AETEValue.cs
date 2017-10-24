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
using System.Runtime.Serialization;

namespace PSFilterHostDll
{
	/// <summary>
	/// The class that encapsulates an AETE scripting parameter.
	/// </summary>
	[Serializable]
	internal sealed class AETEValue
	{
		private uint type;
		private int flags;
		private int size;
		private object value;

		/// <summary>
		/// Gets the type of data.
		/// </summary>
		public uint Type
		{
			get
			{
				return type;
			}
		}


		/// <summary>
		/// Gets the flags.
		/// </summary>
		public int Flags
		{
			get
			{
				return flags;
			}
		}

		/// <summary>
		/// Gets the size.
		/// </summary>
		public int Size
		{
			get
			{
				return size;
			}
		}

		/// <summary>
		/// Gets the value.
		/// </summary>
		public object Value
		{
			get
			{
				return value;
			}
		}

		internal AETEValue(uint type, int flags, int size, object value)
		{
			this.type = type;
			this.flags = flags;
			this.size = size;
			this.value = value;
		}
	}

	[Serializable]
	internal sealed class UnitFloat : ISerializable
	{
		private readonly uint unit;
		private readonly double value;

		public uint Unit
		{
			get
			{
				return unit;
			}
		}

		public double Value
		{
			get
			{
				return value;
			}
		}

		public UnitFloat(uint unit, double value)
		{
			this.unit = unit;
			this.value = value;
		}

		private UnitFloat(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}

			this.unit = info.GetUInt32("unit");
			this.value = info.GetDouble("value");
		}

		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}

			info.AddValue("unit", this.unit);
			info.AddValue("value", this.value);
		}
	}

	[Serializable]
	internal sealed class EnumeratedValue
	{
		private readonly uint type;
		private readonly uint value;

		public uint Type
		{
			get
			{
				return this.type;
			}
		}

		public uint Value
		{
			get
			{
				return this.value;
			}
		}

		public EnumeratedValue(uint type, uint value)
		{
			this.type = type;
			this.value = value;
		}
	}
}
