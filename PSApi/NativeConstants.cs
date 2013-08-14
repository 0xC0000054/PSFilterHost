/////////////////////////////////////////////////////////////////////////////////
//
// Adobe(R) Photoshop(R) filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft internal License:
//   Copyright (C) 2012-2013 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

namespace PSFilterLoad.PSApi
{
    internal static class NativeConstants
    {
        internal const int GPTR = 64;

        internal const int LOAD_LIBRARY_AS_DATAFILE = 2;

        internal const uint MEM_COMMIT = 0x1000;
        internal const uint MEM_RESERVE = 0x2000;
        internal const uint MEM_RELEASE = 0x8000;

        internal const int PAGE_NOACCESS = 1;
        internal const int PAGE_READONLY = 2;
        internal const int PAGE_READWRITE = 4;
        internal const int PAGE_WRITECOPY = 8;
        internal const int PAGE_EXECUTE = 16;
        internal const int PAGE_EXECUTE_READ = 32;
        internal const int PAGE_EXECUTE_READWRITE = 64;
        internal const int PAGE_EXECUTE_WRITECOPY = 128;
        internal const int PAGE_GUARD = 256;
    }
}
