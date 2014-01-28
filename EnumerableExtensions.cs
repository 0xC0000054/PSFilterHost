/////////////////////////////////////////////////////////////////////////////////
//
// Adobe(R) Photoshop(R) filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2014 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

#if !NET_35_OR_GREATER
namespace System.Linq
{
    internal static class EnumerableExtensions
    {
        /// <summary>
        /// Determines whether a sequence contains any elements.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the collection.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <returns><c>true</c> if the collection contains at least one element; otherwise <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">collection is null.</exception>
        public static bool Any<T>(this System.Collections.Generic.IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection", "collection is null.");

            using (var enumerator = collection.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    return true;
                }
            }

            return false;
        }
    }
} 
#endif
