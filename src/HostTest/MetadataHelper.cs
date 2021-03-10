/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft public License:
//   Copyright (C) 2012-2021 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HostTest
{
    internal static class MetadataHelper
    {
        private enum ImageOrientation : ushort
        {
            /// <summary>
            /// The orientation is not specified.
            /// </summary>
            None = 0,

            /// <summary>
            /// The 0th row is at the visual top of the image, and the 0th column is the visual left-hand side
            /// </summary>
            TopLeft = 1,

            /// <summary>
            /// The 0th row is at the visual top of the image, and the 0th column is the visual right-hand side.
            /// </summary>
            TopRight = 2,

            /// <summary>
            /// The 0th row represents the visual bottom of the image, and the 0th column represents the visual right-hand side.
            /// </summary>
            BottomRight = 3,

            /// <summary>
            /// The 0th row represents the visual bottom of the image, and the 0th column represents the visual left-hand side.
            /// </summary>
            BottomLeft = 4,

            /// <summary>
            /// The 0th row represents the visual left-hand side of the image, and the 0th column represents the visual top.
            /// </summary>
            LeftTop = 5,

            /// <summary>
            /// The 0th row represents the visual right-hand side of the image, and the 0th column represents the visual top.
            /// </summary>
            RightTop = 6,

            /// <summary>
            /// The 0th row represents the visual right-hand side of the image, and the 0th column represents the visual bottom.
            /// </summary>
            RightBottom = 7,

            /// <summary>
            /// The 0th row represents the visual left-hand side of the image, and the 0th column represents the visual bottom.
            /// </summary>
            LeftBottom = 8
        }

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

        private static BitmapMetadata GetEXIFMetadata(BitmapMetadata metadata, string format)
        {
            BitmapMetadata exif = null;
            // GIF and PNG files do not contain EXIF meta data.
            if (format != "gif" && format != "png")
            {
                try
                {
                    if (format == "jpg")
                    {
                        exif = metadata.GetQuery("/app1/ifd/exif") as BitmapMetadata;
                    }
                    else
                    {
                        exif = metadata.GetQuery("/ifd/exif") as BitmapMetadata;
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
        private static BitmapMetadata LoadPNGMetadata(string xmp)
        {
            BitmapMetadata xmpData = null;

            using (MemoryStream stream = new MemoryStream())
            {
                // PNG stores the XMP meta-data in an iTXt chunk as an UTF8 encoded string,
                // so we have to save it to a dummy tiff and grab the XMP meta-data on load.
                BitmapMetadata tiffMetadata = new BitmapMetadata("tiff");
                tiffMetadata.SetQuery("/ifd/xmp", new BitmapMetadata("xmp"));
                tiffMetadata.SetQuery("/ifd/xmp", Encoding.UTF8.GetBytes(xmp));

                BitmapSource source = BitmapSource.Create(1, 1, 96.0, 96.0, PixelFormats.Gray8, null, new byte[] { 255 }, 1);
                TiffBitmapEncoder encoder = new TiffBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(source, null, tiffMetadata, null));
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

        private static BitmapMetadata GetXMPMetadata(BitmapMetadata metadata, string format)
        {
            BitmapMetadata xmp = null;

            // GIF files do not contain frame level XMP meta data.
            if (format != "gif")
            {
                try
                {
                    if (format == "png")
                    {
                        BitmapMetadata textChunk = metadata.GetQuery("/iTXt") as BitmapMetadata;

                        if (textChunk != null)
                        {
                            string keyWord = textChunk.GetQuery("/Keyword") as string;

                            if ((keyWord != null) && keyWord == "XML:com.adobe.xmp")
                            {
                                string data = textChunk.GetQuery("/TextEntry") as string;

                                if (!string.IsNullOrEmpty(data))
                                {
                                    xmp = LoadPNGMetadata(data);
                                }
                            }
                        }
                    }
                    else if (format == "jpg")
                    {
                        xmp = metadata.GetQuery("/xmp") as BitmapMetadata;
                    }
                    else
                    {
                        try
                        {
                            xmp = metadata.GetQuery("/ifd/xmp") as BitmapMetadata;
                        }
                        catch (IOException)
                        {
                            // WINCODEC_ERR_INVALIDQUERYREQUEST
                        }

                        if (xmp == null)
                        {
                            // Some codecs may store the XMP data outside of the IFD block.
                            xmp = metadata.GetQuery("/xmp") as BitmapMetadata;
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

        private static BitmapMetadata GetIPTCMetadata(BitmapMetadata metadata, string format)
        {
            BitmapMetadata iptc = null;
            // GIF and PNG files do not contain IPTC meta data.
            if (format != "gif" && format != "png")
            {
                try
                {
                    if (format == "jpg")
                    {
                        iptc = metadata.GetQuery("/app13/irb/8bimiptc/iptc") as BitmapMetadata;
                    }
                    else
                    {
                        try
                        {
                            iptc = metadata.GetQuery("/ifd/iptc") as BitmapMetadata;
                        }
                        catch (IOException)
                        {
                            // WINCODEC_ERR_INVALIDQUERYREQUEST
                        }

                        if (iptc == null)
                        {
                            iptc = metadata.GetQuery("/ifd/irb/8bimiptc/iptc") as BitmapMetadata;
                        }
                    }
                }
                catch (IOException)
                {
                    // WINCODEC_ERR_INVALIDQUERYREQUEST
                }
            }

            return iptc;
        }

        /// <summary>
        /// Converts the meta-data to TIFF format.
        /// </summary>
        /// <param name="metadata">The meta data.</param>
        /// <returns>The converted meta data or null</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="metadata"/> is null.
        /// </exception>
        internal static BitmapMetadata ConvertMetadataToTIFF(BitmapMetadata metadata)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            string format = string.Empty;

            try
            {
                format = metadata.Format; // Some WIC codecs do not implement the format property.
            }
            catch (ArgumentException)
            {
            }
            catch (NotSupportedException)
            {
            }

            if (format != "tiff")
            {
                BitmapMetadata exif = GetEXIFMetadata(metadata, format);
                BitmapMetadata xmp = GetXMPMetadata(metadata, format);
                BitmapMetadata iptc = GetIPTCMetadata(metadata, format);

                if (exif == null && xmp == null && iptc == null)
                {
                    return null;
                }

                BitmapMetadata tiffMetadata = new BitmapMetadata("tiff");

                if (exif != null)
                {
                    tiffMetadata.SetQuery("/ifd/exif", new BitmapMetadata("exif"));

                    foreach (var tag in exif)
                    {
                        object value = exif.GetQuery(tag);

                        BitmapMetadata exifSub = value as BitmapMetadata;

                        if (exifSub != null)
                        {
                            string baseQuery = "/ifd/exif" + tag;

                            CopySubIFDRecursive(ref tiffMetadata, exifSub, baseQuery);
                        }
                        else
                        {
                            tiffMetadata.SetQuery("/ifd/exif" + tag, value);
                        }
                    }
                }

                if (xmp != null)
                {
                    tiffMetadata.SetQuery("/ifd/xmp", new BitmapMetadata("xmp"));

                    foreach (var tag in xmp)
                    {
                        object value = xmp.GetQuery(tag);

                        BitmapMetadata xmpSub = value as BitmapMetadata;

                        if (xmpSub != null)
                        {
                            CopySubIFDRecursive(ref tiffMetadata, xmpSub, "/ifd/xmp" + tag);
                        }
                        else
                        {
                            tiffMetadata.SetQuery("/ifd/xmp" + tag, value);
                        }
                    }
                }

                if (iptc != null)
                {
                    tiffMetadata.SetQuery("/ifd/iptc", new BitmapMetadata("iptc"));

                    foreach (var tag in iptc)
                    {
                        object value = iptc.GetQuery(tag);

                        BitmapMetadata iptcSub = value as BitmapMetadata;

                        if (iptcSub != null)
                        {
                            CopySubIFDRecursive(ref tiffMetadata, iptcSub, "/ifd/iptc" + tag);
                        }
                        else
                        {
                            tiffMetadata.SetQuery("/ifd/iptc" + tag, value);
                        }
                    }
                }

                return tiffMetadata;
            }

            return metadata;
        }

        /// <summary>
        /// Gets the IPTC caption of the image.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <returns>The IPTC caption or null.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="image"/> is null.
        /// </exception>
        internal static string GetIPTCCaption(BitmapSource image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            BitmapMetadata metadata = null;

            try
            {
                metadata = image.Metadata as BitmapMetadata;
            }
            catch (NotSupportedException)
            {
            }

            if (metadata != null)
            {
                string iptcCaption = null;

                try
                {
                    iptcCaption = metadata.GetQuery("/ifd/iptc/{str=Caption}") as string;
                }
                catch (IOException)
                {
                    // WINCODEC_ERR_INVALIDQUERYREQUEST
                }

                if (iptcCaption == null)
                {
                    try
                    {
                        iptcCaption = metadata.GetQuery("/ifd/xmp/dc:description/x-default") as string;
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

        /// <summary>
        /// Gets the orientation transform.
        /// </summary>
        /// <param name="metadata">The meta data.</param>
        /// <returns>The orientation transform.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="metadata"/> is null.</exception>
        internal static Transform GetOrientationTransform(BitmapMetadata metadata)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            Transform transform = null;

            string format = string.Empty;

            try
            {
                format = metadata.Format; // Some WIC codecs do not implement the format property.
            }
            catch (ArgumentException)
            {
            }
            catch (NotSupportedException)
            {
            }

            ImageOrientation orientation = GetImageOrientation(metadata, format);

            if (orientation != ImageOrientation.None)
            {
                switch (orientation)
                {
                    case ImageOrientation.TopLeft:
                        // Do nothing
                        break;
                    case ImageOrientation.TopRight:
                        // Flip horizontally.
                        transform = new ScaleTransform() { ScaleX = -1 };
                        break;
                    case ImageOrientation.BottomRight:
                        // Rotate 180 degrees.
                        transform = new RotateTransform(180);
                        break;
                    case ImageOrientation.BottomLeft:
                        // Flip vertically.
                        transform = new ScaleTransform { ScaleY = -1 };
                        break;
                    case ImageOrientation.LeftTop:
                        transform = new TransformGroup()
                        {
                            Children = new TransformCollection(2)
                                {
                                    // Rotate 90 degrees clockwise and flip horizontally.
                                    new RotateTransform(90),
                                    new ScaleTransform { ScaleX = -1 }
                                }
                        };
                        break;
                    case ImageOrientation.RightTop:
                        // Rotate 90 degrees clockwise.
                        transform = new RotateTransform(90);
                        break;
                    case ImageOrientation.RightBottom:
                        transform = new TransformGroup()
                        {
                            // Rotate 270 degrees clockwise and flip horizontally.
                            Children = new TransformCollection(2)
                                {
                                    new RotateTransform(270),
                                    new ScaleTransform { ScaleX = -1 }
                                }
                        };
                        break;
                    case ImageOrientation.LeftBottom:
                        // Rotate 270 degrees clockwise.
                        transform = new RotateTransform(270);
                        break;
                }
            }

            return transform;
        }

        /// <summary>
        /// Sets the image orientation to indicate the origin is the top left corner.
        /// </summary>
        /// <param name="metadata">The meta data.</param>
        /// <returns>The modified meta data.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="metadata"/> is null.</exception>
        internal static BitmapMetadata SetOrientationToTopLeft(BitmapMetadata metadata)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            string format = string.Empty;

            try
            {
                format = metadata.Format; // Some WIC codecs do not implement the format property.
            }
            catch (ArgumentException)
            {
            }
            catch (NotSupportedException)
            {
            }

            BitmapMetadata newMetadata = metadata.Clone();

            try
            {
                if (format == "jpg")
                {
                    if (metadata.ContainsQuery("/app1/ifd"))
                    {
                        newMetadata.SetQuery("/app1/ifd/{ushort=274}", (ushort)ImageOrientation.TopLeft);
                    }

                    if (metadata.ContainsQuery("/xmp"))
                    {
                        newMetadata.SetQuery("/xmp/tiff:Orientation", ((ushort)ImageOrientation.TopLeft).ToString(CultureInfo.InvariantCulture));
                    }
                }
                else if (format == "png")
                {
                    BitmapMetadata xmp = GetXMPMetadata(metadata, format);

                    if (xmp != null)
                    {
                        xmp.SetQuery("/tiff:Orientation", ((ushort)ImageOrientation.TopLeft).ToString(CultureInfo.InvariantCulture));

                        if (string.Equals(metadata.GetQuery("/iTXt/Keyword") as string, "XML:com.adobe.xmp", StringComparison.Ordinal))
                        {
                            byte[] packet = ExtractXMPPacket(xmp);

                            if (packet != null)
                            {
                                newMetadata.SetQuery("/iTXt/TextEntry", Encoding.UTF8.GetString(packet));
                            }
                        }
                    }
                }
                else if (format != "gif")
                {
                    if (metadata.ContainsQuery("/ifd"))
                    {
                        newMetadata.SetQuery("/ifd/{ushort=274}", (ushort)ImageOrientation.TopLeft);

                        if (metadata.ContainsQuery("/ifd/xmp"))
                        {
                            newMetadata.SetQuery("/ifd/xmp/tiff:Orientation", ((ushort)ImageOrientation.TopLeft).ToString(CultureInfo.InvariantCulture));
                        }
                    }
                }
            }
            catch (IOException)
            {
                // WINCODEC_ERR_INVALIDQUERYREQUEST
            }

            return newMetadata;
        }

        #region Save format conversion

        private static BitmapMetadata ConvertMetadataToJPEG(BitmapMetadata metadata, string format)
        {
            BitmapMetadata exif = GetEXIFMetadata(metadata, format);
            BitmapMetadata xmp = GetXMPMetadata(metadata, format);
            BitmapMetadata iptc = GetIPTCMetadata(metadata, format);

            if (exif == null && xmp == null && iptc == null)
            {
                return null;
            }

            BitmapMetadata jpegMetadata = new BitmapMetadata("jpg");

            if (exif != null)
            {
                jpegMetadata.SetQuery("/app1/ifd/exif", new BitmapMetadata("exif"));

                foreach (var tag in exif)
                {
                    object value = exif.GetQuery(tag);

                    BitmapMetadata exifSub = value as BitmapMetadata;

                    if (exifSub != null)
                    {
                        string baseQuery = "/app1/ifd/exif" + tag;

                        CopySubIFDRecursive(ref jpegMetadata, exifSub, baseQuery);
                    }
                    else
                    {
                        jpegMetadata.SetQuery("/app1/ifd/exif" + tag, value);
                    }
                }
            }

            if (xmp != null)
            {
                jpegMetadata.SetQuery("/xmp", new BitmapMetadata("xmp"));

                foreach (var tag in xmp)
                {
                    object value = xmp.GetQuery(tag);

                    BitmapMetadata xmpSub = value as BitmapMetadata;

                    if (xmpSub != null)
                    {
                        CopySubIFDRecursive(ref jpegMetadata, xmpSub, "/xmp" + tag);
                    }
                    else
                    {
                        jpegMetadata.SetQuery("/xmp" + tag, value);
                    }
                }
            }

            if (iptc != null)
            {
                jpegMetadata.SetQuery("/app13/irb/8bimiptc/iptc", new BitmapMetadata("iptc"));

                foreach (var tag in iptc)
                {
                    object value = iptc.GetQuery(tag);

                    BitmapMetadata iptcSub = value as BitmapMetadata;

                    if (iptcSub != null)
                    {
                        CopySubIFDRecursive(ref jpegMetadata, iptcSub, "/app13/irb/8bimiptc/iptc" + tag);
                    }
                    else
                    {
                        jpegMetadata.SetQuery("/app13/irb/8bimiptc/iptc" + tag, value);
                    }
                }
            }

            return jpegMetadata;
        }

        private static byte[] ExtractXMPPacket(BitmapMetadata xmp)
        {
            BitmapMetadata tiffMetadata = new BitmapMetadata("tiff");
            tiffMetadata.SetQuery("/ifd/xmp", new BitmapMetadata("xmp"));

            foreach (var tag in xmp)
            {
                object value = xmp.GetQuery(tag);

                BitmapMetadata xmpSub = value as BitmapMetadata;

                if (xmpSub != null)
                {
                    CopySubIFDRecursive(ref tiffMetadata, xmpSub, "/ifd/xmp" + tag);
                }
                else
                {
                    tiffMetadata.SetQuery("/ifd/xmp" + tag, value);
                }
            }

            byte[] xmpBytes = null;

            using (MemoryStream stream = new MemoryStream())
            {
                // Create a dummy tiff to extract the XMP packet from.
                BitmapSource source = BitmapSource.Create(1, 1, 96.0, 96.0, PixelFormats.Gray8, null, new byte[] { 255 }, 1);
                TiffBitmapEncoder encoder = new TiffBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(source, null, tiffMetadata, null));
                encoder.Save(stream);

                xmpBytes = TiffReader.ExtractXMP(stream);
            }

            return xmpBytes;
        }

        private static BitmapMetadata ConvertMetadataToPNG(BitmapMetadata metadata, string format)
        {
            BitmapMetadata xmp = GetXMPMetadata(metadata, format);

            if (xmp != null)
            {
                byte[] packet = ExtractXMPPacket(xmp);

                if (packet != null)
                {
                    BitmapMetadata pngMetadata = new BitmapMetadata("png");
                    pngMetadata.SetQuery("/iTXt", new BitmapMetadata("iTXt"));

                    // The Keyword property is an ANSI string (VT_LPSTR), which must passed as a char array in order for it to be marshaled correctly.
                    char[] keyWordChars = "XML:com.adobe.xmp".ToCharArray();
                    pngMetadata.SetQuery("/iTXt/Keyword", keyWordChars);

                    pngMetadata.SetQuery("/iTXt/TextEntry", Encoding.UTF8.GetString(packet));

                    return pngMetadata;
                }
            }

            return null;
        }

        private static BitmapMetadata ConvertMetadataToWMPhoto(BitmapMetadata metadata, string format)
        {
            BitmapMetadata exif = GetEXIFMetadata(metadata, format);
            BitmapMetadata xmp = GetXMPMetadata(metadata, format);
            BitmapMetadata iptc = GetIPTCMetadata(metadata, format);

            if (exif == null && xmp == null && iptc == null)
            {
                return null;
            }

            BitmapMetadata wmpMetadata = new BitmapMetadata("wmphoto");

            if (exif != null)
            {
                wmpMetadata.SetQuery("/ifd/exif", new BitmapMetadata("exif"));

                foreach (var tag in exif)
                {
                    object value = exif.GetQuery(tag);

                    BitmapMetadata exifSub = value as BitmapMetadata;

                    if (exifSub != null)
                    {
                        string baseQuery = "/ifd/exif" + tag;

                        CopySubIFDRecursive(ref wmpMetadata, exifSub, baseQuery);
                    }
                    else
                    {
                        wmpMetadata.SetQuery("/ifd/exif" + tag, value);
                    }
                }
            }

            if (xmp != null)
            {
                wmpMetadata.SetQuery("/ifd/xmp", new BitmapMetadata("xmp"));

                foreach (var tag in xmp)
                {
                    object value = xmp.GetQuery(tag);

                    BitmapMetadata xmpSub = value as BitmapMetadata;

                    if (xmpSub != null)
                    {
                        CopySubIFDRecursive(ref wmpMetadata, xmpSub, "/ifd/xmp" + tag);
                    }
                    else
                    {
                        wmpMetadata.SetQuery("/ifd/xmp" + tag, value);
                    }
                }
            }

            if (iptc != null)
            {
                wmpMetadata.SetQuery("/ifd/iptc", new BitmapMetadata("iptc"));

                foreach (var tag in iptc)
                {
                    object value = iptc.GetQuery(tag);

                    BitmapMetadata iptcSub = value as BitmapMetadata;

                    if (iptcSub != null)
                    {
                        CopySubIFDRecursive(ref wmpMetadata, iptcSub, "/ifd/iptc" + tag);
                    }
                    else
                    {
                        wmpMetadata.SetQuery("/ifd/iptc" + tag, value);
                    }
                }
            }

            return wmpMetadata;
        }
        #endregion

        /// <summary>
        /// Converts the meta-data to the encoder format.
        /// </summary>
        /// <param name="metadata">The meta-data.</param>
        /// <param name="encoder">The encoder.</param>
        /// <returns>The converted meta-data, or null.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="metadata"/> is null.
        /// or
        /// <paramref name="encoder"/> is null.
        /// </exception>
        internal static BitmapMetadata ConvertSaveMetadataFormat(BitmapMetadata metadata, BitmapEncoder encoder)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }
            if (encoder == null)
            {
                throw new ArgumentNullException(nameof(encoder));
            }

            string format = string.Empty;

            try
            {
                format = metadata.Format; // Some WIC codecs do not implement the format property.
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
                    return metadata;
                }
                else
                {
                    return ConvertMetadataToTIFF(metadata);
                }
            }
            else if (encoderType == typeof(JpegBitmapEncoder))
            {
                if (format == "jpg")
                {
                    return metadata;
                }
                else
                {
                    return ConvertMetadataToJPEG(metadata, format);
                }
            }
            else if (encoderType == typeof(PngBitmapEncoder))
            {
                if (format == "png")
                {
                    return metadata;
                }
                else
                {
                    return ConvertMetadataToPNG(metadata, format);
                }
            }
            else if (encoderType == typeof(WmpBitmapEncoder))
            {
                if (format == "wmphoto")
                {
                    return metadata;
                }
                else
                {
                    return ConvertMetadataToWMPhoto(metadata, format);
                }
            }

            return null;
        }

        private static ImageOrientation GetImageOrientation(BitmapMetadata metadata, string format)
        {
            ImageOrientation orientation = ImageOrientation.None;

            object orientationObject = null;

            try
            {
                if (format == "jpg")
                {
                    orientationObject = metadata.GetQuery("/app1/ifd/{ushort=274}");
                }
                else if (format != "gif" && format != "png")
                {
                    orientationObject = metadata.GetQuery("/ifd/{ushort=274}");
                }
            }
            catch (IOException)
            {
                // WINCODEC_ERR_INVALIDQUERYREQUEST
            }

            if (orientationObject == null)
            {
                BitmapMetadata xmp = GetXMPMetadata(metadata, format);

                if (xmp != null)
                {
                    try
                    {
                        orientationObject = xmp.GetQuery("/tiff:Orientation");
                    }
                    catch (IOException)
                    {
                        // WINCODEC_ERR_INVALIDQUERYREQUEST
                    }
                }
            }

            if (orientationObject != null)
            {
                ushort? nullableValue = null;

                if (orientationObject is ushort result)
                {
                    nullableValue = result;
                }
                else
                {
                    // WIC treats the XMP tiff:Orientation property as a string.
                    if (ushort.TryParse(orientationObject as string, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort value))
                    {
                        nullableValue = value;
                    }
                }

                if (nullableValue.HasValue)
                {
                    ushort value = nullableValue.Value;

                    if (value >= (ushort)ImageOrientation.TopLeft && value <= (ushort)ImageOrientation.LeftBottom)
                    {
                        orientation = (ImageOrientation)value;
                    }
                }
            }

            return orientation;
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
                public readonly ushort tag;
                public readonly DataType type;
                public readonly uint count;
                public readonly uint offset;

                public IFD(Stream stream, bool littleEndian)
                {
                    tag = ReadShort(stream, littleEndian);
                    type = (DataType)ReadShort(stream, littleEndian);
                    count = ReadLong(stream, littleEndian);
                    offset = ReadLong(stream, littleEndian);
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
                    return (uint)(byte1 | (byte2 << 8) | (byte3 << 16) | (byte4 << 24));
                }
                else
                {
                    return (uint)((byte1 << 24) | (byte2 << 16) | (byte3 << 8) | byte4);
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
