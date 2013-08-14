/////////////////////////////////////////////////////////////////////////////////
//
// Adobe(R) Photoshop(R) filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft public License:
//   Copyright (C) 2012-2013 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Windows.Media.Imaging;
using System.IO;

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

        private static BitmapMetadata ConvertIFDMetadata(BitmapMetadata source)
        {
            BitmapMetadata ifd = null;
            BitmapMetadata xmp = null; // Some codecs may store the XMP data outside the IFD block.

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
                xmp = source.GetQuery("/xmp") as BitmapMetadata;
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

                        tiffMetaData.SetQuery("/ifd/xmp", System.Text.Encoding.UTF8.GetBytes(data)); // The XMP specification requires TIFF XMP meta-data to be UTF8 encoded,      
                 
                        return tiffMetaData;
                    }
                }
                
            }

            return null;
        }
        
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

    }
}
