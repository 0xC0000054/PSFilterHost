﻿/////////////////////////////////////////////////////////////////////////////////
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

using System;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.PSApi
{
    internal sealed class ImageServicesSuite
    {
        private readonly PIResampleProc interpolate1DProc;
        private readonly PIResampleProc interpolate2DProc;

        public ImageServicesSuite()
        {
            interpolate1DProc = new PIResampleProc(Interpolate1DProc);
            interpolate2DProc = new PIResampleProc(Interpolate2DProc);
        }

        public unsafe IntPtr CreateImageServicesSuitePointer()
        {
            IntPtr imageServicesProcsPtr = Memory.Allocate(Marshal.SizeOf(typeof(ImageServicesProcs)), true);

            ImageServicesProcs* imageServicesProcs = (ImageServicesProcs*)imageServicesProcsPtr.ToPointer();

            imageServicesProcs->imageServicesProcsVersion = PSConstants.kCurrentImageServicesProcsVersion;
            imageServicesProcs->numImageServicesProcs = PSConstants.kCurrentImageServicesProcsCount;
            imageServicesProcs->interpolate1DProc = Marshal.GetFunctionPointerForDelegate(interpolate1DProc);
            imageServicesProcs->interpolate2DProc = Marshal.GetFunctionPointerForDelegate(interpolate2DProc);

            return imageServicesProcsPtr;
        }

        private short Interpolate1DProc(ref PSImagePlane source, ref PSImagePlane destination, ref Rect16 area, IntPtr coords, short method)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.ImageServices, string.Format("srcBounds: {0}, dstBounds: {1}, area: {2}, method: {3}",
                    new object[] { source.bounds.ToString(), destination.bounds.ToString(), area.ToString(), ((InterpolationModes)method).ToString() }));
#endif
            return PSError.memFullErr;
        }

        private unsafe short Interpolate2DProc(ref PSImagePlane source, ref PSImagePlane destination, ref Rect16 area, IntPtr coords, short method)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.ImageServices, string.Format("srcBounds: {0}, dstBounds: {1}, area: {2}, method: {3}",
                    new object[] { source.bounds.ToString(), destination.bounds.ToString(), area.ToString(), ((InterpolationModes)method).ToString() }));
#endif

            return PSError.memFullErr;
        }
    }
}
