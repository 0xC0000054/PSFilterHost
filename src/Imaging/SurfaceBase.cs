﻿/////////////////////////////////////////////////////////////////////////////////
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
    internal abstract class SurfaceBase : IDisposable, ISurfaceBase
    {
        protected readonly int width;
        protected readonly int height;
        protected readonly long stride;
        protected readonly double dpiX;
        protected readonly double dpiY;
        protected MemoryBlock scan0;
        private readonly int bytesPerPixel;
        private bool disposed;

        public int Width => width;

        public int Height => height;

        public unsafe MemoryBlock Scan0 => scan0;

        public long Stride => stride;

        public Rectangle Bounds => new Rectangle(0, 0, width, height);

        public double DpiX => dpiX;

        public double DpiY => dpiY;

        public abstract int ChannelCount
        {
            get;
        }

        public abstract int BitsPerChannel
        {
            get;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SurfaceBase"/> class.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="bytesPerPixel">The bytes per pixel.</param>
        /// <param name="dpiX">The horizontal resolution of the surface.</param>
        /// <param name="dpiY">The vertical resolution of the surface.</param>
        protected SurfaceBase(int width, int height, int bytesPerPixel, double dpiX, double dpiY)
            : this(width, height, bytesPerPixel, dpiX, dpiY, SurfaceCreationOptions.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SurfaceBase" /> class.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="bytesPerPixel">The bytes per pixel.</param>
        /// <param name="dpiX">The horizontal resolution of the surface.</param>
        /// <param name="dpiY">The vertical resolution of the surface.</param>
        /// <param name="options">The surface creation options.</param>
        protected SurfaceBase(int width, int height, int bytesPerPixel, double dpiX, double dpiY, SurfaceCreationOptions options)
        {
            this.width = width;
            this.height = height;
            this.bytesPerPixel = bytesPerPixel;
            stride = width * bytesPerPixel;
            if ((options & SurfaceCreationOptions.GdiPlusCompatableStride) == SurfaceCreationOptions.GdiPlusCompatableStride)
            {
                // GDI+ expects the stride to be a multiple of four.
                stride = (stride + 3) & ~3;
            }

            this.dpiX = dpiX;
            this.dpiY = dpiY;
            scan0 = new MemoryBlock(stride * height);
            disposed = false;
        }

        /// <summary>
        /// Determines if the requested pixel coordinate is within bounds.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <returns>true if (x,y) is in bounds, false if it's not.</returns>
        public bool IsVisible(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        public void CopySurface(SurfaceBase source)
        {
            if (width == source.width &&
               height == source.height &&
               stride == source.stride &&
               (width * bytesPerPixel) == stride)
            {
                unsafe
                {
                    ImageSurfaceMemory.Copy(scan0.VoidStar,
                                source.Scan0.VoidStar,
                                ((ulong)(height - 1) * (ulong)stride) + ((ulong)width * (ulong)bytesPerPixel));
                }
            }
            else
            {
                int copyWidth = Math.Min(width, source.Width);
                int copyHeight = Math.Min(height, source.Height);

                unsafe
                {
                    for (int y = 0; y < copyHeight; ++y)
                    {
                        ImageSurfaceMemory.Copy(GetRowAddressUnchecked(y), source.GetRowAddressUnchecked(y), (ulong)copyWidth * (ulong)bytesPerPixel);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a lookup table for mapping the 16-bit pixel data from [0, 65535] to the internal 16 bit range used by Adobe(R) Photoshop(R).
        /// </summary>
        /// <returns>The resulting lookup table.</returns>
        protected static ushort[] CreatePhotoshopRangeLookupTable()
        {
            ushort[] table = new ushort[65536];

            // According to the Photoshop SDK 16-bit image data is stored in the range of [0, 32768].
            for (int i = 0; i < table.Length; i++)
            {
                table[i] = (ushort)(((i * 32768) + 32767) / 65535);
            }

            return table;
        }

        /// <summary>
        /// Normalizes the 16-bit image data to [0, 65535] from the internal 16 bit range used by Adobe(R) Photoshop(R).
        /// </summary>
        /// <param name="x">The value to normalize.</param>
        /// <returns>The normalized value.</returns>
        protected static ushort Fix16BitRange(ushort x)
        {
            int value = x * 2; // double the value and clamp between 0 and 65535.

            if (value < 0)
            {
                return 0;
            }

            if (value > 65535)
            {
                return 65535;
            }

            return (ushort)value;
        }
#if GDIPLUS
        public abstract unsafe Bitmap ToGdipBitmap();
#else
        public abstract unsafe System.Windows.Media.Imaging.BitmapSource ToBitmapSource();
#endif
        public unsafe byte* GetRowAddressUnchecked(int y)
        {
            return (byte*)scan0.VoidStar + (y * stride);
        }

        public unsafe byte* GetPointAddress(int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= Height)
            {
                throw new ArgumentOutOfRangeException("(x,y)", new Point(x, y), "Coordinates out of range, max=" + new Size(width - 1, height - 1).ToString());
            }

            return GetPointAddressUnchecked(x, y);
        }

        public unsafe byte* GetPointAddressClamped(int x, int y)
        {
            int clampedX = Int32Util.Clamp(x, 0, width - 1);
            int clampedY = Int32Util.Clamp(y, 0, height - 1);

            return GetPointAddressUnchecked(clampedX, clampedY);
        }

        public unsafe byte* GetPointAddressUnchecked(int x, int y)
        {
            return (byte*)scan0.VoidStar + (y * stride) + (x * bytesPerPixel);
        }

        /// <summary>
        /// Fits the source surface to this surface.
        /// </summary>
        /// <param name="source">The Surface to read pixels from.</param>
        public void FitSurface(SurfaceBase source)
        {
            if (width == source.width && height == source.height)
            {
                CopySurface(source);
            }
            else
            {
                FitSurfaceImpl(source);
            }
        }

        protected abstract unsafe void FitSurfaceImpl(SurfaceBase source);

        public virtual unsafe bool HasTransparency()
        {
            return false;
        }

        public void SetAlphaToOpaque()
        {
            SetAlphaToOpaqueImpl(Bounds);
        }

        public void SetAlphaToOpaque(Rectangle[] scans)
        {
            if (scans == null)
            {
                throw new ArgumentNullException(nameof(scans));
            }

            for (int i = 0; i < scans.Length; i++)
            {
                SetAlphaToOpaqueImpl(scans[i]);
            }
        }

        protected virtual unsafe void SetAlphaToOpaqueImpl(Rectangle rect)
        {

        }

        private void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                disposed = true;
                if (scan0 != null)
                {
                    scan0.Dispose();
                    scan0 = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
