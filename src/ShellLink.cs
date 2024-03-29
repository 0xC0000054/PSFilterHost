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

using PSFilterHostDll.Interop;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PSFilterHostDll
{
    /// <summary>
    /// Encapsulates a ShellLink shortcut file
    /// </summary>
    internal sealed class ShellLink : IDisposable
    {
        private NativeInterfaces.IShellLinkW shellLink;
        private bool disposed;

        [ComImport(), Guid(NativeConstants.CLSID_ShellLink)]
        private class ShellLinkCoClass
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShellLink"/> class.
        /// </summary>
        public ShellLink()
        {
            shellLink = (NativeInterfaces.IShellLinkW)new ShellLinkCoClass();
            disposed = false;
        }

        /// <summary>
        /// Loads a shortcut from a file.
        /// </summary>
        /// <param name="linkPath">The shortcut to load.</param>
        public bool Load(string linkPath)
        {
            return ((NativeInterfaces.IPersistFile)shellLink).Load(linkPath, NativeConstants.STGM_READ) == NativeConstants.S_OK;
        }

        /// <summary>
        /// Gets the target path of the shortcut.
        /// </summary>
        public string Path
        {
            get
            {
                StringBuilder sb = new StringBuilder(260);

                if (shellLink.GetPath(sb, sb.Capacity, IntPtr.Zero, 0U) != NativeConstants.S_OK)
                {
                    return string.Empty;
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="ShellLink"/> is reclaimed by garbage collection.
        /// </summary>
        ~ShellLink()
        {
            Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;

                if (disposing)
                {
                }

                if (shellLink != null)
                {
                    Marshal.ReleaseComObject(shellLink);
                    shellLink = null;
                }
            }
        }
    }
}
