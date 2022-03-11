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

/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See License-pdn.txt for full licensing and attribution details.             //
//                                                                             //
/////////////////////////////////////////////////////////////////////////////////
using System;

namespace PSFilterHostDll.Imaging
{
    internal sealed unsafe class MemoryBlock : IDisposable
    {
        // blocks this size or larger are allocated with AllocateLarge (VirtualAlloc) instead of Allocate (HeapAlloc)
        private const long largeBlockThreshold = 65536;

        private long length;

        // if parentBlock == null, then we allocated the pointer and are responsible for deallocating it
        // if parentBlock != null, then the parentBlock allocated it, not us
        private void* voidStar;

        private bool valid; // if voidStar is null, and this is false, we know that it's null because allocation failed. otherwise we have a real error

        private MemoryBlock parentBlock = null;

        private bool disposed = false;

        public long Length
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(nameof(MemoryBlock));
                }

                return length;
            }
        }

        public IntPtr Pointer
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(nameof(MemoryBlock));
                }

                return new IntPtr(voidStar);
            }
        }

        public void* VoidStar
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(nameof(MemoryBlock));
                }

                return voidStar;
            }
        }

        /// <summary>
        /// Creates a new MemoryBlock instance and allocates the requested number of bytes.
        /// </summary>
        /// <param name="bytes"></param>
        public MemoryBlock(long bytes)
        {
            if (bytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bytes), bytes, "Bytes must be greater than zero");
            }

            length = bytes;
            parentBlock = null;
            voidStar = Allocate(bytes).ToPointer();
            valid = true;
        }

        ~MemoryBlock()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;

                if (disposing)
                {
                }

                if (valid && parentBlock == null)
                {
                    if (length >= largeBlockThreshold)
                    {
                        ImageSurfaceMemory.FreeLarge(new IntPtr(voidStar), (ulong)length);
                    }
                    else
                    {
                        ImageSurfaceMemory.Free(new IntPtr(voidStar));
                    }
                }

                parentBlock = null;
                voidStar = null;
                valid = false;
            }
        }

        private static IntPtr Allocate(long bytes)
        {
            return Allocate(bytes, true);
        }

        private static IntPtr Allocate(long bytes, bool allowRetry)
        {
            IntPtr block;

            try
            {
                if (bytes >= largeBlockThreshold)
                {
                    block = ImageSurfaceMemory.AllocateLarge((ulong)bytes);
                }
                else
                {
                    block = ImageSurfaceMemory.Allocate((ulong)bytes);
                }
            }
            catch (OutOfMemoryException)
            {
                if (allowRetry)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    return Allocate(bytes, false);
                }
                else
                {
                    throw;
                }
            }

            return block;
        }
    }
}
