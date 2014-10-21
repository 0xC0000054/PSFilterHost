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

namespace PSFilterLoad.PSApi
{
// Disable CS0649, Field 'field' is never assigned to, and will always have its default value 'value'
#pragma warning disable 0649 

	internal struct PIProperty
	{
		public uint vendorID;  /* Vendor specific identifier. */
		public uint propertyKey;		/* Identification key for this property type. */
		public int propertyID;		/* Index within this property type. Must be unique for properties of a given type in a PiPL. */
		public int propertyLength;	/* Length of following data array. Will be rounded to a multiple of 4. */

		public const int SizeOf = 16; 
	}

	internal struct PITerminology
	{
		public int version;
		public uint classID;
		public uint eventID;
		public short terminologyID;

		public const int SizeOf = 14;
	}
#pragma warning restore 0649

	internal static class PIPropertyID
	{
		/// <summary>
		/// The property giving the plug-in's kind; 8BFM for filters - 'kind'
		/// </summary>
		public const uint PIKindProperty = 0x6b696e64U;
		/// <summary>
		/// Win32 Intel code descriptor; Entrypoint - 'wx86'
		/// </summary>
		public const uint PIWin32X86CodeProperty = 0x77783836U;
		/// <summary>
		/// Win64 Intel code descriptor; Entrypoint - '8664'
		/// </summary>
		public const uint PIWin64X86CodeProperty = 0x38363634U;
		/// <summary>
		/// Major(int16).Minor(int16) version number - 'vers'
		/// </summary>
		public const uint PIVersionProperty = 0x76657273U;
		/// <summary>
		/// Image modes supported flags - 'mode' 
		/// </summary>
		public const uint PIImageModesProperty = 0x6d6f6465U;
		/// <summary>
		/// Category name that appears on top level menu - 'catg'
		/// </summary>
		public const uint PICategoryProperty = 0x63617467U;
		/// <summary>
		/// Menu name - 'name' 
		/// </summary>
		public const uint PINameProperty = 0x6e616d65U;
		/// <summary>
		/// Has Terminology Property - 'hstm'
		/// </summary>
		public const uint PIHasTerminologyProperty = 0x6873746dU;
		/// <summary>
		/// FilterCaseInfo Property - 'fici'
		/// </summary>
		public const uint PIFilterCaseInfoProperty = 0x66696369U;
		/// <summary>
		/// EnableInfo property - 'enbl'
		/// </summary>
		public const uint EnableInfo = 0x656e626cU;
		/// <summary>
		/// Creator code of required host, such as '8BIM' for Adobe Photoshop. - 'host' 
		/// </summary>
		public const uint PIRequiredHostProperty = 0x686f7374U;
		/// <summary>
		/// The property indicating the filter does not have an about box - 'nabo'
		/// </summary>
		public const uint NoAboutBox = 0x6e61626fU;
	}
	
}
