/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.PSApi
{
    internal static class IPTCData
    {
        private const ushort IPTCTagSignature = 0x1c02;
        private const byte RecordVersionType = 0;
        private const byte CaptionType = 120;
        private const ushort IPTCVersion = 2;
        internal const int MaxCaptionLength = 2000;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct IPTCTag
        {
            /// <summary>
            /// The signature of an IPTC tag.
            /// </summary>
            public ushort signature;
            /// <summary>
            /// The type of an IPTC tag.
            /// </summary>
            public byte type;
            /// <summary>
            /// The length of the following data.
            /// </summary>
            public ushort length;

            /// <summary>
            /// Initializes a new instance of the <see cref="IPTCTag"/> structure.
            /// </summary>
            public IPTCTag(byte type, ushort length)
            {
                this.signature = IPTCTagSignature;
                this.type = type;
                this.length = length;
            }
        }
 
        /// <summary>
        /// The record containing the IPTC-NAA specification version.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct IPTCRecordVersion
        {
            public IPTCTag tag;
            public ushort version;
        }

        /// <summary>
        /// The record containing the IPTC-NAA caption tag.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct IPTCCaption
        {
            public IPTCRecordVersion version;
            public IPTCTag tag;

            /// <summary>
            /// Initializes a new instance of the <see cref="IPTCCaption"/> structure.
            /// </summary>
            /// <param name="data">The pointer to the unmanaged caption header.</param>
            /// <exception cref="ArgumentNullException"><paramref name="data"/> is null.</exception>
            public unsafe IPTCCaption(IntPtr data)
            {
                if (data == IntPtr.Zero)
                {
                    throw new ArgumentNullException("data");
                }

                byte* ptr = (byte*)data.ToPointer();

                // Swap the version structure to little-endian.
                this.version.tag.signature = SwapUInt16(*(ushort*)ptr);
                ptr += 2;
                this.version.tag.type = *ptr;
                ptr += 1;
                this.version.tag.length = SwapUInt16(*(ushort*)ptr);
                ptr += 2;
                this.version.version = SwapUInt16(*(ushort*)ptr);
                ptr += 2;

                // Swap the tag structure to little-endian.
                this.tag.signature = SwapUInt16(*(ushort*)ptr);
                ptr += 2;
                this.tag.type = *ptr;
                ptr += 1;
                this.tag.length = SwapUInt16(*(ushort*)ptr);
                ptr += 2;
            }

            /// <summary>
            /// Converts the structure to a byte array in big-endian format.
            /// </summary>
            internal unsafe byte[] ToByteArray()
            {
                int size = Marshal.SizeOf(typeof(IPTCCaption));
                byte[] bytes = new byte[size];

                fixed (byte* pinData = bytes)
                {
                    byte* ptr = pinData;

                    // Swap the version structure to big-endian.
                    *((ushort*)ptr) = SwapUInt16(this.version.tag.signature);
                    ptr += 2;
                    *ptr = this.version.tag.type;
                    ptr += 1;
                    *((ushort*)ptr) = SwapUInt16(this.version.tag.length);
                    ptr += 2;
                    *((ushort*)ptr) = SwapUInt16(this.version.version);
                    ptr += 2;
                    
                    // Swap the tag structure to big-endian.
                    *((ushort*)ptr) = SwapUInt16(this.tag.signature);
                    ptr += 2;
                    *ptr = this.tag.type;
                    ptr += 1;
                    *((ushort*)ptr) = SwapUInt16(this.tag.length);
                    ptr += 2;
                }

                return bytes;
            }
        }


        private static unsafe IPTCRecordVersion CreateVersionRecord()
        {
            IPTCRecordVersion record = new IPTCRecordVersion
            {
                tag = new IPTCTag(RecordVersionType, sizeof(ushort)),
                version = IPTCVersion
            };

            return record;
        }

        /// <summary>
        /// Creates the IPTC-NAA caption record.
        /// </summary>
        /// <param name="captionLength">Length of the caption data in bytes.</param>
        internal static IPTCCaption CreateCaptionRecord(int captionLength)
        {
            IPTCCaption caption = new IPTCCaption
            {
                version = CreateVersionRecord(),
                tag = new IPTCTag(CaptionType, (ushort)captionLength)
            };

            return caption;
        }

        /// <summary>
        /// Converts the caption from unmanaged memory.
        /// </summary>
        /// <param name="data">The data.</param>
        internal static IPTCCaption CaptionFromMemory(IntPtr data)
        {
            IPTCCaption caption = new IPTCCaption(data);
            
            return caption;
        }

        private static ushort SwapUInt16(ushort value)
        {
            return (ushort)(((value & 0xff) << 8) | ((value >> 8) & 0xff));
        }
    } 
}
