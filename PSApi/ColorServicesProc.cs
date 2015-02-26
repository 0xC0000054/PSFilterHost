/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2015 Nicholas Hayes
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
    internal enum SpecialColorID : int
    {
        ForegroundColor = 0,
        BackgroundColor = 1
    }

    internal enum ColorServicesSelector : short
    {
        ChooseColor = 0,
        ConvertColor = 1,
        SamplePoint = 2,
        GetSpecialColor = 3
    }

    internal enum ColorSpace : short
    {
        ChosenSpace = -1,
        RGBSpace = 0,
        HSBSpace = 1,
        CMYKSpace = 2,
        LabSpace = 3,
        GraySpace = 4,
        HSLSpace = 5,
        XYZSpace = 6
    } 

    [StructLayout(LayoutKind.Explicit)]
    internal struct SelectorParameters
    {
        [FieldOffset(0)]
        public IntPtr pickerPrompt;
        [FieldOffset(0)]
        public IntPtr globalSamplePoint;
        [FieldOffset(0)]
        public SpecialColorID specialColorID;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short ColorServicesProc(ref ColorServicesInfo info);

    [StructLayout(LayoutKind.Sequential)]
    internal struct ColorServicesInfo
    {
        public int infoSize;
        public ColorServicesSelector selector;
        public ColorSpace sourceSpace;
        public ColorSpace resultSpace;
        public byte resultGamutInfoValid;
        public byte resultInGamut;
        public IntPtr reservedSourceSpaceInfo;
        public IntPtr reservedResultSpaceInfo;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.I2, SizeConst = 4)]
        public short[] colorComponents;
        public IntPtr reserved;
        public SelectorParameters selectorParameter;
    }

}
