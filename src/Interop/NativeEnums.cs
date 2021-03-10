/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2021 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterHostDll.Interop
{
    internal static class NativeEnums
    {
#pragma warning disable RCS1154 // Sort enum members.
#pragma warning disable RCS1191 // Declare enum value as combination of names.

        [Flags]
        internal enum FileShare : uint
        {
            None = 0,
            Read = 1,
            Write = 2,
            Delete = 4
        }

        internal enum CreationDisposition : uint
        {
            CreateNew = 1,
            CreateAlways = 2,
            OpenExisting = 3,
            OpenAlways = 4,
            TruncateExisting = 5
        }

        internal enum FindExInfoLevel : int
        {
            Standard = 0,
            Basic
        }

        internal enum FindExSearchOp : int
        {
            NameMatch = 0,
            LimitToDirectories,
            LimitToDevices
        }

        [Flags]
        internal enum FindExAdditionalFlags : uint
        {
            None = 0U,
            CaseSensitive = 1U,
            LargeFetch = 2U
        }

        internal enum FILE_INFO_BY_HANDLE_CLASS : int
        {
            FileBasicInfo = 0,
            FileStandardInfo = 1,
            FileNameInfo = 2,
            FileRenameInfo = 3,
            FileDispositionInfo = 4,
            FileAllocationInfo = 5,
            FileEndOfFileInfo = 6,
            FileStreamInfo = 7,
            FileCompressionInfo = 8,
            FileAttributeTagInfo = 9,
            FileIdBothDirectoryInfo = 10,// 0x0A
            FileIdBothDirectoryRestartInfo = 11, // 0xB
            FileIoPriorityHintInfo = 12, // 0xC
            FileRemoteProtocolInfo = 13, // 0xD
            FileFullDirectoryInfo = 14, // 0xE
            FileFullDirectoryRestartInfo = 15, // 0xF
            FileStorageInfo = 16, // 0x10
            FileAlignmentInfo = 17, // 0x11
            FileIdInfo = 18, // 0x12
            FileIdExtdDirectoryInfo = 19, // 0x13
            FileIdExtdDirectoryRestartInfo = 20, // 0x14
        }

        internal static class Mscms
        {
            internal enum ProfileType : uint
            {
                FileName = 1,
                MemoryBuffer = 2
            }

            internal enum ProfileAccess : int
            {
                Read = 1,
                ReadWrite = 2
            }

            internal enum RenderingIntent : uint
            {
                Perceptual = 0,
                RelativeColormetric = 1,
                Saturation = 3,
                AbsoluteColormetric = 3
            }

            [Flags()]
            internal enum TransformFlags : uint
            {
                None = 0,
                ProofMode = 1,
                NormalMode = 2,
                BestMode = 3,
                EnableGamutChecking = 0x00010000,
                UseRelativeColormetric = 0x00020000,
                FastTranslate = 0x00040000
            }

            internal enum BMFORMAT : int
            {
                BM_x555RGB = 0,
                BM_x555XYZ = 257,
                BM_x555Yxy,
                BM_x555Lab,
                BM_x555G3CH,
                BM_RGBTRIPLETS = 2,
                BM_BGRTRIPLETS = 4,
                BM_XYZTRIPLETS = 513,
                BM_YxyTRIPLETS,
                BM_LabTRIPLETS,
                BM_G3CHTRIPLETS,
                BM_5CHANNEL,
                BM_6CHANNEL,
                BM_7CHANNEL,
                BM_8CHANNEL,
                BM_GRAY,
                BM_xRGBQUADS = 8,
                BM_xBGRQUADS = 16,
                BM_xG3CHQUADS = 772,
                BM_KYMCQUADS,
                BM_CMYKQUADS = 32,
                BM_10b_RGB = 9,
                BM_10b_XYZ = 1025,
                BM_10b_Yxy,
                BM_10b_Lab,
                BM_10b_G3CH,
                BM_NAMED_INDEX,
                BM_16b_RGB = 10,
                BM_16b_XYZ = 1281,
                BM_16b_Yxy,
                BM_16b_Lab,
                BM_16b_G3CH,
                BM_16b_GRAY,
                BM_565RGB = 1,
                BM_32b_scRGB = 1537,
                BM_32b_scARGB = 1538,
                BM_S2DOT13FIXED_scRGB = 1539,
                BM_S2DOT13FIXED_scARGB = 1540,
                BM_R10G10B10A2 = 1793,
                BM_R10G10B10A2_XR = 1794,
                BM_R16G16B16A16_FLOAT = 1795,
            }
        }

#pragma warning restore RCS1154 // Sort enum members.
#pragma warning restore RCS1191 // Declare enum value as combination of names.
    }
}
