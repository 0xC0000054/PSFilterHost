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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HostTest.Properties;
using HostTest.Tools;
using PSFilterHostDll;
using System.Threading;

namespace HostTest
{
	internal partial class Form1 : Form
	{
		private BitmapSource srcImage;
		private BitmapSource dstImage;
		private PseudoResourceCollection pseudoResources;
		private Dictionary<PluginData, ParameterData> paramDict;
		private HistoryStack historyStack;
		private bool setRepeatEffect;
		private bool setFilterApplyText;
		private string filterName;
		private string versionString;
		private string imageFileName;
		private string imageType;
		private Size panelClientSize;
		private AbortMessageFilter messageFilter;
		private string srcImageTempFileName;

		private static readonly string[] imageFileExtensions = WICHelpers.GetDecoderFileExtensions();

		public Form1()
		{
			InitializeComponent();
			this.srcImage = null;
			this.dstImage = null;
			this.pseudoResources = null;
			this.paramDict = new Dictionary<PluginData, ParameterData>();
			this.historyStack = new HistoryStack();            
			this.setRepeatEffect = false;          
			this.setFilterApplyText = false;
			this.filterName = string.Empty;
			this.imageFileName = string.Empty;
			this.imageType = string.Empty;
			this.panelClientSize = Size.Empty;
			this.srcImageTempFileName = string.Empty;

			if (IntPtr.Size == 8)
			{
				this.versionString = " x64";
				this.Text += versionString;
			}
			else
			{
				this.versionString = string.Empty;
			}
			this.messageFilter = new AbortMessageFilter();
			Application.AddMessageFilter(this.messageFilter);

		}

		private void ProcessCommandLine()
		{
			string[] args = Environment.GetCommandLineArgs();
			if (args.Length == 2)
			{			
				FileInfo info = new FileInfo(args[1]);

				if (info.Exists && imageFileExtensions.Contains(info.Extension, StringComparer.OrdinalIgnoreCase))
				{
					try
					{
						OpenFile(args[1]);
					}
					catch (FileFormatException)
					{
					}
					catch (NotSupportedException) // WINCODEC_ERR_COMPONENTNOTFOUND
					{
					}
				}
			}
		}

		private void ShowErrorMessage(string message)
		{
			if (base.InvokeRequired)
			{
				base.Invoke(new Action<string>(delegate(string error)
					{
						MessageBox.Show(this, error, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
					}), new object[] { message });
			}
			else
			{
				MessageBox.Show(this, message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
				if (filtersToolStripMenuItem.HasDropDownItems)
				{
					this.filtersToolStripMenuItem.DropDownItems.Clear(); 
				}

				if (aboutPluginsMenuItem.HasDropDownItems)
				{
					this.aboutPluginsMenuItem.DropDownItems.Clear();
				}

				FilterCollection list = PSFilterHost.QueryDirectory(path, true);

				if (list.Count > 0)
				{
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

					this.filtersToolStripMenuItem.DropDownItems.AddRange(filters);

					this.aboutPluginsMenuItem.DropDownItems.AddRange(filterAbout.ToArray());
					if (!aboutPluginsMenuItem.Available)
					{
						this.aboutMenuToolStripSeparator.Available = true;
						this.aboutPluginsMenuItem.Available = true;
					}

					EnableFiltersForImageFormat();

					this.toolStripStatusLabel1.Text = string.Empty; 
				}
				else
				{
					this.toolStripStatusLabel1.Text = Resources.NoFiltersStatusText;

					if (aboutPluginsMenuItem.Available)
					{
						this.aboutMenuToolStripSeparator.Available = false;
						this.aboutPluginsMenuItem.Available = false;
					}
				}
			}
			catch (DirectoryNotFoundException ex)
			{
				ShowErrorMessage(ex.Message);
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

		private void RunPhotoshopFilter_Click(object sender, EventArgs e)
		{
			ToolStripItem item = (ToolStripItem)sender;
			PluginData pluginData = (PluginData)item.Tag;
			this.setRepeatEffect = false;
			RunPhotoshopFilterThread(pluginData, false);

			if (setRepeatEffect)
			{
				SetRepeatEffectMenuItem(item);
				this.setRepeatEffect = false;
			}
		}

		private void ShowFilterAboutDialog(object sender, EventArgs e)
		{
			ToolStripItem item = (ToolStripItem)sender;
			PluginData pluginData = (PluginData)item.Tag;

			try
			{
				PSFilterHost.ShowAboutDialog(pluginData, this.Handle);
			}
			catch (FileNotFoundException ex)
			{
				ShowErrorMessage(ex.Message);
			}
			catch (FilterRunException ex)
			{
				ShowErrorMessage(ex.Message);
			}
		}

		private Thread filterThread;
		private void RunPhotoshopFilterThread(PluginData pluginData, bool repeatEffect)
		{
			if (filterThread == null)
			{
				filterThread = new Thread(() => RunPhotoshopFilterImpl(pluginData, repeatEffect)) { IsBackground = true, Priority = ThreadPriority.AboveNormal };
				filterThread.Start();

				while (filterThread.IsAlive)
				{
					Application.DoEvents();
				}

				filterThread.Join();
				filterThread = null;

				this.toolStripProgressBar1.Value = 0;
				this.toolStripProgressBar1.Visible = false;
				this.toolStripStatusLabel1.Text = string.Empty;
			}
		}

		private void SaveImageOnUIThread()
		{
			if (string.IsNullOrEmpty(srcImageTempFileName))
			{
				this.srcImageTempFileName = Path.GetTempFileName();

				using (FileStream stream = new FileStream(srcImageTempFileName, FileMode.Create, FileAccess.Write))
				{
					TiffBitmapEncoder enc = new TiffBitmapEncoder();
					enc.Frames.Add(BitmapFrame.Create(this.srcImage));
					enc.Save(stream);
				}  
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
			if (historyStack.Count == 0)
			{
				// add the original image to the history stack.
				base.BeginInvoke(new Action(delegate()
				{
					this.historyStack.AddHistoryItem(this.canvas.ToCanvasHistoryState(), this.srcImage);
				}));
			}

			BitmapSource srcTemp = null;

			if (dstImage == null)
			{
				// save the srcImage to a temporary file on the UI thread and load it on this thread to fix the cross threading issues.
				if (string.IsNullOrEmpty(srcImageTempFileName))
				{
					base.Invoke(new Action(SaveImageOnUIThread)); 
				}

				using (FileStream stream = new FileStream(srcImageTempFileName, FileMode.Open, FileAccess.Read))
				{
					srcTemp = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
				}
			}

			IntPtr owner = (IntPtr)base.Invoke(new Func<IntPtr>(delegate() { return this.Handle; }));

			try
			{
				System.Windows.Media.Color primary = GDIPlusToWPFColor(this.primaryColorBtn.RectangleColor);
				System.Windows.Media.Color secondary = GDIPlusToWPFColor(this.secondaryColorBtn.RectangleColor);

				using (PSFilterHost host = new PSFilterHost(dstImage ?? srcTemp, primary, secondary, selection, owner))
				{
					host.SetAbortCallback(new AbortFunc(messageFilter.AbortFilter));
					host.UpdateProgress += new EventHandler<FilterProgressEventArgs>(UpdateFilterProgress);
					if (repeatEffect && paramDict.ContainsKey(pluginData))
					{
						host.FilterParameters = paramDict[pluginData];
					}

					if ((pseudoResources != null) && pseudoResources.Count > 0)
					{
						host.PseudoResources = pseudoResources;
					}

					this.filterName = pluginData.Title.TrimEnd('.');
					this.setFilterApplyText = false;
					this.messageFilter.Reset();

					if (host.RunFilter(pluginData))
					{
						this.dstImage = host.Dest;

						FormatConvertedBitmap convertedImage = null;

						if (dstImage.Format == PixelFormats.Rgba64)
						{
							convertedImage = new FormatConvertedBitmap(this.dstImage, PixelFormats.Bgra32, null, 0.0);
						}

						using (MemoryStream stream = new MemoryStream())
						{
							PngBitmapEncoder enc = new PngBitmapEncoder();
							enc.Frames.Add(BitmapFrame.Create(convertedImage ?? this.dstImage));
							enc.Save(stream);

							base.Invoke(new Action<MemoryStream>(delegate(MemoryStream ms)
							{
								this.canvas.Surface = new Bitmap(ms, true);
							}), new object[] { stream });
						}

						this.historyStack.AddHistoryItem(this.canvas.ToCanvasHistoryState(), this.dstImage);

						if (!repeatEffect)
						{
							if (paramDict.ContainsKey(pluginData))
							{
								paramDict[pluginData] = host.FilterParameters;
							}
							else
							{
								paramDict.Add(pluginData, host.FilterParameters);
							}

							this.pseudoResources = host.PseudoResources;
							this.setRepeatEffect = true;
						}

					}

				}
			}
			catch (FileNotFoundException ex)
			{
				ShowErrorMessage(ex.Message);
			}
			catch (FilterRunException ex)
			{
				string message = ex.Message;

				if (ex.InnerException != null)
				{
					message = ex.InnerException.ToString();
				}

				ShowErrorMessage(message);
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
				base.Invoke(new Action<int>(delegate(int value)
				{
					this.toolStripProgressBar1.Value = value;
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
					ShowErrorMessage(ex.Message);
				}
				catch (FileFormatException ex)
				{
					ShowErrorMessage(ex.Message);
				}
				catch (UnauthorizedAccessException ex)
				{
					ShowErrorMessage(ex.Message);
				}
			}
		}

		private void OpenFile(string path)
		{
			BitmapFrame frame = BitmapFrame.Create(new Uri(path), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);

			srcImage = frame.Clone();
			srcImage.Freeze();
		  
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

			this.panel1.SuspendLayout();

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
					enc.Frames.Add(BitmapFrame.Create(srcImage));
					enc.Save(ms);

					this.canvas.Surface = new Bitmap(ms, true);
				}

				this.imageType += "8";
			}

			this.panelClientSize = this.panel1.ClientSize;

			if (canvas.Size.Width > panel1.ClientSize.Width ||
				canvas.Size.Height > panel1.ClientSize.Height)
			{
				// calculate the new client size with the scrollbars manually so we can resize before they appear.
				Size clientSize = panel1.ClientSize;
				
				if (canvas.Size.Width > clientSize.Width) 
				{
					clientSize.Width -= SystemInformation.VerticalScrollBarWidth;
				}

				if (canvas.Size.Height > clientSize.Height)
				{
					clientSize.Height -= SystemInformation.HorizontalScrollBarHeight;
				}

				this.panelClientSize = clientSize;
				this.canvas.ZoomToWindow(clientSize);
			}
			else
			{
				this.Text = string.Format(Resources.TitleStringFormat, new object[] { this.versionString, this.imageFileName, 100, this.imageType });
			} 

			this.panel1.ResumeLayout(true);

			EnableFiltersForImageFormat();

			this.pointerSelectBtn.Enabled = true;
			this.rectangleSelectBtn.Enabled = true;
			this.elipseSelectBtn.Enabled = true;
			this.zoomInBtn.Enabled = this.canvas.CanZoomIn();
			this.zoomOutBtn.Enabled = this.canvas.CanZoomOut();

			this.toolStrip1.Refresh();
			historyStack.Clear();
			
			this.canvas.IsDirty = false;
			this.dstImage = null;

			if (!string.IsNullOrEmpty(srcImageTempFileName))
			{
				File.Delete(srcImageTempFileName);
				this.srcImageTempFileName = string.Empty;
			}
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (dstImage != null)
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

					if (ext == ".bmp" || ext == ".dib" || ext == ".rle")
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

					using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
					{
						enc.Save(fs);
					}

					this.canvas.IsDirty = false;
				} 
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

			string pluginDir = Path.Combine(Application.StartupPath, "Plugins");
			if (Directory.Exists(pluginDir))
			{
				QueryDirectory(pluginDir);
			}
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
		
		private void zoomToWindowBtn_Click(object sender, EventArgs e)
		{
			if (!panelClientSize.IsEmpty)
			{
				this.canvas.ZoomToWindow(panelClientSize);
			}
		}

		private void zoomToActualSizeBtn_Click(object sender, EventArgs e)
		{
			this.canvas.ZoomToActualSize();
		}

		private void canvas_ZoomChanged(object sender, CanvasZoomChangedEventArgs e)
		{
			this.zoomOutBtn.Enabled = this.canvas.CanZoomOut();
			this.zoomInBtn.Enabled = this.canvas.CanZoomIn();

			this.zoomToActualSizeBtn.Enabled = this.canvas.CanZoomIn();
			this.zoomToWindowBtn.Enabled = this.canvas.CanZoomToWindow(panelClientSize);

			int percent = 0;
			if (e.NewZoom < 0.10f)
			{
				percent = (int)Math.Round(e.NewZoom * 1000f);
			}
			else
			{
				percent = (int)Math.Round(e.NewZoom * 100f);
			}

			this.Text = string.Format(Resources.TitleStringFormat, new object[] { this.versionString, this.imageFileName, percent, this.imageType });
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
			if (filterThread != null)
			{
				e.Cancel = true;
				return;
			}

			if (canvas.IsDirty)
			{
				TaskButton save = new TaskButton(Resources.saveHS, Resources.SaveChangesText, Resources.SaveChangesDescription);
				TaskButton discard = new TaskButton(Resources.FileClose, Resources.DontSaveChangesText, Resources.DontSaveChangesDescription);
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

		private void Form1_Resize(object sender, EventArgs e)
		{
			if (!panel1.ClientSize.IsEmpty && panelClientSize != panel1.ClientSize && canvas.Surface != null)
			{
				this.canvas.ResetSize();
				panelClientSize = panel1.ClientSize;

				zoomToWindowBtn_Click(this, EventArgs.Empty);                
			}
		}

		private void canvas_DirtyChanged(object sender, CanvasDirtyChangedEventArgs e)
		{
			this.saveToolStripMenuItem.Enabled = e.Dirty; 
		}

		private string dropImageFileName;
		private void Form1_DragEnter(object sender, DragEventArgs e)
		{
			this.dropImageFileName = string.Empty;
			if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
			{
				string[] files = e.Data.GetData(DataFormats.FileDrop, false) as string[];

				if ((files.Length == 1) && imageFileExtensions.Contains(Path.GetExtension(files[0]), StringComparer.OrdinalIgnoreCase))
				{
					e.Effect = DragDropEffects.Copy;
					this.dropImageFileName = files[0];
				}
			}
		}

		private void Form1_DragDrop(object sender, DragEventArgs e)
		{
			if (!string.IsNullOrEmpty(dropImageFileName))
			{
				try
				{
					this.OpenFile(dropImageFileName);
				}
				catch (FileNotFoundException ex)
				{
					ShowErrorMessage(ex.Message);
				}
				catch (FileFormatException ex)
				{
					ShowErrorMessage(ex.Message);
				}
				catch (UnauthorizedAccessException ex)
				{
					ShowErrorMessage(ex.Message);
				}
			}
		}

		private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (AboutBox box = new AboutBox())
			{
				box.ShowDialog(this);
			}
		}

	}
}
