/////////////////////////////////////////////////////////////////////////////////
//
// Adobe(R) Photoshop(R) filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft public License:
//   Copyright (C) 2012-2013 Nicholas Hayes
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
	internal sealed class Canvas : UserControl
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
		private float scale;
		private Bitmap scaledImage;
		private bool isDirty;

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
			base.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			base.SetStyle(ControlStyles.Selectable, true);

			this.imageBounds = Rectangle.Empty;
			this.selectionRegion = null;
			this.image = null;
			this.checkerBoardBitmap = null;

			this.path = null;
			this.selectionClass = null;
			this.scale = 1f;
			this.resetClip = false;
			this.scaledImage = null;
			this.isDirty = false;
		}

		public bool IsDirty
		{
			get
			{
				return isDirty;
			}
			set
			{
				isDirty = value;
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

				if (this.selectionClass != null)
				{
					this.MouseDown -= this.selectionClass.MouseDown;
					this.MouseMove -= this.selectionClass.MouseMove;
					this.MouseUp -= this.selectionClass.MouseUp;
					this.KeyDown -= this.selectionClass.KeyDown;
					this.selectionClass.OnCursorChanged -= this.OnCursorChanged;
					this.selectionClass.OnSelectedPathChanged -= this.OnSelectionPathChanged;

					this.selectionClass.Dispose();
					this.selectionClass = null;
				}
				if (selectionRegion != null)
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
					selectionClass.OnCursorChanged += new EventHandler<CursorChangedEventArgs>(this.OnCursorChanged);
					selectionClass.OnSelectedPathChanged += new EventHandler<SelectionPathChangedEventArgs>(this.OnSelectionPathChanged);
				}

			}
		}
	
		public event EventHandler<CanvasZoomChangingEventArgs> ZoomChanged;

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
					this.path.Dispose();
					this.path = null;
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

			}
			base.Dispose(disposing);
		}

		private void OnSelectionPathChanged(object sender, SelectionPathChangedEventArgs e)
		{
			if (e.SelectedPath != null)
			{
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
				selectionRegion = new Region(selectPath);
			}
			else if (deSelect)
			{
				if (path != null)
				{
					path.Dispose();
					path = null;
				}

				if (selectionRegion != null)
				{
					selectionRegion.Dispose();
					selectionRegion = null;
				}
			}
			this.Invalidate();
		}

		private void ResetGraphicsClip()
		{
			resetClip = true;
			this.Invalidate();
		}

		private bool resetClip;
		protected override void OnPaint(PaintEventArgs pe)
		{
		  
			if (resetClip)
			{
				pe.Graphics.ResetClip();
				pe.Graphics.SetClip(imageBounds);
				resetClip = false;
			}

			if (this.checkerBoardBitmap != null && this.image != null)
			{
				pe.Graphics.Clear(Color.Transparent);
				pe.Graphics.DrawImageUnscaledAndClipped(this.checkerBoardBitmap, this.imageBounds);

				if (scaledImage != null)
				{
					pe.Graphics.DrawImage(this.scaledImage, this.imageBounds);
				}
				else
				{
					pe.Graphics.DrawImage(this.image, this.imageBounds);
				}				

				if (selectionRegion != null)
				{
					// draw the selection outline.

					Graphics g = pe.Graphics;

					if (outlinePen1 == null)
					{
						outlinePen1 = new Pen(Color.FromArgb(160, Color.Black), 1.0f);
						outlinePen1.Alignment = PenAlignment.Outset;
						outlinePen1.LineJoin = LineJoin.Bevel;
						outlinePen1.Width = -1;
					}

					if (outlinePen2 == null)
					{
						outlinePen2 = new Pen(Color.White, 1.0f);
						outlinePen2.Alignment = PenAlignment.Outset;
						outlinePen2.LineJoin = LineJoin.Bevel;
						outlinePen2.MiterLimit = 2;
						outlinePen2.Width = -1;
						outlinePen2.DashStyle = DashStyle.Dash;
						outlinePen2.DashPattern = new float[] { 4, 4 };
						outlinePen2.Color = Color.White;
						outlinePen2.DashOffset = 4.0f;
					}

					PixelOffsetMode oldPOM = g.PixelOffsetMode;
					g.PixelOffsetMode = PixelOffsetMode.None;

					SmoothingMode oldSM = g.SmoothingMode;
					g.SmoothingMode = SmoothingMode.AntiAlias;

					// scale the selection region if necessary
					if (scaledImage != null)
					{
						using (GraphicsPath temp = (GraphicsPath)this.path.Clone())
						{
							using (Matrix matrix = new Matrix())
							{
								matrix.Scale(this.scale, this.scale);

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
			checkerBoardBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

			BitmapData bd = checkerBoardBitmap.LockBits(new Rectangle(0, 0, checkerBoardBitmap.Width, checkerBoardBitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			try
			{
				for (int y = 0; y < checkerBoardBitmap.Height; y++)
				{
					byte* p = (byte*)bd.Scan0.ToPointer() + (y * bd.Stride);
					for (int x = 0; x < checkerBoardBitmap.Width; x++)
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
					if ((image == null) || image.Width != value.Width || image.Height != value.Height)
					{

						if (path != null)
						{
							path.Dispose();
							path = null;
						}

						if (selectionRegion != null)
						{
							selectionRegion.Dispose();
							selectionRegion = null;
						}

						imageBounds = new Rectangle(0, 0, value.Width, value.Height);
						this.DrawCheckerBoardBitmap(value.Width, value.Height);

						if (image != null)
						{
							this.image.Dispose();
							this.image = null;
						}

						if (value.PixelFormat != PixelFormat.Format32bppArgb)
						{
							this.image = value.Clone(imageBounds, PixelFormat.Format32bppArgb);
						}
						else
						{
							this.image = value.Clone(imageBounds, PixelFormat.Format32bppArgb);
						}
						this.Size = new Size(value.Width, value.Height);

						this.ResetZoom();
						this.isDirty = false;
					}
					else
					{
						this.CopyFromBitmap(value, new Rectangle(0, 0, value.Width, value.Height));
						this.isDirty = true;
						this.ZoomCanvas();
					}
				}
			}
		}

		public GraphicsPath ClipPath
		{
			get 
			{
				return path;
			}
		}

		/// <summary>
		/// Copies from the source to the destination bitmap.
		/// </summary>
		/// <param name="source">The source bitmap.</param>
		/// <param name="bounds">The area of the bitmap to copy.</param>
		[SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		private void CopyFromBitmap(Bitmap source, Rectangle bounds)
		{
			using (Graphics gr = Graphics.FromImage(this.image))
			{
				gr.DrawImage(source, bounds, bounds, GraphicsUnit.Pixel);
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
			if (scale < maxZoom)
			{
				int index = -1;

				for (int i = 0; i < zoomFactors.Length; i++)
				{
					if (zoomFactors[i] > scale)
					{
						index = i;
						break;
					}
				}

				if (index == -1)
				{
					index = zoomFactors.Length - 1;
				}

				this.scale = zoomFactors[index];

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
			int index = -1;

			for (int i = zoomFactors.Length - 1; i >= 0; i--)
			{
				if (zoomFactors[i] < scale)
				{
					index = i;
					break;
				}
			}

			if (index == -1)
			{
				index = 0;
			}

			float factor = zoomFactors[index];

			return ((image.Width * factor) >= 1f && (image.Height * factor) >= 1f);
		}

		/// <summary>
		/// Determines whether the image can zoom in.
		/// </summary>
		/// <returns>
		///   <c>true</c> if the image can zoom in; otherwise, <c>false</c>.
		/// </returns>
		public bool CanZoomIn()
		{ 
			return (this.scale < maxZoom);
		}

		/// <summary>
		/// Zooms the image out.
		/// </summary>
		public void ZoomOut()
		{
			if (scale > minZoom)
			{
				int index = -1;

				for (int i = zoomFactors.Length - 1; i >= 0; i--)
				{
					if (zoomFactors[i] < scale)
					{
						index = i;
						break;
					}
				}

				if (index == -1)
				{
					index = 0;
				}

				this.scale = zoomFactors[index];

				this.ZoomCanvas();
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

				float ratio = ratioX < ratioY ? ratioX : ratioY;


				this.ZoomCanvas(ratio);
			}
		}

		private void ZoomCanvas()
		{
			this.ZoomCanvas(this.scale);
		}

		private void ZoomCanvas(float ratio)
		{
			if ((scaledImage == null) || scaledImage.Width != image.Width || scaledImage.Height != image.Height)
			{
				if (ZoomChanged != null)
				{
					ZoomChanged.Invoke(this, new CanvasZoomChangingEventArgs(ratio, minZoom, maxZoom));
				}

				if (scaledImage != null)
				{
					scaledImage.Dispose();
					scaledImage = null;
				}

				int imageWidth = this.image.Width;
				int imageHeight = this.image.Height;

				int scaledWidth = (int)((float)imageWidth * ratio);
				int scaledHeight = (int)((float)imageHeight * ratio);

				if (scaledWidth != imageWidth && scaledHeight != imageHeight)
				{
					this.scaledImage = new Bitmap(scaledWidth, scaledHeight, PixelFormat.Format32bppArgb);
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

		private void ResetZoom()
		{
			if (scaledImage != null)
			{
				scaledImage.Dispose();
				scaledImage = null;
			}

			this.scale = 1f;
			this.ResetGraphicsClip();

			if (ZoomChanged != null)
			{
				ZoomChanged.Invoke(this, new CanvasZoomChangingEventArgs(this.scale, minZoom, maxZoom));
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
			int imageWidth = image.Width;
			int imageHeight = image.Height;

			float ratioX = (float)maxWidth / (float)imageWidth;
			float ratioY = (float)maxHeight / (float)imageHeight;

			float ratio = ratioX < ratioY ? ratioX : ratioY;

			int newWidth = (int)((float)imageWidth * ratio);
			int newHeight = (int)((float)imageHeight * ratio);

			Bitmap scaled = null;
			using(Bitmap temp = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb))
			{
				using (Graphics gr = Graphics.FromImage(temp))
				{
					gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
					gr.SmoothingMode = SmoothingMode.HighQuality;
					gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
					gr.CompositingQuality = CompositingQuality.HighQuality;

					gr.DrawImage(this.image, new Rectangle(0, 0, newWidth, newHeight), new Rectangle(0, 0, imageWidth, imageHeight), GraphicsUnit.Pixel);     
				}

				scaled = (Bitmap)temp.Clone();
			}


			return scaled;
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
