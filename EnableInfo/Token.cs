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

using System.Globalization;

namespace PSFilterHostDll.EnableInfo
{
    internal sealed class Token
    {
        public Token(TokenType type) : this(type, null)
        {
        }

        public Token(TokenType type, string value)
        {
            Type = type;
            Value = value;
        }

        public TokenType Type { get; }

        public string Value { get; }

        public override string ToString()
        {
            if (Value != null)
            {
                return string.Format(CultureInfo.InvariantCulture, "Type: {0}, Value: {1}", Type, Value);
            }
            else
            {
                return string.Format(CultureInfo.InvariantCulture, "Type: {0}", Type);
            }
        }
    }
}
