/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterHostDll
{
    internal static class OS
    {
        private static volatile bool checkedIsWindows7OrLater;
        private static volatile bool isWindows7OrLater;

        public static bool IsWindows7OrLater
        {
            get
            {
                if (!checkedIsWindows7OrLater)
                {
                    OperatingSystem os = Environment.OSVersion;
                    isWindows7OrLater = (os.Platform == PlatformID.Win32NT && ((os.Version.Major == 6 && os.Version.Minor >= 1) || os.Version.Major > 6));
                    checkedIsWindows7OrLater = true;
                }

                return isWindows7OrLater;
            }
        }
    }
}
