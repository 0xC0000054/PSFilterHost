/////////////////////////////////////////////////////////////////////////////////
//
// Adobe(R) Photoshop(R) filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft public License:
//   Copyright (C) 2012-2014 Nicholas Hayes
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
				if (!tiffMetaData.ContainsQuery("/ifd"))
				{
					tiffMetaData.SetQuery("/ifd", new BitmapMetadata("ifd"));
				}

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
				if (!tiffMetaData.ContainsQuery("/ifd/xmp"))
				{
					tiffMetaData.SetQuery("/ifd/xmp", new BitmapMetadata("xmp"));
				}

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
				if (!tiffMetaData.ContainsQuery("/ifd/exif"))
				{
					tiffMetaData.SetQuery("/ifd/exif", new BitmapMetadata("exif"));
				}

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
				if (!tiffMetaData.ContainsQuery("/ifd/xmp"))
				{
					tiffMetaData.SetQuery("/ifd/xmp", new BitmapMetadata("xmp"));
				}

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
				if (!tiffMetaData.ContainsQuery("/ifd/iptc"))
				{
					tiffMetaData.SetQuery("/ifd/iptc", new BitmapMetadata("iptc"));
				}

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

			return exif;
		}

		private static BitmapMetadata GetXMPMetaData(BitmapMetadata metaData, string format)
		{
			BitmapMetadata xmp = null;

			if (format == "png")
			{
				BitmapMetadata textChunk = null;

				try
				{
					textChunk = metaData.GetQuery("/iTXt") as BitmapMetadata;
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
							// PNG stores the XMP meta-data in an iTXt chunk as an UTF8 encoded string, so we have to save it to a dummy tiff and grab the XMP meta-data on load. 
							BitmapMetadata tiffMetaData = new BitmapMetadata("tiff");

							//tiffMetaData.SetQuery("/ifd/xmp", new BitmapMetadata("xmp"));
							tiffMetaData.SetQuery("/ifd/xmp", System.Text.Encoding.UTF8.GetBytes(data));

							using (MemoryStream stream = new MemoryStream())
							{
								BitmapSource source = BitmapSource.Create(1, 1, 96.0, 96.0, System.Windows.Media.PixelFormats.Gray8, null, new byte[] { 255 }, 1);
								TiffBitmapEncoder encoder = new TiffBitmapEncoder();
								encoder.Frames.Add(BitmapFrame.Create(source, null, tiffMetaData, null));
								encoder.Save(stream);

								BitmapDecoder dec = TiffBitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
								
								if (dec.Frames.Count == 1)
								{
									BitmapMetadata meta = dec.Frames[0].Metadata as BitmapMetadata;
									BitmapMetadata block = meta.GetQuery("/ifd/xmp") as BitmapMetadata;

									xmp = block.Clone();
								}                           
																
							}
							
						}
					}
				}
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
					try
					{
						xmp = metaData.GetQuery("/xmp") as BitmapMetadata;
					}
					catch (IOException)
					{
						// WINCODEC_ERR_INVALIDQUERYREQUEST
					}
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
				if (!jpegMetaData.ContainsQuery("/app1/ifd/exif"))
				{
					jpegMetaData.SetQuery("/app1/ifd/exif", new BitmapMetadata("exif"));
				}

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
				if (!jpegMetaData.ContainsQuery("/xmp"))
				{
					jpegMetaData.SetQuery("/xmp", new BitmapMetadata("xmp"));
				}

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
			MemoryStream stream = new MemoryStream();

			try
			{
				// Create a dummy tiff to extract the XMP packet from.
				BitmapSource source = BitmapSource.Create(1, 1, 96.0, 96.0, System.Windows.Media.PixelFormats.Gray8, null, new byte[] { 255 }, 1);
				TiffBitmapEncoder encoder = new TiffBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(source, null, tiffMetaData, null));
				encoder.Save(stream);

				using (BinaryReader reader = new BinaryReader(stream))
				{
					stream = null;

					xmpBytes = TiffReader.ExtractXMP(reader);
				}
			}
			finally
			{
				if (stream != null)
				{
					stream.Dispose();
					stream = null;
				} 
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
			// If the meta-data contains an IFD block (DNG, TIFF etc) we copy it along with any sub IFD blocks, otherwise we only copy the EXIF and/or XMP blocks.
			if (metaData.ContainsQuery("/ifd")) 
			{
				BitmapMetadata ifd = null;
				BitmapMetadata xmp = null;

				try
				{
					ifd = metaData.GetQuery("/ifd") as BitmapMetadata;
				}
				catch (IOException)
				{
					// WINCODEC_ERR_INVALIDQUERYREQUEST
				}

				try
				{
					xmp = metaData.GetQuery("/xmp") as BitmapMetadata; // Some codecs may store the XMP data outside the IFD block.
				}
				catch (IOException)
				{
					// WINCODEC_ERR_INVALIDQUERYREQUEST
				}

				if (ifd == null && xmp == null)
				{
					return null;
				}

				BitmapMetadata wmpMetaData = new BitmapMetadata("wmphoto");

				if (ifd != null)
				{
					if (!wmpMetaData.ContainsQuery("/ifd"))
					{
						wmpMetaData.SetQuery("/ifd", new BitmapMetadata("ifd"));
					}

					foreach (var tag in ifd)
					{
						object value = ifd.GetQuery(tag);

						BitmapMetadata ifdSub = value as BitmapMetadata;

						if (ifdSub != null)
						{
							string baseQuery = "/ifd" + tag;

							CopySubIFDRecursive(ref wmpMetaData, ifdSub, baseQuery);
						}
						else
						{
							wmpMetaData.SetQuery("/ifd" + tag, value);
						}
					}
				}

				if (xmp != null)
				{
					if (!wmpMetaData.ContainsQuery("/ifd/xmp"))
					{
						wmpMetaData.SetQuery("/ifd/xmp", new BitmapMetadata("xmp"));
					}

					foreach (var tag in xmp)
					{
						object value = xmp.GetQuery(tag);

						BitmapMetadata xmpSub = value as BitmapMetadata;

						if (xmpSub != null)
						{
							string baseQuery = "/ifd/xmp" + tag;

							CopySubIFDRecursive(ref wmpMetaData, xmpSub, baseQuery);
						}
						else
						{
							wmpMetaData.SetQuery("/ifd/xmp" + tag, value);
						}
					}
				}

				return wmpMetaData;
			}
			else
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
					if (!wmpMetaData.ContainsQuery("/ifd/exif"))
					{
						wmpMetaData.SetQuery("/ifd/exif", new BitmapMetadata("exif"));
					}

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
					if (!wmpMetaData.ContainsQuery("/ifd/xmp"))
					{
						wmpMetaData.SetQuery("/ifd/xmp", new BitmapMetadata("xmp"));
					}

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
				public ushort tag;
				public DataType type;
				public uint count;
				public uint offset;

				public IFD(BinaryReader reader, bool littleEndian)
				{
					this.tag = ReadShort(reader, littleEndian);
					this.type = (DataType)ReadShort(reader, littleEndian);
					this.count = ReadLong(reader, littleEndian);
					this.offset = ReadLong(reader, littleEndian);
				}
			}

			private static ushort ReadShort(BinaryReader reader, bool littleEndian)
			{
				byte byte0 = reader.ReadByte();
				byte byte1 = reader.ReadByte();

				if (littleEndian)
				{
					return (ushort)(byte0 | (byte1 << 8));
				}
				else
				{
					return (ushort)((byte0 << 8) | byte1);
				}
			}

			private static uint ReadLong(BinaryReader reader, bool littleEndian)
			{
				byte byte0 = reader.ReadByte();
				byte byte1 = reader.ReadByte();
				byte byte2 = reader.ReadByte();
				byte byte3 = reader.ReadByte();

				if (littleEndian)
				{
					return (ushort)(byte0 | (((byte1 << 8) | (byte2 << 16)) | (byte3 << 24)));
				}
				else
				{
					return (ushort)((((byte0 << 8) | (byte1 << 16)) | (byte2 << 24)) | byte3);
				}
			}

			/// <summary>
			/// Extracts the XMP packet from a TIFF file.
			/// </summary>
			/// <param name="reader">The reader.</param>
			/// <returns>The extracted XMP packet, or null.</returns>
			internal static byte[] ExtractXMP(BinaryReader reader)
			{
				reader.BaseStream.Position = 0L;

				ushort byteOrder = reader.ReadUInt16();

				bool littleEndian = byteOrder == 0x4949;

				reader.BaseStream.Position += 2L; // skip the TIFF signature.

				uint ifdOffset = ReadLong(reader, littleEndian);
				reader.BaseStream.Seek((long)ifdOffset, SeekOrigin.Begin);

				int ifdCount = ReadShort(reader, littleEndian);
				
				IFD xmpIfd = new IFD();
 
				for (int i = 0; i < ifdCount; i++)
				{
					IFD ifd = new IFD(reader, littleEndian);

					if (ifd.tag == 700)
					{
						xmpIfd = ifd;
						break;
					}
				}

				if (xmpIfd.tag != 0 && (xmpIfd.type == DataType.Byte || xmpIfd.type == DataType.Undefined))
				{
					reader.BaseStream.Seek((long)xmpIfd.offset, SeekOrigin.Begin);

					byte[] bytes = reader.ReadBytes((int)xmpIfd.count);

					return bytes;
				}

				return null;
			}
		}

	}
}
