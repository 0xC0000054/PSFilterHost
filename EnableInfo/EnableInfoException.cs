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

using System;
using System.Runtime.Serialization;

namespace PSFilterHostDll.EnableInfo
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1064:ExceptionsShouldBePublic")]
    [Serializable]
    internal sealed class EnableInfoException : Exception
    {
        public EnableInfoException()
        {
        }

        public EnableInfoException(string message) : base(message)
        {
        }

        public EnableInfoException(string message, Exception innerException) : base(message, innerException)
        {
        }

        private EnableInfoException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
