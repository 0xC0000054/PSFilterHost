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
using System.Collections.ObjectModel;
using System.Text;

namespace PSFilterHostDll.EnableInfo
{
    internal sealed class LegacyEnableInfoParser
    {
        private enum TokenType
        {
            None,
            InFunction,
            BooleanConstant,
            Identifier,
            Or,
            Equals,
            Number,
            LParen,
            RParen
        }

        private class Token
        {
            public string value;
            public int intValue;
            public TokenType type;

            public Token()
            {
                value = string.Empty;
                intValue = 0;
                type = TokenType.None;
            }
        }

        private string imageMode;
        private char[] chars;
        private int index;
        private int length;

        private const string inFunction = "in";
        private const string psImageMode = "PSHOP_ImageMode";
        private const string psImageDepth = "PSHOP_ImageDepth";

        private const string RGBMode = "RGBMode";
        private const string RGB48Mode = "RGB48Mode";
        private const string GrayScaleMode = "GrayScaleMode";
        private const string Gray16Mode = "Gray16Mode";

        public LegacyEnableInfoParser() : this(false)
        {
        }

        public LegacyEnableInfoParser(bool grayScale)
        {
            index = 0;
            imageMode = grayScale ? Gray16Mode : RGB48Mode;
        }

        public bool Parse(string info)
        {
            chars = info.ToCharArray();
            length = chars.Length;

            bool supports16Bit = false;

            while (index < length && !supports16Bit)
            {
                Token token = NextToken();

                switch (token.type)
                {
                    case TokenType.InFunction: // parse the in() function
                        supports16Bit = ParseInFunction();
                        break;
                    case TokenType.BooleanConstant: // enable all modes
                        supports16Bit = token.value == "true" && length == 4;
                        break;
                    case TokenType.Or: // the || PSHOP_ImageDepth == 16 case
                        supports16Bit = ParseOr();
                        break;
                }
            }

            return supports16Bit;
        }

        private bool SupportsMode(string mode)
        {
            imageMode = mode;
            index = 0;

            bool supportsMode = false;

            while (index < length && !supportsMode)
            {
                Token token = NextToken();

                switch (token.type)
                {
                    case TokenType.InFunction: // parse the in() function
                        supportsMode = ParseInFunction();
                        break;
                    case TokenType.BooleanConstant: // enable all modes
                        supportsMode = token.value == "true" && length == 4;
                        break;
                }
            }

            return supportsMode;
        }

        public ushort GetSupportedModes(string info)
        {
            chars = info.ToCharArray();
            length = chars.Length;

            ushort modes = 0;

            if (SupportsMode(RGBMode))
            {
                modes |= PSFilterHostDll.PSApi.PSConstants.flagSupportsRGBColor;
            }

            if (SupportsMode(GrayScaleMode))
            {
                modes |= PSFilterHostDll.PSApi.PSConstants.flagSupportsGrayScale;
            }

            if (SupportsMode(RGB48Mode))
            {
                modes |= PSFilterHostDll.PSApi.PSConstants.flagSupportsRGB48;
            }

            if (SupportsMode(Gray16Mode))
            {
                modes |= PSFilterHostDll.PSApi.PSConstants.flagSupportsGray16;
            }

            return modes;
        }

        private void SkipWhitespace()
        {
            while (char.IsWhiteSpace(chars[index]))
            {
                index++;

                if (index == length)
                {
                    break;
                }
            }
        }

        private Token NextToken()
        {
            SkipWhitespace();

            Token token = new Token();

            if (index < length)
            {
                if (chars[index] >= '0' && chars[index] <= '9')
                {
                    StringBuilder sb = new StringBuilder();
                    while (Char.IsDigit(chars[index]))
                    {
                        sb.Append(chars[index]);
                        index++;

                        if (index == length)
                        {
                            break;
                        }
                    }
                    token.intValue = int.Parse(sb.ToString(), System.Globalization.CultureInfo.InvariantCulture);
                    token.type = TokenType.Number;
                }
                else if (Char.IsLetter(chars[index]))
                {
                    int startIndex = index;

                    while (Char.IsLetter(chars[index]) || chars[index] == '_')
                    {
                        index++;

                        if (index == length)
                        {
                            break;
                        }
                    }

                    token.value = new string(chars, startIndex, index - startIndex);

                    if (token.value == inFunction)
                    {
                        token.type = TokenType.InFunction;
                    }
                    else if (string.Equals(token.value, "true", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(token.value, "false", StringComparison.OrdinalIgnoreCase))
                    {
                        token.type = TokenType.BooleanConstant;
                    }
                    else
                    {
                        token.type = TokenType.Identifier;
                    }
                }
                else
                {
                    switch (chars[index])
                    {
                        case '|':
                            if (chars[index + 1] == '|')
                            {
                                token.value = "||";
                                token.type = TokenType.Or;
                                index += 2;
                            }
                            else
                            {
                                index++;
                            }
                            break;
                        case '=':
                            if (chars[index + 1] == '=')
                            {
                                token.value = "==";
                                token.type = TokenType.Equals;
                                index += 2;
                            }
                            else
                            {
                                index++;
                            }
                            break;
                        case '(':
                            token.type = TokenType.LParen;
                            index++;
                            break;
                        case ')':
                            token.type = TokenType.RParen;
                            index++;
                            break;
                        default:
                            index++;
                            break;
                    }
                }
            }

            return token;
        }

        private bool ParseInFunction()
        {
            SkipWhitespace();
            int argCount = 1;

            if (chars[index] == '(')
            {
                index++;
            }

            int startIndex = index;

            while (chars[index] != ')')
            {
                if (chars[index] == ',')
                {
                    argCount++;
                }
                index++;
            }

            string args = new string(chars, startIndex, index - startIndex);

            if (index < length)
            {
                index++; // Skip the closing parentheses.
            }

            string[] split = args.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries); // remove spaces from the strings
            int sLength = split.Length;

            if (split[0] == psImageMode)
            {
                for (int i = 1; i < sLength; i++)
                {
                    if (split[i] == imageMode)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool ParseOr()
        {
            Token keyword = NextToken();

            if (keyword.value == psImageDepth)
            {
                Token equal = NextToken();

                if (equal.type == TokenType.Equals)
                {
                    Token depth = NextToken();

                    return depth.intValue == 16;
                }
            }

            return false;
        }
    }
}
