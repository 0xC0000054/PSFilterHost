/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using System.Text;

#if GDIPLUS
using System.Drawing;
#else
using System.Windows.Media.Imaging;
#endif

namespace PSFilterHostDll.PSApi
{
	internal sealed class ImageMetaData : IDisposable
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
		/// Initializes a new instance of the <see cref="ImageMetaData"/> class.
		/// </summary>
		/// <param name="image">The image that contains the meta data.</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="image"/> is null.</exception>
#if GDIPLUS
		public ImageMetaData(Bitmap image)
#else
		public ImageMetaData(BitmapSource image)
#endif
		{
			if (image == null)
			{
				throw new ArgumentNullException("image");
			}

			this.image = image;
			this.exifBytes = null;
			this.xmpBytes = null;
			this.extractedExif = false;
			this.extractedXMP = false;
			this.disposed = false;
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
			if (this.disposed)
			{
				throw new ObjectDisposedException("ImageMetaData");
			}

			bytes = null;

			// Return the cached data if it has already been extracted.
			if (exif)
			{
				if (this.extractedExif)
				{
					bytes = this.exifBytes;
					return (bytes != null);
				}
			}
			else
			{
				if (this.extractedXMP)
				{
					bytes = this.xmpBytes;
					return (bytes != null);
				}
			}

#if !GDIPLUS
			BitmapMetadata metaData = null;

			try
			{
				metaData = this.image.Metadata as BitmapMetadata;
			}
			catch (NotSupportedException)
			{
			}

			if (metaData == null)
			{
				this.extractedExif = true;
				this.extractedXMP = true;

				return false;
			}

			if (exif)
			{
				BitmapMetadata exifMetaData = MetaDataConverter.GetEXIFMetaData(metaData);

				if (exifMetaData == null)
				{
					this.extractedExif = true;

					return false;
				}
				metaData = exifMetaData;
			}
			else
			{
				BitmapMetadata xmpMetaData = MetaDataConverter.GetXMPMetaData(metaData);

				if (xmpMetaData == null)
				{
					this.extractedXMP = true;

					return false;
				}
				metaData = xmpMetaData;
			}
#endif

			if (exif)
			{
				using (MemoryStream ms = new MemoryStream())
				{
#if GDIPLUS
					this.image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
#else
					JpegBitmapEncoder enc = new JpegBitmapEncoder();
					enc.Frames.Add(BitmapFrame.Create(this.image, null, metaData, null));
					enc.Save(ms);
#endif
					this.exifBytes = JpegReader.ExtractEXIF(ms.GetBuffer());
					this.extractedExif = true;
					bytes = this.exifBytes;
				}
			}
			else
			{
				using (MemoryStream ms = new MemoryStream())
				{
#if GDIPLUS
					this.image.Save(ms, System.Drawing.Imaging.ImageFormat.Tiff);
#else
					TiffBitmapEncoder enc = new TiffBitmapEncoder();
					enc.Frames.Add(BitmapFrame.Create(this.image, null, metaData, null));
					enc.Save(ms);
#endif
					this.xmpBytes = TiffReader.ExtractXMP(ms);
					this.extractedXMP = true;
					bytes = this.xmpBytes;
				}
			}

			return (bytes != null);
		}

		public void Dispose()
		{
			if (!this.disposed)
			{
				this.disposed = true;

#if GDIPLUS
				if (this.image != null)
				{
					this.image.Dispose();
					this.image = null;
				} 
#endif

				this.exifBytes = null;
				this.xmpBytes = null;
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
			private const int EXIFSegmentHeaderLength = sizeof(ushort) + EXIFSignatureLength;

			private static ushort ReadUInt16BigEndian(byte[] buffer, int startIndex)
			{
				return (ushort)((buffer[startIndex] << 8) | buffer[startIndex + 1]);
			}

			/// <summary>
			/// Extracts the EXIF data from a JPEG image.
			/// </summary>
			/// <param name="jpegBytes">The JPEG image bytes.</param>
			/// <returns>The extracted EXIF data, or null.</returns>
			internal static byte[] ExtractEXIF(byte[] jpegBytes)
			{
				try
				{
					if (jpegBytes.Length > 2)
					{
						ushort marker = ReadUInt16BigEndian(jpegBytes, 0);

						// Check the file signature.
						if (marker == JpegMarkers.StartOfImage)
						{
							int index = 2;
							int length = jpegBytes.Length;

							while (index < length)
							{
								marker = ReadUInt16BigEndian(jpegBytes, index);
								if (marker == 0xFFFF)
								{
									// Skip the first padding byte and read the marker again.
									index++;
									continue;
								}
								else
								{
									index += 2;
								}

								if (marker == JpegMarkers.StartOfScan || marker == JpegMarkers.EndOfImage)
								{
									// The application data segments always come before these markers.
									break;
								}

								// The segment length field includes its own length in the total.
								// The index is not incremented after reading it to avoid having to subtract
								// 2 bytes from the length when skipping a segment.
								ushort segmentLength = ReadUInt16BigEndian(jpegBytes, index);

								if (marker == JpegMarkers.App1 && segmentLength >= EXIFSegmentHeaderLength)
								{
									string sig = Encoding.UTF8.GetString(jpegBytes, index + 2, EXIFSignatureLength);
									if (sig.Equals("Exif\0\0", StringComparison.Ordinal))
									{
										int exifDataSize = segmentLength - EXIFSegmentHeaderLength;
										byte[] exifData = null;

										if (exifDataSize > 0)
										{
											exifData = new byte[exifDataSize];
											Buffer.BlockCopy(jpegBytes, index + EXIFSegmentHeaderLength, exifData, 0, exifDataSize);
										}

										return exifData;
									}
								}

								index += segmentLength;
							}
						} 
					}
				}
				catch (IndexOutOfRangeException)
				{
				}

				return null;
			}
		}

		private static class TiffReader
		{
			enum DataType : ushort
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

			struct IFD
			{
				public ushort tag;
				public DataType type;
				public uint count;
				public uint offset;

				public IFD(Stream stream, bool littleEndian)
				{
					this.tag = ReadShort(stream, littleEndian);
					this.type = (DataType)ReadShort(stream, littleEndian);
					this.count = ReadLong(stream, littleEndian);
					this.offset = ReadLong(stream, littleEndian);
				}
			}

			private static ushort ReadShort(Stream stream, bool littleEndian)
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

				if (littleEndian)
				{
					return (ushort)(byte1 | (byte2 << 8));
				}
				else
				{
					return (ushort)((byte1 << 8) | byte2);
				}
			}

			private static uint ReadLong(Stream stream, bool littleEndian)
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

				if (littleEndian)
				{
					return (uint)(byte1 | (((byte2 << 8) | (byte3 << 16)) | (byte4 << 24)));
				}
				else
				{
					return (uint)((((byte1 << 24) | (byte2 << 16)) | (byte3 << 8)) | byte4);
				}
			}

			private const ushort LittleEndianByteOrder = 0x4949;
			private const ushort TIFFSignature = 42;
			private const ushort XmpTag = 700;

			/// <summary>
			/// Extracts the XMP packet from a TIFF file.
			/// </summary>
			/// <param name="stream">The stream to read.</param>
			/// <returns>The extracted XMP packet, or null.</returns>
			internal static byte[] ExtractXMP(Stream stream)
			{
				stream.Position = 0L;

				try
				{
					ushort byteOrder = ReadShort(stream, false);

					bool littleEndian = byteOrder == LittleEndianByteOrder;

					ushort signature = ReadShort(stream, littleEndian);

					if (signature == TIFFSignature)
					{
						uint ifdOffset = ReadLong(stream, littleEndian);
						stream.Seek(ifdOffset, SeekOrigin.Begin);

						int ifdCount = ReadShort(stream, littleEndian);

						for (int i = 0; i < ifdCount; i++)
						{
							IFD ifd = new IFD(stream, littleEndian);

							if (ifd.tag == XmpTag && (ifd.type == DataType.Byte || ifd.type == DataType.Undefined))
							{
								stream.Seek(ifd.offset, SeekOrigin.Begin);

								int count = (int)ifd.count;

								byte[] xmpBytes = new byte[count];

								int numBytesToRead = count;
								int numBytesRead = 0;
								do
								{
									int n = stream.Read(xmpBytes, numBytesRead, numBytesToRead);
									numBytesRead += n;
									numBytesToRead -= n;
								} while (numBytesToRead > 0);

								return xmpBytes;
							}
						}
					}
				}
				catch (EndOfStreamException)
				{
				}

				return null;
			}
		}
	}
}
