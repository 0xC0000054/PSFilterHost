/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2021 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.PSApi.PICA
{
    internal sealed class ErrorSuite
    {
        private readonly ErrorSuiteSetErrorFromPString setErrorFromPString;
        private readonly ErrorSuiteSetErrorFromCString setErrorFromCString;
        private readonly ErrorSuiteSetErrorFromZString setErrorFromZString;
        private readonly IASZStringSuite zstringSuite;
        private string errorMessage;

        /// <summary>
        /// Gets the error message.
        /// </summary>
        /// <value>
        /// The error message.
        /// </value>
        public string ErrorMessage => errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorSuite"/> class.
        /// </summary>
        /// <param name="zstringSuite">The ASZString suite.</param>
        /// <exception cref="ArgumentNullException"><paramref name="zstringSuite"/> is null.</exception>
        public ErrorSuite(IASZStringSuite zstringSuite)
        {
            if (zstringSuite == null)
            {
                throw new ArgumentNullException(nameof(zstringSuite));
            }

            setErrorFromPString = new ErrorSuiteSetErrorFromPString(SetErrorFromPString);
            setErrorFromCString = new ErrorSuiteSetErrorFromCString(SetErrorFromCString);
            setErrorFromZString = new ErrorSuiteSetErrorFromZString(SetErrorFromZString);
            this.zstringSuite = zstringSuite;
            errorMessage = null;
        }

        /// <summary>
        /// Creates the error suite version 1 structure.
        /// </summary>
        /// <returns>A <see cref="PSErrorSuite1"/> structure.</returns>
        public PSErrorSuite1 CreateErrorSuite1()
        {
            PSErrorSuite1 suite = new PSErrorSuite1
            {
                SetErrorFromPString = Marshal.GetFunctionPointerForDelegate(setErrorFromPString),
                SetErrorFromCString = Marshal.GetFunctionPointerForDelegate(setErrorFromCString),
                SetErrorFromZString = Marshal.GetFunctionPointerForDelegate(setErrorFromZString)
            };

            return suite;
        }

        private unsafe int SetErrorFromPString(IntPtr str)
        {
            if (str != IntPtr.Zero)
            {
                errorMessage = StringUtil.FromPascalString((byte*)str.ToPointer());

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int SetErrorFromCString(IntPtr str)
        {
            if (str != IntPtr.Zero)
            {
                errorMessage = StringUtil.FromCString(str, StringUtil.StringTrimOption.WhiteSpaceAndNullTerminator);

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int SetErrorFromZString(ASZString str)
        {
            string value;
            if (zstringSuite.ConvertToString(str, out value))
            {
                errorMessage = value;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }
    }
}
