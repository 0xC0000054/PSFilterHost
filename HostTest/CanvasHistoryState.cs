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
using System.Collections.Generic;
using System.Drawing;
using HostTest.Tools;
using System.Windows.Media.Imaging;

namespace HostTest
{
    /// <summary>
    /// The class that encapsulates the history state of the Canvas control.
    /// </summary>
    [Serializable]
    internal sealed class CanvasHistoryState : IDisposable
    {
        private Bitmap image;

        /// <summary>
        /// Initializes a new instance of the <see cref="CanvasHistoryState"/> class, this is only called from <see cref="Canvas.ToCanvasHistoryState()"/>.
        /// </summary>
        /// <param name="image">The current canvas image.</param>
        public CanvasHistoryState(Bitmap image)
        {
            this.image = (Bitmap)image.Clone();

            this.disposed = false;
        }



        public Bitmap Image
        {
            get
            {
                return image;
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
                this.disposed = true;

                if (disposing && image != null)
                {
                    image.Dispose();
                    image = null;
                }
            }
        }

    }
}
