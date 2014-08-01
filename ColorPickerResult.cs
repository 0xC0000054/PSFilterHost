/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2014 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

namespace PSFilterHostDll
{
	/// <summary>
	/// The class that holds the color selected from the host application.
	/// </summary>
	/// <remarks>
	/// <para>The RGB values are initially populated with the default color that the filter wants selected in the host's color dialog.</para>
	/// <para>When the user closes the host's color dialog, the host will set the RGB values to the user's selected color.</para>
	/// </remarks>
	/// <threadsafety static="true" instance="false" />
	public sealed class ColorPickerResult
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ColorPickerResult"/> class.
		/// </summary>
		/// <param name="red">The red component of the initial color.</param>
		/// <param name="green">The green component of the initial color.</param>
		/// <param name="blue">The blue component of the initial color.</param>
		internal ColorPickerResult(short red, short green, short blue)
		{
			this.R = (byte)red;
			this.G = (byte)green;
			this.B = (byte)blue;
		}

		/// <summary>
		/// Gets or sets the Red component of the color.
		/// </summary>
		/// <value>
		/// The Red component of the color.
		/// </value>
		public byte R
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the Green component of the color.
		/// </summary>
		/// <value>
		/// The Green component of the color.
		/// </value>
		public byte G
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the Blue component of the color.
		/// </summary>
		/// <value>
		/// The Blue component of the color.
		/// </value>
		public byte B
		{
			get;
			set;
		}
	}
}
