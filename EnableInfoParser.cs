/////////////////////////////////////////////////////////////////////////////////
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
using System.Collections.Generic;
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

		private List<string> keywords;
		private List<string> constants;

		private string imageMode;
		private char[] chars;
		private int index;
		private int length;
		private bool supports16Bit;

		private const string inFunction = "in";
		private const string psImageMode = "PSHOP_ImageMode";
		private const string psImageDepth = "PSHOP_ImageDepth";

		public EnableInfoParser(bool grayScale)
		{
			keywords  = new List<string>(new[] { psImageMode, psImageDepth });
			constants = new List<string>(new[] { "true", "false", "GrayScaleMode", "RGBMode", "Gray16Mode", "RGB48Mode" });

			imageMode =  grayScale ? "Gray16Mode" : "RGB48Mode";
			
			index = 0;
			supports16Bit = false;
		}


		public bool Parse(string info)
		{
			chars = info.ToCharArray();

			length = chars.Length; 

			while (index < length && !supports16Bit)
			{
				Expression exp = this.NextToken();

				switch (exp.type)
				{
					case TokenTypes.Function: // parse the in() function
						ParseFunction(); 
						break;
					case TokenTypes.Constant: // enable all modes
						this.supports16Bit = (exp.value == "true" && length == 4);
						break;
					case TokenTypes.Or: // the || PSHOP_ImageDepth == 16 case
						ParseOr();
						break;

				} 
			}

			return supports16Bit;
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

			StringBuilder sb = new StringBuilder();

			Expression exp = new Expression();

			if (chars[index] >= '0' && chars[index] <= '9') 
			{
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
				while (Char.IsLetter(chars[index]) || chars[index] == '_')
				{
					sb.Append(chars[index]);
					index++;

					if (index == length)
					{
						break;
					} 
				}
				exp.value = sb.ToString();
				
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
						exp.value = "||";
						exp.type = TokenTypes.Or;
						index += 2;
						break;
					case '=':
						exp.value = "==";
						exp.type = TokenTypes.Equals;
						index += 2;
						break;
					case '(':
						exp.type = TokenTypes.LParen;
						index++;
						break;
					case ')':
						exp.type = TokenTypes.RParen;
						index++;
						break;
				}
			}

			return exp;
		}

		private void ParseFunction()
		{
			SkipWhitespace();
			int argCount = 1;

			int startIndex = index + 1; 
			int i2 = startIndex;
			while (chars[i2] != ')')
			{
				if (chars[i2] == ',')
				{
					argCount++;
				}
				i2++;
			}

			int len = i2 - startIndex;

			index += len;

			if (index < length)
			{
				index += 1; // skip the ) char 
			}

			string args = new string(chars, startIndex, len);

			string[] split = args.Split(new char[] {' ', ','}, StringSplitOptions.RemoveEmptyEntries); // remove spaces from the strings
			int sLength = split.Length;

			if (split[0] == psImageMode)
			{
				for (int i = 1; i < sLength; i++)
				{
					if (split[i] == imageMode)
					{
						this.supports16Bit = true;
					}
				}
			}
		}

		private void ParseOr()
		{
			Expression keyword = this.NextToken();

			if (keyword.value == psImageDepth)
			{
				Expression equal = this.NextToken();

#if DEBUG
				System.Diagnostics.Debug.Assert(equal.type == TokenTypes.Equals);
#endif
				
				Expression depth = this.NextToken();

				this.supports16Bit = depth.intValue == 16; 
				
			}
		
		}
	}
}
