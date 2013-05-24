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

namespace HostTest
{
	public partial class Form1 : Form
	{

		public Form1()
		{
			InitializeComponent();
			srcImage = null;
			dstImage = null;
			pseudoResources = null;
			parmDict = new Dictionary<string, ParameterData>();
		}
				
		private BitmapSource srcImage;
		private BitmapSource dstImage;
		private PseudoResourceCollection pseudoResources;
		private Dictionary<string, ParameterData> parmDict;

		private void browseFilterBtn_Click(object sender, EventArgs e)
		{
			folderBrowserDialog1.SelectedPath = string.Empty;
			
			if (folderBrowserDialog1.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
			{
				this.filterDirectoryTxt.Text = folderBrowserDialog1.SelectedPath;
				QueryDirectory(folderBrowserDialog1.SelectedPath);
			}
		}

		private void QueryDirectory(string path)
		{
			try
			{
				FilterCollection list = PSFilterHost.QueryDirectory(path, true);

				Dictionary<string, ToolStripMenuItem> filterList = new Dictionary<string, ToolStripMenuItem>(list.Count);
				List<ToolStripItem> filterAbout = new List<ToolStripItem>(list.Count);

				foreach (var plug in list)
				{
											
					ToolStripMenuItem child = new ToolStripMenuItem(plug.Title, null, new EventHandler(RunPhotoshopFilter_Click)) { Name = plug.Title, Tag = plug };
					ToolStripMenuItem aboutItem = new ToolStripMenuItem(plug.Title, null, new EventHandler(ShowFilterAboutDialog)) {  Tag = plug };

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
				filterAbout.Sort(comparer);

				filtersToolStripMenuItem.DropDownItems.AddRange(filters);

				ToolStripMenuItem helpMenuItem = new ToolStripMenuItem("About Plugins", null, filterAbout.ToArray());
				helpToolStripMenuItem.DropDownItems.Add(helpMenuItem);
				

				EnableFiltersForImageFormat();
			}
			catch (DirectoryNotFoundException ex)
			{
				MessageBox.Show(ex.Message, this.Text);
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

		private void RunPhotoshopFilterImpl(PluginData pluginData, bool repeatEffect)
		{

			GraphicsPath gp = null;
			Region rgn = null;

			try
			{
				using (PSFilterHost host = new PSFilterHost(srcImage, Colors.Black, Colors.White, rgn, this.Handle))
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

								this.pictureBox2.Image = Image.FromStream(ms, true);
							}
						}
						else
						{
							using (MemoryStream ms = new MemoryStream())
							{
								TiffBitmapEncoder enc = new TiffBitmapEncoder();
								enc.Frames.Add(BitmapFrame.Create(dstImage));
								enc.Save(ms);

								this.pictureBox2.Image = Image.FromStream(ms, true);
							}
						} 

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
						this.progressBar1.Value = 0;
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
				if (gp != null)
				{
					gp.Dispose();
					gp = null;
				}
				if (rgn != null)
				{
					rgn.Dispose();
					rgn = null;
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

		private void UpdateFilterProgress(object sender, FilterProgressEventArgs e)
		{
			if (base.InvokeRequired)
			{
				base.Invoke(new Action<int>(delegate(int i)
				{
					this.progressBar1.Value = e.Progress;
				}), new object[] { e.Progress });
			}
			else
			{
				this.progressBar1.Value = e.Progress;
			}
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (openFileDialog1.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
			{
				this.OpenFile(openFileDialog1.FileName);
			}
		}

		private void OpenFile(string path)
		{
		   
			try
			{
				using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
				{
					BitmapDecoder decoder = BitmapDecoder.Create(fs, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
					srcImage = decoder.Frames[0].Clone();

					PixelFormat format = srcImage.Format;
					int channelCount = format.Masks.Count;
					int bitsPerChannel = format.BitsPerPixel / channelCount;

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

							pictureBox1.Image = Image.FromStream(ms, true);
						}
					}
					else
					{
						using (MemoryStream ms = new MemoryStream())
						{
							PngBitmapEncoder enc = new PngBitmapEncoder();
							enc.Frames.Add(decoder.Frames[0]);
							enc.Save(ms);

							pictureBox1.Image = Image.FromStream(ms, true);
						}
					}
				}
				EnableFiltersForImageFormat();
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.ToString(), "Error loading image", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (dstImage.Format == PixelFormats.Bgra32 || dstImage.Format == PixelFormats.Bgr24)
			{
				saveFileDialog1.Filter = "BMP (*.bmp) | *.bmp | JPEG (*.jpg) | *.jpg | PNG (*.png) | *.png | TIFF (*.tif) | *.tif";
				saveFileDialog1.FilterIndex = 3;
			}
			else
			{
				saveFileDialog1.Filter = "PNG (*.png) | *.png | TIFF (*.tif) | *.tif";
				saveFileDialog1.FilterIndex = 1;
			}

			if (saveFileDialog1.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
			{
				string path = saveFileDialog1.FileName;
				string ext = Path.GetExtension(path).ToLowerInvariant();
				BitmapEncoder enc = null;

				if (ext == ".bmp")
				{
					enc = new BmpBitmapEncoder();
				}
				else if (ext == ".jpg")
				{
					enc = new JpegBitmapEncoder();
				}
				else if (ext == ".png")
				{
					enc = new PngBitmapEncoder();
				}
				else if (ext == ".tif")
				{
					enc = new TiffBitmapEncoder() { Compression = TiffCompressOption.Lzw };
				}

				enc.Frames.Add(BitmapFrame.Create(dstImage));

				using(FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					enc.Save(fs);
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
		}

	}
}
