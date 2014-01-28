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
using System.Globalization;
using System.Security.Permissions;
using PSFilterLoad.PSApi;

namespace PSFilterHostDll.BGRASurface
{    
	internal static class BGRASurfaceMemory
	{
		private static IntPtr hHeap;
		/// <summary>
		/// Creates the heap.
		/// </summary>
		public static unsafe void CreateHeap()
		{
			if (hHeap == IntPtr.Zero)
			{
				SafeNativeMethods.HeapSetInformation(IntPtr.Zero, 1, null, new UIntPtr(0U)); // HeapEnableTerminationOnCorruption
				hHeap = SafeNativeMethods.HeapCreate(0, UIntPtr.Zero, UIntPtr.Zero);
				uint info = 2; // low fragmentation heap

				SafeNativeMethods.HeapSetInformation(hHeap, 0, (void*)&info, new UIntPtr(4U));
			}
		}

		/// <summary>
		/// Allocates a block of memory at least as large as the amount requested.
		/// </summary>
		/// <param name="bytes">The number of bytes you want to allocate.</param>
		/// <returns>A pointer to a block of memory at least as large as <b>bytes</b>.</returns>
		/// <exception cref="OutOfMemoryException">Thrown if the memory manager could not fulfill the request for a memory block at least as large as <b>bytes</b>.</exception>
		public static IntPtr Allocate(ulong bytes)
		{
			if (hHeap == IntPtr.Zero)
			{
				throw new InvalidOperationException("heap has already been destroyed");
			}
			else
			{
				IntPtr block = SafeNativeMethods.HeapAlloc(hHeap, 0, new UIntPtr(bytes));

				if (block == IntPtr.Zero)
				{
					throw new OutOfMemoryException("HeapAlloc returned a null pointer");
				}

				if (bytes > 0)
				{
					GC.AddMemoryPressure((long)bytes);
				}

				return block;
			}
		}

		/// <summary>
		/// Allocates a block of memory at least as large as the amount requested.
		/// </summary>
		/// <param name="bytes">The number of bytes you want to allocate.</param>
		/// <returns>A pointer to a block of memory at least as large as bytes</returns>
		/// <remarks>
		/// This method uses an alternate method for allocating memory (VirtualAlloc in Windows). The allocation
		/// granularity is the page size of the system (usually 4K). Blocks allocated with this method may also
		/// be protected using the ProtectBlock method.
		/// </remarks>
		public static IntPtr AllocateLarge(long bytes)
		{
			IntPtr block = SafeNativeMethods.VirtualAlloc(IntPtr.Zero, new UIntPtr((ulong)bytes),
				NativeConstants.MEM_COMMIT, NativeConstants.PAGE_READWRITE);

			if (block == IntPtr.Zero)
			{
				throw new OutOfMemoryException("VirtualAlloc returned a null pointer");
			}

			if (bytes > 0)
			{
				GC.AddMemoryPressure((long)bytes);
			}

			return block;
		}

		/// <summary>
		/// Frees a block of memory previously allocated with Allocate().
		/// </summary>
		/// <param name="block">The block to free.</param>
		/// <exception cref="InvalidOperationException">There was an error freeing the block.</exception>
		public static void Free(IntPtr block)
		{
			if (hHeap != IntPtr.Zero)
			{
				long bytes = (long)SafeNativeMethods.HeapSize(hHeap, 0, block);

				bool result = SafeNativeMethods.HeapFree(hHeap, 0, block);

				if (!result)
				{
					int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
					throw new InvalidOperationException("HeapFree returned an error: " + error.ToString(CultureInfo.InvariantCulture));
				}

				if (bytes > 0)
				{
					GC.RemoveMemoryPressure(bytes);
				}
			}
			else
			{
#if REPORTLEAKS
				throw new InvalidOperationException("memory leak! check the debug output for more info, and http://blogs.msdn.com/ricom/archive/2004/12/10/279612.aspx to track it down");
#endif
			}
		}

		/// <summary>
		/// Frees a block of memory previous allocated with AllocateLarge().
		/// </summary>
		/// <param name="block">The block to free.</param>
		/// <param name="bytes">The size of the block.</param>
		public static void FreeLarge(IntPtr block, ulong bytes)
		{
			bool result = SafeNativeMethods.VirtualFree(block, UIntPtr.Zero, NativeConstants.MEM_RELEASE);

			if (!result)
			{
				int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
				throw new InvalidOperationException("VirtualFree returned an error: " + error.ToString(CultureInfo.InvariantCulture));
			}

			if (bytes > 0)
			{
				GC.RemoveMemoryPressure((long)bytes);
			}
		}
		/// <summary>
		/// Copies bytes from one area of memory to another. Since this function only
		/// takes pointers, it can not do any bounds checking.
		/// </summary>
		/// <param name="dst">The starting address of where to copy bytes to.</param>
		/// <param name="src">The starting address of where to copy bytes from.</param>
		/// <param name="length">The number of bytes to copy</param>
		public static unsafe void Copy(void* dst, void* src, ulong length)
		{
			SafeNativeMethods.memcpy(dst, src, new UIntPtr(length));
		}

		/// <summary>
		/// Destroys the heap.
		/// </summary>
		public static void DestroyHeap()
		{
			if (hHeap != IntPtr.Zero)
			{
				SafeNativeMethods.HeapDestroy(hHeap);
				hHeap = IntPtr.Zero;
			}
		}
	   
	}
}
