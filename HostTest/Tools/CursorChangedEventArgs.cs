/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2014 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Windows.Forms;

namespace HostTest.Tools
{
    class CursorChangedEventArgs : EventArgs, IDisposable
    {
        private Cursor cursor;
        public CursorChangedEventArgs(Cursor newCursor)
        {
            this.cursor = newCursor;
            this.disposed = false;
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
                    if (this.cursor != null)
                    {
                        this.cursor.Dispose();
                        this.cursor = null;
                    }
                    disposed = true;
                }
            }
        }

    }
}
