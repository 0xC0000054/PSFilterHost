/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft public License:
//   Copyright (C) 2012-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace HostTest
{
	internal static class MetaDataHelper
	{
		private static void CopySubIFDRecursive(ref BitmapMetadata parent, BitmapMetadata ifd, string query)
		{
			if (!parent.ContainsQuery(query))
			{
				parent.SetQuery(query, new BitmapMetadata(ifd.Format));
			}

			foreach (var tag in ifd)
			{
				object value = ifd.GetQuery(tag);

				BitmapMetadata ifdSub = value as BitmapMetadata;

				if (ifdSub != null)
				{
					CopySubIFDRecursive(ref parent, ifdSub, query + tag);
				}
				else
				{
					parent.SetQuery(query + tag, value);
				}
			}
		}

		#region TIFF conversion
		private static BitmapMetadata ConvertIFDMetadata(BitmapMetadata source)
		{
			BitmapMetadata ifd = null;
			BitmapMetadata xmp = null;

			try
			{
				ifd = source.GetQuery("/ifd") as BitmapMetadata;
			}
			catch (IOException)
			{
				// WINCODEC_ERR_INVALIDQUERYREQUEST
			}

			try
			{
				xmp = source.GetQuery("/xmp") as BitmapMetadata; // Some codecs may store the XMP data outside the IFD block.
			}
			catch (IOException)
			{
				// WINCODEC_ERR_INVALIDQUERYREQUEST
			}

			if (ifd == null && xmp == null)
			{
				return null;
			}

			BitmapMetadata tiffMetaData = new BitmapMetadata("tiff");

			if (ifd != null)
			{
				tiffMetaData.SetQuery("/ifd", new BitmapMetadata("ifd"));

				foreach (var tag in ifd)
				{
					object value = ifd.GetQuery(tag);

					BitmapMetadata ifdSub = value as BitmapMetadata;

					if (ifdSub != null)
					{
						string baseQuery = "/ifd" + tag;

						CopySubIFDRecursive(ref tiffMetaData, ifdSub, baseQuery);
					}
					else
					{
						tiffMetaData.SetQuery("/ifd" + tag, value);
					}
				}
			}

			if (xmp != null)
			{
				tiffMetaData.SetQuery("/ifd/xmp", new BitmapMetadata("xmp"));

				foreach (var tag in xmp)
				{
					object value = xmp.GetQuery(tag);

					BitmapMetadata xmpSub = value as BitmapMetadata;

					if (xmpSub != null)
					{
						string baseQuery = "/ifd/xmp" + tag;

						CopySubIFDRecursive(ref tiffMetaData, xmpSub, baseQuery);
					}
					else
					{
						tiffMetaData.SetQuery("/ifd/xmp" + tag, value);
					}
				}
			}

			return tiffMetaData;
		}

		private static BitmapMetadata ConvertJPEGMetaData(BitmapMetadata jpegMetaData)
		{
			BitmapMetadata exif = null;
			BitmapMetadata xmp = null;
			BitmapMetadata iptc = null;

			try
			{
				exif = jpegMetaData.GetQuery("/app1/ifd/exif") as BitmapMetadata;
			}
			catch (IOException)
			{
				// WINCODEC_ERR_INVALIDQUERYREQUEST
			}

			try
			{
				xmp = jpegMetaData.GetQuery("/xmp") as BitmapMetadata;
			}
			catch (IOException)
			{
				// WINCODEC_ERR_INVALIDQUERYREQUEST
			}

			try
			{
				iptc = jpegMetaData.GetQuery("/app13/irb/8bimiptc/iptc") as BitmapMetadata;
			}
			catch (IOException)
			{
				// WINCODEC_ERR_INVALIDQUERYREQUEST
			}

			if (exif == null && xmp == null)
			{
				return null;
			}

			BitmapMetadata tiffMetaData = new BitmapMetadata("tiff");

			if (exif != null)
			{
				tiffMetaData.SetQuery("/ifd/exif", new BitmapMetadata("exif"));

				foreach (var tag in exif)
				{
					object value = exif.GetQuery(tag);

					BitmapMetadata exifSub = value as BitmapMetadata;

					if (exifSub != null)
					{
						string baseQuery = "/ifd/exif" + tag;

						CopySubIFDRecursive(ref tiffMetaData, exifSub, baseQuery);
					}
					else
					{
						tiffMetaData.SetQuery("/ifd/exif" + tag, value);
					}
				}
			}

			if (xmp != null)
			{
				tiffMetaData.SetQuery("/ifd/xmp", new BitmapMetadata("xmp"));

				foreach (var tag in xmp)
				{
					object value = xmp.GetQuery(tag);

					BitmapMetadata xmpSub = value as BitmapMetadata;

					if (xmpSub != null)
					{
						string baseQuery = "/ifd/xmp" + tag;

						CopySubIFDRecursive(ref tiffMetaData, xmpSub, baseQuery);
					}
					else
					{
						tiffMetaData.SetQuery("/ifd/xmp" + tag, value);
					}
				}
			}

			if (iptc != null)
			{
				tiffMetaData.SetQuery("/ifd/iptc", new BitmapMetadata("iptc"));

				foreach (var tag in iptc)
				{
					object value = iptc.GetQuery(tag);

					BitmapMetadata iptcSub = value as BitmapMetadata;

					if (iptcSub != null)
					{
						string baseQuery = "/ifd/iptc" + tag;

						CopySubIFDRecursive(ref tiffMetaData, iptcSub, baseQuery);
					}
					else
					{
						tiffMetaData.SetQuery("/ifd/iptc" + tag, value);
					}
				}
			}

			return tiffMetaData;
		}

		private static BitmapMetadata ConvertPNGMetaData(BitmapMetadata metadata)
		{
			BitmapMetadata textChunk = null;

			try
			{
				textChunk = metadata.GetQuery("/iTXt") as BitmapMetadata;
			}
			catch (IOException)
			{
				// WINCODEC_ERR_INVALIDQUERYREQUEST
			}

			if (textChunk != null)
			{
				string keyWord = textChunk.GetQuery("/Keyword") as string;

				if ((keyWord != null) && keyWord == "XML:com.adobe.xmp")
				{
					string data = textChunk.GetQuery("/TextEntry") as string;

					if (data != null)
					{
						BitmapMetadata tiffMetaData = new BitmapMetadata("tiff");
						tiffMetaData.SetQuery("/ifd/xmp", new BitmapMetadata("xmp"));

						tiffMetaData.SetQuery("/ifd/xmp", System.Text.Encoding.UTF8.GetBytes(data)); // The XMP specification requires TIFF XMP meta-data to be UTF8 encoded.      

						return tiffMetaData;
					}
				}

			}

			return null;
		}
		
		#endregion        
		/// <summary>
		/// Converts the meta-data to TIFF format.
		/// </summary>
		/// <param name="metaData">The meta data.</param>
		/// <returns>The converted meta data or null</returns>
		internal static BitmapMetadata ConvertMetaDataToTIFF(BitmapMetadata metaData)
		{
			if (metaData == null)
			{
				return null;
			}

			string format = string.Empty;

			try
			{
				format = metaData.Format; // Some WIC codecs do not implement the format property.
			}
			catch (ArgumentException)
			{
			}
			catch (NotSupportedException)
			{
			}
			
			if (format != "tiff")
			{
				if (format == "gif")
				{
					return null; // GIF files do not contain frame level EXIF or XMP meta data.
				}
				else if (format == "jpg")
				{
					return ConvertJPEGMetaData(metaData);
				}
				else if (format == "png")
				{
					return ConvertPNGMetaData(metaData);
				}
				else
				{
					return ConvertIFDMetadata(metaData);
				}
			}

			return metaData;
		}

		/// <summary>
		/// Gets the IPTC caption of the image.
		/// </summary>
		/// <param name="image">The image.</param>
		/// <returns></returns>
		internal static string GetIPTCCaption(BitmapSource image)
		{
			BitmapMetadata metaData = null;

			try
			{
				metaData = image.Metadata as BitmapMetadata;
			}
			catch (NotSupportedException)
			{
			}

			if (metaData != null)
			{
				string iptcCaption = null;

				try
				{
					iptcCaption = metaData.GetQuery("/ifd/iptc/{str=Caption}") as string;
				}
				catch (IOException)
				{
					// WINCODEC_ERR_INVALIDQUERYREQUEST
				}

				if (iptcCaption == null)
				{
					try
					{
						iptcCaption = metaData.GetQuery("/ifd/xmp/dc:description") as string;
					}
					catch (IOException)
					{
						// WINCODEC_ERR_INVALIDQUERYREQUEST
					}
				}

				return iptcCaption;
			}

			return null;
		}

		#region Save format conversion
		private static BitmapMetadata GetEXIFMetaData(BitmapMetadata metaData, string format)
		{
			BitmapMetadata exif = null;
			// GIF and PNG files do not contain EXIF meta data.
			if (format != "gif" && format != "png")
			{
				try
				{
					if (format == "jpg")
					{
						exif = metaData.GetQuery("/app1/ifd/exif") as BitmapMetadata;
					}
					else
					{
						exif = metaData.GetQuery("/ifd/exif") as BitmapMetadata;
					}
				}
				catch (IOException)
				{
					// WINCODEC_ERR_INVALIDQUERYREQUEST
				} 
			}

			return exif;
		}

		/// <summary>
		/// Loads the PNG XMP meta data using a dummy TIFF.
		/// </summary>
		/// <param name="xmp">The XMP string to load.</param>
		/// <returns>The loaded XMP block, or null.</returns>
		private static BitmapMetadata LoadPNGMetaData(string xmp)
		{
			BitmapMetadata xmpData = null;

			using (MemoryStream stream = new MemoryStream())
			{
				// PNG stores the XMP meta-data in an iTXt chunk as an UTF8 encoded string,
				// so we have to save it to a dummy tiff and grab the XMP meta-data on load. 
				BitmapMetadata tiffMetaData = new BitmapMetadata("tiff");
				tiffMetaData.SetQuery("/ifd/xmp", new BitmapMetadata("xmp"));
				tiffMetaData.SetQuery("/ifd/xmp", System.Text.Encoding.UTF8.GetBytes(xmp));

				BitmapSource source = BitmapSource.Create(1, 1, 96.0, 96.0, System.Windows.Media.PixelFormats.Gray8, null, new byte[] { 255 }, 1);
				TiffBitmapEncoder encoder = new TiffBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(source, null, tiffMetaData, null));
				encoder.Save(stream);

				TiffBitmapDecoder dec = new TiffBitmapDecoder(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);

				if (dec.Frames.Count == 1)
				{
					BitmapMetadata meta = dec.Frames[0].Metadata as BitmapMetadata;
					if (meta != null)
					{
						xmpData = meta.GetQuery("/ifd/xmp") as BitmapMetadata;
					}
				}
			}

			return xmpData;
		}

		private static BitmapMetadata GetXMPMetaData(BitmapMetadata metaData, string format)
		{
			BitmapMetadata xmp = null;

			// GIF files do not contain frame level XMP meta data.
			if (format != "gif")
			{
				try
				{
					if (format == "png")
					{
						BitmapMetadata textChunk = metaData.GetQuery("/iTXt") as BitmapMetadata;

						if (textChunk != null)
						{
							string keyWord = textChunk.GetQuery("/Keyword") as string;

							if ((keyWord != null) && keyWord == "XML:com.adobe.xmp")
							{
								string data = textChunk.GetQuery("/TextEntry") as string;

								if (!string.IsNullOrEmpty(data))
								{
									xmp = LoadPNGMetaData(data);
								}
							} 
						}
					}
					else if (format == "jpg")
					{
						xmp = metaData.GetQuery("/xmp") as BitmapMetadata;
					}
					else
					{
						try
						{
							xmp = metaData.GetQuery("/ifd/xmp") as BitmapMetadata;
						}
						catch (IOException)
						{
							// WINCODEC_ERR_INVALIDQUERYREQUEST
						}

						if (xmp == null)
						{
							// Some codecs may store the XMP data outside of the IFD block.
							xmp = metaData.GetQuery("/xmp") as BitmapMetadata;
						}
					}
				}
				catch (IOException)
				{
					// WINCODEC_ERR_INVALIDQUERYREQUEST
				}
			}

			return xmp;
		}

		private static BitmapMetadata ConvertMetaDataToJPEG(BitmapMetadata metaData, string format)
		{
			BitmapMetadata exif = GetEXIFMetaData(metaData, format);
			BitmapMetadata xmp = GetXMPMetaData(metaData, format);

			if (exif == null && xmp == null)
			{
				return null;
			}

			BitmapMetadata jpegMetaData = new BitmapMetadata("jpg");

			if (exif != null)
			{
				jpegMetaData.SetQuery("/app1/ifd/exif", new BitmapMetadata("exif"));

				foreach (var tag in exif)
				{
					object value = exif.GetQuery(tag);

					BitmapMetadata exifSub = value as BitmapMetadata;

					if (exifSub != null)
					{
						string baseQuery = "/app1/ifd/exif" + tag;

						CopySubIFDRecursive(ref jpegMetaData, exifSub, baseQuery);
					}
					else
					{
						jpegMetaData.SetQuery("/app1/ifd/exif" + tag, value);
					}
				}

			}

			if (xmp != null)
			{
				jpegMetaData.SetQuery("/xmp", new BitmapMetadata("xmp"));

				foreach (var tag in xmp)
				{
					object value = xmp.GetQuery(tag);

					BitmapMetadata xmpSub = value as BitmapMetadata;

					if (xmpSub != null)
					{
						CopySubIFDRecursive(ref jpegMetaData, xmpSub, "/xmp" + tag);
					}
					else
					{
						jpegMetaData.SetQuery("/xmp" + tag, value);
					}
				}
			}

			return jpegMetaData;
		}

		private static byte[] ExtractXMPPacket(BitmapMetadata xmp)
		{
			BitmapMetadata tiffMetaData = new BitmapMetadata("tiff");
			tiffMetaData.SetQuery("/ifd/xmp", new BitmapMetadata("xmp"));

			foreach (var tag in xmp)
			{
				object value = xmp.GetQuery(tag);

				BitmapMetadata xmpSub = value as BitmapMetadata;

				if (xmpSub != null)
				{
					CopySubIFDRecursive(ref tiffMetaData, xmpSub, "/ifd/xmp" + tag);
				}
				else
				{
					tiffMetaData.SetQuery("/ifd/xmp" + tag, value);
				}
			}

			byte[] xmpBytes = null;
			
			using (MemoryStream stream = new MemoryStream())
			{
				// Create a dummy tiff to extract the XMP packet from.
				BitmapSource source = BitmapSource.Create(1, 1, 96.0, 96.0, System.Windows.Media.PixelFormats.Gray8, null, new byte[] { 255 }, 1);
				TiffBitmapEncoder encoder = new TiffBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(source, null, tiffMetaData, null));
				encoder.Save(stream);

				xmpBytes = TiffReader.ExtractXMP(stream);
			}

			return xmpBytes;
		}

		private static BitmapMetadata ConvertMetaDataToPNG(BitmapMetadata metaData, string format)
		{
			BitmapMetadata xmp = GetXMPMetaData(metaData, format);

			if (xmp != null)
			{
				byte[] packet = ExtractXMPPacket(xmp);

				if (packet != null)
				{
					BitmapMetadata pngMetaData = new BitmapMetadata("png");
					pngMetaData.SetQuery("/iTXt", new BitmapMetadata("iTXt"));

					// The Keyword property is an ANSI string (VT_LPSTR), which must passed as a char array in order for it to be marshaled correctly.
					char[] keyWordChars = "XML:com.adobe.xmp".ToCharArray();
					pngMetaData.SetQuery("/iTXt/Keyword", keyWordChars);

					pngMetaData.SetQuery("/iTXt/TextEntry", System.Text.Encoding.UTF8.GetString(packet));

					return pngMetaData;
				}
			}

			return null;
		}

		private static BitmapMetadata ConvertMetaDataToWMPhoto(BitmapMetadata metaData, string format)
		{
			BitmapMetadata exif = GetEXIFMetaData(metaData, format);
			BitmapMetadata xmp = GetXMPMetaData(metaData, format);

			if (exif == null && xmp == null)
			{
				return null;
			}

			BitmapMetadata wmpMetaData = new BitmapMetadata("wmphoto");

			if (exif != null)
			{
				wmpMetaData.SetQuery("/ifd/exif", new BitmapMetadata("exif"));

				foreach (var tag in exif)
				{
					object value = exif.GetQuery(tag);

					BitmapMetadata exifSub = value as BitmapMetadata;

					if (exifSub != null)
					{
						string baseQuery = "/ifd/exif" + tag;

						CopySubIFDRecursive(ref wmpMetaData, exifSub, baseQuery);
					}
					else
					{
						wmpMetaData.SetQuery("/ifd/exif" + tag, value);
					}
				}

			}

			if (xmp != null)
			{
				wmpMetaData.SetQuery("/ifd/xmp", new BitmapMetadata("xmp"));

				foreach (var tag in xmp)
				{
					object value = xmp.GetQuery(tag);

					BitmapMetadata xmpSub = value as BitmapMetadata;

					if (xmpSub != null)
					{
						CopySubIFDRecursive(ref wmpMetaData, xmpSub, "/ifd/xmp" + tag);
					}
					else
					{
						wmpMetaData.SetQuery("/ifd/xmp" + tag, value);
					}
				}
			}

			return wmpMetaData;
		}
		#endregion

		/// <summary>
		/// Converts the meta-data to the encoder format.
		/// </summary>
		/// <param name="metaData">The meta-data.</param>
		/// <param name="encoder">The encoder.</param>
		/// <returns>The converted meta-data, or null.</returns>
		internal static BitmapMetadata ConvertSaveMetaDataFormat(BitmapMetadata metaData, BitmapEncoder encoder)
		{
			string format = string.Empty;

			try
			{
				format = metaData.Format; // Some WIC codecs do not implement the format property.
			}
			catch (ArgumentException)
			{
			}
			catch (NotSupportedException)
			{
			}            
			
			Type encoderType = encoder.GetType();
			
			if (encoderType == typeof(TiffBitmapEncoder))
			{
				if (format == "tiff")
				{
					return metaData;
				}
				else
				{
					return ConvertMetaDataToTIFF(metaData); 
				}
			}
			else if (encoderType == typeof(JpegBitmapEncoder))
			{
				if (format == "jpg")
				{
					return metaData;
				}
				else
				{
					return ConvertMetaDataToJPEG(metaData, format);
				}
			}
			else if (encoderType == typeof(PngBitmapEncoder))
			{
				if (format == "png")
				{
					return metaData;
				}
				else
				{
					return ConvertMetaDataToPNG(metaData, format);
				}
			}
			else if (encoderType == typeof(WmpBitmapEncoder))
			{
				if (format == "wmphoto")
				{
					return metaData;
				}
				else
				{
					return ConvertMetaDataToWMPhoto(metaData, format);
				}
			}

			return null;
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
				public readonly ushort tag;
				public readonly DataType type;
				public readonly uint count;
				public readonly uint offset;

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
