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

using PSFilterHostDll.Interop;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.PSApi
{
    // This class is a singleton because plug-ins can use it to allocate memory for pointers embedded
    // in the API structures that will be freed when the LoadPsFilter class is finalized.
    internal sealed class BufferSuite
    {
        private readonly AllocateBufferProc allocProc;
        private readonly FreeBufferProc freeProc;
        private readonly LockBufferProc lockProc;
        private readonly UnlockBufferProc unlockProc;
        private readonly BufferSpaceProc spaceProc;
        private readonly Dictionary<IntPtr, int> bufferIDs;

        private BufferSuite()
        {
            allocProc = new AllocateBufferProc(AllocateBufferProc);
            freeProc = new FreeBufferProc(BufferFreeProc);
            lockProc = new LockBufferProc(BufferLockProc);
            unlockProc = new UnlockBufferProc(BufferUnlockProc);
            spaceProc = new BufferSpaceProc(BufferSpaceProc);
            bufferIDs = new Dictionary<IntPtr, int>(IntPtrEqualityComparer.Instance);
        }

        public static BufferSuite Instance { get; } = new BufferSuite();

        public int AvailableSpace => BufferSpaceProc();

        public bool AllocatedBySuite(IntPtr buffer)
        {
            return bufferIDs.ContainsKey(buffer);
        }

        public IntPtr CreateBufferProcsPointer()
        {
            IntPtr bufferProcsPtr = Memory.Allocate(Marshal.SizeOf(typeof(BufferProcs)), true);

            unsafe
            {
                BufferProcs* bufferProcs = (BufferProcs*)bufferProcsPtr.ToPointer();
                bufferProcs->bufferProcsVersion = PSConstants.kCurrentBufferProcsVersion;
                bufferProcs->numBufferProcs = PSConstants.kCurrentBufferProcsCount;
                bufferProcs->allocateProc = Marshal.GetFunctionPointerForDelegate(allocProc);
                bufferProcs->freeProc = Marshal.GetFunctionPointerForDelegate(freeProc);
                bufferProcs->lockProc = Marshal.GetFunctionPointerForDelegate(lockProc);
                bufferProcs->unlockProc = Marshal.GetFunctionPointerForDelegate(unlockProc);
                bufferProcs->spaceProc = Marshal.GetFunctionPointerForDelegate(spaceProc);
            }

            return bufferProcsPtr;
        }

        public void FreeBuffer(IntPtr bufferID)
        {
            BufferUnlockProc(bufferID);
            BufferFreeProc(bufferID);
        }

        public void FreeRemainingBuffers()
        {
            foreach (KeyValuePair<IntPtr, int> item in bufferIDs)
            {
                Memory.Free(item.Key);
            }
            bufferIDs.Clear();
        }

        public int GetBufferSize(IntPtr bufferID)
        {
            if (bufferIDs.TryGetValue(bufferID, out int size))
            {
                return size;
            }

            return 0;
        }

        public IntPtr LockBuffer(IntPtr bufferID)
        {
            return BufferLockProc(bufferID, 0);
        }

        public void UnlockBuffer(IntPtr bufferID)
        {
            BufferUnlockProc(bufferID);
        }

        private short AllocateBufferProc(int size, ref IntPtr bufferID)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.BufferSuite, string.Format("Size: {0}", size));
#endif
            if (size < 0)
            {
                return PSError.paramErr;
            }

            short err = PSError.noErr;
            try
            {
                bufferID = Memory.Allocate(size, false);

                bufferIDs.Add(bufferID, size);
            }
            catch (OutOfMemoryException)
            {
                // Free the buffer memory if the framework throws an OutOfMemoryException when adding to the bufferIDs list.
                if (bufferID != IntPtr.Zero)
                {
                    Memory.Free(bufferID);
                    bufferID = IntPtr.Zero;
                }

                err = PSError.memFullErr;
            }

            return err;
        }

        private void BufferFreeProc(IntPtr bufferID)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.BufferSuite, string.Format("Buffer: 0x{0}, Size: {1}", bufferID.ToHexString(), GetBufferSize(bufferID)));
#endif
            Memory.Free(bufferID);

            bufferIDs.Remove(bufferID);
        }

        private IntPtr BufferLockProc(IntPtr bufferID, byte moveHigh)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.BufferSuite, string.Format("Buffer: 0x{0}, moveHigh: {1}", bufferID.ToHexString(), moveHigh != 0));
#endif

            return bufferID;
        }

        private void BufferUnlockProc(IntPtr bufferID)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.BufferSuite, string.Format("Buffer: 0x{0}", bufferID.ToHexString()));
#endif
        }

        private int BufferSpaceProc()
        {
            // Assume that we have 1 GB of available space.
            int space = 1024 * 1024 * 1024;

            NativeStructs.MEMORYSTATUSEX buffer = new NativeStructs.MEMORYSTATUSEX
            {
                dwLength = (uint)Marshal.SizeOf(typeof(NativeStructs.MEMORYSTATUSEX))
            };

            if (SafeNativeMethods.GlobalMemoryStatusEx(ref buffer))
            {
                if (buffer.ullAvailVirtual < (ulong)space)
                {
                    space = (int)buffer.ullAvailVirtual;
                }
            }

            return space;
        }
    }
}
