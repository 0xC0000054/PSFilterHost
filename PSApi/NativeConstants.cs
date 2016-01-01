﻿/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft internal License:
//   Copyright (C) 2012-2016 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

namespace PSFilterHostDll.PSApi
{
    internal static class NativeConstants
    {
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
    }
}
