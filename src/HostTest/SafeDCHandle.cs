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

using Microsoft.Win32.SafeHandles;

namespace HostTest
{
    /// <summary>
    /// Represents a handle to a device context
    /// </summary>
    internal sealed class SafeDCHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeDCHandle() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return SafeNativeMethods.DeleteDC(handle);
        }
    }
}
