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

namespace PSFilterHostDll.PSApi
{
    // As IntPtr does not implement IEquatable<T> in .NET 4.6.2 and earlier
    // using EqualityComparer<IntPtr>.Default will cause it to be boxed during comparisons.
    // This comparer is used with any types that need an IEqualityComparer<IntPtr> to prevent boxing.

    /// <summary>
    /// Represents an equality comparer for <see cref="IntPtr"/>.
    /// </summary>
    /// <seealso cref="IEqualityComparer{T}"/>
    internal sealed class IntPtrEqualityComparer : IEqualityComparer<IntPtr>
    {
        private static readonly IntPtrEqualityComparer instance = new IntPtrEqualityComparer();

        private IntPtrEqualityComparer()
        {
        }

        public static IntPtrEqualityComparer Instance
        {
            get
            {
                return instance;
            }
        }

        public bool Equals(IntPtr x, IntPtr y)
        {
            return x == y;
        }

        public int GetHashCode(IntPtr obj)
        {
            return obj.GetHashCode();
        }
    }
}
