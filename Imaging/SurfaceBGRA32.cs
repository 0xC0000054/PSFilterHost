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

// Adapted from:
/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See License-pdn.txt for full licensing and attribution details.             //
//                                                                             //
/////////////////////////////////////////////////////////////////////////////////
using System;
using System.Drawing;

namespace PSFilterHostDll.Imaging
{
    /// <summary>
    /// Surface class for 32 bits per pixel BGRA image data. Each channel is allocated 8 bits per pixel.
    /// </summary>
    internal sealed class SurfaceBGRA32 : SurfaceBase
    {
        public SurfaceBGRA32(int width, int height) : this(width, height, 96.0, 96.0)
        {
        }

        public SurfaceBGRA32(int width, int height, double dpiX, double dpiY) : base(width, height, 4, dpiX, dpiY)
        {
        }

        public override int ChannelCount => 4;

        public override int BitsPerChannel => 8;

        public unsafe Bitmap CreateAliasedBitmap()
        {
            return new Bitmap(width, height, (int)stride, System.Drawing.Imaging.PixelFormat.Format32bppArgb, scan0.Pointer);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "Required as Bitmap.SetResolution is documented to throw it.")]
        public override unsafe Bitmap ToGdipBitmap()
        {
            Bitmap image = null;

            System.Drawing.Imaging.PixelFormat format;
            if (HasTransparency())
            {
                format = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
            }
            else
            {
                format = System.Drawing.Imaging.PixelFormat.Format24bppRgb;
            }

            using (Bitmap temp = new Bitmap(width, height, format))
            {
                System.Drawing.Imaging.BitmapData bitmapData = temp.LockBits(new Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.WriteOnly, format);
                try
                {
                    byte* destScan0 = (byte*)bitmapData.Scan0;
                    int destStride = bitmapData.Stride;

                    if (format == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            ColorBgra8* src = (ColorBgra8*)GetRowAddressUnchecked(y);
                            byte* dst = destScan0 + (y * destStride);

                            for (int x = 0; x < width; x++)
                            {
                                dst[0] = src->B;
                                dst[1] = src->G;
                                dst[2] = src->R;
                                dst[3] = src->A;

                                src++;
                                dst += 4;
                            }
                        }
                    }
                    else
                    {
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

#if !GDIPLUS
        public override unsafe System.Windows.Media.Imaging.BitmapSource ToBitmapSource()
        {
            System.Windows.Media.PixelFormat format;

            IntPtr buffer;
            int bufferSize;
            int destStride;

            if (HasTransparency())
            {
                format = System.Windows.Media.PixelFormats.Bgra32;
                destStride = ((width * format.BitsPerPixel) + 7) / 8;
                bufferSize = destStride * height;

                buffer = PSApi.Memory.Allocate((ulong)bufferSize, PSApi.MemoryAllocationFlags.Default);

                byte* destScan0 = (byte*)buffer;

                for (int y = 0; y < height; y++)
                {
                    ColorBgra8* src = (ColorBgra8*)GetRowAddressUnchecked(y);
                    byte* dst = destScan0 + (y * destStride);

                    for (int x = 0; x < width; x++)
                    {
                        dst[0] = src->R;
                        dst[1] = src->G;
                        dst[2] = src->B;
                        dst[3] = src->A;

                        src++;
                        dst += 4;
                    }
                }
            }
            else
            {
                format = System.Windows.Media.PixelFormats.Bgr24;
                destStride = ((width * format.BitsPerPixel) + 7) / 8;

                bufferSize = destStride * height;

                buffer = PSApi.Memory.Allocate((ulong)bufferSize, PSApi.MemoryAllocationFlags.Default);

                byte* destScan0 = (byte*)buffer;

                for (int y = 0; y < height; y++)
                {
                    ColorBgra8* src = (ColorBgra8*)GetRowAddressUnchecked(y);
                    byte* dst = destScan0 + (y * destStride);

                    for (int x = 0; x < width; x++)
                    {
                        dst[0] = src->R;
                        dst[1] = src->G;
                        dst[2] = src->B;

                        src++;
                        dst += 3;
                    }
                }
            }

            return System.Windows.Media.Imaging.BitmapSource.Create(width, height, dpiX, dpiY, format, null, buffer, bufferSize, destStride);
        }
#endif

        public override unsafe bool HasTransparency()
        {
            for (int y = 0; y < height; y++)
            {
                ColorBgra8* ptr = (ColorBgra8*)GetRowAddressUnchecked(y);
                ColorBgra8* endPtr = ptr + width;

                while (ptr < endPtr)
                {
                    if (ptr->A < 255)
                    {
                        return true;
                    }

                    ptr++;
                }
            }

            return false;
        }

        public override unsafe void SuperSampleFitSurface(SurfaceBase source)
        {
            Rectangle dstRoi2 = Rectangle.Intersect(source.Bounds, Bounds);

            int srcHeight = source.Height;
            int srcWidth = source.Width;
            long srcStride = source.Stride;

            for (int dstY = dstRoi2.Top; dstY < dstRoi2.Bottom; ++dstY)
            {
                double srcTop = (double)(dstY * srcHeight) / (double)height;
                double srcTopFloor = Math.Floor(srcTop);
                double srcTopWeight = 1 - (srcTop - srcTopFloor);
                int srcTopInt = (int)srcTopFloor;

                double srcBottom = (double)((dstY + 1) * srcHeight) / (double)height;
                double srcBottomFloor = Math.Floor(srcBottom - 0.00001);
                double srcBottomWeight = srcBottom - srcBottomFloor;
                int srcBottomInt = (int)srcBottomFloor;

                ColorBgra8* dstPtr = (ColorBgra8*)GetPointAddressUnchecked(dstRoi2.Left, dstY);

                for (int dstX = dstRoi2.Left; dstX < dstRoi2.Right; ++dstX)
                {
                    double srcLeft = (double)(dstX * srcWidth) / (double)width;
                    double srcLeftFloor = Math.Floor(srcLeft);
                    double srcLeftWeight = 1 - (srcLeft - srcLeftFloor);
                    int srcLeftInt = (int)srcLeftFloor;

                    double srcRight = (double)((dstX + 1) * srcWidth) / (double)width;
                    double srcRightFloor = Math.Floor(srcRight - 0.00001);
                    double srcRightWeight = srcRight - srcRightFloor;
                    int srcRightInt = (int)srcRightFloor;

                    double blueSum = 0;
                    double greenSum = 0;
                    double redSum = 0;
                    double alphaSum = 0;

                    // left fractional edge
                    ColorBgra8* srcLeftPtr = (ColorBgra8*)source.GetPointAddressUnchecked(srcLeftInt, srcTopInt + 1);

                    for (int srcY = srcTopInt + 1; srcY < srcBottomInt; ++srcY)
                    {
                        double a = srcLeftPtr->A;
                        blueSum += srcLeftPtr->B * srcLeftWeight * a;
                        greenSum += srcLeftPtr->G * srcLeftWeight * a;
                        redSum += srcLeftPtr->R * srcLeftWeight * a;
                        alphaSum += srcLeftPtr->A * srcLeftWeight;
                        srcLeftPtr = (ColorBgra8*)((byte*)srcLeftPtr + srcStride);
                    }

                    // right fractional edge
                    ColorBgra8* srcRightPtr = (ColorBgra8*)source.GetPointAddressUnchecked(srcRightInt, srcTopInt + 1);
                    for (int srcY = srcTopInt + 1; srcY < srcBottomInt; ++srcY)
                    {
                        double a = srcRightPtr->A;
                        blueSum += srcRightPtr->B * srcRightWeight * a;
                        greenSum += srcRightPtr->G * srcRightWeight * a;
                        redSum += srcRightPtr->R * srcRightWeight * a;
                        alphaSum += srcRightPtr->A * srcRightWeight;
                        srcRightPtr = (ColorBgra8*)((byte*)srcRightPtr + srcStride);
                    }

                    // top fractional edge
                    ColorBgra8* srcTopPtr = (ColorBgra8*)source.GetPointAddressUnchecked(srcLeftInt + 1, srcTopInt);
                    for (int srcX = srcLeftInt + 1; srcX < srcRightInt; ++srcX)
                    {
                        double a = srcTopPtr->A;
                        blueSum += srcTopPtr->B * srcTopWeight * a;
                        greenSum += srcTopPtr->G * srcTopWeight * a;
                        redSum += srcTopPtr->R * srcTopWeight * a;
                        alphaSum += srcTopPtr->A * srcTopWeight;
                        ++srcTopPtr;
                    }

                    // bottom fractional edge
                    ColorBgra8* srcBottomPtr = (ColorBgra8*)source.GetPointAddressUnchecked(srcLeftInt + 1, srcBottomInt);
                    for (int srcX = srcLeftInt + 1; srcX < srcRightInt; ++srcX)
                    {
                        double a = srcBottomPtr->A;
                        blueSum += srcBottomPtr->B * srcBottomWeight * a;
                        greenSum += srcBottomPtr->G * srcBottomWeight * a;
                        redSum += srcBottomPtr->R * srcBottomWeight * a;
                        alphaSum += srcBottomPtr->A * srcBottomWeight;
                        ++srcBottomPtr;
                    }

                    // center area
                    for (int srcY = srcTopInt + 1; srcY < srcBottomInt; ++srcY)
                    {
                        ColorBgra8* srcPtr = (ColorBgra8*)source.GetPointAddressUnchecked(srcLeftInt + 1, srcY);

                        for (int srcX = srcLeftInt + 1; srcX < srcRightInt; ++srcX)
                        {
                            double a = srcPtr->A;
                            blueSum += (double)srcPtr->B * a;
                            greenSum += (double)srcPtr->G * a;
                            redSum += (double)srcPtr->R * a;
                            alphaSum += (double)srcPtr->A;
                            ++srcPtr;
                        }
                    }

                    // four corner pixels
                    ColorBgra8 srcTL = *(ColorBgra8*)source.GetPointAddress(srcLeftInt, srcTopInt);
                    double srcTLA = srcTL.A;
                    blueSum += srcTL.B * (srcTopWeight * srcLeftWeight) * srcTLA;
                    greenSum += srcTL.G * (srcTopWeight * srcLeftWeight) * srcTLA;
                    redSum += srcTL.R * (srcTopWeight * srcLeftWeight) * srcTLA;
                    alphaSum += srcTL.A * (srcTopWeight * srcLeftWeight);

                    ColorBgra8 srcTR = *(ColorBgra8*)source.GetPointAddress(srcRightInt, srcTopInt);
                    double srcTRA = srcTR.A;
                    blueSum += srcTR.B * (srcTopWeight * srcRightWeight) * srcTRA;
                    greenSum += srcTR.G * (srcTopWeight * srcRightWeight) * srcTRA;
                    redSum += srcTR.R * (srcTopWeight * srcRightWeight) * srcTRA;
                    alphaSum += srcTR.A * (srcTopWeight * srcRightWeight);

                    ColorBgra8 srcBL = *(ColorBgra8*)source.GetPointAddress(srcLeftInt, srcBottomInt);
                    double srcBLA = srcBL.A;
                    blueSum += srcBL.B * (srcBottomWeight * srcLeftWeight) * srcBLA;
                    greenSum += srcBL.G * (srcBottomWeight * srcLeftWeight) * srcBLA;
                    redSum += srcBL.R * (srcBottomWeight * srcLeftWeight) * srcBLA;
                    alphaSum += srcBL.A * (srcBottomWeight * srcLeftWeight);

                    ColorBgra8 srcBR = *(ColorBgra8*)source.GetPointAddress(srcRightInt, srcBottomInt);
                    double srcBRA = srcBR.A;
                    blueSum += srcBR.B * (srcBottomWeight * srcRightWeight) * srcBRA;
                    greenSum += srcBR.G * (srcBottomWeight * srcRightWeight) * srcBRA;
                    redSum += srcBR.R * (srcBottomWeight * srcRightWeight) * srcBRA;
                    alphaSum += srcBR.A * (srcBottomWeight * srcRightWeight);

                    double area = (srcRight - srcLeft) * (srcBottom - srcTop);

                    double alpha = alphaSum / area;
                    double blue;
                    double green;
                    double red;

                    if (alpha == 0)
                    {
                        blue = 0;
                        green = 0;
                        red = 0;
                    }
                    else
                    {
                        blue = blueSum / alphaSum;
                        green = greenSum / alphaSum;
                        red = redSum / alphaSum;
                    }

                    // add 0.5 so that rounding goes in the direction we want it to
                    blue += 0.5;
                    green += 0.5;
                    red += 0.5;
                    alpha += 0.5;

                    dstPtr->Bgra = (uint)blue + ((uint)green << 8) + ((uint)red << 16) + ((uint)alpha << 24);
                    ++dstPtr;
                }
            }
        }

        protected override unsafe void BicubicFitSurfaceUnchecked(SurfaceBase source, Rectangle dstRoi)
        {
            Rectangle roi = Rectangle.Intersect(dstRoi, Bounds);

            IntPtr rColCacheIP = ImageSurfaceMemory.Allocate(4 * (ulong)roi.Width * (ulong)sizeof(double));
            double* rColCache = (double*)rColCacheIP.ToPointer();

            int srcHeight = source.Height;
            int srcWidth = source.Width;
            long srcStride = source.Stride;

            // Precompute and then cache the value of R() for each column
            for (int dstX = roi.Left; dstX < roi.Right; ++dstX)
            {
                double srcColumn = (double)(dstX * (srcWidth - 1)) / (double)(width - 1);
                double srcColumnFloor = Math.Floor(srcColumn);
                double srcColumnFrac = srcColumn - srcColumnFloor;

                for (int m = -1; m <= 2; ++m)
                {
                    int index = (m + 1) + ((dstX - roi.Left) * 4);
                    double x = m - srcColumnFrac;
                    rColCache[index] = R(x);
                }
            }

            // Set this up so we can cache the R()'s for every row
            double* rRowCache = stackalloc double[4];

            for (int dstY = roi.Top; dstY < roi.Bottom; ++dstY)
            {
                double srcRow = (double)(dstY * (srcHeight - 1)) / (double)(height - 1);
                double srcRowFloor = Math.Floor(srcRow);
                double srcRowFrac = srcRow - srcRowFloor;
                int srcRowInt = (int)srcRow;
                ColorBgra8* dstPtr = (ColorBgra8*)GetPointAddressUnchecked(roi.Left, dstY);

                // Compute the R() values for this row
                for (int n = -1; n <= 2; ++n)
                {
                    double x = srcRowFrac - n;
                    rRowCache[n + 1] = R(x);
                }

                rColCache = (double*)rColCacheIP.ToPointer();
                ColorBgra8* srcRowPtr = (ColorBgra8*)source.GetRowAddressUnchecked(srcRowInt - 1);

                for (int dstX = roi.Left; dstX < roi.Right; dstX++)
                {
                    double srcColumn = (double)(dstX * (srcWidth - 1)) / (double)(width - 1);
                    double srcColumnFloor = Math.Floor(srcColumn);
                    int srcColumnInt = (int)srcColumn;

                    double blueSum = 0;
                    double greenSum = 0;
                    double redSum = 0;
                    double alphaSum = 0;
                    double totalWeight = 0;

                    ColorBgra8* srcPtr = srcRowPtr + srcColumnInt - 1;
                    for (int n = 0; n <= 3; ++n)
                    {
                        double w0 = rColCache[0] * rRowCache[n];
                        double w1 = rColCache[1] * rRowCache[n];
                        double w2 = rColCache[2] * rRowCache[n];
                        double w3 = rColCache[3] * rRowCache[n];

                        double a0 = srcPtr[0].A;
                        double a1 = srcPtr[1].A;
                        double a2 = srcPtr[2].A;
                        double a3 = srcPtr[3].A;

                        alphaSum += (a0 * w0) + (a1 * w1) + (a2 * w2) + (a3 * w3);
                        totalWeight += w0 + w1 + w2 + w3;

                        blueSum += (a0 * srcPtr[0].B * w0) + (a1 * srcPtr[1].B * w1) + (a2 * srcPtr[2].B * w2) + (a3 * srcPtr[3].B * w3);
                        greenSum += (a0 * srcPtr[0].G * w0) + (a1 * srcPtr[1].G * w1) + (a2 * srcPtr[2].G * w2) + (a3 * srcPtr[3].G * w3);
                        redSum += (a0 * srcPtr[0].R * w0) + (a1 * srcPtr[1].R * w1) + (a2 * srcPtr[2].R * w2) + (a3 * srcPtr[3].R * w3);

                        srcPtr = (ColorBgra8*)((byte*)srcPtr + srcStride);
                    }

                    double alpha = alphaSum / totalWeight;

                    double blue;
                    double green;
                    double red;

                    if (alpha == 0)
                    {
                        blue = 0;
                        green = 0;
                        red = 0;
                    }
                    else
                    {
                        blue = blueSum / alphaSum;
                        green = greenSum / alphaSum;
                        red = redSum / alphaSum;

                        // add 0.5 to ensure truncation to uint results in rounding
                        alpha += 0.5;
                        blue += 0.5;
                        green += 0.5;
                        red += 0.5;
                    }

                    dstPtr->Bgra = (uint)blue + ((uint)green << 8) + ((uint)red << 16) + ((uint)alpha << 24);
                    ++dstPtr;
                    rColCache += 4;
                } // for (dstX...
            } // for (dstY...

            ImageSurfaceMemory.Free(rColCacheIP);
        }

        protected override unsafe void BicubicFitSurfaceChecked(SurfaceBase source, Rectangle dstRoi)
        {
            Rectangle roi = Rectangle.Intersect(dstRoi, Bounds);

            IntPtr rColCacheIP = ImageSurfaceMemory.Allocate(4 * (ulong)roi.Width * (ulong)sizeof(double));
            double* rColCache = (double*)rColCacheIP.ToPointer();

            int srcHeight = source.Height;
            int srcWidth = source.Width;
            long srcStride = source.Stride;

            // Precompute and then cache the value of R() for each column
            for (int dstX = roi.Left; dstX < roi.Right; ++dstX)
            {
                double srcColumn = (double)(dstX * (srcWidth - 1)) / (double)(width - 1);
                double srcColumnFloor = Math.Floor(srcColumn);
                double srcColumnFrac = srcColumn - srcColumnFloor;

                for (int m = -1; m <= 2; ++m)
                {
                    int index = (m + 1) + ((dstX - roi.Left) * 4);
                    double x = m - srcColumnFrac;
                    rColCache[index] = R(x);
                }
            }

            // Set this up so we can cache the R()'s for every row
            double* rRowCache = stackalloc double[4];

            for (int dstY = roi.Top; dstY < roi.Bottom; ++dstY)
            {
                double srcRow = (double)(dstY * (srcHeight - 1)) / (double)(height - 1);
                double srcRowFloor = (double)Math.Floor(srcRow);
                double srcRowFrac = srcRow - srcRowFloor;
                int srcRowInt = (int)srcRow;
                ColorBgra8* dstPtr = (ColorBgra8*)GetPointAddressUnchecked(roi.Left, dstY);

                // Compute the R() values for this row
                for (int n = -1; n <= 2; ++n)
                {
                    double x = srcRowFrac - n;
                    rRowCache[n + 1] = R(x);
                }

                // See Perf Note below
                //int nFirst = Math.Max(-srcRowInt, -1);
                //int nLast = Math.Min(source.height - srcRowInt - 1, 2);

                for (int dstX = roi.Left; dstX < roi.Right; dstX++)
                {
                    double srcColumn = (double)(dstX * (srcWidth - 1)) / (double)(width - 1);
                    double srcColumnFloor = Math.Floor(srcColumn);
                    int srcColumnInt = (int)srcColumn;

                    double blueSum = 0;
                    double greenSum = 0;
                    double redSum = 0;
                    double alphaSum = 0;
                    double totalWeight = 0;

                    // See Perf Note below
                    //int mFirst = Math.Max(-srcColumnInt, -1);
                    //int mLast = Math.Min(source.width - srcColumnInt - 1, 2);

                    ColorBgra8* srcPtr = (ColorBgra8*)source.GetPointAddressUnchecked(srcColumnInt - 1, srcRowInt - 1);

                    for (int n = -1; n <= 2; ++n)
                    {
                        int srcY = srcRowInt + n;

                        for (int m = -1; m <= 2; ++m)
                        {
                            // Perf Note: It actually benchmarks faster on my system to do
                            // a bounds check for every (m,n) than it is to limit the loop
                            // to nFirst-Last and mFirst-mLast.
                            // I'm leaving the code above, albeit commented out, so that
                            // benchmarking between these two can still be performed.
                            if (source.IsVisible(srcColumnInt + m, srcY))
                            {
                                double w0 = rColCache[(m + 1) + (4 * (dstX - roi.Left))];
                                double w1 = rRowCache[n + 1];
                                double w = w0 * w1;

                                blueSum += srcPtr->B * w * srcPtr->A;
                                greenSum += srcPtr->G * w * srcPtr->A;
                                redSum += srcPtr->R * w * srcPtr->A;
                                alphaSum += srcPtr->A * w;

                                totalWeight += w;
                            }

                            ++srcPtr;
                        }

                        srcPtr = (ColorBgra8*)((byte*)(srcPtr - 4) + srcStride);
                    }

                    double alpha = alphaSum / totalWeight;
                    double blue;
                    double green;
                    double red;

                    if (alpha == 0)
                    {
                        blue = 0;
                        green = 0;
                        red = 0;
                    }
                    else
                    {
                        blue = blueSum / alphaSum;
                        green = greenSum / alphaSum;
                        red = redSum / alphaSum;

                        // add 0.5 to ensure truncation to uint results in rounding
                        alpha += 0.5;
                        blue += 0.5;
                        green += 0.5;
                        red += 0.5;
                    }

                    dstPtr->Bgra = (uint)blue + ((uint)green << 8) + ((uint)red << 16) + ((uint)alpha << 24);
                    ++dstPtr;
                } // for (dstX...
            } // for (dstY...

            ImageSurfaceMemory.Free(rColCacheIP);
        }

        protected override unsafe void SetAlphaToOpaqueImpl(Rectangle rect)
        {
            int top = rect.Top;
            int left = rect.Left;
            int right = rect.Right;
            int bottom = rect.Bottom;

            for (int y = top; y < bottom; y++)
            {
                ColorBgra8* ptr = (ColorBgra8*)GetPointAddressUnchecked(left, y);

                for (int x = left; x < right; x++)
                {
                    ptr->A = 255;

                    ptr++;
                }
            }
        }
    }
}
