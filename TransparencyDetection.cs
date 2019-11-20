/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2019 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

#if GDIPLUS
using System.Drawing;
using System.Drawing.Imaging;
#else
using System.Windows.Media;
using System.Windows.Media.Imaging;
#endif

namespace PSFilterHostDll
{
    internal static class TransparencyDetection
    {
#if GDIPLUS
        /// <summary>
        /// Determines whether the specified image has transparency.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <returns><c>true</c> if the image has transparency; otherwise, <c>false</c></returns>
        /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
        public static unsafe bool ImageHasTransparency(Bitmap image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            PixelFormat format = image.PixelFormat;

            // According to MSDN GDI+ can load 16 bits per channel images, but they will converted
            // to 8 bits per channel for processing and saving.
            // See the remarks section on the following page:
            // https://docs.microsoft.com/en-us/windows/win32/gdiplus/-gdiplus-constant-image-pixel-format-constants

            if (format == PixelFormat.Format32bppArgb ||
                format == PixelFormat.Format32bppPArgb ||
                format == PixelFormat.Format64bppArgb ||
                format == PixelFormat.Format64bppPArgb ||
                format == PixelFormat.Format16bppArgb1555)
            {
                int width = image.Width;
                int height = image.Height;

                BitmapData bitmapData = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    byte* scan0 = (byte*)bitmapData.Scan0;
                    int stride = bitmapData.Stride;

                    for (int y = 0; y < height; y++)
                    {
                        byte* ptr = scan0 + (y * stride);

                        for (int x = 0; x < width; x++)
                        {
                            if (ptr[3] < 255)
                            {
                                return true;
                            }

                            ptr += 4;
                        }
                    }
                }
                finally
                {
                    image.UnlockBits(bitmapData);
                }
            }

            return false;
        }
#else
        /// <summary>
        /// Determines whether the specified image has transparency.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <returns><c>true</c> if the image has transparency; otherwise, <c>false</c></returns>
        /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
        public static unsafe bool ImageHasTransparency(BitmapSource image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            PixelFormat format = image.Format;

            if (format == PixelFormats.Bgra32 ||
                format == PixelFormats.Rgba64 ||
                format == PixelFormats.Rgba128Float ||
                format == PixelFormats.Pbgra32 ||
                format == PixelFormats.Prgba64 ||
                format == PixelFormats.Prgba128Float)
            {
                int width = image.PixelWidth;
                int height = image.PixelHeight;

                WriteableBitmap writeableBitmap = new WriteableBitmap(image);

                byte* scan0 = (byte*)writeableBitmap.BackBuffer;
                int stride = writeableBitmap.BackBufferStride;

                if (format == PixelFormats.Bgra32 || format == PixelFormats.Pbgra32)
                {
                    for (int y = 0; y < height; y++)
                    {
                        byte* ptr = scan0 + (y * stride);

                        for (int x = 0; x < width; x++)
                        {
                            if (ptr[3] < 255)
                            {
                                return true;
                            }

                            ptr += 4;
                        }
                    }
                }
                else if (format == PixelFormats.Rgba64 || format == PixelFormats.Prgba64)
                {
                    for (int y = 0; y < height; y++)
                    {
                        ushort* ptr = (ushort*)(scan0 + (y * stride));

                        for (int x = 0; x < width; x++)
                        {
                            if (ptr[3] < 65535)
                            {
                                return true;
                            }

                            ptr += 4;
                        }
                    }
                }
                else if (format == PixelFormats.Rgba128Float || format == PixelFormats.Prgba128Float)
                {
                    for (int y = 0; y < height; y++)
                    {
                        float* ptr = (float*)(scan0 + (y * stride));

                        for (int x = 0; x < width; x++)
                        {
                            if (ptr[3] < 1.0f)
                            {
                                return true;
                            }

                            ptr += 4;
                        }
                    }
                }
            }

            return false;
        }
#endif
    }
}
