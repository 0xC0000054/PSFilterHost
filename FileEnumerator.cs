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

        private enum FindExInfoLevel : int
        {
            Standard = 0,
            Basic,
            MaxInfoLevel
        }

        private enum FindExSearchOps : int
        {
            NameMatch = 0,
            LimitToDirectories,
            LimitToDevices
        }

        [Flags]
        private enum FindExAdditionalFlags : uint
        {
            None = 0U,
            CaseSensitive = 1U,
            LargeFetch = 2U
        }

        [SuppressUnmanagedCodeSecurity]
        private static class UnsafeNativeMethods
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static extern SafeFindHandle FindFirstFileExW([In(), MarshalAs(UnmanagedType.LPWStr)] string fileName, [In()] FindExInfoLevel infoLevel, out WIN32_FIND_DATAW data, [In()] FindExSearchOps searchOp, [In()] IntPtr searchFilter, [In()] FindExAdditionalFlags flags);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FindNextFileW(SafeFindHandle hndFindFile, out WIN32_FIND_DATAW lpFindFileData);

            [DllImport("kernel32.dll", ExactSpelling = true), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FindClose(IntPtr handle);

            [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
            internal static extern uint GetFileAttributesW([In(), MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

            [DllImport("kernel32.dll", ExactSpelling = true)]
            internal static extern uint SetErrorMode(uint uMode);

            [DllImport("kernel32.dll", ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetThreadErrorMode(uint dwNewMode, out uint lpOldMode);
        }

        private static class NativeConstants
        {
            internal const uint FILE_ATTRIBUTE_DIRECTORY = 16U;
            internal const uint FILE_ATTRIBUTE_REPARSE_POINT = 1024U;
            internal const uint INVALID_FILE_ATTRIBUTES = 0xFFFFFFFF;
            internal const int ERROR_FILE_NOT_FOUND = 2;
            internal const int ERROR_PATH_NOT_FOUND = 3;
            internal const int ERROR_ACCESS_DENIED = 5;
            internal const int ERROR_DIRECTORY = 267;
            internal const uint SEM_FAILCRITICALERRORS = 1U;
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

        private static string GetPermissionPath(string path, bool searchSubDirectories)
        {
            char end = path[path.Length - 1];

            if (!searchSubDirectories)
            {
                if (end == Path.DirectorySeparatorChar || end == Path.AltDirectorySeparatorChar)
                {
                    return path + ".";
                }

                return path + Path.DirectorySeparatorChar + "."; // Demand permission for the current directory only
            }

            if (end == Path.DirectorySeparatorChar || end == Path.AltDirectorySeparatorChar)
            {
                return path;
            }

            return path + Path.DirectorySeparatorChar; // Demand permission for the current directory and all subdirectories.
        }

        private static string GetWin32ErrorMessage(int error)
        {
            return new System.ComponentModel.Win32Exception(error).Message;
        }

        private static int MakeHRFromWin32Error(int error)
        {
            return unchecked(((int)0x80070000) | (error & 0xffff));
        }

        private static void CheckDirectoryAccess(string fullPath)
        {
            uint attributes = UnsafeNativeMethods.GetFileAttributesW(fullPath);

            if (attributes == NativeConstants.INVALID_FILE_ATTRIBUTES)
            {
                int error = Marshal.GetLastWin32Error();

                switch (error)
                {
                    case NativeConstants.ERROR_FILE_NOT_FOUND:
                    case NativeConstants.ERROR_PATH_NOT_FOUND:
                        throw new DirectoryNotFoundException();
                    case NativeConstants.ERROR_ACCESS_DENIED:
                        throw new UnauthorizedAccessException(PSFilterHostDll.Properties.Resources.PathAccessDenied);
                    default:
                        throw new IOException(GetWin32ErrorMessage(error), MakeHRFromWin32Error(error));
                }
            }
            else if ((attributes & NativeConstants.FILE_ATTRIBUTE_DIRECTORY) == 0)
            {
                // The path is a file name instead of a directory.
                throw new IOException(GetWin32ErrorMessage(NativeConstants.ERROR_DIRECTORY), MakeHRFromWin32Error(NativeConstants.ERROR_DIRECTORY));
            }
        }

        private static uint SetErrorModeWrapper(uint newMode)
        {
            uint oldMode;

            if (OS.IsWindows7OrLater)
            {
                UnsafeNativeMethods.SetThreadErrorMode(newMode, out oldMode);
            }
            else
            {
                oldMode = UnsafeNativeMethods.SetErrorMode(newMode);
            }

            return oldMode;
        }

        internal static IEnumerable<string> EnumerateFiles(string directory, string[] fileExtensions, bool searchSubDirectories)
        {
            if (directory == null)
            {
                throw new ArgumentNullException("directory");
            }

            if (fileExtensions == null)
            {
                throw new ArgumentNullException("fileExtensions");
            }

            if (fileExtensions.Length == 0)
            {
                throw new ArgumentException("fileExtensions array is empty.");
            }

            // Adapted from: http://weblogs.asp.net/podwysocki/archive/2008/10/16/functional-net-fighting-friction-in-the-bcl-with-directory-getfiles.aspx
            string fullPath = Path.GetFullPath(directory);

            CheckDirectoryAccess(fullPath);

            new FileIOPermission(FileIOPermissionAccess.PathDiscovery, GetPermissionPath(fullPath, searchSubDirectories)).Demand();

            var findData = new WIN32_FIND_DATAW();

            FindExInfoLevel infoLevel = FindExInfoLevel.Standard;
            FindExAdditionalFlags flags = FindExAdditionalFlags.None;

            if (OS.IsWindows7OrLater)
            {
                // Suppress the querying of short filenames and use a larger buffer on Windows 7 and later.
                infoLevel = FindExInfoLevel.Basic;
                flags = FindExAdditionalFlags.LargeFetch;
            }
            Queue<string> directories = new Queue<string>();
            directories.Enqueue(fullPath);

            uint oldErrorMode = SetErrorModeWrapper(NativeConstants.SEM_FAILCRITICALERRORS);
            try
            {
                do
                {
                    string path = directories.Dequeue();

                    using (var findHandle = UnsafeNativeMethods.FindFirstFileExW(Path.Combine(path, "*"), infoLevel, out findData, FindExSearchOps.NameMatch, IntPtr.Zero, flags))
                    {
                        if (!findHandle.IsInvalid)
                        {
                            do
                            {
                                if ((findData.dwFileAttributes & NativeConstants.FILE_ATTRIBUTE_DIRECTORY) == NativeConstants.FILE_ATTRIBUTE_DIRECTORY)
                                {
                                    if (searchSubDirectories)
                                    {
                                        if (findData.cFileName != "." && findData.cFileName != ".." && (findData.dwFileAttributes & NativeConstants.FILE_ATTRIBUTE_REPARSE_POINT) == 0)
                                        {
                                            var subdirectory = Path.Combine(path, findData.cFileName);

                                            directories.Enqueue(subdirectory);
                                        }
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < fileExtensions.Length; i++)
                                    {
                                        if (findData.cFileName.EndsWith(fileExtensions[i], StringComparison.OrdinalIgnoreCase))
                                        {
                                            yield return Path.Combine(path, findData.cFileName);
                                        }
                                    }
                                }

                            } while (UnsafeNativeMethods.FindNextFileW(findHandle, out findData));
                        }
                    }
                } while (directories.Count > 0);
            }
            finally
            {
                SetErrorModeWrapper(oldErrorMode);
            }
        }

    }
}
