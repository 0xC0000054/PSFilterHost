/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2016 Nicholas Hayes
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
            return SafeNativeMethods.DeleteDC(this.handle);
        }
    }
}
