/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2019 Nicholas Hayes
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
        public uint Type => type;

        /// <summary>
        /// Gets the flags.
        /// </summary>
        public int Flags => flags;

        /// <summary>
        /// Gets the size.
        /// </summary>
        public int Size => size;

        /// <summary>
        /// Gets the value.
        /// </summary>
        public object Value => value;

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

        public uint Unit => unit;

        public double Value => value;

        public UnitFloat(uint unit, double value)
        {
            this.unit = unit;
            this.value = value;
        }

        private UnitFloat(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            unit = info.GetUInt32("unit");
            value = info.GetDouble("value");
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue("unit", unit);
            info.AddValue("value", value);
        }
    }

    [Serializable]
    internal sealed class EnumeratedValue
    {
        private readonly uint type;
        private readonly uint value;

        public uint Type => type;

        public uint Value => value;

        public EnumeratedValue(uint type, uint value)
        {
            this.type = type;
            this.value = value;
        }
    }
}
