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

namespace PSFilterHostDll.PSApi
{
    internal interface IPropertySuite
    {
        /// <summary>
        /// Creates the property suite structure.
        /// </summary>
        /// <returns>A <see cref="PropertyProcs"/> structure.</returns>
        PropertyProcs CreatePropertySuite();
    }
}
