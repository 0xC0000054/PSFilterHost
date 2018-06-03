/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2018 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using Microsoft.Win32.SafeHandles;

namespace PSFilterHostDll.Interop
{
    internal sealed class SafeTransformHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeTransformHandle() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return UnsafeNativeMethods.Mscms.DeleteColorTransform(handle);
        }
    }
}
