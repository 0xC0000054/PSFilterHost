/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/* Adapted from PIGeneral.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.PSApi
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct PixelMemoryDesc
	{
		public IntPtr data;
		public int rowBits;
		public int colBits;
		public int bitOffset;
		public int depth;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct PSScaling
	{
		public VRect sourceRect;
		public VRect destinationRect;
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate short ReadPixelsProc([In()] IntPtr port, [In()] ref PSScaling scaling, [In()] ref VRect writeRect, [In()] ref PixelMemoryDesc destination, ref VRect wroteRect);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate short WriteBasePixelsProc([In()] IntPtr port, [In()] ref VRect writeRect, [In()] PixelMemoryDesc source);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate short ReadPortForWritePortProc(ref IntPtr readPort, [In()] IntPtr writePort);

	[StructLayout(LayoutKind.Sequential)]
	internal struct ChannelPortProcs
	{
		public short channelPortProcsVersion;
		public short numChannelPortProcs;
		public IntPtr readPixelsProc;
		public IntPtr writeBasePixelsProc;
		public IntPtr readPortForWritePortProc;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct ReadChannelDesc
	{
		public int minVersion;		// The minimum and maximum version which 
		public int maxVersion;		// can be used to interpret this record. 

		public IntPtr next;	// The next descriptor in the list. 

		public IntPtr port;	// The port to use for reading. 

		public VRect bounds;			// The bounds of the channel data. 
		public int depth;			// The depth of the data.

		public VPoint tileSize;		// The size of the tiles. 
		public VPoint tileOrigin;		// The origin for the tiles. 

		public byte target; // Is this a target channel?

		public byte shown; // Is this channel shown?

		public ChannelTypes channelType;		// The channel type.

		public short padding;			// Reserved. Defaults to zero. 

		public IntPtr contextInfo;		// A pointer to additional info dependent on context.

		public IntPtr name;		// The name of the channel.
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct WriteChannelDesc
	{
		public int minVersion;		// The minimum and maximum version which 
		public int maxVersion;		// can be used to interpret this record. 
	
		public IntPtr next;	// The next descriptor in the list. 
	
		public IntPtr port;	// The port to use for reading.
	
		public VRect bounds;			// The bounds of the channel data. 
		public int depth;			// The depth of the data. 
	
		public VPoint tileSize;		// The size of the tiles. 
		public VPoint tileOrigin;		// The origin for the tiles. 
	
		public ChannelTypes channelType;		// The channel type. 

		public short padding;			// Reserved. Defaults to zero.
	
		public IntPtr contextInfo;		// A pointer to additional info dependent on context. 
	
		public IntPtr name;		// The name of the channel. 
	
	}

	internal enum ChannelTypes : short
	{
		Unspecified = 0,
		Red = 1,
		Green = 2,
		Blue = 3,
		Cyan = 4,
		Magenta = 5,
		Yellow = 6,
		Black = 7,
		L = 8,
		A = 9,
		B = 10,
		Duotone = 11,
		Index = 12,
		Bitmap = 13,
		ColorSelected = 14,
		ColorProtected = 15,
		Transparency = 16,
		LayerMask = 17,
		InvertedLayerMask = 18,
		SelectionMask = 19
	}

}
