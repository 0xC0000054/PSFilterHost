/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2019 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using Microsoft.Win32.SafeHandles;

#if !NET_40_OR_GREATER
using System.Security.Permissions;
#endif
/* The following code is quoted from Mike Stall's blog
 * Type-safe Managed wrappers for kernel32!GetProcAddress
 * http://blogs.msdn.com/b/jmstall/archive/2007/01/06/typesafe-getprocaddress.aspx
 */

namespace PSFilterHostDll.Interop
{
#if NET_40_OR_GREATER
	[System.Security.SecurityCritical()]
#else
	[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
#endif
	internal sealed class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		/// <summary>
		/// Create safe library handle
		/// </summary>
		private SafeLibraryHandle() : base(true) { }

		/// <summary>
		/// Release handle
		/// </summary>
		protected override bool ReleaseHandle()
		{
			return UnsafeNativeMethods.FreeLibrary(handle);
		}
	}
}


