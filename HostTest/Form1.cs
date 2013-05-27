/////////////////////////////////////////////////////////////////////////////////
//
// Adobe(R) Photoshop(R) filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2013 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HostTest.Properties;
using PSFilterHostDll;
using HostTest.Tools;

namespace HostTest
{
	internal partial class Form1 : Form
	{
		private BitmapSource srcImage;
		private BitmapSource dstImage;
		private PseudoResourceCollection pseudoResources;
		private Dictionary<string, ParameterData> parmDict;
		private HistoryStack historyStack;

		public Form1()
		{
			InitializeComponent();
			srcImage = null;
			dstImage = null;
			pseudoResources = null;
			parmDict = new Dictionary<string, ParameterData>();
			historyStack = new HistoryStack();
		}

		private void ProcessCommandLine()
		{
			string[] args = Environment.GetCommandLineArgs();
			if (args.Length > 1)
			{
				bool foundDir = false;
				bool foundImage = false;
				for (int i = 1; i < args.Length; i++)
				{
					FileInfo info = new FileInfo(args[i]);

					if (info.Exists)
					{
						if ((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
						{
							if (!foundDir)
							{
								QueryDirectory(args[i]);
								foundDir = true;
							}
						}
						else
						{
							if (!foundImage)
							{
								try
								{
									OpenFile(args[i]);
									foundImage = true;
								}
								catch (NotSupportedException) // WINCODEC_ERR_COMPONENTNOTFOUND
								{
								}
							}
						} 
					}

				}
			}
		}

		private void loadFiltersMenuItem_Click(object sender, EventArgs e)
		{
			folderBrowserDialog1.SelectedPath = string.Empty;

			if (folderBrowserDialog1.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
			{
				QueryDirectory(folderBrowserDialog1.SelectedPath);
			}
		}


		private void QueryDirectory(string path)
		{
			this.Cursor = Cursors.WaitCursor;
			try
			{
				FilterCollection list = PSFilterHost.QueryDirectory(path, true);

				Dictionary<string, ToolStripMenuItem> filterList = new Dictionary<string, ToolStripMenuItem>(list.Count);
				List<ToolStripItem> filterAbout = new List<ToolStripItem>(list.Count);

				foreach (var plug in list)
				{

					ToolStripMenuItem child = new ToolStripMenuItem(plug.Title, null, new EventHandler(RunPhotoshopFilter_Click)) { Name = plug.Title, Tag = plug };
					ToolStripMenuItem aboutItem = new ToolStripMenuItem(plug.Title, null, new EventHandler(ShowFilterAboutDialog)) { Tag = plug };

					if (filterList.ContainsKey(plug.Category))
					{
						ToolStripMenuItem parent = filterList[plug.Category];

						if (!parent.DropDownItems.ContainsKey(plug.Title))
						{
							parent.DropDownItems.Add(child);
							filterAbout.Add(aboutItem);
						}

					}
					else
					{
						ToolStripMenuItem parent = new ToolStripMenuItem(plug.Category, null, new ToolStripItem[] { child });
						filterList.Add(plug.Category, parent);
						filterAbout.Add(aboutItem);
					}


				}

				filtersToolStripMenuItem.DropDownItems.Clear();
				helpToolStripMenuItem.DropDownItems.Clear();

				ToolStripMenuItem[] filters = new ToolStripMenuItem[filterList.Values.Count];
				filterList.Values.CopyTo(filters, 0);

				ToolStripItemComparer comparer = new ToolStripItemComparer();

				Array.Sort<ToolStripItem>(filters, comparer);

				// sort the items in the sub menus.
				int length = filters.Length;
				for (int i = 0; i < length; i++)
				{
					ToolStripMenuItem menu = filters[i];

					ToolStripMenuItem[] items = new ToolStripMenuItem[menu.DropDownItems.Count];
					menu.DropDownItems.CopyTo(items, 0);

					Array.Sort<ToolStripItem>(items, comparer);

					menu.DropDownItems.Clear();
					menu.DropDownItems.AddRange(items);
				}

				filterAbout.Sort(comparer);

				filtersToolStripMenuItem.DropDownItems.AddRange(filters);

				ToolStripMenuItem helpMenuItem = new ToolStripMenuItem(Resources.AboutPluginsText, null, filterAbout.ToArray());
				helpToolStripMenuItem.DropDownItems.Add(helpMenuItem);


				EnableFiltersForImageFormat();

				this.toolStripStatusLabel1.Text = string.Empty;
			}
			catch (DirectoryNotFoundException ex)
			{
				MessageBox.Show(ex.Message, this.Text);
			}
			finally
			{
				this.Cursor = Cursors.Default;
			}
		}

		private void EnableFiltersForImageFormat()
		{
			if (srcImage != null)
			{
				PixelFormat format = srcImage.Format;

				ToolStripItemCollection items = filtersToolStripMenuItem.DropDownItems;
				int length = items.Count;

				for (int i = 0; i < length; i++)
				{
					ToolStripMenuItem menu = (ToolStripMenuItem)items[i];

					if (menu.HasDropDownItems)
					{
						ToolStripItemCollection nodes = menu.DropDownItems;
						int nCount = nodes.Count;
						List<bool> catEnabled = new List<bool>(nCount);

						for (int j = 0; j < nCount; j++)
						{
							PluginData data = (PluginData)nodes[j].Tag;

							bool enabled = data.SupportsImageMode(format);
							catEnabled.Add(enabled);
							nodes[j].Enabled = enabled;
						}

						menu.Enabled = catEnabled.Contains(true);
					}
					else
					{
						PluginData data = (PluginData)menu.Tag;

						menu.Enabled = data.SupportsImageMode(format);
					}
				}
				
			}
			else
			{
				ToolStripItemCollection items = filtersToolStripMenuItem.DropDownItems;
				int length = items.Count;

				for (int i = 0; i < length; i++)
				{
					items[i].Enabled = false;
				}
			}
			
		}

		private bool setRepeatEffect;
		private void RunPhotoshopFilter_Click(object sender, EventArgs e)
		{
			ToolStripItem item = (ToolStripItem)sender;
			PluginData pluginData = (PluginData)item.Tag;
			this.setRepeatEffect = false;
			RunPhotoshopFilterImpl(pluginData, false);

			if (setRepeatEffect)
			{
				SetRepeatEffectMenuItem(item);
				this.setRepeatEffect = false;
			}
		}

		private string filterName;
		private void ShowFilterAboutDialog(object sender, EventArgs e)
		{
			ToolStripItem item = (ToolStripItem)sender;
			PluginData pluginData = (PluginData)item.Tag;

			try
			{
				PSFilterHost.ShowAboutDialog(pluginData, this.Handle);
			}
			catch (FilterRunException ex)
			{
				MessageBox.Show(this, ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private static System.Windows.Media.Color GDIPlusToWPFColor(System.Drawing.Color color)
		{
			return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
		}

		private void RunPhotoshopFilterImpl(PluginData pluginData, bool repeatEffect)
		{

			
			Region selection = null;

			if (canvas.ClipPath != null)
			{
				selection = new Region(canvas.ClipPath);
			}

			try
			{
				System.Windows.Media.Color primary = GDIPlusToWPFColor(this.primaryColorBtn.RectangleColor);
				System.Windows.Media.Color secondary = GDIPlusToWPFColor(this.secondaryColorBtn.RectangleColor);

				using (PSFilterHost host = new PSFilterHost(dstImage ?? srcImage, primary, secondary, selection, this.Handle))
				{
					host.UpdateProgress += new EventHandler<FilterProgressEventArgs>(UpdateFilterProgress);
					if (repeatEffect && parmDict.ContainsKey(pluginData.FileName))
					{
						host.FilterParameters = parmDict[pluginData.FileName];
					}

					if ((pseudoResources != null) && pseudoResources.Count > 0)
					{
						host.PseudoResources = pseudoResources;
					}

					this.filterName = pluginData.Title.TrimEnd('.');
					this.setFilterApplyText = false;

					if (host.RunFilter(pluginData))
					{
						this.dstImage = host.Dest;
						if (dstImage.Format == PixelFormats.Rgba64)
						{
							FormatConvertedBitmap conv = new FormatConvertedBitmap(this.dstImage, PixelFormats.Bgra32, null, 0.0);
						
							using (MemoryStream ms = new MemoryStream())
							{
								TiffBitmapEncoder enc = new TiffBitmapEncoder();
								enc.Frames.Add(BitmapFrame.Create(conv));
								enc.Save(ms);

								this.canvas.Surface = new Bitmap(ms, true);
							}
						}
						else
						{
							using (MemoryStream ms = new MemoryStream())
							{
								TiffBitmapEncoder enc = new TiffBitmapEncoder();
								enc.Frames.Add(BitmapFrame.Create(dstImage));
								enc.Save(ms);

								this.canvas.Surface = new Bitmap(ms, true);
							}
						}

						this.historyStack.AddHistoryItem(this.canvas.ToCanvasHistoryState(), this.dstImage);

						if (!repeatEffect)
						{
							if (parmDict.ContainsKey(pluginData.FileName))
							{
								parmDict[pluginData.FileName] = host.FilterParameters;
							}
							else
							{
								parmDict.Add(pluginData.FileName, host.FilterParameters);
							}

							this.pseudoResources = host.PseudoResources;

							this.setRepeatEffect = true;
						}
						this.toolStripProgressBar1.Value = 0;
						this.toolStripProgressBar1.Visible = false;
						this.toolStripStatusLabel1.Text = string.Empty;
					}

				}
			}
			catch (FilterRunException ex)
			{
				string message = ex.Message;

				if (ex.InnerException != null)
				{
					message = ex.InnerException.ToString();
				}

				MessageBox.Show(this, message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			catch (FileNotFoundException ex)
			{
				MessageBox.Show(this, ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally
			{
				if (selection != null)
				{
					selection.Dispose();
					selection = null;
				}
			}
		}

		private void SetRepeatEffectMenuItem(ToolStripItem item)
		{
			if (!filtersToolStripMenuItem.DropDownItems.ContainsKey("repeatEffect"))
			{
				ToolStripMenuItem repeatItem = new ToolStripMenuItem(Resources.RepeatEffectMenuText + item.Text.TrimEnd('.'), null, new EventHandler(RepeatLastEffect))
				{
					Name = "repeatEffect",
					Tag = item.Tag,
					ShowShortcutKeys = true,
					ShortcutKeys = Keys.Control | Keys.F
				};

				filtersToolStripMenuItem.DropDownItems.Insert(0, repeatItem);
			}
			else
			{
				ToolStripMenuItem repeatItem = (ToolStripMenuItem)filtersToolStripMenuItem.DropDownItems[0];
				repeatItem.Text = Resources.RepeatEffectMenuText + item.Text.TrimEnd('.');
				repeatItem.Tag = item.Tag;
			}
		}

		private void RepeatLastEffect(object sender, EventArgs e)
		{
			ToolStripItem item = (ToolStripItem)sender;
			if (item.Tag != null)
			{
				RunPhotoshopFilterImpl((PluginData)item.Tag, true);
			}
		}

		private bool setFilterApplyText;
		private void SetApplyFilterText()
		{
			if (base.InvokeRequired)
			{
				base.Invoke(new Action<string>(delegate(string text)
				{
					this.toolStripStatusLabel1.Text = string.Format(Resources.ApplyFilterStatusFormat, this.filterName);
					this.toolStripProgressBar1.Visible = true;
				}), new object[] { filterName });
			}
			else
			{
				this.toolStripStatusLabel1.Text = string.Format(Resources.ApplyFilterStatusFormat, this.filterName);
				this.toolStripProgressBar1.Visible = true;
			}
			Application.DoEvents();
		}

		private void UpdateFilterProgress(object sender, FilterProgressEventArgs e)
		{
			if (!setFilterApplyText)
			{
				SetApplyFilterText();
				this.setFilterApplyText = true;
			}

			if (base.InvokeRequired)
			{
				base.Invoke(new Action<int>(delegate(int i)
				{
					this.toolStripProgressBar1.Value = e.Progress;
				}), new object[] { e.Progress });
			}
			else
			{
				this.toolStripProgressBar1.Value = e.Progress;
			}
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (openFileDialog1.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
			{
				try
				{
					this.OpenFile(openFileDialog1.FileName);
				}
				catch (FileNotFoundException ex)
				{
					MessageBox.Show(this, ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				catch (FileFormatException ex)
				{
					MessageBox.Show(this, ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				catch (UnauthorizedAccessException ex)
				{
					MessageBox.Show(this, ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private string imageFileName;
		private string imageType;

		private void OpenFile(string path)
		{
			using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
			{
				BitmapDecoder decoder = BitmapDecoder.Create(fs, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
				srcImage = decoder.Frames[0].Clone();

				PixelFormat format = srcImage.Format;
				int channelCount = format.Masks.Count;
				int bitsPerChannel = format.BitsPerPixel / channelCount;

				this.imageFileName = Path.GetFileName(path);

				if (format == PixelFormats.BlackWhite || format == PixelFormats.Gray2 || format == PixelFormats.Gray4 || format == PixelFormats.Gray8 ||
					format == PixelFormats.Gray16 || format == PixelFormats.Gray32Float)
				{
					this.imageType = "Gray/";
				}
				else
				{
					this.imageType = "RGB/";
				}

				if (bitsPerChannel >= 16)
				{
					FormatConvertedBitmap conv = new FormatConvertedBitmap();
					conv.BeginInit();
					conv.Source = srcImage;
					conv.DestinationFormat = channelCount == 4 ? PixelFormats.Bgra32 : PixelFormats.Bgr24;
					conv.EndInit();

					using (MemoryStream ms = new MemoryStream())
					{
						PngBitmapEncoder enc = new PngBitmapEncoder();
						enc.Frames.Add(BitmapFrame.Create(conv));
						enc.Save(ms);

						this.canvas.Surface = new Bitmap(ms, true);
					}

					this.imageType += "16";
				}
				else
				{
					using (MemoryStream ms = new MemoryStream())
					{
						PngBitmapEncoder enc = new PngBitmapEncoder();
						enc.Frames.Add(decoder.Frames[0]);
						enc.Save(ms);

						this.canvas.Surface = new Bitmap(ms, true);
					}

					this.imageType += "8";
				}

				this.Text = string.Format(Resources.TitleStringFormat, this.imageFileName, 100, this.imageType);
			}
			EnableFiltersForImageFormat();

			this.pointerSelectBtn.Enabled = true;
			this.rectangleSelectBtn.Enabled = true;
			this.elipseSelectBtn.Enabled = true;
			this.zoomInBtn.Enabled = this.canvas.CanZoomIn();
			this.zoomOutBtn.Enabled = this.canvas.CanZoomOut();

			if (historyStack.Count > 0)
			{
				historyStack.Clear();
			}
			this.historyStack.AddHistoryItem(this.canvas.ToCanvasHistoryState(), srcImage);
			this.canvas.IsDirty = false;
			this.dstImage = null;
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (dstImage.Format == PixelFormats.Bgra32 || dstImage.Format == PixelFormats.Bgr24)
			{
				saveFileDialog1.Filter = "BMP (*.BMP, *.DIB, *.RLE)|*.BMP;*.DIB;*.RLE|JPEG (*.JPG, *.JPEG, *.JPE, *.JFIF)|*.JPG;*.JPEG;*.JPE;*.JFIF|PNG (*.PNG)|*.PNG|TIFF (*.TIF, *.TIFF)|*.TIF;*.TIFF";
				saveFileDialog1.FilterIndex = 3;
			}
			else
			{
				saveFileDialog1.Filter = "PNG (*.PNG)|*.PNG|TIFF (*.TIF, *.TIFF)|*.TIF;*.TIFF";
				saveFileDialog1.FilterIndex = 1;
			}

			if (saveFileDialog1.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
			{
				string path = saveFileDialog1.FileName;
				string ext = Path.GetExtension(path).ToLowerInvariant();
				BitmapEncoder enc = null;

				if (ext == ".bmp" || ext == ".dib" || ext == ".rle" )
				{
					enc = new BmpBitmapEncoder();
				}
				else if (ext == ".jpg" || ext == ".jpeg" || ext == ".jpe" || ext == ".jiff")
				{
					enc = new JpegBitmapEncoder();
				}
				else if (ext == ".png")
				{
					enc = new PngBitmapEncoder();
				}
				else if (ext == ".tif" || ext == ".tiff")
				{
					enc = new TiffBitmapEncoder();
				}

				enc.Frames.Add(BitmapFrame.Create(dstImage));

				using(FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					enc.Save(fs);
				}

				this.canvas.IsDirty = false;
			}
		}

		static class NativeMethods
		{
			[DllImport("kernel32.dll", EntryPoint = "SetProcessDEPPolicy")]
			[return: MarshalAs(UnmanagedType.Bool)]
			internal static extern bool SetProcessDEPPolicy(uint dwFlags);
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			// Try to Opt-out of DEP on a 32-bit OS as many filters do not support it.
			if (IntPtr.Size == 4)
			{
				try
				{
					NativeMethods.SetProcessDEPPolicy(0U);
				}
				catch (EntryPointNotFoundException)
				{
					// This method is only present on Vista SP1 or XP SP3 and later. 
				}
			}
			ProcessCommandLine();
		}
		
		private void pointerSelectBtn_Click(object sender, EventArgs e)
		{
			if (!pointerSelectBtn.Checked)
			{
				rectangleSelectBtn.Checked = false;
				elipseSelectBtn.Checked = false;
				pointerSelectBtn.Checked = true;
			}

			if (canvas.SelectionType != null)
			{
				canvas.SelectionType = null;
			}
		}

		private void rectangleSelectBtn_Click(object sender, EventArgs e)
		{
			if (!rectangleSelectBtn.Checked)
			{
				pointerSelectBtn.Checked = false;
				elipseSelectBtn.Checked = false;
				rectangleSelectBtn.Checked = true;
				this.toolStripStatusLabel1.Text = Resources.RectangleSelectionToolStatusText;
			}

			if ((canvas.SelectionType == null) || canvas.SelectionType.GetType() != typeof(RectangleSelectTool))
			{
				this.canvas.SelectionType = new RectangleSelectTool();
			}
		}

		private void elipseSelectBtn_Click(object sender, EventArgs e)
		{
			if (!elipseSelectBtn.Checked)
			{
				pointerSelectBtn.Checked = false;
				rectangleSelectBtn.Checked = false;
				elipseSelectBtn.Checked = true;
				this.toolStripStatusLabel1.Text = Resources.EllipseSelectionToolStatusText;
			}

			if ((canvas.SelectionType == null) || canvas.SelectionType.GetType() != typeof(ElipseSelectTool))
			{
				this.canvas.SelectionType = new ElipseSelectTool();
			}
		}

		private void zoomInBtn_Click(object sender, EventArgs e)
		{
			this.canvas.ZoomIn();
		}

		private void zoomOutBtn_Click(object sender, EventArgs e)
		{
			this.canvas.ZoomOut();
		} 
		
		private void canvas_ZoomChanged(object sender, CanvasZoomChangingEventArgs e)
		{
			this.zoomOutBtn.Enabled = this.canvas.CanZoomOut();
			this.zoomInBtn.Enabled = this.canvas.CanZoomIn();

			int percent = 0;
			if (e.NewZoom < 0.10f)
			{
				percent = (int)Math.Round(e.NewZoom * 1000f);
			}
			else
			{
				percent = (int)Math.Round(e.NewZoom * 100f);
			}

			this.Text = string.Format(Resources.TitleStringFormat, this.imageFileName, percent, this.imageType);
		}

		private void undoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			historyStack.StepBackward(this.canvas, ref this.dstImage);

		}

		private void redoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			historyStack.StepForward(this.canvas, ref this.dstImage);
		}

		private void primaryColorBtn_Click(object sender, EventArgs e)
		{
			this.colorDialog1.Color = this.primaryColorBtn.RectangleColor;
			if (colorDialog1.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
			{
				this.primaryColorBtn.RectangleColor = colorDialog1.Color;
			}
		}

		private void secondaryColorBtn_Click(object sender, EventArgs e)
		{
			this.colorDialog1.Color = this.secondaryColorBtn.RectangleColor;
			if (colorDialog1.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
			{
				this.secondaryColorBtn.RectangleColor = colorDialog1.Color;
			}
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (canvas.IsDirty)
			{
				TaskButton save = new TaskButton(Resources.saveHS, Resources.SaveChangesText, Resources.SaveChangesDescription);
				TaskButton discard = new TaskButton(Resources.MenuFileCloseIcon, Resources.DontSaveChangesText, Resources.DontSaveChangesDescription);
				TaskButton cancel = new TaskButton(Resources.CancelIcon, Resources.CancelText, Resources.CancelDescription);
				string actionText = string.Format(Resources.UnsavedChangesText, this.imageFileName);

				int width96 = (TaskDialog.DefaultPixelWidth96Dpi * 4) / 3; // 33% larger

				using (Bitmap bmp = this.canvas.ResizeCopy(96, 96))
				{
					TaskButton result = TaskDialog.Show(this, Resources.eventlogWarn, Resources.SaveChangesCaption, bmp, true, actionText,
								new TaskButton[] { save, discard, cancel }, save, cancel, width96);

					if (result == save)
					{
						saveToolStripMenuItem_Click(this, EventArgs.Empty);
					}
					else if (result == cancel)
					{
						e.Cancel = true;
					} 
				}
			}
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Close();
		}
	}
}
