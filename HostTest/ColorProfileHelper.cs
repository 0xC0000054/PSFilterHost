/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2016 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;

namespace HostTest
{
    internal static class ColorProfileHelper
    {
        /// <summary>
        /// Gets the path of the color profile for the monitor containing the specified window handle.
        /// </summary>
        /// <param name="hwnd">The window handle for which to retrieve the color profile.</param>
        /// <returns>
        /// A string containing the color profile for the monitor; or null if no color profile is assigned.
        /// </returns>
        internal static string GetMonitorColorProfilePath(IntPtr hwnd)
        {
            string profile = null;

            IntPtr hMonitor = SafeNativeMethods.MonitorFromWindow(hwnd, NativeConstants.MONITOR_DEFAULTTONEAREST);

            NativeStructs.MONITORINFOEX monitorInfo = new NativeStructs.MONITORINFOEX();
            monitorInfo.cbSize = (uint)Marshal.SizeOf(typeof(NativeStructs.MONITORINFOEX));

            if (SafeNativeMethods.GetMonitorInfo(hMonitor, ref monitorInfo))
            {
                using (SafeDCHandle hdc = SafeNativeMethods.CreateDC(monitorInfo.szDeviceName, monitorInfo.szDeviceName, null, IntPtr.Zero))
                {
                    if (!hdc.IsInvalid)
                    {
                        uint size = 0;
                        SafeNativeMethods.GetICMProfile(hdc, ref size, null);

                        if (size > 0)
                        {
                            StringBuilder builder = new StringBuilder((int)size);

                            if (SafeNativeMethods.GetICMProfile(hdc, ref size, builder))
                            {
                                profile = builder.ToString();
                            }
                        }
                    }
                }
            }

            return profile;
        }

        /// <summary>
        /// Gets a <see cref="ColorContext"/> initialized to the sRGB color profile.
        /// </summary>
        /// <returns>A <c>ColorContext</c> initialized to the sRGB color profile.</returns>
        internal static ColorContext GetSrgbColorContext()
        {
            Uri path = GetSrgbProfilePath();
            return new ColorContext(path);
        }

        private static Uri GetSrgbProfilePath()
        {
            StringBuilder builder = new StringBuilder(NativeConstants.MAX_PATH);
            uint bufferSize = NativeConstants.MAX_PATH;

            if (!SafeNativeMethods.GetStandardColorSpaceProfile(null, NativeConstants.LCS_sRGB, builder, ref bufferSize))
            {
                throw new Win32Exception();
            }

            Uri path = null;

            string profile = builder.ToString();
            // GetStandardColorSpace may return a relative path to the system color directory.
            if (!Uri.TryCreate(profile, UriKind.Absolute, out path))
            {
                bufferSize = NativeConstants.MAX_PATH;
                if (!SafeNativeMethods.GetColorDirectory(null, builder, ref bufferSize))
                {
                    throw new Win32Exception();
                }
                path = new Uri(Path.Combine(builder.ToString(), profile), UriKind.Absolute);
            }

            return path;
        }

    }
}
