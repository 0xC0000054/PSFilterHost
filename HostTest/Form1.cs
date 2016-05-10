/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2016 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using HostTest.Properties;
using HostTest.Tools;
using PSFilterHostDll;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HostTest
{
	internal partial class Form1 : Form
	{
		private BitmapSource srcImage;
		private BitmapSource dstImage;
		private PseudoResourceCollection pseudoResources;
		private Dictionary<PluginData, ParameterData> filterParameters;
		private HistoryStack historyStack;   
		private Thread filterThread;
		private ToolStripItem currentFilterMenuItem;
		private bool setFilterApplyText;
		private string filterName;
		private readonly string applicationName;
		private string imageFileName;
		private string imageType;
		private string dropImageFileName;
		private Size panelClientSize;
		private AbortMessageFilter messageFilter;  
		private string srcImageTempFileName;
		private string currentPluginDirectory;
		private HostInformation hostInfo;
		private BitmapMetadata srcMetaData;
		private readonly bool highDPIMode;
		private ColorContext srcColorContext;
		private ColorContext monitorColorContext;
		private string monitorColorProfilePath;
		private HostColorManagement hostColorProfiles;

		private static readonly ReadOnlyCollection<string> ImageFileExtensions = WICHelpers.GetDecoderFileExtensions();
		private static readonly ColorContext SrgbColorContext = ColorProfileHelper.GetSrgbColorContext();
		
		public Form1()
		{
			InitializeComponent();
			this.srcImage = null;
			this.dstImage = null;
			this.pseudoResources = null;
			this.filterParameters = new Dictionary<PluginData, ParameterData>();
			this.historyStack = new HistoryStack();
			this.historyStack.HistoryChanged += new EventHandler(historyStack_HistoryChanged);
			this.currentFilterMenuItem = null;          
			this.setFilterApplyText = false;
			this.filterName = string.Empty;
			this.imageFileName = string.Empty;
			this.imageType = string.Empty;
			this.dropImageFileName = string.Empty;
			this.panelClientSize = Size.Empty;
			this.srcImageTempFileName = string.Empty;
			this.currentPluginDirectory = string.Empty;
			this.hostInfo = new HostInformation();
			this.srcMetaData = null;
			this.srcMetaData = null;
			this.srcColorContext = null;
			this.monitorColorContext = null;
			this.monitorColorProfilePath = null;
			this.hostColorProfiles = null;

			if (IntPtr.Size == 8)
			{
				this.Text += " x64";
			}

			this.applicationName = this.Text;
			
			this.messageFilter = new AbortMessageFilter();
			Application.AddMessageFilter(this.messageFilter);

			this.openFileDialog1.Filter = WICHelpers.GetOpenDialogFilterString();

			PaintDotNet.SystemLayer.UI.InitScaling(this);
			this.highDPIMode = PaintDotNet.SystemLayer.UI.GetXScaleFactor() > 1f;
			InitializeDPIScaling();
		}

		private void InitializeDPIScaling()
		{
			// DPI scaling for the ToolStrip and MenuStrip classes was added in .NET 4.5.2, when targeting .NET 3.5 and earlier we have to scale the image size manually.
			// The scroll buttons in the ToolStripDropDownMenu have not been updated with high DPI support, so we will scale them when the menu is first opened.
			if (Environment.Version.Major < 4)
			{
				ScaleToolStripImageSize(this.menuStrip1);
				ScaleToolStripImageSize(this.toolStrip1);
				ToolStripManager.Renderer = new DpiAwareToolStripRenderer();
			}
			if (primaryColorBtn.ImageSize != toolStrip1.ImageScalingSize)
			{
				primaryColorBtn.ImageSize = toolStrip1.ImageScalingSize;
			}
			if (secondaryColorBtn.ImageSize != toolStrip1.ImageScalingSize)
			{
				secondaryColorBtn.ImageSize = toolStrip1.ImageScalingSize;
			}
		}

		/// <summary>
		/// Scales the size of the tool strip images to match the system DPI setting.
		/// </summary>
		/// <param name="toolStrip">The tool strip.</param>
		private static void ScaleToolStripImageSize(ToolStrip toolStrip)
		{
			Size scaledImageSize = PaintDotNet.SystemLayer.UI.ScaleSize(toolStrip.ImageScalingSize);

			if (toolStrip.ImageScalingSize != scaledImageSize)
			{
				// Temporarily disable the AutoSize property so the new ImageScalingSize will be used during layout,
				// see http://msdn.microsoft.com/en-us/library/system.windows.forms.toolstrip.imagescalingsize.aspx.
				toolStrip.AutoSize = false;  
				toolStrip.ImageScalingSize = scaledImageSize;
				toolStrip.PerformLayout();
				toolStrip.AutoSize = true;
			}
		}

		private void ProcessCommandLine()
		{
			string[] args = Environment.GetCommandLineArgs();
			if (args.Length == 2)
			{			
				FileInfo info = new FileInfo(args[1]);

				if (info.Exists && ImageFileExtensions.Contains(info.Extension, StringComparer.OrdinalIgnoreCase))
				{
					try
					{
						OpenFile(args[1]);
					}
					catch (FileNotFoundException ex)
					{
						ShowErrorMessage(ex.Message);
					}
					catch (FileFormatException ex)
					{
						ShowErrorMessage(ex.Message);
					}
					catch (IOException ex)
					{
						ShowErrorMessage(ex.Message);
					}
					catch (NotSupportedException ex) // WINCODEC_ERR_COMPONENTNOTFOUND
					{
						ShowErrorMessage(ex.Message);
					}
					catch (UnauthorizedAccessException ex)
					{
						ShowErrorMessage(ex.Message);
					}
				}
			}
		}

		private void UpdateMonitorColorProfile()
		{
			string newMonitorProfile = ColorProfileHelper.GetMonitorColorProfilePath(this.Handle);
			if (string.IsNullOrEmpty(newMonitorProfile))
			{
				// If the current monitor does not have a color profile use the default sRGB profile. 
				this.monitorColorProfilePath = SrgbColorContext.ProfileUri.LocalPath;
				this.monitorColorContext = SrgbColorContext;
			}
			else if (!newMonitorProfile.Equals(monitorColorProfilePath, StringComparison.OrdinalIgnoreCase))
			{
				this.monitorColorProfilePath = newMonitorProfile;
				Uri uri = new Uri(newMonitorProfile, UriKind.Absolute);
				this.monitorColorContext = new ColorContext(uri);
			}
		}

		static class NativeMethods
		{
			[DllImport("kernel32.dll", EntryPoint = "SetProcessDEPPolicy")]
			[return: MarshalAs(UnmanagedType.Bool)]
			internal static extern bool SetProcessDEPPolicy(uint dwFlags);

			[DllImport("kernel32.dll", EntryPoint = "SetErrorMode")]
			internal static extern uint SetErrorMode(uint uMode);

			internal const uint SEM_FAILCRITICALERRORS = 1U;
			internal const uint SEM_NOOPENFILEERRORBOX = 0x8000U;
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			// Try to Opt-out of DEP when running as a 32-bit process as many filters do not support it.
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

			// Disable the error dialog that is shown when a filter cannot find a missing dependency.
			uint oldMode = NativeMethods.SetErrorMode(0U);
			NativeMethods.SetErrorMode(oldMode | NativeMethods.SEM_FAILCRITICALERRORS | NativeMethods.SEM_NOOPENFILEERRORBOX);
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);

			string pluginDir = Path.Combine(Application.StartupPath, "Plug-Ins");
			if (Directory.Exists(pluginDir))
			{
				QueryDirectory(pluginDir);
			}

			UpdateMonitorColorProfile();

			ProcessCommandLine();
		}

		protected override void OnLocationChanged(EventArgs e)
		{
			base.OnLocationChanged(e);

			UpdateMonitorColorProfile();
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);

			UpdateMonitorColorProfile();
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
			if (folderBrowserDialog1.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
			{
				QueryDirectory(this.folderBrowserDialog1.SelectedPath);
			}
		}

		private void QueryDirectory(string path)
		{
			if (!backgroundWorker1.IsBusy)
			{
				this.Cursor = Cursors.WaitCursor;

				if (filtersToolStripMenuItem.HasDropDownItems)
				{
					this.filtersToolStripMenuItem.DropDownItems.Clear();
				}

				if (aboutPluginsMenuItem.HasDropDownItems)
				{
					this.aboutPluginsMenuItem.DropDownItems.Clear();
				}

				WorkerArgs args = new WorkerArgs(path);

				this.backgroundWorker1.RunWorkerAsync(args); 
			}
		}

		private void EnableFiltersForImageFormat()
		{
			if (srcImage != null)
			{
				PixelFormat format = srcImage.Format;

				ToolStripItemCollection items = filtersToolStripMenuItem.DropDownItems;
				for (int i = 0; i < items.Count; i++)
				{
					ToolStripMenuItemEx menu = items[i] as ToolStripMenuItemEx;

					if (menu != null)
					{
						var nodes = menu.SubMenuItems;
						int count = nodes.Count;
						List<bool> catEnabled = new List<bool>(count);

						for (int j = 0; j < count; j++)
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
						ToolStripItem repeatMenuItem = items[i];

						if (repeatMenuItem is ToolStripMenuItem)
						{
							PluginData data = (PluginData)repeatMenuItem.Tag;

							repeatMenuItem.Enabled = data.SupportsImageMode(format); 
						}
					}
				}
					
			}
			else
			{
				ToolStripItemCollection items = filtersToolStripMenuItem.DropDownItems;
				for (int i = 0; i < items.Count; i++)
				{
					items[i].Enabled = false;
				}
			}
				
		}

		private void RunPhotoshopFilter_Click(object sender, EventArgs e)
		{
			ToolStripItem item = (ToolStripItem)sender;
			PluginData pluginData = (PluginData)item.Tag;
			this.currentFilterMenuItem = item;
			
			RunPhotoshopFilterThread(pluginData, true);
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

		private void RunPhotoshopFilterThread(PluginData pluginData, bool showUI)
		{
			if (filterThread == null)
			{
				this.Cursor = Cursors.WaitCursor;

				this.filterThread = new Thread(() => RunPhotoshopFilterImpl(pluginData, showUI));
				this.filterThread.IsBackground = true;
				this.filterThread.Priority = ThreadPriority.AboveNormal;
				this.filterThread.SetApartmentState(ApartmentState.STA); // Some filters may use OLE which requires Single Threaded Apartment mode.
				this.filterThread.Start();
			}
		}

		private void FilterCompleted(bool setRepeatFilter)
		{
			if (setRepeatFilter)
			{
				SetRepeatEffectMenuItem();
			}

			this.filterThread.Join();
			this.filterThread = null;

			this.currentFilterMenuItem = null;
			this.Cursor = Cursors.Default;
			this.toolStripProgressBar1.Value = 0;
			this.toolStripProgressBar1.Visible = false;
			this.toolStripStatusLabel1.Text = string.Empty;
		}

		private void SaveImageOnUIThread()
		{
			if (string.IsNullOrEmpty(srcImageTempFileName))
			{
				if (historyStack.Count == 0)
				{
					// Add the original image to the history stack.
					this.historyStack.AddHistoryItem(this.canvas.ToCanvasHistoryState(), this.srcImage);
				}

				this.srcImageTempFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".tif");

				BitmapMetadata metaData = null;

				try
				{
					metaData = this.srcImage.Metadata as BitmapMetadata;
				}
				catch (NotSupportedException)
				{
				}
					
				this.srcMetaData = null;
				if (metaData != null)
				{
					// As WIC does not automatically convert between meta-data formats we have to do it manually.
					BitmapMetadata convertedMetaData = MetaDataHelper.ConvertMetaDataToTIFF(metaData);
					if (convertedMetaData != null)
					{
						this.srcMetaData = convertedMetaData.Clone();
						this.srcMetaData.Freeze();
					}
					metaData = convertedMetaData;
				}

				this.hostColorProfiles = null;
				if (srcColorContext != null && srcColorContext != monitorColorContext)
				{
					byte[] documentColorProfile = null;
					using (Stream stream = srcColorContext.OpenProfileStream())
					{
						int length = (int)stream.Length;

						documentColorProfile = new byte[length];

						int numBytesToRead = length;
						int numBytesRead = 0;
						do
						{
							int n = stream.Read(documentColorProfile, numBytesRead, numBytesToRead);
							numBytesRead += n;
							numBytesToRead -= n;
						} while (numBytesToRead > 0);
					}

					this.hostColorProfiles = new HostColorManagement(documentColorProfile, this.monitorColorProfilePath);
				}

				using (FileStream stream = new FileStream(srcImageTempFileName, FileMode.Create, FileAccess.Write))
				{
					TiffBitmapEncoder encoder = new TiffBitmapEncoder();
					encoder.Frames.Add(BitmapFrame.Create(this.srcImage, null, metaData, null));
					encoder.Save(stream);
				}  
			}
		}

		private static System.Windows.Media.Color GDIPlusToWPFColor(System.Drawing.Color color)
		{
			return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
		}

		private void RunPhotoshopFilterImpl(PluginData pluginData, bool showUI)
		{
			Region selection = null;

			if (canvas.ClipPath != null)
			{
				selection = new Region(canvas.ClipPath);
			}
			
			BitmapSource image = null;

			if (dstImage == null)
			{
				// save the srcImage to a temporary file on the UI thread and load it on this thread to fix the cross threading issues.
				if (string.IsNullOrEmpty(srcImageTempFileName))
				{
					base.Invoke(new Action(SaveImageOnUIThread)); 
				}

				using (FileStream stream = new FileStream(srcImageTempFileName, FileMode.Open, FileAccess.Read))
				{
					BitmapCreateOptions createOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;
					image = BitmapFrame.Create(stream, createOptions, BitmapCacheOption.OnLoad);
				}

				hostInfo.Caption = MetaDataHelper.GetIPTCCaption(image);
			}
			else
			{
				image = BitmapFrame.Create(dstImage, null, srcMetaData, null); // Create a new BitmapFrame so the source image's meta-data is available to the filters.
			}

			IntPtr owner = (IntPtr)base.Invoke(new Func<IntPtr>(delegate() { return this.Handle; }));
			bool setRepeatFilter = false;

			try
			{
				System.Windows.Media.Color primary = GDIPlusToWPFColor(this.primaryColorBtn.Color);
				System.Windows.Media.Color secondary = GDIPlusToWPFColor(this.secondaryColorBtn.Color);

				using (PSFilterHost host = new PSFilterHost(image, primary, secondary, selection, owner))
				{
					host.SetAbortCallback(new AbortFunc(this.messageFilter.AbortFilterCallback));
					host.SetPickColorCallback(new PickColor(PickColorCallback));
					host.UpdateProgress += new EventHandler<FilterProgressEventArgs>(UpdateFilterProgress);
					if (filterParameters.ContainsKey(pluginData))
					{
						host.FilterParameters = filterParameters[pluginData];
					}

					if ((pseudoResources != null) && pseudoResources.Count > 0)
					{
						host.PseudoResources = pseudoResources;
					}

					host.HostInfo = this.hostInfo;
					if (hostColorProfiles != null)
					{
						host.SetColorProfiles(hostColorProfiles);
					}

					this.filterName = pluginData.Title.TrimEnd('.');
					this.setFilterApplyText = false;
					this.messageFilter.Reset();

					if (host.RunFilter(pluginData, showUI))
					{
						this.dstImage = host.Dest;

						FormatConvertedBitmap convertedImage = null;

						int channelCount = dstImage.Format.Masks.Count;
						int bitsPerChannel = dstImage.Format.BitsPerPixel / channelCount;

						if (bitsPerChannel >= 16)
						{
							convertedImage = new FormatConvertedBitmap(this.dstImage, channelCount == 4 ? PixelFormats.Bgra32 : PixelFormats.Bgr24, null, 0.0);
						}

						this.canvas.SuspendPaint();

						UpdateCanvasImage(convertedImage ?? this.dstImage);

						this.historyStack.AddHistoryItem(this.canvas.ToCanvasHistoryState(), this.dstImage);

						this.canvas.ResumePaint();

						if (showUI)
						{
							if (filterParameters.ContainsKey(pluginData))
							{
								filterParameters[pluginData] = host.FilterParameters;
							}
							else
							{
								filterParameters.Add(pluginData, host.FilterParameters);
							}

							this.pseudoResources = host.PseudoResources;
							this.hostInfo = host.HostInfo;
							setRepeatFilter = true;
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
			catch (ImageSizeTooLargeException ex)
			{
				ShowErrorMessage(ex.Message);
			}
			finally
			{
				if (selection != null)
				{
					selection.Dispose();
					selection = null;
				}
				base.BeginInvoke(new Action(() => FilterCompleted(setRepeatFilter)));
			}

		}

		private void SetRepeatEffectMenuItem()
		{
			if (this.currentFilterMenuItem != null)
			{
				if (!filtersToolStripMenuItem.DropDownItems.ContainsKey("repeatEffect"))
				{
					ToolStripMenuItem repeatItem = new ToolStripMenuItem(this.currentFilterMenuItem.Text.TrimEnd('.'), null, new EventHandler(RepeatLastEffect))
					{
						Name = "repeatEffect",
						Tag = this.currentFilterMenuItem.Tag,
						ShowShortcutKeys = true,
						ShortcutKeys = Keys.Control | Keys.F
					};
					ToolStripSeparator repeatSeparator = new ToolStripSeparator();

					ToolStripItem[] filterCategories = new ToolStripItem[filtersToolStripMenuItem.DropDownItems.Count];
					filtersToolStripMenuItem.DropDownItems.CopyTo(filterCategories, 0);

					filtersToolStripMenuItem.DropDownItems.Clear();
					filtersToolStripMenuItem.DropDownItems.Add(repeatItem);
					filtersToolStripMenuItem.DropDownItems.Add(repeatSeparator);
					filtersToolStripMenuItem.DropDownItems.AddRange(filterCategories);
				}
				else
				{
					ToolStripMenuItem repeatItem = (ToolStripMenuItem)filtersToolStripMenuItem.DropDownItems[0];
					repeatItem.Text = this.currentFilterMenuItem.Text.TrimEnd('.');
					repeatItem.Tag = this.currentFilterMenuItem.Tag;
				} 
			}
		}

		private void RepeatLastEffect(object sender, EventArgs e)
		{
			ToolStripItem item = (ToolStripItem)sender;
			if (item.Tag != null)
			{
				RunPhotoshopFilterThread((PluginData)item.Tag, false);
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
					OpenFile(openFileDialog1.FileName);
				}
				catch (FileNotFoundException ex)
				{
					ShowErrorMessage(ex.Message);
				}
				catch (FileFormatException ex)
				{
					ShowErrorMessage(ex.Message);
				}
				catch (IOException ex)
				{
					ShowErrorMessage(ex.Message);
				}
				catch (NotSupportedException ex) // WINCODEC_ERR_COMPONENTNOTFOUND
				{
					ShowErrorMessage(ex.Message);
				}
				catch (UnauthorizedAccessException ex)
				{
					ShowErrorMessage(ex.Message);
				}
			}
		}

		private void UpdateCanvasImage(BitmapSource image)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				BitmapSource colorCorrectedImage = null;
				if (srcColorContext != null && srcColorContext != monitorColorContext)
				{
					PixelFormat format = image.Format.BitsPerPixel <= 24 ? PixelFormats.Bgr24 : PixelFormats.Bgra32;
					try
					{
						colorCorrectedImage = new ColorConvertedBitmap(image, srcColorContext, monitorColorContext, format);
					}
					catch (FileFormatException)
					{
						// Ignore the image color context if it is not valid.
						this.srcColorContext = null;
					}
				}

				PngBitmapEncoder enc = new PngBitmapEncoder();
				enc.Frames.Add(BitmapFrame.Create(colorCorrectedImage ?? image, null, null, null));
				enc.Save(stream);

				if (base.InvokeRequired)
				{
					base.Invoke(new Action<MemoryStream>(delegate (MemoryStream ms)
					{
						this.canvas.Surface = new Bitmap(ms);
					}), new object[] { stream });
				}
				else
				{
					this.canvas.Surface = new Bitmap(stream); 
				}
			}
		}

		private void OpenFile(string path)
		{
			this.Cursor = Cursors.WaitCursor;
			try
			{
				BitmapCreateOptions createOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;
				BitmapFrame frame = BitmapFrame.Create(new Uri(path), createOptions, BitmapCacheOption.None);

				srcImage = frame.Clone();
				srcImage.Freeze();

				if (frame.ColorContexts != null)
				{
					this.srcColorContext = frame.ColorContexts[0];
				}
				else
				{
					// If the image does not have an embedded color profile assume it is sRGB.
					this.srcColorContext = SrgbColorContext;
				}

				PixelFormat format = srcImage.Format;
				int channelCount = format.Masks.Count;
				int bitsPerChannel = format.BitsPerPixel / channelCount;

				this.imageFileName = Path.GetFileName(path);

				if (format == PixelFormats.BlackWhite ||
					format == PixelFormats.Gray2 ||
					format == PixelFormats.Gray4 ||
					format == PixelFormats.Gray8 ||
					format == PixelFormats.Gray16 ||
					format == PixelFormats.Gray32Float)
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
					// Convert the image to an 8 bits-per-channel format for display.
					UpdateCanvasImage(new FormatConvertedBitmap(srcImage, channelCount == 4 ? PixelFormats.Bgra32 : PixelFormats.Bgr24, null, 0.0));

					this.imageType += "16";
				}
				else
				{
					UpdateCanvasImage(srcImage);

					this.imageType += "8";
				}

				this.panelClientSize = this.panel1.ClientSize;

				if (canvas.Size.Width > panel1.ClientSize.Width ||
					canvas.Size.Height > panel1.ClientSize.Height)
				{
					// Calculate the new client size with the scrollbars manually so we can resize before they appear.
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
					this.Text = string.Format(Resources.TitleStringFormat, new object[] { this.applicationName, this.imageFileName, 100, this.imageType });

					this.zoomInBtn.Enabled = this.canvas.CanZoomIn();
					this.zoomOutBtn.Enabled = this.canvas.CanZoomOut();
					this.zoomToWindowBtn.Enabled = this.canvas.CanZoomToWindow(this.panel1.ClientSize);
					this.zoomToActualSizeBtn.Enabled = this.canvas.CanZoomToActualSize();
				}

				this.panel1.ResumeLayout(true);

				EnableFiltersForImageFormat();

				this.pointerSelectBtn.Enabled = true;
				this.rectangleSelectBtn.Enabled = true;
				this.elipseSelectBtn.Enabled = true;

				this.historyStack.Clear();

				this.canvas.IsDirty = false;
				this.dstImage = null;

				this.hostInfo.Title = this.imageFileName;

				if (!string.IsNullOrEmpty(srcImageTempFileName))
				{
					File.Delete(srcImageTempFileName);
					this.srcImageTempFileName = string.Empty;
				}
			}
			catch (Exception)
			{
				throw;
			}
			finally
			{
				this.Cursor = Cursors.Default;
			}
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (dstImage != null)
			{
				int bitsPerChannel = dstImage.Format.BitsPerPixel / dstImage.Format.Masks.Count;

				if (bitsPerChannel >= 16)
				{
					this.saveFileDialog1.Filter = "PNG Image (*.png)|*.png|TIFF Image (*.tif, *.tiff)|*.tif;*.tiff|Windows Media Photo (*.wdp, *.jxr)|*.wdp;*.jxr";
					this.saveFileDialog1.FilterIndex = 1;
				}
				else
				{
					this.saveFileDialog1.Filter = "Bitmap Image (*.bmp)|*.bmp|GIF Image (*.gif)|*.gif|JPEG Image (*.jpg, *.jpeg, *.jpe)|*.jpg;*.jpeg;*.jpe|PNG Image (*.png)|*.png|TIFF Image (*.tif, *.tiff)|*.tif;*.tiff|Windows Media Photo (*.wdp, *.jxr)|*.wdp;*.jxr";
					this.saveFileDialog1.FilterIndex = 4;
				}

				if (saveFileDialog1.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
				{
					string path = this.saveFileDialog1.FileName;
					BitmapEncoder encoder = null;

					switch (Path.GetExtension(path).ToUpperInvariant())
					{
						case ".BMP":
							encoder = new BmpBitmapEncoder();
							break;
						case ".GIF":
							encoder = new GifBitmapEncoder();
							break;
						case ".JPG":
						case ".JPEG":
						case ".JPE":
							encoder = new JpegBitmapEncoder();
							break;
						case ".PNG":
							encoder = new PngBitmapEncoder();
							break;
						case ".TIF":
						case ".TIFF":
							encoder = new TiffBitmapEncoder();
							break;
						case ".WDP":
						case ".JXR":
							encoder = new WmpBitmapEncoder();
							break;
					}

					BitmapMetadata metaData = null;

					try
					{
						metaData = this.srcImage.Metadata as BitmapMetadata;
					}
					catch (NotSupportedException)
					{
					}

					if (metaData != null)
					{
						metaData = MetaDataHelper.ConvertSaveMetaDataFormat(metaData, encoder);
					}

					ReadOnlyCollection<ColorContext> colorContexts = null;
					if (srcColorContext != null)
					{
						colorContexts = Array.AsReadOnly(new ColorContext[] { this.srcColorContext });
					}

					encoder.Frames.Add(BitmapFrame.Create(dstImage, null, metaData, colorContexts));

					using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
					{
						encoder.Save(fs);
					}

					this.canvas.IsDirty = false;
				}
			}
		}
			
		private void pointerSelectBtn_Click(object sender, EventArgs e)
		{
			if (!pointerSelectBtn.Checked)
			{
				this.rectangleSelectBtn.Checked = false;
				this.elipseSelectBtn.Checked = false;
				this.pointerSelectBtn.Checked = true;
				this.toolStripStatusLabel1.Text = string.Empty;
			}

			if (canvas.SelectionType != null)
			{
				this.canvas.SelectionType = null;
			}
		}

		private void rectangleSelectBtn_Click(object sender, EventArgs e)
		{
			if (!rectangleSelectBtn.Checked)
			{
				this.pointerSelectBtn.Checked = false;
				this.elipseSelectBtn.Checked = false;
				this.rectangleSelectBtn.Checked = true;
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
				this.pointerSelectBtn.Checked = false;
				this.rectangleSelectBtn.Checked = false;
				this.elipseSelectBtn.Checked = true;
				this.toolStripStatusLabel1.Text = Resources.EllipseSelectionToolStatusText;
			}

			if ((canvas.SelectionType == null) || canvas.SelectionType.GetType() != typeof(EllipseSelectTool))
			{
				this.canvas.SelectionType = new EllipseSelectTool();
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

			this.zoomToActualSizeBtn.Enabled = this.canvas.CanZoomToActualSize();
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

			this.Text = string.Format(Resources.TitleStringFormat, new object[] { this.applicationName, this.imageFileName, percent, this.imageType });
		}


		private void undoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.historyStack.StepBackward(this.canvas, ref this.dstImage);
			EnableUndoButtons();
		}

		private void redoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.historyStack.StepForward(this.canvas, ref this.dstImage);
			EnableUndoButtons();
		}

		private void EnableUndoButtons()
		{
			this.undoToolStripMenuItem.Enabled = this.historyStack.CanUndo;
			this.redoToolStripMenuItem.Enabled = this.historyStack.CanRedo;
		}

		private void historyStack_HistoryChanged(object sender, EventArgs e)
		{
			if (base.InvokeRequired)
			{
				base.Invoke(new Action(delegate()
				{
					EnableUndoButtons();
				}));
			}
			else
			{
				EnableUndoButtons();
			}
		}

		private void primaryColorBtn_Click(object sender, EventArgs e)
		{
			using (ColorPickerForm dialog = new ColorPickerForm(Resources.ChoosePrimaryColor))
			{
				dialog.Color = this.primaryColorBtn.Color;
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					this.primaryColorBtn.Color = dialog.Color;
				}
			}
		}

		private void secondaryColorBtn_Click(object sender, EventArgs e)
		{
			using (ColorPickerForm dialog = new ColorPickerForm(Resources.ChooseSecondaryColor))
			{
				dialog.Color = this.secondaryColorBtn.Color;
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					this.secondaryColorBtn.Color = dialog.Color;
				}
			}
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			if (filterThread != null)
			{
				e.Cancel = true;
			}
			else if (canvas.IsDirty)
			{
				TaskButton save = new TaskButton(Resources.saveHS, Resources.SaveChangesText, Resources.SaveChangesDescription);
				TaskButton discard = new TaskButton(Resources.FileClose, Resources.DontSaveChangesText, Resources.DontSaveChangesDescription);
				TaskButton cancel = new TaskButton(Resources.CancelIcon, Resources.CancelText, Resources.CancelDescription);
				string actionText = string.Format(Resources.UnsavedChangesText, this.imageFileName);

				int width96 = (TaskDialog.DefaultPixelWidth96Dpi * 4) / 3; // 33% larger

				using (Bitmap bmp = this.canvas.ResizeCopy(96, 96))
				{
					TaskButton result = TaskDialog.Show(this, Resources.Warning, Resources.SaveChangesCaption, bmp, true, actionText,
								new TaskButton[] { save, discard, cancel }, save, cancel, width96);

					if (result == save)
					{
						this.saveToolStripMenuItem_Click(this, EventArgs.Empty);
					}
					else if (result == cancel)
					{
						e.Cancel = true;
					}
				}
			}

			base.OnFormClosing(e);
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			if (base.WindowState == FormWindowState.Maximized && this.canvas.IsActualSize)
			{
				// If the window is maximized with the canvas at 100% do not resize the canvas.
				return;
			}

			if (!panel1.ClientSize.IsEmpty && panelClientSize != panel1.ClientSize && canvas.Surface != null)
			{
				this.canvas.ResetSize();
				this.panelClientSize = panel1.ClientSize;

				this.zoomToWindowBtn_Click(this, EventArgs.Empty);
			}
		}

		private void canvas_DirtyChanged(object sender, CanvasDirtyChangedEventArgs e)
		{
			this.saveToolStripMenuItem.Enabled = e.Dirty; 
		}

		private static bool FileDropIsImage(string file)
		{
			bool result = false;

			try
			{
				if ((File.GetAttributes(file) & FileAttributes.Directory) == 0)
				{
					result = ImageFileExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase);
				}
			}
			catch (ArgumentException)
			{
			}
			catch (IOException)
			{
			}
			catch (NotSupportedException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}

			return result;
		}

		protected override void OnDragEnter(DragEventArgs drgevent)
		{
			this.dropImageFileName = string.Empty;
			drgevent.Effect = DragDropEffects.None;
			if (drgevent.Data.GetDataPresent(DataFormats.FileDrop, false))
			{
				string[] files = drgevent.Data.GetData(DataFormats.FileDrop, false) as string[];

				if (files != null && files.Length == 1 && FileDropIsImage(files[0]))
				{
					drgevent.Effect = DragDropEffects.Copy;
					this.dropImageFileName = files[0];
				}
			}
			
			base.OnDragEnter(drgevent);
		}

		protected override void OnDragDrop(DragEventArgs drgevent)
		{
			if (!string.IsNullOrEmpty(this.dropImageFileName))
			{
				try
				{
					OpenFile(this.dropImageFileName);
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
			
			base.OnDragDrop(drgevent);
		}

		private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (AboutBox box = new AboutBox())
			{
				box.ShowDialog(this);
			}
		}

		private void refreshFiltersToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!string.IsNullOrEmpty(currentPluginDirectory))
			{
				QueryDirectory(currentPluginDirectory);
			}
		}

		private sealed class WorkerArgs
		{
			internal string Path
			{
				get;
				private set;
			}
			internal ToolStripMenuItemEx[] Filters
			{
				get;
				set;
			}
			internal ToolStripItem[] AboutFilters
			{
				get;
				set;
			}

			internal WorkerArgs(string path)
			{
				this.Path = path;
				this.Filters = null;
				this.AboutFilters = null;
			}
		}

		private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
		{
			WorkerArgs args = (WorkerArgs)e.Argument;

			Dictionary<string, ToolStripMenuItemEx> filterList = new Dictionary<string, ToolStripMenuItemEx>(StringComparer.Ordinal);
			List<ToolStripItem> aboutList = new List<ToolStripItem>();

			foreach (var plug in PSFilterHost.EnumerateFilters(args.Path, SearchOption.AllDirectories))
			{
				// The **Hidden** category is used for filters that are not directly invoked by the user.
				if (!plug.Category.Equals("**Hidden**", StringComparison.Ordinal))
				{
					ToolStripMenuItem child = new ToolStripMenuItem(plug.Title, null, new EventHandler(RunPhotoshopFilter_Click)) { Name = plug.Title, Tag = plug };
					ToolStripMenuItem aboutItem = new ToolStripMenuItem(plug.Title, null, new EventHandler(ShowFilterAboutDialog)) { Tag = plug };

					if (filterList.ContainsKey(plug.Category))
					{
						ToolStripMenuItemEx parent = filterList[plug.Category];

						if (!parent.SubMenuItems.ContainsKey(plug.Title))
						{
							parent.SubMenuItems.Add(child);
							if (plug.HasAboutBox)
							{
								aboutList.Add(aboutItem);
							}
						}
					}
					else
					{
						ToolStripMenuItemEx parent = new ToolStripMenuItemEx(plug.Category, child);
						filterList.Add(plug.Category, parent);
						if (plug.HasAboutBox)
						{
							aboutList.Add(aboutItem);
						}
					} 
				}
			}

			if (filterList.Count > 0)
			{
				ToolStripMenuItemEx[] filters = new ToolStripMenuItemEx[filterList.Values.Count];
				filterList.Values.CopyTo(filters, 0);

				ToolStripItemComparer comparer = new ToolStripItemComparer();

				Array.Sort<ToolStripItem>(filters, comparer);

				// sort the items in the sub menus.
				for (int i = 0; i < filters.Length; i++)
				{
					filters[i].SubMenuItems.Sort(comparer);
				}

				aboutList.Sort(comparer);

				args.Filters = filters;
				args.AboutFilters = aboutList.ToArray();
			}
			
			e.Result = args;
		}

		private void backgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				ShowErrorMessage(e.Error.Message);
			}
			else
			{
				WorkerArgs args = (WorkerArgs)e.Result;
				if (args.Filters != null)
				{
					this.filtersToolStripMenuItem.DropDownItems.AddRange(args.Filters);

					if (args.AboutFilters.Length > 0)
					{
						this.aboutPluginsMenuItem.DropDownItems.AddRange(args.AboutFilters);
						if (!aboutPluginsMenuItem.Available)
						{
							this.aboutMenuToolStripSeparator.Available = true;
							this.aboutPluginsMenuItem.Available = true;
						} 
					}
					else
					{
						this.aboutMenuToolStripSeparator.Available = false;
						this.aboutPluginsMenuItem.Available = false;
					}

					EnableFiltersForImageFormat();

					this.toolStripStatusLabel1.Text = string.Empty;
					this.currentPluginDirectory = args.Path;
					this.refreshFiltersToolStripMenuItem.Enabled = true;
				}
				else
				{
					this.toolStripStatusLabel1.Text = Resources.NoFiltersStatusText;
					this.refreshFiltersToolStripMenuItem.Enabled = false;
					this.currentPluginDirectory = string.Empty;

					if (aboutPluginsMenuItem.Available)
					{
						this.aboutMenuToolStripSeparator.Available = false;
						this.aboutPluginsMenuItem.Available = false;
					}
				}
			}

			this.Cursor = Cursors.Default;
		}

		private ColorPickerResult PickColorCallback(string prompt, byte defaultRed, byte defaultGreen, byte defaultBlue)
		{
			ColorPickerResult color = null;

			using (ColorPickerForm dialog = new ColorPickerForm(prompt))
			{
				dialog.Color = System.Drawing.Color.FromArgb(defaultRed, defaultGreen, defaultBlue);

				if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					color = new ColorPickerResult(dialog.Color);
				}

			}

			return color;
		}

		private void filtersToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
		{
			if (this.highDPIMode)
			{
				ToolStripDropDownItem item = (ToolStripDropDownItem)sender;
				DpiAwareToolStripRenderer.ScaleScrollButtonArrows(item.DropDown as ToolStripDropDownMenu);
			}
		}

		private void aboutPluginsMenuItem_DropDownOpening(object sender, EventArgs e)
		{
			if (this.highDPIMode)
			{
				ToolStripDropDownItem item = (ToolStripDropDownItem)sender;
				DpiAwareToolStripRenderer.ScaleScrollButtonArrows(item.DropDown as ToolStripDropDownMenu);
			}
		}
	}
}
