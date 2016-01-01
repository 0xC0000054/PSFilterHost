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
using System.Collections.ObjectModel;
using System.Text;

namespace PSFilterHostDll
{
	internal sealed class EnableInfoParser
	{
		enum TokenTypes
		{
			None,
			Function,
			Constant, 
			Keyword,
			Or,
			Equals,
			Number,
			LParen, 
			RParen
		}

		class Expression
		{
			public string value;
			public int intValue;
			public TokenTypes type;

			public Expression()
			{
				this.value = string.Empty;
				this.intValue = 0;
				this.type = TokenTypes.None;
			}
		}

		private ReadOnlyCollection<string> keywords;
		private ReadOnlyCollection<string> constants;

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

		public EnableInfoParser()
		{
			Init();
		}

		public EnableInfoParser(bool grayScale)
		{
			Init();
			this.imageMode = grayScale ? Gray16Mode : RGB48Mode;
		}

		private void Init()
		{
			this.keywords = new ReadOnlyCollection<string>(new[] { psImageMode, psImageDepth });
			this.constants = new ReadOnlyCollection<string>(new[] { "true", "false", GrayScaleMode, RGBMode, Gray16Mode, RGB48Mode });
			this.index = 0;
		}

		public bool Parse(string info)
		{
			this.chars = info.ToCharArray();
			this.length = chars.Length;

			bool supports16Bit = false;

			while (index < length && !supports16Bit)
			{
				Expression exp = this.NextToken();

				switch (exp.type)
				{
					case TokenTypes.Function: // parse the in() function
						supports16Bit = ParseFunction(); 
						break;
					case TokenTypes.Constant: // enable all modes
						supports16Bit = (exp.value == "true" && length == 4);
						break;
					case TokenTypes.Or: // the || PSHOP_ImageDepth == 16 case
						supports16Bit = ParseOr();
						break;

				} 
			}

			return supports16Bit;
		}

		private bool SupportsMode(string mode)
		{
			this.imageMode = mode;
			this.index = 0;

			bool supportsMode = false;

			while (index < length && !supportsMode)
			{
				Expression exp = this.NextToken();

				switch (exp.type)
				{
					case TokenTypes.Function: // parse the in() function
						supportsMode = ParseFunction();
						break;
					case TokenTypes.Constant: // enable all modes
						supportsMode = (exp.value == "true" && length == 4);
						break;
				}
			}

			return supportsMode;
		}

		public ushort GetSupportedModes(string info)
		{
			this.chars = info.ToCharArray();
			this.length = chars.Length;

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

		private Expression NextToken()
		{
			SkipWhitespace();

			Expression exp = new Expression();

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
					exp.intValue = int.Parse(sb.ToString(), System.Globalization.CultureInfo.InvariantCulture);
					exp.type = TokenTypes.Number;
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

					exp.value = new string(chars, startIndex, index - startIndex);

					if (exp.value == inFunction)
					{
						exp.type = TokenTypes.Function;
					}
					else if (keywords.Contains(exp.value))
					{
						exp.type = TokenTypes.Keyword;
					}
					else if (constants.Contains(exp.value))
					{
						exp.type = TokenTypes.Constant;
					}
				}
				else
				{
					switch (chars[index])
					{
						case '|':
							if (chars[index + 1] == '|')
							{
								exp.value = "||";
								exp.type = TokenTypes.Or;
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
								exp.value = "==";
								exp.type = TokenTypes.Equals;
								index += 2;
							}
							else
							{
								index++;
							}
							break;
						case '(':
							exp.type = TokenTypes.LParen;
							index++;
							break;
						case ')':
							exp.type = TokenTypes.RParen;
							index++;
							break;
						default:
							index++;
							break;
					}
				} 
			}

			return exp;
		}

		private bool ParseFunction()
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
			Expression keyword = this.NextToken();

			if (keyword.value == psImageDepth)
			{
				Expression equal = this.NextToken();

				if (equal.type == TokenTypes.Equals)
				{
					Expression depth = this.NextToken();

					return depth.intValue == 16;  
				}
			}

			return false;
		}
	}
}
