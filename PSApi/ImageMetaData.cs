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
		/// Reads the JPEG APP1 section to extract EXIF or XMP meta data.
		/// </summary>
		/// <param name="jpegData">The JPEG image byte array.</param>
		/// <param name="exif">if set to <c>true</c> extract the EXIF meta data; otherwise extract the XMP meta data.</param>
		/// <returns> The extracted data or null.</returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="jpegData"/> is null.</exception>
		private static unsafe byte[] ReadJpegAPP1(byte[] jpegData, bool exif)
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

						string sig;

						if (exif)
						{
							sig = new string((sbyte*)p + 2, 0, 6, Windows1252Encoding);

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
							sig = new string((sbyte*)p + 2, 0, 29, Windows1252Encoding);

							if (sig == "http://ns.adobe.com/xap/1.0/\0")
							{
								// TODO: The XMP extension packets are not supported, so the XMP data must be less that 65502 bytes in size.
								int xmpLength = sectionLength - 31;
								bytes = new byte[xmpLength];

								Marshal.Copy(new IntPtr(p + 31), bytes, 0, xmpLength);
							}

							p += sectionLength;
						}

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

			if (MetaDataConverter.IsJPEGMetaData(metaData))
			{
				try
				{
					if (exif)
					{
						if (metaData.GetQuery("/app1/ifd/exif") == null)
						{
							this.extractedExif = true;
							return false;
						}
					}
					else
					{
						if (metaData.GetQuery("/xmp") == null)
						{
							this.extractedXMP = true;
							return false;
						}
					}
				}
				catch (IOException)
				{
					return false; // WINCODEC_ERR_INVALIDQUERYREQUEST
				}
			}
			else
			{
				BitmapMetadata converted = MetaDataConverter.ConvertMetaDataToJPEG(metaData, exif);

				if (converted == null)
				{
					if (exif)
					{
						this.extractedExif = true;
					}
					else
					{
						this.extractedXMP = true;
					}

					return false;
				}
				metaData = converted;
			}
#endif

			using (MemoryStream ms = new MemoryStream())
			{
#if GDIPLUS
				this.image.Save(ms, ImageFormat.Jpeg);
#else
				JpegBitmapEncoder enc = new JpegBitmapEncoder();
				enc.Frames.Add(BitmapFrame.Create(this.image, null, metaData, null));
				enc.Save(ms);
#endif
				if (exif)
				{
					this.exifBytes = ReadJpegAPP1(ms.GetBuffer(), true);
					this.extractedExif = true;					
					bytes = this.exifBytes;
				}
				else
				{
					this.xmpBytes = ReadJpegAPP1(ms.GetBuffer(), false);
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
	}
}
