/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Drawing;
using PSFilterHostDll.Interop;

namespace PSFilterHostDll.Imaging
{
    /// <summary>
    /// Surface class for 24 bits per pixel BGR image data. Each channel is allocated 8 bits per pixel.
    /// </summary>
    internal sealed class SurfaceBGR24 : SurfaceBase, IColorManagedSurface, IDisplayPixelsSurface
    {
        public SurfaceBGR24(int width, int height) : this(width, height, 96.0, 96.0)
        {
        }

        public SurfaceBGR24(int width, int height, double dpiX, double dpiY) : base(width, height, 3, dpiX, dpiY, SurfaceCreationOptions.GdiPlusCompatableStride)
        {
        }

        public override int ChannelCount => 3;

        public override int BitsPerChannel => 8;

        public NativeEnums.Mscms.BMFORMAT MscmsFormat => NativeEnums.Mscms.BMFORMAT.BM_RGBTRIPLETS;

        Bitmap IDisplayPixelsSurface.CreateAliasedBitmap()
        {
            return new Bitmap(width, height, (int)stride, System.Drawing.Imaging.PixelFormat.Format24bppRgb, scan0.Pointer);
        }

        bool IDisplayPixelsSurface.SupportsTransparency => false;

#if GDIPLUS
        public override unsafe Bitmap ToGdipBitmap()
        {
            Bitmap image = null;

            const System.Drawing.Imaging.PixelFormat format = System.Drawing.Imaging.PixelFormat.Format24bppRgb;

            using (Bitmap temp = new Bitmap(width, height, format))
            {
                System.Drawing.Imaging.BitmapData bitmapData = temp.LockBits(new Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.WriteOnly, format);
                try
                {
                    byte* destScan0 = (byte*)bitmapData.Scan0;
                    int destStride = bitmapData.Stride;

                    for (int y = 0; y < height; y++)
                    {
                        ColorBgr8* src = (ColorBgr8*)GetRowAddressUnchecked(y);
                        byte* dst = destScan0 + (y * destStride);

                        for (int x = 0; x < width; x++)
                        {
                            dst[0] = src->B;
                            dst[1] = src->G;
                            dst[2] = src->R;

                            src++;
                            dst += 3;
                        }
                    }
                }
                finally
                {
                    temp.UnlockBits(bitmapData);
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
                    System.Windows.Media.PixelFormats.Bgr24,
                    null);

            bitmap.Lock();
            try
            {
                byte* destScan0 = (byte*)bitmap.BackBuffer;
                int destStride = bitmap.BackBufferStride;

                for (int y = 0; y < height; y++)
                {
                    ColorBgra8* src = (ColorBgra8*)GetRowAddressUnchecked(y);
                    byte* dst = destScan0 + (y * destStride);

                    for (int x = 0; x < width; x++)
                    {
                        dst[0] = src->B;
                        dst[1] = src->G;
                        dst[2] = src->R;

                        src++;
                        dst += 3;
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
                    ColorBgr8* destRow = (ColorBgr8*)GetRowAddressUnchecked(y);
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
                        ColorBgr8 p00 = *(ColorBgr8*)source.GetPointAddressClamped(xint - 1, yint - 1);
                        ColorBgr8 p10 = *(ColorBgr8*)source.GetPointAddressClamped(xint + 0, yint - 1);
                        ColorBgr8 p20 = *(ColorBgr8*)source.GetPointAddressClamped(xint + 1, yint - 1);
                        ColorBgr8 p30 = *(ColorBgr8*)source.GetPointAddressClamped(xint + 2, yint - 1);

                        // 2nd row
                        ColorBgr8 p01 = *(ColorBgr8*)source.GetPointAddressClamped(xint - 1, yint + 0);
                        ColorBgr8 p11 = *(ColorBgr8*)source.GetPointAddressClamped(xint + 0, yint + 0);
                        ColorBgr8 p21 = *(ColorBgr8*)source.GetPointAddressClamped(xint + 1, yint + 0);
                        ColorBgr8 p31 = *(ColorBgr8*)source.GetPointAddressClamped(xint + 2, yint + 0);

                        // 3rd row
                        ColorBgr8 p02 = *(ColorBgr8*)source.GetPointAddressClamped(xint - 1, yint + 1);
                        ColorBgr8 p12 = *(ColorBgr8*)source.GetPointAddressClamped(xint + 0, yint + 1);
                        ColorBgr8 p22 = *(ColorBgr8*)source.GetPointAddressClamped(xint + 1, yint + 1);
                        ColorBgr8 p32 = *(ColorBgr8*)source.GetPointAddressClamped(xint + 2, yint + 1);

                        // 4th row
                        ColorBgr8 p03 = *(ColorBgr8*)source.GetPointAddressClamped(xint - 1, yint + 2);
                        ColorBgr8 p13 = *(ColorBgr8*)source.GetPointAddressClamped(xint + 0, yint + 2);
                        ColorBgr8 p23 = *(ColorBgr8*)source.GetPointAddressClamped(xint + 1, yint + 2);
                        ColorBgr8 p33 = *(ColorBgr8*)source.GetPointAddressClamped(xint + 2, yint + 2);

                        float blue0 = BicubicUtil.CubicHermite(p00.B, p10.B, p20.B, p30.B, xfract);
                        float blue1 = BicubicUtil.CubicHermite(p01.B, p11.B, p21.B, p31.B, xfract);
                        float blue2 = BicubicUtil.CubicHermite(p02.B, p12.B, p22.B, p32.B, xfract);
                        float blue3 = BicubicUtil.CubicHermite(p03.B, p13.B, p23.B, p33.B, xfract);

                        float blue = BicubicUtil.CubicHermite(blue0, blue1, blue2, blue3, yfract);

                        float green0 = BicubicUtil.CubicHermite(p00.G, p10.G, p20.G, p30.G, xfract);
                        float green1 = BicubicUtil.CubicHermite(p01.G, p11.G, p21.G, p31.G, xfract);
                        float green2 = BicubicUtil.CubicHermite(p02.G, p12.G, p22.G, p32.G, xfract);
                        float green3 = BicubicUtil.CubicHermite(p03.G, p13.G, p23.G, p33.G, xfract);

                        float green = BicubicUtil.CubicHermite(green0, green1, green2, green3, yfract);

                        float red0 = BicubicUtil.CubicHermite(p00.R, p10.R, p20.R, p30.R, xfract);
                        float red1 = BicubicUtil.CubicHermite(p01.R, p11.R, p21.R, p31.R, xfract);
                        float red2 = BicubicUtil.CubicHermite(p02.R, p12.R, p22.R, p32.R, xfract);
                        float red3 = BicubicUtil.CubicHermite(p03.R, p13.R, p23.R, p33.R, xfract);

                        float red = BicubicUtil.CubicHermite(red0, red1, red2, red3, yfract);

                        destRow->B = (byte)FloatUtil.Clamp(blue, 0, 255);
                        destRow->G = (byte)FloatUtil.Clamp(green, 0, 255);
                        destRow->R = (byte)FloatUtil.Clamp(red, 0, 255);
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
