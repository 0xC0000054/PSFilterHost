﻿/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

namespace HostTest
{
    internal static class NativeConstants
    {
        internal const uint WM_MOUSEACTIVATE = 0x21;
        internal const uint MA_ACTIVATE = 1;
        internal const uint MA_ACTIVATEANDEAT = 2;
        internal const uint MA_NOACTIVATE = 3;
        internal const uint MA_NOACTIVATEANDEAT = 4;

        internal const uint MONITOR_DEFAULTTONEAREST = 2;

        internal const uint LCS_sRGB = 0x73524742;
        internal const int MAX_PATH = 260;

        internal const int S_OK = 0;
        internal const int S_FALSE = 1;

        internal const string CLSID_FileOpenDialog = "DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7";

        internal const string IID_IModalWindow = "b4db1657-70d7-485e-8e3e-6fcb5a5c1802";
        internal const string IID_IFileDialog = "42f85136-db7e-439c-85f1-e4075d135fc8";
        internal const string IID_IFileOpenDialog = "d57c7288-d4ad-4768-be02-9d969532d960";
        internal const string IID_IFileDialogEvents = "973510DB-7D7F-452B-8975-74A85828D354";
        internal const string IID_IFileDialogControlEvents = "36116642-D713-4b97-9B83-7484A9D00433";
        internal const string IID_IFileDialogCustomize = "8016b7b3-3d49-4504-a0aa-2a37494e606f";
        internal const string IID_IShellItem = "43826D1E-E718-42EE-BC55-A1E261C37BFE";
        internal const string IID_IShellItemArray = "B63EA76D-1F85-456F-A19C-48159EFA858B";

        internal const uint SEM_FAILCRITICALERRORS = 1;
    }
}
