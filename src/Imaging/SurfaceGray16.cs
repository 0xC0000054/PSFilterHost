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
#endif

namespace PSFilterHostDll.Imaging
{
    /// <summary>
    /// Surface class for 16 bits per pixel gray scale image data.
    /// </summary>
    internal sealed class SurfaceGray16 : SurfaceBase
    {
        public SurfaceGray16(int width, int height) : this(width, height, 96.0, 96.0)
        {
        }

        public SurfaceGray16(int width, int height, double dpiX, double dpiY) : base(width, height, 2, dpiX, dpiY)
        {
        }

        public override int ChannelCount => 1;

        public override int BitsPerChannel => 16;

        /// <summary>
        /// Scales the data to the internal 16 bit range used by Adobe(R) Photoshop(R).
        /// </summary>
        public unsafe void ScaleToPhotoshop16BitRange()
        {
            ushort[] map = CreatePhotoshopRangeLookupTable();
            for (int y = 0; y < height; y++)
            {
                ushort* ptr = (ushort*)GetRowAddressUnchecked(y);
                ushort* ptrEnd = ptr + width;

                while (ptr < ptrEnd)
                {
                    *ptr = map[*ptr];

                    ptr++;
                }
            }
        }

#if GDIPLUS
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
                    "Microsoft.Design",
                    "CA1031:DoNotCatchGeneralExceptionTypes",
                    Justification = "Required as Bitmap.SetResolution is documented to throw it.")]
        public override unsafe Bitmap ToGdipBitmap()
        {
            Bitmap image = null;

            const System.Drawing.Imaging.PixelFormat format = System.Drawing.Imaging.PixelFormat.Format16bppGrayScale;

            using (Bitmap temp = new Bitmap(width, height, format))
            {
                System.Drawing.Imaging.BitmapData bitmapData = temp.LockBits(new Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.WriteOnly, format);
                try
                {
                    byte* destScan0 = (byte*)bitmapData.Scan0;
                    int destStride = bitmapData.Stride;

                    for (int y = 0; y < height; y++)
                    {
                        ushort* src = (ushort*)GetRowAddressUnchecked(y);
                        ushort* dst = (ushort*)(destScan0 + (y * destStride));

                        for (int x = 0; x < width; x++)
                        {
                            *dst = Fix16BitRange(*src);

                            src++;
                            dst++;
                        }
                    }
                }
                finally
                {
                    temp.UnlockBits(bitmapData);
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
                    System.Windows.Media.PixelFormats.Gray16,
                    null);

            bitmap.Lock();
            try
            {
                byte* destScan0 = (byte*)bitmap.BackBuffer;
                int destStride = bitmap.BackBufferStride;

                for (int y = 0; y < height; y++)
                {
                    ushort* src = (ushort*)GetRowAddressUnchecked(y);
                    ushort* dst = (ushort*)(destScan0 + (y * destStride));

                    for (int x = 0; x < width; x++)
                    {
                        *dst = Fix16BitRange(*src);

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
                    ushort* destRow = (ushort*)GetRowAddressUnchecked(y);
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
                        ushort p00 = *(ushort*)source.GetPointAddressClamped(xint - 1, yint - 1);
                        ushort p10 = *(ushort*)source.GetPointAddressClamped(xint + 0, yint - 1);
                        ushort p20 = *(ushort*)source.GetPointAddressClamped(xint + 1, yint - 1);
                        ushort p30 = *(ushort*)source.GetPointAddressClamped(xint + 2, yint - 1);

                        // 2nd row
                        ushort p01 = *(ushort*)source.GetPointAddressClamped(xint - 1, yint + 0);
                        ushort p11 = *(ushort*)source.GetPointAddressClamped(xint + 0, yint + 0);
                        ushort p21 = *(ushort*)source.GetPointAddressClamped(xint + 1, yint + 0);
                        ushort p31 = *(ushort*)source.GetPointAddressClamped(xint + 2, yint + 0);

                        // 3rd row
                        ushort p02 = *(ushort*)source.GetPointAddressClamped(xint - 1, yint + 1);
                        ushort p12 = *(ushort*)source.GetPointAddressClamped(xint + 0, yint + 1);
                        ushort p22 = *(ushort*)source.GetPointAddressClamped(xint + 1, yint + 1);
                        ushort p32 = *(ushort*)source.GetPointAddressClamped(xint + 2, yint + 1);

                        // 4th row
                        ushort p03 = *(ushort*)source.GetPointAddressClamped(xint - 1, yint + 2);
                        ushort p13 = *(ushort*)source.GetPointAddressClamped(xint + 0, yint + 2);
                        ushort p23 = *(ushort*)source.GetPointAddressClamped(xint + 1, yint + 2);
                        ushort p33 = *(ushort*)source.GetPointAddressClamped(xint + 2, yint + 2);

                        float gray0 = BicubicUtil.CubicHermite(p00, p10, p20, p30, xfract);
                        float gray1 = BicubicUtil.CubicHermite(p01, p11, p21, p31, xfract);
                        float gray2 = BicubicUtil.CubicHermite(p02, p12, p22, p32, xfract);
                        float gray3 = BicubicUtil.CubicHermite(p03, p13, p23, p33, xfract);

                        float gray = BicubicUtil.CubicHermite(gray0, gray1, gray2, gray3, yfract);

                        *destRow = (ushort)FloatUtil.Clamp(gray, 0, 32768);
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
