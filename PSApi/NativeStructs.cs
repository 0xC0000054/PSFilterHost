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

// Portions adapted from:
/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See License-pdn.txt for full licensing and attribution details.             //
//                                                                             //
/////////////////////////////////////////////////////////////////////////////////
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    static class NativeStructs
    {
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        internal struct MEMORY_BASIC_INFORMATION
        {

            /// PVOID->void*
            public System.IntPtr BaseAddress;

            /// PVOID->void*
            public System.IntPtr AllocationBase;

            /// DWORD->unsigned int
            public uint AllocationProtect;

            /// SIZE_T->ULONG_PTR->unsigned int
            public System.UIntPtr RegionSize;

            /// DWORD->unsigned int
            public uint State;

            /// DWORD->unsigned int
            public uint Protect;

            /// DWORD->unsigned int
            public uint Type;
        }

#pragma warning disable 0649
        
        internal struct RGNDATAHEADER
        {
            internal uint dwSize;
            internal uint iType;
            internal uint nCount;
            internal uint nRgnSize;
            internal RECT rcBound;
        };

#pragma warning restore 0649

        [StructLayout(LayoutKind.Sequential)]
        internal struct RGNDATA
        {
            internal RGNDATAHEADER rdh;

            internal unsafe static RECT* GetRectsPointer(RGNDATA* me)
            {
                return (RECT*)((byte*)me + sizeof(RGNDATAHEADER));
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RECT
        {
            internal int left;
            internal int top;
            internal int right;
            internal int bottom;
        }

    }
}
