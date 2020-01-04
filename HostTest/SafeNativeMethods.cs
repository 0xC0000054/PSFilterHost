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

using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;

namespace HostTest
{
    [System.Security.SuppressUnmanagedCodeSecurity]
    internal static class SafeNativeMethods
    {
        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        internal static extern SafeDCHandle CreateDC(
            string lpszDriver,
            string lpszDevice,
            string lpszOutput,
            IntPtr lpInitData
            );

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [DllImport("gdi32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetICMProfile(SafeDCHandle hdc, ref uint bufferSize, [Out()] StringBuilder buffer);

        [DllImport("user32.dll", ExactSpelling = true)]
        internal static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref NativeStructs.MONITORINFOEX lpmi);

        [DllImport("mscms.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetStandardColorSpaceProfile(
            string pMachineName,
            uint dwProfileID,
            [Out()] StringBuilder pProfileName,
            ref uint pdwSize
            );

        [DllImport("mscms.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetColorDirectory(string pMachineName, [Out()] StringBuilder pBuffer, ref uint pdwSize);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            IntPtr pbc,
            [In()] ref Guid riid,
            [MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] out NativeInterfaces.IShellItem ppv
            );

        [DllImport("kernel32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetProcessDEPPolicy(uint dwFlags);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern uint SetErrorMode(uint uMode);
    }
}
