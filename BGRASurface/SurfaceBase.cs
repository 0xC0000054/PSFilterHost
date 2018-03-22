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
using System.Drawing;

namespace PSFilterHostDll.BGRASurface
{
	internal abstract class SurfaceBase : IDisposable
	{
		protected readonly int width;
		protected readonly int height;
		protected readonly long stride;
		protected readonly double dpiX;
		protected readonly double dpiY;
		protected MemoryBlock scan0;
		private readonly int bytesPerPixel;
		private bool disposed;

		public int Width
		{
			get { return width; }
		}

		public int Height
		{
			get { return height; }
		}

		public unsafe MemoryBlock Scan0
		{
			get { return scan0; }
		}

		public long Stride
		{
			get { return stride; }
		}

		public Rectangle Bounds
		{
			get { return new Rectangle(0, 0, width, height); }
		}

		public double DpiX
		{
			get { return dpiX; }
		}

		public double DpiY
		{
			get { return dpiY; }
		}

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
		public SurfaceBase(int width, int height, int bytesPerPixel, double dpiX, double dpiY)
		{
			this.width = width;
			this.height = height;
			this.bytesPerPixel = bytesPerPixel;
			this.stride = width * bytesPerPixel;
			this.dpiX = dpiX;
			this.dpiY = dpiY;
			this.scan0 = new MemoryBlock(stride * height);
			this.disposed = false;
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
			if (this.width == source.width &&
			   this.height == source.height &&
			   this.stride == source.stride &&
			   (this.width * this.bytesPerPixel) == this.stride)
			{
				unsafe
				{
					BGRASurfaceMemory.Copy(this.scan0.VoidStar,
								source.Scan0.VoidStar,
								((ulong)(height - 1) * (ulong)stride) + ((ulong)width * (ulong)this.bytesPerPixel));
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
						BGRASurfaceMemory.Copy(this.GetRowAddressUnchecked(y), source.GetRowAddressUnchecked(y), (ulong)copyWidth * (ulong)this.bytesPerPixel);
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

		public unsafe abstract Bitmap CreateAliasedBitmap();
#if !GDIPLUS
		public unsafe abstract System.Windows.Media.Imaging.BitmapSource CreateAliasedBitmapSource();
#endif
		public unsafe byte* GetRowAddressUnchecked(int y)
		{
			return ((byte*)scan0.VoidStar + (y * stride));
		}

		public unsafe byte* GetPointAddress(int x, int y)
		{
			if (x < 0 || y < 0 || x >= this.width || y >= this.Height)
			{
				throw new ArgumentOutOfRangeException("(x,y)", new Point(x, y), "Coordinates out of range, max=" + new Size(this.width - 1, this.height - 1).ToString());
			}

			return GetPointAddressUnchecked(x, y);
		}

		public unsafe byte* GetPointAddressUnchecked(int x, int y)
		{
			return (((byte*)scan0.VoidStar + (y * stride)) + (x * bytesPerPixel));
		}

		/// <summary>
		/// Fits the source surface to this surface using bicubic interpolation.
		/// </summary>
		/// <param name="source">The Surface to read pixels from.</param>
		/// <remarks>
		/// This method was implemented with correctness, not performance, in mind.
		/// Based on: "Bicubic Interpolation for Image Scaling" by Paul Bourke,
		///           http://astronomy.swin.edu.au/%7Epbourke/colour/bicubic/
		/// </remarks>
		public void BicubicFitSurface(SurfaceBase source)
		{
			float leftF = (1 * (float)(width - 1)) / (float)(source.width - 1);
			float topF = (1 * (height - 1)) / (float)(source.height - 1);
			float rightF = ((float)(source.width - 3) * (float)(width - 1)) / (float)(source.width - 1);
			float bottomF = ((float)(source.Height - 3) * (float)(height - 1)) / (float)(source.height - 1);

			int left = (int)Math.Ceiling((double)leftF);
			int top = (int)Math.Ceiling((double)topF);
			int right = (int)Math.Floor((double)rightF);
			int bottom = (int)Math.Floor((double)bottomF);

			Rectangle[] rois = new Rectangle[] {
												   Rectangle.FromLTRB(left, top, right, bottom),
												   new Rectangle(0, 0, width, top),
												   new Rectangle(0, top, left, height - top),
												   new Rectangle(right, top, width - right, height - top),
												   new Rectangle(left, bottom, right - left, height - bottom)
											   };
			Rectangle dstRoi = this.Bounds;
			for (int i = 0; i < rois.Length; ++i)
			{
				rois[i].Intersect(dstRoi);

				if (rois[i].Width > 0 && rois[i].Height > 0)
				{
					if (i == 0)
					{
						BicubicFitSurfaceUnchecked(source, rois[i]);
					}
					else
					{
						BicubicFitSurfaceChecked(source, rois[i]);
					}
				}
			}
		}

		protected static double CubeClamped(double x)
		{
			if (x >= 0)
			{
				return x * x * x;
			}
			else
			{
				return 0;
			}
		}

		/// <summary>
		/// Implements R() as defined at http://astronomy.swin.edu.au/%7Epbourke/colour/bicubic/
		/// </summary>
		protected static double R(double x)
		{
			return (CubeClamped(x + 2) - (4 * CubeClamped(x + 1)) + (6 * CubeClamped(x)) - (4 * CubeClamped(x - 1))) / 6;
		}

		protected unsafe abstract void BicubicFitSurfaceUnchecked(SurfaceBase source, Rectangle dstRoi);
		protected unsafe abstract void BicubicFitSurfaceChecked(SurfaceBase source, Rectangle dstRoi);

		public unsafe abstract void SuperSampleFitSurface(SurfaceBase source);


		public unsafe virtual bool HasTransparency()
		{
			return false;
		}

		public void SetAlphaToOpaque()
		{
			SetAlphaToOpaqueImpl(this.Bounds);
		}

		public void SetAlphaToOpaque(Rectangle[] scans)
		{
			if (scans == null)
				throw new ArgumentNullException("scans");

			for (int i = 0; i < scans.Length; i++)
			{
				SetAlphaToOpaqueImpl(scans[i]);
			}
		}

		protected unsafe virtual void SetAlphaToOpaqueImpl(Rectangle rect)
		{

		}

		private void Dispose(bool disposing)
		{
			if (!disposed && disposing)
			{
				this.disposed = true;
				if (scan0 != null)
				{
					this.scan0.Dispose();
					this.scan0 = null;
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
