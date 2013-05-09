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

/* Adapted from PIActions.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/


using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate IntPtr OpenWriteDescriptorProc();

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short CloseWriteDescriptorProc(System.IntPtr param0, ref System.IntPtr param1);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutIntegerProc([In()]System.IntPtr param0, [In()]uint param1, [In()]int param2);
   
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutFloatProc(System.IntPtr param0, uint param1, ref double param2);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutUnitFloatProc([In()]System.IntPtr param0, uint param1, uint param2, ref double param3);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutBooleanProc([In()]System.IntPtr param0, uint param1, [In()]byte param2);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutTextProc([In()]System.IntPtr param0, [In()]uint param1, [In(), MarshalAs(UnmanagedType.SysInt)]IntPtr param2);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutAliasProc(System.IntPtr param0, uint param1, [In()]System.IntPtr param2);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutEnumeratedProc(System.IntPtr param0, uint key, uint type, uint value);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutClassProc(System.IntPtr param0, uint param1, uint param2);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutSimpleReferenceProc(System.IntPtr param0, uint param1, ref PIDescriptorSimpleReference param2);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutObjectProc(System.IntPtr param0, uint param1, uint param2, System.IntPtr param3);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutCountProc(System.IntPtr param0, uint param1, uint count);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutStringProc(System.IntPtr param0, uint param1, IntPtr param2);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutScopedClassProc(System.IntPtr param0, uint param1, uint param2);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutScopedObjectProc(System.IntPtr param0, uint param1, uint param2, ref System.IntPtr param3);

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct PIDescriptorSimpleReference__keyData
    {
        public fixed byte name[256];
        public int index;
        public uint type;
        public uint value;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PIDescriptorSimpleReference
    {
        public uint desiredClass;
        public uint keyForm;
        public PIDescriptorSimpleReference__keyData Struct1;
    }

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate IntPtr OpenReadDescriptorProc(System.IntPtr param0, IntPtr param1);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short CloseReadDescriptorProc(System.IntPtr param0);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate byte GetKeyProc(System.IntPtr param0, ref uint key, ref uint type, ref int flags);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetIntegerProc(System.IntPtr param0, ref int param1);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetFloatProc(System.IntPtr param0, ref double param1);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetUnitFloatProc(System.IntPtr param0, ref uint param1, ref double param2);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetBooleanProc(System.IntPtr param0, ref byte param1);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetTextProc(System.IntPtr param0, ref System.IntPtr param1);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetAliasProc(System.IntPtr param0, ref System.IntPtr param1);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetEnumeratedProc(System.IntPtr param0, ref uint param1);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetClassProc(System.IntPtr param0, ref uint param1);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetSimpleReferenceProc(System.IntPtr param0, ref PIDescriptorSimpleReference param1);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetObjectProc(System.IntPtr param0, ref uint param1, ref System.IntPtr param2);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetCountProc(System.IntPtr param0, ref uint param1);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetStringProc(System.IntPtr param0, System.IntPtr param1);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetPinnedIntegerProc(System.IntPtr param0, int param1, int param2, ref int param3);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetPinnedFloatProc(System.IntPtr param0, ref double param1, ref double param2, ref double param3);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetPinnedUnitFloatProc(System.IntPtr param0, ref double param1, ref double param2, ref uint param3, ref double param4);

    [StructLayout(LayoutKind.Sequential)]
    internal struct WriteDescriptorProcs
    {
        public short writeDescriptorProcsVersion;
        public short numWriteDescriptorProcs;

        public IntPtr openWriteDescriptorProc;
        public IntPtr closeWriteDescriptorProc;
        public IntPtr putIntegerProc;
        public IntPtr putFloatProc;
        public IntPtr putUnitFloatProc;
        public IntPtr putBooleanProc;
        public IntPtr putTextProc;
        public IntPtr putAliasProc;
        public IntPtr putEnumeratedProc;
        public IntPtr putClassProc;
        public IntPtr putSimpleReferenceProc;
        public IntPtr putObjectProc;
        public IntPtr putCountProc;
        public IntPtr putStringProc;
        public IntPtr putScopedClassProc;
        public IntPtr putScopedObjectProc;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ReadDescriptorProcs
    {
        public short readDescriptorProcsVersion;
        public short numReadDescriptorProcs;
        
        public IntPtr openReadDescriptorProc;
        public IntPtr closeReadDescriptorProc;
        public IntPtr getKeyProc;
        public IntPtr getIntegerProc;
        public IntPtr getFloatProc;
        public IntPtr getUnitFloatProc;
        public IntPtr getBooleanProc;
        public IntPtr getTextProc;
        public IntPtr getAliasProc;
        public IntPtr getEnumeratedProc;
        public IntPtr getClassProc;
        public IntPtr getSimpleReferenceProc;
        public IntPtr getObjectProc;
        public IntPtr getCountProc;
        public IntPtr getStringProc;
        public IntPtr getPinnedIntegerProc;
        public IntPtr getPinnedFloatProc;
        public IntPtr getPinnedUnitFloatProc;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PIDescriptorParameters
    {
        public short descriptorParametersVersion;
        public short playInfo;
        public short recordInfo;
        public IntPtr descriptor;
        public IntPtr writeDescriptorProcs;
        public IntPtr readDescriptorProcs;
    }


    internal static class RecordInfo
    {
        public const short plugInDialogOptional = 0;
        public const short plugInDialogRequired = 1;
        public const short plugInDialogNone = 2;
    }

    internal static class PlayInfo 
    {
        public const short plugInDialogDontDisplay = 0;
        public const short plugInDialogDisplay = 1;
        public const short plugInDialogSilent = 2;
    }



}
