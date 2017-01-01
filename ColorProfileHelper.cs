/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterHostDll.PSApi;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PSFilterHostDll
{
    internal static class ColorProfileHelper
    {
        /// <summary>
        /// Gets the color profile assigned to the primary monitor.
        /// </summary>
        /// <returns>The path of the color profile assigned to the primary monitor; or an empty string if no profile is assigned.</returns>
        internal static string GetPrimaryMonitorProfile()
        {
            string fileName = string.Empty;

            string deviceName = GetPrimaryMonitorDeviceName();
            if (!string.IsNullOrEmpty(deviceName))
            {
                IntPtr hdc = IntPtr.Zero;
                try
                {
                    hdc = SafeNativeMethods.CreateDCW(deviceName, deviceName, null, IntPtr.Zero);
                    if (hdc != IntPtr.Zero)
                    {
                        uint bufferSize = 0;
                        SafeNativeMethods.GetICMProfileW(hdc, ref bufferSize, null);

                        if (bufferSize > 0)
                        {
                            StringBuilder builder = new StringBuilder((int)bufferSize);
                            if (SafeNativeMethods.GetICMProfileW(hdc, ref bufferSize, builder))
                            {
                                fileName = builder.ToString();
                            }
                        }
                    }
                }
                finally
                {
                    if (hdc != IntPtr.Zero)
                    {
                        SafeNativeMethods.DeleteDC(hdc);
                        hdc = IntPtr.Zero;
                    }
                }
            }

            return fileName;
        }

        private static string GetPrimaryMonitorDeviceName()
        {
            string name = null;

            if (SafeNativeMethods.GetSystemMetrics(NativeConstants.SM_CMONITORS) != 0)
            {
                NativeStructs.POINT point = new NativeStructs.POINT
                {
                    x = 0,
                    y = 0
                };

                IntPtr hMonitor = SafeNativeMethods.MonitorFromPoint(point, NativeConstants.MONITOR_DEFAULTTOPRIMARY);

                NativeStructs.MONITORINFOEX monitorInfo = new NativeStructs.MONITORINFOEX();
                monitorInfo.cbSize = (uint)Marshal.SizeOf(typeof(NativeStructs.MONITORINFOEX));

                if (SafeNativeMethods.GetMonitorInfoW(hMonitor, ref monitorInfo))
                {
                    name = monitorInfo.szDeviceName.TrimEnd('\0');
                }
            }

            return name;
        }
    }
}
