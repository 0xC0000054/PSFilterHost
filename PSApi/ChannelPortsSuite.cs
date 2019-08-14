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

using PSFilterHostDll.Imaging;
using System;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.PSApi
{
    internal sealed class ChannelPortsSuite : IDisposable
    {
        private readonly IFilterImageProvider filterImageProvider;
        private readonly ImageModes imageMode;
        private readonly ReadPixelsProc readPixelsProc;
        private readonly WriteBasePixelsProc writeBasePixelsProc;
        private readonly ReadPortForWritePortProc readPortForWritePortProc;

        private SurfaceBase scaledChannelSurface;
        private SurfaceBase ditheredChannelSurface;
        private SurfaceGray8 scaledSelectionMask;
        private ImageModes ditheredChannelImageMode;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelPortsSuite"/> class.
        /// </summary>
        /// <param name="filterImageProvider">The filter image provider.</param>
        /// <param name="imageMode">The image mode.</param>
        /// <exception cref="ArgumentNullException"><paramref name="filterImageProvider"/> is null.</exception>
        public ChannelPortsSuite(IFilterImageProvider filterImageProvider, ImageModes imageMode)
        {
            if (filterImageProvider == null)
            {
                throw new ArgumentNullException(nameof(filterImageProvider));
            }

            this.filterImageProvider = filterImageProvider;
            this.imageMode = imageMode;
            readPixelsProc = new ReadPixelsProc(ReadPixelsProc);
            writeBasePixelsProc = new WriteBasePixelsProc(WriteBasePixels);
            readPortForWritePortProc = new ReadPortForWritePortProc(ReadPortForWritePort);
            scaledChannelSurface = null;
            ditheredChannelSurface = null;
            scaledSelectionMask = null;
            disposed = false;
        }

        public unsafe IntPtr CreateChannelPortsSuitePointer()
        {
            IntPtr channelPortsPtr = Memory.Allocate(Marshal.SizeOf(typeof(ChannelPortProcs)), true);

            ChannelPortProcs* channelPorts = (ChannelPortProcs*)channelPortsPtr.ToPointer();
            channelPorts->channelPortProcsVersion = PSConstants.kCurrentChannelPortProcsVersion;
            channelPorts->numChannelPortProcs = PSConstants.kCurrentChannelPortProcsCount;
            channelPorts->readPixelsProc = Marshal.GetFunctionPointerForDelegate(readPixelsProc);
            channelPorts->writeBasePixelsProc = Marshal.GetFunctionPointerForDelegate(writeBasePixelsProc);
            channelPorts->readPortForWritePortProc = Marshal.GetFunctionPointerForDelegate(readPortForWritePortProc);

            return channelPortsPtr;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                if (scaledChannelSurface != null)
                {
                    scaledChannelSurface.Dispose();
                    scaledChannelSurface = null;
                }

                if (ditheredChannelSurface != null)
                {
                    ditheredChannelSurface.Dispose();
                    ditheredChannelSurface = null;
                }

                if (scaledSelectionMask != null)
                {
                    scaledSelectionMask.Dispose();
                    scaledSelectionMask = null;
                }
            }
        }

        private static unsafe void FillChannelData(int channel, PixelMemoryDesc destiniation, SurfaceBase source, VRect srcRect, ImageModes mode)
        {
            byte* dstPtr = (byte*)destiniation.data.ToPointer();
            int stride = destiniation.rowBits / 8;
            int bpp = destiniation.colBits / 8;
            int offset = destiniation.bitOffset / 8;

            switch (mode)
            {
                case ImageModes.GrayScale:

                    for (int y = srcRect.top; y < srcRect.bottom; y++)
                    {
                        byte* src = source.GetPointAddressUnchecked(srcRect.left, y);
                        byte* dst = dstPtr + (y * stride) + offset;
                        for (int x = srcRect.left; x < srcRect.right; x++)
                        {
                            *dst = *src;

                            src++;
                            dst += bpp;
                        }
                    }

                    break;
                case ImageModes.RGB:

                    for (int y = srcRect.top; y < srcRect.bottom; y++)
                    {
                        byte* src = source.GetPointAddressUnchecked(srcRect.left, y);
                        byte* dst = dstPtr + (y * stride) + offset;
                        for (int x = srcRect.left; x < srcRect.right; x++)
                        {
                            switch (channel)
                            {
                                case PSConstants.ChannelPorts.Red:
                                    *dst = src[2];
                                    break;
                                case PSConstants.ChannelPorts.Green:
                                    *dst = src[1];
                                    break;
                                case PSConstants.ChannelPorts.Blue:
                                    *dst = src[0];
                                    break;
                                case PSConstants.ChannelPorts.Alpha:
                                    *dst = src[3];
                                    break;
                            }
                            src += 4;
                            dst += bpp;
                        }
                    }

                    break;
                case ImageModes.Gray16:

                    for (int y = srcRect.top; y < srcRect.bottom; y++)
                    {
                        ushort* src = (ushort*)source.GetPointAddressUnchecked(srcRect.left, y);
                        ushort* dst = (ushort*)(dstPtr + (y * stride) + offset);
                        for (int x = srcRect.left; x < srcRect.right; x++)
                        {
                            *dst = *src;

                            src++;
                            dst += bpp;
                        }
                    }

                    break;
                case ImageModes.RGB48:

                    for (int y = srcRect.top; y < srcRect.bottom; y++)
                    {
                        ushort* src = (ushort*)source.GetPointAddressUnchecked(srcRect.left, y);
                        ushort* dst = (ushort*)(dstPtr + (y * stride) + offset);
                        for (int x = srcRect.left; x < srcRect.right; x++)
                        {
                            switch (channel)
                            {
                                case PSConstants.ChannelPorts.Red:
                                    *dst = src[2];
                                    break;
                                case PSConstants.ChannelPorts.Green:
                                    *dst = src[1];
                                    break;
                                case PSConstants.ChannelPorts.Blue:
                                    *dst = src[0];
                                    break;
                                case PSConstants.ChannelPorts.Alpha:
                                    *dst = src[3];
                                    break;
                            }
                            src += 4;
                            dst += bpp;
                        }
                    }

                    break;
            }
        }

        private static unsafe void FillSelectionMask(PixelMemoryDesc destiniation, SurfaceGray8 source, VRect srcRect)
        {
            byte* dstPtr = (byte*)destiniation.data.ToPointer();
            int stride = destiniation.rowBits / 8;
            int bpp = destiniation.colBits / 8;
            int offset = destiniation.bitOffset / 8;

            for (int y = srcRect.top; y < srcRect.bottom; y++)
            {
                byte* src = source.GetPointAddressUnchecked(srcRect.left, y);
                byte* dst = dstPtr + (y * stride) + offset;
                for (int x = srcRect.left; x < srcRect.right; x++)
                {
                    *dst = *src;

                    src++;
                    dst += bpp;
                }
            }
        }

        private unsafe short CreateDitheredChannelPortSurface(SurfaceBase source)
        {
            int width = source.Width;
            int height = source.Height;

            try
            {
                switch (imageMode)
                {
                    case ImageModes.Gray16:

                        ditheredChannelImageMode = ImageModes.GrayScale;
                        ditheredChannelSurface = SurfaceFactory.CreateFromImageMode(width, height, ditheredChannelImageMode);

                        for (int y = 0; y < height; y++)
                        {
                            ushort* src = (ushort*)source.GetRowAddressUnchecked(y);
                            byte* dst = ditheredChannelSurface.GetRowAddressUnchecked(y);
                            for (int x = 0; x < width; x++)
                            {
                                *dst = (byte)((*src * 10) / 1285);

                                src++;
                                dst++;
                            }
                        }
                        break;

                    case ImageModes.RGB48:

                        ditheredChannelImageMode = ImageModes.RGB;
                        ditheredChannelSurface = SurfaceFactory.CreateFromImageMode(width, height, ditheredChannelImageMode);

                        for (int y = 0; y < height; y++)
                        {
                            ushort* src = (ushort*)source.GetRowAddressUnchecked(y);
                            byte* dst = ditheredChannelSurface.GetRowAddressUnchecked(y);
                            for (int x = 0; x < width; x++)
                            {
                                dst[0] = (byte)((src[0] * 10) / 1285);
                                dst[1] = (byte)((src[1] * 10) / 1285);
                                dst[2] = (byte)((src[2] * 10) / 1285);
                                dst[3] = (byte)((src[3] * 10) / 1285);

                                src += 4;
                                dst += 4;
                            }
                        }

                        break;
                }
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.noErr;
        }

        private unsafe short ReadPixelsProc(IntPtr port, ref PSScaling scaling, ref VRect writeRect, ref PixelMemoryDesc destination, ref VRect wroteRect)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.ChannelPorts, string.Format("port: {0}, rect: {1}", port.ToString(), writeRect.ToString()));
#endif

            if (destination.depth != 8 && destination.depth != 16)
            {
                return PSError.errUnsupportedDepth;
            }

            // The offsets must be aligned to a System.Byte.
            if ((destination.bitOffset % 8) != 0)
            {
                return PSError.errUnsupportedBitOffset;
            }

            if ((destination.colBits % 8) != 0)
            {
                return PSError.errUnsupportedColBits;
            }

            if ((destination.rowBits % 8) != 0)
            {
                return PSError.errUnsupportedRowBits;
            }

            int channel = port.ToInt32();

            if (channel < PSConstants.ChannelPorts.Gray || channel > PSConstants.ChannelPorts.SelectionMask)
            {
                return PSError.errUnknownPort;
            }

            VRect srcRect = scaling.sourceRect;
            VRect dstRect = scaling.destinationRect;

            int srcWidth = srcRect.right - srcRect.left;
            int srcHeight = srcRect.bottom - srcRect.top;
            int dstWidth = dstRect.right - dstRect.left;
            int dstHeight = dstRect.bottom - dstRect.top;
            bool isSelection = channel == PSConstants.ChannelPorts.SelectionMask;
            SurfaceBase source = filterImageProvider.Source;

            if ((source.BitsPerChannel == 8 || isSelection) && destination.depth == 16)
            {
                return PSError.errUnsupportedDepthConversion; // converting 8-bit image data to 16-bit is not supported.
            }

            if (isSelection)
            {
                if (srcWidth == dstWidth && srcHeight == dstHeight)
                {
                    FillSelectionMask(destination, filterImageProvider.Mask, srcRect);
                }
                else if (dstWidth < srcWidth || dstHeight < srcHeight) // scale down
                {
                    if ((scaledSelectionMask == null) || scaledSelectionMask.Width != dstWidth || scaledSelectionMask.Height != dstHeight)
                    {
                        if (scaledSelectionMask != null)
                        {
                            scaledSelectionMask.Dispose();
                            scaledSelectionMask = null;
                        }

                        try
                        {
                            scaledSelectionMask = new SurfaceGray8(dstWidth, dstHeight);
                            scaledSelectionMask.FitSurface(filterImageProvider.Mask);
                        }
                        catch (OutOfMemoryException)
                        {
                            return PSError.memFullErr;
                        }
                    }

                    FillSelectionMask(destination, scaledSelectionMask, dstRect);
                }
                else if (dstWidth > srcWidth || dstHeight > srcHeight) // scale up
                {
                    if ((scaledSelectionMask == null) || scaledSelectionMask.Width != dstWidth || scaledSelectionMask.Height != dstHeight)
                    {
                        if (scaledSelectionMask != null)
                        {
                            scaledSelectionMask.Dispose();
                            scaledSelectionMask = null;
                        }

                        try
                        {
                            scaledSelectionMask = new SurfaceGray8(dstWidth, dstHeight);
                            scaledSelectionMask.FitSurface(filterImageProvider.Mask);
                        }
                        catch (OutOfMemoryException)
                        {
                            return PSError.memFullErr;
                        }
                    }

                    FillSelectionMask(destination, scaledSelectionMask, dstRect);
                }
            }
            else
            {
                ImageModes mode = imageMode;

                if (source.BitsPerChannel == 16 && destination.depth == 8)
                {
                    if (ditheredChannelSurface == null)
                    {
                        short err = CreateDitheredChannelPortSurface(source);
                        if (err != PSError.noErr)
                        {
                            return err;
                        }
                    }
                    mode = ditheredChannelImageMode;
                }

                if (srcWidth == dstWidth && srcHeight == dstHeight)
                {
                    FillChannelData(channel, destination, ditheredChannelSurface ?? source, srcRect, mode);
                }
                else if (dstWidth < srcWidth || dstHeight < srcHeight) // scale down
                {
                    if ((scaledChannelSurface == null) || scaledChannelSurface.Width != dstWidth || scaledChannelSurface.Height != dstHeight)
                    {
                        if (scaledChannelSurface != null)
                        {
                            scaledChannelSurface.Dispose();
                            scaledChannelSurface = null;
                        }

                        try
                        {
                            scaledChannelSurface = SurfaceFactory.CreateFromImageMode(dstWidth, dstHeight, mode);
                            scaledChannelSurface.FitSurface(ditheredChannelSurface ?? source);
                        }
                        catch (OutOfMemoryException)
                        {
                            return PSError.memFullErr;
                        }
                    }

                    FillChannelData(channel, destination, scaledChannelSurface, dstRect, mode);
                }
                else if (dstWidth > srcWidth || dstHeight > srcHeight) // scale up
                {
                    if ((scaledChannelSurface == null) || scaledChannelSurface.Width != dstWidth || scaledChannelSurface.Height != dstHeight)
                    {
                        if (scaledChannelSurface != null)
                        {
                            scaledChannelSurface.Dispose();
                            scaledChannelSurface = null;
                        }

                        try
                        {
                            scaledChannelSurface = SurfaceFactory.CreateFromImageMode(dstWidth, dstHeight, mode);
                            scaledChannelSurface.FitSurface(ditheredChannelSurface ?? source);
                        }
                        catch (OutOfMemoryException)
                        {
                            return PSError.memFullErr;
                        }
                    }

                    FillChannelData(channel, destination, scaledChannelSurface, dstRect, mode);
                }
            }

            wroteRect = dstRect;

            return PSError.noErr;
        }

        private short WriteBasePixels(IntPtr port, ref VRect writeRect, PixelMemoryDesc srcDesc)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.ChannelPorts, string.Format("port: {0}, rect: {1}", port.ToString(), writeRect.ToString()));
#endif
            return PSError.memFullErr;
        }

        private short ReadPortForWritePort(ref IntPtr readPort, IntPtr writePort)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.ChannelPorts, string.Format("readPort: {0}, writePort: {1}", readPort.ToString(), writePort.ToString()));
#endif
            return PSError.memFullErr;
        }
    }
}
