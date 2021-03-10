/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2021 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

// Adapted from:
/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See License-pdn.txt for full licensing and attribution details.             //
//                                                                             //
/////////////////////////////////////////////////////////////////////////////////
using System;
#if GDIPLUS
using System.Drawing;
using System.Drawing.Imaging;
#endif

namespace PSFilterHostDll.Imaging
{
    /// <summary>
    /// Surface class for 8 bits per pixel gray scale image data.
    /// </summary>
    internal sealed class SurfaceGray8 : SurfaceBase
    {
        public SurfaceGray8(int width, int height) : this(width, height, 96.0, 96.0)
        {
        }

        public SurfaceGray8(int width, int height, double dpiX, double dpiY) : base(width, height, 1, dpiX, dpiY)
        {
        }

        public override int ChannelCount => 1;

        public override int BitsPerChannel => 8;

#if GDIPLUS
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
                    "Microsoft.Design",
                    "CA1031:DoNotCatchGeneralExceptionTypes",
                    Justification = "Required as Bitmap.SetResolution is documented to throw it.")]
        public override unsafe Bitmap ToGdipBitmap()
        {
            Bitmap image = null;

            using (Bitmap temp = new Bitmap(width, height, PixelFormat.Format8bppIndexed))
            {
                ColorPalette pal = temp.Palette;

                for (int i = 0; i < 256; i++)
                {
                    pal.Entries[i] = Color.FromArgb(i, i, i);
                }

                temp.Palette = pal;

                BitmapData bd = temp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, temp.PixelFormat);

                try
                {
                    byte* pixels = (byte*)this.scan0.VoidStar;

                    byte* scan0 = (byte*)bd.Scan0.ToPointer();
                    int bmpStride = bd.Stride;
                    for (int y = 0; y < height; y++)
                    {
                        byte* src = pixels + (y * stride);
                        byte* dst = scan0 + (y * bmpStride);

                        for (int x = 0; x < width; x++)
                        {
                            *dst = *src;

                            src++;
                            dst++;
                        }
                    }
                }
                finally
                {
                    temp.UnlockBits(bd);
                }

                try
                {
                    temp.SetResolution((float)dpiX, (float)dpiY);
                }
                catch (Exception)
                {
                    // Ignore any errors when setting the resolution.
                }

                image = (Bitmap)temp.Clone();
            }

            return image;
        }

#else
        public override unsafe System.Windows.Media.Imaging.BitmapSource ToBitmapSource()
        {
            System.Windows.Media.Imaging.WriteableBitmap bitmap = new System.Windows.Media.Imaging.WriteableBitmap(
                width,
                height,
                dpiX,
                dpiY,
                System.Windows.Media.PixelFormats.Gray8,
                null
                );

            bitmap.Lock();
            try
            {
                byte* destScan0 = (byte*)bitmap.BackBuffer;
                int destStride = bitmap.BackBufferStride;

                for (int y = 0; y < height; y++)
                {
                    byte* src = GetRowAddressUnchecked(y);
                    byte* dst = destScan0 + (y * destStride);

                    for (int x = 0; x < width; x++)
                    {
                        *dst = *src;

                        src++;
                        dst++;
                    }
                }
            }
            finally
            {
                bitmap.Unlock();
            }

            bitmap.Freeze();

            return bitmap;
        }
#endif

        protected override unsafe void FitSurfaceImpl(SurfaceBase source)
        {
            float lastRowIndex = height - 1;
            float lastColumnIndex = width - 1;

            IntPtr srcColCachePtr = IntPtr.Zero;

            try
            {
                srcColCachePtr = ImageSurfaceMemory.Allocate((ulong)width * sizeof(float));
                float* srcColCache = (float*)srcColCachePtr;

                // Precompute the source column indexes.
                for (int x = 0; x < width; x++)
                {
                    float u = x / lastColumnIndex;

                    srcColCache[x] = (u * source.Width) - 0.5f;
                }

                for (int y = 0; y < height; y++)
                {
                    byte* destRow = GetRowAddressUnchecked(y);
                    float v = y / lastRowIndex;

                    float srcY = (v * source.Height) - 0.5f;
                    int yint = (int)srcY;
                    float yfract = srcY - (float)Math.Floor(srcY);

                    for (int x = 0; x < width; x++)
                    {
                        float srcX = srcColCache[x];
                        int xint = (int)srcX;
                        float xfract = srcX - (float)Math.Floor(srcX);

                        // 1st row
                        byte p00 = *source.GetPointAddressClamped(xint - 1, yint - 1);
                        byte p10 = *source.GetPointAddressClamped(xint + 0, yint - 1);
                        byte p20 = *source.GetPointAddressClamped(xint + 1, yint - 1);
                        byte p30 = *source.GetPointAddressClamped(xint + 2, yint - 1);

                        // 2nd row
                        byte p01 = *source.GetPointAddressClamped(xint - 1, yint + 0);
                        byte p11 = *source.GetPointAddressClamped(xint + 0, yint + 0);
                        byte p21 = *source.GetPointAddressClamped(xint + 1, yint + 0);
                        byte p31 = *source.GetPointAddressClamped(xint + 2, yint + 0);

                        // 3rd row
                        byte p02 = *source.GetPointAddressClamped(xint - 1, yint + 1);
                        byte p12 = *source.GetPointAddressClamped(xint + 0, yint + 1);
                        byte p22 = *source.GetPointAddressClamped(xint + 1, yint + 1);
                        byte p32 = *source.GetPointAddressClamped(xint + 2, yint + 1);

                        // 4th row
                        byte p03 = *source.GetPointAddressClamped(xint - 1, yint + 2);
                        byte p13 = *source.GetPointAddressClamped(xint + 0, yint + 2);
                        byte p23 = *source.GetPointAddressClamped(xint + 1, yint + 2);
                        byte p33 = *source.GetPointAddressClamped(xint + 2, yint + 2);

                        float gray0 = BicubicUtil.CubicHermite(p00, p10, p20, p30, xfract);
                        float gray1 = BicubicUtil.CubicHermite(p01, p11, p21, p31, xfract);
                        float gray2 = BicubicUtil.CubicHermite(p02, p12, p22, p32, xfract);
                        float gray3 = BicubicUtil.CubicHermite(p03, p13, p23, p33, xfract);

                        float gray = BicubicUtil.CubicHermite(gray0, gray1, gray2, gray3, yfract);

                        *destRow = (byte)FloatUtil.Clamp(gray, 0, 255);
                        destRow++;
                    }
                }
            }
            finally
            {
                if (srcColCachePtr != IntPtr.Zero)
                {
                    ImageSurfaceMemory.Free(srcColCachePtr);
                    srcColCachePtr = IntPtr.Zero;
                }
            }
        }
    }
}
