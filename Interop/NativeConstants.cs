/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft internal License:
//   Copyright (C) 2012-2019 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

namespace PSFilterHostDll.Interop
{
    internal static class NativeConstants
    {
        internal const uint FILE_ATTRIBUTE_DIRECTORY = 16U;
        internal const uint FILE_ATTRIBUTE_REPARSE_POINT = 1024U;

        internal const uint INVALID_FILE_ATTRIBUTES = 0xFFFFFFFF;

        internal const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

        internal const uint GENERIC_READ = 0x80000000;

        internal const uint SEM_FAILCRITICALERRORS = 1U;

        internal const int GPTR = 64;

        internal const int LOAD_LIBRARY_AS_DATAFILE = 2;

        internal const uint MEM_COMMIT = 0x1000;
        internal const uint MEM_RESERVE = 0x2000;
        internal const uint MEM_RELEASE = 0x8000;

        internal const uint HEAP_ZERO_MEMORY = 8;

        internal const int PAGE_NOACCESS = 1;
        internal const int PAGE_READONLY = 2;
        internal const int PAGE_READWRITE = 4;
        internal const int PAGE_WRITECOPY = 8;
        internal const int PAGE_EXECUTE = 16;
        internal const int PAGE_EXECUTE_READ = 32;
        internal const int PAGE_EXECUTE_READWRITE = 64;
        internal const int PAGE_EXECUTE_WRITECOPY = 128;
        internal const int PAGE_GUARD = 256;

        internal const int SM_CMONITORS = 80;
        internal const uint MONITOR_DEFAULTTOPRIMARY = 1;

        internal const int CMM_FROM_PROFILE = 0;

        internal const int ERROR_SUCCESS = 0;
        internal const int ERROR_FILE_NOT_FOUND = 2;
        internal const int ERROR_PATH_NOT_FOUND = 3;
        internal const int ERROR_ACCESS_DENIED = 5;
        internal const int ERROR_NO_MORE_FILES = 18;
        internal const int ERROR_DIRECTORY = 267;

        internal const int STGM_READ = 0;

        internal const int S_OK = 0;

        internal const string CLSID_ShellLink = "00021401-0000-0000-C000-000000000046";

        internal const string IID_IPersist = "0000010c-0000-0000-c000-000000000046";
        internal const string IID_IPersistFile = "0000010b-0000-0000-C000-000000000046";
        internal const string IID_IShellLinkW = "000214F9-0000-0000-C000-000000000046";
    }
}
