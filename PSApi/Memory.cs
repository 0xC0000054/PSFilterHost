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

// Adapted from:
/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See License-pdn.txt for full licensing and attribution details.             //
//                                                                             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
	/// <summary>
	/// Contains methods for allocating and freeing memory from the default process heap.
	/// </summary>
	internal static class Memory 
	{
		private static IntPtr hHeap = SafeNativeMethods.GetProcessHeap();

		/// <summary>
		/// Allocates a block of memory from the default process heap.
		/// </summary>
		/// <param name="size">The size of the memory to allocate.</param>
		/// <param name="zeroFill">if <c>true</c> the allocated memory will be set to zero.</param>
		/// <returns>A pointer to the allocated block of memory.</returns>
		public static IntPtr Allocate(long size, bool zeroFill)
		{
			if (hHeap == IntPtr.Zero)
			{
				throw new InvalidOperationException("heap has already been destroyed");
			}

			IntPtr block = IntPtr.Zero;
			try
			{
				UIntPtr bytes = new UIntPtr((ulong)size);
				block = SafeNativeMethods.HeapAlloc(hHeap, zeroFill ? 8U : 0U, bytes);
			}
			catch (OverflowException ex)
			{
				throw new OutOfMemoryException(string.Format("Overflow while trying to allocate {0} bytes", size.ToString("N")), ex);
			}
			if (block == IntPtr.Zero)
			{
				throw new OutOfMemoryException(string.Format("HeapAlloc returned a null pointer while trying to allocate {0} bytes", size.ToString("N")));
			}

			if (size > 0L)
			{
				GC.AddMemoryPressure(size);
			}

			return block;
		}

		/// <summary>
		/// Frees the block of memory allocated by Allocate().
		/// </summary>
		/// <param name="hMem">The block to free.</param>
		public static void Free(IntPtr hMem)
		{
			if (hHeap != IntPtr.Zero)
			{
				long size = Size(hMem);
				if (!SafeNativeMethods.HeapFree(hHeap, 0, hMem))
				{
					int error = Marshal.GetLastWin32Error();

					throw new InvalidOperationException(string.Format("HeapFree returned an error {0}", error.ToString("X8", System.Globalization.CultureInfo.InvariantCulture)));
				}

				if (size > 0L)
				{
					GC.RemoveMemoryPressure(size);
				}
			}
		}

		/// <summary>
		/// Resizes the memory block previously allocated by Allocate().
		/// </summary>
		/// <param name="pv">The pointer to the block to resize.</param>
		/// <param name="newSize">The new size of the block.</param>
		/// <returns>The pointer to the resized block.</returns>
		public static IntPtr ReAlloc(IntPtr pv, long newSize)
		{
			if (hHeap == IntPtr.Zero)
			{
				throw new InvalidOperationException("heap has already been destroyed");
			}
			IntPtr block = IntPtr.Zero;

			long oldSize = Size(pv);

			try
			{
				UIntPtr bytes = new UIntPtr((ulong)newSize);
				block = SafeNativeMethods.HeapReAlloc(hHeap, 0U, pv, bytes);
			}
			catch (OverflowException ex)
			{
				throw new OutOfMemoryException(string.Format("Overflow while trying to allocate {0} bytes", newSize.ToString("N")), ex);
			}
			if (block == IntPtr.Zero)
			{
				throw new OutOfMemoryException(string.Format("HeapAlloc returned a null pointer while trying to allocate {0} bytes", newSize.ToString("N")));
			}

			if (oldSize > 0L)
			{
				GC.RemoveMemoryPressure(oldSize);
			}

			if (newSize > 0)
			{
				GC.AddMemoryPressure(newSize);
			}

			return block;
		}

		/// <summary>
		/// Retrieves the size of the allocated memory block
		/// </summary>
		/// <param name="hMem">The block pointer to retrieve the size of.</param>
		/// <returns>The size of the allocated block.</returns>
		public static long Size(IntPtr hMem)
		{
			if (hHeap != IntPtr.Zero)
			{
				return (long)SafeNativeMethods.HeapSize(hHeap, 0, hMem).ToUInt64();
			}

			return 0L;
		}
	}
}
