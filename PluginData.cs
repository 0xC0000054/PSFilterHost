/////////////////////////////////////////////////////////////////////////////////
//
// Adobe(R) Photoshop(R) filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2013 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;
using PSFilterLoad.PSApi;

#if GDIPLUS
using System.Drawing.Imaging;
#else
using System.Windows.Media;
#endif

namespace PSFilterHostDll
{
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate int pluginEntryPoint(short selector, IntPtr pluginParamBlock, ref IntPtr pluginData, ref short result);
    /// <summary>
    /// The class that encapsulates an Adobe® Photoshop® filter plugin
    /// </summary>
    public sealed class PluginData

    {
        private string fileName;
        private string entryPoint;
        private string category;
        private string title;
        private FilterCaseInfo[] filterInfo;
        private PluginAETE aete;
        internal string enableInfo;
        internal ushort supportedModes;
        internal string[] moduleEntryPoints; 

        /// <summary>
        /// The structure containing the dll entrypoint
        /// </summary>
        internal PIEntrypoint module;

        /// <summary>
        /// Gets the filename of the  of the filter.
        /// </summary>
        /// <value>
        /// The filename of the filter.
        /// </value>
        public string FileName
        {
            get { return fileName; }
            internal set { fileName = value; }
        }
        /// <summary>
        /// Gets the entry point.
        /// </summary>
        public string EntryPoint
        {
            get { return entryPoint; }
            internal set { entryPoint = value; }
        }
        /// <summary>
        /// Gets the category.
        /// </summary>
        public string Category
        {
            get { return category; }
            internal set { category = value; }
        }

        /// <summary>
        /// Gets the title.
        /// </summary>
        public string Title
        {
            get { return title; }
            internal set { title = value; }
        }
 
        internal FilterCaseInfo[] FilterInfo
        {
            get { return filterInfo; }
            set { filterInfo = value; }
        }

        internal PluginAETE Aete
        {
            get { return aete; }
            set { aete = value; }
        }

#if !GDIPLUS
        /// <summary>
        ///Checks if the filter supports processing images in the current <see cref="System.Windows.Media.PixelFormat"/>.
        /// </summary>
        /// <param name="mode">The <see cref="System.Windows.Media.PixelFormat"/> of the image to process.</param>
        /// <returns><c>true</c> if the filter can process the image; otherwise <c>false</c>.</returns>
        public bool SupportsImageMode(PixelFormat mode)
        {
            if (mode == PixelFormats.BlackWhite || mode == PixelFormats.Gray2 || mode == PixelFormats.Gray4 || mode == PixelFormats.Gray8)
            {
                return ((this.supportedModes & PSConstants.flagSupportsGrayScale) == PSConstants.flagSupportsGrayScale);
            }
            else if (mode == PixelFormats.Gray16 || mode == PixelFormats.Gray32Float)
            {
                return Supports16BitMode(true);
            }
            else if (mode == PixelFormats.Rgb48 || mode == PixelFormats.Rgba64 || mode == PixelFormats.Rgba128Float || mode == PixelFormats.Rgb128Float ||
                mode == PixelFormats.Prgba128Float || mode == PixelFormats.Prgba64)
            {
                return Supports16BitMode(false);
            }
            else
            {
                return ((this.supportedModes & PSConstants.flagSupportsRGBColor) == PSConstants.flagSupportsRGBColor);
            }
        } 


        /// <summary>
        /// Checks if the filter supports processing 16-bit images.
        /// </summary>
        /// <returns><c>true</c> if the filter supports 16-bit mode; otherwise <c>false</c>.</returns>
        private bool Supports16BitMode(bool grayScale)
        {
            if (!string.IsNullOrEmpty(this.enableInfo))
            {
                EnableInfoParser parser = new EnableInfoParser(grayScale);
                return parser.Parse(this.enableInfo);
            }

            if (grayScale)
            {
                return ((this.supportedModes & PSConstants.flagSupportsGray16) == PSConstants.flagSupportsGray16);
            }

            return ((this.supportedModes & PSConstants.flagSupportsRGB48) == PSConstants.flagSupportsRGB48);
        }
#endif
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginData"/> class.
        /// </summary>
        internal PluginData(string fileName)
        {
            this.fileName = fileName;
            this.category = string.Empty;
            this.entryPoint = string.Empty;
            this.title = string.Empty;
            this.filterInfo = null;
            this.aete = null;
            this.enableInfo = string.Empty;
            this.supportedModes = 0;
            this.module = new PIEntrypoint();
            this.moduleEntryPoints = null;
        }

        internal bool IsValid()
        {
            return (!string.IsNullOrEmpty(category) && !string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(entryPoint));
        }
    }

    internal struct PIEntrypoint
    {
        /// <summary>
        /// The pointer to the dll module handle
        /// </summary>
        public SafeLibraryHandle dll;
        /// <summary>
        /// The entrypoint for the FilterParmBlock and AboutRecord
        /// </summary>
        public pluginEntryPoint entryPoint;
    }

}
