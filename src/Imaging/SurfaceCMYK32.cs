/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2018 Nicholas Hayes
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
using PSFilterHostDll.Interop;

#if GDIPLUS
using System.Drawing;
using System.Drawing.Imaging;
#endif

namespace PSFilterHostDll.Imaging
{
    /// <summary>
    /// Surface class for 32 bits per pixel CMYK image data. Each channel is allocated 8 bits per pixel.
    /// </summary>
    internal sealed class SurfaceCMYK32 : SurfaceBase, IColorManagedSurface
    {
        public SurfaceCMYK32(int width, int height) : this(width, height, 96.0, 96.0)
        {
        }

        public SurfaceCMYK32(int width, int height, double dpiX, double dpiY) : base(width, height, 4, dpiX, dpiY)
        {
        }

        public override int ChannelCount => 4;

        public override int BitsPerChannel => 8;

        public NativeEnums.Mscms.BMFORMAT MscmsFormat => NativeEnums.Mscms.BMFORMAT.BM_KYMCQUADS;

#if GDIPLUS
        public override unsafe Bitmap ToGdipBitmap()
        {
            Bitmap image = null;
            Bitmap temp = null;

            const PixelFormat format = PixelFormat.Format24bppRgb;

            try
            {
                temp = new Bitmap(width, height, format);

                BitmapData bitmapData = temp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, format);
                try
                {
                    byte* destScan0 = (byte*)bitmapData.Scan0;
                    int destStride = bitmapData.Stride;

                    for (int y = 0; y < height; y++)
                    {
                        ColorCmyk8* src = (ColorCmyk8*)GetRowAddressUnchecked(y);
                        byte* dst = destScan0 + (y * destStride);

                        for (int x = 0; x < width; x++)
                        {
                            byte cyan = src->C;
                            byte magenta = src->M;
                            byte yellow = src->Y;
                            byte black = src->K;

                            int red = 255 - Math.Min(255, cyan * (255 - black) / 255 + black);
                            int green = 255 - Math.Min(255, magenta * (255 - black) / 255 + black);
                            int blue = 255 - Math.Min(255, yellow * (255 - black) / 255 + black);

                            dst[2] = (byte)red;
                            dst[1] = (byte)green;
                            dst[0] = (byte)blue;

                            src++;
                            dst += 3;
                        }
                    }
                }
                finally
                {
                    temp.UnlockBits(bitmapData);
                }

                image = temp;
                temp = null;
            }
            finally
            {
                if (temp != null)
                {
                    temp.Dispose();
                    temp = null;
                }
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
                System.Windows.Media.PixelFormats.Cmyk32,
                null
                );

            bitmap.Lock();
            try
            {
                byte* destScan0 = (byte*)bitmap.BackBuffer;
                int destStride = bitmap.BackBufferStride;

                for (int y = 0; y < height; y++)
                {
                    ColorCmyk8* src = (ColorCmyk8*)GetRowAddressUnchecked(y);
                    byte* dst = destScan0 + (y * destStride);

                    for (int x = 0; x < width; x++)
                    {
                        dst[0] = src->C;
                        dst[1] = src->M;
                        dst[2] = src->Y;
                        dst[3] = src->K;

                        src++;
                        dst += 4;
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
                    ColorCmyk8* destRow = (ColorCmyk8*)GetRowAddressUnchecked(y);
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
                        ColorCmyk8 p00 = *(ColorCmyk8*)source.GetPointAddressClamped(xint - 1, yint - 1);
                        ColorCmyk8 p10 = *(ColorCmyk8*)source.GetPointAddressClamped(xint + 0, yint - 1);
                        ColorCmyk8 p20 = *(ColorCmyk8*)source.GetPointAddressClamped(xint + 1, yint - 1);
                        ColorCmyk8 p30 = *(ColorCmyk8*)source.GetPointAddressClamped(xint + 2, yint - 1);

                        // 2nd row
                        ColorCmyk8 p01 = *(ColorCmyk8*)source.GetPointAddressClamped(xint - 1, yint + 0);
                        ColorCmyk8 p11 = *(ColorCmyk8*)source.GetPointAddressClamped(xint + 0, yint + 0);
                        ColorCmyk8 p21 = *(ColorCmyk8*)source.GetPointAddressClamped(xint + 1, yint + 0);
                        ColorCmyk8 p31 = *(ColorCmyk8*)source.GetPointAddressClamped(xint + 2, yint + 0);

                        // 3rd row
                        ColorCmyk8 p02 = *(ColorCmyk8*)source.GetPointAddressClamped(xint - 1, yint + 1);
                        ColorCmyk8 p12 = *(ColorCmyk8*)source.GetPointAddressClamped(xint + 0, yint + 1);
                        ColorCmyk8 p22 = *(ColorCmyk8*)source.GetPointAddressClamped(xint + 1, yint + 1);
                        ColorCmyk8 p32 = *(ColorCmyk8*)source.GetPointAddressClamped(xint + 2, yint + 1);

                        // 4th row
                        ColorCmyk8 p03 = *(ColorCmyk8*)source.GetPointAddressClamped(xint - 1, yint + 2);
                        ColorCmyk8 p13 = *(ColorCmyk8*)source.GetPointAddressClamped(xint + 0, yint + 2);
                        ColorCmyk8 p23 = *(ColorCmyk8*)source.GetPointAddressClamped(xint + 1, yint + 2);
                        ColorCmyk8 p33 = *(ColorCmyk8*)source.GetPointAddressClamped(xint + 2, yint + 2);

                        float cyan0 = BicubicUtil.CubicHermite(p00.C, p10.C, p20.C, p30.C, xfract);
                        float cyan1 = BicubicUtil.CubicHermite(p01.C, p11.C, p21.C, p31.C, xfract);
                        float cyan2 = BicubicUtil.CubicHermite(p02.C, p12.C, p22.C, p32.C, xfract);
                        float cyan3 = BicubicUtil.CubicHermite(p03.C, p13.C, p23.C, p33.C, xfract);

                        float cyan = BicubicUtil.CubicHermite(cyan0, cyan1, cyan2, cyan3, yfract);

                        float magenta0 = BicubicUtil.CubicHermite(p00.M, p10.M, p20.M, p30.M, xfract);
                        float magenta1 = BicubicUtil.CubicHermite(p01.M, p11.M, p21.M, p31.M, xfract);
                        float magenta2 = BicubicUtil.CubicHermite(p02.M, p12.M, p22.M, p32.M, xfract);
                        float magenta3 = BicubicUtil.CubicHermite(p03.M, p13.M, p23.M, p33.M, xfract);

                        float magenta = BicubicUtil.CubicHermite(magenta0, magenta1, magenta2, magenta3, yfract);

                        float yellow0 = BicubicUtil.CubicHermite(p00.Y, p10.Y, p20.Y, p30.Y, xfract);
                        float yellow1 = BicubicUtil.CubicHermite(p01.Y, p11.Y, p21.Y, p31.Y, xfract);
                        float yellow2 = BicubicUtil.CubicHermite(p02.Y, p12.Y, p22.Y, p32.Y, xfract);
                        float yellow3 = BicubicUtil.CubicHermite(p03.Y, p13.Y, p23.Y, p33.Y, xfract);

                        float yellow = BicubicUtil.CubicHermite(yellow0, yellow1, yellow2, yellow3, yfract);

                        float black0 = BicubicUtil.CubicHermite(p00.K, p10.K, p20.K, p30.K, xfract);
                        float black1 = BicubicUtil.CubicHermite(p01.K, p11.K, p21.K, p31.K, xfract);
                        float black2 = BicubicUtil.CubicHermite(p02.K, p12.K, p22.K, p32.K, xfract);
                        float black3 = BicubicUtil.CubicHermite(p03.K, p13.K, p23.K, p33.K, xfract);

                        float black = BicubicUtil.CubicHermite(black0, black1, black2, black3, yfract);

                        destRow->C = (byte)FloatUtil.Clamp(cyan, 0, 255);
                        destRow->M = (byte)FloatUtil.Clamp(magenta, 0, 255);
                        destRow->Y = (byte)FloatUtil.Clamp(yellow, 0, 255);
                        destRow->K = (byte)FloatUtil.Clamp(black, 0, 255);
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
