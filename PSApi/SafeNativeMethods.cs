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

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [System.Security.SuppressUnmanagedCodeSecurity]
    internal static class SafeNativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        internal static extern IntPtr HeapCreate([In()] uint flOptions, [In()] UIntPtr dwInitialsize, [In()] UIntPtr dwMaximumSize);

        [DllImport("kernel32.dll", SetLastError = false, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool HeapDestroy([In()] IntPtr hHeap);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        internal static extern IntPtr GetProcessHeap();

        [DllImport("kernel32.dll", SetLastError = false, ExactSpelling = true)]
        internal static extern IntPtr HeapAlloc([In()] IntPtr hHeap, [In()] uint dwFlags, [In()] UIntPtr dwSize);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool HeapFree([In()] IntPtr hHeap, [In()] uint dwFlags, [In()] IntPtr lpMem);

        [DllImport("kernel32.dll", SetLastError = false, ExactSpelling = true)]
        internal static extern IntPtr HeapReAlloc([In()] IntPtr hHeap, [In()] uint dwFlags, [In()] IntPtr lpMem, [In()] UIntPtr dwBytes);

        [DllImport("kernel32.dll", SetLastError = false, ExactSpelling = true)]
        internal static extern UIntPtr HeapSize([In()] IntPtr hHeap, [In()] uint dwFlags, [In()] IntPtr lpMem);

        [DllImport("kernel32.dll", SetLastError = false, ExactSpelling = true)]
        internal static extern unsafe uint HeapSetInformation([In()] IntPtr HeapHandle, [In()] int HeapInformationClass, [In()] void* HeapInformation, [In()] UIntPtr HeapInformationLength);

        [DllImport("kernel32.dll", SetLastError = false, ExactSpelling = true)]
        internal static extern IntPtr VirtualAlloc([In()] IntPtr lpAddress, [In()] UIntPtr dwSize, [In()] uint flAllocationType, [In()] uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool VirtualFree([In()] IntPtr lpAddress, [In()] UIntPtr dwSize, [In()] uint dwFreeType);

        [DllImport("kernel32.dll", SetLastError = false, ExactSpelling = true)]
        internal static extern unsafe UIntPtr VirtualQuery([In()] IntPtr address, ref NativeStructs.MEMORY_BASIC_INFORMATION buffer, [In()] UIntPtr sizeOfBuffer);

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = false, ExactSpelling = true)]
        internal static extern unsafe void memcpy([In()] void* dst, [In()] void* src, [In()] UIntPtr length);

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = false, ExactSpelling = true)]
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
    }
}
