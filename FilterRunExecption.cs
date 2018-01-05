/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2018 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.Serialization;

namespace PSFilterHostDll
{
    /// <summary>
    /// The exception that is thrown when a filter returns an error.
    /// </summary>
    /// <threadsafety static="true" instance="false" />
    [Serializable]
    public sealed class FilterRunException : Exception, ISerializable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FilterRunException"/> class.
        /// </summary>
        /// <overloads>Initializes a new instance of the <see cref="FilterRunException"/> class.</overloads>
        public FilterRunException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterRunException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message.</param>
        public FilterRunException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterRunException"/> class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner exception.</param>
        public FilterRunException(string message, Exception inner)
            : base(message, inner)
        {
        }

        // This constructor is needed for serialization.
        private FilterRunException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
