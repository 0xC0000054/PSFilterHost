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

#if GDIPLUS
using System.Drawing;
#else
using System.Windows.Media.Imaging;
#endif

namespace PSFilterHostDll.PSApi
{
    internal sealed class ImageMetadata : IDisposable
    {
#if GDIPLUS
        private Bitmap image;
#else
        private BitmapSource image;
#endif
        private byte[] exifBytes;
        private byte[] xmpBytes;
        private bool extractedExif;
        private bool extractedXMP;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageMetadata"/> class.
        /// </summary>
        /// <param name="image">The image that contains the meta data.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="image"/> is null.</exception>
#if GDIPLUS
        public ImageMetadata(Bitmap image)
#else
        public ImageMetadata(BitmapSource image)
#endif
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

#if GDIPLUS
            this.image = (Bitmap)image.Clone();
#else
            this.image = image.Clone();
#endif
            exifBytes = null;
            xmpBytes = null;
            extractedExif = false;
            extractedXMP = false;
            disposed = false;
        }

        /// <summary>
        /// Extracts the meta data from the image.
        /// </summary>
        /// <param name="bytes">The output bytes.</param>
        /// <param name="exif">set to <c>true</c> if the EXIF data is requested.</param>
        /// <returns><c>true</c> if the meta data was extracted; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ObjectDisposedException">The object has been disposed.</exception>
        public bool Extract(out byte[] bytes, bool exif)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ImageMetadata));
            }

            bytes = null;

            // Return the cached data if it has already been extracted.
            if (exif)
            {
                if (extractedExif)
                {
                    bytes = exifBytes;
                    return (bytes != null);
                }
            }
            else
            {
                if (extractedXMP)
                {
                    bytes = xmpBytes;
                    return (bytes != null);
                }
            }

#if !GDIPLUS
            BitmapMetadata metadata = null;

            try
            {
                metadata = image.Metadata as BitmapMetadata;
            }
            catch (NotSupportedException)
            {
            }

            if (metadata == null)
            {
                extractedExif = true;
                extractedXMP = true;

                return false;
            }

            if (exif)
            {
                BitmapMetadata exifMetadata = MetadataConverter.GetEXIFMetadata(metadata);

                if (exifMetadata == null)
                {
                    extractedExif = true;

                    return false;
                }
                metadata = exifMetadata;
            }
            else
            {
                BitmapMetadata xmpMetadata = MetadataConverter.GetXMPMetadata(metadata);

                if (xmpMetadata == null)
                {
                    extractedXMP = true;

                    return false;
                }
                metadata = xmpMetadata;
            }
#endif

            if (exif)
            {
                using (MemoryStream ms = new MemoryStream())
                {
#if GDIPLUS
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
#else
                    JpegBitmapEncoder enc = new JpegBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(image, null, metadata, null));
                    enc.Save(ms);
#endif
                    exifBytes = JpegReader.ExtractEXIF(ms);
                    extractedExif = true;
                    bytes = exifBytes;
                }
            }
            else
            {
                using (MemoryStream ms = new MemoryStream())
                {
#if GDIPLUS
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Tiff);
#else
                    TiffBitmapEncoder enc = new TiffBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(image, null, metadata, null));
                    enc.Save(ms);
#endif
                    xmpBytes = TiffReader.ExtractXMP(ms);
                    extractedXMP = true;
                    bytes = xmpBytes;
                }
            }

            return (bytes != null);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

#if GDIPLUS
                if (image != null)
                {
                    image.Dispose();
                    image = null;
                }
#endif

                exifBytes = null;
                xmpBytes = null;
            }
        }

        private static class JpegReader
        {
            private static class JpegMarkers
            {
                internal const ushort StartOfImage = 0xFFD8;
                internal const ushort EndOfImage = 0xFFD9;
                internal const ushort StartOfScan = 0xFFDA;
                internal const ushort App1 = 0xFFE1;
            }

            private const int EXIFSignatureLength = 6;

            /// <summary>
            /// Extracts the EXIF data from a JPEG image.
            /// </summary>
            /// <param name="stream">The JPEG image.</param>
            /// <returns>The extracted EXIF data, or null.</returns>
            internal static byte[] ExtractEXIF(Stream stream)
            {
                byte[] exifData = null;

                stream.Position = 0L;

                try
                {
                    using (EndianBinaryReader reader = new EndianBinaryReader(stream, Endianess.Big, true))
                    {
                        exifData = ExtractEXIFBlob(reader);
                    }
                }
                catch (EndOfStreamException)
                {
                }

                return exifData;
            }

            private static byte[] ExtractEXIFBlob(EndianBinaryReader reader)
            {
                ushort marker = reader.ReadUInt16();

                // Check the file signature.
                if (marker == JpegMarkers.StartOfImage)
                {
                    while (reader.Position < reader.Length)
                    {
                        marker = reader.ReadUInt16();
                        if (marker == 0xFFFF)
                        {
                            // Skip the first padding byte and read the marker again.
                            reader.Position++;
                            continue;
                        }

                        if (marker == JpegMarkers.StartOfScan || marker == JpegMarkers.EndOfImage)
                        {
                            // The application data segments always come before these markers.
                            break;
                        }

                        ushort segmentLength = reader.ReadUInt16();

                        // The segment length field includes its own length in the total.
                        segmentLength -= sizeof(ushort);

                        if (marker == JpegMarkers.App1 && segmentLength >= EXIFSignatureLength)
                        {
                            string sig = reader.ReadAsciiString(EXIFSignatureLength);
                            if (sig.Equals("Exif\0\0", StringComparison.Ordinal))
                            {
                                int exifDataSize = segmentLength - EXIFSignatureLength;

                                byte[] exifBytes = null;

                                if (exifDataSize > 0)
                                {
                                    exifBytes = reader.ReadBytes(exifDataSize);
                                }

                                return exifBytes;
                            }
                        }

                        reader.Position += segmentLength;
                    }
                }

                return null;
            }
        }

        private static class TiffReader
        {
            private enum DataType : ushort
            {
                Byte = 1,
                Ascii = 2,
                Short = 3,
                Long = 4,
                Rational = 5,
                SByte = 6,
                Undefined = 7,
                SShort = 8,
                SLong = 9,
                SRational = 10,
                Float = 11,
                Double = 12
            }

            private struct IFD
            {
                public ushort tag;
                public DataType type;
                public uint count;
                public uint offset;

                public IFD(EndianBinaryReader reader)
                {
                    tag = reader.ReadUInt16();
                    type = (DataType)reader.ReadUInt16();
                    count = reader.ReadUInt32();
                    offset = reader.ReadUInt32();
                }
            }

            private static Endianess? TryDetectTiffByteOrder(Stream stream)
            {
                int byte1 = stream.ReadByte();
                if (byte1 == -1)
                {
                    return null;
                }

                int byte2 = stream.ReadByte();
                if (byte2 == -1)
                {
                    return null;
                }

                if (byte1 == 0x4D && byte2 == 0x4D)
                {
                    return Endianess.Big;
                }
                else if (byte1 == 0x49 && byte2 == 0x49)
                {
                    return Endianess.Little;
                }
                else
                {
                    return null;
                }
            }

            private const ushort TIFFSignature = 42;
            private const ushort XmpTag = 700;

            /// <summary>
            /// Extracts the XMP packet from a TIFF file.
            /// </summary>
            /// <param name="stream">The stream to read.</param>
            /// <returns>The extracted XMP packet, or null.</returns>
            internal static byte[] ExtractXMP(Stream stream)
            {
                byte[] xmpBytes = null;

                stream.Position = 0L;

                try
                {
                    Endianess? byteOrder = TryDetectTiffByteOrder(stream);

                    if (byteOrder.HasValue)
                    {
                        using (EndianBinaryReader reader = new EndianBinaryReader(stream, byteOrder.Value, true))
                        {
                            xmpBytes = ExtractXMPPacket(reader);
                        }
                    }
                }
                catch (EndOfStreamException)
                {
                }

                return xmpBytes;
            }

            /// <summary>
            /// Extracts the XMP packet.
            /// </summary>
            /// <param name="reader">The reader.</param>
            /// <returns>The extracted XMP packet.</returns>
            /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
            private static byte[] ExtractXMPPacket(EndianBinaryReader reader)
            {
                ushort signature = reader.ReadUInt16();

                if (signature == TIFFSignature)
                {
                    uint ifdOffset = reader.ReadUInt32();
                    reader.Position = ifdOffset;

                    int ifdCount = reader.ReadUInt16();

                    for (int i = 0; i < ifdCount; i++)
                    {
                        IFD ifd = new IFD(reader);

                        if (ifd.tag == XmpTag && (ifd.type == DataType.Byte || ifd.type == DataType.Undefined))
                        {
                            if (ifd.count > int.MaxValue)
                            {
                                // The .NET Framework does not support arrays larger than 2 GB.
                                return null;
                            }

                            reader.Position = ifd.offset;

                            return reader.ReadBytes((int)ifd.count);
                        }
                    }
                }

                return null;
            }
        }
    }
}
