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

using PSFilterHostDll.BGRASurface;

namespace PSFilterHostDll.PSApi
{
    /// <summary>
    /// Provides access to the images that a filter reads and writes.
    /// </summary>
    internal interface IFilterImageProvider
    {
        /// <summary>
        /// Gets the filter source image.
        /// </summary>
        /// <value>
        /// The filter source image.
        /// </value>
        SurfaceBase Source
        {
            get;
        }

        /// <summary>
        /// Gets the filter destination image.
        /// </summary>
        /// <value>
        /// The filter destination image.
        /// </value>
        SurfaceBase Destination
        {
            get;
        }

        /// <summary>
        /// Gets the filter mask image.
        /// </summary>
        /// <value>
        /// The filter mask image.
        /// </value>
        SurfaceGray8 Mask
        {
            get;
        }
    }
}
