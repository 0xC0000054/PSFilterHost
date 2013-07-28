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
using System.Security.Permissions;
using PSFilterLoad.PSApi;

#if !GDIPLUS
using System.Windows.Media.Imaging;
#endif

#if NET_35_OR_GREATER
using System.Linq;
#endif

#if NET_40_OR_GREATER
using System.Security;
#endif

namespace PSFilterHostDll
{
	/// <summary>
	/// The class that enumerates and runs the Adobe® Photoshop® filters.
	/// </summary>
	/// <threadsafety static="true" instance="false" />
	public sealed class PSFilterHost : IDisposable
	{
#if GDIPLUS
		private Bitmap source;
		private Bitmap dest;		
		private Color primaryColor;
		private Color secondaryColor;		
#else
		private BitmapSource source;
		private BitmapSource dest;
		private System.Windows.Media.Color primaryColor;
		private System.Windows.Media.Color secondaryColor;
#endif

		private ParameterData filterParameters;      
		private Region selectedRegion;
		private IntPtr owner;
		private AbortFunc abortFunc;
		private List<PSResource> pseudoResources;
		private HostInformation hostInfo;

		/// <summary>
		/// The event fired when the filter updates it's progress. 
		/// </summary>
		public event EventHandler<FilterProgressEventArgs> UpdateProgress;

#if GDIPLUS

#if NET_40_OR_GREATER
		/// <summary>
		/// Initializes a new instance of the <see cref="PSFilterHost"/> class.
		/// </summary>
		/// <param name="sourceImage">The image to filter.</param>
		/// <param name="parentWindowHandle">The main window handle of the host application.</param>
		/// <exception cref="System.ArgumentNullException">The source image is null.</exception>
		/// <permission cref="System.Security.SecurityCriticalAttribute">requires full trust for the immediate caller. This member cannot be used by partially trusted or transparent code.</permission>
		[SecurityCritical()]
#else
		/// <summary>
		/// Initializes a new instance of the <see cref="PSFilterHost"/> class.
		/// </summary>
		/// <param name="sourceImage">The image to filter.</param>
		/// <param name="parentWindowHandle">The main window handle of the host application.</param>
		/// <exception cref="System.ArgumentNullException">The source image is null.</exception>
		/// <permission cref="SecurityPermission"> for unmanaged code permission. <para>Associated enumeration: <see cref="SecurityPermissionFlag.UnmanagedCode"/> Security action: <see cref="SecurityAction.LinkDemand"/></para></permission>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
#endif
		public PSFilterHost(Bitmap sourceImage, IntPtr parentWindowHandle)
			: this(sourceImage, Color.Black, Color.White, null, parentWindowHandle)
		{
		}

#if NET_40_OR_GREATER
		/// <summary>
		/// Initializes a new instance of the <see cref="PSFilterHost"/> class.
		/// </summary>
		/// <param name="sourceImage">The image to filter.</param>
		/// <param name="primary">The primary (foreground) color of the host application.</param>
		/// <param name="secondary">The secondary (background) color of the host application.</param>
		/// <param name="selectedRegion">The <see cref="System.Drawing.Region"/> defining the shape of the selection.</param>
		/// <param name="parentWindowHandle">The main window handle of the host application.</param>
		/// <exception cref="System.ArgumentNullException">The source image is null.</exception>
		/// <permission cref="System.Security.SecurityCriticalAttribute">requires full trust for the immediate caller. This member cannot be used by partially trusted or transparent code.</permission>
		[SecurityCritical()]
#else
		/// <summary>
		/// Initializes a new instance of the <see cref="PSFilterHost"/> class.
		/// </summary>
		/// <param name="sourceImage">The image to filter.</param>
		/// <param name="primary">The primary (foreground) color of the host application.</param>
		/// <param name="secondary">The secondary (background) color of the host application.</param>
		/// <param name="selectedRegion">The <see cref="System.Drawing.Region"/> defining the shape of the selection.</param>
		/// <param name="parentWindowHandle">The main window handle of the host application.</param>
		/// <exception cref="System.ArgumentNullException">The source image is null.</exception>
		/// <permission cref="SecurityPermission"> for unmanaged code permission. <para>Associated enumeration: <see cref="SecurityPermissionFlag.UnmanagedCode"/> Security action: <see cref="SecurityAction.LinkDemand"/></para></permission>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
#endif		
		public PSFilterHost(Bitmap sourceImage, Color primary, Color secondary, Region selectedRegion, IntPtr parentWindowHandle)
		{
			if (sourceImage == null)
				throw new ArgumentNullException("sourceImage", "sourceImage is null.");

			this.source = (Bitmap)sourceImage.Clone();  
			this.dest = null;
			this.filterParameters = null;
			this.primaryColor = primary;
			this.secondaryColor = secondary;
			if (selectedRegion != null)
			{
				this.selectedRegion = selectedRegion.Clone();
			}
			else
			{
				this.selectedRegion = null;
			}
			this.owner = parentWindowHandle;
			this.pseudoResources = null;
			this.abortFunc = null;
			PSFilterHostDll.BGRASurface.BGRASurfaceMemory.CreateHeap();
		}

		/// <summary>
		/// Gets the destination image.
		/// </summary>
		public Bitmap Dest
		{
			get
			{
				return dest;
			}
		}
#else

#if NET_40_OR_GREATER
		/// <summary>
		/// Initializes a new instance of the <see cref="PSFilterHost"/> class.
		/// </summary>
		/// <param name="sourceImage">The image to filter.</param>
		/// <param name="parentWindowHandle">The main window handle of the host application.</param>
		/// <exception cref="System.ArgumentNullException">The source image is null.</exception>
		/// <permission cref="System.Security.SecurityCriticalAttribute">requires full trust for the immediate caller. This member cannot be used by partially trusted or transparent code.</permission>
		[SecurityCritical()]
#else
		/// <summary>
		/// Initializes a new instance of the <see cref="PSFilterHost"/> class.
		/// </summary>
		/// <param name="sourceImage">The image to filter.</param>
		/// <param name="parentWindowHandle">The main window handle of the host application.</param>
		/// <exception cref="System.ArgumentNullException">The source image is null.</exception>
		/// <permission cref="SecurityPermission"> for unmanaged code permission. <para>Associated enumeration: <see cref="SecurityPermissionFlag.UnmanagedCode"/> Security action: <see cref="SecurityAction.LinkDemand"/></para></permission>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
#endif
		public PSFilterHost(BitmapSource sourceImage, IntPtr parentWindowHandle) : this(sourceImage, System.Windows.Media.Colors.Black, System.Windows.Media.Colors.White, null, parentWindowHandle)
		{
		}

#if NET_40_OR_GREATER
		/// <summary>
		/// Initializes a new instance of the <see cref="PSFilterHost"/> class.
		/// </summary>
		/// <param name="sourceImage">The image to filter.</param>
		/// <param name="primary">The primary (foreground) color of the host application.</param>
		/// <param name="secondary">The secondary (background) color of the host application.</param>
		/// <param name="selectedRegion">The <see cref="System.Drawing.Region"/> defining the shape of the selection.</param>
		/// <param name="parentWindowHandle">The main window handle of the host application.</param>
		/// <exception cref="System.ArgumentNullException">The source image is null.</exception>
		/// <permission cref="System.Security.SecurityCriticalAttribute">requires full trust for the immediate caller. This member cannot be used by partially trusted or transparent code.</permission>
		[SecurityCritical()]
#else
		/// <summary>
		/// Initializes a new instance of the <see cref="PSFilterHost"/> class.
		/// </summary>
		/// <param name="sourceImage">The image to filter.</param>
		/// <param name="primary">The primary (foreground) color of the host application.</param>
		/// <param name="secondary">The secondary (background) color of the host application.</param>
		/// <param name="selectedRegion">The <see cref="System.Drawing.Region"/> defining the shape of the selection.</param>
		/// <param name="parentWindowHandle">The main window handle of the host application.</param>
		/// <exception cref="System.ArgumentNullException">The source image is null.</exception>
		/// <permission cref="SecurityPermission"> for unmanaged code permission. <para>Associated enumeration: <see cref="SecurityPermissionFlag.UnmanagedCode"/> Security action: <see cref="SecurityAction.LinkDemand"/></para></permission>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
#endif
		public PSFilterHost(BitmapSource sourceImage,  System.Windows.Media.Color primary,  System.Windows.Media.Color secondary, Region selectedRegion, IntPtr parentWindowHandle)
		{
			if (sourceImage == null)
				throw new ArgumentNullException("sourceImage", "sourceImage is null.");

			this.source = sourceImage.Clone();
			this.dest = null;
			this.filterParameters = null;
			this.primaryColor = primary;
			this.secondaryColor = secondary;
			if (selectedRegion != null)
			{
				this.selectedRegion = selectedRegion.Clone();
			}
			else
			{
				this.selectedRegion = null;
			}
			this.owner = parentWindowHandle;
			this.pseudoResources = null;
			this.abortFunc = null;
			this.hostInfo = null;
			PSFilterHostDll.BGRASurface.BGRASurfaceMemory.CreateHeap();
		}

		/// <summary>
		/// Gets the destination image.
		/// </summary>
		public BitmapSource Dest
		{
			get
			{
				return dest;
			}
		}
#endif

		/// <summary>
		/// Gets or sets the filter parameters used for the 'Repeat Effect' command.
		/// </summary>
		/// <value>
		/// The filter parameters to use.
		/// </value>
		public ParameterData FilterParameters
		{
			get
			{
				return filterParameters;
			}
			set
			{
				filterParameters = value;
			}
		}

		/// <summary>
		/// Gets or sets the host information used by the filters.
		/// </summary>
		/// <value>
		/// The host information.
		/// </value>
		public HostInformation HostInfo
		{
			get
			{
				return hostInfo;
			}
			set
			{
				hostInfo = value;
			}
		}

		/// <summary>
		/// Gets or sets the pseudo resources used by the plug-ins.
		/// </summary>
		/// <value>
		/// The pseudo resources.
		/// </value>
		public PseudoResourceCollection PseudoResources
		{
			get
			{
				return new PseudoResourceCollection(pseudoResources);
			}
			set
			{
				if ((value != null) && value.Count > 0)
				{
					this.pseudoResources = new List<PSResource>(value);
				}
			}
		}

		/// <summary>
		/// Sets the abort function callback delegate.
		/// </summary>
		/// <param name="abortCallback">The abort callback.</param>
		/// <exception cref="System.ArgumentNullException">The abortCallback is null.</exception>
		public void SetAbortCallback(AbortFunc abortCallback)
		{
			if (abortCallback == null)
				throw new ArgumentNullException("abortCallback", "abortCallback is null.");

			this.abortFunc = abortCallback;
		}


#if NET_40_OR_GREATER
		/// <summary>
		/// Queries the directory for filters to load.
		/// </summary>
		/// <param name="directory">The directory to query.</param>
		/// <param name="searchSubDirectories">if set to <c>true</c> search the subdirectories.</param>
		/// <returns>A new <see cref="FilterCollection"/> containing the list of loaded filters.</returns>
		/// <exception cref="System.ArgumentException">The directory string is null or empty.</exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">The specified directory was not found.</exception>
		/// <permission cref="System.Security.SecurityCriticalAttribute">requires full trust for the immediate caller. This member cannot be used by partially trusted or transparent code.</permission>
		[SecurityCritical()]
#else
		/// <summary>
		/// Queries the directory for filters to load.
		/// </summary>
		/// <param name="directory">The directory to query.</param>
		/// <param name="searchSubDirectories">if set to <c>true</c> search the subdirectories.</param>
		/// <returns>A new <see cref="FilterCollection"/> containing the list of loaded filters.</returns>
		/// <exception cref="System.ArgumentException">The directory string is null or empty.</exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">The specified directory was not found.</exception>
		/// <permission cref="SecurityPermission"> for unmanaged code permission. <para>Associated enumeration: <see cref="SecurityPermissionFlag.UnmanagedCode"/> Security action: <see cref="SecurityAction.LinkDemand"/></para></permission>  
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
#endif
		public static FilterCollection QueryDirectory(string directory, bool searchSubDirectories)
		{
			if (string.IsNullOrEmpty(directory))
				throw new ArgumentException("directory is null or empty.", "directory");

			new FileIOPermission(FileIOPermissionAccess.PathDiscovery, directory).Demand();

			List<PluginData> pluginInfo = new List<PluginData>();

			List<string> files = new List<string>();

			foreach (var path in FileEnumerator.EnumerateFiles(directory, ".8bf", searchSubDirectories))
			{
				files.Add(path);
			}

#if NET_35_OR_GREATER
			IEnumerable<string> links = FileEnumerator.EnumerateFiles(directory, ".lnk", searchSubDirectories);
			if (links.Any())
			{
				using (ShellLink shortcut = new ShellLink())
				{
					foreach (var file in links)
					{
						shortcut.Load(file);
						string path = ShortcutHelper.FixWoW64ShortcutPath(shortcut.Path);

						if (File.Exists(path) && Path.GetExtension(path).Equals(".8bf", StringComparison.OrdinalIgnoreCase))
						{
							files.Add(path);
						}
					}
				}
			}
#else
			string[] links = FileEnumerator.GetFiles(directory, ".lnk", searchSubDirectories);
			if (links.Length > 0)
			{
				using (ShellLink shortcut = new ShellLink())
				{
					foreach (var item in links)
					{
						shortcut.Load(item);
						string path = ShortcutHelper.FixWoW64ShortcutPath(shortcut.Path);

						if (File.Exists(path) && Path.GetExtension(path).Equals(".8bf", StringComparison.OrdinalIgnoreCase))
						{
							files.Add(path);
						}
					}
				}
			}
#endif
			// prevent any dialog from showing for missing DLLs.
			uint oldErrorMode = SafeNativeMethods.SetErrorMode(SafeNativeMethods.SetErrorMode(0U) | NativeConstants.SEM_FAILCRITICALERRORS); 
			try
			{
				foreach (var item in files)
				{
					List<PluginData> pluginData;
					if (LoadPsFilter.QueryPlugin(item, out pluginData))
					{
						int count = pluginData.Count;

						if (count > 1)
						{
							/* If the DLL contains more than one filter, add a list of all the entry points to each individual filter. 
							 * Per the SDK only one entry point in a module will display the about box the rest are dummy calls so we must call all of them. 
							 */
							string[] entryPoints = new string[count];
							for (int i = 0; i < count; i++)
							{
								entryPoints[i] = pluginData[i].EntryPoint;
							}

							for (int i = 0; i < count; i++)
							{
								pluginData[i].moduleEntryPoints = entryPoints;
							}
						}

						pluginInfo.AddRange(pluginData);
					}
				}
			}
			finally
			{
				SafeNativeMethods.SetErrorMode(oldErrorMode);
			}			
				
			return new FilterCollection(pluginInfo);
		}


#if NET_40_OR_GREATER
		/// <summary>
		/// Runs the specified filter.
		/// </summary>
		/// <param name="pluginData">The <see cref="PluginData"/> of the filter to run.</param>
		/// <returns>
		/// True if successful; false if the user canceled the dialog.
		/// </returns>
		/// <exception cref="System.ArgumentNullException">The <paramref name="pluginData"/> is null.</exception>
		/// <exception cref="System.IO.FileNotFoundException">The filter cannot be found.</exception>
		/// <exception cref="System.ObjectDisposedException">The object has been disposed.</exception>
		/// <exception cref="FilterRunException">The filter returns an error.</exception>
		/// <exception cref="ImageSizeTooLargeException">The width or height of the source image exceeds 32000 pixels.</exception>
		/// <permission cref="System.Security.SecurityCriticalAttribute">requires full trust for the immediate caller. This member cannot be used by partially trusted or transparent code.</permission>
		[SecurityCritical()]
#else      
		/// <summary>
		/// Runs the specified filter.
		/// </summary>
		/// <param name="pluginData">The <see cref="PluginData"/> of the filter to run.</param>
		/// <returns>
		/// True if successful; false if the user canceled the dialog.
		/// </returns>
		/// <exception cref="System.ArgumentNullException">The <paramref name="pluginData"/> is null.</exception>
		/// <exception cref="System.IO.FileNotFoundException">The filter cannot be found.</exception>
		/// <exception cref="System.ObjectDisposedException">The object has been disposed.</exception>
		/// <exception cref="FilterRunException">The filter returns an error.</exception>
		/// <exception cref="ImageSizeTooLargeException">The width or height of the source image exceeds 32000 pixels.</exception>
		/// <permission cref="SecurityPermission"> for unmanaged code permission. <para>Associated enumeration: <see cref="SecurityPermissionFlag.UnmanagedCode"/> Security action: <see cref="SecurityAction.LinkDemand"/></para></permission> 
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
#endif
		public bool RunFilter(PluginData pluginData)
		{
			if (pluginData == null)
				throw new ArgumentNullException("pluginData", "pluginData is null.");
			
			if (disposed)
				throw new ObjectDisposedException("PSFilterHost");

			bool result = false;

			using (LoadPsFilter lps = new LoadPsFilter(source, primaryColor, secondaryColor, selectedRegion, owner))
			{
				if (abortFunc != null)
				{
					lps.SetAbortFunc(abortFunc);
				}

				if (UpdateProgress != null)
				{
					lps.SetProgressFunc(new ProgressProc(OnFilterReportProgress));
				}

				if (filterParameters != null)
				{
					lps.ParameterData = filterParameters;
					lps.IsRepeatEffect = true;
				}

				if (pseudoResources != null)
				{
					lps.PseudoResources = pseudoResources;
				}

				if (hostInfo != null)
				{
					lps.HostInformation = hostInfo;
				}  

				try
				{
					result = lps.RunPlugin(pluginData);
				}
				catch (FileNotFoundException)
				{
					throw;
				}
				catch (Exception ex)
				{
					throw new FilterRunException(ex.Message, ex);
				}

				if (result && string.IsNullOrEmpty(lps.ErrorMessage))
				{

#if GDIPLUS
					this.dest = new Bitmap(lps.Dest.CreateAliasedBitmap());
#else
					this.dest = lps.Dest.CreateAliasedBitmapSource().Clone();
					this.dest.Freeze();
#endif
					


#if DEBUG
					using (FileStream fs = new FileStream(Path.Combine(Path.GetDirectoryName(typeof(PSFilterHost).Assembly.Location), "dest.png"), FileMode.Create, FileAccess.Write))
					{
#if GDIPLUS
						this.dest.Save(fs, System.Drawing.Imaging.ImageFormat.Png);
#else
						PngBitmapEncoder enc = new PngBitmapEncoder();
						enc.Frames.Add(BitmapFrame.Create(this.dest));
						enc.Save(fs); 
#endif
					} 
#endif

					this.filterParameters = lps.ParameterData;
					this.pseudoResources = lps.PseudoResources;
					this.hostInfo = lps.HostInformation;
				}
				else if (!string.IsNullOrEmpty(lps.ErrorMessage))
				{
					throw new FilterRunException(lps.ErrorMessage);
				}
			}

			return result;
		}

#if NET_40_OR_GREATER
		/// <summary>
		/// Shows the filter's about dialog.
		/// </summary>
		/// <param name="pluginData">The <see cref="PluginData"/> of the filter.</param>
		/// <param name="parentWindowHandle">The parent window handle.</param>
		/// <exception cref="System.ArgumentNullException">The pluginData is null.</exception>
		/// <exception cref="System.IO.FileNotFoundException">The filter cannot be found.</exception>
		/// <exception cref="FilterRunException">The filter returns an error.</exception> 
		/// <permission cref="System.Security.SecurityCriticalAttribute">requires full trust for the immediate caller. This member cannot be used by partially trusted or transparent code.</permission>
		[SecurityCritical()]
#else
		/// <summary>
		/// Shows the filter's about dialog.
		/// </summary>
		/// <param name="pluginData">The <see cref="PluginData"/> of the filter.</param>
		/// <param name="parentWindowHandle">The parent window handle.</param>
		/// <exception cref="System.ArgumentNullException">The pluginData is null.</exception>
		/// <exception cref="System.IO.FileNotFoundException">The filter cannot be found.</exception>
		/// <exception cref="FilterRunException">The filter returns an error.</exception> 
		/// <permission cref="SecurityPermission"> for unmanaged code permission. <para>Associated enumeration: <see cref="SecurityPermissionFlag.UnmanagedCode"/> Security action: <see cref="SecurityAction.LinkDemand"/></para></permission>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]        
#endif
		public static void ShowAboutDialog(PluginData pluginData, IntPtr parentWindowHandle)
		{
			if (pluginData == null)
				throw new ArgumentNullException("pluginData", "pluginData is null.");

			string errorMessage = string.Empty;
			
			bool result = LoadPsFilter.ShowAboutDialog(pluginData, parentWindowHandle, out errorMessage);
			if (!result && !string.IsNullOrEmpty(errorMessage))
			{
				throw new FilterRunException(errorMessage);
			}
		}

		private void OnFilterReportProgress(int done, int total)
		{
			double progress = ((double)done / (double)total) * 100.0;

			if (progress < 0.0)
			{
				progress = 0.0;
			}
			else if (progress > 100.0)
			{
				progress = 100.0;
			}

			UpdateProgress.Invoke(this, new FilterProgressEventArgs((int)progress)); 
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="PSFilterHost"/> is reclaimed by garbage collection.
		/// </summary>
		~PSFilterHost()
		{
			Dispose(false);
		}

		private bool disposed;
		private void Dispose(bool disposing)
		{
			if (!disposed)
			{
				disposed = true;

				if (disposing)
				{
#if GDIPLUS
					if (source != null)
					{
						source.Dispose();
						source = null;
					}

					if (dest != null)
					{
						dest.Dispose();
						dest = null;
					}
#endif

					if (selectedRegion != null)
					{
						selectedRegion.Dispose();
						selectedRegion = null;
					}
				}

				PSFilterHostDll.BGRASurface.BGRASurfaceMemory.DestroyHeap();
			}
		}
	}
}
