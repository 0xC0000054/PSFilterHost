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

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [System.Security.SuppressUnmanagedCodeSecurity]
    internal static class SafeNativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr HeapCreate(uint flOptions, IntPtr dwInitialsize, IntPtr dwMaximumSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool HeapDestroy(IntPtr hHeap);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetProcessHeap();

        [DllImport("kernel32.dll", SetLastError = false),]
        internal static extern IntPtr HeapAlloc(IntPtr hHeap, uint dwFlags, UIntPtr dwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool HeapFree(IntPtr hHeap, uint dwFlags, IntPtr lpMem);

        [DllImport("kernel32.dll", SetLastError = false)]
        internal static extern IntPtr HeapReAlloc(IntPtr hHeap, uint dwFlags, IntPtr lpMem, UIntPtr dwBytes);

        [DllImport("kernel32.dll", SetLastError = false)]
        internal static extern UIntPtr HeapSize(IntPtr hHeap, uint dwFlags, IntPtr lpMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern unsafe uint HeapSetInformation(IntPtr HeapHandle, int HeapInformationClass, void* HeapInformation, UIntPtr HeapInformationLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr VirtualAlloc(IntPtr lpAddress, UIntPtr dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool VirtualFree(IntPtr lpAddress, UIntPtr dwSize, uint dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern unsafe IntPtr VirtualQuery(IntPtr address, ref NativeStructs.MEMORY_BASIC_INFORMATION buffer, IntPtr sizeOfBuffer);

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        internal static extern unsafe void memcpy(void* dst, void* src, UIntPtr length);

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        internal static extern IntPtr memset(IntPtr dest, int c, UIntPtr count);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetWindowText(IntPtr hWnd, string lpString);

        [DllImport("kernel32.dll", EntryPoint = "GlobalSize")]
        internal static extern IntPtr GlobalSize([In()] System.IntPtr hMem);

        [DllImport("kernel32.dll", EntryPoint = "GlobalFree")]
        internal static extern IntPtr GlobalFree([In()] System.IntPtr hMem);

        [DllImport("kernel32.dll", EntryPoint = "GlobalLock")]
        internal static extern IntPtr GlobalLock([In()] System.IntPtr hMem);

        [DllImport("kernel32.dll", EntryPoint = "GlobalReAlloc")]
        internal static extern IntPtr GlobalReAlloc([In()] IntPtr hMem, UIntPtr dwBytes, uint uFlags);

        [DllImport("kernel32.dll", EntryPoint = "GlobalUnlock")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GlobalUnlock([In()] System.IntPtr hMem);

        [DllImport("gdi32.dll", EntryPoint = "GetRegionData")]
        internal unsafe static extern uint GetRegionData([In()] System.IntPtr hrgn, uint nCount, NativeStructs.RGNDATA* lpRgnData);

        [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleDC")]
        internal static extern IntPtr CreateCompatibleDC([In()] System.IntPtr hdc);

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject([In()] System.IntPtr hdc);

        [DllImport("gdi32.dll", EntryPoint = "DeleteDC")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteDC([In()] System.IntPtr hdc);

        [DllImport("kernel32.dll", EntryPoint = "SetErrorMode")]
        internal static extern uint SetErrorMode(uint uMode);
    }
}
