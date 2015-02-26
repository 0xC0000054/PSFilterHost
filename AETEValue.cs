/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2015 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

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
	internal struct UnitFloat
	{
		public uint unit;
		public double value;
	}
  
}
