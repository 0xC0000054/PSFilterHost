/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2015 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

// Portions of this code derived from:
/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See License-pdn.txt for full licensing and attribution details.             //
//                                                                             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HostTest.Tools
{
    /// <summary>
    /// The abstract base class of the selection tools.
    /// </summary>
    [Serializable]
    internal abstract class SelectionBase : IDisposable
    {

        [field: NonSerialized]
        public event EventHandler<SelectionPathChangedEventArgs> SelectedPathChanged;
        [field: NonSerialized]
        public EventHandler<CursorChangedEventArgs> CursorChanged;

        private bool tracking;
        private List<Point> selectPoints;

        public SelectionBase()
        {
            this.tracking = false;
            this.selectPoints = null;
            this.disposed = false;
        }

        internal void MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                this.tracking = true;

                this.selectPoints = new List<Point>();
                this.selectPoints.Add(new Point(e.X, e.Y));

                this.OnCursorChanged(Cursors.Cross);
            }
        }

        internal void MouseMove(object sender, MouseEventArgs e)
        {
            if (tracking)
            {
                Point newPoint = new Point(e.X, e.Y);

                if (newPoint != selectPoints[selectPoints.Count - 1])
                {
                    selectPoints.Add(newPoint);
                }
                this.RenderSelection();
            }
        }
        internal void MouseUp(object sender, MouseEventArgs e)
        {
            this.tracking = false;
            this.OnCursorChanged(Cursors.Default);    
            this.RenderSelection();
        }

        internal void KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.KeyCode == Keys.Escape) || (e.Modifiers == Keys.Control && e.KeyCode == Keys.D))
            {
                e.Handled = true;// clear the selection if Escape or Ctrl + D is pressed.
                this.selectPoints.Clear();

                this.RenderSelection();
            }
        }

        protected virtual List<Point> TrimShapePath(List<Point> trimPoints)
        {
            return trimPoints;
        }

        private static List<PointF> PointListToPointFList(List<Point> points)
        {
            List<PointF> array = new List<PointF>(points.Count);

            for (int i = 0; i < points.Count; i++)
            {
                array.Add(points[i]);
            }

            return array;
        }

        protected List<PointF> CreateShape(List<Point> points)
        {
            return PointListToPointFList(points);
        }

        protected static Rectangle PointsToRectangle(PointF a, PointF b)
        {
            int x = (int)Math.Min(a.X, b.X);
            int y = (int)Math.Min(a.Y, b.Y);
            int width = (int)Math.Abs(a.X - b.X) + 1;
            int height = (int)Math.Abs(a.Y - b.Y) + 1;

            return new Rectangle(x, y, width, height);
        }
       
        protected virtual GraphicsPath RenderShape(List<PointF> shapePoints)
        {
            // the PointF structure is used by the ElipseSelectTool.
            GraphicsPath path = new GraphicsPath();
            Rectangle bounds = PointsToRectangle(shapePoints[0], shapePoints[shapePoints.Count - 1]);
            path.AddRectangle(bounds);

            return path;
        }

        protected void RenderSelection()
        {
            if (selectPoints != null)
            {
                List<Point> trimPoints = this.TrimShapePath(this.selectPoints);
                List<PointF> shapePoints = this.CreateShape(trimPoints);

                EventHandler<SelectionPathChangedEventArgs> handler = SelectedPathChanged;
                if (handler != null)
                { 
                    GraphicsPath path = null;

                    try
                    {
                        if (shapePoints.Count >= 2)
                        {
                            path = RenderShape(shapePoints);
                        }

                        using (SelectionPathChangedEventArgs args = new SelectionPathChangedEventArgs(path))
                        {
                            handler(this, args);
                        }
                    }
                    finally
                    {
                        if (path != null)
                        {
                            path.Dispose();
                        }
                    } 
                } 
            }

        }

        protected void OnCursorChanged(Cursor cursor)
        {
            if (this.CursorChanged != null)
            {
                this.CursorChanged(this, new CursorChangedEventArgs(cursor));
            }
        }

        private bool disposed;
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        public void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (selectPoints != null)
                    {
                        this.selectPoints.Clear();
                    } 

                    this.disposed = true;
                }
            }

        }

    }
}
