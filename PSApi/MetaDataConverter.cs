/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2016 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

#if !GDIPLUS
using System;
using System.Linq;
using System.IO;
using System.Windows.Media.Imaging;

namespace PSFilterHostDll.PSApi
{
    internal static class MetaDataConverter
    {
        private static void CopySubBlockRecursive(ref BitmapMetadata parent, BitmapMetadata child, string query)
        {
            if (!parent.ContainsQuery(query))
            {
                parent.SetQuery(query, new BitmapMetadata(child.Format));
            }

            foreach (var tag in child)
            {
                object value = child.GetQuery(tag);

                BitmapMetadata subBlock = value as BitmapMetadata;

                if (subBlock != null)
                {
                    CopySubBlockRecursive(ref parent, subBlock, query + tag);
                }
                else
                {
                    parent.SetQuery(query + tag, value);
                }
            }
        }

        /// <summary>
        /// Converts the EXIF meta data to JPEG format.
        /// </summary>
        /// <param name="metaData">The meta data to convert.</param>
        /// <returns>The converted meta data or null.</returns>
        private static BitmapMetadata ConvertEXIFMetaData(BitmapMetadata metaData)
        {
            BitmapMetadata exifData = null;

            try
            {
                exifData = metaData.GetQuery("/ifd/exif") as BitmapMetadata;
            }
            catch (IOException)
            {
                // WINCODEC_ERR_INVALIDQUERYREQUEST
            }

            // Return null if the EXIF block does not contain any data.
            if ((exifData == null) || !exifData.Any())
            {
                return null;
            }

            BitmapMetadata jpegMetaData = new BitmapMetadata("jpg");
            jpegMetaData.SetQuery("/app1/ifd/exif", new BitmapMetadata("exif"));

            foreach (var tag in exifData)
            {
                object value = exifData.GetQuery(tag);
                BitmapMetadata exifSub = value as BitmapMetadata;

                if (exifSub != null)
                {
                    CopySubBlockRecursive(ref jpegMetaData, exifSub, "/app1/ifd/exif" + tag);
                }
                else
                {
                    jpegMetaData.SetQuery("/app1/ifd/exif" + tag, value);
                }
            }

            // Set the fields that are relevant for EXIF.
            try
            {
                if (!string.IsNullOrEmpty(metaData.ApplicationName))
                {
                    jpegMetaData.ApplicationName = metaData.ApplicationName;
                }

                if (!string.IsNullOrEmpty(metaData.CameraManufacturer))
                {
                    jpegMetaData.CameraManufacturer = metaData.CameraManufacturer;
                }

                if (!string.IsNullOrEmpty(metaData.CameraModel))
                {
                    jpegMetaData.CameraModel = metaData.CameraModel;
                }
            }
            catch (NotSupportedException)
            {
            }

            return jpegMetaData;
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
                // PNG stores the XMP meta-data in an iTXt chunk as an UTF8 encoded string, so we have to save it to a dummy tiff and grab the XMP meta-data on load. 
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

        /// <summary>
        /// Converts the XMP meta data to TIFF format.
        /// </summary>
        /// <param name="metaData">The meta data to convert.</param>
        /// <param name="format">The format of the meta data.</param>
        /// <returns>The converted meta data or null.</returns>
        private static BitmapMetadata ConvertXMPMetaData(BitmapMetadata metaData, string format)
        {
            BitmapMetadata xmpData = null;

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
                            string textEntry = textChunk.GetQuery("/TextEntry") as string;

                            if (!string.IsNullOrEmpty(textEntry))
                            {
                                xmpData = LoadPNGMetaData(textEntry);
                            }
                        }
                    }
                }
                else if (format == "jpg")
                {
                    xmpData = metaData.GetQuery("/xmp") as BitmapMetadata;
                }
                else
                {
                    try
                    {
                        xmpData = metaData.GetQuery("/ifd/xmp") as BitmapMetadata;
                    }
                    catch (IOException)
                    {
                        // WINCODEC_ERR_INVALIDQUERYREQUEST
                    }

                    if (xmpData == null)
                    {
                        // Some codecs may store the XMP data outside of the IFD block.
                        xmpData = metaData.GetQuery("/xmp") as BitmapMetadata;
                    }
                }
            }
            catch (IOException)
            {
                // WINCODEC_ERR_INVALIDQUERYREQUEST
            }


            // Return null if the XMP block does not contain any data.
            if ((xmpData == null) || !xmpData.Any())
            {
                return null;
            }

            BitmapMetadata tiffMetaData = new BitmapMetadata("tiff");
            tiffMetaData.SetQuery("/ifd/xmp", new BitmapMetadata("xmp"));

            foreach (var tag in xmpData)
            {
                object value = xmpData.GetQuery(tag);
                BitmapMetadata xmpSub = value as BitmapMetadata;

                if (xmpSub != null)
                {
                    CopySubBlockRecursive(ref tiffMetaData, xmpSub, "/ifd/xmp" + tag);
                }
                else
                {
                    tiffMetaData.SetQuery("/ifd/xmp" + tag, value);
                }
            }

            return tiffMetaData;
        }

        /// <summary>
        /// Retrieves the EXIF meta data in JPEG format.
        /// </summary>
        /// <param name="metaData">The meta data.</param>
        /// <returns>The EXIF meta data, or null.</returns>
        internal static BitmapMetadata GetEXIFMetaData(BitmapMetadata metaData)
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

            if (format == "jpg")
            {
                if (metaData.ContainsQuery("/app1/ifd/exif"))
                {
                    return metaData;
                }
            }
            else if (format != "gif" && format != "png")
            {
                // GIF and PNG files do not contain EXIF meta data.
                return ConvertEXIFMetaData(metaData);
            }

            return null;
        }

        /// <summary>
        /// Retrieves the XMP meta data in TIFF format.
        /// </summary>
        /// <param name="metaData">The meta data.</param>
        /// <returns>The XMP meta data, or null.</returns>
        internal static BitmapMetadata GetXMPMetaData(BitmapMetadata metaData)
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

            if (format == "tiff")
            {
                if (metaData.ContainsQuery("/ifd/xmp"))
                {
                    return metaData;
                }
            }
            else if (format != "gif")
            {
                // GIF files do not contain frame-level XMP meta data.
                return ConvertXMPMetaData(metaData, format);
            }

            return null;
        }
    }
}
#endif
