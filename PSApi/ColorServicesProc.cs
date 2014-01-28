/////////////////////////////////////////////////////////////////////////////////
//
// Adobe(R) Photoshop(R) filter host for .NET
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

using System.Runtime.InteropServices;
namespace PSFilterLoad.PSApi
{
    [StructLayoutAttribute(LayoutKind.Explicit)]
    internal struct SelectorParameters
    {
        [FieldOffsetAttribute(0)]
        public System.IntPtr pickerPrompt;
        [FieldOffsetAttribute(0)]
        public System.IntPtr globalSamplePoint;
        [FieldOffsetAttribute(0)]
        public int specialColorID;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short ColorServicesProc(ref ColorServicesInfo info);

    [StructLayoutAttribute(LayoutKind.Sequential)]
    internal unsafe struct ColorServicesInfo
    {
        public int infoSize;
        public short selector;
        public short sourceSpace;
        public short resultSpace;
        public byte resultGamutInfoValid;
        public byte resultInGamut;
        public System.IntPtr reservedSourceSpaceInfo;
        public System.IntPtr reservedResultSpaceInfo;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.I2, SizeConst = 4)]
        public short[] colorComponents;
        public System.IntPtr reserved;
        public SelectorParameters selectorParameter;
    }

}
