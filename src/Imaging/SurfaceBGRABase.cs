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
    /// The base class for BGRA surfaces
    /// </summary>
    /// <seealso cref="SurfaceBase"/>
    internal abstract class SurfaceBGRABase : SurfaceBase
    {
        protected SurfaceBGRABase(int width, int height, int bytesPerPixel, double dpiX, double dpiY) : base(width, height, bytesPerPixel, dpiX, dpiY)
        {
        }

        public sealed override int ChannelCount => 4;

        protected sealed override void FitSurfaceImpl(SurfaceBase source)
        {
            // This method was implemented with correctness, not performance, in mind.
            // Based on: "Bicubic Interpolation for Image Scaling" by Paul Bourke,
            // http://astronomy.swin.edu.au/%7Epbourke/colour/bicubic/

            float leftF = (1 * (float)(width - 1)) / (float)(source.Width - 1);
            float topF = (1 * (height - 1)) / (float)(source.Height - 1);
            float rightF = ((float)(source.Width - 3) * (float)(width - 1)) / (float)(source.Width - 1);
            float bottomF = ((float)(source.Height - 3) * (float)(height - 1)) / (float)(source.Height - 1);

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
            Rectangle dstRoi = Bounds;
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

        protected abstract unsafe void BicubicFitSurfaceUnchecked(SurfaceBase source, Rectangle dstRoi);

        protected abstract unsafe void BicubicFitSurfaceChecked(SurfaceBase source, Rectangle dstRoi);
    }
}
