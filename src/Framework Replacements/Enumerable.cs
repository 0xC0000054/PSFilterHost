/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2020 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

#if NET20
namespace System.Linq
{
    internal static class Enumerable
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

        /// <summary>
        /// Returns an empty IEnumerable(T) that has the specified type argument.
        /// </summary>
        /// <typeparam name="TResult">The type to assign to the type parameter of the returned generic IEnumerable(T).</typeparam>
        /// <returns>An empty IEnumerable(T) whose type argument is TResult.</returns>
        public static System.Collections.Generic.IEnumerable<TResult> Empty<TResult>()
        {
            return EmptyClass<TResult>.Instance;
        }

        private sealed class EmptyClass<T>
        {
            public static readonly T[] Instance = new T[0];
        }
    }
}
#endif
