/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2015 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using PSFilterLoad.PSApi;

#if !GDIPLUS
using System.Windows.Media;
#endif

namespace PSFilterHostDll
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate void PluginEntryPoint(FilterSelector selector, IntPtr pluginParamBlock, ref IntPtr pluginData, ref short result);
    /// <summary>
    /// The class that encapsulates a Photoshop-compatible filter plug-in.
    /// </summary>
    /// <threadsafety static="true" instance="false" />
    [Serializable]
    public sealed class PluginData : IEquatable<PluginData>
    {
        private string fileName;
        private string entryPoint;
        private string category;
        private string title;
        private FilterCaseInfo[] filterInfo;
        private PluginAETE aete;
        internal string enableInfo;
        internal ushort? supportedModes;
        internal string[] moduleEntryPoints;
        [OptionalField(VersionAdded = 2)]
        private bool hasAboutBox;

        /// <summary>
        /// The structure containing the dll entrypoint
        /// </summary>
        [NonSerialized]
        internal PIEntrypoint module;

        /// <summary>
        /// Gets the filename of the filter.
        /// </summary>
        public string FileName
        {
            get 
            {
                return this.fileName; 
            }
        }
        /// <summary>
        /// Gets the entry point of the filter.
        /// </summary>
        public string EntryPoint
        {
            get 
            {
                return this.entryPoint; 
            }
            internal set 
            {
                this.entryPoint = value;
            }
        }
        /// <summary>
        /// Gets the category of the filter.
        /// </summary>
        public string Category
        {
            get 
            {
                return this.category;
            }
            internal set 
            {
                this.category = value;
            }
        }

        /// <summary>
        /// Gets the title of the filter.
        /// </summary>
        public string Title
        {
            get 
            {
                return this.title;
            }
            internal set 
            {
                this.title = value; 
            }
        }
 
        internal FilterCaseInfo[] FilterInfo
        {
            get 
            {
                return this.filterInfo; 
            }
            set 
            {
                this.filterInfo = value;
            }
        }

        internal PluginAETE Aete
        {
            get 
            {
                return this.aete; 
            }
            set 
            {
                this.aete = value; 
            }
        }
        
        /// <summary>
        /// Gets a value indicating whether this filter has an about box.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this filter has an about box; otherwise, <c>false</c>.
        /// </value>
        public bool HasAboutBox
        {
            get 
            { 
                return this.hasAboutBox; 
            }
            internal set 
            {
                this.hasAboutBox = value; 
            }
        }

#if !GDIPLUS
        /// <summary>
        /// Checks if the filter supports processing images in the current <see cref="System.Windows.Media.PixelFormat"/>.
        /// </summary>
        /// <param name="mode">The <see cref="System.Windows.Media.PixelFormat"/> of the image to process.</param>
        /// <returns><c>true</c> if the filter can process the image; otherwise <c>false</c>.</returns>
        public bool SupportsImageMode(PixelFormat mode)
        {
            if (!this.supportedModes.HasValue)
            {
                DetectSupportedModes();
            }

            if (mode == PixelFormats.BlackWhite || mode == PixelFormats.Gray2 || mode == PixelFormats.Gray4 || mode == PixelFormats.Gray8)
            {
                return ((this.supportedModes.Value & PSConstants.flagSupportsGrayScale) == PSConstants.flagSupportsGrayScale);
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
                return ((this.supportedModes.Value & PSConstants.flagSupportsRGBColor) == PSConstants.flagSupportsRGBColor);
            }
        }

        private void DetectSupportedModes()
        {
            if (!string.IsNullOrEmpty(this.enableInfo))
            {
                if (this.enableInfo == "true")
                {
                    this.supportedModes = ushort.MaxValue; // All modes are supported
                }
                else
                {
                    EnableInfoParser parser = new EnableInfoParser();
                    this.supportedModes = parser.GetSupportedModes(this.enableInfo);
                }
            }
            else
            {
                this.supportedModes = 0;
            }
        }

        /// <summary>
        /// Checks if the filter supports processing 16-bit images.
        /// </summary>
        /// <param name="grayScale">if set to <c>true</c> check if processing 16-bit gray scale is supported; otherwise check if 48-bit RGB is supported.</param>
        /// <returns>
        ///   <c>true</c> if the filter supports 16-bit mode; otherwise <c>false</c>.
        /// </returns>
        private bool Supports16BitMode(bool grayScale)
        {
            if (!string.IsNullOrEmpty(this.enableInfo))
            {
                EnableInfoParser parser = new EnableInfoParser(grayScale);
                return parser.Parse(this.enableInfo);
            }

            if (grayScale)
            {
                return ((this.supportedModes.Value & PSConstants.flagSupportsGray16) == PSConstants.flagSupportsGray16);
            }

            return ((this.supportedModes.Value & PSConstants.flagSupportsRGB48) == PSConstants.flagSupportsRGB48);
        }
#endif
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginData"/> class.
        /// </summary>
        /// <param name="fileName">The file name of the filter.</param>
        internal PluginData(string fileName)
        {
            this.fileName = fileName;
            this.category = string.Empty;
            this.entryPoint = string.Empty;
            this.title = string.Empty;
            this.filterInfo = null;
            this.aete = null;
            this.enableInfo = string.Empty;
            this.supportedModes = null;
            this.module = new PIEntrypoint();
            this.moduleEntryPoints = null;
            this.hasAboutBox = true;
        }

        [OnDeserializing]
        private void SetOptionalFieldDefaults(StreamingContext context)
        {
            this.hasAboutBox = true;
        }

        /// <summary>
        /// Determines whether this instance is valid.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
        /// </returns>
        internal bool IsValid()
        {
            return (!string.IsNullOrEmpty(this.category) && !string.IsNullOrEmpty(this.title) && !string.IsNullOrEmpty(this.entryPoint));
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return (this.fileName.GetHashCode() ^ this.category.GetHashCode() ^ this.title.GetHashCode() ^ this.entryPoint.GetHashCode());
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            PluginData other = obj as PluginData;

            if (other != null)
            {
                return Equals(other);
            }

            return false;
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// <c>true</c> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(PluginData other)
        {
            if (other == null)
            {
                return false;
            }

            return (this.fileName == other.fileName && this.category == other.category && this.entryPoint == other.entryPoint && this.title == other.title);
        }
    }

    internal struct PIEntrypoint
    {
        /// <summary>
        /// The dll module handle
        /// </summary>
        public SafeLibraryHandle dll;
        /// <summary>
        /// The entrypoint for the FilterParmBlock and AboutRecord
        /// </summary>
        public PluginEntryPoint entryPoint;
    }
}
