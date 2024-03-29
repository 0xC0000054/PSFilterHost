﻿/////////////////////////////////////////////////////////////////////////////////
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

using PSFilterHostDll.Interop;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.PSApi.PICA
{
    internal sealed class PICABufferSuite : IDisposable
    {
        private sealed class BufferEntry : IDisposable
        {
            private IntPtr pointer;
            private readonly uint size;
            private bool disposed;

            public BufferEntry(IntPtr pointer, uint size)
            {
                this.pointer = pointer;
                this.size = size;
                disposed = false;
            }

            public uint Size => size;

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                    }

                    if (pointer != IntPtr.Zero)
                    {
                        Memory.Free(pointer);
                        pointer = IntPtr.Zero;
                    }

                    disposed = true;
                }
            }

            ~BufferEntry()
            {
                Dispose(false);
            }
        }

        private readonly PSBufferSuiteNew bufferSuiteNew;
        private readonly PSBufferSuiteDispose bufferSuiteDispose;
        private readonly PSBufferSuiteGetSize bufferSuiteGetSize;
        private readonly PSBufferSuiteGetSpace bufferSuiteGetSpace;

        private Dictionary<IntPtr, BufferEntry> buffers;
        private bool disposed;

        public unsafe PICABufferSuite()
        {
            bufferSuiteNew = new PSBufferSuiteNew(PSBufferNew);
            bufferSuiteDispose = new PSBufferSuiteDispose(PSBufferDispose);
            bufferSuiteGetSize = new PSBufferSuiteGetSize(PSBufferGetSize);
            bufferSuiteGetSpace = new PSBufferSuiteGetSpace(PSBufferGetSpace);
            buffers = new Dictionary<IntPtr, BufferEntry>(IntPtrEqualityComparer.Instance);
            disposed = false;
        }

        private unsafe IntPtr PSBufferNew(uint* requestedSize, uint minimumSize)
        {
            IntPtr ptr = IntPtr.Zero;

            try
            {
                if (requestedSize != null && *requestedSize > minimumSize)
                {
                    uint allocatedSize = 0;
                    uint size = *requestedSize;
                    while (size > minimumSize)
                    {
                        // Allocate the largest buffer we can that is greater than the specified minimum size.
                        ptr = Memory.Allocate(size, MemoryAllocationFlags.ReturnZeroOnOutOfMemory);
                        if (ptr != IntPtr.Zero)
                        {
                            buffers.Add(ptr, new BufferEntry(ptr, size));
                            allocatedSize = size;
                            break;
                        }

                        size /= 2;
                    }

                    if (ptr == IntPtr.Zero)
                    {
                        // If we cannot allocate a buffer larger than the minimum size
                        // attempt to allocate a buffer at the minimum size.

                        ptr = Memory.Allocate(minimumSize, MemoryAllocationFlags.ReturnZeroOnOutOfMemory);
                        if (ptr != IntPtr.Zero)
                        {
                            buffers.Add(ptr, new BufferEntry(ptr, minimumSize));
                            allocatedSize = minimumSize;
                        }
                    }

                    // The requested size pointer is used as an output parameter to return the actual number of bytes allocated.
                    *requestedSize = allocatedSize;
                }
                else
                {
                    ptr = Memory.Allocate(minimumSize, MemoryAllocationFlags.ReturnZeroOnOutOfMemory);
                    if (ptr != IntPtr.Zero)
                    {
                        buffers.Add(ptr, new BufferEntry(ptr, minimumSize));
                    }
                }
            }
            catch (OutOfMemoryException)
            {
                // Free the buffer memory if the framework throws an OutOfMemoryException when adding to the buffers list.
                if (ptr != IntPtr.Zero)
                {
                    Memory.Free(ptr);
                    ptr = IntPtr.Zero;
                }
            }

            return ptr;
        }

        private unsafe void PSBufferDispose(IntPtr* buffer)
        {
            BufferEntry entry;
            if (buffer != null && buffers.TryGetValue(*buffer, out entry))
            {
                entry.Dispose();
                buffers.Remove(*buffer);
                // This method is documented to set the pointer to null after it has been freed.
                buffer = null;
            }
        }

        private uint PSBufferGetSize(IntPtr buffer)
        {
            BufferEntry entry;
            if (buffer != IntPtr.Zero && buffers.TryGetValue(buffer, out entry))
            {
                return entry.Size;
            }

            return 0;
        }

        private uint PSBufferGetSpace()
        {
            // Assume that we have 1 GB of available space.
            uint space = 1024 * 1024 * 1024;

            NativeStructs.MEMORYSTATUSEX buffer = new NativeStructs.MEMORYSTATUSEX
            {
                dwLength = (uint)Marshal.SizeOf(typeof(NativeStructs.MEMORYSTATUSEX))
            };

            if (SafeNativeMethods.GlobalMemoryStatusEx(ref buffer))
            {
                if (buffer.ullAvailVirtual < uint.MaxValue)
                {
                    space = (uint)buffer.ullAvailVirtual;
                }
            }

            return space;
        }

        public PSBufferSuite1 CreateBufferSuite1()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(PICABufferSuite));
            }

            PSBufferSuite1 suite = new PSBufferSuite1
            {
                New = Marshal.GetFunctionPointerForDelegate(bufferSuiteNew),
                Dispose = Marshal.GetFunctionPointerForDelegate(bufferSuiteDispose),
                GetSize = Marshal.GetFunctionPointerForDelegate(bufferSuiteGetSize),
                GetSpace = Marshal.GetFunctionPointerForDelegate(bufferSuiteGetSpace)
            };

            return suite;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                foreach (var item in buffers)
                {
                    item.Value.Dispose();
                }
            }
        }
    }
}
