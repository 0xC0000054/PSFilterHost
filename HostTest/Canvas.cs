/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft public License:
//   Copyright (C) 2012-2017 Nicholas Hayes
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
			this.DoubleBuffered = true;
			base.SetStyle(ControlStyles.UserPaint, true);
			base.SetStyle(ControlStyles.Selectable, true);

			this.imageBounds = Rectangle.Empty;
			this.selectionRegion = null;
			this.image = null;
			this.checkerBoardBitmap = null;

			this.path = null;
			this.selectionClass = null;
			this.zoomFactor = 1f;
			this.resetClip = false;
			this.scaledImage = null;
			this.isDirty = false;
			this.normalizedPath = null;
		}

		public bool IsDirty
		{
			get
			{
				return this.isDirty;
			}
			set
			{
				if (this.isDirty != value)
				{
					this.isDirty = value;

					if (DirtyChanged != null)
					{
						DirtyChanged.Invoke(this, new CanvasDirtyChangedEventArgs(value));
					}
				}
			}
		}

		public SelectionBase SelectionType
		{
			get
			{
				return this.selectionClass;
			}
			set
			{

				if (this.selectionClass != null)
				{
					this.MouseDown -= this.selectionClass.MouseDown;
					this.MouseMove -= this.selectionClass.MouseMove;
					this.MouseUp -= this.selectionClass.MouseUp;
					this.KeyDown -= this.selectionClass.KeyDown;
					this.selectionClass.CursorChanged -= this.OnCursorChanged;
					this.selectionClass.SelectedPathChanged -= this.OnSelectionPathChanged;

					this.selectionClass.Dispose();
					this.selectionClass = null;
				}
				if (this.selectionRegion != null)
				{
					this.RenderSelection(null, true);
				}

				if (value != null)
				{
					this.selectionClass = value;
					this.MouseDown += new MouseEventHandler(selectionClass.MouseDown);
					this.MouseMove += new MouseEventHandler(selectionClass.MouseMove);
					this.MouseUp += new MouseEventHandler(selectionClass.MouseUp);
					this.KeyDown += new KeyEventHandler(selectionClass.KeyDown);
					this.selectionClass.CursorChanged += new EventHandler<CursorChangedEventArgs>(this.OnCursorChanged);
					this.selectionClass.SelectedPathChanged += new EventHandler<SelectionPathChangedEventArgs>(this.OnSelectionPathChanged);
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
				this.selectionFactor = this.zoomFactor;
				this.RenderSelection(e.SelectedPath, false);
			}
			else
			{
				this.RenderSelection(null, true);
			}

		}

		private void OnCursorChanged(object sender, CursorChangedEventArgs e)
		{
			this.Cursor = e.NewCursor;
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
				this.path = (GraphicsPath)selectPath.Clone();
				this.selectionRegion = new Region(selectPath);

				if (this.normalizedPath != null)
				{
					this.normalizedPath.Dispose();
					this.normalizedPath = null;
				}
			}
			else if (deSelect)
			{
				if (this.path != null)
				{
					this.path.Dispose();
					this.path = null;
				}

				if (this.normalizedPath != null)
				{
					this.normalizedPath.Dispose();
					this.normalizedPath = null;
				}

				if (this.selectionRegion != null)
				{
					this.selectionRegion.Dispose();
					this.selectionRegion = null;
				}
			}
			this.Invalidate();
		}

		private void ResetGraphicsClip()
		{
			this.resetClip = true;
			if (this.suspendPaintCounter == 0)
			{
				this.Invalidate();
				this.Update();
			}
		}

		/// <summary>
		/// Suspends the redrawing of the canvas.
		/// </summary>
		public void SuspendPaint()
		{
			this.suspendPaintCounter++;
		}

		/// <summary>
		/// Resumes the redrawing of the canvas.
		/// </summary>
		public void ResumePaint()
		{
			this.suspendPaintCounter--;

			if (this.suspendPaintCounter == 0)
			{
				if (base.InvokeRequired)
				{
					base.BeginInvoke(new Action(delegate()
						{
							this.Invalidate();
							this.Update();
						}));
				}
				else
				{
					this.Invalidate();
					this.Update();
				}
			}
		}

		protected override void OnPaint(PaintEventArgs pe)
		{
#if DEBUG
			System.Diagnostics.Debug.Assert(this.suspendPaintCounter == 0);
#endif

			if (this.resetClip)
			{
				if (this.imageBounds.Width < pe.ClipRectangle.Width && this.imageBounds.Height < pe.ClipRectangle.Height)
				{
					pe.Graphics.ResetClip();
					pe.Graphics.SetClip(this.imageBounds);
				}

				this.resetClip = false;
			}

			if (this.image != null)
			{
				CompositingMode oldCM = pe.Graphics.CompositingMode;

				pe.Graphics.CompositingMode = CompositingMode.SourceCopy;
				if (this.checkerBoardBitmap != null)
				{
					pe.Graphics.DrawImage(this.checkerBoardBitmap, pe.ClipRectangle, pe.ClipRectangle, GraphicsUnit.Pixel);
					pe.Graphics.CompositingMode = CompositingMode.SourceOver;
				}

				if (this.scaledImage != null)
				{
					pe.Graphics.DrawImage(this.scaledImage, pe.ClipRectangle, pe.ClipRectangle, GraphicsUnit.Pixel);
				}
				else
				{
					pe.Graphics.DrawImage(this.image, pe.ClipRectangle, pe.ClipRectangle, GraphicsUnit.Pixel);
				}

				pe.Graphics.CompositingMode = oldCM;

				if (this.selectionRegion != null)
				{
					// draw the selection outline.

					if (this.outlinePen1 == null)
					{
						this.outlinePen1 = new Pen(Color.FromArgb(160, Color.Black), 1.0f)
						{
							Alignment = PenAlignment.Outset,
							LineJoin = LineJoin.Bevel,
							Width = -1
						};
					}

					if (this.outlinePen2 == null)
					{
						this.outlinePen2 = new Pen(Color.White, 1.0f)
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

					Graphics g = pe.Graphics;

					PixelOffsetMode oldPOM = g.PixelOffsetMode;
					g.PixelOffsetMode = PixelOffsetMode.None;

					SmoothingMode oldSM = g.SmoothingMode;
					g.SmoothingMode = SmoothingMode.AntiAlias;

					// scale the selection region if necessary
					if (this.zoomFactor != this.selectionFactor)
					{
						float factor = this.zoomFactor / this.selectionFactor;

						using (GraphicsPath temp = (GraphicsPath)this.path.Clone())
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
						g.DrawPath(outlinePen1, this.path);
						g.DrawPath(outlinePen2, this.path);
					}

					g.PixelOffsetMode = oldPOM;
					g.SmoothingMode = oldSM;
				}

			}

			base.OnPaint(pe);
		}

		/// <summary>
		/// Draws the checker board bitmap.
		/// </summary>
		/// <param name="width">The width of the Bitmap to be created.</param>
		/// <param name="height">The height of the Bitmap to be created.</param>
		[SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		private unsafe void DrawCheckerBoardBitmap(int width, int height)
		{
			this.checkerBoardBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

			BitmapData bd = this.checkerBoardBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
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
				this.checkerBoardBitmap.UnlockBits(bd);
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
				return this.image;
			}
			set
			{
				if (value != null)
				{
					if ((this.image == null) || this.image.Width != value.Width || this.image.Height != value.Height)
					{

						if (this.path != null)
						{
							this.path.Dispose();
							this.path = null;
						}

						if (this.normalizedPath != null)
						{
							this.normalizedPath.Dispose();
							this.normalizedPath = null;
						}

						if (this.selectionRegion != null)
						{
							this.selectionRegion.Dispose();
							this.selectionRegion = null;
						}

						if (this.checkerBoardBitmap != null)
						{
							this.checkerBoardBitmap.Dispose();
							this.checkerBoardBitmap = null;
						}

						if (this.image != null)
						{
							this.image.Dispose();
							this.image = null;
						}

						this.imageBounds = new Rectangle(0, 0, value.Width, value.Height);

						if (HasTransparency(value))
						{
							this.image = value.Clone(imageBounds, PixelFormat.Format32bppArgb);
							this.DrawCheckerBoardBitmap(value.Width, value.Height);
						}
						else
						{
							this.image = value.Clone(imageBounds, PixelFormat.Format24bppRgb);
						}

						this.Size = new Size(value.Width, value.Height);

						this.ResetZoom(false);
						this.IsDirty = false;

						this.Invalidate();
					}
					else
					{
						this.CopyFromBitmap(value, new Rectangle(0, 0, value.Width, value.Height));
						this.IsDirty = true;
						this.ZoomCanvas();
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
				if (this.selectionFactor == 1f)
				{
					return this.path;
				}
				else
				{
					if (this.normalizedPath == null && this.path != null)
					{
						this.normalizedPath = (GraphicsPath)this.path.Clone();
						float factor = 1f / this.selectionFactor; // scale the selection up to 100%.

						using (Matrix matrix = new Matrix())
						{
							matrix.Scale(factor, factor);

							this.normalizedPath.Transform(matrix);
						}
					}

					return this.normalizedPath;
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

			if (hasAlpha && this.image.PixelFormat != PixelFormat.Format32bppArgb)
			{
				this.image.Dispose();
				this.image = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
			}
			else if (!hasAlpha && this.image.PixelFormat == PixelFormat.Format32bppArgb)
			{
				this.image.Dispose();
				this.image = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
			}

			if ((this.checkerBoardBitmap == null) && hasAlpha)
			{
				this.DrawCheckerBoardBitmap(source.Width, source.Height);
			}


			BitmapData srcData = source.LockBits(bounds, ImageLockMode.ReadOnly, source.PixelFormat);
			BitmapData dstData = this.image.LockBits(bounds, ImageLockMode.WriteOnly, image.PixelFormat);
			int width = source.Width;
			int height = source.Height;

			try
			{
				byte* srcPtr = (byte*)srcData.Scan0.ToPointer();
				int srcStride = srcData.Stride;
				byte* dstPtr = (byte*)dstData.Scan0.ToPointer();
				int dstStride = dstData.Stride;

				int srcBpp = Image.GetPixelFormatSize(source.PixelFormat) / 8;
				int dstBpp = Image.GetPixelFormatSize(this.image.PixelFormat) / 8;

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
				this.image.UnlockBits(dstData);
			}

		}

		public CanvasHistoryState ToCanvasHistoryState()
		{
			return new CanvasHistoryState(this.image);
		}

		/// <summary>
		/// Copies the CanvasHistoryState data to the canvas.
		/// </summary>
		/// <param name="historyState">The CanvasHistoryState to copy.</param>
		public void CopyFromHistoryState(CanvasHistoryState historyState)
		{
			this.Surface = historyState.Image;

			RenderSelection(this.path, false);
		}

		/// <summary>
		/// Zooms the image in.
		/// </summary>
		public void ZoomIn()
		{
			if (this.zoomFactor < maxZoom)
			{
				int index = -1;

				for (int i = 0; i < zoomFactors.Length; i++)
				{
					if (zoomFactors[i] > this.zoomFactor)
					{
						index = i;
						break;
					}
				}

				if (index == -1)
				{
					index = zoomFactors.Length - 1;
				}

				this.zoomFactor = zoomFactors[index];

				this.ZoomCanvas();
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
			if (this.zoomFactor > minZoom)
			{
				int index = 0;

				for (int i = zoomFactors.Length - 1; i >= 0; i--)
				{
					if (zoomFactors[i] < this.zoomFactor)
					{
						index = i;
						break;
					}
				}

				float factor = zoomFactors[index];

				return ((this.image.Width * factor) >= 1f && (this.image.Height * factor) >= 1f);
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
			return (this.zoomFactor < maxZoom);
		}

		/// <summary>
		/// Zooms the image out.
		/// </summary>
		public void ZoomOut()
		{
			if (this.zoomFactor > minZoom)
			{
				int index = -1;

				for (int i = zoomFactors.Length - 1; i >= 0; i--)
				{
					if (zoomFactors[i] < this.zoomFactor)
					{
						index = i;
						break;
					}
				}

				if (index == -1)
				{
					index = 0;
				}

				this.zoomFactor = zoomFactors[index];

				this.ZoomCanvas();
			}
		}

		/// <summary>
		/// Zooms the image to fit in the specified window.
		/// </summary>
		/// <param name="windowSize"> The size of the window.</param>
		public void ZoomToWindow(Size windowSize)
		{
			if (this.image.Width > windowSize.Width || this.image.Height > windowSize.Height)
			{
				float ratioX = (float)windowSize.Width / (float)this.image.Width;
				float ratioY = (float)windowSize.Height / (float)this.image.Height;

				this.zoomFactor = ratioX < ratioY ? ratioX : ratioY;

				this.ZoomCanvas();
			}
			else
			{
				this.ResetZoom(true);
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
			if (this.image.Width > windowSize.Width || this.image.Height > windowSize.Height)
			{
				float ratioX = (float)windowSize.Width / (float)this.image.Width;
				float ratioY = (float)windowSize.Height / (float)this.image.Height;

				float ratio = ratioX < ratioY ? ratioX : ratioY;

				return (this.zoomFactor != ratio);
			}

			return false;
		}

		public void ZoomToActualSize()
		{
			this.zoomFactor = 1f;
			this.ZoomCanvas();
		}

		public bool CanZoomToActualSize()
		{
			return (this.zoomFactor != 1f);
		}

		public bool IsActualSize
		{
			get
			{
				return (this.zoomFactor == 1f);
			}
		}

		private void OnZoomChanged()
		{
			if (ZoomChanged != null)
			{
				ZoomChanged.Invoke(this, new CanvasZoomChangedEventArgs(zoomFactor));
			}
		}

		private void ZoomCanvas()
		{
			if ((scaledImage == null) || scaledImage.Width != this.image.Width || scaledImage.Height != this.image.Height)
			{
				this.OnZoomChanged();

				if (scaledImage != null)
				{
					scaledImage.Dispose();
					scaledImage = null;
				}

				int imageWidth = this.image.Width;
				int imageHeight = this.image.Height;

				int scaledWidth = (int)((float)imageWidth * zoomFactor);
				int scaledHeight = (int)((float)imageHeight * zoomFactor);

				if (scaledWidth != imageWidth && scaledHeight != imageHeight)
				{
					this.scaledImage = new Bitmap(scaledWidth, scaledHeight, this.image.PixelFormat);
					this.imageBounds = new Rectangle(0, 0, scaledWidth, scaledHeight);

					using (Graphics gr = Graphics.FromImage(scaledImage))
					{
						gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
						gr.SmoothingMode = SmoothingMode.HighQuality;
						gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
						gr.CompositingQuality = CompositingQuality.HighQuality;

						gr.DrawImage(this.image, this.imageBounds, new Rectangle(0, 0, imageWidth, imageHeight), GraphicsUnit.Pixel);
					}
					this.Size = new Size(scaledWidth, scaledHeight);
				}
				else
				{
					this.imageBounds = new Rectangle(0, 0, imageWidth, imageHeight);
					this.Size = new Size(imageWidth, imageHeight);
				}

				this.ResetGraphicsClip();
			}
		}

		private void ResetZoom(bool invalidate)
		{
			if (this.scaledImage != null)
			{
				this.scaledImage.Dispose();
				this.scaledImage = null;

				this.zoomFactor = 1f;

				this.OnZoomChanged();

				if (invalidate && suspendPaintCounter == 0)
				{
					this.Invalidate();
					this.Update();
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
			if (this.image.Width > maxWidth || this.image.Height > maxHeight)
			{
				int imageWidth = this.image.Width;
				int imageHeight = this.image.Height;

				float ratioX = (float)maxWidth / (float)imageWidth;
				float ratioY = (float)maxHeight / (float)imageHeight;

				float ratio = ratioX < ratioY ? ratioX : ratioY;

				int newWidth = (int)((float)imageWidth * ratio);
				int newHeight = (int)((float)imageHeight * ratio);

				Bitmap scaled = null;
				Bitmap temp = null;

				try
				{
					temp = new Bitmap(newWidth, newHeight, this.image.PixelFormat);
					using (Graphics gr = Graphics.FromImage(temp))
					{
						gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
						gr.SmoothingMode = SmoothingMode.HighQuality;
						gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
						gr.CompositingQuality = CompositingQuality.HighQuality;

						gr.DrawImage(this.image, new Rectangle(0, 0, newWidth, newHeight), new Rectangle(0, 0, imageWidth, imageHeight), GraphicsUnit.Pixel);
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

			return this.image.Clone() as Bitmap;
		}

		public void ResetSize()
		{
			if (this.Size != this.image.Size)
			{
				this.Size = new Size(this.image.Width, this.image.Height);
			}
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			base.OnMouseWheel(e);

			if (ModifierKeys == Keys.Control && this.image != null)
			{
				if (e.Delta > 0)
				{
					this.ZoomIn();
				}
				else
				{
					this.ZoomOut();
				}
			}

		}

		private void InitializeComponent()
		{
			this.SuspendLayout();
			//
			// Canvas
			//
			this.Name = "Canvas";
			this.ResumeLayout(false);

		}

	}
}
