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

namespace PSFilterHostDll
{
#if GDIPLUS
    /// <summary>
    /// Represents the current state of the host application.
    /// </summary>
    /// <seealso cref="PluginData.SupportsHostState(System.Drawing.Bitmap, HostState)"/>
    /// <threadsafety static="true" instance="false"/>
#else
    /// <summary>
    /// Represents the current state of the host application.
    /// </summary>
    /// <seealso cref="PluginData.SupportsHostState(System.Windows.Media.Imaging.BitmapSource, HostState)"/>
    /// <threadsafety static="true" instance="false"/>
#endif
    public sealed class HostState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HostState"/> class.
        /// </summary>
        public HostState()
        {
            HasMultipleLayers = false;
            HasSelection = false;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the current document has multiple layers.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the current document has multiple layers; otherwise, <c>false</c>.
        /// </value>
        public bool HasMultipleLayers
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the host has an active selection.
        /// </summary>
        /// <value>
        ///   <c>true</c> if host has an active selection; otherwise, <c>false</c>.
        /// </value>
        public bool HasSelection
        {
            get;
            set;
        }
    }
}
