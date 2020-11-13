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
using System.Runtime.Serialization;

namespace PSFilterHostDll
{
    /// <summary>
    /// The exception that is thrown when the image size exceeds 32000 pixels in width or height.
    /// </summary>
    /// <threadsafety static="true" instance="false" />
    [Serializable]
    public sealed class ImageSizeTooLargeException : Exception, ISerializable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageSizeTooLargeException"/> class.
        /// </summary>
        /// <overloads>Initializes a new instance of the <see cref="ImageSizeTooLargeException"/> class.</overloads>
        public ImageSizeTooLargeException() : base()
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageSizeTooLargeException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public ImageSizeTooLargeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageSizeTooLargeException"/> class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message to display.</param>
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
