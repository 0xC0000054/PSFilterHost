/////////////////////////////////////////////////////////////////////////////////
//
// Adobe(R) Photoshop(R) filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2013 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace PSFilterHostDll
{
    /// <summary>
    /// Enumerates through a directory using the native API.
    /// </summary>
    internal static class FileEnumerator
    {

#if NET_40_OR_GREATER
        [SecurityCritical()]
#else
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
#endif
        private sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private SafeFindHandle() : base(true) { }

            protected override bool ReleaseHandle()
            {
                return UnsafeNativeMethods.FindClose(handle);
            }
        }

        [SuppressUnmanagedCodeSecurity]
        private static class UnsafeNativeMethods
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static extern SafeFindHandle FindFirstFileW(string fileName, out WIN32_FIND_DATAW data);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FindNextFileW(SafeFindHandle hndFindFile, out WIN32_FIND_DATAW lpFindFileData);

            [DllImport("kernel32.dll", ExactSpelling = true), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FindClose(IntPtr handle);
        }
 
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WIN32_FIND_DATAW 
        {
            public uint dwFileAttributes;
            public FILETIME ftCreationTime;
            public FILETIME ftLastAccessTime;
            public FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=14)]
            public string cAlternateFileName;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        internal static IEnumerable<string> EnumerateFiles(string directory, string fileExtension, bool searchSubDirectories)
        {             
            // Adapted from: http://weblogs.asp.net/podwysocki/archive/2008/10/16/functional-net-fighting-friction-in-the-bcl-with-directory-getfiles.aspx

            var findData = new WIN32_FIND_DATAW();

            using (var findHandle = UnsafeNativeMethods.FindFirstFileW(directory + @"\*", out findData))
            {
                if (!findHandle.IsInvalid)
                {
                    do
                    {
                        if ((findData.dwFileAttributes & 16U) != 0U)
                        {
                            if (searchSubDirectories)
                            {
                                if (findData.cFileName != "." && findData.cFileName != "..")
                                {
                                    var subdirectory = Path.Combine(directory, findData.cFileName);

                                    new FileIOPermission(FileIOPermissionAccess.PathDiscovery, subdirectory).Demand();

                                    foreach (var file in EnumerateFiles(subdirectory, fileExtension, searchSubDirectories))
                                        yield return file;
                                }
                            }
                        }
                        else
                        {
                            if (string.Compare(Path.GetExtension(findData.cFileName), fileExtension, StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                var filePath = Path.Combine(directory, findData.cFileName);
                                yield return filePath;
                            }
                        } 

                    } while (UnsafeNativeMethods.FindNextFileW(findHandle, out findData));
                }
            }  
            
        }

    }
}
