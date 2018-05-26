/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft public License:
//   Copyright (C) 2012-2018 Nicholas Hayes
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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Security.Permissions;
using System.Windows.Forms;
using HostTest.Tools;

namespace HostTest
{
	/// <summary>
	/// The control that displays the loaded Bitmap.
	/// </summary>
	internal sealed class Canvas : Control
	{
		private Rectangle imageBounds;
		private Region selectionRegion;
		private Pen outlinePen1;
		private Pen outlinePen2;
		private Bitmap image;
		private Bitmap checkerBoardBitmap;
		private GraphicsPath path;
		private SelectionBase selectionClass;

		private static readonly float[] zoomFactors;
		private static readonly float minZoom;
		private static readonly float maxZoom;
		private float zoomFactor;
		private Bitmap scaledImage;
		private bool isDirty;
		private float selectionFactor;
		private GraphicsPath normalizedPath;

		private bool resetClip;
		private int suspendPaintCounter;

		public event EventHandler<CanvasZoomChangedEventArgs> ZoomChanged;
		public event EventHandler<CanvasDirtyChangedEventArgs> DirtyChanged;

		static Canvas()
		{
			zoomFactors = new float[] { 0.001f, 0.002f, 0.003f, 0.004f, 0.005f, 0.008f, 0.12f, 0.16f, 0.25f, 0.33f,
			0.5f, 0.66f, 1f};

			minZoom = zoomFactors[0];
			maxZoom = zoomFactors[zoomFactors.Length - 1];
		}

		public Canvas()
		{
			DoubleBuffered = true;
			base.SetStyle(ControlStyles.UserPaint, true);
			base.SetStyle(ControlStyles.Selectable, true);

			imageBounds = Rectangle.Empty;
			selectionRegion = null;
			image = null;
			checkerBoardBitmap = null;

			path = null;
			selectionClass = null;
			zoomFactor = 1f;
			resetClip = false;
			scaledImage = null;
			isDirty = false;
			normalizedPath = null;
		}

		public bool IsDirty
		{
			get
			{
				return isDirty;
			}
			set
			{
				if (isDirty != value)
				{
					isDirty = value;

					DirtyChanged?.Invoke(this, new CanvasDirtyChangedEventArgs(value));
				}
			}
		}

		public SelectionBase SelectionType
		{
			get
			{
				return selectionClass;
			}
			set
			{
				if (selectionClass != null)
				{
					MouseDown -= selectionClass.MouseDown;
					MouseMove -= selectionClass.MouseMove;
					MouseUp -= selectionClass.MouseUp;
					KeyDown -= selectionClass.KeyDown;
					selectionClass.CursorChanged -= OnCursorChanged;
					selectionClass.SelectedPathChanged -= OnSelectionPathChanged;

					selectionClass.Dispose();
					selectionClass = null;
				}
				if (selectionRegion != null)
				{
					RenderSelection(null, true);
				}

				if (value != null)
				{
					selectionClass = value;
					MouseDown += new MouseEventHandler(selectionClass.MouseDown);
					MouseMove += new MouseEventHandler(selectionClass.MouseMove);
					MouseUp += new MouseEventHandler(selectionClass.MouseUp);
					KeyDown += new KeyEventHandler(selectionClass.KeyDown);
					selectionClass.CursorChanged += new EventHandler<CursorChangedEventArgs>(OnCursorChanged);
					selectionClass.SelectedPathChanged += new EventHandler<SelectionPathChangedEventArgs>(OnSelectionPathChanged);
				}
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (image != null)
				{
					image.Dispose();
					image = null;
				}
				if (checkerBoardBitmap != null)
				{
					checkerBoardBitmap.Dispose();
					checkerBoardBitmap = null;
				}
				if (selectionRegion != null)
				{
					selectionRegion.Dispose();
					selectionRegion = null;
				}
				if (path != null)
				{
					path.Dispose();
					path = null;
				}
				if (selectionClass != null)
				{
					selectionClass.Dispose();
					selectionClass = null;
				}
				if (scaledImage != null)
				{
					scaledImage.Dispose();
					scaledImage = null;
				}
				if (outlinePen1 != null)
				{
					outlinePen1.Dispose();
					outlinePen1 = null;
				}
				if (outlinePen2 != null)
				{
					outlinePen2.Dispose();
					outlinePen2 = null;
				}
				if (normalizedPath != null)
				{
					normalizedPath.Dispose();
					normalizedPath = null;
				}
			}
			base.Dispose(disposing);
		}

		private void OnSelectionPathChanged(object sender, SelectionPathChangedEventArgs e)
		{
			if (e.SelectedPath != null)
			{
				selectionFactor = zoomFactor;
				RenderSelection(e.SelectedPath, false);
			}
			else
			{
				RenderSelection(null, true);
			}
		}

		private void OnCursorChanged(object sender, CursorChangedEventArgs e)
		{
			Cursor = e.NewCursor;
		}

		/// <summary>
		/// Renders the selection.
		/// </summary>
		/// <param name="selectPath">The selection path.</param>
		/// <param name="deSelect">If set to <c>true</c> clear the selection.</param>
		private void RenderSelection(GraphicsPath selectPath, bool deSelect)
		{
			if (selectPath != null && selectPath.PointCount > 2)
			{
				path = (GraphicsPath)selectPath.Clone();
				selectionRegion = new Region(selectPath);

				if (normalizedPath != null)
				{
					normalizedPath.Dispose();
					normalizedPath = null;
				}
			}
			else if (deSelect)
			{
				if (path != null)
				{
					path.Dispose();
					path = null;
				}

				if (normalizedPath != null)
				{
					normalizedPath.Dispose();
					normalizedPath = null;
				}

				if (selectionRegion != null)
				{
					selectionRegion.Dispose();
					selectionRegion = null;
				}
			}
			Invalidate();
		}

		private void ResetGraphicsClip()
		{
			resetClip = true;
			if (suspendPaintCounter == 0)
			{
				Invalidate();
				Update();
			}
		}

		/// <summary>
		/// Clears the selection.
		/// </summary>
		public void ClearSelection()
		{
			RenderSelection(null, true);
		}

		/// <summary>
		/// Suspends the redrawing of the canvas.
		/// </summary>
		public void SuspendPaint()
		{
			suspendPaintCounter++;
		}

		/// <summary>
		/// Resumes the redrawing of the canvas.
		/// </summary>
		public void ResumePaint()
		{
			suspendPaintCounter--;

			if (suspendPaintCounter == 0)
			{
				if (InvokeRequired)
				{
					BeginInvoke(new Action(delegate()
						{
							Invalidate();
							Update();
						}));
				}
				else
				{
					Invalidate();
					Update();
				}
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
#if DEBUG
			System.Diagnostics.Debug.Assert(suspendPaintCounter == 0);
#endif

			if (resetClip)
			{
				if (imageBounds.Width < e.ClipRectangle.Width && imageBounds.Height < e.ClipRectangle.Height)
				{
					e.Graphics.ResetClip();
					e.Graphics.SetClip(imageBounds);
				}

				resetClip = false;
			}

			if (image != null)
			{
				CompositingMode oldCM = e.Graphics.CompositingMode;

				e.Graphics.CompositingMode = CompositingMode.SourceCopy;
				if (checkerBoardBitmap != null)
				{
					e.Graphics.DrawImage(checkerBoardBitmap, e.ClipRectangle, e.ClipRectangle, GraphicsUnit.Pixel);
					e.Graphics.CompositingMode = CompositingMode.SourceOver;
				}

				if (scaledImage != null)
				{
					e.Graphics.DrawImage(scaledImage, e.ClipRectangle, e.ClipRectangle, GraphicsUnit.Pixel);
				}
				else
				{
					e.Graphics.DrawImage(image, e.ClipRectangle, e.ClipRectangle, GraphicsUnit.Pixel);
				}

				e.Graphics.CompositingMode = oldCM;

				if (selectionRegion != null)
				{
					// draw the selection outline.

					if (outlinePen1 == null)
					{
						outlinePen1 = new Pen(Color.FromArgb(160, Color.Black), 1.0f)
						{
							Alignment = PenAlignment.Outset,
							LineJoin = LineJoin.Bevel,
							Width = -1
						};
					}

					if (outlinePen2 == null)
					{
						outlinePen2 = new Pen(Color.White, 1.0f)
						{
							Alignment = PenAlignment.Outset,
							LineJoin = LineJoin.Bevel,
							MiterLimit = 2,
							Width = -1,
							DashStyle = DashStyle.Dash,
							DashPattern = new float[] { 4, 4 },
							Color = Color.White,
							DashOffset = 4.0f
						};
					}

					Graphics g = e.Graphics;

					PixelOffsetMode oldPOM = g.PixelOffsetMode;
					g.PixelOffsetMode = PixelOffsetMode.None;

					SmoothingMode oldSM = g.SmoothingMode;
					g.SmoothingMode = SmoothingMode.AntiAlias;

					// scale the selection region if necessary
					if (zoomFactor != selectionFactor)
					{
						float factor = zoomFactor / selectionFactor;

						using (GraphicsPath temp = (GraphicsPath)path.Clone())
						{
							using (Matrix matrix = new Matrix())
							{
								matrix.Scale(factor, factor);

								temp.Transform(matrix);
							}

							g.DrawPath(outlinePen1, temp);
							g.DrawPath(outlinePen2, temp);
						}
					}
					else
					{
						g.DrawPath(outlinePen1, path);
						g.DrawPath(outlinePen2, path);
					}

					g.PixelOffsetMode = oldPOM;
					g.SmoothingMode = oldSM;
				}
			}

			base.OnPaint(e);
		}

		/// <summary>
		/// Draws the checker board bitmap.
		/// </summary>
		/// <param name="width">The width of the Bitmap to be created.</param>
		/// <param name="height">The height of the Bitmap to be created.</param>
		[SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		private unsafe void DrawCheckerBoardBitmap(int width, int height)
		{
			checkerBoardBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

			BitmapData bd = checkerBoardBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			try
			{
				byte* scan0 = (byte*)bd.Scan0.ToPointer();
				int stride = bd.Stride;

				for (int y = 0; y < height; y++)
				{
					byte* p = scan0 + (y * stride);
					for (int x = 0; x < width; x++)
					{
						// This code was taken from Paint.NET
						byte v = (byte)((((x ^ y) & 8) * 8) + 191); // 8x8 pixel checkerboard tiles.

						p[0] = p[1] = p[2] = v;
						p[3] = 255;
						p += 4;
					}
				}
			}
			finally
			{
				checkerBoardBitmap.UnlockBits(bd);
			}
		}

		private static unsafe bool HasTransparency(Bitmap source)
		{
			if (Image.IsAlphaPixelFormat(source.PixelFormat))
			{
				int width = source.Width;
				int height = source.Height;

				BitmapData bd = source.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

				try
				{
					byte* scan0 = (byte*)bd.Scan0.ToPointer();
					int stride = bd.Stride;

					for (int y = 0; y < height; y++)
					{
						byte* ptr = scan0 + (y * stride);
						byte* ptrEnd = ptr + width;

						while (ptr < ptrEnd)
						{
							if (ptr[3] < 255)
							{
								return true;
							}
							ptr += 4;
						}
					}
				}
				finally
				{
					source.UnlockBits(bd);
				}
			}

			return false;
		}

		public Bitmap Surface
		{
			get
			{
				return image;
			}
			set
			{
				if (value != null)
				{
					if ((image == null) || image.Width != value.Width || image.Height != value.Height)
					{
						if (path != null)
						{
							path.Dispose();
							path = null;
						}

						if (normalizedPath != null)
						{
							normalizedPath.Dispose();
							normalizedPath = null;
						}

						if (selectionRegion != null)
						{
							selectionRegion.Dispose();
							selectionRegion = null;
						}

						if (checkerBoardBitmap != null)
						{
							checkerBoardBitmap.Dispose();
							checkerBoardBitmap = null;
						}

						if (image != null)
						{
							image.Dispose();
							image = null;
						}

						imageBounds = new Rectangle(0, 0, value.Width, value.Height);
						image = new Bitmap(value.Width, value.Height, value.PixelFormat);
						CopyFromBitmap(value, imageBounds);

						Size = new Size(value.Width, value.Height);

						ResetZoom(false);
						IsDirty = false;

						Invalidate();
					}
					else
					{
						CopyFromBitmap(value, new Rectangle(0, 0, value.Width, value.Height));
						IsDirty = true;
						ZoomCanvas();
					}
				}
			}
		}

		/// <summary>
		/// Gets the selection clipping path used by the filters.
		/// </summary>
		public GraphicsPath ClipPath
		{
			get
			{
				if (selectionFactor == 1f)
				{
					return path;
				}
				else
				{
					if (normalizedPath == null && path != null)
					{
						normalizedPath = (GraphicsPath)path.Clone();
						float factor = 1f / selectionFactor; // scale the selection up to 100%.

						using (Matrix matrix = new Matrix())
						{
							matrix.Scale(factor, factor);

							normalizedPath.Transform(matrix);
						}
					}

					return normalizedPath;
				}
			}
		}

		/// <summary>
		/// Copies from the source to the destination bitmap.
		/// </summary>
		/// <param name="source">The source bitmap.</param>
		/// <param name="bounds">The area of the bitmap to copy.</param>
		[SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		private unsafe void CopyFromBitmap(Bitmap source, Rectangle bounds)
		{
			bool hasAlpha = HasTransparency(source);

			if (hasAlpha && image.PixelFormat != PixelFormat.Format32bppArgb)
			{
				image.Dispose();
				image = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
			}
			else if (!hasAlpha && image.PixelFormat == PixelFormat.Format32bppArgb)
			{
				image.Dispose();
				image = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
			}

			if ((checkerBoardBitmap == null) && hasAlpha)
			{
				DrawCheckerBoardBitmap(source.Width, source.Height);
			}

			BitmapData srcData = source.LockBits(bounds, ImageLockMode.ReadOnly, source.PixelFormat);
			BitmapData dstData = image.LockBits(bounds, ImageLockMode.WriteOnly, image.PixelFormat);
			int width = source.Width;
			int height = source.Height;

			try
			{
				byte* srcPtr = (byte*)srcData.Scan0.ToPointer();
				int srcStride = srcData.Stride;
				byte* dstPtr = (byte*)dstData.Scan0.ToPointer();
				int dstStride = dstData.Stride;

				int srcBpp = Image.GetPixelFormatSize(source.PixelFormat) / 8;
				int dstBpp = Image.GetPixelFormatSize(image.PixelFormat) / 8;

				for (int y = 0; y < height; y++)
				{
					byte* src = srcPtr + (y * srcStride);
					byte* dst = dstPtr + (y * dstStride);

					for (int x = 0; x < width; x++)
					{
						dst[0] = src[0];
						dst[1] = src[1];
						dst[2] = src[2];

						if (hasAlpha)
						{
							dst[3] = src[3];
						}

						src += srcBpp;
						dst += dstBpp;
					}
				}
			}
			finally
			{
				source.UnlockBits(srcData);
				image.UnlockBits(dstData);
			}
		}

		public CanvasHistoryState ToCanvasHistoryState()
		{
			return new CanvasHistoryState(image);
		}

		/// <summary>
		/// Copies the CanvasHistoryState data to the canvas.
		/// </summary>
		/// <param name="historyState">The CanvasHistoryState to copy.</param>
		public void CopyFromHistoryState(CanvasHistoryState historyState)
		{
			Surface = historyState.Image;

			RenderSelection(path, false);
		}

		/// <summary>
		/// Zooms the image in.
		/// </summary>
		public void ZoomIn()
		{
			if (zoomFactor < maxZoom)
			{
				int index = -1;

				for (int i = 0; i < zoomFactors.Length; i++)
				{
					if (zoomFactors[i] > zoomFactor)
					{
						index = i;
						break;
					}
				}

				if (index == -1)
				{
					index = zoomFactors.Length - 1;
				}

				zoomFactor = zoomFactors[index];

				ZoomCanvas();
			}
		}

		/// <summary>
		/// Determines whether the image can zoom out.
		/// </summary>
		/// <returns>
		///   <c>true</c> if the image can zoom out; otherwise, <c>false</c>.
		/// </returns>
		public bool CanZoomOut()
		{
			if (zoomFactor > minZoom)
			{
				int index = 0;

				for (int i = zoomFactors.Length - 1; i >= 0; i--)
				{
					if (zoomFactors[i] < zoomFactor)
					{
						index = i;
						break;
					}
				}

				float factor = zoomFactors[index];

				return ((image.Width * factor) >= 1f && (image.Height * factor) >= 1f);
			}

			return false;
		}

		/// <summary>
		/// Determines whether the image can zoom in.
		/// </summary>
		/// <returns>
		///   <c>true</c> if the image can zoom in; otherwise, <c>false</c>.
		/// </returns>
		public bool CanZoomIn()
		{
			return (zoomFactor < maxZoom);
		}

		/// <summary>
		/// Zooms the image out.
		/// </summary>
		public void ZoomOut()
		{
			if (zoomFactor > minZoom)
			{
				int index = -1;

				for (int i = zoomFactors.Length - 1; i >= 0; i--)
				{
					if (zoomFactors[i] < zoomFactor)
					{
						index = i;
						break;
					}
				}

				if (index == -1)
				{
					index = 0;
				}

				zoomFactor = zoomFactors[index];

				ZoomCanvas();
			}
		}

		/// <summary>
		/// Zooms the image to fit in the specified window.
		/// </summary>
		/// <param name="windowSize"> The size of the window.</param>
		public void ZoomToWindow(Size windowSize)
		{
			if (image.Width > windowSize.Width || image.Height > windowSize.Height)
			{
				float ratioX = (float)windowSize.Width / (float)image.Width;
				float ratioY = (float)windowSize.Height / (float)image.Height;

				zoomFactor = ratioX < ratioY ? ratioX : ratioY;

				ZoomCanvas();
			}
			else
			{
				ResetZoom(true);
			}
		}

		/// <summary>
		/// Determines whether the image can zoom to fit the specified size.
		/// </summary>
		/// <param name="windowSize">Size of the window.</param>
		/// <returns>
		///   <c>true</c> if the image can zoom to fit the specified window size; otherwise, <c>false</c>.
		/// </returns>
		public bool CanZoomToWindow(Size windowSize)
		{
			if (image.Width > windowSize.Width || image.Height > windowSize.Height)
			{
				float ratioX = (float)windowSize.Width / (float)image.Width;
				float ratioY = (float)windowSize.Height / (float)image.Height;

				float ratio = ratioX < ratioY ? ratioX : ratioY;

				return (zoomFactor != ratio);
			}

			return false;
		}

		public void ZoomToActualSize()
		{
			zoomFactor = 1f;
			ZoomCanvas();
		}

		public bool CanZoomToActualSize()
		{
			return (zoomFactor != 1f);
		}

		public bool IsActualSize
		{
			get
			{
				return (zoomFactor == 1f);
			}
		}

		private void OnZoomChanged()
		{
			ZoomChanged?.Invoke(this, new CanvasZoomChangedEventArgs(zoomFactor));
		}

		private void ZoomCanvas()
		{
			if ((scaledImage == null) || scaledImage.Width != image.Width || scaledImage.Height != image.Height)
			{
				OnZoomChanged();

				if (scaledImage != null)
				{
					scaledImage.Dispose();
					scaledImage = null;
				}

				int imageWidth = image.Width;
				int imageHeight = image.Height;

				int scaledWidth = (int)((float)imageWidth * zoomFactor);
				int scaledHeight = (int)((float)imageHeight * zoomFactor);

				if (scaledWidth != imageWidth && scaledHeight != imageHeight)
				{
					scaledImage = new Bitmap(scaledWidth, scaledHeight, image.PixelFormat);
					imageBounds = new Rectangle(0, 0, scaledWidth, scaledHeight);

					using (Graphics gr = Graphics.FromImage(scaledImage))
					{
						gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
						gr.SmoothingMode = SmoothingMode.HighQuality;
						gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
						gr.CompositingQuality = CompositingQuality.HighQuality;

						gr.DrawImage(image, imageBounds, new Rectangle(0, 0, imageWidth, imageHeight), GraphicsUnit.Pixel);
					}
					Size = new Size(scaledWidth, scaledHeight);
				}
				else
				{
					imageBounds = new Rectangle(0, 0, imageWidth, imageHeight);
					Size = new Size(imageWidth, imageHeight);
				}

				ResetGraphicsClip();
			}
		}

		private void ResetZoom(bool invalidate)
		{
			if (scaledImage != null)
			{
				scaledImage.Dispose();
				scaledImage = null;

				zoomFactor = 1f;

				OnZoomChanged();

				if (invalidate && suspendPaintCounter == 0)
				{
					Invalidate();
					Update();
				}
			}
		}

		/// <summary>
		/// Copies the canvas image and resizes it to the specified size maintaining the aspect ratio.
		/// </summary>
		/// <param name="maxWidth">The maximum width.</param>
		/// <param name="maxHeight">The maximum height.</param>
		/// <returns>The scaled image.</returns>
		public Bitmap ResizeCopy(int maxWidth, int maxHeight)
		{
			if (image.Width > maxWidth || image.Height > maxHeight)
			{
				int imageWidth = image.Width;
				int imageHeight = image.Height;

				float ratioX = (float)maxWidth / (float)imageWidth;
				float ratioY = (float)maxHeight / (float)imageHeight;

				float ratio = ratioX < ratioY ? ratioX : ratioY;

				int newWidth = (int)((float)imageWidth * ratio);
				int newHeight = (int)((float)imageHeight * ratio);

				Bitmap scaled = null;
				Bitmap temp = null;

				try
				{
					temp = new Bitmap(newWidth, newHeight, image.PixelFormat);
					using (Graphics gr = Graphics.FromImage(temp))
					{
						gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
						gr.SmoothingMode = SmoothingMode.HighQuality;
						gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
						gr.CompositingQuality = CompositingQuality.HighQuality;

						gr.DrawImage(image, new Rectangle(0, 0, newWidth, newHeight), new Rectangle(0, 0, imageWidth, imageHeight), GraphicsUnit.Pixel);
					}

					scaled = temp;
					temp = null;
				}
				finally
				{
					if (temp != null)
					{
						temp.Dispose();
						temp = null;
					}
				}

				return scaled;
			}

			return image.Clone() as Bitmap;
		}

		public void ResetSize()
		{
			if (Size != image.Size)
			{
				Size = new Size(image.Width, image.Height);
			}
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			base.OnMouseWheel(e);

			if (ModifierKeys == Keys.Control && image != null)
			{
				if (e.Delta > 0)
				{
					ZoomIn();
				}
				else
				{
					ZoomOut();
				}
			}
		}

		private void InitializeComponent()
		{
			SuspendLayout();
			//
			// Canvas
			//
			Name = "Canvas";
			ResumeLayout(false);
		}
	}
}
