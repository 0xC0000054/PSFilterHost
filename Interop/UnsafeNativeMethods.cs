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
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.Interop
{
    [System.Security.SuppressUnmanagedCodeSecurity]
    internal static class UnsafeNativeMethods
    {
        [return: MarshalAs(UnmanagedType.Bool)]
        internal delegate bool EnumResNameDelegate([In()] IntPtr hModule, [In()] IntPtr lpszType, [In()] IntPtr lpszName, [In()] IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static extern SafeLibraryHandle LoadLibraryExW([In(), MarshalAs(UnmanagedType.LPWStr)] string lpFileName, [In()] IntPtr hFile, [In()] uint dwFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnumResourceNamesW(
            [In()] IntPtr hModule,
            [In(), MarshalAs(UnmanagedType.LPWStr)] string lpszType,
            [In()] EnumResNameDelegate lpEnumFunc,
            [In()] IntPtr lParam
            );

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern IntPtr FindResourceW([In()] IntPtr hModule, [In()] IntPtr lpName, [In()] IntPtr lpType);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr LoadResource([In()] IntPtr hModule, [In()] IntPtr hResource);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr LockResource([In()] IntPtr hGlobal);

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [DllImport("kernel32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeLibrary([In()] IntPtr hModule);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, BestFitMapping = false)]
        internal static extern IntPtr GetProcAddress([In()] SafeLibraryHandle hModule, [In(), MarshalAs(UnmanagedType.LPStr)] string lpProcName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static extern SafeFindHandle FindFirstFileExW(
                [In(), MarshalAs(UnmanagedType.LPWStr)] string fileName,
                [In()] NativeEnums.FindExInfoLevel infoLevel,
                [Out()] WIN32_FIND_DATAW data,
                [In()] NativeEnums.FindExSearchOp searchOp,
                [In()] IntPtr searchFilter,
                [In()] NativeEnums.FindExAdditionalFlags flags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FindNextFileW([In()] SafeFindHandle hndFindFile, [Out()] WIN32_FIND_DATAW lpFindFileData);

        [DllImport("kernel32.dll", ExactSpelling = true), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FindClose([In()] IntPtr handle);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern uint GetFileAttributesW([In(), MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern uint SetErrorMode([In()] uint uMode);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetThreadErrorMode([In()] uint dwNewMode, [Out()] out uint lpOldMode);

        internal static class Mscms
        {
            [DllImport("mscms.dll", ExactSpelling = true, SetLastError = true)]
            internal static extern SafeProfileHandle OpenColorProfileW(
                [In()] ref NativeStructs.Mscms.PROFILE profile,
                [In()] NativeEnums.Mscms.ProfileAccess desiredAccess,
                [In()] NativeEnums.FileShare shareMode,
                [In()] NativeEnums.CreationDisposition creationMode
                );

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [DllImport("mscms.dll", ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CloseColorProfile([In()] IntPtr handle);

            [DllImport("mscms.dll", ExactSpelling = true, SetLastError = true)]
            internal static extern SafeTransformHandle CreateMultiProfileTransform(
                [In()] IntPtr[] pahProfiles,
                [In()] uint nProfiles,
                [In()] uint[] padwIntent,
                [In()] uint nIntents,
                [In()] NativeEnums.Mscms.TransformFlags dwFlags,
                [In()] uint indexPreferredCMM
                );

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [DllImport("mscms.dll", ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool DeleteColorTransform([In()] IntPtr handle);

            [DllImport("mscms.dll", ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool TranslateBitmapBits(
                [In()] SafeTransformHandle hTransform,
                [In()] IntPtr pSrcBits,
                [In()] NativeEnums.Mscms.BMFORMAT bmInput,
                [In()] uint dwWidth,
                [In()] uint dwHeight,
                [In()] uint dwInputStride,
                [In()] IntPtr pDestBits,
                [In()] NativeEnums.Mscms.BMFORMAT bmOutput,
                [In()] uint dwOutputStride,
                [In()] IntPtr pfnCallback,
                [In()] IntPtr ulCallbackData
                );

            [DllImport("mscms.dll", ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetColorProfileHeader([In()] SafeProfileHandle hProfile, [Out()] out NativeStructs.Mscms.PROFILEHEADER pHeader);
        }
    }
}
