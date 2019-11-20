/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2019 Nicholas Hayes
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
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
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
        private BitmapMetadata srcMetadata;
        private readonly bool highDPIMode;
        private ColorContext srcColorContext;
        private ColorContext monitorColorContext;
        private string monitorColorProfilePath;
        private HostColorManagement hostColorProfiles;
        private PluginSettingsRegistry sessionSettings;
        private bool disabledIncompatableFilters;

        private static readonly ReadOnlyCollection<string> ImageFileExtensions = WICHelpers.GetDecoderFileExtensions();
        private static readonly ColorContext SrgbColorContext = ColorProfileHelper.GetSrgbColorContext();

        public Form1()
        {
            InitializeComponent();
            srcImage = null;
            dstImage = null;
            pseudoResources = null;
            filterParameters = new Dictionary<PluginData, ParameterData>();
            historyStack = new HistoryStack();
            historyStack.HistoryChanged += new EventHandler(historyStack_HistoryChanged);
            currentFilterMenuItem = null;
            setFilterApplyText = false;
            filterName = string.Empty;
            imageFileName = string.Empty;
            imageType = string.Empty;
            dropImageFileName = string.Empty;
            panelClientSize = Size.Empty;
            srcImageTempFileName = string.Empty;
            currentPluginDirectory = string.Empty;
            hostInfo = new HostInformation();
            srcMetadata = null;
            srcColorContext = null;
            monitorColorContext = null;
            monitorColorProfilePath = null;
            hostColorProfiles = null;
            sessionSettings = null;

            if (IntPtr.Size == 8)
            {
                Text += " x64";
            }

            applicationName = Text;

            messageFilter = new AbortMessageFilter();
            Application.AddMessageFilter(messageFilter);

            openFileDialog1.Filter = WICHelpers.GetOpenDialogFilterString();

            PaintDotNet.SystemLayer.UI.InitScaling(this);
            highDPIMode = PaintDotNet.SystemLayer.UI.GetXScaleFactor() > 1f;
            InitializeDPIScaling();
        }

        private void InitializeDPIScaling()
        {
            // DPI scaling for the ToolStrip and MenuStrip classes was added in .NET 4.5.2, when targeting .NET 3.5 and earlier we have to scale the image size manually.
            // The scroll buttons in the ToolStripDropDownMenu have not been updated with high DPI support, so we will scale them when the menu is first opened.
            if (Environment.Version.Major < 4)
            {
                ScaleToolStripImageSize(menuStrip1);
                ScaleToolStripImageSize(toolStrip1);
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
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                if (args.Length == 2)
                {
                    string path = Path.GetFullPath(args[1]);

                    if (ImageFileExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                    {
                        OpenFile(path);
                    }
                }
            }
            catch (ArgumentException ex)
            {
                ShowErrorMessage(ex.Message);
            }
            catch (ExternalException ex)
            {
                ShowErrorMessage(ex.Message);
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
            catch (NotSupportedException ex)
            {
                ShowErrorMessage(ex.Message);
            }
            catch (SecurityException ex)
            {
                ShowErrorMessage(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                ShowErrorMessage(ex.Message);
            }
        }

        private void UpdateMonitorColorProfile()
        {
            string newMonitorProfile = ColorProfileHelper.GetMonitorColorProfilePath(Handle);
            if (string.IsNullOrEmpty(newMonitorProfile))
            {
                // If the current monitor does not have a color profile use the default sRGB profile.
                monitorColorProfilePath = SrgbColorContext.ProfileUri.LocalPath;
                monitorColorContext = SrgbColorContext;
            }
            else if (!newMonitorProfile.Equals(monitorColorProfilePath, StringComparison.OrdinalIgnoreCase))
            {
                monitorColorProfilePath = newMonitorProfile;
                Uri uri = new Uri(newMonitorProfile, UriKind.Absolute);
                monitorColorContext = new ColorContext(uri);
            }
        }

        private void LoadPluginSessionSettings()
        {
            try
            {
                string path = Path.Combine(Application.StartupPath, "PluginRegistry.dat");

                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    sessionSettings = (PluginSettingsRegistry)bf.Deserialize(fs);
                    sessionSettings.Dirty = false;
                }
            }
            catch (FileNotFoundException)
            {
                // This file will only exist if a plug-in has persisted settings from a previous session.
            }
            catch (IOException ex)
            {
                ShowErrorMessage(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                ShowErrorMessage(ex.Message);
            }
        }

        private void SavePluginSessionSettings()
        {
            if (sessionSettings != null)
            {
                if (sessionSettings.Dirty)
                {
                    try
                    {
                        string path = Path.Combine(Application.StartupPath, "PluginRegistry.dat");

                        using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            BinaryFormatter bf = new BinaryFormatter();
                            bf.Serialize(fs, sessionSettings);
                        }
                        sessionSettings.Dirty = false;
                    }
                    catch (IOException ex)
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

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Try to Opt-out of DEP when running as a 32-bit process as many filters do not support it.
            if (IntPtr.Size == 4)
            {
                try
                {
                    SafeNativeMethods.SetProcessDEPPolicy(0U);
                }
                catch (EntryPointNotFoundException)
                {
                    // This method is only present on Vista SP1 or XP SP3 and later.
                }
            }

            // Disable the error dialog that is shown when a filter cannot find a missing dependency.
            uint oldMode = SafeNativeMethods.SetErrorMode(0U);
            SafeNativeMethods.SetErrorMode(oldMode | NativeConstants.SEM_FAILCRITICALERRORS);
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
            LoadPluginSessionSettings();

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
            if (InvokeRequired)
            {
                Invoke(new Action<string>(delegate (string error)
                    {
                        MessageBox.Show(this, error, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }), new object[] { message });
            }
            else
            {
                MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void loadFiltersMenuItem_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog(this) == DialogResult.OK)
            {
                QueryDirectory(folderBrowserDialog1.SelectedPath);
            }
        }

        private void QueryDirectory(string path)
        {
            if (!backgroundWorker1.IsBusy)
            {
                Cursor = Cursors.WaitCursor;

                if (filtersToolStripMenuItem.HasDropDownItems)
                {
                    filtersToolStripMenuItem.DropDownItems.Clear();
                }

                if (aboutPluginsMenuItem.HasDropDownItems)
                {
                    aboutPluginsMenuItem.DropDownItems.Clear();
                }

                WorkerArgs args = new WorkerArgs(path);

                backgroundWorker1.RunWorkerAsync(args);
            }
        }

        private void EnableFiltersForHostState()
        {
            if (srcImage != null)
            {
                HostState hostState = new HostState
                {
                    HasMultipleLayers = false,
                    HasSelection = canvas.ClipPath != null
                };

                ToolStripItemCollection items = filtersToolStripMenuItem.DropDownItems;
                for (int i = 0; i < items.Count; i++)
                {
                    ToolStripMenuItemEx menu = items[i] as ToolStripMenuItemEx;

                    if (menu != null)
                    {
                        var nodes = menu.DropDownItems;
                        int count = nodes.Count;
                        bool catEnabled = false;

                        for (int j = 0; j < count; j++)
                        {
                            PluginData data = (PluginData)nodes[j].Tag;

                            bool enabled = data.SupportsHostState(srcImage, hostState);
                            catEnabled |= enabled;
                            nodes[j].Enabled = enabled;
                        }

                        menu.Enabled = catEnabled;
                    }
                    else
                    {
                        ToolStripItem repeatMenuItem = items[i];

                        if (repeatMenuItem is ToolStripMenuItem)
                        {
                            PluginData data = (PluginData)repeatMenuItem.Tag;

                            repeatMenuItem.Enabled = data.SupportsHostState(srcImage, hostState);
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
            currentFilterMenuItem = item;

            RunPhotoshopFilterThread(pluginData, true);
        }

        private void ShowFilterAboutDialog(object sender, EventArgs e)
        {
            ToolStripItem item = (ToolStripItem)sender;
            PluginData pluginData = (PluginData)item.Tag;

            try
            {
                PSFilterHost.ShowAboutDialog(pluginData, Handle);
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
                Cursor = Cursors.WaitCursor;

                filterThread = new Thread(() => RunPhotoshopFilterImpl(pluginData, showUI))
                {
                    IsBackground = true,
                    Priority = ThreadPriority.AboveNormal
                };
                filterThread.SetApartmentState(ApartmentState.STA); // Some filters may use OLE which requires Single Threaded Apartment mode.
                filterThread.Start();
            }
        }

        private void FilterCompleted(bool setRepeatFilter)
        {
            if (setRepeatFilter)
            {
                SetRepeatEffectMenuItem();
            }

            filterThread.Join();
            filterThread = null;

            currentFilterMenuItem = null;
            Cursor = Cursors.Default;
            toolStripProgressBar1.Value = 0;
            toolStripProgressBar1.Visible = false;
            toolStripStatusLabel1.Text = string.Empty;
        }

        private void SaveImageOnUIThread()
        {
            if (string.IsNullOrEmpty(srcImageTempFileName))
            {
                if (historyStack.Count == 0)
                {
                    // Add the original image to the history stack.
                    historyStack.AddHistoryItem(canvas.ToCanvasHistoryState(), srcImage);
                }

                srcImageTempFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".tif");

                BitmapMetadata metadata = null;

                try
                {
                    metadata = srcImage.Metadata as BitmapMetadata;
                }
                catch (NotSupportedException)
                {
                }

                srcMetadata = null;
                if (metadata != null)
                {
                    // As WIC does not automatically convert between meta-data formats we have to do it manually.
                    BitmapMetadata convertedMetadata = MetadataHelper.ConvertMetadataToTIFF(metadata);
                    if (convertedMetadata != null)
                    {
                        srcMetadata = convertedMetadata.Clone();
                        srcMetadata.Freeze();
                    }
                    metadata = convertedMetadata;
                }

                hostColorProfiles = null;
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

                    hostColorProfiles = new HostColorManagement(documentColorProfile, monitorColorProfilePath);
                }

                using (FileStream stream = new FileStream(srcImageTempFileName, FileMode.Create, FileAccess.Write))
                {
                    TiffBitmapEncoder encoder = new TiffBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(srcImage, null, metadata, null));
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
                    Invoke(new Action(SaveImageOnUIThread));
                }

                using (FileStream stream = new FileStream(srcImageTempFileName, FileMode.Open, FileAccess.Read))
                {
                    const BitmapCreateOptions createOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;
                    image = BitmapFrame.Create(stream, createOptions, BitmapCacheOption.OnLoad);
                }

                hostInfo.Caption = MetadataHelper.GetIPTCCaption(image);
            }
            else
            {
                image = BitmapFrame.Create(dstImage, null, srcMetadata, null); // Create a new BitmapFrame so the source image's meta-data is available to the filters.
            }
            hostInfo.HighDpi = highDPIMode;

            IntPtr owner = (IntPtr)Invoke(new Func<IntPtr>(delegate () { return Handle; }));
            bool setRepeatFilter = false;

            try
            {
                System.Windows.Media.Color primary = GDIPlusToWPFColor(primaryColorBtn.Color);
                System.Windows.Media.Color secondary = GDIPlusToWPFColor(secondaryColorBtn.Color);

                using (PSFilterHost host = new PSFilterHost(image, primary, secondary, selection, owner))
                {
                    host.SetAbortCallback(new AbortFunc(messageFilter.AbortFilterCallback));
                    host.SetPickColorCallback(new PickColor(PickColorCallback));
                    host.UpdateProgress += new EventHandler<FilterProgressEventArgs>(UpdateFilterProgress);

                    ParameterData parameters;
                    if (filterParameters.TryGetValue(pluginData, out parameters))
                    {
                        host.FilterParameters = parameters;
                    }

                    host.PseudoResources = pseudoResources;
                    host.HostInfo = hostInfo;
                    host.SessionSettings = sessionSettings;
                    if (hostColorProfiles != null)
                    {
                        host.SetColorProfiles(hostColorProfiles);
                    }

                    filterName = pluginData.Title.TrimEnd('.');
                    setFilterApplyText = false;
                    messageFilter.Reset();

                    if (host.RunFilter(pluginData, showUI))
                    {
                        dstImage = host.Dest;

                        FormatConvertedBitmap convertedImage = null;

                        int bitsPerChannel = dstImage.Format.GetBitsPerChannel();

                        if (bitsPerChannel >= 16)
                        {
                            convertedImage = new FormatConvertedBitmap(dstImage, dstImage.Format.IsAlphaFormat() ? PixelFormats.Bgra32 : PixelFormats.Bgr24, null, 0.0);
                        }

                        canvas.SuspendPaint();

                        UpdateCanvasImage(convertedImage ?? dstImage);

                        historyStack.AddHistoryItem(canvas.ToCanvasHistoryState(), dstImage);

                        canvas.ResumePaint();

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

                            pseudoResources = host.PseudoResources;
                            hostInfo = host.HostInfo;
                            sessionSettings = host.SessionSettings;
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
                BeginInvoke(new Action(() => FilterCompleted(setRepeatFilter)));
            }
        }

        private void SetRepeatEffectMenuItem()
        {
            if (currentFilterMenuItem != null)
            {
                if (!filtersToolStripMenuItem.DropDownItems.ContainsKey("repeatEffect"))
                {
                    ToolStripMenuItem repeatItem = new ToolStripMenuItem(currentFilterMenuItem.Text.TrimEnd('.'), null, new EventHandler(RepeatLastEffect))
                    {
                        Name = "repeatEffect",
                        Tag = currentFilterMenuItem.Tag,
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
                    repeatItem.Text = currentFilterMenuItem.Text.TrimEnd('.');
                    repeatItem.Tag = currentFilterMenuItem.Tag;
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
            if (InvokeRequired)
            {
                Invoke(new Action<string>(delegate (string text)
                {
                    toolStripStatusLabel1.Text = string.Format(Resources.ApplyFilterStatusFormat, text);
                    toolStripProgressBar1.Visible = true;
                }), new object[] { filterName });
            }
            else
            {
                toolStripStatusLabel1.Text = string.Format(Resources.ApplyFilterStatusFormat, filterName);
                toolStripProgressBar1.Visible = true;
            }
        }

        private void UpdateFilterProgress(object sender, FilterProgressEventArgs e)
        {
            if (!setFilterApplyText)
            {
                SetApplyFilterText();
                setFilterApplyText = true;
            }

            if (InvokeRequired)
            {
                Invoke(new Action<int>(delegate (int value)
                {
                    toolStripProgressBar1.Value = value;
                }), new object[] { e.Progress });
            }
            else
            {
                toolStripProgressBar1.Value = e.Progress;
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    OpenFile(openFileDialog1.FileName);
                }
                catch (ArgumentException ex)
                {
                    ShowErrorMessage(ex.Message);
                }
                catch (ExternalException ex)
                {
                    ShowErrorMessage(ex.Message);
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
                catch (SecurityException ex)
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
                        srcColorContext = null;
                    }
                }

                PngBitmapEncoder enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(colorCorrectedImage ?? image, null, null, null));
                enc.Save(stream);

                if (InvokeRequired)
                {
                    Invoke(new Action<MemoryStream>(delegate (MemoryStream ms)
                    {
                        canvas.Surface = new Bitmap(ms);
                    }), new object[] { stream });
                }
                else
                {
                    canvas.Surface = new Bitmap(stream);
                }
            }
        }

        /// <summary>
        /// Adjusts the image orientation based in the meta data.
        /// </summary>
        /// <param name="frame">The source image.</param>
        /// <returns>The adjusted image.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="frame"/> is null.</exception>
        private static BitmapFrame AdjustImageOrientation(BitmapFrame frame)
        {
            if (frame == null)
            {
                throw new ArgumentNullException(nameof(frame));
            }

            BitmapMetadata metadata = null;

            try
            {
                metadata = frame.Metadata as BitmapMetadata;
            }
            catch (NotSupportedException)
            {
            }

            if (metadata != null)
            {
                Transform transform = MetadataHelper.GetOrientationTransform(metadata);

                if (transform != null)
                {
                    TransformedBitmap transformedBitmap = new TransformedBitmap(frame, transform);

                    return BitmapFrame.Create(transformedBitmap, null, MetadataHelper.SetOrientationToTopLeft(metadata), null);
                }
            }

            return BitmapFrame.Create(frame.Clone(), null, metadata?.Clone(), null);
        }

        private void OpenFile(string path)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                const BitmapCreateOptions createOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;
                BitmapFrame frame = BitmapFrame.Create(new Uri(path), createOptions, BitmapCacheOption.None);

                PixelFormat actualFormat = frame.DetectDishonestAlphaFormat();

                if (frame.Format != actualFormat)
                {
                    // Convert the image to a non-alpha format if it does not have transparency.
                    FormatConvertedBitmap convertedBitmap = new FormatConvertedBitmap(frame, actualFormat, null, 0.0);

                    BitmapMetadata metadata = null;

                    try
                    {
                        metadata = frame.Metadata as BitmapMetadata;
                    }
                    catch (NotSupportedException)
                    {
                    }

                    srcImage = AdjustImageOrientation(BitmapFrame.Create(convertedBitmap, null, metadata, null));
                    srcImage.Freeze();
                }
                else
                {
                    srcImage = AdjustImageOrientation(frame);
                    srcImage.Freeze();
                }

                if (frame.ColorContexts != null)
                {
                    srcColorContext = frame.ColorContexts[0];
                }
                else
                {
                    // If the image does not have an embedded color profile assume it is sRGB.
                    srcColorContext = SrgbColorContext;
                }

                PixelFormat format = srcImage.Format;
                int bitsPerChannel = format.GetBitsPerChannel();

                imageFileName = Path.GetFileName(path);

                if (format == PixelFormats.BlackWhite ||
                    format == PixelFormats.Gray2 ||
                    format == PixelFormats.Gray4 ||
                    format == PixelFormats.Gray8 ||
                    format == PixelFormats.Gray16 ||
                    format == PixelFormats.Gray32Float)
                {
                    imageType = "Gray/";
                }
                else
                {
                    imageType = "RGB/";
                }

                canvas.SuspendSelectionEvents();

                canvas.ClearSelection();

                panel1.SuspendLayout();

                if (bitsPerChannel >= 16)
                {
                    // Convert the image to an 8 bits-per-channel format for display.
                    UpdateCanvasImage(new FormatConvertedBitmap(srcImage, format.IsAlphaFormat() ? PixelFormats.Bgra32 : PixelFormats.Bgr24, null, 0.0));

                    imageType += "16";
                }
                else
                {
                    UpdateCanvasImage(srcImage);

                    imageType += "8";
                }

                panelClientSize = panel1.ClientSize;

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

                    panelClientSize = clientSize;
                    canvas.ZoomToWindow(clientSize);
                }
                else
                {
                    Text = string.Format(Resources.TitleStringFormat, new object[] { applicationName, imageFileName, 100, imageType });

                    zoomInBtn.Enabled = canvas.CanZoomIn();
                    zoomOutBtn.Enabled = canvas.CanZoomOut();
                    zoomToWindowBtn.Enabled = canvas.CanZoomToWindow(panel1.ClientSize);
                    zoomToActualSizeBtn.Enabled = canvas.CanZoomToActualSize();
                }

                canvas.ResumeSelectionEvents();
                panel1.ResumeLayout(true);

                disabledIncompatableFilters = false;

                pointerSelectBtn.Enabled = true;
                rectangleSelectBtn.Enabled = true;
                elipseSelectBtn.Enabled = true;

                historyStack.Clear();

                canvas.IsDirty = false;
                dstImage = null;

                hostInfo.Title = imageFileName;

                if (!string.IsNullOrEmpty(srcImageTempFileName))
                {
                    File.Delete(srcImageTempFileName);
                    srcImageTempFileName = string.Empty;
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dstImage != null)
            {
                int bitsPerChannel = dstImage.Format.BitsPerPixel / dstImage.Format.Masks.Count;

                if (bitsPerChannel >= 16)
                {
                    saveFileDialog1.Filter = "PNG Image (*.png)|*.png|TIFF Image (*.tif, *.tiff)|*.tif;*.tiff|Windows Media Photo (*.wdp, *.jxr)|*.wdp;*.jxr";
                    saveFileDialog1.FilterIndex = 1;
                }
                else
                {
                    saveFileDialog1.Filter = "Bitmap Image (*.bmp)|*.bmp|GIF Image (*.gif)|*.gif|JPEG Image (*.jpg, *.jpeg, *.jpe)|*.jpg;*.jpeg;*.jpe|PNG Image (*.png)|*.png|TIFF Image (*.tif, *.tiff)|*.tif;*.tiff|Windows Media Photo (*.wdp, *.jxr)|*.wdp;*.jxr";
                    saveFileDialog1.FilterIndex = 4;
                }
                saveFileDialog1.FileName = Path.ChangeExtension(imageFileName, null);

                if (saveFileDialog1.ShowDialog(this) == DialogResult.OK)
                {
                    string path = saveFileDialog1.FileName;
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

                    BitmapMetadata metadata = null;

                    try
                    {
                        metadata = srcImage.Metadata as BitmapMetadata;
                    }
                    catch (NotSupportedException)
                    {
                    }

                    if (metadata != null)
                    {
                        metadata = MetadataHelper.ConvertSaveMetadataFormat(metadata, encoder);
                    }

                    ReadOnlyCollection<ColorContext> colorContexts = null;
                    if (srcColorContext != null)
                    {
                        colorContexts = Array.AsReadOnly(new ColorContext[] { srcColorContext });
                    }

                    encoder.Frames.Add(BitmapFrame.Create(dstImage, null, metadata, colorContexts));

                    try
                    {
                        using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            encoder.Save(fs);
                        }

                        canvas.IsDirty = false;
                    }
                    catch (ArgumentException ex)
                    {
                        ShowErrorMessage(ex.Message);
                    }
                    catch (IOException ex)
                    {
                        ShowErrorMessage(ex.Message);
                    }
                    catch (NotSupportedException ex)
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

        private void pointerSelectBtn_Click(object sender, EventArgs e)
        {
            if (!pointerSelectBtn.Checked)
            {
                rectangleSelectBtn.Checked = false;
                elipseSelectBtn.Checked = false;
                pointerSelectBtn.Checked = true;
                toolStripStatusLabel1.Text = string.Empty;
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
                toolStripStatusLabel1.Text = Resources.RectangleSelectionToolStatusText;
            }

            if ((canvas.SelectionType == null) || canvas.SelectionType.GetType() != typeof(RectangleSelectTool))
            {
                canvas.SelectionType = new RectangleSelectTool();
            }
        }

        private void elipseSelectBtn_Click(object sender, EventArgs e)
        {
            if (!elipseSelectBtn.Checked)
            {
                pointerSelectBtn.Checked = false;
                rectangleSelectBtn.Checked = false;
                elipseSelectBtn.Checked = true;
                toolStripStatusLabel1.Text = Resources.EllipseSelectionToolStatusText;
            }

            if ((canvas.SelectionType == null) || canvas.SelectionType.GetType() != typeof(EllipseSelectTool))
            {
                canvas.SelectionType = new EllipseSelectTool();
            }
        }

        private void zoomInBtn_Click(object sender, EventArgs e)
        {
            canvas.ZoomIn();
        }

        private void zoomOutBtn_Click(object sender, EventArgs e)
        {
            canvas.ZoomOut();
        }

        private void zoomToWindowBtn_Click(object sender, EventArgs e)
        {
            if (!panelClientSize.IsEmpty)
            {
                canvas.ZoomToWindow(panelClientSize);
            }
        }

        private void zoomToActualSizeBtn_Click(object sender, EventArgs e)
        {
            canvas.ZoomToActualSize();
        }

        private void canvas_ZoomChanged(object sender, CanvasZoomChangedEventArgs e)
        {
            zoomOutBtn.Enabled = canvas.CanZoomOut();
            zoomInBtn.Enabled = canvas.CanZoomIn();

            zoomToActualSizeBtn.Enabled = canvas.CanZoomToActualSize();
            zoomToWindowBtn.Enabled = canvas.CanZoomToWindow(panelClientSize);

            int percent;
            if (e.NewZoom < 0.10f)
            {
                percent = (int)Math.Round(e.NewZoom * 1000f);
            }
            else
            {
                percent = (int)Math.Round(e.NewZoom * 100f);
            }

            Text = string.Format(Resources.TitleStringFormat, new object[] { applicationName, imageFileName, percent, imageType });
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            historyStack.StepBackward(canvas, ref dstImage);
            EnableUndoButtons();
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            historyStack.StepForward(canvas, ref dstImage);
            EnableUndoButtons();
        }

        private void EnableUndoButtons()
        {
            undoToolStripMenuItem.Enabled = historyStack.CanUndo;
            redoToolStripMenuItem.Enabled = historyStack.CanRedo;
        }

        private void historyStack_HistoryChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(delegate ()
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
                dialog.Color = primaryColorBtn.Color;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    primaryColorBtn.Color = dialog.Color;
                }
            }
        }

        private void secondaryColorBtn_Click(object sender, EventArgs e)
        {
            using (ColorPickerForm dialog = new ColorPickerForm(Resources.ChooseSecondaryColor))
            {
                dialog.Color = secondaryColorBtn.Color;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    secondaryColorBtn.Color = dialog.Color;
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
                string actionText = string.Format(Resources.UnsavedChangesText, imageFileName);

                int width96 = (TaskDialog.DefaultPixelWidth96Dpi * 4) / 3; // 33% larger

                using (Bitmap bmp = canvas.ResizeCopy(96, 96))
                {
                    TaskButton result = TaskDialog.Show(this, Resources.Warning, Resources.SaveChangesCaption, bmp, true, actionText,
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

            if (!e.Cancel)
            {
                SavePluginSessionSettings();
            }

            base.OnFormClosing(e);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (WindowState == FormWindowState.Maximized && canvas.IsActualSize)
            {
                // If the window is maximized with the canvas at 100% do not resize the canvas.
                return;
            }

            if (!panel1.ClientSize.IsEmpty && panelClientSize != panel1.ClientSize && canvas.Surface != null)
            {
                canvas.ResetSize();
                panelClientSize = panel1.ClientSize;

                zoomToWindowBtn_Click(this, EventArgs.Empty);
            }
        }

        private void canvas_DirtyChanged(object sender, CanvasDirtyChangedEventArgs e)
        {
            saveToolStripMenuItem.Enabled = e.Dirty;
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
            dropImageFileName = string.Empty;
            drgevent.Effect = DragDropEffects.None;
            if (drgevent.Data.GetDataPresent(DataFormats.FileDrop, false))
            {
                string[] files = drgevent.Data.GetData(DataFormats.FileDrop, false) as string[];

                if (files != null && files.Length == 1 && FileDropIsImage(files[0]))
                {
                    drgevent.Effect = DragDropEffects.Copy;
                    dropImageFileName = files[0];
                }
            }

            base.OnDragEnter(drgevent);
        }

        protected override void OnDragDrop(DragEventArgs drgevent)
        {
            if (!string.IsNullOrEmpty(dropImageFileName))
            {
                try
                {
                    OpenFile(dropImageFileName);
                }
                catch (ArgumentException ex)
                {
                    ShowErrorMessage(ex.Message);
                }
                catch (ExternalException ex)
                {
                    ShowErrorMessage(ex.Message);
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
                catch (NotSupportedException ex)
                {
                    ShowErrorMessage(ex.Message);
                }
                catch (SecurityException ex)
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
                Path = path;
                Filters = null;
                AboutFilters = null;
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

                        if (!parent.DropDownItems.ContainsKey(plug.Title))
                        {
                            parent.DropDownItems.Add(child);
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

                Array.Sort(filters, comparer);

                // sort the items in the sub menus.
                for (int i = 0; i < filters.Length; i++)
                {
                    filters[i].DropDownItems.Sort(comparer);
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
                    filtersToolStripMenuItem.DropDownItems.AddRange(args.Filters);

                    if (args.AboutFilters.Length > 0)
                    {
                        aboutPluginsMenuItem.DropDownItems.AddRange(args.AboutFilters);
                        if (!aboutPluginsMenuItem.Available)
                        {
                            aboutMenuToolStripSeparator.Available = true;
                            aboutPluginsMenuItem.Available = true;
                        }
                    }
                    else
                    {
                        aboutMenuToolStripSeparator.Available = false;
                        aboutPluginsMenuItem.Available = false;
                    }

                    EnableFiltersForHostState();

                    toolStripStatusLabel1.Text = string.Empty;
                    currentPluginDirectory = args.Path;
                    refreshFiltersToolStripMenuItem.Enabled = true;
                }
                else
                {
                    toolStripStatusLabel1.Text = Resources.NoFiltersStatusText;
                    refreshFiltersToolStripMenuItem.Enabled = false;
                    currentPluginDirectory = string.Empty;

                    if (aboutPluginsMenuItem.Available)
                    {
                        aboutMenuToolStripSeparator.Available = false;
                        aboutPluginsMenuItem.Available = false;
                    }
                }
            }

            Cursor = Cursors.Default;
        }

        private ColorPickerResult PickColorCallback(string prompt, byte defaultRed, byte defaultGreen, byte defaultBlue)
        {
            ColorPickerResult color = null;

            using (ColorPickerForm dialog = new ColorPickerForm(prompt))
            {
                dialog.Color = System.Drawing.Color.FromArgb(defaultRed, defaultGreen, defaultBlue);

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    color = new ColorPickerResult(dialog.Color);
                }
            }

            return color;
        }

        private void filtersToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            if (highDPIMode)
            {
                ToolStripDropDownItem item = (ToolStripDropDownItem)sender;
                DpiAwareToolStripRenderer.ScaleScrollButtonArrows(item.DropDown as ToolStripDropDownMenu);
            }
            if (!disabledIncompatableFilters)
            {
                Cursor = Cursors.WaitCursor;
                try
                {
                    EnableFiltersForHostState();
                }
                finally
                {
                    Cursor = Cursors.Default;
                }
                disabledIncompatableFilters = true;
            }
        }

        private void aboutPluginsMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            if (highDPIMode)
            {
                ToolStripDropDownItem item = (ToolStripDropDownItem)sender;
                DpiAwareToolStripRenderer.ScaleScrollButtonArrows(item.DropDown as ToolStripDropDownMenu);
            }
        }

        private void canvas_SelectionCreated(object sender, EventArgs e)
        {
            disabledIncompatableFilters = false;
        }

        private void canvas_SelectionDestroyed(object sender, EventArgs e)
        {
            disabledIncompatableFilters = false;
        }
    }
}
