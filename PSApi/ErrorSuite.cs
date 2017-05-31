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

using System;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.PSApi
{
    internal sealed class ErrorSuite
    {
        private readonly ErrorSuiteSetErrorFromPString setErrorFromPString;
        private readonly ErrorSuiteSetErrorFromCString setErrorFromCString;
        private readonly ErrorSuiteSetErrorFromZString setErrorFromZString;
        private string errorMessage;

        /// <summary>
        /// Gets the error message.
        /// </summary>
        /// <value>
        /// The error message.
        /// </value>
        public string ErrorMessage
        {
            get
            {
                return this.errorMessage;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has an error message.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has an error message; otherwise, <c>false</c>.
        /// </value>
        public bool HasErrorMessage
        {
            get
            {
                return this.errorMessage != null;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorSuite"/> class.
        /// </summary>
        public ErrorSuite()
        {
            this.setErrorFromPString = new ErrorSuiteSetErrorFromPString(SetErrorFromPString);
            this.setErrorFromCString = new ErrorSuiteSetErrorFromCString(SetErrorFromCString);
            this.setErrorFromZString = new ErrorSuiteSetErrorFromZString(SetErrorFromZString);
            this.errorMessage = null;
        }

        /// <summary>
        /// Creates the error suite version 1 structure.
        /// </summary>
        /// <returns>A <see cref="PSErrorSuite1"/> structure.</returns>
        public PSErrorSuite1 CreateErrorSuite1()
        {
            PSErrorSuite1 suite = new PSErrorSuite1
            {
                SetErrorFromPString = Marshal.GetFunctionPointerForDelegate(this.setErrorFromPString),
                SetErrorFromCString = Marshal.GetFunctionPointerForDelegate(this.setErrorFromCString),
                SetErrorFromZString = Marshal.GetFunctionPointerForDelegate(this.setErrorFromZString)
            };

            return suite;
        }

        private unsafe int SetErrorFromPString(IntPtr str)
        {
            if (str != IntPtr.Zero)
            {
                this.errorMessage = StringUtil.FromPascalString((byte*)str.ToPointer());

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int SetErrorFromCString(IntPtr str)
        {
            if (str != IntPtr.Zero)
            {
                this.errorMessage = Marshal.PtrToStringAnsi(str);

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int SetErrorFromZString(IntPtr str)
        {
            string value;
            if (PICA.ASZStringSuite.Instance.ConvertToString(str, out value))
            {
                this.errorMessage = value;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }
    }
}
