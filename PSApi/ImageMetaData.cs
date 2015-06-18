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
using System.Runtime.InteropServices;
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

		private static readonly Encoding Windows1252Encoding = Encoding.GetEncoding(1252);

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
		/// Reads the JPEG APP1 section to extract the EXIF meta data.
		/// </summary>
		/// <param name="jpegData">The JPEG image byte array.</param>
		/// <returns> The extracted data or null.</returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="jpegData"/> is null.</exception>
		private static unsafe byte[] ExtractEXIFMetaData(byte[] jpegData)
		{
			if (jpegData == null)
			{
				throw new ArgumentNullException("jpegData");
			}

			byte[] bytes = null;
			fixed (byte* ptr = jpegData)
			{
				byte* p = ptr;
				if (p[0] != 0xff && p[1] != 0xd8) // JPEG file signature
				{
					return null;
				}
				p += 2;

				while ((p[0] == 0xff && (p[1] >= 0xe0 && p[1] <= 0xef)) && bytes == null) // APP sections
				{
					int sectionLength = ((p[2] << 8) | p[3]); // JPEG uses big-endian   

					if (p[0] == 0xff && p[1] == 0xe1) // APP1
					{
						p += 2; // skip the header bytes

						string sig = new string((sbyte*)p + 2, 0, 6, Windows1252Encoding);

						if (sig == "Exif\0\0")
						{
							int exifLength = sectionLength - 8; // subtract the signature and section length size to get the data length. 
							bytes = new byte[exifLength];

							Marshal.Copy(new IntPtr(p + 8), bytes, 0, exifLength);
						}

						p += sectionLength;
					}
					else
					{
						p += sectionLength + 2;
					}
				}
			}

			return bytes;
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
					this.image.Save(ms, ImageFormat.Jpeg);
#else
					JpegBitmapEncoder enc = new JpegBitmapEncoder();
					enc.Frames.Add(BitmapFrame.Create(this.image, null, metaData, null));
					enc.Save(ms);
#endif
					this.exifBytes = ExtractEXIFMetaData(ms.GetBuffer());
					this.extractedExif = true;
					bytes = this.exifBytes;
				}
			}
			else
			{
				using (MemoryStream ms = new MemoryStream())
				{
#if GDIPLUS
					this.image.Save(ms, ImageFormat.Tiff);
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
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				this.disposed = true;

				if (disposing)
				{
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
						stream.Seek((long)ifdOffset, SeekOrigin.Begin);

						int ifdCount = ReadShort(stream, littleEndian);

						for (int i = 0; i < ifdCount; i++)
						{
							IFD ifd = new IFD(stream, littleEndian);

							if (ifd.tag == XmpTag && (ifd.type == DataType.Byte || ifd.type == DataType.Undefined))
							{
								stream.Seek((long)ifd.offset, SeekOrigin.Begin);

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
