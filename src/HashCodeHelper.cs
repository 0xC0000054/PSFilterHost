/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2021 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

namespace PSFilterHostDll
{
    /// <summary>
    /// Contains methods to generate a hash code from multiple fields when overriding the <see cref="object.GetHashCode"/> method.
    /// </summary>
    internal static class HashCodeHelper
    {
        private const int InitialPrime = 23;
        private const int MultiplierPrime = 127;

        private static int CombineHashCodes(params int[] hashCodes)
        {
            if (hashCodes == null)
            {
                throw new ArgumentNullException(nameof(hashCodes));
            }

            int hash = InitialPrime;

            unchecked
            {
                for (int i = 0; i < hashCodes.Length; i++)
                {
                    hash = (hash * MultiplierPrime) + hashCodes[i];
                }
            }

            return hash;
        }

        private static int GetParameterHashCode<T>(T parameter)
        {
            if (EqualityComparer<T>.Default.Equals(parameter, default(T)))
            {
                return 0;
            }

            return parameter.GetHashCode();
        }

        /// <summary>
        /// Returns a hash code for the specified objects.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <param name="arg1">The first object to use when generating the hash code.</param>
        /// <param name="arg2">The second object to use when generating the hash code.</param>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        internal static int GetHashCode<T1, T2>(T1 arg1, T2 arg2)
        {
            int hash1 = GetParameterHashCode(arg1);
            int hash2 = GetParameterHashCode(arg2);

            return CombineHashCodes(hash1, hash2);
        }

        /// <summary>
        /// Returns a hash code for the specified objects.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
        /// <param name="arg1">The first object to use when generating the hash code.</param>
        /// <param name="arg2">The second object to use when generating the hash code.</param>
        /// <param name="arg3">The third object to use when generating the hash code.</param>
        /// <param name="arg4">The fourth object to use when generating the hash code.</param>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        internal static int GetHashCode<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            int hash1 = GetParameterHashCode(arg1);
            int hash2 = GetParameterHashCode(arg2);
            int hash3 = GetParameterHashCode(arg3);
            int hash4 = GetParameterHashCode(arg4);

            return CombineHashCodes(hash1, hash2, hash3, hash4);
        }
    }
}
