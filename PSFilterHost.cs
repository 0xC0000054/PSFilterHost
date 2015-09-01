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

using PSFilterHostDll.PSApi;
using PSFilterHostDll.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Security;
using System.IO;
using System.Threading;

#if !GDIPLUS
using System.Windows.Media.Imaging;
#endif

#if !NET_40_OR_GREATER
using System.Security.Permissions;
#endif

namespace PSFilterHostDll
{
	/// <summary>
	/// The class that enumerates and runs the Photoshop-compatible filters.
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
		private bool disposed;
		private ParameterData filterParameters;      
		private Region selectedRegion;
		private IntPtr owner;
		private AbortFunc abortFunc;
		private List<PSResource> pseudoResources;
		private HostInformation hostInfo;
		private PickColor pickColor;

		/// <summary>
		/// The event fired when the filter updates it's progress. 
		/// </summary>
		public event EventHandler<FilterProgressEventArgs> UpdateProgress;

#if NET_40_OR_GREATER
		/// <summary>
		/// Initializes a new instance of the <see cref="PSFilterHost"/> class.
		/// </summary>
		/// <param name="sourceImage">The image to filter.</param>
		/// <param name="parentWindowHandle">The main window handle of the host application.</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="sourceImage"/> is null.</exception>
		/// <exception cref="ImageSizeTooLargeException">The <paramref name="sourceImage"/> is greater that 32000 pixels in width or height.</exception>
		/// <permission cref="System.Security.SecurityCriticalAttribute">requires full trust for the immediate caller. This member cannot be used by partially trusted or transparent code.</permission>
		[SecurityCritical()]
#else
		/// <summary>
		/// Initializes a new instance of the <see cref="PSFilterHost"/> class.
		/// </summary>
		/// <param name="sourceImage">The image to filter.</param>
		/// <param name="parentWindowHandle">The main window handle of the host application.</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="sourceImage"/> is null.</exception>
		/// <exception cref="ImageSizeTooLargeException">The <paramref name="sourceImage"/> is greater that 32000 pixels in width or height.</exception>
		/// <permission cref="SecurityPermission"> for unmanaged code permission. <para>Associated enumeration: <see cref="SecurityPermissionFlag.UnmanagedCode"/> Security action: <see cref="SecurityAction.LinkDemand"/></para></permission>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
#endif

#if GDIPLUS
		public PSFilterHost(Bitmap sourceImage, IntPtr parentWindowHandle) : this(sourceImage, Color.Black, Color.White, null, parentWindowHandle)
		{
		}
#else
		public PSFilterHost(BitmapSource sourceImage, IntPtr parentWindowHandle) : this(sourceImage, System.Windows.Media.Colors.Black, System.Windows.Media.Colors.White, null, parentWindowHandle)
		{
		}
#endif

#if NET_40_OR_GREATER
		/// <summary>
		/// Initializes a new instance of the <see cref="PSFilterHost"/> class.
		/// </summary>
		/// <param name="sourceImage">The image to filter.</param>
		/// <param name="primary">The primary (foreground) color of the host application.</param>
		/// <param name="secondary">The secondary (background) color of the host application.</param>
		/// <param name="selectedRegion">The <see cref="System.Drawing.Region"/> defining the shape of the selection.</param>
		/// <param name="parentWindowHandle">The main window handle of the host application.</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="sourceImage"/> is null.</exception>
		/// <exception cref="ImageSizeTooLargeException">The <paramref name="sourceImage"/> is greater that 32000 pixels in width or height.</exception>
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
		/// <exception cref="System.ArgumentNullException"><paramref name="sourceImage"/> is null.</exception>
		/// <exception cref="ImageSizeTooLargeException">The <paramref name="sourceImage"/> is greater that 32000 pixels in width or height.</exception>
		/// <permission cref="SecurityPermission"> for unmanaged code permission. <para>Associated enumeration: <see cref="SecurityPermissionFlag.UnmanagedCode"/> Security action: <see cref="SecurityAction.LinkDemand"/></para></permission>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
#endif

#if GDIPLUS
		public PSFilterHost(Bitmap sourceImage, Color primary, Color secondary, Region selectedRegion, IntPtr parentWindowHandle)
#else
		public PSFilterHost(BitmapSource sourceImage,  System.Windows.Media.Color primary,  System.Windows.Media.Color secondary, Region selectedRegion, IntPtr parentWindowHandle)
#endif
		{
			if (sourceImage == null)
			{
				throw new ArgumentNullException("sourceImage", "sourceImage is null.");
			}

			int imageWidth = 0;
			int imageHeight = 0;

#if GDIPLUS
			imageWidth = sourceImage.Width;
			imageHeight = sourceImage.Height;
#else
			imageWidth = sourceImage.PixelWidth;
			imageHeight = sourceImage.PixelHeight;
#endif

			if (imageWidth > 32000 || imageHeight > 32000)
			{
				string message = string.Empty;
				if (imageWidth > 32000 && imageHeight > 32000)
				{
					message = Resources.ImageSizeTooLarge;
				}
				else
				{
					if (imageWidth > 32000)
					{
						message = Resources.ImageWidthTooLarge;
					}
					else
					{
						message = Resources.ImageHeightTooLarge;
					}
				}

				throw new ImageSizeTooLargeException(message);
			}

#if GDIPLUS
			this.source = (Bitmap)sourceImage.Clone();
#else
			this.source = sourceImage.Clone();
#endif
			this.disposed = false;
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
			this.pseudoResources = new List<PSResource>();
			this.abortFunc = null;
			this.hostInfo = null;
		}

#if GDIPLUS
		/// <summary>
		/// Gets the destination image.
		/// </summary>
		/// <exception cref="System.ObjectDisposedException">The object has been disposed.</exception>
		public Bitmap Dest
		{
			get
			{
				if (disposed)
				{
					throw new ObjectDisposedException("PSFilterHost");
				}

				return this.dest;
			}
		} 
#else
		/// <summary>
		/// Gets the destination image.
		/// </summary>
		/// <exception cref="System.ObjectDisposedException">The object has been disposed.</exception>
		public BitmapSource Dest
		{
			get
			{
				if (disposed)
				{
					throw new ObjectDisposedException("PSFilterHost");
				}

				return this.dest;
			}
		} 
#endif

		/// <summary>
		/// Gets or sets the <see cref="ParameterData"/> used to apply the filter with the previous settings.
		/// </summary>
		/// <value>
		/// The filter parameters to use.
		/// </value>
		public ParameterData FilterParameters
		{
			get
			{
				return this.filterParameters;
			}
			set
			{
				this.filterParameters = value;
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
				return this.hostInfo;
			}
			set
			{
				this.hostInfo = value;
			}
		}

		/// <summary>
		/// Gets or sets the Pseudo-Resources used by the plug-ins.
		/// </summary>
		/// <value>
		/// The pseudo resources.
		/// </value>
		public PseudoResourceCollection PseudoResources
		{
			get
			{
				return new PseudoResourceCollection(this.pseudoResources);
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
		/// <exception cref="System.ArgumentNullException"><paramref name="abortCallback"/> is null.</exception>
		public void SetAbortCallback(AbortFunc abortCallback)
		{
			if (abortCallback == null)
			{
				throw new ArgumentNullException("abortCallback");
			}

			this.abortFunc = abortCallback;
		}

		/// <summary>
		/// Sets the pick color function callback delegate.
		/// </summary>
		/// <param name="pickerCallback">The color picker callback.</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="pickerCallback"/> is null.</exception>
		public void SetPickColorCallback(PickColor pickerCallback)
		{
			if (pickerCallback == null)
			{
				throw new ArgumentNullException("pickerCallback");
			}

			this.pickColor = pickerCallback;
		}

#if NET_40_OR_GREATER
		/// <summary>
		/// Queries the directory for filters to load.
		/// </summary>
		/// <param name="path">The directory to search.</param>
		/// <param name="searchSubdirectories"><c>true</c> if the search operation should include all subdirectories; otherwise <c>false</c> to include only the current directory.</param>
		/// <returns>A new <see cref="FilterCollection"/> containing the filters found in the directory specified by <paramref name="path"/>.</returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="path"/> is null.</exception>
		/// <exception cref="System.ArgumentException"><paramref name="path"/> is a 0 length string, or contains only white-space, or contains one or more invalid characters as defined by <see cref="System.IO.Path.GetInvalidPathChars"/>.</exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">The directory specified by <paramref name="path"/> does not exist.</exception>
		/// <exception cref="System.IO.IOException"><paramref name="path"/> is a file name.</exception>
		/// <exception cref="System.IO.PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
		/// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
		/// <exception cref="System.UnauthorizedAccessException">The caller does not have the required permission.</exception>
		/// <permission cref="System.Security.SecurityCriticalAttribute">requires full trust for the immediate caller. This member cannot be used by partially trusted or transparent code.</permission>
		[SecurityCritical()]
#else
		/// <summary>
		/// Queries the directory for filters to load.
		/// </summary>
		/// <param name="path">The directory to search.</param>
		/// <param name="searchSubdirectories"><c>true</c> if the search operation should include all subdirectories; otherwise <c>false</c> to include only the current directory.</param>
		/// <returns>A new <see cref="FilterCollection"/> containing the filters found in the directory specified by <paramref name="path"/>.</returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="path"/> is null.</exception>
		/// <exception cref="System.ArgumentException"><paramref name="path"/> is a 0 length string, or contains only white-space, or contains one or more invalid characters as defined by <see cref="System.IO.Path.GetInvalidPathChars"/>.</exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">The directory specified by <paramref name="path"/> does not exist.</exception>
		/// <exception cref="System.IO.IOException"><paramref name="path"/> is a file name.</exception>
		/// <exception cref="System.IO.PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
		/// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
		/// <exception cref="System.UnauthorizedAccessException">The caller does not have the required permission.</exception>
		/// <permission cref="SecurityPermission"> for unmanaged code permission. <para>Associated enumeration: <see cref="SecurityPermissionFlag.UnmanagedCode"/> Security action: <see cref="SecurityAction.LinkDemand"/></para></permission>  
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
#endif
		public static FilterCollection QueryDirectory(string path, bool searchSubdirectories)
		{
			if (path == null)
			{
				throw new ArgumentNullException("path");
			}

			var filters = EnumerateFilters(path, searchSubdirectories);

			return new FilterCollection(filters);
		}


#if NET_40_OR_GREATER
		/// <summary>
		/// Enumerates the directory for filters to load.
		/// </summary>
		/// <param name="path">The directory to search.</param>
		/// <param name="searchSubdirectories"><c>true</c> if the search operation should include all subdirectories; otherwise <c>false</c> to include only the current directory.</param>
		/// <returns>An enumerable collection containing the filters found in the directory specified by <paramref name="path"/>.</returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="path"/> is null.</exception>
		/// <exception cref="System.ArgumentException"><paramref name="path"/> is a 0 length string, or contains only white-space, or contains one or more invalid characters as defined by <see cref="System.IO.Path.GetInvalidPathChars"/>.</exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">The directory specified by <paramref name="path"/> does not exist.</exception>
		/// <exception cref="System.IO.IOException"><paramref name="path"/> is a file name.</exception>
		/// <exception cref="System.IO.PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
		/// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
		/// <exception cref="System.UnauthorizedAccessException">The caller does not have the required permission.</exception>
		/// <permission cref="System.Security.SecurityCriticalAttribute">requires full trust for the immediate caller. This member cannot be used by partially trusted or transparent code.</permission>
		[SecurityCritical()]
#else
		/// <summary>
		/// Enumerates the directory for filters to load.
		/// </summary>
		/// <param name="path">The directory to search.</param>
		/// <param name="searchSubdirectories"><c>true</c> if the search operation should include all subdirectories; otherwise <c>false</c> to include only the current directory.</param>
		/// <returns>An enumerable collection containing the filters found  in the directory specified by <paramref name="path"/>.</returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="path"/> is null.</exception>
		/// <exception cref="System.ArgumentException"><paramref name="path"/> is a 0 length string, or contains only white-space, or contains one or more invalid characters as defined by <see cref="System.IO.Path.GetInvalidPathChars"/>.</exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">The directory specified by <paramref name="path"/> does not exist.</exception>
		/// <exception cref="System.IO.IOException"><paramref name="path"/> is a file name.</exception>
		/// <exception cref="System.IO.PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
		/// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
		/// <exception cref="System.UnauthorizedAccessException">The caller does not have the required permission.</exception>
		/// <permission cref="SecurityPermission"> for unmanaged code permission. <para>Associated enumeration: <see cref="SecurityPermissionFlag.UnmanagedCode"/> Security action: <see cref="SecurityAction.LinkDemand"/></para></permission>  
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
#endif
		public static IEnumerable<PluginData> EnumerateFilters(string path, bool searchSubdirectories)
		{
			if (path == null)
			{
				throw new ArgumentNullException("path");
			}

			using (FileEnumerator enumerator = new FileEnumerator(path, ".8bf", searchSubdirectories, true))
			{
				while (enumerator.MoveNext())
				{
					foreach (var item in LoadPsFilter.QueryPlugin(enumerator.Current))
					{
						yield return item;
					}
				}
			}
		}

#if NET_40_OR_GREATER
		/// <summary>
		/// Enumerates the directory for filters to load.
		/// </summary>
		/// <param name="path">The directory to search.</param>
		/// <param name="searchOption">One of the <see cref="SearchOption"/> values that specifies whether the search operation should include only the current directory or should include all subdirectories.</param>
		/// <returns>An enumerable collection containing the filters found in the directory specified by <paramref name="path"/>.</returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="path"/> is null.</exception>
		/// <exception cref="System.ArgumentException"><paramref name="path"/> is a 0 length string, or contains only white-space, or contains one or more invalid characters as defined by <see cref="System.IO.Path.GetInvalidPathChars"/>.</exception>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="searchOption"/> is not a valid <see cref="System.IO.SearchOption"/> value.</exception>
		/// <exception cref="System.IO.DirectoryNotFoundException"><paramref name="path"/> is invalid, such as referring to an unmapped drive.</exception>
		/// <exception cref="System.IO.IOException"><paramref name="path"/> is a file name.</exception>
		/// <exception cref="System.IO.PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
		/// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
		/// <exception cref="System.UnauthorizedAccessException">The caller does not have the required permission.</exception>
		/// <permission cref="System.Security.SecurityCriticalAttribute">requires full trust for the immediate caller. This member cannot be used by partially trusted or transparent code.</permission>
		[SecurityCritical()]
#else
		/// <summary>
		/// Enumerates the directory for filters to load.
		/// </summary>
		/// <param name="path">The directory to search.</param>
		/// <param name="searchOption">One of the <see cref="SearchOption"/> values that specifies whether the search operation should include only the current directory or should include all subdirectories.</param>
		/// <returns>An enumerable collection containing the filters found  in the directory specified by <paramref name="path"/>.</returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="path"/> is null.</exception>
		/// <exception cref="System.ArgumentException"><paramref name="path"/> is a 0 length string, or contains only white-space, or contains one or more invalid characters as defined by <see cref="System.IO.Path.GetInvalidPathChars"/>.</exception>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="searchOption"/> is not a valid <see cref="System.IO.SearchOption"/> value.</exception>
		/// <exception cref="System.IO.DirectoryNotFoundException"><paramref name="path"/> is invalid, such as referring to an unmapped drive.</exception>
		/// <exception cref="System.IO.IOException"><paramref name="path"/> is a file name.</exception>
		/// <exception cref="System.IO.PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
		/// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
		/// <exception cref="System.UnauthorizedAccessException">The caller does not have the required permission.</exception>
		/// <permission cref="SecurityPermission"> for unmanaged code permission. <para>Associated enumeration: <see cref="SecurityPermissionFlag.UnmanagedCode"/> Security action: <see cref="SecurityAction.LinkDemand"/></para></permission>  
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
#endif
		public static IEnumerable<PluginData> EnumerateFilters(string path, SearchOption searchOption)
		{
			if (path == null)
			{
				throw new ArgumentNullException("path");
			}

			if (searchOption < SearchOption.TopDirectoryOnly || searchOption > SearchOption.AllDirectories)
			{
				throw new ArgumentOutOfRangeException("searchOption");
			}

			return EnumerateFilters(path, searchOption == SearchOption.AllDirectories);
		}


#if NET_40_OR_GREATER
		/// <summary>
		/// Runs the specified filter.
		/// </summary>
		/// <param name="pluginData">The <see cref="PluginData"/> of the filter to run.</param>
		/// <returns>
		/// <c>true</c> if the filter completed processing; otherwise, <c>false</c>.
		/// </returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="pluginData"/> is null.</exception>
		/// <exception cref="System.IO.FileNotFoundException">The filter cannot be found.</exception>
		/// <exception cref="System.ObjectDisposedException">The object has been disposed.</exception>
		/// <exception cref="FilterRunException">The filter returns an error.</exception>
		/// <permission cref="System.Security.SecurityCriticalAttribute">requires full trust for the immediate caller. This member cannot be used by partially trusted or transparent code.</permission>
		[SecurityCritical()]
#else      
		/// <summary>
		/// Runs the specified filter.
		/// </summary>
		/// <param name="pluginData">The <see cref="PluginData"/> of the filter to run.</param>
		/// <returns>
		/// <c>true</c> if the filter completed processing; otherwise, <c>false</c>.
		/// </returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="pluginData"/> is null.</exception>
		/// <exception cref="System.IO.FileNotFoundException">The filter cannot be found.</exception>
		/// <exception cref="System.ObjectDisposedException">The object has been disposed.</exception>
		/// <exception cref="FilterRunException">The filter returns an error.</exception>
		/// <permission cref="SecurityPermission"> for unmanaged code permission. <para>Associated enumeration: <see cref="SecurityPermissionFlag.UnmanagedCode"/> Security action: <see cref="SecurityAction.LinkDemand"/></para></permission> 
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
#endif
		public bool RunFilter(PluginData pluginData)
		{
			if (pluginData == null)
			{
				throw new ArgumentNullException("pluginData");
			}

			if (disposed)
			{
				throw new ObjectDisposedException("PSFilterHost");
			}

			bool result = false;

			using (LoadPsFilter lps = new LoadPsFilter(source, primaryColor, secondaryColor, selectedRegion, owner))
			{
				if (abortFunc != null)
				{
					lps.SetAbortFunc(abortFunc);
				}

				if (pickColor != null)
				{
					lps.SetPickColor(pickColor);
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

				if (pseudoResources.Count > 0)
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
				catch (SecurityException)
				{
					throw;
				}
				catch (Exception ex)
				{
					if (ex is OutOfMemoryException || ex is StackOverflowException || ex is ThreadAbortException)
					{
						throw;
					}

					throw new FilterRunException(ex.Message, ex);
				}

				if (result)
				{
#if GDIPLUS
					this.dest = new Bitmap(lps.Dest.CreateAliasedBitmap());
#else
					this.dest = lps.Dest.CreateAliasedBitmapSource().Clone();
					this.dest.Freeze();
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
		/// <exception cref="System.ArgumentNullException"><paramref name="pluginData"/> is null.</exception>
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
		/// <exception cref="System.ArgumentNullException"><paramref name="pluginData"/> is null.</exception>
		/// <exception cref="System.IO.FileNotFoundException">The filter cannot be found.</exception>
		/// <exception cref="FilterRunException">The filter returns an error.</exception> 
		/// <permission cref="SecurityPermission"> for unmanaged code permission. <para>Associated enumeration: <see cref="SecurityPermissionFlag.UnmanagedCode"/> Security action: <see cref="SecurityAction.LinkDemand"/></para></permission>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]        
#endif
		public static void ShowAboutDialog(PluginData pluginData, IntPtr parentWindowHandle)
		{
			if (pluginData == null)
			{
				throw new ArgumentNullException("pluginData");
			}

			if (pluginData.HasAboutBox)
			{
				string errorMessage = string.Empty;

				bool result;

				try
				{
					result = LoadPsFilter.ShowAboutDialog(pluginData, parentWindowHandle, out errorMessage);
				}
				catch (FileNotFoundException)
				{
					throw;
				}
				catch (SecurityException)
				{
					throw;
				}
				catch (Exception ex)
				{
					if (ex is OutOfMemoryException || ex is StackOverflowException || ex is ThreadAbortException)
					{
						throw;
					}

					throw new FilterRunException(ex.Message, ex);
				}

				if (!result && !string.IsNullOrEmpty(errorMessage))
				{
					throw new FilterRunException(errorMessage);
				} 
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

			EventHandler<FilterProgressEventArgs> handler = UpdateProgress;
			if (handler != null)
			{
				handler(this, new FilterProgressEventArgs((int)progress));
			}
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
