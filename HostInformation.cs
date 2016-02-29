/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2016 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;

namespace PSFilterHostDll
{
    /// <summary>
    /// The class that encapsulates the host information used by the filters.
    /// </summary>
    /// <threadsafety static="true" instance="false" />
    public sealed class HostInformation
    {
        private string title;
        private string caption;
        private Uri url;
        private bool copyRight;
        private bool waterMark;
        private HostRulerUnit rulerUnit;

        /// <summary>
        /// Gets or sets the title of the document.
        /// </summary>
        /// <value>
        /// The title of the document.
        /// </value>
        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                title = value;
            }
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
            get
            {
                return caption;
            }
            set
            {
                caption = value;
            }
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
            get
            {
                return url;
            }
            set
            {
                url = value;
            }
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
            get
            {
                return copyRight;
            }
            set
            {
                copyRight = value;
            }
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
            get
            {
                return waterMark;
            }
            set
            {
                waterMark = value;
            }
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
            get
            {
                return rulerUnit;
            }
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
        /// Initializes a new instance of the <see cref="HostInformation"/> class.
        /// </summary>
        public HostInformation()
        {
            this.title = null;
            this.caption = null;
            this.url = null;
            this.copyRight = false;
            this.waterMark = false;
            this.rulerUnit = HostRulerUnit.Pixels;
        }
    }
}
