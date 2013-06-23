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
    internal delegate short CloseWriteDescriptorProc(IntPtr descriptor, ref IntPtr descriptorHandle);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutIntegerProc([In()]IntPtr descriptor, [In()]uint key, [In()]int data);
   
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutFloatProc([In()]IntPtr descriptor, uint key, ref double param2);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutUnitFloatProc([In()]IntPtr descriptor, [In()]uint param1, [In()]uint param2, [In()]ref double param3);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutBooleanProc([In()]IntPtr descriptor, [In()]uint param1, [In()]byte param2);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutTextProc([In()]IntPtr descriptor, [In()]uint param1, [In(), MarshalAs(UnmanagedType.SysInt)]IntPtr param2);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutAliasProc([In()]IntPtr descriptor, [In()]uint param1, [In()]IntPtr param2);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutEnumeratedProc([In()]IntPtr descriptor, [In()]uint key, [In()]uint type, [In()]uint value);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutClassProc([In()]IntPtr descriptor, [In()]uint param1, [In()]uint param2);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutSimpleReferenceProc([In()]IntPtr descriptor, [In()]uint param1, [In()]PIDescriptorSimpleReference param2);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutObjectProc([In()]IntPtr descriptor, [In()]uint key, [In()]uint param2, [In()]IntPtr param3);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutCountProc([In()]IntPtr descriptor, [In()]uint param1, [In()]uint count);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutStringProc([In()]IntPtr descriptor, [In()]uint param1, [In()]IntPtr param2);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutScopedClassProc([In()]IntPtr descriptor, [In()]uint param1, [In()]uint param2);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutScopedObjectProc([In()]IntPtr descriptor, [In()]uint param1, [In()]uint param2, [In()]IntPtr param3);

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
    internal delegate IntPtr OpenReadDescriptorProc(IntPtr descriptor, IntPtr param1);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short CloseReadDescriptorProc(IntPtr descriptor);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate byte GetKeyProc(IntPtr descriptor, ref uint key, ref uint type, ref int flags);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetIntegerProc(IntPtr descriptor, ref int param1);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetFloatProc(IntPtr descriptor, ref double param1);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetUnitFloatProc(IntPtr descriptor, ref uint param1, ref double param2);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetBooleanProc(IntPtr descriptor, ref byte param1);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetTextProc(IntPtr descriptor, ref IntPtr param1);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetAliasProc(IntPtr descriptor, ref IntPtr param1);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetEnumeratedProc(IntPtr descriptor, ref uint param1);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetClassProc(IntPtr descriptor, ref uint param1);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetSimpleReferenceProc(IntPtr descriptor, ref PIDescriptorSimpleReference param1);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetObjectProc(IntPtr descriptor, ref uint param1, ref IntPtr param2);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetCountProc(IntPtr descriptor, ref uint param1);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetStringProc(IntPtr descriptor, IntPtr param1);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetPinnedIntegerProc(IntPtr descriptor, int param1, int param2, ref int param3);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetPinnedFloatProc(IntPtr descriptor, ref double param1, ref double param2, ref double param3);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetPinnedUnitFloatProc(IntPtr descriptor, ref double param1, ref double param2, ref uint param3, ref double param4);

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
        public PlayInfo playInfo;
        public RecordInfo recordInfo;
        public IntPtr descriptor;
        public IntPtr writeDescriptorProcs;
        public IntPtr readDescriptorProcs;
    }

    internal enum RecordInfo : short
    {
        plugInDialogOptional = 0,
        plugInDialogRequired = 1,
        plugInDialogNone = 2
    }

    internal enum PlayInfo : short
    {
        plugInDialogDontDisplay = 0,
        plugInDialogDisplay = 1,
        plugInDialogSilent = 2
    }



}
