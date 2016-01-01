/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2016 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System.ComponentModel;
using PSFilterHostDll.PSApi;

#if GDIPLUS
using System.Drawing;
#else
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging; 
#endif

namespace PSFilterHostDll.BGRASurface
{
	/// <summary>
	/// Factory for the classes derived from SurfaceBase.
	/// </summary>
	internal static class SurfaceFactory
	{

#if GDIPLUS 
		/// <summary>
		/// Creates a new surface based on the PixelFormat of the Bitmap.
		/// </summary>
		/// <param name="image">The input Bitmap.</param>
		/// <param name="imageMode">The ImageMode of the current surface.</param>
		/// <returns></returns>
		internal static unsafe SurfaceBase CreateFromGdipBitmap(Bitmap image, out ImageModes imageMode)
		{
			int width = image.Width;
			int height = image.Height;

			imageMode = ImageModes.RGB;
			Surface32 surface = new Surface32(width, height);

			using (Bitmap temp = new Bitmap(image)) // Copy the image to remove any invalid meta-data that causes LockBits to fail.
			{
				System.Drawing.Imaging.BitmapData data = temp.LockBits(new System.Drawing.Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

				try
				{
					byte* scan0 = (byte*)data.Scan0.ToPointer();
					int stride = data.Stride;

					ulong length = (ulong)width * 4UL;
					for (int y = 0; y < height; y++)
					{
						BGRASurfaceMemory.Copy(surface.GetRowAddressUnchecked(y), scan0 + (y * stride), length);
					}
				}
				finally
				{
					temp.UnlockBits(data);
				}  
			}
			

			return surface;
		}
#else
		/// <summary>
		/// Creates a new surface based on the PixelFormat of the BitmapSource.
		/// </summary>
		/// <param name="bitmap">The input BitmapSource.</param>
		/// <param name="imageMode">The ImageMode of the current surface.</param>
		/// <returns></returns>
		internal static unsafe SurfaceBase CreateFromBitmapSource(BitmapSource bitmap, out ImageModes imageMode)
		{
			System.Windows.Media.PixelFormat format = bitmap.Format;
			int width = bitmap.PixelWidth;
			int height = bitmap.PixelHeight;

			if (format == PixelFormats.BlackWhite || format == PixelFormats.Gray2 || format == PixelFormats.Gray4 || format == PixelFormats.Gray8)
			{
				imageMode = ImageModes.GrayScale;
				Surface8 surface = new Surface8(width, height);

				if (format != PixelFormats.Gray8)
				{
					FormatConvertedBitmap conv = new FormatConvertedBitmap(bitmap, PixelFormats.Gray8, null, 0.0);
					conv.CopyPixels(Int32Rect.Empty, surface.Scan0.Pointer, (int)surface.Scan0.Length, width);
				}
				else
				{
					bitmap.CopyPixels(Int32Rect.Empty, surface.Scan0.Pointer, (int)surface.Scan0.Length, width);
				}

				return surface;
			}
			else if (format == PixelFormats.Gray16 || format == PixelFormats.Gray32Float)
			{
				imageMode = ImageModes.Gray16;
				Surface16 surface = new Surface16(width, height);

				if (format == PixelFormats.Gray32Float)
				{
					FormatConvertedBitmap conv = new FormatConvertedBitmap(bitmap, PixelFormats.Gray16, null, 0.0);
					conv.CopyPixels(Int32Rect.Empty, surface.Scan0.Pointer, (int)surface.Scan0.Length, width * 2);
				}
				else
				{
					bitmap.CopyPixels(Int32Rect.Empty, surface.Scan0.Pointer, (int)surface.Scan0.Length, width * 2);
				}

				surface.ScaleToPhotoshop16BitRange();

				return surface;
			}
			else if (format == PixelFormats.Rgb48 || format == PixelFormats.Rgba64 || format == PixelFormats.Rgba128Float || format == PixelFormats.Rgb128Float ||
				format == PixelFormats.Prgba128Float || format == PixelFormats.Prgba64)
			{
				int bpp, stride;
				ushort[] pixels = null;
				if (format == PixelFormats.Rgba128Float || format == PixelFormats.Rgb128Float || format == PixelFormats.Prgba128Float || format == PixelFormats.Prgba64)
				{
					PixelFormat dstFormat = format == PixelFormats.Rgb128Float ? PixelFormats.Rgb48 : PixelFormats.Rgba64;
					FormatConvertedBitmap conv = new FormatConvertedBitmap(bitmap, dstFormat, null, 0.0);

					bpp = dstFormat.BitsPerPixel / 16;
					stride = width * bpp;
					pixels = new ushort[stride * height];

					conv.CopyPixels(pixels, stride * 2, 0);
				}
				else
				{
					bpp = format.BitsPerPixel / 16;
					stride = width * bpp;
					pixels = new ushort[stride * height];

					bitmap.CopyPixels(pixels, stride * 2, 0);
				}

				imageMode = ImageModes.RGB48;
				Surface64 surface = new Surface64(width, height);

				fixed (ushort* ptr = pixels)
				{
					for (int y = 0; y < height; y++)
					{
						ushort* src = ptr + (y * stride);
						ColorBgra16* dst = (ColorBgra16*)surface.GetRowAddressUnchecked(y);
						for (int x = 0; x < width; x++)
						{
							dst->R = src[0];
							dst->G = src[1];
							dst->B = src[2];

							if (format == PixelFormats.Rgba64)
							{
								dst->A = src[3];
							}
							else
							{
								dst->A = 65535;
							}

							src += bpp;
							dst++;
						}
					}
				}

				surface.ScaleToPhotoshop16BitRange();

				return surface;
			}
			else
			{
				imageMode = ImageModes.RGB;
				Surface32 surface = new Surface32(width, height);

				if (format != PixelFormats.Bgra32)
				{
					FormatConvertedBitmap conv = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0.0);
					conv.CopyPixels(Int32Rect.Empty, surface.Scan0.Pointer, (int)surface.Scan0.Length, width * 4);
				}
				else
				{
					bitmap.CopyPixels(Int32Rect.Empty, surface.Scan0.Pointer, (int)surface.Scan0.Length, width * 4);
				}

				return surface;
			}

		} 
#endif

		/// <summary>
		/// Creates an new surface from the host image mode.
		/// </summary>
		/// <param name="width">The width of the new surface.</param>
		/// <param name="height">The height of the new surface.</param>
		/// <param name="mode">The current <see cref="ImageModes"/> of the host.</param>
		/// <returns></returns>
		internal static SurfaceBase CreateFromImageMode(int width, int height, ImageModes mode)
		{
			switch (mode)
			{
				case ImageModes.GrayScale:
					return new Surface8(width, height);
				case ImageModes.RGB:
					return new Surface32(width, height);
				case ImageModes.Gray16:
					return new Surface16(width, height);
				case ImageModes.RGB48:
					return new Surface64(width, height);
				default:
					throw new InvalidEnumArgumentException("mode", (int)mode, typeof(ImageModes));
			}
		}
	}
}
