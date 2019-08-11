/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2019 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using System.Security;

namespace PSFilterHostDll.PSApi
{
    internal static class PEFile
    {
        private const ushort IMAGE_DOS_SIGNATURE = 0x5A4D; // MZ
        private const uint IMAGE_NT_SIGNATURE = 0x00004550; // PE00
        private const ushort IMAGE_FILE_MACHINE_I386 = 0x14C;
        private const ushort IMAGE_FILE_MACHINE_AMD64 = 0x8664;
        private const int NTSignatureOffsetLocation = 0x3C;

        /// <summary>
        /// Checks that the processor architecture of the DLL matches the current process.
        /// </summary>
        /// <param name="fileName">The file name to check.</param>
        /// <returns><c>true</c> if the processor architecture matches the current process; otherwise <c>false</c>.</returns>
        internal static bool CheckProcessorArchitecture(string fileName)
        {
            FileStream stream = null;

            try
            {
                stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

                using (EndianBinaryReader reader = new EndianBinaryReader(stream, Endianess.Little))
                {
                    stream = null;

                    return CheckProcessorArchitectureImpl(reader);
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
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                    stream = null;
                }
            }

            return false;
        }

        private static bool CheckProcessorArchitectureImpl(EndianBinaryReader reader)
        {
            ushort dosSignature = reader.ReadUInt16();
            if (dosSignature == IMAGE_DOS_SIGNATURE)
            {
                reader.Position = NTSignatureOffsetLocation;

                uint ntSignatureOffset = reader.ReadUInt32();

                reader.Position = ntSignatureOffset;

                uint ntSignature = reader.ReadUInt32();
                if (ntSignature == IMAGE_NT_SIGNATURE)
                {
                    ushort machineType = reader.ReadUInt16();

                    if (IntPtr.Size == 4)
                    {
                        return machineType == IMAGE_FILE_MACHINE_I386;
                    }
                    else if (IntPtr.Size == 8)
                    {
                        return machineType == IMAGE_FILE_MACHINE_AMD64;
                    }
                }
            }

            return false;
        }
    }
}
