﻿/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/* Adapted from ASZStringSuite.h, PIBufferSuite.h, PIColorSpaceSuite.h, PIHandleSuite.h, PIUIHooskSuite.h, SPPlugs.h
*  Copyright 1986 - 2000 Adobe Systems Incorporated
*  All Rights Reserved
*/

using System;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.PSApi.PICA
{
    #region BufferSuite Delegates
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate IntPtr PSBufferSuiteNew(uint* requestedSize, uint minimumSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate void PSBufferSuiteDispose(IntPtr* buffer);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate uint PSBufferSuiteGetSize(IntPtr buffer);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate uint PSBufferSuiteGetSpace();
    #endregion

    #region ColorSpace Delegates
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int CSMake(ColorID* colorID);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int CSDelete(ColorID* colorID);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSStuffComponents(ColorID colorID, ColorSpace colorSpace, byte c0, byte c1, byte c2, byte c3);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int CSExtractComponents(ColorID colorID, ColorSpace colorSpace, byte* c0, byte* c1, byte* c2, byte* c3, byte* gamutFlag);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSStuffXYZ(ColorID colorID, CS_XYZ xyz);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int CSExtractXYZ(ColorID colorID, CS_XYZ* xyz);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSConvert8(ColorSpace inputCSpace, ColorSpace outputCSpace, IntPtr colorArray, short count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSConvert16(ColorSpace inputCSpace, ColorSpace outputCSpace, IntPtr colorArray, short count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int CSGetNativeSpace(ColorID colorID, ColorSpace* nativeSpace);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int CSIsBookColor(ColorID colorID, byte* isBookColor);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int CSExtractColorName(ColorID colorID, ASZString* colorName);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int CSPickColor(ColorID* colorID, ASZString promptString);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSConvert(IntPtr inputData, IntPtr outputData, short count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSConvertToMonitorRGB(ColorSpace inputCSpace, IntPtr inputData, IntPtr outputData, short count);
    #endregion

    #region HandleSuite Delegates
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate void SetPIHandleLockDelegate(IntPtr handle, byte lockHandle, IntPtr* address, byte* oldLock);
    #endregion

    #region UIHooks Delegates
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr UISuiteMainWindowHandle();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int UISuiteHostSetCursor(IntPtr cursor);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate uint UISuiteHostTickCount();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int UISuiteGetPluginName(IntPtr plugInRef, ASZString* plugInName);
    #endregion

    #region ASZStringSuite Delegates
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ASZStringMakeFromUnicode(IntPtr src, UIntPtr byteCount, ASZString* newZString);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ASZStringMakeFromCString(IntPtr src, UIntPtr byteCount, ASZString* newZString);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ASZStringMakeFromPascalString(IntPtr src, UIntPtr byteCount, ASZString* newZString);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ASZStringMakeRomanizationOfInteger(int value, ASZString* newZString);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ASZStringMakeRomanizationOfFixed(int value,
                                                                  short places,
                                                                  [MarshalAs(UnmanagedType.Bool)] bool trim,
                                                                  [MarshalAs(UnmanagedType.Bool)] bool isSigned,
                                                                  ASZString* newZString);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ASZStringMakeRomanizationOfDouble(double value, ASZString* newZString);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate ASZString ASZStringGetEmpty();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ASZStringCopy(ASZString source, ASZString* copy);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringReplace(ASZString zstr, uint index, ASZString replacement);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringTrimEllipsis(ASZString zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringTrimSpaces(ASZString zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringRemoveAccelerators(ASZString zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringAddRef(ASZString zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringRelease(ASZString zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal delegate bool ASZStringIsAllWhiteSpace(ASZString zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal delegate bool ASZStringIsEmpty(ASZString zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal delegate bool ASZStringWillReplace(ASZString zstr, uint index);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate uint ASZStringLengthAsUnicodeCString(ASZString zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringAsUnicodeCString(ASZString zstr, IntPtr str, uint strSize, [MarshalAs(UnmanagedType.Bool)] bool checkStrSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate uint ASZStringLengthAsCString(ASZString zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringAsCString(ASZString zstr, IntPtr str, uint strSize, [MarshalAs(UnmanagedType.Bool)] bool checkStrSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate uint ASZStringLengthAsPascalString(ASZString zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringAsPascalString(ASZString zstr, IntPtr str, uint strBufferSize, [MarshalAs(UnmanagedType.Bool)] bool checkBufferSize);
    #endregion
}
