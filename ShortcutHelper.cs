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
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

#if !NET_40_OR_GREATER
using System.Security.Permissions;
#endif

namespace PSFilterHostDll
{

#if NET_40_OR_GREATER
    [SecurityCritical]
#else    
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
#endif
    internal static class ShortcutHelper
    {
        [SuppressUnmanagedCodeSecurity]
        private static class SafeNativeMethods
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal extern static IntPtr GetModuleHandleW([In()] string moduleName);

            [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, BestFitMapping = false)]
            internal static extern IntPtr GetProcAddress([In()] IntPtr hModule, [In(), MarshalAs(UnmanagedType.LPStr)] string lpProcName);

            [DllImport("kernel32.dll", ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool IsWow64Process([In()] IntPtr hProcess, [MarshalAs(UnmanagedType.Bool)] out bool Wow64Process);

            [DllImport("kernel32.dll", ExactSpelling = true)]
            internal static extern IntPtr GetCurrentProcess();
        }

        private static bool IsWoW64Process()
        {
            if (IntPtr.Size == 4)
            {
                IntPtr hMod = SafeNativeMethods.GetModuleHandleW("kernel32.dll");

                if (hMod != IntPtr.Zero)
                {
                    if (SafeNativeMethods.GetProcAddress(hMod, "IsWow64Process") != IntPtr.Zero)
                    {
                        bool isWow64 = false;
                        if (SafeNativeMethods.IsWow64Process(SafeNativeMethods.GetCurrentProcess(), out isWow64))
                        {
                            return isWow64;
                        }
                    }
                } 
            }

            return false;
        }

        private static bool IsWin7OrLater()
        {
            OperatingSystem os = Environment.OSVersion;

            return (os.Platform == PlatformID.Win32NT && (os.Version.Major > 6 || (os.Version.Major == 6 && os.Version.Minor >= 1)));
        }

        /// <summary>
        /// Fixes the Program Files shortcut path when running under the WoW64 subsystem.
        /// </summary>
        /// <param name="path">The path to fix.</param>
        /// <returns>The fixed path.</returns>
        internal static string FixWoW64ShortcutPath(string path)
        {
            if (!File.Exists(path) && IsWoW64Process())
            {
                // WoW64 changes the 64-bit Program Files path to the 32-bit Program Files path, so we change it back. 
                string programFiles86 = Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%");

                int index = path.IndexOf(programFiles86, StringComparison.OrdinalIgnoreCase);
                if (index >= 0 && path.Length > programFiles86.Length)
                {
                    string newPath = path.Remove(index, programFiles86.Length + 1); // remove the trailing slash.

                    if (IsWin7OrLater())
                    {                    
                        return Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramW6432%"), newPath); 
                    }
                    else
                    {
                        // if we are not running Windows 7 or later remove the (x86) identifier instead.
                        string x86 = "(x86)";

                        int index86 = programFiles86.IndexOf(x86, StringComparison.OrdinalIgnoreCase);

                        if (index86 >= 0)
                        {
                            string programFiles = programFiles86.Remove(index86, x86.Length).Trim();
                            
                            return Path.Combine(programFiles, newPath);
                        }
                                                
                    }
                }
            }

            return path;
        }

    }
}
