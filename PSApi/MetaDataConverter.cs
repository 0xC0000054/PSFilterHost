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
                        string baseQuery = "/app1/ifd/exif" + tag;

                        if (!jpegMetaData.ContainsQuery(baseQuery))
                        {
                            jpegMetaData.SetQuery(baseQuery, new BitmapMetadata(exifSub.Format));
                        }

                        foreach (var subTag in exifSub)
                        {
                            object subValue = exifSub.GetQuery(subTag);
                            jpegMetaData.SetQuery(baseQuery + subTag, subValue);
                        }
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
                        string baseQuery = "/xmp" + tag;

                        if (!jpegMetaData.ContainsQuery(baseQuery))
                        {
                            jpegMetaData.SetQuery(baseQuery, new BitmapMetadata(xmpSub.Format));
                        }

                        foreach (var subTag in xmpSub)
                        {
                            object subValue = xmpSub.GetQuery(subTag);
                            jpegMetaData.SetQuery(baseQuery + subTag, subValue);
                        }
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
                        string data = textChunk.GetQuery("/TextEntry") as string;

                        if (!string.IsNullOrEmpty(data))
                        {
                            BitmapMetadata jpegMetaData = new BitmapMetadata("jpg");

                            if (!jpegMetaData.ContainsQuery("/xmp"))
                            {
                                jpegMetaData.SetQuery("/xmp", new BitmapMetadata("xmp"));
                            }

                            jpegMetaData.SetQuery("/xmp", Encoding.UTF8.GetBytes(data)); // The XMP specification requires the packet to be a UTF8 encoded byte array.

                            return jpegMetaData;
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
