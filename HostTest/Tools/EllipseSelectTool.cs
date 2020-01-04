/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2020 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

// Portions of this code adapted from:
/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See License-pdn.txt for full licensing and attribution details.             //
//                                                                             //
/////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace HostTest.Tools
{
    /// <summary>
    /// The elliptical selection tool class.
    /// </summary>
    [System.Serializable]
    internal class EllipseSelectTool : SelectionBase
    {
        public EllipseSelectTool() : base()
        {
        }

        /// <summary>
        /// Trims the shape path.
        /// </summary>
        /// <param name="trimPoints">The trim points.</param>
        /// <returns>The trimmed shape points.</returns>
        protected override List<Point> TrimShapePath(List<Point> trimPoints)
        {
            // The following code is from Paint.NET.
            List<Point> array = new List<Point>();

            if (trimPoints.Count > 0)
            {
                array.Add(trimPoints[0]);

                if (trimPoints.Count > 1)
                {
                    array.Add(trimPoints[trimPoints.Count - 1]);
                }
            }

            return array;
        }

        protected override GraphicsPath RenderShape(List<PointF> shapePoints)
        {
            GraphicsPath path = new GraphicsPath();
            Rectangle bounds = PointsToRectangle(shapePoints[0], shapePoints[shapePoints.Count - 1]);
            path.AddEllipse(bounds);

            return path;
        }
    }
}
