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

using System.Windows.Media;

namespace HostTest
{
    internal static class PixelFormatExtensions
    {
        public static bool IsAlphaFormat(this PixelFormat format)
        {
            return (format == PixelFormats.Bgra32 ||
                    format == PixelFormats.Rgba64 ||
                    format == PixelFormats.Rgba128Float ||
                    format == PixelFormats.Pbgra32 ||
                    format == PixelFormats.Prgba64 ||
                    format == PixelFormats.Prgba128Float);
        }

        public static int GetBitsPerChannel(this PixelFormat format)
        {
            return (format.BitsPerPixel / format.Masks.Count);
        }
    }
}
