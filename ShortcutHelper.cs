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

        private static readonly string ProgramFilesX86 = Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%");

        /// <summary>
        /// Fixes the Program Files shortcut path when running under the WoW64 subsystem.
        /// </summary>
        /// <param name="path">The path to fix.</param>
        /// <param name="fixedPath">The fixed path.</param>
        /// <returns><c>true</c> if the path was fixed; otherwise, <c>false</c>.</returns>
        internal static bool FixWoW64ShortcutPath(string path, out string fixedPath)
        {
            fixedPath = null;

            if (IsWoW64Process())
            {
                // WoW64 changes the 64-bit Program Files path to the 32-bit Program Files path.
                if (path.StartsWith(ProgramFilesX86, StringComparison.OrdinalIgnoreCase))
                {
                    string filePath = string.Empty;
                    if (path.Length > ProgramFilesX86.Length)
                    {
                        // Remove the trailing slash, otherwise Path.Combine will mistake the file path for a network path.
                        filePath = path.Remove(0, ProgramFilesX86.Length + 1);
                    }

                    if (OS.IsWindows7OrLater)
                    {
                        fixedPath = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramW6432%"), filePath);
                        return true;
                    }
                    else
                    {
                        // If we are not running Windows 7 or later remove the (x86) identifier instead.
                        const string x86 = "(x86)";

                        int index = ProgramFilesX86.IndexOf(x86, StringComparison.OrdinalIgnoreCase);

                        if (index >= 0)
                        {
                            string programFiles = ProgramFilesX86.Remove(index, x86.Length).Trim();
                            fixedPath = Path.Combine(programFiles, filePath);
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
