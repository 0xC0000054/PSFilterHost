/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2015 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.PSApi.PICA
{
    internal static class PICABufferSuite
    {
        private static PSBufferSuiteNew bufferSuiteNew = new PSBufferSuiteNew(PSBufferNew);
        private static PSBufferSuiteDispose bufferSuiteDispose = new PSBufferSuiteDispose(PSBufferDispose);
        private static PSBufferSuiteGetSize bufferSuiteGetSize = new PSBufferSuiteGetSize(PSBufferGetSize);
        private static PSBufferSuiteGetSpace bufferSuiteGetSpace = new PSBufferSuiteGetSpace(PSBufferGetSpace);

        private static IntPtr PSBufferNew(ref uint requestedSize, uint minimumSize)
        {

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Memory.Allocate(requestedSize, false);

                return ptr;
            }
            catch (NullReferenceException)
            {
            }
            catch (OutOfMemoryException)
            {
            }


            try
            {
                ptr = Memory.Allocate(minimumSize, false);

                return ptr;
            }
            catch (OutOfMemoryException)
            {
            }


            return IntPtr.Zero;
        }

        private static void PSBufferDispose(ref IntPtr buffer)
        {
            Memory.Free(buffer);
            buffer = IntPtr.Zero;
        }

        private static uint PSBufferGetSize(IntPtr buffer)
        {
            return (uint)Memory.Size(buffer);
        }

        private static uint PSBufferGetSpace()
        {
            // Assume that we have 1 GB of available space.
            uint space = 1024 * 1024 * 1024;

            NativeStructs.MEMORYSTATUSEX buffer = new NativeStructs.MEMORYSTATUSEX();
            buffer.dwLength = (uint)Marshal.SizeOf(typeof(NativeStructs.MEMORYSTATUSEX));

            if (SafeNativeMethods.GlobalMemoryStatusEx(ref buffer))
            {
                if (buffer.ullAvailVirtual < uint.MaxValue)
                {
                    space = (uint)buffer.ullAvailVirtual;
                }
            }

            return space;
        }

        public static PSBufferSuite1 CreateBufferSuite1()
        {
            PSBufferSuite1 suite = new PSBufferSuite1();
            suite.New = Marshal.GetFunctionPointerForDelegate(bufferSuiteNew);
            suite.Dispose = Marshal.GetFunctionPointerForDelegate(bufferSuiteDispose);
            suite.GetSize = Marshal.GetFunctionPointerForDelegate(bufferSuiteGetSize);
            suite.GetSpace = Marshal.GetFunctionPointerForDelegate(bufferSuiteGetSpace);

            return suite;
        }
    }
}
