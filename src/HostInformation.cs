/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2020 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;

namespace PSFilterHostDll
{
    /// <summary>
    /// Represents the host information used by the filters.
    /// </summary>
    /// <threadsafety static="true" instance="false" />
    public sealed class HostInformation
    {
#pragma warning disable IDE0032 // Use auto property
        private string title;
        private string caption;
        private Uri url;
        private bool copyRight;
        private bool waterMark;
        private HostRulerUnit rulerUnit;
        private bool highDpi;
#pragma warning restore IDE0032 // Use auto property

        /// <summary>
        /// Gets or sets the title of the document.
        /// </summary>
        /// <value>
        /// The title of the document.
        /// </value>
        public string Title
        {
            get => title;
            set => title = value;
        }

        /// <summary>
        /// Gets or sets the caption of the document.
        /// </summary>
        /// <value>
        /// The caption of the document.
        /// </value>
        /// <remarks>
        /// This can be set by a filter.
        /// </remarks>
        public string Caption
        {
            get => caption;
            set => caption = value;
        }

        /// <summary>
        /// Gets or sets the URL of the document.
        /// </summary>
        /// <value>
        /// The URL of the document.
        /// </value>
        /// <remarks>
        /// This can be set by a filter.
        /// </remarks>
        public Uri Url
        {
            get => url;
            set => url = value;
        }

        /// <summary>
        /// Gets or sets the copyright status of the document.
        /// </summary>
        /// <value>
        /// The copyright status of the document.
        /// </value>
        /// <remarks>
        /// This can be set by a filter.
        /// </remarks>
        public bool Copyright
        {
            get => copyRight;
            set => copyRight = value;
        }

        /// <summary>
        /// Gets or sets the watermark status of the document.
        /// </summary>
        /// <value>
        /// The watermark status of the document.
        /// </value>
        /// <remarks>
        /// This can be set by a filter to indicate if it has found an embedded digital signature.
        /// </remarks>
        public bool Watermark
        {
            get => waterMark;
            set => waterMark = value;
        }

        /// <summary>
        /// Gets or sets the ruler measurement unit of the document.
        /// </summary>
        /// <value>
        /// The ruler measurement unit of the document.
        /// </value>
        /// <exception cref="InvalidEnumArgumentException">
        /// Ruler unit cannot be set as it does not use a valid value, as defined in the <see cref="HostRulerUnit"/> enumeration.
        /// </exception>
        public HostRulerUnit RulerUnit
        {
            get => rulerUnit;
            set
            {
                if (value < HostRulerUnit.Pixels || value > HostRulerUnit.Percent)
                {
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(HostRulerUnit));
                }

                rulerUnit = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the host is running in high DPI mode.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the host is running in high DPI mode; otherwise, <c>false</c>.
        /// </value>
        public bool HighDpi
        {
            get => highDpi;
            set => highDpi = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HostInformation"/> class.
        /// </summary>
        public HostInformation()
        {
            title = null;
            caption = null;
            url = null;
            copyRight = false;
            waterMark = false;
            rulerUnit = HostRulerUnit.Pixels;
            highDpi = false;
        }
    }
}
