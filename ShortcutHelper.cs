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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace PSFilterHostDll
{

    internal static class ShortcutHelper
    {
        [System.Security.SuppressUnmanagedCodeSecurity]
        private static class SafeNativeMethods
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
            internal extern static IntPtr GetModuleHandle([In()] string moduleName);

            [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, BestFitMapping = false)]
            internal static extern IntPtr GetProcAddress([In()] IntPtr hModule, [In(), MarshalAs(UnmanagedType.LPStr)] string lpProcName);

            [DllImport("kernel32.dll", EntryPoint = "IsWow64Process")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool IsWow64Process([In()] IntPtr hProcess, [MarshalAs(UnmanagedType.Bool)] out bool Wow64Process);
        }

        private static bool IsWoW64Process()
        {
            if (IntPtr.Size == 4)
            {
                IntPtr hMod = SafeNativeMethods.GetModuleHandle("kernel32.dll");

                if (hMod != IntPtr.Zero)
                {
                    if (SafeNativeMethods.GetProcAddress(hMod, "IsWow64Process") != IntPtr.Zero)
                    {
                        bool isWow64 = false;
                        if (SafeNativeMethods.IsWow64Process(Process.GetCurrentProcess().Handle, out isWow64))
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
            Version os = Environment.OSVersion.Version;

            return (os.Major > 6 || (os.Major == 6 && os.Minor >= 1));
        }

        /// <summary>
        /// Fixes the Program Files shortcut path when running under the WoW64 subsystem.
        /// </summary>
        /// <param name="path">The path to fix.</param>
        /// <returns>The fixed path.</returns>
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        internal static string FixWoW64ShortcutPath(string path)
        {
            if (!File.Exists(path) && IsWoW64Process())
            {
                // WoW64 changes the 64-bit Program Files path to the 32-bit Program Files path, so we change it back. 
                string programFiles86 = Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%");

                int index = path.IndexOf(programFiles86);
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

                        int index86 = programFiles86.IndexOf(x86);

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
