/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2020 Nicholas Hayes
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

using System;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.Interop
{
    internal static class NativeStructs
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public UIntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
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

            internal static unsafe RECT* GetRectsPointer(RGNDATA* me)
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

        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct MONITORINFOEX
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDeviceName;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BY_HANDLE_FILE_INFORMATION
        {
            public uint dwFileAttributes;
            public FILETIME ftCreationTime;
            public FILETIME ftLastAccessTime;
            public FILETIME ftLastWriteTime;
            public uint dwVolumeSerialNumber;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint nNumberOfLinks;
            public uint nFileIndexHigh;
            public uint nFileIndexLow;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct FILE_ID_INFO
        {
            public ulong VolumeSerialNumber;
            public fixed byte FileID[16];
        }

        internal static class Mscms
        {
            [StructLayout(LayoutKind.Sequential)]
            internal struct PROFILE
            {
                public NativeEnums.Mscms.ProfileType dwType;
                public unsafe void* pProfileData;
                public uint cbDataSize;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal unsafe struct PROFILEHEADER
            {
                public uint phSize;
                public uint phCMMType;
                public uint phVersion;
                public uint phClass;
                public uint phDataColorSpace;
                public uint phConnectionSpace;
                public uint phDateTime1;
                public uint phDateTime2;
                public uint phDateTime3;
                public uint phSignature;
                public uint phPlatform;
                public uint phProfileFlags;
                public uint phManufacturer;
                public uint phModel;
                public uint phAttributes1;
                public uint phAttributes2;
                public uint phRenderingIntent;
                public int phIlluminantX;
                public int phIlluminantY;
                public int phIlluminantZ;
                public uint phCreator;
                public fixed byte phReserved[44];
            }
        }
    }
}
