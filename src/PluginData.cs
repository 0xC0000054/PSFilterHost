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
using System.Globalization;
using System.Diagnostics;
using System.Runtime.Serialization;
using PSFilterHostDll.EnableInfo;
using PSFilterHostDll.PSApi;

#if GDIPLUS
using System.Drawing;
#else
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;
#endif

namespace PSFilterHostDll
{
    /// <summary>
    /// Represents the information used to load and execute a Photoshop-compatible filter plug-in.
    /// </summary>
    /// <threadsafety static="true" instance="false" />
    [DebuggerTypeProxy(typeof(PluginDataDebugView))]
    [Serializable]
    public sealed class PluginData : IEquatable<PluginData>
    {
        private readonly string fileName;
        private readonly string entryPoint;
        private readonly string category;
        private readonly string title;
        private FilterCaseInfo[] filterInfo;
        private readonly PluginAETE aete;
        private readonly string enableInfo;
        private ushort? supportedModes;
        private string[] moduleEntryPoints;
        [OptionalField(VersionAdded = 2)]
        private bool hasAboutBox;

        /// <summary>
        /// Gets the filename of the filter.
        /// </summary>
        public string FileName => fileName;

        /// <summary>
        /// Gets the entry point of the filter.
        /// </summary>
        public string EntryPoint => entryPoint;

        /// <summary>
        /// Gets the category of the filter.
        /// </summary>
        public string Category => category;

        /// <summary>
        /// Gets the title of the filter.
        /// </summary>
        public string Title => title;

        /// <summary>
        /// Gets a value indicating whether this instance has information that describes how images with transparency should be processed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has information that describes how images with transparency should be processed.; otherwise, <c>false</c>.
        /// </value>
        internal bool HasFilterInfo => filterInfo != null;

        /// <summary>
        /// Gets the scripting information used by the plug-in.
        /// </summary>
        internal PluginAETE Aete => aete;

        /// <summary>
        /// Gets or sets the entry points used to show the about box for a module containing multiple plug-ins.
        /// </summary>
        internal string[] ModuleEntryPoints
        {
            get => moduleEntryPoints;
            set => moduleEntryPoints = value;
        }

        /// <summary>
        /// Gets a value indicating whether this filter has an about box.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this filter has an about box; otherwise, <c>false</c>.
        /// </value>
        public bool HasAboutBox => hasAboutBox;

        /// <summary>
        /// Gets the filter information that describes how images with transparency should be processed.
        /// </summary>
        /// <param name="filterCase">The filter case.</param>
        /// <returns>The <see cref="FilterCaseInfo"/> for the specified filter case.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="filterCase"/> is not a valid <see cref="FilterCase"/> value.</exception>
        /// <exception cref="InvalidOperationException">Cannot call this method when a HasFilterInfo is false.</exception>
        internal FilterCaseInfo GetFilterInfo(FilterCase filterCase)
        {
            if (filterCase < FilterCase.FlatImageNoSelection || filterCase > FilterCase.ProtectedTransparencyWithSelection)
            {
                throw new ArgumentOutOfRangeException(nameof(filterCase));
            }

            if (!HasFilterInfo)
            {
                throw new InvalidOperationException("Cannot call this method when HasFilterInfo is false.");
            }

            int index = (int)filterCase - 1;

            return filterInfo[index];
        }

        /// <summary>
        /// Gets the mode that indicates how the filter processes transparency.
        /// </summary>
        /// <param name="imageMode">The image mode.</param>
        /// <param name="hasSelection"><c>true</c> if the host has an active selection; otherwise, <c>false</c>.</param>
        /// <param name="hasTransparency">A <see cref="Func{TResult}"/> delegate that allows the method to determine if the image has transparency.</param>
        /// <returns>One of the <see cref="FilterCase"/> values indicating how the filter processes transparency.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="hasTransparency"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="imageMode"/> is not a supported value.</exception>
        internal FilterCase GetFilterTransparencyMode(ImageModes imageMode, bool hasSelection, Func<bool> hasTransparency)
        {
            if (hasTransparency == null)
            {
                throw new ArgumentNullException(nameof(hasTransparency));
            }

            FilterCase filterCase;

            if (imageMode == ImageModes.RGB || imageMode == ImageModes.RGB48)
            {
                // Some filters do not handle transparency correctly despite what their FilterInfo says.
                if (filterInfo == null ||
                    category.Equals("Axion", StringComparison.Ordinal) ||
                    (category.Equals("Vizros 4", StringComparison.Ordinal) && title.StartsWith("Lake", StringComparison.Ordinal)) ||
                    (category.Equals("Nik Collection", StringComparison.Ordinal) && title.StartsWith("Dfine 2", StringComparison.Ordinal)))
                {
                    if (hasTransparency())
                    {
                        filterCase = FilterCase.FloatingSelection;
                    }
                    else
                    {
                        filterCase = hasSelection ? FilterCase.FlatImageWithSelection : FilterCase.FlatImageNoSelection;
                    }
                }
                else
                {
                    filterCase = hasSelection ? FilterCase.EditableTransparencyWithSelection : FilterCase.EditableTransparencyNoSelection;

                    int filterCaseIndex = (int)filterCase - 1;

                    // If the EditableTransparency cases are not supported use the other modes.
                    if (!filterInfo[filterCaseIndex].IsSupported)
                    {
                        if (hasTransparency())
                        {
                            if (filterInfo[filterCaseIndex + 2].IsSupported)
                            {
                                switch (filterCase)
                                {
                                    case FilterCase.EditableTransparencyNoSelection:
                                        filterCase = FilterCase.ProtectedTransparencyNoSelection;
                                        break;
                                    case FilterCase.EditableTransparencyWithSelection:
                                        filterCase = FilterCase.ProtectedTransparencyWithSelection;
                                        break;
                                }
                            }
                            else
                            {
                                // If the protected transparency modes are not supported treat the transparency as a floating selection.
                                filterCase = FilterCase.FloatingSelection;
                            }
                        }
                        else
                        {
                            switch (filterCase)
                            {
                                case FilterCase.EditableTransparencyNoSelection:
                                    filterCase = FilterCase.FlatImageNoSelection;
                                    break;
                                case FilterCase.EditableTransparencyWithSelection:
                                    filterCase = FilterCase.FlatImageWithSelection;
                                    break;
                            }
                        }
                    }
                }
            }
            else
            {
                switch (imageMode)
                {
                    case ImageModes.GrayScale:
                    case ImageModes.Gray16:
                    case ImageModes.CMYK:
                        filterCase = hasSelection ? FilterCase.FlatImageWithSelection : FilterCase.FlatImageNoSelection;
                        break;
                    default:
                        throw new ArgumentException(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Image mode {0} is not supported.", imageMode));
                }
            }

            return filterCase;
        }

#if GDIPLUS
        /// <summary>
        /// Determines whether the filter can process the specified image and host application state.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="hostState">The current state of the host application.</param>
        /// <returns>
        /// <c>true</c> if the filter can process the image and host application state; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <para>
        ///     <paramref name="image"/> is null.
        /// </para>
        /// <para>
        ///     -or-
        /// </para>
        /// <para>
        ///     <paramref name="hostState"/> is null.
        /// </para>
        /// </exception>
        /// <remarks>
        /// <para>
        ///    A filter can specify the image and host application conditions it requires to execute.
        ///    For example, a filter may specify that the image must be at least 128 pixels in width
        ///    or that the host must have an active selection.
        /// </para>
        /// <para>
        ///    The host can use this method to prevent a filter from being selected in its user interface
        ///    when the conditions the filter requires to execute are not met.
        /// <note type="note">
        ///    Some filters may remain enabled in the host's user interface and display an error message at runtime.
        /// </note>
        /// </para>
        /// </remarks>
        /// <seealso cref="HostState"/>
        public bool SupportsHostState(Bitmap image, HostState hostState)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }
            if (hostState == null)
            {
                throw new ArgumentNullException(nameof(hostState));
            }

            const ImageModes imageMode = ImageModes.RGB;
            FilterCase filterCase = GetFilterTransparencyMode(imageMode, hostState.HasSelection, () => TransparencyDetection.ImageHasTransparency(image));

            return IsHostStateSupported(image.Width, image.Height, imageMode, filterCase, hostState);
        }
#else
        /// <summary>
        /// Determines whether the filter can process the specified image and host application state.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="hostState">The current state of the host application.</param>
        /// <returns>
        /// <c>true</c> if the filter can process the image and host application state; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <para>
        ///     <paramref name="image"/> is null.
        /// </para>
        /// <para>
        ///     -or-
        /// </para>
        /// <para>
        ///     <paramref name="hostState"/> is null.
        /// </para>
        /// </exception>
        /// <remarks>
        /// <para>
        ///    A filter can specify the image and host application conditions it requires to execute.
        ///    For example, a filter may specify that the image must be at least 128 pixels in width
        ///    or that the host must have an active selection.
        /// </para>
        /// <para>
        ///    The host can use this method to prevent a filter from being selected in its user interface
        ///    when the conditions the filter requires to execute are not met.
        /// <note type="note">
        ///    Some filters may remain enabled in the host's user interface and display an error message at runtime.
        /// </note>
        /// </para>
        /// </remarks>
        /// <seealso cref="HostState"/>
        public bool SupportsHostState(BitmapSource image, HostState hostState)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }
            if (hostState == null)
            {
                throw new ArgumentNullException(nameof(hostState));
            }

            ImageModes imageMode = ImageModes.RGB;

            PixelFormat format = image.Format;

            if (format == PixelFormats.Cmyk32)
            {
                imageMode = ImageModes.CMYK;
            }
            else if (format == PixelFormats.BlackWhite ||
                     format == PixelFormats.Gray2 ||
                     format == PixelFormats.Gray4 ||
                     format == PixelFormats.Gray8)
            {
                imageMode = ImageModes.GrayScale;
            }
            else if (format == PixelFormats.Gray16 || format == PixelFormats.Gray32Float)
            {
                imageMode = ImageModes.Gray16;
            }
            else if (format == PixelFormats.Rgb48 ||
                     format == PixelFormats.Rgba64 ||
                     format == PixelFormats.Rgba128Float ||
                     format == PixelFormats.Rgb128Float ||
                     format == PixelFormats.Prgba64 ||
                     format == PixelFormats.Prgba128Float)
            {
                imageMode = ImageModes.RGB48;
            }

            FilterCase filterCase = GetFilterTransparencyMode(imageMode, hostState.HasSelection, () => TransparencyDetection.ImageHasTransparency(image));

            return IsHostStateSupported(image.PixelWidth, image.PixelHeight, imageMode, filterCase, hostState);
        }
#endif

        private bool IsHostStateSupported(int imageWidth, int imageHeight, ImageModes imageMode, FilterCase filterCase, HostState hostState)
        {
            if (!supportedModes.HasValue)
            {
                if (string.Equals(enableInfo, "true", StringComparison.OrdinalIgnoreCase))
                {
                    // All image modes are supported.
                    supportedModes = ushort.MaxValue;
                }
                else
                {
                    supportedModes = 0;
                }
            }

            bool result;
            switch (imageMode)
            {
                case ImageModes.CMYK:
                    result = (supportedModes.Value & PSConstants.flagSupportsCMYKColor) == PSConstants.flagSupportsCMYKColor;
                    break;
                case ImageModes.GrayScale:
                    result = (supportedModes.Value & PSConstants.flagSupportsGrayScale) == PSConstants.flagSupportsGrayScale;
                    break;
                case ImageModes.Gray16:
                    result = (supportedModes.Value & PSConstants.flagSupportsGray16) == PSConstants.flagSupportsGray16;
                    break;
                case ImageModes.RGB:
                    result = (supportedModes.Value & PSConstants.flagSupportsRGBColor) == PSConstants.flagSupportsRGBColor;
                    break;
                case ImageModes.RGB48:
                    result = (supportedModes.Value & PSConstants.flagSupportsRGB48) == PSConstants.flagSupportsRGB48;
                    break;
                default:
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Image mode {0} is not supported.", imageMode));
            }

            if (!string.IsNullOrEmpty(enableInfo))
            {
                int targetChannelCount;
                int trueChannelCount;
                bool hasTransparencyMask = false;

                switch (imageMode)
                {
                    case ImageModes.Bitmap:
                    case ImageModes.GrayScale:
                    case ImageModes.Indexed:
                    case ImageModes.Multichannel:
                    case ImageModes.Duotone:
                    case ImageModes.Gray16:
                        targetChannelCount = 1;
                        trueChannelCount = 1;
                        break;
                    case ImageModes.CMYK:
                        targetChannelCount = 4;
                        trueChannelCount = 4;
                        break;
                    case ImageModes.HSL:
                    case ImageModes.HSB:
                    case ImageModes.Lab:
                        targetChannelCount = 3;
                        trueChannelCount = 3;
                        break;
                    case ImageModes.RGB:
                    case ImageModes.RGB48:
                        targetChannelCount = 3;
                        if (filterCase == FilterCase.EditableTransparencyNoSelection ||
                            filterCase == FilterCase.EditableTransparencyWithSelection ||
                            filterCase == FilterCase.ProtectedTransparencyNoSelection ||
                            filterCase == FilterCase.ProtectedTransparencyWithSelection)
                        {
                            trueChannelCount = 4;
                            hasTransparencyMask = true;
                        }
                        else
                        {
                            trueChannelCount = 3;
                        }
                        break;
                    default:
                        throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Image mode {0} is not supported.", imageMode));
                }

                EnableInfoVariables variables = new EnableInfoVariables(imageWidth, imageHeight, imageMode, hasTransparencyMask, targetChannelCount,
                                                                        trueChannelCount, hostState);

                bool? enableInfoResult = EnableInfoResultCache.Instance.TryGetValue(enableInfo, variables);
                if (enableInfoResult.HasValue)
                {
                    result = enableInfoResult.Value;
                }
            }

            if (filterInfo != null)
            {
                result &= filterInfo[(int)filterCase - 1].IsSupported;
            }

            return result;
        }

#if !GDIPLUS
        /// <summary>
        /// Checks if the filter supports processing images in the current <see cref="System.Windows.Media.PixelFormat"/>.
        /// </summary>
        /// <param name="mode">The <see cref="System.Windows.Media.PixelFormat"/> of the image to process.</param>
        /// <returns><c>true</c> if the filter can process the image; otherwise <c>false</c>.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Please use SupportsHostState(BitmapSource, HostState) instead.", false)]
        public bool SupportsImageMode(PixelFormat mode)
        {
            if (!supportedModes.HasValue)
            {
                DetectSupportedModes();
            }

            if (mode == PixelFormats.Cmyk32)
            {
                return (supportedModes.Value & PSConstants.flagSupportsCMYKColor) == PSConstants.flagSupportsCMYKColor;
            }
            else if (mode == PixelFormats.BlackWhite || mode == PixelFormats.Gray2 || mode == PixelFormats.Gray4 || mode == PixelFormats.Gray8)
            {
                return (supportedModes.Value & PSConstants.flagSupportsGrayScale) == PSConstants.flagSupportsGrayScale;
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
                return (supportedModes.Value & PSConstants.flagSupportsRGBColor) == PSConstants.flagSupportsRGBColor;
            }
        }

        private void DetectSupportedModes()
        {
            if (!string.IsNullOrEmpty(enableInfo))
            {
                if (enableInfo == "true")
                {
                    supportedModes = ushort.MaxValue; // All modes are supported
                }
                else
                {
                    LegacyEnableInfoParser parser = new LegacyEnableInfoParser();
                    supportedModes = parser.GetSupportedModes(enableInfo);
                }
            }
            else
            {
                supportedModes = 0;
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
            if (!string.IsNullOrEmpty(enableInfo))
            {
                LegacyEnableInfoParser parser = new LegacyEnableInfoParser(grayScale);
                return parser.Parse(enableInfo);
            }

            if (grayScale)
            {
                return (supportedModes.Value & PSConstants.flagSupportsGray16) == PSConstants.flagSupportsGray16;
            }

            return (supportedModes.Value & PSConstants.flagSupportsRGB48) == PSConstants.flagSupportsRGB48;
        }
#endif
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginData"/> class.
        /// </summary>
        /// <param name="fileName">The file name of the filter.</param>
        /// <param name="entryPoint">The entry point of the filter.</param>
        /// <param name="category">The category of the filter.</param>
        /// <param name="title">The title of the filter.</param>
        /// <param name="supportedModes">The bit field describing the image modes supported by the filter.</param>
        internal PluginData(string fileName, string entryPoint, string category, string title, ushort supportedModes) :
            this(fileName, entryPoint, category, title, null, null, null, supportedModes, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginData" /> class.
        /// </summary>
        /// <param name="fileName">The file name of the filter.</param>
        /// <param name="entryPoint">The entry point of the filter.</param>
        /// <param name="category">The category of the filter.</param>
        /// <param name="title">The title of the filter.</param>
        /// <param name="filterInfo">The filter information that describes how images with transparency should be processed.</param>
        /// <param name="aete">The scripting data of the filter.</param>
        /// <param name="enableInfo">The information describing the conditions that the filter requires to execute.</param>
        /// <param name="supportedModes">The bit field describing the image modes supported by the filter.</param>
        /// <param name="hasAboutBox">Indicates if the filter has an about box.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> is null.</exception>
        internal PluginData(string fileName, string entryPoint, string category, string title, FilterCaseInfo[] filterInfo, PluginAETE aete,
            string enableInfo, ushort? supportedModes, bool hasAboutBox)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            this.fileName = fileName;
            this.entryPoint = entryPoint;
            this.category = category;
            this.title = title;
            this.filterInfo = filterInfo;
            this.aete = aete;
            this.enableInfo = enableInfo;
            this.supportedModes = supportedModes;
            moduleEntryPoints = null;
            this.hasAboutBox = hasAboutBox;
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            hasAboutBox = true;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
        }

        /// <summary>
        /// Determines whether this instance is valid.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
        /// </returns>
        internal bool IsValid()
        {
            return !string.IsNullOrEmpty(category) && !string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(entryPoint);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            int hash = 23;

            unchecked
            {
                hash = (hash * 127) + (fileName != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(fileName) : 0);
                hash = (hash * 127) + (entryPoint != null ? entryPoint.GetHashCode() : 0);
                hash = (hash * 127) + (category != null ? category.GetHashCode() : 0);
                hash = (hash * 127) + (title != null ? title.GetHashCode() : 0);
            }

            return hash;
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
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

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

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(fileName, other.fileName, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(entryPoint, other.entryPoint, StringComparison.Ordinal) &&
                   string.Equals(category, other.category, StringComparison.Ordinal) &&
                   string.Equals(title, other.title, StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether two PluginData instances have the same value.
        /// </summary>
        /// <param name="p1">The first object to compare.</param>
        /// <param name="p2">The second object to compare.</param>
        /// <returns>
        /// <c>true</c> if the PluginData instances are equal; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator ==(PluginData p1, PluginData p2)
        {
            if (ReferenceEquals(p1, p2))
            {
                return true;
            }

            if (((object)p1) == null || ((object)p2) == null)
            {
                return false;
            }

            return p1.Equals(p2);
        }

        /// <summary>
        /// Determines whether two PluginData instances do not have the same value.
        /// </summary>
        /// <param name="p1">The first object to compare.</param>
        /// <param name="p2">The second object to compare.</param>
        /// <returns>
        /// <c>true</c> if the PluginData instances are not equal; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator !=(PluginData p1, PluginData p2)
        {
            return !(p1 == p2);
        }

        private sealed class PluginDataDebugView
        {
            private readonly PluginData pluginData;

            public PluginDataDebugView(PluginData pluginData)
            {
                if (pluginData == null)
                {
                    throw new ArgumentNullException(nameof(pluginData));
                }

                this.pluginData = pluginData;
            }

            public string FileName => pluginData.FileName;

            public string EntryPoint => pluginData.EntryPoint;

            public string Category => pluginData.Category;

            public string Title => pluginData.Title;

            public bool HasAboutBox => pluginData.HasAboutBox;
        }
    }
}
