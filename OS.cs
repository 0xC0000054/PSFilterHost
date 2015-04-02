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

namespace PSFilterHostDll
{
    internal static class OS
    {
        private static bool checkedIsWindows7OrLater;
        private static bool isWindows7OrLater;

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
