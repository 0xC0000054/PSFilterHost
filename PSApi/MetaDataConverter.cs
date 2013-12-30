/////////////////////////////////////////////////////////////////////////////////
//
// Adobe(R) Photoshop(R) filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2013 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Text;
using System.Linq;
using System.IO;

#if !GDIPLUS
using System.Windows.Media.Imaging;
#endif

namespace PSFilterLoad.PSApi
{
#if !GDIPLUS
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
        /// Converts the IFD meta data to JPEG format.
        /// </summary>
        /// <param name="metaData">The meta data to convert.</param>
        /// <param name="exif">set to <c>true</c> if the EXIF data is requested.</param>
        /// <returns>The converted meta data or null.</returns>
        private static BitmapMetadata ConvertIFDMetaData(BitmapMetadata metaData, bool exif)
        {
            BitmapMetadata jpegMetaData = new BitmapMetadata("jpg");

            if (exif)
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

                if ((exifData == null) || !exifData.Any())  // Return null if the EXIF block does not contain any data.
                {
                    return null;
                }

                if (!jpegMetaData.ContainsQuery("/app1/ifd/exif"))
                {
                    jpegMetaData.SetQuery("/app1/ifd/exif", new BitmapMetadata("exif"));
                }

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

                // set the fields that are relevant for EXIF.
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
            }
            else
            {
                BitmapMetadata xmpData = null;

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
                    try
                    {
                        xmpData = metaData.GetQuery("/xmp") as BitmapMetadata; // Some codecs may store the XMP data outside of the IFD block.
                    }
                    catch (IOException)
                    {
                        // WINCODEC_ERR_INVALIDQUERYREQUEST
                    }
                }

                if ((xmpData == null) || !xmpData.Any()) // Return null if the XMP block does not contain any data.
                {
                    return null;
                }

                if (!jpegMetaData.ContainsQuery("/xmp"))
                {
                    jpegMetaData.SetQuery("/xmp", new BitmapMetadata("xmp"));
                }

                foreach (var tag in xmpData)
                {
                    object value = xmpData.GetQuery(tag);
                    BitmapMetadata xmpSub = value as BitmapMetadata;

                    if (xmpSub != null)
                    {
                        CopySubBlockRecursive(ref jpegMetaData, xmpSub, "/xmp" + tag);
                    }
                    else
                    {
                        jpegMetaData.SetQuery("/xmp" + tag, value);
                    }

                }
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
            // PNG stores the XMP meta-data in an iTXt chunk as an UTF8 encoded string, so we have to save it to a dummy tiff and grab the XMP meta-data on load. 
            BitmapMetadata tiffMetaData = new BitmapMetadata("tiff");

            tiffMetaData.SetQuery("/ifd/xmp", new BitmapMetadata("xmp"));
            tiffMetaData.SetQuery("/ifd/xmp", System.Text.Encoding.UTF8.GetBytes(xmp));

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

                    return block;
                }
            }

            return null;
        }

        /// <summary>
        /// Converts the PNG XMP meta data to JPEG format.
        /// </summary>
        /// <param name="metadata">The meta data.</param>
        /// <param name="exif">if set to <c>true</c> convert the EXIF data; otherwise convert the XMP data.</param>
        /// <returns>The converted XMP meta data; or null.</returns>
        private static BitmapMetadata ConvertPNGMetaData(BitmapMetadata metadata, bool exif)
        {
            if (!exif) // Only XMP is documented for PNG.
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
                        string xmp = textChunk.GetQuery("/TextEntry") as string;

                        if (!string.IsNullOrEmpty(xmp))
                        {
                            BitmapMetadata xmpData = LoadPNGMetaData(xmp);

                            if (xmpData != null)
                            {
                                BitmapMetadata jpegMetaData = new BitmapMetadata("jpg");

                                if (!jpegMetaData.ContainsQuery("/xmp"))
                                {
                                    jpegMetaData.SetQuery("/xmp", new BitmapMetadata("xmp"));
                                }

                                foreach (var tag in xmpData)
                                {
                                    object value = xmpData.GetQuery(tag);
                                    BitmapMetadata xmpSub = value as BitmapMetadata;

                                    if (xmpSub != null)
                                    {
                                        CopySubBlockRecursive(ref jpegMetaData, xmpSub, "/xmp" + tag);
                                    }
                                    else
                                    {
                                        jpegMetaData.SetQuery("/xmp" + tag, value);
                                    }

                                }

                                return jpegMetaData;
                            }
                        }
                    }

                }
            }

            return null;
        }
        
        /// <summary>
        /// Determines whether the specified meta data is in JPEG format.
        /// </summary>
        /// <param name="metaData">The meta data.</param>
        /// <returns>
        ///   <c>true</c> if the meta data is JPEG format; otherwise, <c>false</c>.
        /// </returns>
        internal static bool IsJPEGMetaData(BitmapMetadata metaData)
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

            return (format == "jpg");
        }

        /// <summary>
        /// Converts the meta data to JPEG format.
        /// </summary>
        /// <param name="metaData">The meta data.</param>
        /// <param name="exif">if set to <c>true</c> convert the EXIF data; otherwise convert the XMP data.</param>
        /// <returns>The converted meta data, or null.</returns>
        internal static BitmapMetadata ConvertMetaDataToJPEG(BitmapMetadata metaData, bool exif)
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

            if (format != "jpg")
            {
                if (format == "gif")
                {
                    return null; // GIF files do not contain frame-level EXIF or XMP meta data.
                }
                else if (format == "png")
                {
                    return ConvertPNGMetaData(metaData, exif);
                }
                else
                {
                    return ConvertIFDMetaData(metaData, exif);
                }
            }

            return metaData;
        }
    } 
#endif
}
