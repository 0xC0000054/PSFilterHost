﻿/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using Microsoft.Win32.SafeHandles;

namespace PSFilterHostDll.PSApi
{
    internal sealed class SafeProfileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeProfileHandle() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return UnsafeNativeMethods.Mscms.CloseColorProfile(this.handle);
        }
    }
}
