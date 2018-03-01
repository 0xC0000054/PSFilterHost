/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2018 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.PSApi
{
    [System.Security.SuppressUnmanagedCodeSecurity]
    internal static class SafeNativeMethods
    {
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr HeapCreate([In()] uint flOptions, [In()] UIntPtr dwInitialsize, [In()] UIntPtr dwMaximumSize);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool HeapDestroy([In()] IntPtr hHeap);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr GetProcessHeap();

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr HeapAlloc([In()] IntPtr hHeap, [In()] uint dwFlags, [In()] UIntPtr dwSize);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool HeapFree([In()] IntPtr hHeap, [In()] uint dwFlags, [In()] IntPtr lpMem);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr HeapReAlloc(
            [In()] IntPtr hHeap,
            [In()] uint dwFlags,
            [In()] IntPtr lpMem,
            [In()] UIntPtr dwBytes
            );

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern UIntPtr HeapSize([In()] IntPtr hHeap, [In()] uint dwFlags, [In()] IntPtr lpMem);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern unsafe uint HeapSetInformation(
            [In()] IntPtr HeapHandle,
            [In()] int HeapInformationClass,
            [In()] void* HeapInformation,
            [In()] UIntPtr HeapInformationLength
            );

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr VirtualAlloc(
            [In()] IntPtr lpAddress,
            [In()] UIntPtr dwSize,
            [In()] uint flAllocationType,
            [In()] uint flProtect
            );

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool VirtualFree([In()] IntPtr lpAddress, [In()] UIntPtr dwSize, [In()] uint dwFreeType);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern unsafe UIntPtr VirtualQuery(
            [In()] IntPtr address,
            [Out()] out NativeStructs.MEMORY_BASIC_INFORMATION buffer,
            [In()] UIntPtr sizeOfBuffer
            );

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        internal static extern unsafe void memcpy([In()] void* dst, [In()] void* src, [In()] UIntPtr length);

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        internal static extern IntPtr memset([In()] IntPtr dest, [In()] int c, [In()] UIntPtr count);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetWindowTextW([In()] IntPtr hWnd, [In(), MarshalAs(UnmanagedType.LPWStr)] string lpString);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr GlobalSize([In()] IntPtr hMem);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr GlobalFree([In()] IntPtr hMem);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr GlobalLock([In()] IntPtr hMem);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr GlobalReAlloc([In()] IntPtr hMem, [In()] UIntPtr dwBytes, [In()] uint uFlags);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GlobalUnlock([In()] IntPtr hMem);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        internal unsafe static extern uint GetRegionData([In()] IntPtr hrgn, [In()] uint nCount, [Out()] NativeStructs.RGNDATA* lpRgnData);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        internal static extern IntPtr CreateCompatibleDC([In()] IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject([In()] IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteDC([In()] IntPtr hdc);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GlobalMemoryStatusEx([In(), Out()] ref NativeStructs.MEMORYSTATUSEX lpBuffer);

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern IntPtr CreateDCW(
            [In(), MarshalAs(UnmanagedType.LPWStr)] string lpszDriver,
            [In(), MarshalAs(UnmanagedType.LPWStr)] string lpszDevice,
            [In(), MarshalAs(UnmanagedType.LPWStr)] string lpszOutput,
            [In()] IntPtr lpInitData
            );

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetICMProfileW([In()] IntPtr hdc, [In(), Out()] ref uint bufferSize, [Out()] System.Text.StringBuilder buffer);

        [DllImport("user32.dll", ExactSpelling = true)]
        internal static extern int GetSystemMetrics([In()] int nIndex);

        [DllImport("user32.dll", ExactSpelling = true)]
        internal static extern IntPtr MonitorFromPoint([In()] NativeStructs.POINT pt, [In()] uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetMonitorInfoW([In()] IntPtr hMonitor, [In(), Out()] ref NativeStructs.MONITORINFOEX lpmi);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern int lstrlenA([In()] IntPtr ptr);
    }
}
