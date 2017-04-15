/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Drawing.Drawing2D;

namespace HostTest.Tools
{
    internal sealed class SelectionPathChangedEventArgs : EventArgs, IDisposable
    {
        private GraphicsPath selectedPath;
        private bool disposed;

        public SelectionPathChangedEventArgs(GraphicsPath path)
        {
            this.disposed = false;
            if (path != null)
            {
                this.selectedPath = (GraphicsPath)path.Clone();
            }
            else
            {
                this.selectedPath = null;
            }
        }

        public GraphicsPath SelectedPath
        {
            get
            {
                return this.selectedPath;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {                    
                this.disposed = true;

                if (disposing)
                {
                    if (this.selectedPath != null)
                    {
                        this.selectedPath.Dispose();
                        this.selectedPath = null;
                    }
                }
            }
        }
    }
}
