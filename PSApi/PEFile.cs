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
using System.IO;
using System.Security;

namespace PSFilterLoad.PSApi
{
    internal static class PEFile
    {
        private static uint ReadUInt32(Stream stream)
        {
            int byte1 = stream.ReadByte();
            if (byte1 == -1)
            {
                throw new EndOfStreamException();
            }
            
            int byte2 = stream.ReadByte();
            if (byte2 == -1)
            {
                throw new EndOfStreamException();
            }

            int byte3 = stream.ReadByte();
            if (byte3 == -1)
            {
                throw new EndOfStreamException();
            }

            int byte4 = stream.ReadByte();
            if (byte4 == -1)
            {
                throw new EndOfStreamException();
            }

            return (uint)(byte1 | (byte2 << 8) | (byte3 << 16) | (byte4 << 24));
        }

        private static ushort ReadUInt16(Stream stream)
        {
            int byte1 = stream.ReadByte();
            if (byte1 == -1)
            {
                throw new EndOfStreamException();
            }

            int byte2 = stream.ReadByte();
            if (byte2 == -1)
            {
                throw new EndOfStreamException();
            }

            return (ushort)(byte1 | (byte2 << 8));
        }

        private const ushort IMAGE_DOS_SIGNATURE = 0x5A4D; // MZ
        private const uint IMAGE_NT_SIGNATURE = 0x00004550; // PE00
        private const ushort IMAGE_FILE_MACHINE_I386 = 0x14C;
        private const ushort IMAGE_FILE_MACHINE_AMD64 = 0x8664;
        private const int NtHeaderOffsetLocation = 0x3C;

        /// <summary>
        /// Checks that the processor architecture of the DLL matches the current process.
        /// </summary>
        /// <param name="fileName">The file name to check.</param>
        /// <returns><c>true</c> if the processor architecture matches the current process; otherwise <c>false</c>.</returns>
        internal static bool CheckProcessorArchitecture(string fileName)
        {
            try
            {
                using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    ushort dosSignature = ReadUInt16(stream);
                    if (dosSignature == IMAGE_DOS_SIGNATURE)
                    {
                        stream.Seek(NtHeaderOffsetLocation, SeekOrigin.Begin);

                        uint ntHeaderOffset = ReadUInt32(stream);

                        stream.Seek(ntHeaderOffset, SeekOrigin.Begin);

                        uint ntHeaderSignature = ReadUInt32(stream);
                        if (ntHeaderSignature == IMAGE_NT_SIGNATURE)
                        {
                            ushort machineType = ReadUInt16(stream);

                            if (machineType == IMAGE_FILE_MACHINE_I386)
                            {
                                return (IntPtr.Size == 4);
                            }
                            else if (machineType == IMAGE_FILE_MACHINE_AMD64)
                            {
                                return (IntPtr.Size == 8);
                            }
                        }
                    }
                }
            }
            catch (ArgumentException)
            {
            }
            catch (IOException)
            {
            }
            catch (NotSupportedException)
            {
            }
            catch (SecurityException)
            { 
            }
            catch (UnauthorizedAccessException)
            {
            }

            return false;
        }


    }
}
