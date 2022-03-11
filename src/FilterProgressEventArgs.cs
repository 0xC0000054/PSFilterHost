/////////////////////////////////////////////////////////////////////////////////
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

using System;

namespace PSFilterHostDll
{
    /// <summary>
    /// Provides data for the <see cref="PSFilterHost.UpdateProgress"/> event.
    /// </summary>
    public sealed class FilterProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the progress of the render.
        /// </summary>
        public int Progress { get; }

        internal FilterProgressEventArgs(int progressDone)
        {
            Progress = progressDone;
        }
    }
}
