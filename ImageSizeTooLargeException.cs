﻿/////////////////////////////////////////////////////////////////////////////////
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
using System.Runtime.Serialization;

namespace PSFilterHostDll
{
    /// <summary>
    /// The exception that is thrown when the image size exceeds 32000 pixels in width or height.
    /// </summary>
    [Serializable]
    public sealed class ImageSizeTooLargeException : Exception, ISerializable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageSizeTooLargeException"/> class.
        /// </summary>
        public ImageSizeTooLargeException()
            : base("An ImageSizeTooLargeException has occured")
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageSizeTooLargeException"/> class.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public ImageSizeTooLargeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageSizeTooLargeException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner exception.</param>
        public ImageSizeTooLargeException(string message, Exception inner)
            : base(message, inner)
        { 
        }

        private ImageSizeTooLargeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }


    }
}
