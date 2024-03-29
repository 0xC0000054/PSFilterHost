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

using Microsoft.Win32.SafeHandles;
using PSFilterHostDll.Interop;
using PSFilterHostDll.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace PSFilterHostDll
{
    /// <summary>
    /// Enumerates through a directory using the native API.
    /// </summary>
#if !NET20 && !NET35
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
#endif
    internal sealed class FileEnumerator : IEnumerator<string>
    {
        private const int STATE_INIT = 0;
        private const int STATE_FIND_FILES = 1;
        private const int STATE_FINISH = 2;

        private int state;
        private bool disposed;
        private SafeFindHandle handle;
        private ShellLink shellLink;
        private Queue<SearchData> searchDirectories;
        private SearchData searchData;
        private string current;
        private bool needsPathDiscoveryDemand;
        private string shellLinkTarget;
        private HashSet<DirectoryIdentifier> visitedDirectories;

        private readonly NativeEnums.FindExInfoLevel infoLevel;
        private readonly NativeEnums.FindExAdditionalFlags additionalFlags;
        private readonly string fileExtension;
        private readonly SearchOption searchOption;
        private readonly bool dereferenceLinks;
        private readonly uint oldErrorMode;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileEnumerator"/> class.
        /// </summary>
        /// <param name="path">The directory to search.</param>
        /// <param name="fileExtension">The file extension to search for.</param>
        /// <param name="searchOption">
        /// One of the <see cref="SearchOption"/> values that specifies whether the search operation should include only the current directory or should include all subdirectories.
        /// </param>
        /// <param name="dereferenceLinks">If set to <c>true</c> search the target of shortcuts.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> in null.
        /// -or-
        /// <paramref name="fileExtension"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is a 0 length string, or contains only white-space, or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="searchOption"/> is not a valid <see cref="SearchOption"/> value.</exception>
        /// <exception cref="DirectoryNotFoundException">The directory specified by <paramref name="path"/> does not exist.</exception>
        /// <exception cref="IOException"><paramref name="path"/> is a file.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or combined exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters and file names must be less than 260 characters.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        public FileEnumerator(string path, string fileExtension, SearchOption searchOption, bool dereferenceLinks)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (fileExtension == null)
            {
                throw new ArgumentNullException(nameof(fileExtension));
            }
            if (searchOption < SearchOption.TopDirectoryOnly || searchOption > SearchOption.AllDirectories)
            {
                throw new ArgumentOutOfRangeException(nameof(searchOption));
            }

            string fullPath = Path.GetFullPath(path);
            string demandPath = GetPermissionPath(fullPath, false);
            new FileIOPermission(FileIOPermissionAccess.PathDiscovery, demandPath).Demand();
            needsPathDiscoveryDemand = false;

            searchData = new SearchData(fullPath);
            this.fileExtension = fileExtension;
            this.searchOption = searchOption;
            searchDirectories = new Queue<SearchData>();
            if (dereferenceLinks)
            {
                shellLink = new ShellLink();
                this.dereferenceLinks = true;
            }
            else
            {
                shellLink = null;
                this.dereferenceLinks = false;
            }
            shellLinkTarget = null;
            visitedDirectories = new HashSet<DirectoryIdentifier>();

            if (OS.IsWindows7OrLater)
            {
                // Suppress the querying of short filenames and use a larger buffer on Windows 7 and later.
                infoLevel = NativeEnums.FindExInfoLevel.Basic;
                additionalFlags = NativeEnums.FindExAdditionalFlags.LargeFetch;
            }
            else
            {
                infoLevel = NativeEnums.FindExInfoLevel.Standard;
                additionalFlags = NativeEnums.FindExAdditionalFlags.None;
            }
            oldErrorMode = SetErrorModeWrapper(NativeConstants.SEM_FAILCRITICALERRORS);
            state = -1;
            current = null;
            disposed = false;
            Init();
        }

        /// <summary>
        /// Gets the demand path for the FileIOPermission.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="includeSubDirectories">if set to <c>true</c> include the sub directories of <paramref name="path"/>.</param>
        /// <returns></returns>
        private static string GetPermissionPath(string path, bool includeSubDirectories)
        {
            char end = path[path.Length - 1];

            if (!includeSubDirectories)
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

        /// <summary>
        /// Performs a FileIOPermission demand for PathDiscovery on the specified directory.
        /// </summary>
        /// <param name="directory">The path.</param>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        private static void DoDemand(string directory)
        {
            string demandPath = GetPermissionPath(directory, false);
            new FileIOPermission(FileIOPermissionAccess.PathDiscovery, demandPath).Demand();
        }

        private static string GetWin32ErrorMessage(int error)
        {
            return new Win32Exception(error).Message;
        }

        private static int MakeHRFromWin32Error(int error)
        {
            return unchecked(((int)0x80070000) | (error & 0xffff));
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

        private bool FileMatchesFilter(string file)
        {
            return file.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Throws an exception for the specified Win32 error code.
        /// </summary>
        /// <param name="win32Error">The Win32 error code.</param>
        private void HandleError(int win32Error)
        {
            Dispose();
            switch (win32Error)
            {
                case NativeConstants.ERROR_PATH_NOT_FOUND:
                    throw new DirectoryNotFoundException(string.Format(CultureInfo.InvariantCulture, Resources.DirectoryNotFoundWithPath, searchData.path));
                case NativeConstants.ERROR_ACCESS_DENIED:
                    throw new UnauthorizedAccessException(string.Format(CultureInfo.InvariantCulture, Resources.AccessDeniedWithPath, searchData.path));
                case NativeConstants.ERROR_DIRECTORY:
                default:
                    throw new IOException(GetWin32ErrorMessage(win32Error), MakeHRFromWin32Error(win32Error));
            }
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        private void Init()
        {
            WIN32_FIND_DATAW findData = new WIN32_FIND_DATAW();
            string searchPath = Path.Combine(searchData.path, "*");
            handle = UnsafeNativeMethods.FindFirstFileExW(searchPath, infoLevel, findData, NativeEnums.FindExSearchOp.NameMatch, IntPtr.Zero, additionalFlags);

            if (handle.IsInvalid)
            {
                int error = Marshal.GetLastWin32Error();

                if (error != NativeConstants.ERROR_FILE_NOT_FOUND && error != NativeConstants.ERROR_NO_MORE_FILES)
                {
                    HandleError(error);
                }
                else
                {
                    // If no matching files are found exit when MoveNext is called.
                    // This may happen for an empty root directory.
                    state = STATE_FINISH;
                }
            }
            else
            {
                AddToVisitedDirectories(searchData.path);
                state = STATE_INIT;
                if (FirstFileIncluded(findData))
                {
                    current = CreateFilePath(findData);
                }
            }
        }

        /// <summary>
        /// Resolves the shortcut target.
        /// </summary>
        /// <param name="path">The shortcut target to resolve.</param>
        /// <param name="isDirectory">set to <c>true</c> if the target is a directory.</param>
        /// <returns>The target of the shortcut; or null if the target does not exist.</returns>
        private static string ResolveShortcutTarget(string path, out bool isDirectory)
        {
            isDirectory = false;

            if (!string.IsNullOrEmpty(path))
            {
                uint attributes = UnsafeNativeMethods.GetFileAttributesW(path);
                if (attributes != NativeConstants.INVALID_FILE_ATTRIBUTES)
                {
                    isDirectory = (attributes & NativeConstants.FILE_ATTRIBUTE_DIRECTORY) == NativeConstants.FILE_ATTRIBUTE_DIRECTORY;
                    return path;
                }
                else
                {
                    // Some plug-in installers that create shortcuts to their plug-ins will install
                    // both the 64-bit and X86 plug-in into a subdirectory of the 64-bit Program Files.
                    //
                    // These shortcuts will fail to resolve because the WoW64 subsystem redirects the shortcut
                    // from the 64-bit Program Files to a nonexistent folder in Program Files (x86).
                    if (IntPtr.Size == 4)
                    {
                        int error = Marshal.GetLastWin32Error();

                        if (error == NativeConstants.ERROR_FILE_NOT_FOUND || error == NativeConstants.ERROR_PATH_NOT_FOUND)
                        {
                            string fixedPath;
                            if (ShortcutHelper.FixWoW64ShortcutPath(path, out fixedPath))
                            {
                                attributes = UnsafeNativeMethods.GetFileAttributesW(fixedPath);
                                if (attributes != NativeConstants.INVALID_FILE_ATTRIBUTES)
                                {
                                    isDirectory = (attributes & NativeConstants.FILE_ATTRIBUTE_DIRECTORY) == NativeConstants.FILE_ATTRIBUTE_DIRECTORY;
                                    return fixedPath;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Adds the specified path to the directories that have been processed.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if <paramref name="path"/> is a new directory; otherwise, <c>false</c>.</returns>
        private unsafe bool AddToVisitedDirectories(string path)
        {
            bool result = false;

            // FILE_FLAG_BACKUP_SEMANTICS is required to open a directory handle.
            // See https://msdn.microsoft.com/en-us/library/windows/desktop/aa365258(v=vs.85).aspx
            // The directory handle is opened with write and delete permissions so that other processes
            // can change the files and subdirectories it contains.
            using (SafeFileHandle directoryHandle = UnsafeNativeMethods.CreateFileW(path, NativeConstants.GENERIC_READ,
                   NativeEnums.FileShare.Read | NativeEnums.FileShare.Write | NativeEnums.FileShare.Delete, IntPtr.Zero,
                   NativeEnums.CreationDisposition.OpenExisting, NativeConstants.FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero))
            {
                if (!directoryHandle.IsInvalid)
                {
                    // The BY_HANDLE_FILE_INFORMATION and FILE_ID_INFO structures contain fields that uniquely identify a file or directory.
                    // This information is used to track the directories that have been processed and prevent
                    // an infinite loop if a recursive NTFS junction point or directory shortcut is encountered.

                    if (OS.IsWindows8OrLater)
                    {
                        NativeStructs.FILE_ID_INFO fileInfo;
                        uint bufferSize = (uint)sizeof(NativeStructs.FILE_ID_INFO);

                        if (UnsafeNativeMethods.GetFileInformationByHandleEx(directoryHandle, NativeEnums.FILE_INFO_BY_HANDLE_CLASS.FileIdInfo, &fileInfo, bufferSize))
                        {
                            result = visitedDirectories.Add(new DirectoryIdentifier(fileInfo));
                        }
                    }
                    else
                    {
                        NativeStructs.BY_HANDLE_FILE_INFORMATION fileInfo;

                        if (UnsafeNativeMethods.GetFileInformationByHandle(directoryHandle, out fileInfo))
                        {
                            result = visitedDirectories.Add(new DirectoryIdentifier(fileInfo.dwVolumeSerialNumber, fileInfo.nFileIndexHigh, fileInfo.nFileIndexLow));
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Determines whether the specified path has not been searched.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>
        ///   <c>true</c> if the specified path has not been searched; otherwise, <c>false</c>.
        /// </returns>
        private bool IsNewDirectory(string path)
        {
            return AddToVisitedDirectories(path);
        }

        /// <summary>
        /// Adds the directory to search list if it has not already been searched.
        /// </summary>
        /// <param name="findData">The find data.</param>
        private void AddDirectoryToSearchList(WIN32_FIND_DATAW findData)
        {
            string path = Path.Combine(searchData.path, findData.cFileName);

            if (IsNewDirectory(path))
            {
                searchDirectories.Enqueue(new SearchData(path));
            }
        }

        private bool FirstFileIncluded(WIN32_FIND_DATAW findData)
        {
            if ((findData.dwFileAttributes & NativeConstants.FILE_ATTRIBUTE_DIRECTORY) == NativeConstants.FILE_ATTRIBUTE_DIRECTORY)
            {
                if (searchOption == SearchOption.AllDirectories && !findData.cFileName.Equals(".") && !findData.cFileName.Equals(".."))
                {
                    AddDirectoryToSearchList(findData);
                }
            }
            else
            {
                return IsFileIncluded(findData);
            }

            return false;
        }

        private bool IsFileIncluded(WIN32_FIND_DATAW findData)
        {
            if (findData.cFileName.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) && dereferenceLinks)
            {
                if (shellLink.Load(Path.Combine(searchData.path, findData.cFileName)))
                {
                    bool isDirectory;
                    string target = ResolveShortcutTarget(shellLink.Path, out isDirectory);

                    if (!string.IsNullOrEmpty(target))
                    {
                        if (isDirectory)
                        {
                            if (IsNewDirectory(target))
                            {
                                // If the shortcut target is a directory, add it to the search list.
                                searchDirectories.Enqueue(new SearchData(target));
                            }
                        }
                        else if (FileMatchesFilter(target))
                        {
                            shellLinkTarget = target;
                            return true;
                        }
                    }
                }
            }
            else if (FileMatchesFilter(findData.cFileName))
            {
                if (needsPathDiscoveryDemand)
                {
                    DoDemand(searchData.path);
                    needsPathDiscoveryDemand = false;
                }
                shellLinkTarget = null;

                return true;
            }

            return false;
        }

        private string CreateFilePath(WIN32_FIND_DATAW findData)
        {
            return shellLinkTarget ?? Path.Combine(searchData.path, findData.cFileName);
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        public string Current => current;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                if (handle != null)
                {
                    handle.Dispose();
                    handle = null;
                }

                if (shellLink != null)
                {
                    shellLink.Dispose();
                    shellLink = null;
                }
                current = null;
                state = -1;
                SetErrorModeWrapper(oldErrorMode);
            }
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        object IEnumerator.Current
        {
            get
            {
                if (current == null)
                {
                    throw new InvalidOperationException();
                }

                return current;
            }
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        public bool MoveNext()
        {
            WIN32_FIND_DATAW findData = new WIN32_FIND_DATAW();

            switch (state)
            {
                case STATE_INIT:
                    state = STATE_FIND_FILES;

                    if (current != null)
                    {
                        return true;
                    }
                    else
                    {
                        goto case STATE_FIND_FILES;
                    }
                case STATE_FIND_FILES:
                    do
                    {
                        if (handle == null)
                        {
                            searchData = searchDirectories.Dequeue();

                            string searchPath = Path.Combine(searchData.path, "*");
                            handle = UnsafeNativeMethods.FindFirstFileExW(searchPath, infoLevel, findData, NativeEnums.FindExSearchOp.NameMatch, IntPtr.Zero, additionalFlags);

                            if (handle.IsInvalid)
                            {
                                handle.Dispose();
                                handle = null;

                                if (searchDirectories.Count > 0)
                                {
                                    continue;
                                }
                                else
                                {
                                    state = STATE_FINISH;
                                    goto case STATE_FINISH;
                                }
                            }
                            needsPathDiscoveryDemand = true;
                            if (FirstFileIncluded(findData))
                            {
                                current = CreateFilePath(findData);
                                return true;
                            }
                        }

                        while (UnsafeNativeMethods.FindNextFileW(handle, findData))
                        {
                            if ((findData.dwFileAttributes & NativeConstants.FILE_ATTRIBUTE_DIRECTORY) == 0)
                            {
                                if (IsFileIncluded(findData))
                                {
                                    current = CreateFilePath(findData);
                                    return true;
                                }
                            }
                            else if (searchOption == SearchOption.AllDirectories && !findData.cFileName.Equals(".") && !findData.cFileName.Equals(".."))
                            {
                                AddDirectoryToSearchList(findData);
                            }
                        }

                        handle.Dispose();
                        handle = null;

                    } while (searchDirectories.Count > 0);

                    state = STATE_FINISH;
                    goto case STATE_FINISH;
                case STATE_FINISH:
                    Dispose();
                    break;
            }

            return false;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

        private struct DirectoryIdentifier : IEquatable<DirectoryIdentifier>
        {
            public readonly ulong volumeSerialNumber;
            public readonly ulong fileIndexHigh;
            public readonly ulong fileIndexLow;

            public DirectoryIdentifier(uint volumeSerialNumber, uint fileIndexHigh, uint fileIndexLow)
            {
                this.volumeSerialNumber = volumeSerialNumber;
                this.fileIndexHigh = fileIndexHigh;
                this.fileIndexLow = fileIndexLow;
            }

            public unsafe DirectoryIdentifier(NativeStructs.FILE_ID_INFO info)
            {
                volumeSerialNumber = info.VolumeSerialNumber;
                // The FileId field stores the low index first.
                fileIndexLow = *(ulong*)info.FileID;
                fileIndexHigh = *(ulong*)(info.FileID + 8);
            }

            public override bool Equals(object obj)
            {
                if (obj is DirectoryIdentifier identifier)
                {
                    return Equals(identifier);
                }

                return false;
            }

            public bool Equals(DirectoryIdentifier other)
            {
                return volumeSerialNumber == other.volumeSerialNumber &&
                       fileIndexHigh == other.fileIndexHigh &&
                       fileIndexLow == other.fileIndexLow;
            }

            public override int GetHashCode()
            {
                int hashCode = -1532359432;

                unchecked
                {
                    hashCode = (hashCode * -1521134295) + volumeSerialNumber.GetHashCode();
                    hashCode = (hashCode * -1521134295) + fileIndexHigh.GetHashCode();
                    hashCode = (hashCode * -1521134295) + fileIndexLow.GetHashCode();
                }

                return hashCode;
            }

            public static bool operator ==(DirectoryIdentifier file1, DirectoryIdentifier file2)
            {
                return file1.Equals(file2);
            }

            public static bool operator !=(DirectoryIdentifier file1, DirectoryIdentifier file2)
            {
                return !file1.Equals(file2);
            }
        }

        private sealed class SearchData
        {
            public readonly string path;

            /// <summary>
            /// Initializes a new instance of the <see cref="SearchData"/> class.
            /// </summary>
            /// <param name="path">The path.</param>
            /// <exception cref="System.ArgumentNullException"><paramref name="path"/> is null.</exception>
            public SearchData(string path)
            {
                if (path == null)
                {
                    throw new ArgumentNullException(nameof(path));
                }

                this.path = path;
            }
        }
    }
}
