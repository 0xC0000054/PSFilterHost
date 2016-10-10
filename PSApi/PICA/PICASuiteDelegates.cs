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

/* Adapted from PIBufferSuite.h, PIColorSpaceSuite.h, PIHandleSuite.h, PIUIHooskSuite.h, SPPlugs.h
*  Copyright 1986 - 2000 Adobe Systems Incorporated              
*  All Rights Reserved
*/

using System;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.PSApi.PICA
{
	#region BufferSuite Delegates
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate IntPtr PSBufferSuiteNew(ref uint requestedSize, uint minimumSize);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate void PSBufferSuiteDispose(ref IntPtr buffer);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate uint PSBufferSuiteGetSize(IntPtr buffer);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate uint PSBufferSuiteGetSpace();
	#endregion

#if PICASUITEDEBUG
	#region ColorSpace Delegates
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate int CSMake(IntPtr colorID);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate int CSDelete(IntPtr colorID);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate int CSStuffComponents(IntPtr colorID, short colorSpace, ref byte c0, ref byte c1, ref byte c2, ref byte c3);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate int CSExtractComponents(IntPtr colorID, short colorSpace, ref byte c0, ref byte c1, ref byte c2, ref byte c3, ref byte gamutFlag);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate int CSStuffXYZ(IntPtr colorID, CS_XYZ xyz);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate int CSExtractXYZ(IntPtr colorID, ref CS_XYZ xyz);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate int CSConvert8(ColorSpace inputCSpace, ColorSpace outputCSpace, IntPtr colorArray, short count);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate int CSConvert16(short inputCSpace, short outputCSpace, IntPtr colorArray, short count);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate int CSGetNativeSpace(IntPtr colorID, ref short nativeSpace);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate int CSIsBookColor(IntPtr colorID, ref byte isBookColor);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate int CSExtractColorName(IntPtr colorID, ref IntPtr colorName);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate int CSPickColor(IntPtr colorID, IntPtr promptString);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate int CSConvert(IntPtr inputData, IntPtr outputData, short count);
	#endregion
#endif

	#region HandleSuite Delegates
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate void SetPIHandleLockDelegate(IntPtr handle, byte lockHandle, ref IntPtr address, ref byte oldLock);
	#endregion

	#region UIHooks Delegates
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate IntPtr UISuiteMainWindowHandle();
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate int UISuiteHostSetCursor(IntPtr cursor);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate uint UISuiteHostTickCount();
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate int UISuiteGetPluginName(IntPtr plugInRef, ref IntPtr plugInName);
	#endregion

#if PICASUITEDEBUG
	#region SPPlugin Delegates
	internal delegate int SPAllocatePluginList(IntPtr strings, ref IntPtr pluginList);
	internal delegate int SPFreePluginList(ref IntPtr pluginList);
	internal delegate int SPGetPluginListNeededSuiteAvailable(IntPtr pluginList, ref int available);

	internal delegate int SPAddPlugin(IntPtr pluginList, IntPtr fileSpec, IntPtr PiPL, IntPtr adapterName, IntPtr adapterInfo, IntPtr plugin);

	internal delegate int SPNewPluginListIterator(IntPtr pluginList, ref IntPtr iter);
	internal delegate int SPNextPlugin(IntPtr iter, ref IntPtr plugin);
	internal delegate int SPDeletePluginListIterator(IntPtr iter);

	internal delegate int SPGetHostPluginEntry(IntPtr plugin, ref IntPtr host);
	internal delegate int SPGetPluginFileSpecification(IntPtr plugin, ref IntPtr fileSpec);
	internal delegate int SPGetPluginPropertyList(IntPtr plugin, ref IntPtr propertyList);
	internal delegate int SPGetPluginGlobals(IntPtr plugin, ref IntPtr globals);
	internal delegate int SPSetPluginGlobals(IntPtr plugin, IntPtr globals);
	internal delegate int SPGetPluginStarted(IntPtr plugin, ref int started);
	internal delegate int SPSetPluginStarted(IntPtr plugin, long started);
	internal delegate int SPGetPluginSkipShutdown(IntPtr plugin, ref int skipShutdown);
	internal delegate int SPSetPluginSkipShutdown(IntPtr plugin, long skipShutdown);
	internal delegate int SPGetPluginBroken(IntPtr plugin, ref int broken);
	internal delegate int SPSetPluginBroken(IntPtr plugin, long broken);
	internal delegate int SPGetPluginAdapter(IntPtr plugin, ref IntPtr adapter);
	internal delegate int SPGetPluginAdapterInfo(IntPtr plugin, ref IntPtr adapterInfo);
	internal delegate int SPSetPluginAdapterInfo(IntPtr plugin, IntPtr adapterInfo);

	internal delegate int SPFindPluginProperty(IntPtr plugin, uint vendorID, uint propertyKey, long propertyID, ref IntPtr p);

	internal delegate int SPGetPluginName(IntPtr plugin, ref IntPtr name);
	internal delegate int SPSetPluginName(IntPtr plugin, IntPtr name);
	internal delegate int SPGetNamedPlugin(IntPtr name, ref IntPtr plugin);

	internal delegate int SPSetPluginPropertyList(IntPtr plugin, IntPtr file);
	#endregion
#endif
}
