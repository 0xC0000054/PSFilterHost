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

using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HostTest
{
	internal static class BitmapFrameExtensions
	{
		/// <summary>
		/// Determines whether the specified frame uses an alpha channel <see cref="PixelFormat"/> with an opaque alpha channel.
		/// </summary>
		/// <param name="frame">The <see cref="BitmapFrame"/> to check.</param>
		/// <returns>The actual pixel format of the image.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="frame"/> is null.</exception>
		public static PixelFormat DetectDishonestAlphaFormat(this BitmapFrame frame)
		{
			if (frame == null)
			{
				throw new ArgumentNullException(nameof(frame));
			}

			PixelFormat format = frame.Format;
			PixelFormat actualFormat = format;

			// WIC will sometimes load images as an alpha format with an opaque alpha channel.

			if (format == PixelFormats.Bgra32 || format == PixelFormats.Pbgra32)
			{
				if (!HasTransparency(frame))
				{
					actualFormat = PixelFormats.Bgr24;
				}
			}
			else if (format == PixelFormats.Rgba64 || format == PixelFormats.Prgba64)
			{
				if (!HasTransparency(frame))
				{
					actualFormat = PixelFormats.Rgb48;
				}
			}
			else if (format == PixelFormats.Rgba128Float || format == PixelFormats.Prgba128Float)
			{
				if (!HasTransparency(frame))
				{
					actualFormat = PixelFormats.Rgb128Float;
				}
			}

			return actualFormat;
		}

		/// <summary>
		/// Determines whether the specified image has transparency.
		/// </summary>
		/// <param name="image">The image.</param>
		/// <returns><c>true</c> if the image has transparency; otherwise, <c>false</c></returns>
		/// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
		private static unsafe bool HasTransparency(BitmapSource image)
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
	}
}
