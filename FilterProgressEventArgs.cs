/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterHostDll
{
    /// <summary>
    /// Provides data for the <see cref="PSFilterHost.UpdateProgress"/> event.
    /// </summary>
    public sealed class FilterProgressEventArgs : EventArgs
    {
        private int progress;

        /// <summary>
        /// Gets the progress of the render.
        /// </summary>
        public int Progress
        {
            get
            {
                return progress;
            }
        }

        internal FilterProgressEventArgs(int progressDone)
        {
            this.progress = progressDone;
        }

    }
}
