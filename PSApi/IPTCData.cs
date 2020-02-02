﻿/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2020 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PSFilterHostDll.PSApi
{
    internal sealed class IPTCData
    {
        private const byte IPTCTagSignature = 0x1c;
        private const ushort IPTCVersion = 2;
        private const int MaxCaptionLength = 2000;

        private byte[] captionRecordBytes;
        private bool createdCaptionRecord;

        public IPTCData()
        {
            captionRecordBytes = null;
            createdCaptionRecord = false;
        }

        /// <summary>
        /// Creates the IPTC-NAA caption record.
        /// </summary>
        /// <param name="value">A string containing the caption.</param>
        /// <param name="captionRecord">The byte array containing the caption data.</param>
        /// <returns><c>true</c> if the <paramref name="value"/> was converted successfully; otherwise, <c>false</c></returns>
        internal bool TryCreateCaptionRecord(string value, out byte[] captionRecord)
        {
            if (!createdCaptionRecord)
            {
                createdCaptionRecord = true;

                if (!string.IsNullOrEmpty(value))
                {
                    int captionLength = Encoding.ASCII.GetByteCount(value);

                    if (captionLength < MaxCaptionLength)
                    {
                        IPTCCaption captionHeader = new IPTCCaption
                        {
                            version = new IPTCRecordVersion(IPTCVersion),
                            tag = new IPTCTag(IPTCRecord.App2, App2DataSets.Caption, (ushort)captionLength)
                        };

                        captionRecordBytes = new byte[IPTCCaption.SizeOf + captionLength];

                        captionHeader.Write(captionRecordBytes);
                        Encoding.ASCII.GetBytes(value, 0, value.Length, captionRecordBytes, IPTCCaption.SizeOf);
                    }
                }
            }

            captionRecord = captionRecordBytes;
            return captionRecordBytes != null;
        }

        /// <summary>
        /// Converts the caption from unmanaged memory.
        /// </summary>
        /// <param name="data">The data.</param>
        internal static string CaptionFromMemory(IntPtr data)
        {
            if (data != IntPtr.Zero)
            {
                IPTCCaption captionHeader = new IPTCCaption(data);
                string caption = string.Empty;

                if (captionHeader.tag.length > 0)
                {
                    byte[] bytes = new byte[captionHeader.tag.length];

                    Marshal.Copy(new IntPtr(data.ToInt64() + IPTCCaption.SizeOf), bytes, 0, bytes.Length);

                    caption = Encoding.ASCII.GetString(bytes);
                }

                return caption;
            }

            return null;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct IPTCTag
        {
            /// <summary>
            /// The signature of an IPTC tag.
            /// </summary>
            public byte signature;
            /// <summary>
            /// The record type of an IPTC tag.
            /// </summary>
            public byte record;
            /// <summary>
            /// The data set of an IPTC tag.
            /// </summary>
            public byte dataSet;
            /// <summary>
            /// The length of the following data.
            /// </summary>
            public ushort length;

            /// <summary>
            /// Initializes a new instance of the <see cref="IPTCTag"/> structure.
            /// </summary>
            public IPTCTag(byte record, byte dataSet, ushort length)
            {
                signature = IPTCTagSignature;
                this.record = record;
                this.dataSet = dataSet;
                this.length = length;
            }
        }

        /// <summary>
        /// The record containing the IPTC-NAA specification version.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct IPTCRecordVersion
        {
            public IPTCTag tag;
            public ushort version;

            public IPTCRecordVersion(ushort version)
            {
                tag = new IPTCTag(IPTCRecord.App2, App2DataSets.RecordVersion, sizeof(ushort));
                this.version = version;
            }
        }

        /// <summary>
        /// The record containing the IPTC-NAA caption tag.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct IPTCCaption
        {
            public IPTCRecordVersion version;
            public IPTCTag tag;

            public static readonly int SizeOf = Marshal.SizeOf(typeof(IPTCCaption));

            /// <summary>
            /// Initializes a new instance of the <see cref="IPTCCaption"/> structure.
            /// </summary>
            /// <param name="data">The pointer to the unmanaged caption header.</param>
            /// <exception cref="ArgumentNullException"><paramref name="data"/> is null.</exception>
            public unsafe IPTCCaption(IntPtr data)
            {
                if (data == IntPtr.Zero)
                {
                    throw new ArgumentNullException(nameof(data));
                }

                byte* ptr = (byte*)data.ToPointer();

                // Swap the version structure to little-endian.
                version.tag.signature = *ptr;
                ptr += 1;
                version.tag.record = *ptr;
                ptr += 1;
                version.tag.dataSet = *ptr;
                ptr += 1;
                version.tag.length = SwapUInt16(*(ushort*)ptr);
                ptr += 2;
                version.version = SwapUInt16(*(ushort*)ptr);
                ptr += 2;

                // Swap the tag structure to little-endian.
                tag.signature = *ptr;
                ptr += 1;
                tag.record = *ptr;
                ptr += 1;
                tag.dataSet = *ptr;
                ptr += 1;
                tag.length = SwapUInt16(*(ushort*)ptr);
            }

            /// <summary>
            /// Writes the structure to the specified byte array in big-endian format.
            /// </summary>
            /// <param name="buffer">The output buffer.</param>
            /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
            /// <exception cref="ArgumentException"><paramref name="buffer"/> is less than the <see cref="IPTCCaption"/> size.</exception>
            internal unsafe void Write(byte[] buffer)
            {
                if (buffer == null)
                {
                    throw new ArgumentNullException(nameof(buffer));
                }

                if (buffer.Length < SizeOf)
                {
                    throw new ArgumentException("The buffer must be >= IPTCCaption.SizeOf.");
                }

                fixed (byte* pinData = buffer)
                {
                    byte* ptr = pinData;

                    // Swap the version structure to big-endian.
                    *ptr = version.tag.signature;
                    ptr += 1;
                    *ptr = version.tag.record;
                    ptr += 1;
                    *ptr = version.tag.dataSet;
                    ptr += 1;
                    *(ushort*)ptr = SwapUInt16(version.tag.length);
                    ptr += 2;
                    *(ushort*)ptr = SwapUInt16(version.version);
                    ptr += 2;

                    // Swap the tag structure to big-endian.
                    *ptr = tag.signature;
                    ptr += 1;
                    *ptr = tag.record;
                    ptr += 1;
                    *ptr = tag.dataSet;
                    ptr += 1;
                    *(ushort*)ptr = SwapUInt16(tag.length);
                }
            }

            private static ushort SwapUInt16(ushort value)
            {
                return (ushort)(((value & 0xff) << 8) | ((value >> 8) & 0xff));
            }
        }

        private sealed class IPTCRecord
        {
            public const byte App2 = 2;
        }

        private sealed class App2DataSets
        {
            public const byte RecordVersion = 0;
            public const byte Caption = 120;
        }
    }
}
