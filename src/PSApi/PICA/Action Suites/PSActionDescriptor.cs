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

/* Adapted from PIActions.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.PSApi.PICA
{
    internal struct PIActionDescriptor : IEquatable<PIActionDescriptor>
    {
        private readonly IntPtr value;

        public static readonly PIActionDescriptor Null = new PIActionDescriptor(0);

        public PIActionDescriptor(int index)
        {
            value = new IntPtr(index);
        }

        public int Index => value.ToInt32();

        public override bool Equals(object obj)
        {
            return obj is PIActionDescriptor other && Equals(other);
        }

        public bool Equals(PIActionDescriptor other)
        {
            return value == other.value;
        }

        public override int GetHashCode()
        {
            return -1584136870 + value.GetHashCode();
        }

        public static bool operator ==(PIActionDescriptor left, PIActionDescriptor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PIActionDescriptor left, PIActionDescriptor right)
        {
            return !left.Equals(right);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorMake(PIActionDescriptor* descriptor);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorFree(PIActionDescriptor descriptor);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetType(PIActionDescriptor descriptor, uint key, uint* type);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetKey(PIActionDescriptor descriptor, uint index, uint* key);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorHasKey(PIActionDescriptor descriptor, uint key, byte* hasKey);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetCount(PIActionDescriptor descriptor, uint* count);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorIsEqual(PIActionDescriptor descriptor, PIActionDescriptor other, byte* isEqual);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorErase(PIActionDescriptor descriptor, uint key);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorClear(PIActionDescriptor descriptor);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutInteger(PIActionDescriptor descriptor, uint key, int value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutFloat(PIActionDescriptor descriptor, uint key, double value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutUnitFloat(PIActionDescriptor descriptor, uint key, uint unit, double value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutString(PIActionDescriptor descriptor, uint key, IntPtr cstrValue);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutBoolean(PIActionDescriptor descriptor, uint key, byte value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutList(PIActionDescriptor descriptor, uint key, PIActionList value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutObject(PIActionDescriptor descriptor, uint key, uint type, PIActionDescriptor value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutGlobalObject(PIActionDescriptor descriptor, uint key, uint type, PIActionDescriptor value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutEnumerated(PIActionDescriptor descriptor, uint key, uint type, uint value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutReference(PIActionDescriptor descriptor, uint key, PIActionReference value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutClass(PIActionDescriptor descriptor, uint key, uint value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutGlobalClass(PIActionDescriptor descriptor, uint key, uint value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutAlias(PIActionDescriptor descriptor, uint key, IntPtr value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetInteger(PIActionDescriptor descriptor, uint key, int* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetFloat(PIActionDescriptor descriptor, uint key, double* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetUnitFloat(PIActionDescriptor descriptor, uint key, uint* unit, double* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetStringLength(PIActionDescriptor descriptor, uint key, uint* stringLength);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetString(PIActionDescriptor descriptor, uint key, IntPtr cstrValue, uint maxLength);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetBoolean(PIActionDescriptor descriptor, uint key, byte* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetList(PIActionDescriptor descriptor, uint key, PIActionList* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetObject(PIActionDescriptor descriptor, uint key, uint* type, PIActionDescriptor* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetGlobalObject(PIActionDescriptor descriptor, uint key, uint* type, PIActionDescriptor* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetEnumerated(PIActionDescriptor descriptor, uint key, uint* type, uint* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetReference(PIActionDescriptor descriptor, uint key, PIActionReference* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetClass(PIActionDescriptor descriptor, uint key, uint* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetGlobalClass(PIActionDescriptor descriptor, uint key, uint* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetAlias(PIActionDescriptor descriptor, uint key, IntPtr* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorHasKeys(PIActionDescriptor descriptor, IntPtr requiredKeys, byte* hasKeys);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutIntegers(PIActionDescriptor descriptor, uint key, uint count, IntPtr value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetIntegers(PIActionDescriptor descriptor, uint key, uint count, IntPtr value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorAsHandle(PIActionDescriptor descriptor, IntPtr* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorHandleToDescriptor(IntPtr value, PIActionDescriptor* descriptor);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutZString(PIActionDescriptor descriptor, uint key, ASZString zstring);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetZString(PIActionDescriptor descriptor, uint key, ASZString* zstring);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutData(PIActionDescriptor descriptor, uint key, int length, IntPtr value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetDataLength(PIActionDescriptor descriptor, uint key, int* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetData(PIActionDescriptor descriptor, uint key, IntPtr value);

#pragma warning disable 108
    [StructLayout(LayoutKind.Sequential)]
    internal struct PSActionDescriptorProc
    {
        public IntPtr Make;
        public IntPtr Free;
        public IntPtr GetType;
        public IntPtr GetKey;
        public IntPtr HasKey;
        public IntPtr GetCount;
        public IntPtr IsEqual;
        public IntPtr Erase;
        public IntPtr Clear;
        public IntPtr PutInteger;
        public IntPtr PutFloat;
        public IntPtr PutUnitFloat;
        public IntPtr PutString;
        public IntPtr PutBoolean;
        public IntPtr PutList;
        public IntPtr PutObject;
        public IntPtr PutGlobalObject;
        public IntPtr PutEnumerated;
        public IntPtr PutReference;
        public IntPtr PutClass;
        public IntPtr PutGlobalClass;
        public IntPtr PutAlias;
        public IntPtr GetInteger;
        public IntPtr GetFloat;
        public IntPtr GetUnitFloat;
        public IntPtr GetStringLength;
        public IntPtr GetString;
        public IntPtr GetBoolean;
        public IntPtr GetList;
        public IntPtr GetObject;
        public IntPtr GetGlobalObject;
        public IntPtr GetEnumerated;
        public IntPtr GetReference;
        public IntPtr GetClass;
        public IntPtr GetGlobalClass;
        public IntPtr GetAlias;
        public IntPtr HasKeys;
        public IntPtr PutIntegers;
        public IntPtr GetIntegers;
        public IntPtr AsHandle;
        public IntPtr HandleToDescriptor;
        public IntPtr PutZString;
        public IntPtr GetZString;
        public IntPtr PutData;
        public IntPtr GetDataLength;
        public IntPtr GetData;
    }
#pragma warning restore 108
}
