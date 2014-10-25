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

/* Adapted from PIGeneral.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
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

	[UnmanagedFunctionPointer(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
	internal delegate short ReadPixelsProc([In()] IntPtr port, [In()] ref PSScaling scaling, [In()] ref VRect writeRect, [In()] ref PixelMemoryDesc destination, ref VRect wroteRect);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
	internal delegate short WriteBasePixelsProc([In()] IntPtr port, [In()] ref VRect writeRect, [In()] PixelMemoryDesc source);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
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

		public short channelType;		// The channel type.

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
	
		public short channelType;		// The channel type. 

		public short padding;			// Reserved. Defaults to zero.
	
		public IntPtr contextInfo;		// A pointer to additional info dependent on context. 
	
		public IntPtr name;		// The name of the channel. 
	
	}

	internal static class ChannelTypes
	{
		public const short ctUnspecified = 0;
		public const short ctRed = 1;
		public const short ctGreen = 2;
		public const short ctBlue = 3;
		public const short ctCyan = 4;
		public const short ctMagenta = 5;
		public const short ctYellow = 6;
		public const short ctBlack = 7;
		public const short ctL = 8;
		public const short ctA = 9;
		public const short ctB = 10;
		public const short ctDuotone = 11;
		public const short ctIndex = 12;
		public const short ctBitmap = 13;
		public const short ctColorSelected = 14;
		public const short ctColorProtected = 15;
		public const short ctTransparency = 16;
		public const short ctLayerMask = 17;
		public const short ctInvertedLayerMask = 18;
		public const short ctSelectionMask = 19;
	}

}
