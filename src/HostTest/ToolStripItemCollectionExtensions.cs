/////////////////////////////////////////////////////////////////////////////////
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
using System.Collections.Generic;
using System.Windows.Forms;

namespace HostTest
{
    internal static class ToolStripItemCollectionExtensions
    {
        /// <summary>
        /// Sorts the ToolStripItemCollection using the specified comparer.
        /// </summary>
        /// <param name="collection">The collection of ToolStripItems.</param>
        /// <param name="comparer">The comparer to use for sorting the ToolStripItems.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="collection"/> is null.
        /// or
        /// <paramref name="comparer"/> is null.
        /// </exception>
        public static void Sort(this ToolStripItemCollection collection, IComparer<ToolStripItem> comparer)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            // If a ToolStripItemCollection only contains one item it does not need to be sorted.
            if (collection.Count > 1)
            {
                ToolStripItem[] items = new ToolStripItem[collection.Count];
                collection.CopyTo(items, 0);

                Array.Sort(items, comparer);

                collection.Clear();
                collection.AddRange(items);
            }
        }
    }
}
