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

namespace PSFilterHostDll
{
	/// <summary>
	/// Represents the color selected from the host application.
	/// </summary>
	/// <threadsafety static="true" instance="false" />
	public sealed class ColorPickerResult
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ColorPickerResult"/> class from the specified red, green and blue components.
		/// </summary>
		/// <param name="red">The red component of the user's chosen color.</param>
		/// <param name="green">The green component of the user's chosen color.</param>
		/// <param name="blue">The blue component of the user's chosen color.</param>
		/// <overloads>Initializes a new instance of the <see cref="ColorPickerResult"/> class.</overloads>
		public ColorPickerResult(byte red, byte green, byte blue)
		{
			this.R = red;
			this.G = green;
			this.B = blue;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ColorPickerResult"/> class from the specified <see cref="System.Drawing.Color"/>.
		/// </summary>
		/// <param name="color">The user's chosen color.</param>
		public ColorPickerResult(System.Drawing.Color color) : this(color.R, color.G, color.B)
		{
		}

#if !GDIPLUS
		/// <summary>
		/// Initializes a new instance of the <see cref="ColorPickerResult"/> class from the specified <see cref="System.Windows.Media.Color"/>
		/// </summary>
		/// <param name="color">The user's chosen color.</param>
		public ColorPickerResult(System.Windows.Media.Color color) : this(color.R, color.G, color.B)
		{
		}
#endif

		/// <summary>
		/// Gets the Red component of the color.
		/// </summary>
		/// <value>
		/// The Red component of the color.
		/// </value>
		public byte R
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the Green component of the color.
		/// </summary>
		/// <value>
		/// The Green component of the color.
		/// </value>
		public byte G
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the Blue component of the color.
		/// </summary>
		/// <value>
		/// The Blue component of the color.
		/// </value>
		public byte B
		{
			get;
			private set;
		}
	}
}
