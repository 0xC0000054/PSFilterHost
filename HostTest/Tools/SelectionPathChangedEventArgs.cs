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
using System.Drawing.Drawing2D;

namespace HostTest.Tools
{
    internal sealed class SelectionPathChangedEventArgs : EventArgs, IDisposable
    {
        private GraphicsPath selectedPath;

        public SelectionPathChangedEventArgs(GraphicsPath path)
        {
            if (path != null)
            {
                selectedPath = (GraphicsPath)path.Clone();
            }
        }

        public GraphicsPath SelectedPath
        {
            get
            {
                return selectedPath;
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
                    if (this.selectedPath != null)
                    {
                        this.selectedPath.Dispose();
                        this.selectedPath = null;
                    }
                    disposed = true;
                }
            }
        }
    }
}
