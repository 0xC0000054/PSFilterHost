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

using System;
using System.Windows.Forms;

namespace HostTest.Tools
{
    internal class CursorChangedEventArgs : EventArgs, IDisposable
    {
        private Cursor cursor;
        public CursorChangedEventArgs(Cursor newCursor)
        {
            cursor = newCursor;
            disposed = false;
        }

        public Cursor NewCursor
        {
            get
            {
                return cursor;
            }
        }

        private bool disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (cursor != null)
                    {
                        cursor.Dispose();
                        cursor = null;
                    }
                    disposed = true;
                }
            }
        }
    }
}
