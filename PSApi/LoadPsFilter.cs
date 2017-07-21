/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See License-pdn.txt for full licensing and attribution details.             //
//                                                                             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using PSFilterHostDll.BGRASurface;
using PSFilterHostDll.Properties;

#if !GDIPLUS
using System.Windows.Media.Imaging;
#endif

namespace PSFilterHostDll.PSApi
{
	internal sealed class LoadPsFilter : IDisposable
	{
		static bool RectNonEmpty(Rect16 rect)
		{
			return (rect.left < rect.right && rect.top < rect.bottom);
		}

		private static readonly int OTOFHandleSize = IntPtr.Size + 4;
		private const int OTOFSignature = 0x464f544f;

		private sealed class ChannelDescPtrs
		{
			public readonly IntPtr address;
			public readonly IntPtr name;

			public ChannelDescPtrs(IntPtr address, IntPtr name)
			{
				this.address = address;
				this.name = name;
			}
		}

		private List<ChannelDescPtrs> channelReadDescPtrs;

		#region CallbackDelegates
		// MiscCallbacks		
		private AdvanceStateProc advanceProc;
		private ColorServicesProc colorProc;
		private DisplayPixelsProc displayPixelsProc;
		private HostProcs hostProc;
		private ProcessEventProc processEventProc;
		private ProgressProc progressProc;
		private TestAbortProc abortProc;
		// ImageServicesProc
#if USEIMAGESERVICES
		private static PIResampleProc resample1DProc;
		private static PIResampleProc resample2DProc;
#endif
		// ChannelPorts
		private ReadPixelsProc readPixelsProc;
		private WriteBasePixelsProc writeBasePixelsProc;
		private ReadPortForWritePortProc readPortForWritePortProc;
		// PropertyProcs
		private GetPropertyProc getPropertyProc;
		private SetPropertyProc setPropertyProc;
		// SPBasic
		private SPBasicAcquireSuite spAcquireSuite;
		private SPBasicAllocateBlock spAllocateBlock;
		private SPBasicFreeBlock spFreeBlock;
		private SPBasicIsEqual spIsEqual;
		private SPBasicReallocateBlock spReallocateBlock;
		private SPBasicReleaseSuite spReleaseSuite;
		private SPBasicUndefined spUndefined;
		#endregion

		private IntPtr filterRecordPtr;
		private IntPtr platFormDataPtr;
		private IntPtr bufferProcsPtr;
		private IntPtr handleProcsPtr;
#if USEIMAGESERVICES
		private IntPtr imageServicesProcsPtr;
#endif
		private IntPtr propertyProcsPtr;
		private IntPtr resourceProcsPtr;

		private IntPtr channelPortsPtr;
		private IntPtr readDocumentPtr;

		private IntPtr descriptorParametersPtr;
		private IntPtr readDescriptorPtr;
		private IntPtr writeDescriptorPtr;
		private IntPtr errorStringPtr;

		private IntPtr basicSuitePtr;

		private GlobalParameters globalParameters;
		private Dictionary<uint, AETEValue> scriptingData;
		private bool showUI;
		private bool parameterDataRestored;
		private bool pluginDataRestored;

		private AbortFunc abortFunc;
		private ProgressProc progressFunc;

		private SurfaceBase source;
		private SurfaceBase dest;
		private SurfaceGray8 mask;
		private SurfaceBGRA32 displaySurface;
		private SurfaceGray8 tempMask;
		private SurfaceBase tempSurface;
		private Bitmap checkerBoardBitmap;
		private SurfaceBGRA32 colorCorrectedDisplaySurface;

		private PluginPhase phase;
		private PluginModule module;

		private IntPtr dataPtr;
		private short result;
		private string errorMessage;
		private HostInformation hostInfo;

		private short filterCase;
		private double dpiX;
		private double dpiY;
		private Region selectedRegion;
		private ImageMetaData imageMetaData;

		private ImageModes imageMode;
		private byte[] backgroundColor;
		private byte[] foregroundColor;

		private bool ignoreAlpha;
		private FilterDataHandling inputHandling;
		private FilterDataHandling outputHandling;
		private IntPtr filterParametersHandle;
		private IntPtr pluginDataHandle;

		private Rect16 lastOutRect;
		private int lastOutRowBytes;
		private int lastOutLoPlane;
		private int lastOutHiPlane;
		private Rect16 lastInRect;
		private int lastInLoPlane;
		private Rect16 lastMaskRect;

		private IntPtr maskDataPtr;
		private IntPtr inDataPtr;
		private IntPtr outDataPtr;

		private SurfaceBase scaledChannelSurface;
		private SurfaceBase ditheredChannelSurface;
		private SurfaceGray8 scaledSelectionMask;
		private ImageModes ditheredChannelImageMode;

		private bool disposed;
		private bool sizesSetup;
		private bool frValuesSetup;
		private bool copyToDest;
		private bool writesOutsideSelection;
		private bool useChannelPorts;
		private ActivePICASuites activePICASuites;
		private PICASuites picaSuites;
		private ColorProfileConverter colorProfileConverter;
		private byte[] documentColorProfile;

		private DescriptorSuite descriptorSuite;
		private ErrorSuite errorSuite;
		private PseudoResourceSuite pseudoResourceSuite;
		private ActionDescriptorSuite actionDescriptorSuite;
		private ActionListSuite actionListSuite;
		private ActionReferenceSuite actionReferenceSuite;
		private DescriptorRegistrySuite descriptorRegistrySuite;

		/// <summary>
		/// The host signature of this library - '.NET'
		/// </summary>
		private const uint HostSignature = 0x2e4e4554;

		public SurfaceBase Dest
		{
			get
			{
				return this.dest;
			}
		}

		/// <summary>
		/// Sets the filter progress callback.
		/// </summary>
		/// <param name="value">The progress callback delegate.</param>
		/// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
		internal void SetProgressFunc(ProgressProc value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}

			progressFunc = value;
		}

		/// <summary>
		/// Sets the filter abort callback.
		/// </summary>
		/// <param name="value">The abort callback delegate.</param>
		/// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
		internal void SetAbortFunc(AbortFunc value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}

			abortFunc = value;
		}

		/// <summary>
		/// Sets the filter color picker callback.
		/// </summary>
		/// <param name="value">The color picker callback delegate.</param>
		/// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
		internal void SetPickColor(PickColor value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}

			ColorPickerManager.SetPickColorCallback(value);
		}

		/// <summary>
		/// Sets the color profiles.
		/// </summary>
		/// <param name="colorProfiles">The color profiles.</param>
		/// <exception cref="ArgumentNullException"><paramref name="colorProfiles"/> is null.</exception>
		internal void SetColorProfiles(HostColorManagement colorProfiles)
		{
			if (colorProfiles == null)
			{
				throw new ArgumentNullException("colorProfiles");
			}

			this.colorProfileConverter.Initialize(colorProfiles);
			this.documentColorProfile = colorProfiles.GetDocumentColorProfile();
		}

		/// <summary>
		/// Gets the plug-in settings for the current session.
		/// </summary>
		/// <returns>
		/// The plug-in settings for the current session, or <c>null</c> if the current session does not contain any plug-in settings.
		/// </returns>
		internal PluginSettingsRegistry GetPluginSettings()
		{
			return this.descriptorRegistrySuite.GetPluginSettings();
		}

		/// <summary>
		/// Sets the plug-in settings for the current session.
		/// </summary>
		/// <param name="settings">The plug-in settings.</param>
		/// <exception cref="ArgumentNullException"><paramref name="settings"/> is null.</exception>
		internal void SetPluginSettings(PluginSettingsRegistry settings)
		{
			this.descriptorRegistrySuite.SetPluginSettings(settings);
		}

		public string ErrorMessage
		{
			get
			{
				return this.errorMessage;
			}
		}


		internal ParameterData ParameterData
		{
			get
			{
				return new ParameterData(this.globalParameters, this.scriptingData);
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}

				this.globalParameters = value.GlobalParameters;
				if (value.ScriptingData != null)
				{
					this.scriptingData = value.ScriptingData;
				}
			}
		}

		/// <summary>
		/// Determines whether the filter should show its user interface.
		/// </summary>
		internal bool ShowUI
		{
			set
			{
				this.showUI = value;
			}
		}

		internal PseudoResourceCollection PseudoResources
		{
			get
			{
				return this.pseudoResourceSuite.PseudoResources;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}

				this.pseudoResourceSuite.PseudoResources = value;
			}
		}

		internal HostInformation HostInformation
		{
			get
			{
				return this.hostInfo;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}

				this.hostInfo = value;
			}
		}

		/// <summary>
		/// Loads and runs Adobe(R) Photoshop(R) filters
		/// </summary>
		/// <param name="sourceImage">The file name of the source image.</param>
		/// <param name="primary">The selected primary color.</param>
		/// <param name="secondary">The selected secondary color.</param>
		/// <param name="selection">The <see cref="System.Drawing.Region"/> that gives the shape of the selection.</param>
		/// <param name="owner">The handle of the parent window</param>
		/// <exception cref="System.ArgumentNullException">The sourceImage is null.</exception>
#if GDIPLUS
		internal LoadPsFilter(Bitmap sourceImage, Color primary, Color secondary, Region selection, IntPtr owner)
#else
		internal LoadPsFilter(BitmapSource sourceImage, System.Windows.Media.Color primary, System.Windows.Media.Color secondary, Region selection, IntPtr owner)
#endif
		{
			if (sourceImage == null)
			{
				throw new ArgumentNullException("sourceImage");
			}

			this.dataPtr = IntPtr.Zero;
			this.phase = PluginPhase.None;
			this.disposed = false;
			this.copyToDest = true;
			this.writesOutsideSelection = false;
			this.sizesSetup = false;
			this.frValuesSetup = false;
			this.showUI = true;
			this.parameterDataRestored = false;
			this.pluginDataRestored = false;
			this.globalParameters = new GlobalParameters();
			this.scriptingData = null;
			this.errorMessage = string.Empty;
			this.filterParametersHandle = IntPtr.Zero;
			this.pluginDataHandle = IntPtr.Zero;
			this.inputHandling = FilterDataHandling.None;
			this.outputHandling = FilterDataHandling.None;

			abortFunc = null;
			progressFunc = null;
			ColorPickerManager.SetPickColorCallback(null);
			this.descriptorSuite = new DescriptorSuite();
			this.pseudoResourceSuite = new PseudoResourceSuite();

			this.useChannelPorts = false;
			this.channelReadDescPtrs = new List<ChannelDescPtrs>();
			this.activePICASuites = new ActivePICASuites();
			this.picaSuites = new PICASuites();
			this.hostInfo = new HostInformation();
			this.colorProfileConverter = new ColorProfileConverter();
			this.documentColorProfile = null;
			this.descriptorRegistrySuite = new DescriptorRegistrySuite();

			this.lastInRect = Rect16.Empty;
			this.lastOutRect = Rect16.Empty;
			this.lastMaskRect = Rect16.Empty;
			this.lastInLoPlane = -1;
			this.lastOutRowBytes = 0;
			this.lastOutHiPlane = 0;
			this.lastOutLoPlane = -1;

#if GDIPLUS
			this.source = SurfaceFactory.CreateFromGdipBitmap(sourceImage, out this.imageMode);
#else
			this.source = SurfaceFactory.CreateFromBitmapSource(sourceImage, out this.imageMode);
#endif
			this.dest = SurfaceFactory.CreateFromImageMode(source.Width, source.Height, this.imageMode);

#if GDIPLUS
			this.dpiX = sourceImage.HorizontalResolution;
			this.dpiY = sourceImage.VerticalResolution;
			this.imageMetaData = new ImageMetaData((Bitmap)sourceImage.Clone());
#else
			this.dpiX = sourceImage.DpiX;
			this.dpiY = sourceImage.DpiY;
			this.imageMetaData = new ImageMetaData(sourceImage.Clone());
#endif

			this.selectedRegion = null;
			this.filterCase = FilterCase.EditableTransparencyNoSelection;

			if (selection != null)
			{
				selection.Intersect(source.Bounds);
				Rectangle selectionBounds = selection.GetBoundsInt();

				if (!selectionBounds.IsEmpty && selectionBounds != source.Bounds)
				{
					this.selectedRegion = selection.Clone();
					this.filterCase = FilterCase.EditableTransparencyWithSelection;
				}
			}

			if (imageMode == ImageModes.GrayScale || imageMode == ImageModes.Gray16)
			{
				switch (filterCase)
				{
					case FilterCase.EditableTransparencyNoSelection:
						this.filterCase = FilterCase.FlatImageNoSelection;
						break;
					case FilterCase.EditableTransparencyWithSelection:
						this.filterCase = FilterCase.FlatImageWithSelection;
						break;
				}
			}

			this.foregroundColor = new byte[4] { primary.R, primary.G, primary.B, 0 };
			this.backgroundColor = new byte[4] { secondary.R, secondary.G, secondary.B, 0 };

			unsafe
			{
				this.platFormDataPtr = Memory.Allocate(Marshal.SizeOf(typeof(PlatformData)), true);
				((PlatformData*)platFormDataPtr.ToPointer())->hwnd = owner;
			}

#if DEBUG
			DebugFlags debugFlags = DebugFlags.AdvanceState;
			debugFlags |= DebugFlags.Call;
			debugFlags |= DebugFlags.ColorServices;
			debugFlags |= DebugFlags.DisplayPixels;
			debugFlags |= DebugFlags.DescriptorParameters;
			debugFlags |= DebugFlags.Error;
			debugFlags |= DebugFlags.HandleSuite;
			debugFlags |= DebugFlags.ImageServices;
			debugFlags |= DebugFlags.MiscCallbacks;
			debugFlags |= DebugFlags.PropertySuite;
			debugFlags |= DebugFlags.ResourceSuite;
			debugFlags |= DebugFlags.SPBasicSuite;
			DebugUtils.GlobalDebugFlags = debugFlags;
#endif
		}

		private bool IgnoreAlphaChannel(PluginData data)
		{
			if (filterCase < FilterCase.EditableTransparencyNoSelection)
			{
				return true; // Return true for the FlatImage cases as we do not have any transparency.
			}

			// Some filters do not handle the alpha channel correctly despite what their FilterInfo says.
			if (data.FilterInfo == null || 
				data.Category.Equals("Axion", StringComparison.Ordinal) ||
				data.Category.Equals("Vizros 4", StringComparison.Ordinal) && data.Title.StartsWith("Lake", StringComparison.Ordinal))
			{
				if (source.HasTransparency())
				{
					this.filterCase = FilterCase.FloatingSelection;
				}
				else
				{
					switch (filterCase)
					{
						case FilterCase.EditableTransparencyNoSelection:
							this.filterCase = FilterCase.FlatImageNoSelection;
							break;
						case FilterCase.EditableTransparencyWithSelection:
							this.filterCase = FilterCase.FlatImageWithSelection;
							break;
					}
				}

				return true;
			}

			int filterCaseIndex = this.filterCase - 1;
			FilterCaseInfo[] filterInfo = data.FilterInfo;

			// If the EditableTransparency cases are not supported use the other modes.
			if (filterInfo[filterCaseIndex].inputHandling == FilterDataHandling.CantFilter)
			{
				bool hasTransparency = source.HasTransparency();
				if (!hasTransparency)
				{
					switch (filterCase)
					{
						case FilterCase.EditableTransparencyNoSelection:
							this.filterCase = FilterCase.FlatImageNoSelection;
							break;
						case FilterCase.EditableTransparencyWithSelection:
							this.filterCase = FilterCase.FlatImageWithSelection;
							break;
					}

					return true;
				}
				else if (filterInfo[filterCaseIndex + 2].inputHandling == FilterDataHandling.CantFilter)
				{
					// If the protected transparency modes are not supported use the next most appropriate mode.
					if (hasTransparency && filterInfo[FilterCase.FloatingSelection - 1].inputHandling != FilterDataHandling.CantFilter)
					{
						this.filterCase = FilterCase.FloatingSelection;
					}
					else
					{
						switch (filterCase)
						{
							case FilterCase.EditableTransparencyNoSelection:
								this.filterCase = FilterCase.FlatImageNoSelection;
								break;
							case FilterCase.EditableTransparencyWithSelection:
								this.filterCase = FilterCase.FlatImageWithSelection;
								break;
						}
					}

					return true;
				}
				else
				{
					switch (filterCase)
					{
						case FilterCase.EditableTransparencyNoSelection:
							this.filterCase = FilterCase.ProtectedTransparencyNoSelection;
							break;
						case FilterCase.EditableTransparencyWithSelection:
							this.filterCase = FilterCase.ProtectedTransparencyWithSelection;
							break;
					}

				}

			}

			return false;
		}

		/// <summary>
		/// Determines whether the memory block is marked as executable.
		/// </summary>
		/// <param name="ptr">The pointer to check.</param>
		/// <returns>
		///   <c>true</c> if memory block is marked as executable; otherwise, <c>false</c>.
		/// </returns>
		private static bool IsMemoryExecutable(IntPtr ptr)
		{
			NativeStructs.MEMORY_BASIC_INFORMATION mbi = new NativeStructs.MEMORY_BASIC_INFORMATION();
			int mbiSize = Marshal.SizeOf(typeof(NativeStructs.MEMORY_BASIC_INFORMATION));

			if (SafeNativeMethods.VirtualQuery(ptr, out mbi, new UIntPtr((ulong)mbiSize)) == UIntPtr.Zero)
			{
				return false;
			}

			const int ExecuteProtect = NativeConstants.PAGE_EXECUTE |
									   NativeConstants.PAGE_EXECUTE_READ |
									   NativeConstants.PAGE_EXECUTE_READWRITE |
									   NativeConstants.PAGE_EXECUTE_WRITECOPY;

			return ((mbi.Protect & ExecuteProtect) != 0);
		}

		/// <summary>
		/// Determines whether the specified address is a fake indirect pointer.
		/// </summary>
		/// <param name="address">The address to check.</param>
		/// <param name="baseAddress">The base address of the memory block.</param>
		/// <param name="baseAddressSize">The size of the memory block at the base address.</param>
		/// <param name="size">The size.</param>
		/// <returns><c>true</c> if the address is a fake indirect pointer; otherwise, <c>false</c></returns>
		private static bool IsFakeIndirectPointer(IntPtr address, IntPtr baseAddress, long baseAddressSize, out long size)
		{
			size = 0L;

			bool result = false;

			// Some plug-ins may use an indirect pointer to the same memory block.
			IntPtr fakeIndirectAddress = new IntPtr(baseAddress.ToInt64() + IntPtr.Size);

			if (address == fakeIndirectAddress)
			{
				result = true;
				size = baseAddressSize - IntPtr.Size;
			}

			return result;
		}

		/// <summary>
		/// Loads a filter from the PluginData.
		/// </summary>
		/// <param name="pdata">The PluginData of the filter to load.</param>
		/// <exception cref="System.EntryPointNotFoundException">The entry point specified by the PluginData.EntryPoint property was not found in PluginData.FileName.</exception>
		/// <exception cref="System.IO.FileNotFoundException">The file specified by the PluginData.FileName property cannot be found.</exception>
		private void LoadFilter(PluginData pdata)
		{
			new FileIOPermission(FileIOPermissionAccess.PathDiscovery | FileIOPermissionAccess.Read, pdata.FileName).Demand();

			module = new PluginModule(pdata.FileName, pdata.EntryPoint);
		}

		/// <summary>
		/// Saves the filter scripting parameters for repeat runs.
		/// </summary>
		private unsafe void SaveScriptingParameters()
		{
			PIDescriptorParameters* descriptorParameters = (PIDescriptorParameters*)descriptorParametersPtr.ToPointer();
			if (descriptorParameters->descriptor != IntPtr.Zero)
			{
				Dictionary<uint, AETEValue> data;
				if (actionDescriptorSuite != null && actionDescriptorSuite.TryGetScriptingData(descriptorParameters->descriptor, out data))
				{
					this.scriptingData = data;
				}
				else if (descriptorSuite.TryGetScriptingData(descriptorParameters->descriptor, out data))
				{
					this.scriptingData = data;
				}
				HandleSuite.Instance.UnlockHandle(descriptorParameters->descriptor);
				HandleSuite.Instance.DisposeHandle(descriptorParameters->descriptor);
				descriptorParameters->descriptor = IntPtr.Zero;
			}
		}

		/// <summary>
		/// Save the filter parameter handles for repeat runs.
		/// </summary>
		private unsafe void SaveParameterHandles()
		{
			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			if (filterRecord->parameters != IntPtr.Zero)
			{
				if (HandleSuite.Instance.AllocatedBySuite(filterRecord->parameters))
				{
					int handleSize = HandleSuite.Instance.GetHandleSize(filterRecord->parameters);

					byte[] buf = new byte[handleSize];
					Marshal.Copy(HandleSuite.Instance.LockHandle(filterRecord->parameters, 0), buf, 0, buf.Length);
					HandleSuite.Instance.UnlockHandle(filterRecord->parameters);

					this.globalParameters.SetParameterDataBytes(buf);
					this.globalParameters.ParameterDataStorageMethod = GlobalParameters.DataStorageMethod.HandleSuite;
				}
				else
				{
					long size = SafeNativeMethods.GlobalSize(filterRecord->parameters).ToInt64();

					if (size > 0L)
					{
						IntPtr parameters = SafeNativeMethods.GlobalLock(filterRecord->parameters);

						try
						{
							IntPtr hPtr = Marshal.ReadIntPtr(parameters);

							if (size == OTOFHandleSize && Marshal.ReadInt32(parameters, IntPtr.Size) == OTOFSignature)
							{
								long ps = SafeNativeMethods.GlobalSize(hPtr).ToInt64();
								if (ps > 0L)
								{
									byte[] buf = new byte[(int)ps];
									Marshal.Copy(hPtr, buf, 0, buf.Length);
									this.globalParameters.SetParameterDataBytes(buf);
									this.globalParameters.ParameterDataStorageMethod = GlobalParameters.DataStorageMethod.OTOFHandle;
									// Some filters may store executable code in the parameter block.
									this.globalParameters.ParameterDataExecutable = IsMemoryExecutable(hPtr);
								}

							}
							else
							{
								long pointerSize = SafeNativeMethods.GlobalSize(hPtr).ToInt64();
								if (pointerSize > 0L || IsFakeIndirectPointer(hPtr, parameters, size, out pointerSize))
								{
									byte[] buf = new byte[(int)pointerSize];

									Marshal.Copy(hPtr, buf, 0, buf.Length);
									this.globalParameters.SetParameterDataBytes(buf);
									this.globalParameters.ParameterDataStorageMethod = GlobalParameters.DataStorageMethod.HandleSuite;
								}
								else
								{
									byte[] buf = new byte[(int)size];

									Marshal.Copy(parameters, buf, 0, buf.Length);
									this.globalParameters.SetParameterDataBytes(buf);
									this.globalParameters.ParameterDataStorageMethod = GlobalParameters.DataStorageMethod.RawBytes;
								}

							}
						}
						finally
						{
							SafeNativeMethods.GlobalUnlock(filterRecord->parameters);
						}

					}
				}

			}

			if (dataPtr != IntPtr.Zero)
			{
				long pluginDataSize = 0L;

				if (!HandleSuite.Instance.AllocatedBySuite(dataPtr))
				{
					if (BufferSuite.Instance.AllocatedBySuite(dataPtr))
					{
						pluginDataSize = Memory.Size(dataPtr);
					}
					else
					{
						pluginDataSize = SafeNativeMethods.GlobalSize(dataPtr).ToInt64();
					}
				}

				IntPtr pluginData = SafeNativeMethods.GlobalLock(dataPtr);

				try
				{
					if (HandleSuite.Instance.AllocatedBySuite(pluginData))
					{
						int ps = HandleSuite.Instance.GetHandleSize(pluginData);
						byte[] dataBuf = new byte[ps];

						Marshal.Copy(HandleSuite.Instance.LockHandle(pluginData, 0), dataBuf, 0, dataBuf.Length);
						HandleSuite.Instance.UnlockHandle(pluginData);

						this.globalParameters.SetPluginDataBytes(dataBuf);
						this.globalParameters.PluginDataStorageMethod = GlobalParameters.DataStorageMethod.HandleSuite;
					}
					else if (pluginDataSize == OTOFHandleSize && Marshal.ReadInt32(pluginData, IntPtr.Size) == OTOFSignature)
					{
						IntPtr hPtr = Marshal.ReadIntPtr(pluginData);
						long ps = SafeNativeMethods.GlobalSize(hPtr).ToInt64();
						if (ps > 0L)
						{
							byte[] dataBuf = new byte[(int)ps];
							Marshal.Copy(hPtr, dataBuf, 0, dataBuf.Length);
							this.globalParameters.SetPluginDataBytes(dataBuf);
							this.globalParameters.PluginDataStorageMethod = GlobalParameters.DataStorageMethod.OTOFHandle;
							this.globalParameters.PluginDataExecutable = IsMemoryExecutable(hPtr);
						}

					}
					else if (pluginDataSize > 0)
					{
						byte[] dataBuf = new byte[(int)pluginDataSize];
						Marshal.Copy(pluginData, dataBuf, 0, dataBuf.Length);
						this.globalParameters.SetPluginDataBytes(dataBuf);
						this.globalParameters.PluginDataStorageMethod = GlobalParameters.DataStorageMethod.RawBytes;
					}

				}
				finally
				{
					SafeNativeMethods.GlobalUnlock(pluginData);
				}

			}
		}

		/// <summary>
		/// Restore the filter parameter handles for repeat runs.
		/// </summary>
		private unsafe void RestoreParameterHandles()
		{
			if (phase == PluginPhase.Parameters)
			{
				return;
			}

			byte[] parameterDataBytes = globalParameters.GetParameterDataBytes();
			if (parameterDataBytes != null)
			{
				FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

				switch (globalParameters.ParameterDataStorageMethod)
				{
					case GlobalParameters.DataStorageMethod.HandleSuite:

						filterRecord->parameters = HandleSuite.Instance.NewHandle(parameterDataBytes.Length);
						if (filterRecord->parameters == IntPtr.Zero)
						{
							throw new OutOfMemoryException(Resources.OutOfMemoryError);
						}

						Marshal.Copy(parameterDataBytes, 0, HandleSuite.Instance.LockHandle(filterRecord->parameters, 0), parameterDataBytes.Length);
						HandleSuite.Instance.UnlockHandle(filterRecord->parameters);
						break;
					case GlobalParameters.DataStorageMethod.OTOFHandle:

						filterRecord->parameters = Memory.Allocate(OTOFHandleSize, false);

						if (globalParameters.ParameterDataExecutable)
						{
							filterParametersHandle = Memory.AllocateExecutable(parameterDataBytes.Length);
						}
						else
						{
							filterParametersHandle = Memory.Allocate(parameterDataBytes.Length, false);
						}

						Marshal.Copy(parameterDataBytes, 0, filterParametersHandle, parameterDataBytes.Length);

						Marshal.WriteIntPtr(filterRecord->parameters, filterParametersHandle);
						Marshal.WriteInt32(filterRecord->parameters, IntPtr.Size, OTOFSignature);
						break;
					case GlobalParameters.DataStorageMethod.RawBytes:

						filterRecord->parameters = Memory.Allocate(parameterDataBytes.Length, false);
						Marshal.Copy(parameterDataBytes, 0, filterRecord->parameters, parameterDataBytes.Length);
						break;
					default:
						throw new InvalidEnumArgumentException("ParameterDataStorageMethod", (int)globalParameters.ParameterDataStorageMethod, typeof(GlobalParameters.DataStorageMethod));
				}
				parameterDataRestored = true;
			}

			byte[] pluginDataBytes = globalParameters.GetPluginDataBytes();

			if (pluginDataBytes != null)
			{
				switch (globalParameters.PluginDataStorageMethod)
				{
					case GlobalParameters.DataStorageMethod.HandleSuite:

						dataPtr = HandleSuite.Instance.NewHandle(pluginDataBytes.Length);
						if (dataPtr == IntPtr.Zero)
						{
							throw new OutOfMemoryException(Resources.OutOfMemoryError);
						}

						Marshal.Copy(pluginDataBytes, 0, HandleSuite.Instance.LockHandle(dataPtr, 0), pluginDataBytes.Length);
						HandleSuite.Instance.UnlockHandle(dataPtr);
						break;
					case GlobalParameters.DataStorageMethod.OTOFHandle:

						dataPtr = Memory.Allocate(OTOFHandleSize, false);

						if (globalParameters.PluginDataExecutable)
						{
							pluginDataHandle = Memory.AllocateExecutable(pluginDataBytes.Length);
						}
						else
						{
							pluginDataHandle = Memory.Allocate(pluginDataBytes.Length, false);
						}

						Marshal.Copy(pluginDataBytes, 0, pluginDataHandle, pluginDataBytes.Length);

						Marshal.WriteIntPtr(dataPtr, pluginDataHandle);
						Marshal.WriteInt32(dataPtr, IntPtr.Size, OTOFSignature);
						break;
					case GlobalParameters.DataStorageMethod.RawBytes:

						dataPtr = Memory.Allocate(pluginDataBytes.Length, false);
						Marshal.Copy(pluginDataBytes, 0, dataPtr, pluginDataBytes.Length);
						break;
					default:
						throw new InvalidEnumArgumentException("PluginDataStorageMethod", (int)globalParameters.PluginDataStorageMethod, typeof(GlobalParameters.DataStorageMethod));
				}
				pluginDataRestored = true;
			}

		}

		private static bool PluginAbout(PluginData pdata, IntPtr owner, out string error)
		{
			short result = PSError.noErr;
			error = string.Empty;

			using (PluginModule module = new PluginModule(pdata.FileName, pdata.EntryPoint))
			{
				PlatformData platform = new PlatformData()
				{
					hwnd = owner
				};

				GCHandle platformDataHandle = GCHandle.Alloc(platform, GCHandleType.Pinned);
				try
				{
					AboutRecord about = new AboutRecord()
					{
						platformData = platformDataHandle.AddrOfPinnedObject(),
						sSPBasic = IntPtr.Zero,
						plugInRef = IntPtr.Zero
					};


					GCHandle aboutRecordHandle = GCHandle.Alloc(about, GCHandleType.Pinned);

					try
					{
						IntPtr dataPtr = IntPtr.Zero;

						// If the filter only has one entry point call about on it.
						if (pdata.ModuleEntryPoints == null)
						{
							module.entryPoint(FilterSelector.About, aboutRecordHandle.AddrOfPinnedObject(), ref dataPtr, ref result);
						}
						else
						{
							// Otherwise call about on all the entry points in the module, per the SDK docs only one of the entry points will display the about box.
							foreach (var entryPoint in pdata.ModuleEntryPoints)
							{
								PluginEntryPoint ep = module.GetEntryPoint(entryPoint);

								ep(FilterSelector.About, aboutRecordHandle.AddrOfPinnedObject(), ref dataPtr, ref result);

								if (result != PSError.noErr)
								{
									break;
								}

								GC.KeepAlive(ep);
							}
						}
					}
					finally
					{
						if (aboutRecordHandle.IsAllocated)
						{
							aboutRecordHandle.Free();
						}
					}
				}
				finally
				{
					if (platformDataHandle.IsAllocated)
					{
						platformDataHandle.Free();
					}
				}
			}


			if (result != PSError.noErr)
			{
				if (result < 0 && result != PSError.userCanceledErr)
				{
					switch (result)
					{
						case PSError.errPlugInHostInsufficient:
							error = Resources.PlugInHostInsufficient;
							break;
						default:
							error = GetMacOSErrorMessage(result);
							break;
					}
				}
#if DEBUG
				DebugUtils.Ping(DebugFlags.Error, string.Format("filterSelectorAbout returned: {0}({1})", error, result));
#endif
				return false;
			}

			return true;
		}

		private unsafe bool PluginApply()
		{
#if DEBUG
			System.Diagnostics.Debug.Assert(phase == PluginPhase.Prepare);
#endif
			result = PSError.noErr;

#if DEBUG
			DebugUtils.Ping(DebugFlags.Call, "Before FilterSelectorStart");
#endif

			module.entryPoint(FilterSelector.Start, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
			DebugUtils.Ping(DebugFlags.Call, "After FilterSelectorStart");
#endif

			if (result != PSError.noErr)
			{
				errorMessage = GetErrorMessage(result);

#if DEBUG
				DebugUtils.Ping(DebugFlags.Error, string.Format("filterSelectorStart returned: {0}({1})", string.IsNullOrEmpty(errorMessage) ? "User Canceled" : errorMessage, result));
#endif
				return false;
			}

			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			while (RectNonEmpty(filterRecord->inRect) || RectNonEmpty(filterRecord->outRect) || RectNonEmpty(filterRecord->maskRect))
			{
				AdvanceStateProc();
				result = PSError.noErr;

#if DEBUG
				DebugUtils.Ping(DebugFlags.Call, "Before FilterSelectorContinue");
#endif

				module.entryPoint(FilterSelector.Continue, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
				DebugUtils.Ping(DebugFlags.Call, "After FilterSelectorContinue");
#endif

				filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

				if (result != PSError.noErr)
				{
					short savedResult = result;
					result = PSError.noErr;

#if DEBUG
					DebugUtils.Ping(DebugFlags.Call, "Before FilterSelectorFinish");
#endif

					module.entryPoint(FilterSelector.Finish, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
					DebugUtils.Ping(DebugFlags.Call, "After FilterSelectorFinish");
#endif

					errorMessage = GetErrorMessage(savedResult);

#if DEBUG
					DebugUtils.Ping(DebugFlags.Error, string.Format("filterSelectorContinue returned: {0}({1})", string.IsNullOrEmpty(errorMessage) ? "User Canceled" : errorMessage, savedResult));
#endif
					return false;
				}

				// Per the SDK the host can call filterSelectorFinish in between filterSelectorContinue calls if it detects a cancel request.
				if (AbortProc())
				{
					result = PSError.noErr;
					module.entryPoint(FilterSelector.Finish, filterRecordPtr, ref dataPtr, ref result);

					if (result != PSError.noErr)
					{
						errorMessage = GetErrorMessage(result);
					}

					return false;
				}
			}
			AdvanceStateProc();


			result = PSError.noErr;

#if DEBUG
			DebugUtils.Ping(DebugFlags.Call, "Before FilterSelectorFinish");
#endif

			module.entryPoint(FilterSelector.Finish, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
			DebugUtils.Ping(DebugFlags.Call, "After FilterSelectorFinish");
#endif

			if (showUI && result == PSError.noErr)
			{
				SaveParameterHandles();
				SaveScriptingParameters();
			}

			PostProcessOutputData();

			filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			if (filterRecord->autoMask != 0)
			{
				ClipToSelection(); // Clip the rendered image to the selection if the filter does not do it for us.
			}

			return true;
		}

		private bool PluginParameters()
		{
			result = PSError.noErr;

			// Photoshop sets the size info before the filterSelectorParameters call even though the documentation says it does not.
			SetupSizes();
			SetFilterRecordValues();
			RestoreParameterHandles();
#if DEBUG
			DebugUtils.Ping(DebugFlags.Call, "Before filterSelectorParameters");
#endif

			module.entryPoint(FilterSelector.Parameters, filterRecordPtr, ref dataPtr, ref result);
#if DEBUG
			unsafe
			{
				FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

				DebugUtils.Ping(DebugFlags.Call, string.Format("data: 0x{0},  parameters: 0x{1}", dataPtr.ToHexString(), filterRecord->parameters.ToHexString()));
			}

			DebugUtils.Ping(DebugFlags.Call, "After filterSelectorParameters");
#endif

			if (result != PSError.noErr)
			{
				errorMessage = GetErrorMessage(result);
#if DEBUG
				DebugUtils.Ping(DebugFlags.Error, string.Format("filterSelectorParameters returned: {0}({1})", string.IsNullOrEmpty(errorMessage) ? "User Canceled" : errorMessage, result));
#endif
				return false;
			}

			phase = PluginPhase.Parameters;

			return true;
		}

		private unsafe void SetFilterRecordValues()
		{
			if (frValuesSetup)
			{
				return;
			}

			frValuesSetup = true;

			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			filterRecord->inRect = Rect16.Empty;
			filterRecord->inData = IntPtr.Zero;
			filterRecord->inRowBytes = 0;

			filterRecord->outRect = Rect16.Empty;
			filterRecord->outData = IntPtr.Zero;
			filterRecord->outRowBytes = 0;

			if (filterCase == FilterCase.FloatingSelection)
			{
				DrawFloatingSelectionMask();
				filterRecord->isFloating = 1;
				filterRecord->haveMask = 1;
				filterRecord->autoMask = 0;
			}
			else if (selectedRegion != null)
			{
				DrawMask();
				filterRecord->isFloating = 0;
				filterRecord->haveMask = 1;
				filterRecord->autoMask = writesOutsideSelection ? (byte)0 : (byte)1;
			}
			else
			{
				filterRecord->isFloating = 0;
				filterRecord->haveMask = 0;
				filterRecord->autoMask = 0;
			}
			filterRecord->maskRect = Rect16.Empty;
			filterRecord->maskData = IntPtr.Zero;
			filterRecord->maskRowBytes = 0;

			filterRecord->imageMode = imageMode;

			if (imageMode == ImageModes.GrayScale || imageMode == ImageModes.Gray16)
			{
				filterRecord->inLayerPlanes = 0;
				filterRecord->inTransparencyMask = 0;
				filterRecord->inNonLayerPlanes = 1;

				filterRecord->inColumnBytes = imageMode == ImageModes.Gray16 ? 2 : 1;

				filterRecord->outLayerPlanes = filterRecord->inLayerPlanes;
				filterRecord->outTransparencyMask = filterRecord->inTransparencyMask;
				filterRecord->outNonLayerPlanes = filterRecord->inNonLayerPlanes;
				filterRecord->outColumnBytes = filterRecord->inColumnBytes;
			}
			else
			{
				if (ignoreAlpha)
				{
					filterRecord->inLayerPlanes = 0;
					filterRecord->inTransparencyMask = 0;
					filterRecord->inNonLayerPlanes = 3;
				}
				else
				{
					filterRecord->inLayerPlanes = 3;
					filterRecord->inTransparencyMask = 1;
					filterRecord->inNonLayerPlanes = 0;
				}

				if (imageMode == ImageModes.RGB48)
				{
					filterRecord->inColumnBytes = ignoreAlpha ? 6 : 8;
				}
				else
				{
					filterRecord->inColumnBytes = ignoreAlpha ? 3 : 4;
				}

				if (filterCase == FilterCase.ProtectedTransparencyNoSelection ||
					filterCase == FilterCase.ProtectedTransparencyWithSelection)
				{
					filterRecord->planes = 3;
					filterRecord->outLayerPlanes = 0;
					filterRecord->outTransparencyMask = 0;
					filterRecord->outNonLayerPlanes = 3;
					filterRecord->outColumnBytes = imageMode == ImageModes.RGB48 ? 6 : 3;

					CopySourceAlpha();
				}
				else
				{
					filterRecord->outLayerPlanes = filterRecord->inLayerPlanes;
					filterRecord->outTransparencyMask = filterRecord->inTransparencyMask;
					filterRecord->outNonLayerPlanes = filterRecord->inNonLayerPlanes;
					filterRecord->outColumnBytes = filterRecord->inColumnBytes;
				}
			}

			filterRecord->inLayerMasks = 0;
			filterRecord->inInvertedLayerMasks = 0;


			filterRecord->outInvertedLayerMasks = filterRecord->inInvertedLayerMasks;
			filterRecord->outLayerMasks = filterRecord->inLayerMasks;

			filterRecord->absLayerPlanes = filterRecord->inLayerPlanes;
			filterRecord->absTransparencyMask = filterRecord->inTransparencyMask;
			filterRecord->absLayerMasks = filterRecord->inLayerMasks;
			filterRecord->absInvertedLayerMasks = filterRecord->inInvertedLayerMasks;

			if (ignoreAlpha && (imageMode == ImageModes.RGB || imageMode == ImageModes.RGB48))
			{
				filterRecord->absNonLayerPlanes = 4;
			}
			else
			{
				filterRecord->absNonLayerPlanes = filterRecord->inNonLayerPlanes;
			}

			filterRecord->inPreDummyPlanes = 0;
			filterRecord->inPostDummyPlanes = 0;
			filterRecord->outPreDummyPlanes = 0;
			filterRecord->outPostDummyPlanes = 0;

			if (imageMode == ImageModes.RGB48 || imageMode == ImageModes.Gray16)
			{
				filterRecord->inPlaneBytes = 2;
				filterRecord->outPlaneBytes = 2;
			}
			else
			{
				filterRecord->inPlaneBytes = 1;
				filterRecord->outPlaneBytes = 1;
			}

		}

		private bool PluginPrepare()
		{
			SetupSizes();
			RestoreParameterHandles();
			SetFilterRecordValues();


			result = PSError.noErr;


#if DEBUG
			DebugUtils.Ping(DebugFlags.Call, "Before filterSelectorPrepare");
#endif
			module.entryPoint(FilterSelector.Prepare, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
			DebugUtils.Ping(DebugFlags.Call, "After filterSelectorPrepare");
#endif

			if (result != PSError.noErr)
			{
				errorMessage = GetErrorMessage(result);
#if DEBUG
				DebugUtils.Ping(DebugFlags.Error, string.Format("filterSelectorPrepare returned: {0}({1})", string.IsNullOrEmpty(errorMessage) ? "User Canceled" : errorMessage, result));
#endif
				return false;
			}

#if DEBUG
			phase = PluginPhase.Prepare;
#endif

			return true;
		}

		/// <summary>
		/// Sets the dest alpha to the source alpha when the filter ignores the alpha channel.
		/// </summary>
		private unsafe void CopySourceAlpha()
		{
			if (!copyToDest && (imageMode == ImageModes.RGB || imageMode == ImageModes.RGB48))
			{
				int width = dest.Width;
				int height = dest.Height;

				if (imageMode == ImageModes.RGB48)
				{
					for (int y = 0; y < height; y++)
					{
						ushort* src = (ushort*)source.GetRowAddressUnchecked(y);
						ushort* dst = (ushort*)dest.GetRowAddressUnchecked(y);

						for (int x = 0; x < width; x++)
						{
							dst[3] = src[3];

							src += 4;
							dst += 4;
						}
					}
				}
				else
				{
					for (int y = 0; y < height; y++)
					{
						byte* src = source.GetRowAddressUnchecked(y);
						byte* dst = dest.GetRowAddressUnchecked(y);

						for (int x = 0; x < width; x++)
						{
							dst[3] = src[3];

							src += 4;
							dst += 4;
						}
					}
				}

#if DEBUG
				using (Bitmap dst = dest.CreateAliasedBitmap())
				{

				}
#endif
			}
		}

		/// <summary>
		/// Clips the output image to the selection.
		/// </summary>
		private unsafe void ClipToSelection()
		{
			if ((mask != null) && !writesOutsideSelection)
			{
				int width = source.Width;
				int height = source.Height;
				int channelCount = dest.ChannelCount;

				if (dest.BitsPerChannel == 16)
				{
					for (int y = 0; y < height; y++)
					{
						ushort* src = (ushort*)source.GetRowAddressUnchecked(y);
						ushort* dst = (ushort*)dest.GetRowAddressUnchecked(y);
						byte* maskByte = mask.GetRowAddressUnchecked(y);
						for (int x = 0; x < width; x++)
						{
							// Copy the source to the destination in areas outside the selection.
							if (*maskByte == 0)
							{
								switch (channelCount)
								{
									case 1:
										*dst = *src;
										break;
									case 4:
										dst[0] = src[0];
										dst[1] = src[1];
										dst[2] = src[2];
										dst[3] = src[3];
										break;
								}
							}

							src += channelCount;
							dst += channelCount;
							maskByte++;
						}
					}
				}
				else
				{
					for (int y = 0; y < height; y++)
					{
						byte* src = source.GetRowAddressUnchecked(y);
						byte* dst = dest.GetRowAddressUnchecked(y);
						byte* maskByte = mask.GetRowAddressUnchecked(y);

						for (int x = 0; x < width; x++)
						{
							if (*maskByte == 0)
							{
								switch (channelCount)
								{
									case 1:
										*dst = *src;
										break;
									case 4:
										dst[0] = src[0];
										dst[1] = src[1];
										dst[2] = src[2];
										dst[3] = src[3];
										break;
								}
							}

							src += channelCount;
							dst += channelCount;
							maskByte++;
						}
					}
				}

			}

		}

		/// <summary>
		/// Determines whether the source surface is completely transparent.
		/// </summary>
		/// <returns>
		///   <c>true</c> if the source surface is completely transparent; otherwise, <c>false</c>.
		/// </returns>
		private unsafe bool IsBlankImage()
		{
			int height = source.Height;
			int width = source.Width;

			if (source.BitsPerChannel == 16)
			{
				for (int y = 0; y < height; y++)
				{
					ushort* ptr = (ushort*)source.GetRowAddressUnchecked(y);
					ushort* endPtr = ptr + width;

					while (ptr < endPtr)
					{
						if (ptr[3] > 0)
						{
							return false;
						}
						ptr += 4;
					}
				}
			}
			else
			{
				for (int y = 0; y < height; y++)
				{
					byte* ptr = source.GetRowAddressUnchecked(y);
					byte* endPtr = ptr + width;

					while (ptr < endPtr)
					{
						if (ptr[3] > 0)
						{
							return false;
						}
						ptr += 4;
					}
				}
			}

			return true;
		}

		private static bool EnableChannelPorts(PluginData data)
		{
			// Enable the channel ports suite for Luce 2.
			return data.Category.Equals("Amico Perry", StringComparison.Ordinal);
		}

		/// <summary>
		/// Runs a filter from the specified PluginData
		/// </summary>
		/// <param name="pdata">The PluginData to run</param>
		/// <returns>True if successful otherwise false</returns>
		internal bool RunPlugin(PluginData pdata)
		{
			LoadFilter(pdata);

			this.useChannelPorts = EnableChannelPorts(pdata);
			this.picaSuites.SetPluginName(pdata.Title.TrimEnd('.'));

			this.ignoreAlpha = IgnoreAlphaChannel(pdata);

			if (pdata.FilterInfo != null)
			{
				FilterCaseInfo info = pdata.FilterInfo[this.filterCase - 1];
				this.inputHandling = info.inputHandling;
				this.outputHandling = info.outputHandling;

				FilterCaseInfoFlags filterCaseFlags = info.flags1;

				this.copyToDest = ((filterCaseFlags & FilterCaseInfoFlags.DontCopyToDestination) == FilterCaseInfoFlags.None);
				this.writesOutsideSelection = ((filterCaseFlags & FilterCaseInfoFlags.WritesOutsideSelection) != FilterCaseInfoFlags.None);

				bool worksWithBlankData = ((filterCaseFlags & FilterCaseInfoFlags.WorksWithBlankData) != FilterCaseInfoFlags.None);

				if ((filterCase == FilterCase.EditableTransparencyNoSelection || filterCase == FilterCase.EditableTransparencyWithSelection) && !worksWithBlankData)
				{
					// If the filter does not support processing completely transparent (blank) images return an error message.
					if (IsBlankImage())
					{
						errorMessage = Resources.BlankDataNotSupported;
						return false;
					}
				}
			}

			if (copyToDest)
			{
				dest.CopySurface(source); // Copy the source image to the dest image if the filter does not write to all the pixels.
			}

			if (ignoreAlpha)
			{
				CopySourceAlpha();
			}

			this.descriptorSuite.Aete = pdata.Aete;

			SetupDelegates();
			SetupSuites();
			SetupFilterRecord();

			PreProcessInputData();

			if (showUI)
			{
				if (!PluginParameters())
				{
#if DEBUG
					DebugUtils.Ping(DebugFlags.Error, "PluginParameters failed");
#endif
					return false;
				}
			}

			if (!PluginPrepare())
			{
#if DEBUG
				DebugUtils.Ping(DebugFlags.Error, "PluginPrepare failed");
#endif
				return false;
			}

			if (!PluginApply())
			{
#if DEBUG
				DebugUtils.Ping(DebugFlags.Error, "PluginApply failed");
#endif
				return false;
			}

			return true;
		}

		internal static bool ShowAboutDialog(PluginData pdata, IntPtr owner, out string errorMessage)
		{
			return PluginAbout(pdata, owner, out errorMessage);
		}

		private static string GetImageModeString(ImageModes mode)
		{
			string imageMode = string.Empty;
			switch (mode)
			{
				case ImageModes.RGB:
					imageMode = Resources.RGBMode;
					break;
				case ImageModes.RGB48:
					imageMode = Resources.RGB48Mode;
					break;
				case ImageModes.GrayScale:
					imageMode = Resources.GrayScaleMode;
					break;
				case ImageModes.Gray16:
					imageMode = Resources.Gray16Mode;
					break;
				default:
					imageMode = mode.ToString("G");
					break;
			}

			return imageMode;
		}

		private static string GetMacOSErrorMessage(short error)
		{
			string message;

			switch (error)
			{
				case PSError.readErr:
				case PSError.writErr:
				case PSError.openErr:
				case PSError.ioErr:
					message = Resources.FileIOError;
					break;
				case PSError.eofErr:
					message = Resources.EndOfFileError;
					break;
				case PSError.dskFulErr:
					message = Resources.DiskFullError;
					break;
				case PSError.fLckdErr:
					message = Resources.FileLockedError;
					break;
				case PSError.vLckdErr:
					message = Resources.VolumeLockedError;
					break;
				case PSError.fnfErr:
					message = Resources.FileNotFoundError;
					break;
				case PSError.memFullErr:
				case PSError.nilHandleErr:
				case PSError.memWZErr:
					message = Resources.OutOfMemoryError;
					break;
				case PSError.paramErr:
				default:
					message = Resources.FilterBadParameters;
					break;
			}

			return message;
		}

		private string GetErrorMessage(short error)
		{
			string message = string.Empty;

			// Any positive integer is a plug-in handled error message.
			if (error < 0 && error != PSError.userCanceledErr)
			{
				if (error == PSError.errReportString)
				{
					if (errorSuite != null && errorSuite.HasErrorMessage)
					{
						message = this.errorSuite.ErrorMessage;
					}
					else
					{
						message = StringUtil.FromPascalString(this.errorStringPtr, string.Empty);
					}
				}
				else
				{
					switch (error)
					{
						case PSError.filterBadMode:
							message = string.Format(CultureInfo.CurrentCulture, Resources.FilterBadModeFormat, GetImageModeString(this.imageMode));
							break;
						case PSError.filterBadParameters:
							message = Resources.FilterBadParameters;
							break;
						case PSError.errPlugInPropertyUndefined:
							message = Resources.PlugInPropertyUndefined;
							break;
						case PSError.errHostDoesNotSupportColStep:
							message = Resources.HostDoesNotSupportColStep;
							break;
						case PSError.errInvalidSamplePoint:
							message = Resources.InvalidSamplePoint;
							break;
						case PSError.errPlugInHostInsufficient:
						case PSError.errUnknownPort:
						case PSError.errUnsupportedBitOffset:
						case PSError.errUnsupportedColBits:
						case PSError.errUnsupportedDepth:
						case PSError.errUnsupportedDepthConversion:
						case PSError.errUnsupportedRowBits:
							message = Resources.PlugInHostInsufficient;
							break;
						default:
							message = GetMacOSErrorMessage(error);
							break;
					} 
				}
			}

			return message;
		}

		private bool AbortProc()
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.MiscCallbacks, string.Empty);
#endif
			if (abortFunc != null)
			{
				return abortFunc();
			}

			return false;
		}

		/// <summary>
		/// Determines whether the filter uses planar order processing.
		/// </summary>
		/// <param name="loPlane">The lo plane.</param>
		/// <param name="hiPlane">The hi plane.</param>
		/// <returns>
		///   <c>true</c> if a single plane of data is requested; otherwise, <c>false</c>.
		/// </returns>
		private static bool IsSinglePlane(short loPlane, short hiPlane)
		{
			return (((hiPlane - loPlane) + 1) == 1);
		}

		/// <summary>
		/// Determines whether the data buffer needs to be resized.
		/// </summary>
		/// <param name="data">The buffer to check.</param>
		/// <param name="rect">The new source rectangle.</param>
		/// <param name="loplane">The loplane.</param>
		/// <param name="hiplane">The hiplane.</param>
		/// <returns>
		///   <c>true</c> if a the buffer needs to be resized; otherwise, <c>false</c>.
		/// </returns>
		private static bool ResizeBuffer(IntPtr data, Rect16 rect, int loplane, int hiplane)
		{
			long size = Memory.Size(data);

			int width = rect.right - rect.left;
			int height = rect.bottom - rect.top;
			int nplanes = hiplane - loplane + 1;

			long bufferSize = ((width * nplanes) * height);

			return (bufferSize != size);
		}

		private unsafe short AdvanceStateProc()
		{
			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			if (outDataPtr != IntPtr.Zero && RectNonEmpty(lastOutRect))
			{
				StoreOutputBuffer(outDataPtr, lastOutRowBytes, lastOutRect, lastOutLoPlane, lastOutHiPlane);
			}

#if DEBUG
			DebugUtils.Ping(DebugFlags.AdvanceState, string.Format("inRect: {0}, outRect: {1}, maskRect: {2}", filterRecord->inRect, filterRecord->outRect, filterRecord->maskRect));
#endif
			short error;

			if (filterRecord->haveMask == 1 && RectNonEmpty(filterRecord->maskRect))
			{
				if (!lastMaskRect.Equals(filterRecord->maskRect))
				{
					if (maskDataPtr != IntPtr.Zero && ResizeBuffer(maskDataPtr, filterRecord->maskRect, 0, 0))
					{
						Memory.Free(maskDataPtr);
						maskDataPtr = IntPtr.Zero;
						filterRecord->maskData = IntPtr.Zero;
					}

					error = FillMaskBuffer(filterRecord);

					if (error != PSError.noErr)
					{
						return error;
					}

					lastMaskRect = filterRecord->maskRect;
				}
			}
			else
			{
				if (maskDataPtr != IntPtr.Zero)
				{
					Memory.Free(maskDataPtr);
					maskDataPtr = IntPtr.Zero;
					filterRecord->maskData = IntPtr.Zero;
				}
				filterRecord->maskRowBytes = 0;
				lastMaskRect = Rect16.Empty;
			}


			if (RectNonEmpty(filterRecord->inRect))
			{
				if (!lastInRect.Equals(filterRecord->inRect) || (IsSinglePlane(filterRecord->inLoPlane, filterRecord->inHiPlane) && lastInLoPlane != filterRecord->inLoPlane))
				{
					if (inDataPtr != IntPtr.Zero && ResizeBuffer(inDataPtr, filterRecord->inRect, filterRecord->inLoPlane, filterRecord->inHiPlane))
					{
						Memory.Free(inDataPtr);
						inDataPtr = IntPtr.Zero;
						filterRecord->inData = IntPtr.Zero;
					}

					error = FillInputBuffer(filterRecord);
					if (error != PSError.noErr)
					{
						return error;
					}

					lastInRect = filterRecord->inRect;
					lastInLoPlane = filterRecord->inLoPlane;
				}
			}
			else
			{
				if (filterRecord->inData != IntPtr.Zero)
				{
					Memory.Free(inDataPtr);
					inDataPtr = IntPtr.Zero;
					filterRecord->inData = IntPtr.Zero;
				}
				filterRecord->inRowBytes = 0;
				lastInRect = Rect16.Empty;
				lastInLoPlane = -1;
			}

			if (RectNonEmpty(filterRecord->outRect))
			{
				if (!lastOutRect.Equals(filterRecord->outRect) || (IsSinglePlane(filterRecord->outLoPlane, filterRecord->outHiPlane) && lastOutLoPlane != filterRecord->outLoPlane))
				{
					if (outDataPtr != IntPtr.Zero && ResizeBuffer(outDataPtr, filterRecord->outRect, filterRecord->outLoPlane, filterRecord->outHiPlane))
					{
						Memory.Free(outDataPtr);
						outDataPtr = IntPtr.Zero;
						filterRecord->outData = IntPtr.Zero;
					}

					error = FillOutputBuffer(filterRecord);
					if (error != PSError.noErr)
					{
						return error;
					}

					// store previous values
					lastOutRowBytes = filterRecord->outRowBytes;
					lastOutRect = filterRecord->outRect;
					lastOutLoPlane = filterRecord->outLoPlane;
					lastOutHiPlane = filterRecord->outHiPlane;
				}

			}
			else
			{
				if (filterRecord->outData != IntPtr.Zero)
				{
					Memory.Free(outDataPtr);
					outDataPtr = IntPtr.Zero;
					filterRecord->outData = IntPtr.Zero;
				}
				filterRecord->outRowBytes = 0;
				lastOutRowBytes = 0;
				lastOutRect = Rect16.Empty;
				lastOutLoPlane = -1;
				lastOutHiPlane = 0;
			}

			return PSError.noErr;
		}

		/// <summary>
		/// Scales the temp surface.
		/// </summary>
		/// <param name="inputRate">The FilterRecord.inputRate to use to scale the image.</param>
		/// <param name="lockRect">The rectangle to clamp the size to.</param>
		private unsafe void ScaleTempSurface(int inputRate, Rectangle lockRect)
		{
			// If the scale rectangle bounds are not valid return a copy of the original surface.
			if (lockRect.X >= source.Width || lockRect.Y >= source.Height)
			{
				if ((tempSurface == null) || tempSurface.Width != source.Width || tempSurface.Height != source.Height)
				{
					if (tempSurface != null)
					{
						tempSurface.Dispose();
						tempSurface = null;
					}

					tempSurface = SurfaceFactory.CreateFromImageMode(source.Width, source.Height, imageMode);
					tempSurface.CopySurface(source);
				}
				return;
			}

			int scaleFactor = FixedToInt32(inputRate);
			if (scaleFactor == 0)
			{
				scaleFactor = 1;
			}

			int scaleWidth = source.Width / scaleFactor;
			int scaleHeight = source.Height / scaleFactor;

			if (lockRect.Width > scaleWidth)
			{
				scaleWidth = lockRect.Width;
			}

			if (lockRect.Height > scaleHeight)
			{
				scaleHeight = lockRect.Height;
			}

			if ((tempSurface == null) || scaleWidth != tempSurface.Width || scaleHeight != tempSurface.Height)
			{
				if (tempSurface != null)
				{
					tempSurface.Dispose();
					tempSurface = null;
				}

				if (scaleFactor > 1) // Filter preview
				{
					tempSurface = SurfaceFactory.CreateFromImageMode(scaleWidth, scaleHeight, imageMode);
					tempSurface.SuperSampleFitSurface(source);
				}
				else
				{
					tempSurface = SurfaceFactory.CreateFromImageMode(source.Width, source.Height, imageMode);
					tempSurface.CopySurface(source);
				}
			}
		}


		/// <summary>
		/// Fills the input buffer with data from the source image.
		/// </summary>
		/// <param name="filterRecord">The filter record.</param>
		private unsafe short FillInputBuffer(FilterRecord* filterRecord)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.AdvanceState, string.Format("inRowBytes: {0}, Rect: {1}, loplane: {2}, hiplane: {3}, inputRate: {4}", new object[] { filterRecord->inRowBytes, filterRecord->inRect,
			filterRecord->inLoPlane, filterRecord->inHiPlane, FixedToInt32(filterRecord->inputRate) }));
#endif
			Rect16 inRect = filterRecord->inRect;

			int nplanes = filterRecord->inHiPlane - filterRecord->inLoPlane + 1;
			int width = inRect.right - inRect.left;
			int height = inRect.bottom - inRect.top;

			Rectangle lockRect = Rectangle.FromLTRB(inRect.left, inRect.top, inRect.right, inRect.bottom);

			int stride = width * nplanes;

			if (inDataPtr == IntPtr.Zero)
			{
				int len = stride * height;

				if (imageMode == ImageModes.RGB48 || imageMode == ImageModes.Gray16)
				{
					len *= 2; // 2 bytes per plane
				}

				try
				{
					inDataPtr = Memory.Allocate(len, false);
				}
				catch (OutOfMemoryException)
				{
					return PSError.memFullErr;
				}
			}
			filterRecord->inData = inDataPtr;
			filterRecord->inRowBytes = stride;
			filterRecord->inColumnBytes = nplanes;

			if (lockRect.Left < 0 || lockRect.Top < 0)
			{
				if (lockRect.Left < 0)
				{
					lockRect.X = 0;
					lockRect.Width -= -inRect.left;
				}

				if (lockRect.Top < 0)
				{
					lockRect.Y = 0;
					lockRect.Height -= -inRect.top;
				}
			}

			try
			{
				ScaleTempSurface(filterRecord->inputRate, lockRect);
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}


			short channelOffset = filterRecord->inLoPlane;
			if (imageMode == ImageModes.RGB || imageMode == ImageModes.RGB48)
			{
				switch (filterRecord->inLoPlane) // Photoshop uses RGBA pixel order so map the Red and Blue channels to BGRA order
				{
					case 0:
						channelOffset = 2;
						break;
					case 2:
						channelOffset = 0;
						break;
				}
			}

			bool validImageBounds = (inRect.left < source.Width && inRect.top < source.Height);
			short padErr = SetFilterPadding(inDataPtr, stride, inRect, nplanes, channelOffset, filterRecord->inputPadding, lockRect, tempSurface);
			if (padErr != PSError.noErr || !validImageBounds)
			{
				return padErr;
			}
			void* ptr = inDataPtr.ToPointer();
			int top = lockRect.Top;
			int left = lockRect.Left;
			int bottom = Math.Min(lockRect.Bottom, tempSurface.Height);
			int right = Math.Min(lockRect.Right, tempSurface.Width);


			switch (imageMode)
			{
				case ImageModes.GrayScale:

					for (int y = top; y < bottom; y++)
					{
						byte* src = tempSurface.GetPointAddressUnchecked(left, y);
						byte* dst = (byte*)ptr + ((y - top) * stride);

						for (int x = left; x < right; x++)
						{
							*dst = *src;

							src++;
							dst++;
						}
					}

					break;
				case ImageModes.Gray16:

					for (int y = top; y < bottom; y++)
					{
						ushort* src = (ushort*)tempSurface.GetPointAddressUnchecked(left, y);
						ushort* dst = (ushort*)ptr + ((y - top) * stride);

						for (int x = left; x < right; x++)
						{
							*dst = *src;

							src++;
							dst++;
						}
					}

					break;
				case ImageModes.RGB:

					for (int y = top; y < bottom; y++)
					{
						byte* src = tempSurface.GetPointAddressUnchecked(left, y);
						byte* dst = (byte*)ptr + ((y - top) * stride);

						for (int x = left; x < right; x++)
						{
							switch (nplanes)
							{
								case 1:
									*dst = src[channelOffset];
									break;
								case 2:
									dst[0] = src[channelOffset];
									dst[1] = src[channelOffset + 1];
									break;
								case 3:
									dst[0] = src[2];
									dst[1] = src[1];
									dst[2] = src[0];
									break;
								case 4:
									dst[0] = src[2];
									dst[1] = src[1];
									dst[2] = src[0];
									dst[3] = src[3];
									break;

							}

							src += 4;
							dst += nplanes;
						}
					}

					break;
				case ImageModes.RGB48:

					for (int y = top; y < bottom; y++)
					{
						ushort* src = (ushort*)tempSurface.GetPointAddressUnchecked(left, y);
						ushort* dst = (ushort*)ptr + ((y - top) * stride);

						for (int x = left; x < right; x++)
						{
							switch (nplanes)
							{
								case 1:
									*dst = src[channelOffset];
									break;
								case 2:
									dst[0] = src[channelOffset];
									dst[1] = src[channelOffset + 1];
									break;
								case 3:
									dst[0] = src[2];
									dst[1] = src[1];
									dst[2] = src[0];
									break;
								case 4:
									dst[0] = src[2];
									dst[1] = src[1];
									dst[2] = src[0];
									dst[3] = src[3];
									break;

							}

							src += 4;
							dst += nplanes;
						}
					}

					break;
			}

			if (imageMode == ImageModes.Gray16 || imageMode == ImageModes.RGB48)
			{
				filterRecord->inRowBytes *= 2;
				filterRecord->inColumnBytes *= 2;
			}

			return PSError.noErr;
		}

		/// <summary>
		/// Fills the output buffer with data from the destination image.
		/// </summary>
		/// <param name="filterRecord">The filter record.</param>
		private unsafe short FillOutputBuffer(FilterRecord* filterRecord)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.AdvanceState, string.Format("outRowBytes: {0}, Rect: {1}, loplane: {2}, hiplane: {3}", new object[] { filterRecord->outRowBytes, filterRecord->outRect, filterRecord->outLoPlane,
				filterRecord->outHiPlane }));

			using (Bitmap dst = dest.CreateAliasedBitmap())
			{
			}
#endif
			Rect16 outRect = filterRecord->outRect;
			int nplanes = filterRecord->outHiPlane - filterRecord->outLoPlane + 1;
			int width = outRect.right - outRect.left;
			int height = outRect.bottom - outRect.top;

			Rectangle lockRect = Rectangle.FromLTRB(outRect.left, outRect.top, outRect.right, outRect.bottom);

			int stride = width * nplanes;

			if (outDataPtr == IntPtr.Zero)
			{
				int len = stride * height;

				if (imageMode == ImageModes.RGB48 || imageMode == ImageModes.Gray16)
				{
					len *= 2;
				}

				try
				{
					outDataPtr = Memory.Allocate(len, false);
				}
				catch (OutOfMemoryException)
				{
					return PSError.memFullErr;
				}
			}
			filterRecord->outData = outDataPtr;
			filterRecord->outRowBytes = stride;
			filterRecord->outColumnBytes = nplanes;

			if (lockRect.Left < 0 || lockRect.Top < 0)
			{
				if (lockRect.Left < 0)
				{
					lockRect.X = 0;
					lockRect.Width -= -outRect.left;
				}

				if (lockRect.Top < 0)
				{
					lockRect.Y = 0;
					lockRect.Height -= -outRect.top;
				}
			}

			short channelOffset = filterRecord->outLoPlane;
			if (imageMode == ImageModes.RGB || imageMode == ImageModes.RGB48)
			{
				switch (filterRecord->outLoPlane) // Photoshop uses RGBA pixel order so map the Red and Blue channels to BGRA order
				{
					case 0:
						channelOffset = 2;
						break;
					case 2:
						channelOffset = 0;
						break;
				}
			}

			short padErr = SetFilterPadding(outDataPtr, stride, outRect, nplanes, channelOffset, filterRecord->outputPadding, lockRect, dest);
			if (padErr != PSError.noErr || (outRect.left >= dest.Width || outRect.top >= dest.Height))
			{
				return padErr;
			}
			void* ptr = outDataPtr.ToPointer();
			int top = lockRect.Top;
			int left = lockRect.Left;
			int bottom = Math.Min(lockRect.Bottom, dest.Height);
			int right = Math.Min(lockRect.Right, dest.Width);

			switch (imageMode)
			{
				case ImageModes.GrayScale:

					for (int y = top; y < bottom; y++)
					{
						byte* src = dest.GetPointAddressUnchecked(left, y);
						byte* dst = (byte*)ptr + ((y - top) * stride);

						for (int x = left; x < right; x++)
						{
							*dst = *src;

							src++;
							dst++;
						}
					}

					break;
				case ImageModes.Gray16:

					for (int y = top; y < bottom; y++)
					{
						ushort* src = (ushort*)dest.GetPointAddressUnchecked(left, y);
						ushort* dst = (ushort*)ptr + ((y - top) * stride);

						for (int x = left; x < right; x++)
						{
							*dst = *src;

							src++;
							dst++;
						}
					}

					break;
				case ImageModes.RGB:

					for (int y = top; y < bottom; y++)
					{
						byte* src = dest.GetPointAddressUnchecked(left, y);
						byte* dst = (byte*)ptr + ((y - top) * stride);

						for (int x = left; x < right; x++)
						{
							switch (nplanes)
							{
								case 1:
									*dst = src[channelOffset];
									break;
								case 2:
									dst[0] = src[channelOffset];
									dst[1] = src[channelOffset + 1];
									break;
								case 3:
									dst[0] = src[2];
									dst[1] = src[1];
									dst[2] = src[0];
									break;
								case 4:
									dst[0] = src[2];
									dst[1] = src[1];
									dst[2] = src[0];
									dst[3] = src[3];
									break;

							}

							src += 4;
							dst += nplanes;
						}
					}

					break;
				case ImageModes.RGB48:

					for (int y = top; y < bottom; y++)
					{
						ushort* src = (ushort*)dest.GetPointAddressUnchecked(left, y);
						ushort* dst = (ushort*)ptr + ((y - top) * stride);

						for (int x = left; x < right; x++)
						{
							switch (nplanes)
							{
								case 1:
									*dst = src[channelOffset];
									break;
								case 2:
									dst[0] = src[channelOffset];
									dst[1] = src[channelOffset + 1];
									break;
								case 3:
									dst[0] = src[2];
									dst[1] = src[1];
									dst[2] = src[0];
									break;
								case 4:
									dst[0] = src[2];
									dst[1] = src[1];
									dst[2] = src[0];
									dst[3] = src[3];
									break;

							}

							src += 4;
							dst += nplanes;
						}
					}

					break;
			}

			if (imageMode == ImageModes.RGB48 || imageMode == ImageModes.Gray16)
			{
				filterRecord->outRowBytes *= 2;
				filterRecord->outColumnBytes *= 2;
			}

			return PSError.noErr;
		}

		private unsafe void ScaleTempMask(int maskRate, Rectangle lockRect)
		{
			// If the rectangle bounds are not valid return a copy of the original surface.
			if (lockRect.X >= mask.Width || lockRect.Y >= mask.Height)
			{
				if ((tempMask == null) || tempMask.Width != mask.Width || tempMask.Height != mask.Height)
				{
					if (tempMask != null)
					{
						tempMask.Dispose();
						tempMask = null;
					}

					tempMask = new SurfaceGray8(mask.Width, mask.Height);
					tempMask.CopySurface(mask);
				}
				return;
			}

			int scaleFactor = FixedToInt32(maskRate);

			if (scaleFactor == 0)
			{
				scaleFactor = 1;
			}
			int scaleWidth = mask.Width / scaleFactor;
			int scaleHeight = mask.Height / scaleFactor;

			if (lockRect.Width > scaleWidth)
			{
				scaleWidth = lockRect.Width;
			}

			if (lockRect.Height > scaleHeight)
			{
				scaleHeight = lockRect.Height;
			}

			if ((tempMask == null) || scaleWidth != tempMask.Width || scaleHeight != tempMask.Height)
			{
				if (tempMask != null)
				{
					tempMask.Dispose();
					tempMask = null;
				}

				if (scaleFactor > 1)
				{
					tempMask = new SurfaceGray8(scaleWidth, scaleHeight);
					tempMask.SuperSampleFitSurface(mask);
				}
				else
				{
					tempMask = new SurfaceGray8(mask.Width, mask.Height);
					tempMask.CopySurface(mask);
				}
			}
		}

		/// <summary>
		/// Fills the mask buffer with data from the mask image.
		/// </summary>
		/// <param name="filterRecord">The filter record.</param>
		private unsafe short FillMaskBuffer(FilterRecord* filterRecord)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.AdvanceState, string.Format("maskRowBytes: {0}, Rect: {1}, maskRate: {2}", new object[] { filterRecord->maskRowBytes, filterRecord->maskRect, FixedToInt32(filterRecord->maskRate) }));
#endif
			Rect16 maskRect = filterRecord->maskRect;
			int width = maskRect.right - maskRect.left;
			int height = maskRect.bottom - maskRect.top;

			Rectangle lockRect = Rectangle.FromLTRB(maskRect.left, maskRect.top, maskRect.right, maskRect.bottom);

			if (lockRect.Left < 0 || lockRect.Top < 0)
			{
				if (lockRect.Left < 0)
				{
					lockRect.X = 0;
					lockRect.Width -= -maskRect.left;
				}

				if (lockRect.Top < 0)
				{
					lockRect.Y = 0;
					lockRect.Height -= -maskRect.top;
				}
			}

			try
			{
				ScaleTempMask(filterRecord->maskRate, lockRect);
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			if (maskDataPtr == IntPtr.Zero)
			{
				int len = width * height;

				try
				{
					maskDataPtr = Memory.Allocate(len, false);
				}
				catch (OutOfMemoryException)
				{
					return PSError.memFullErr;
				}
			}
			filterRecord->maskData = maskDataPtr;
			filterRecord->maskRowBytes = width;

			bool validImageBounds = (maskRect.left < source.Width && maskRect.top < source.Height);
			short err = SetFilterPadding(maskDataPtr, width, maskRect, 1, 0, filterRecord->maskPadding, lockRect, mask);
			if (err != PSError.noErr || !validImageBounds)
			{
				return err;
			}

			byte* ptr = (byte*)maskDataPtr.ToPointer();

			int top = lockRect.Top;
			int left = lockRect.Left;
			int maskHeight = Math.Min(lockRect.Bottom, mask.Height);
			int maskWidth = Math.Min(lockRect.Right, mask.Width);

			for (int y = top; y < maskHeight; y++)
			{
				byte* src = tempMask.GetPointAddressUnchecked(left, y);
				byte* dst = ptr + ((y - top) * width);
				for (int x = left; x < maskWidth; x++)
				{
					*dst = *src;

					src++;
					dst++;
				}
			}

			return PSError.noErr;
		}

		/// <summary>
		/// Stores the output buffer to the destination image.
		/// </summary>
		/// <param name="outData">The output buffer.</param>
		/// <param name="outRowBytes">The stride of the output buffer.</param>
		/// <param name="rect">The target rectangle within the image.</param>
		/// <param name="loplane">The output loPlane.</param>
		/// <param name="hiplane">The output hiPlane.</param>
		private unsafe void StoreOutputBuffer(IntPtr outData, int outRowBytes, Rect16 rect, int loplane, int hiplane)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.AdvanceState, string.Format("inRowBytes = {0}, Rect = {1}, loplane = {2}, hiplane = {3}", new object[] { outRowBytes.ToString(), rect.ToString(), loplane.ToString(), hiplane.ToString() }));
#endif
			if (outData == IntPtr.Zero)
			{
				return;
			}

			if (RectNonEmpty(rect))
			{
				if (rect.left >= source.Width || rect.top >= source.Height)
				{
					return;
				}

				int nplanes = hiplane - loplane + 1;

				int ofs = loplane;
				switch (loplane)
				{
					case 0:
						ofs = 2;
						break;
					case 2:
						ofs = 0;
						break;
				}
				Rectangle lockRect = Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);

				if (lockRect.Left < 0 || lockRect.Top < 0)
				{
					if (lockRect.Left < 0 && lockRect.Top < 0)
					{
						lockRect.X = lockRect.Y = 0;
						lockRect.Width -= -rect.left;
						lockRect.Height -= -rect.top;
					}
					else if (lockRect.Left < 0)
					{
						lockRect.X = 0;
						lockRect.Width -= -rect.left;
					}
					else if (lockRect.Top < 0)
					{
						lockRect.Y = 0;
						lockRect.Height -= -rect.top;
					}
				}


				void* ptr = outData.ToPointer();

				int top = lockRect.Top;
				int left = lockRect.Left;
				int bottom = Math.Min(lockRect.Bottom, dest.Height);
				int right = Math.Min(lockRect.Right, dest.Width);

				int stride16 = outRowBytes / 2;

				switch (imageMode)
				{
					case ImageModes.GrayScale:

						for (int y = top; y < bottom; y++)
						{
							byte* src = (byte*)ptr + ((y - top) * outRowBytes);
							byte* dst = dest.GetPointAddressUnchecked(left, y);

							for (int x = left; x < right; x++)
							{
								*dst = *src;

								src++;
								dst++;
							}
						}

						break;
					case ImageModes.Gray16:

						for (int y = top; y < bottom; y++)
						{
							ushort* src = (ushort*)ptr + ((y - top) * stride16);
							ushort* dst = (ushort*)dest.GetPointAddressUnchecked(left, y);

							for (int x = left; x < right; x++)
							{
								*dst = *src;

								src++;
								dst++;
							}
						}

						break;
					case ImageModes.RGB:

						for (int y = top; y < bottom; y++)
						{
							byte* src = (byte*)ptr + ((y - top) * outRowBytes);
							byte* dst = dest.GetPointAddressUnchecked(left, y);

							for (int x = left; x < right; x++)
							{
								switch (nplanes)
								{
									case 1:
										dst[ofs] = *src;
										break;
									case 2:
										dst[ofs] = src[0];
										dst[ofs + 1] = src[1];
										break;
									case 3:
										dst[0] = src[2];
										dst[1] = src[1];
										dst[2] = src[0];
										break;
									case 4:
										dst[0] = src[2];
										dst[1] = src[1];
										dst[2] = src[0];
										dst[3] = src[3];
										break;

								}

								src += nplanes;
								dst += 4;
							}
						}

						break;
					case ImageModes.RGB48:

						for (int y = top; y < bottom; y++)
						{
							ushort* src = (ushort*)ptr + ((y - top) * stride16);
							ushort* dst = (ushort*)dest.GetPointAddressUnchecked(left, y);

							for (int x = left; x < right; x++)
							{
								switch (nplanes)
								{
									case 1:
										dst[ofs] = src[0];
										break;
									case 2:
										dst[ofs] = src[0];
										dst[ofs + 1] = src[1];
										break;
									case 3:
										dst[0] = src[2];
										dst[1] = src[1];
										dst[2] = src[0];
										break;
									case 4:
										dst[0] = src[2];
										dst[1] = src[1];
										dst[2] = src[0];
										dst[3] = src[3];
										break;
								}

								src += nplanes;
								dst += 4;
							}
						}

						break;
				}

#if DEBUG
				using (Bitmap bmp = dest.CreateAliasedBitmap())
				{
				}
#endif
			}
		}

		/// <summary>
		/// Applies any pre-processing to the input data that the filter may require.
		/// </summary>
		private unsafe void PreProcessInputData()
		{
			if (inputHandling != FilterDataHandling.None && (filterCase == FilterCase.EditableTransparencyNoSelection || filterCase == FilterCase.EditableTransparencyWithSelection))
			{
				int width = source.Width;
				int height = source.Height;

				if (imageMode == ImageModes.RGB48)
				{
					// TODO: Is using 8-bit values for the Zap modes correct for 16-bit images?
					for (int y = 0; y < height; y++)
					{
						ushort* ptr = (ushort*)source.GetRowAddressUnchecked(y);

						for (int x = 0; x < width; x++)
						{
							if (ptr[3] == 0)
							{
								switch (inputHandling)
								{
									case FilterDataHandling.BlackMat:
										break;
									case FilterDataHandling.GrayMat:
										break;
									case FilterDataHandling.WhiteMat:
										break;
									case FilterDataHandling.Defringe:
										break;
									case FilterDataHandling.BlackZap:
										ptr[0] = ptr[1] = ptr[2] = 0;
										break;
									case FilterDataHandling.GrayZap:
										ptr[0] = ptr[1] = ptr[2] = 128;
										break;
									case FilterDataHandling.WhiteZap:
										ptr[0] = ptr[1] = ptr[2] = 255;
										break;
									case FilterDataHandling.BackgroundZap:
										ptr[2] = backgroundColor[0];
										ptr[1] = backgroundColor[1];
										ptr[0] = backgroundColor[2];
										break;
									case FilterDataHandling.ForegroundZap:
										ptr[2] = foregroundColor[0];
										ptr[1] = foregroundColor[1];
										ptr[0] = foregroundColor[2];
										break;
									default:
										break;
								}
							}

							ptr += 4;
						}
					}
				}
				else
				{
					for (int y = 0; y < height; y++)
					{
						byte* ptr = source.GetRowAddressUnchecked(y);

						for (int x = 0; x < width; x++)
						{
							if (ptr[3] == 0)
							{
								switch (inputHandling)
								{
									case FilterDataHandling.BlackMat:
										break;
									case FilterDataHandling.GrayMat:
										break;
									case FilterDataHandling.WhiteMat:
										break;
									case FilterDataHandling.Defringe:
										break;
									case FilterDataHandling.BlackZap:
										ptr[0] = ptr[1] = ptr[2] = 0;
										break;
									case FilterDataHandling.GrayZap:
										ptr[0] = ptr[1] = ptr[2] = 128;
										break;
									case FilterDataHandling.WhiteZap:
										ptr[0] = ptr[1] = ptr[2] = 255;
										break;
									case FilterDataHandling.BackgroundZap:
										ptr[2] = backgroundColor[0];
										ptr[1] = backgroundColor[1];
										ptr[0] = backgroundColor[2];
										break;
									case FilterDataHandling.ForegroundZap:
										ptr[2] = foregroundColor[0];
										ptr[1] = foregroundColor[1];
										ptr[0] = foregroundColor[2];
										break;
									default:
										break;
								}
							}

							ptr += 4;
						}
					}
				}
			}
		}

		/// <summary>
		/// Applies any post processing to the output data that the filter may require.
		/// </summary>
		private unsafe void PostProcessOutputData()
		{
			if (outputHandling == FilterDataHandling.FillMask && (filterCase == FilterCase.EditableTransparencyNoSelection || filterCase == FilterCase.EditableTransparencyWithSelection))
			{
				if ((selectedRegion != null) && !writesOutsideSelection)
				{
					Rectangle[] scans = selectedRegion.GetRegionScansReadOnlyInt();
					dest.SetAlphaToOpaque(scans);
				}
				else
				{
					dest.SetAlphaToOpaque();
				}
			}
		}

		private unsafe short ColorServicesProc(ref ColorServicesInfo info)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.ColorServices, string.Format("selector: {0}", info.selector));
#endif
			short err = PSError.noErr;
			switch (info.selector)
			{
				case ColorServicesSelector.ChooseColor:

					string prompt = StringUtil.FromPascalString(info.selectorParameter.pickerPrompt, string.Empty);

					if (info.sourceSpace != ColorSpace.RGBSpace)
					{
						err = ColorServicesConvert.Convert(info.sourceSpace, ColorSpace.RGBSpace, ref info.colorComponents);

						if (err != PSError.noErr)
						{
							return err;
						}
					}

					byte red = (byte)info.colorComponents[0];
					byte green = (byte)info.colorComponents[1];
					byte blue = (byte)info.colorComponents[2];

					if (ColorPickerManager.ShowColorPickerDialog(prompt, ref red, ref green, ref blue))
					{
						info.colorComponents[0] = red;
						info.colorComponents[1] = green;
						info.colorComponents[2] = blue;

						if (info.resultSpace == ColorSpace.ChosenSpace)
						{
							info.resultSpace = ColorSpace.RGBSpace;
						}

						err = ColorServicesConvert.Convert(ColorSpace.RGBSpace, info.resultSpace, ref info.colorComponents);
					}
					else
					{
						err = PSError.userCanceledErr;
					}

					break;
				case ColorServicesSelector.ConvertColor:

					err = ColorServicesConvert.Convert(info.sourceSpace, info.resultSpace, ref info.colorComponents);

					break;
				case ColorServicesSelector.GetSpecialColor:

					switch (info.selectorParameter.specialColorID)
					{
						case SpecialColorID.BackgroundColor:

							for (int i = 0; i < 4; i++)
							{
								info.colorComponents[i] = (short)backgroundColor[i];
							}
							break;
						case SpecialColorID.ForegroundColor:

							for (int i = 0; i < 4; i++)
							{
								info.colorComponents[i] = (short)foregroundColor[i];
							}
							break;
						default:
							err = PSError.paramErr;
							break;
					}

					if (err == PSError.noErr)
					{
						err = ColorServicesConvert.Convert(ColorSpace.RGBSpace, info.resultSpace, ref info.colorComponents);
					}

					break;
				case ColorServicesSelector.SamplePoint:

					Point16* point = (Point16*)info.selectorParameter.globalSamplePoint.ToPointer();

					if ((point->h >= 0 && point->h < source.Width) && (point->v >= 0 && point->v < source.Height))
					{
						ColorSpace sourceSpace = ColorSpace.RGBSpace;

						if (imageMode == ImageModes.Gray16 || imageMode == ImageModes.RGB48)
						{
							// As this function only handles 8-bit data return 255 if the source image is 16-bit, same as Adobe(R) Photoshop(R).
							if (imageMode == ImageModes.Gray16)
							{
								info.colorComponents[0] = 255;
								info.colorComponents[1] = 0;
								info.colorComponents[2] = 0;
								info.colorComponents[3] = 0;
								sourceSpace = ColorSpace.GraySpace;
							}
							else
							{
								info.colorComponents[0] = 255;
								info.colorComponents[1] = 255;
								info.colorComponents[2] = 255;
								info.colorComponents[3] = 0;
							}
						}
						else
						{
							byte* pixel = source.GetPointAddressUnchecked(point->h, point->v);

							switch (imageMode)
							{
								case ImageModes.GrayScale:
									info.colorComponents[0] = pixel[0];
									info.colorComponents[1] = 0;
									info.colorComponents[2] = 0;
									info.colorComponents[3] = 0;
									sourceSpace = ColorSpace.GraySpace;
									break;
								case ImageModes.RGB:
									info.colorComponents[0] = pixel[2];
									info.colorComponents[1] = pixel[1];
									info.colorComponents[2] = pixel[0];
									info.colorComponents[3] = 0;
									break;
							}
						}
						err = ColorServicesConvert.Convert(sourceSpace, info.resultSpace, ref info.colorComponents);
					}
					else
					{
						err = PSError.errInvalidSamplePoint;
					}

					break;

			}
			return err;
		}

		private static unsafe void FillChannelData(int channel, PixelMemoryDesc destiniation, SurfaceBase source, VRect srcRect, ImageModes mode)
		{
			byte* dstPtr = (byte*)destiniation.data.ToPointer();
			int stride = destiniation.rowBits / 8;
			int bpp = destiniation.colBits / 8;
			int offset = destiniation.bitOffset / 8;

			switch (mode)
			{

				case ImageModes.GrayScale:

					for (int y = srcRect.top; y < srcRect.bottom; y++)
					{
						byte* src = source.GetPointAddressUnchecked(srcRect.left, y);
						byte* dst = dstPtr + (y * stride) + offset;
						for (int x = srcRect.left; x < srcRect.right; x++)
						{
							*dst = *src;

							src++;
							dst += bpp;
						}
					}

					break;
				case ImageModes.RGB:

					for (int y = srcRect.top; y < srcRect.bottom; y++)
					{
						byte* src = source.GetPointAddressUnchecked(srcRect.left, y);
						byte* dst = dstPtr + (y * stride) + offset;
						for (int x = srcRect.left; x < srcRect.right; x++)
						{
							switch (channel)
							{
								case PSConstants.ChannelPorts.Red:
									*dst = src[2];
									break;
								case PSConstants.ChannelPorts.Green:
									*dst = src[1];
									break;
								case PSConstants.ChannelPorts.Blue:
									*dst = src[0];
									break;
								case PSConstants.ChannelPorts.Alpha:
									*dst = src[3];
									break;
							}
							src += 4;
							dst += bpp;
						}
					}

					break;
				case ImageModes.Gray16:

					for (int y = srcRect.top; y < srcRect.bottom; y++)
					{
						ushort* src = (ushort*)source.GetPointAddressUnchecked(srcRect.left, y);
						ushort* dst = (ushort*)(dstPtr + (y * stride) + offset);
						for (int x = srcRect.left; x < srcRect.right; x++)
						{
							*dst = *src;

							src++;
							dst += bpp;
						}
					}

					break;
				case ImageModes.RGB48:

					for (int y = srcRect.top; y < srcRect.bottom; y++)
					{
						ushort* src = (ushort*)source.GetPointAddressUnchecked(srcRect.left, y);
						ushort* dst = (ushort*)(dstPtr + (y * stride) + offset);
						for (int x = srcRect.left; x < srcRect.right; x++)
						{
							switch (channel)
							{
								case PSConstants.ChannelPorts.Red:
									*dst = src[2];
									break;
								case PSConstants.ChannelPorts.Green:
									*dst = src[1];
									break;
								case PSConstants.ChannelPorts.Blue:
									*dst = src[0];
									break;
								case PSConstants.ChannelPorts.Alpha:
									*dst = src[3];
									break;
							}
							src += 4;
							dst += bpp;
						}
					}

					break;
			}

		}

		private static unsafe void FillSelectionMask(PixelMemoryDesc destiniation, SurfaceGray8 source, VRect srcRect)
		{
			byte* dstPtr = (byte*)destiniation.data.ToPointer();
			int stride = destiniation.rowBits / 8;
			int bpp = destiniation.colBits / 8;
			int offset = destiniation.bitOffset / 8;

			for (int y = srcRect.top; y < srcRect.bottom; y++)
			{
				byte* src = source.GetPointAddressUnchecked(srcRect.left, y);
				byte* dst = dstPtr + (y * stride) + offset;
				for (int x = srcRect.left; x < srcRect.right; x++)
				{
					*dst = *src;

					src++;
					dst += bpp;
				}
			}
		}

		private unsafe short CreateDitheredChannelPortSurface()
		{
			int width = source.Width;
			int height = source.Height;

			try
			{
				switch (imageMode)
				{
					case ImageModes.Gray16:

						ditheredChannelImageMode = ImageModes.GrayScale;
						ditheredChannelSurface = SurfaceFactory.CreateFromImageMode(width, height, ditheredChannelImageMode);

						for (int y = 0; y < height; y++)
						{
							ushort* src = (ushort*)source.GetRowAddressUnchecked(y);
							byte* dst = ditheredChannelSurface.GetRowAddressUnchecked(y);
							for (int x = 0; x < width; x++)
							{
								*dst = (byte)((*src * 10) / 1285);

								src++;
								dst++;
							}
						}
						break;

					case ImageModes.RGB48:

						ditheredChannelImageMode = ImageModes.RGB;
						ditheredChannelSurface = SurfaceFactory.CreateFromImageMode(width, height, ditheredChannelImageMode);

						for (int y = 0; y < height; y++)
						{
							ushort* src = (ushort*)source.GetRowAddressUnchecked(y);
							byte* dst = ditheredChannelSurface.GetRowAddressUnchecked(y);
							for (int x = 0; x < width; x++)
							{
								dst[0] = (byte)((src[0] * 10) / 1285);
								dst[1] = (byte)((src[1] * 10) / 1285);
								dst[2] = (byte)((src[2] * 10) / 1285);
								dst[3] = (byte)((src[3] * 10) / 1285);

								src += 4;
								dst += 4;
							}
						}

						break;
				}
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.noErr;
		}

		private unsafe short ReadPixelsProc(IntPtr port, ref PSScaling scaling, ref VRect writeRect, ref PixelMemoryDesc destination, ref VRect wroteRect)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.ChannelPorts, string.Format("port: {0}, rect: {1}", port.ToString(), writeRect.ToString()));
#endif

			if (destination.depth != 8 && destination.depth != 16)
			{
				return PSError.errUnsupportedDepth;
			}

			if ((destination.bitOffset % 8) != 0)  // the offsets must be aligned to a System.Byte. 
			{
				return PSError.errUnsupportedBitOffset;
			}

			if ((destination.colBits % 8) != 0)
			{
				return PSError.errUnsupportedColBits;
			}

			if ((destination.rowBits % 8) != 0)
			{
				return PSError.errUnsupportedRowBits;
			}


			int channel = port.ToInt32();

			if (channel < PSConstants.ChannelPorts.Gray || channel > PSConstants.ChannelPorts.SelectionMask)
			{
				return PSError.errUnknownPort;
			}

			VRect srcRect = scaling.sourceRect;
			VRect dstRect = scaling.destinationRect;

			int srcWidth = srcRect.right - srcRect.left;
			int srcHeight = srcRect.bottom - srcRect.top;
			int dstWidth = dstRect.right - dstRect.left;
			int dstHeight = dstRect.bottom - dstRect.top;
			bool isSelection = channel == PSConstants.ChannelPorts.SelectionMask;

			if ((source.BitsPerChannel == 8 || isSelection) && destination.depth == 16)
			{
				return PSError.errUnsupportedDepthConversion; // converting 8-bit image data to 16-bit is not supported.
			}

			if (isSelection)
			{
				if (srcWidth == dstWidth && srcHeight == dstHeight)
				{
					FillSelectionMask(destination, mask, srcRect);
				}
				else if (dstWidth < srcWidth || dstHeight < srcHeight) // scale down
				{
					if ((scaledSelectionMask == null) || scaledSelectionMask.Width != dstWidth || scaledSelectionMask.Height != dstHeight)
					{
						if (scaledSelectionMask != null)
						{
							scaledSelectionMask.Dispose();
							scaledSelectionMask = null;
						}

						try
						{
							scaledSelectionMask = new SurfaceGray8(dstWidth, dstHeight);
							scaledSelectionMask.SuperSampleFitSurface(mask);
						}
						catch (OutOfMemoryException)
						{
							return PSError.memFullErr;
						}
					}

					FillSelectionMask(destination, scaledSelectionMask, dstRect);
				}
				else if (dstWidth > srcWidth || dstHeight > srcHeight) // scale up
				{
					if ((scaledSelectionMask == null) || scaledSelectionMask.Width != dstWidth || scaledSelectionMask.Height != dstHeight)
					{
						if (scaledSelectionMask != null)
						{
							scaledSelectionMask.Dispose();
							scaledSelectionMask = null;
						}

						try
						{
							scaledSelectionMask = new SurfaceGray8(dstWidth, dstHeight);
							scaledSelectionMask.BicubicFitSurface(mask);
						}
						catch (OutOfMemoryException)
						{
							return PSError.memFullErr;
						}
					}

					FillSelectionMask(destination, scaledSelectionMask, dstRect);
				}

			}
			else
			{
				ImageModes mode = this.imageMode;

				if (source.BitsPerChannel == 16 && destination.depth == 8)
				{
					if (ditheredChannelSurface == null)
					{
						short err = CreateDitheredChannelPortSurface();
						if (err != PSError.noErr)
						{
							return err;
						}
					}

					mode = ditheredChannelImageMode;
				}

				if (srcWidth == dstWidth && srcHeight == dstHeight)
				{
					FillChannelData(channel, destination, ditheredChannelSurface ?? source, srcRect, mode);
				}
				else if (dstWidth < srcWidth || dstHeight < srcHeight) // scale down
				{
					if ((scaledChannelSurface == null) || scaledChannelSurface.Width != dstWidth || scaledChannelSurface.Height != dstHeight)
					{
						if (scaledChannelSurface != null)
						{
							scaledChannelSurface.Dispose();
							scaledChannelSurface = null;
						}

						try
						{
							scaledChannelSurface = SurfaceFactory.CreateFromImageMode(dstWidth, dstHeight, mode);
							scaledChannelSurface.SuperSampleFitSurface(ditheredChannelSurface ?? source);
						}
						catch (OutOfMemoryException)
						{
							return PSError.memFullErr;
						}

#if DEBUG
						using (Bitmap bmp = scaledChannelSurface.CreateAliasedBitmap())
						{

						}
#endif
					}

					FillChannelData(channel, destination, scaledChannelSurface, dstRect, mode);
				}
				else if (dstWidth > srcWidth || dstHeight > srcHeight) // scale up
				{
					if ((scaledChannelSurface == null) || scaledChannelSurface.Width != dstWidth || scaledChannelSurface.Height != dstHeight)
					{
						if (scaledChannelSurface != null)
						{
							scaledChannelSurface.Dispose();
							scaledChannelSurface = null;
						}

						try
						{
							scaledChannelSurface = SurfaceFactory.CreateFromImageMode(dstWidth, dstHeight, mode);
							scaledChannelSurface.BicubicFitSurface(ditheredChannelSurface ?? source);
						}
						catch (OutOfMemoryException)
						{
							return PSError.memFullErr;
						}
					}

					FillChannelData(channel, destination, scaledChannelSurface, dstRect, mode);
				}
			}


			wroteRect = dstRect;

			return PSError.noErr;
		}

		private short WriteBasePixels(IntPtr port, ref VRect writeRect, PixelMemoryDesc srcDesc)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.ChannelPorts, string.Format("port: {0}, rect: {1}", port.ToString(), writeRect.ToString()));
#endif
			return PSError.memFullErr;
		}

		private short ReadPortForWritePort(ref IntPtr readPort, IntPtr writePort)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.ChannelPorts, string.Format("readPort: {0}, writePort: {1}", readPort.ToString(), writePort.ToString()));
#endif
			return PSError.memFullErr;
		}

		private unsafe void CreateReadImageDocument()
		{
			readDocumentPtr = Memory.Allocate(Marshal.SizeOf(typeof(ReadImageDocumentDesc)), true);
			ReadImageDocumentDesc* doc = (ReadImageDocumentDesc*)readDocumentPtr.ToPointer();
			doc->minVersion = PSConstants.kCurrentMinVersReadImageDocDesc;
			doc->maxVersion = PSConstants.kCurrentMaxVersReadImageDocDesc;
			doc->imageMode = (int)imageMode;

			switch (imageMode)
			{
				case ImageModes.GrayScale:
				case ImageModes.RGB:
					doc->depth = 8;
					break;
				case ImageModes.Gray16:
				case ImageModes.RGB48:
					doc->depth = 16;
					break;
			}

			doc->bounds.top = 0;
			doc->bounds.left = 0;
			doc->bounds.right = source.Width;
			doc->bounds.bottom = source.Height;
			doc->hResolution = Int32ToFixed((int)(dpiX + 0.5));
			doc->vResolution = Int32ToFixed((int)(dpiY + 0.5));

			if (imageMode == ImageModes.RGB || imageMode == ImageModes.RGB48)
			{
				IntPtr channel = CreateReadChannelDesc(PSConstants.ChannelPorts.Red, Resources.RedChannelName, doc->depth, doc->bounds);

				ReadChannelDesc* ch = (ReadChannelDesc*)channel.ToPointer();

				for (int i = PSConstants.ChannelPorts.Green; i <= PSConstants.ChannelPorts.Blue; i++)
				{
					string name = null;
					switch (i)
					{
						case PSConstants.ChannelPorts.Green:
							name = Resources.GreenChannelName;
							break;
						case PSConstants.ChannelPorts.Blue:
							name = Resources.BlueChannelName;
							break;
					}

					IntPtr ptr = CreateReadChannelDesc(i, name, doc->depth, doc->bounds);

					ch->next = ptr;

					ch = (ReadChannelDesc*)ptr.ToPointer();
				}

				doc->targetCompositeChannels = doc->mergedCompositeChannels = channel;

				if (!ignoreAlpha)
				{
					IntPtr alphaPtr = CreateReadChannelDesc(PSConstants.ChannelPorts.Alpha, Resources.AlphaChannelName, doc->depth, doc->bounds);
					doc->targetTransparency = doc->mergedTransparency = alphaPtr;
				}
			}
			else
			{
				IntPtr channel = CreateReadChannelDesc(PSConstants.ChannelPorts.Gray, Resources.GrayChannelName, doc->depth, doc->bounds);
				doc->targetCompositeChannels = doc->mergedCompositeChannels = channel;
			}

			if (selectedRegion != null)
			{
				IntPtr selectionPtr = CreateReadChannelDesc(PSConstants.ChannelPorts.SelectionMask, Resources.MaskChannelName, doc->depth, doc->bounds);
				doc->selection = selectionPtr;
			}
		}

		private unsafe IntPtr CreateReadChannelDesc(int channel, string name, int depth, VRect bounds)
		{
			IntPtr addressPtr = Memory.Allocate(Marshal.SizeOf(typeof(ReadChannelDesc)), true);
			IntPtr namePtr = IntPtr.Zero;
			try
			{
				namePtr = Marshal.StringToHGlobalAnsi(name);

				channelReadDescPtrs.Add(new ChannelDescPtrs(addressPtr, namePtr));
			}
			catch (Exception)
			{
				Memory.Free(addressPtr);
				if (namePtr != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(namePtr);
					namePtr = IntPtr.Zero;
				}
				throw;
			}

			ReadChannelDesc* desc = (ReadChannelDesc*)addressPtr.ToPointer();
			desc->minVersion = PSConstants.kCurrentMinVersReadChannelDesc;
			desc->maxVersion = PSConstants.kCurrentMaxVersReadChannelDesc;
			desc->depth = depth;
			desc->bounds = bounds;

			desc->target = (channel < 3) ? (byte)1 : (byte)0;
			desc->shown = (channel < 4) ? (byte)1 : (byte)0;

			desc->tileOrigin.h = 0;
			desc->tileOrigin.v = 0;
			desc->tileSize.h = bounds.right - bounds.left;
			desc->tileSize.v = bounds.bottom - bounds.top;

			desc->port = new IntPtr(channel);
			switch (channel)
			{
				case PSConstants.ChannelPorts.Gray:
					desc->channelType = ChannelTypes.Black;
					break;
				case PSConstants.ChannelPorts.Red:
					desc->channelType = ChannelTypes.Red;
					break;
				case PSConstants.ChannelPorts.Green:
					desc->channelType = ChannelTypes.Green;
					break;
				case PSConstants.ChannelPorts.Blue:
					desc->channelType = ChannelTypes.Blue;
					break;
				case PSConstants.ChannelPorts.Alpha:
					desc->channelType = ChannelTypes.Transparency;
					break;
				case PSConstants.ChannelPorts.SelectionMask:
					desc->channelType = ChannelTypes.SelectionMask;
					break;
			}
			desc->name = namePtr;

			return addressPtr;
		}

		private static unsafe void SetFilterEdgePadding8(IntPtr inData, int inRowBytes, Rect16 rect, int nplanes, short ofs, Rectangle lockRect, SurfaceBase surface)
		{
			int top = rect.top < 0 ? -rect.top : 0;
			int left = rect.left < 0 ? -rect.left : 0;

			int right = lockRect.Right - surface.Width;
			int bottom = lockRect.Bottom - surface.Height;

			int height = rect.bottom - rect.top;
			int width = rect.right - rect.left;

			int row, col;

			byte* ptr = (byte*)inData.ToPointer();

			int srcChannelCount = surface.ChannelCount;


			if (top > 0)
			{
				for (int y = 0; y < top; y++)
				{
					byte* src = surface.GetRowAddressUnchecked(0);
					byte* dst = ptr + (y * inRowBytes);

					for (int x = 0; x < width; x++)
					{
						switch (nplanes)
						{
							case 1:
								*dst = src[ofs];
								break;
							case 2:
								dst[0] = src[ofs];
								dst[1] = src[ofs + 1];
								break;
							case 3:
								dst[0] = src[2];
								dst[1] = src[1];
								dst[2] = src[0];
								break;
							case 4:
								dst[0] = src[2];
								dst[1] = src[1];
								dst[2] = src[0];
								dst[3] = src[3];
								break;
						}

						src += srcChannelCount;
						dst += nplanes;
					}
				}
			}


			if (left > 0)
			{
				for (int y = 0; y < height; y++)
				{
					byte* src = surface.GetPointAddressUnchecked(0, y);
					byte* dst = ptr + (y * inRowBytes);

					for (int x = 0; x < left; x++)
					{
						switch (nplanes)
						{
							case 1:
								*dst = src[ofs];
								break;
							case 2:
								dst[0] = src[ofs];
								dst[1] = src[ofs + 1];
								break;
							case 3:
								dst[0] = src[2];
								dst[1] = src[1];
								dst[2] = src[0];
								break;
							case 4:
								dst[0] = src[2];
								dst[1] = src[1];
								dst[2] = src[0];
								dst[3] = src[3];
								break;
						}
						dst += nplanes;
					}
				}
			}


			if (bottom > 0)
			{
				col = surface.Height - 1;
				int lockBottom = height - 1;
				for (int y = 0; y < bottom; y++)
				{
					byte* src = surface.GetRowAddressUnchecked(col);
					byte* dst = ptr + ((lockBottom - y) * inRowBytes);

					for (int x = 0; x < width; x++)
					{
						switch (nplanes)
						{
							case 1:
								*dst = src[ofs];
								break;
							case 2:
								dst[0] = src[ofs];
								dst[1] = src[ofs + 1];
								break;
							case 3:
								dst[0] = src[2];
								dst[1] = src[1];
								dst[2] = src[0];
								break;
							case 4:
								dst[0] = src[2];
								dst[1] = src[1];
								dst[2] = src[0];
								dst[3] = src[3];
								break;
						}

						src += srcChannelCount;
						dst += nplanes;
					}

				}
			}

			if (right > 0)
			{
				row = surface.Width - 1;
				int rowEnd = width - right;
				for (int y = 0; y < height; y++)
				{
					byte* src = surface.GetPointAddressUnchecked(row, y);
					byte* dst = ptr + (y * inRowBytes) + rowEnd;

					for (int x = 0; x < right; x++)
					{
						switch (nplanes)
						{
							case 1:
								*dst = src[ofs];
								break;
							case 2:
								dst[0] = src[ofs];
								dst[1] = src[ofs + 1];
								break;
							case 3:
								dst[0] = src[2];
								dst[1] = src[1];
								dst[2] = src[0];
								break;
							case 4:
								dst[0] = src[2];
								dst[1] = src[1];
								dst[2] = src[0];
								dst[3] = src[3];
								break;
						}
						dst += nplanes;
					}
				}
			}
		}

		private static unsafe void SetFilterEdgePadding16(IntPtr inData, int inRowBytes, Rect16 rect, int nplanes, short ofs, Rectangle lockRect, SurfaceBase surface)
		{
			int top = rect.top < 0 ? -rect.top : 0;
			int left = rect.left < 0 ? -rect.left : 0;

			int right = lockRect.Right - surface.Width;
			int bottom = lockRect.Bottom - surface.Height;

			int height = rect.bottom - rect.top;
			int width = rect.right - rect.left;

			int row, col;

			byte* ptr = (byte*)inData.ToPointer();

			int srcChannelCount = surface.ChannelCount;

			if (top > 0)
			{
				for (int y = 0; y < top; y++)
				{
					ushort* src = (ushort*)surface.GetRowAddressUnchecked(0);
					ushort* dst = (ushort*)ptr + (y * inRowBytes);

					for (int x = 0; x < width; x++)
					{
						switch (nplanes)
						{
							case 1:
								*dst = src[ofs];
								break;
							case 2:
								dst[0] = src[ofs];
								dst[1] = src[ofs + 1];
								break;
							case 3:
								dst[0] = src[2];
								dst[1] = src[1];
								dst[2] = src[0];
								break;
							case 4:
								dst[0] = src[2];
								dst[1] = src[1];
								dst[2] = src[0];
								dst[3] = src[3];
								break;
						}

						src += srcChannelCount;
						dst += nplanes;
					}
				}
			}

			if (left > 0)
			{
				for (int y = 0; y < height; y++)
				{
					ushort* src = (ushort*)surface.GetPointAddressUnchecked(0, y);
					ushort* dst = (ushort*)ptr + (y * inRowBytes);

					for (int x = 0; x < left; x++)
					{
						switch (nplanes)
						{
							case 1:
								*dst = src[ofs];
								break;
							case 2:
								dst[0] = src[ofs];
								dst[1] = src[ofs + 1];
								break;
							case 3:
								dst[0] = src[2];
								dst[1] = src[1];
								dst[2] = src[0];
								break;
							case 4:
								dst[0] = src[2];
								dst[1] = src[1];
								dst[2] = src[0];
								dst[3] = src[3];
								break;
						}
						dst += nplanes;
					}
				}
			}


			if (bottom > 0)
			{
				col = surface.Height - 1;
				int lockBottom = height - 1;
				for (int y = 0; y < bottom; y++)
				{
					ushort* src = (ushort*)surface.GetRowAddressUnchecked(col);
					ushort* dst = (ushort*)ptr + ((lockBottom - y) * inRowBytes);

					for (int x = 0; x < width; x++)
					{
						switch (nplanes)
						{
							case 1:
								*dst = src[ofs];
								break;
							case 2:
								dst[0] = src[ofs];
								dst[1] = src[ofs + 1];
								break;
							case 3:
								dst[0] = src[2];
								dst[1] = src[1];
								dst[2] = src[0];
								break;
							case 4:
								dst[0] = src[2];
								dst[1] = src[1];
								dst[2] = src[0];
								dst[3] = src[3];
								break;
						}

						src += srcChannelCount;
						dst += nplanes;
					}

				}
			}

			if (right > 0)
			{
				row = surface.Width - 1;
				int rowEnd = width - right;
				for (int y = 0; y < height; y++)
				{
					ushort* src = (ushort*)surface.GetPointAddressUnchecked(row, y);
					ushort* dst = (ushort*)ptr + (y * inRowBytes) + rowEnd;

					for (int x = 0; x < right; x++)
					{
						switch (nplanes)
						{
							case 1:
								*dst = src[ofs];
								break;
							case 2:
								dst[0] = src[ofs];
								dst[1] = src[ofs + 1];
								break;
							case 3:
								dst[0] = src[2];
								dst[1] = src[1];
								dst[2] = src[0];
								break;
							case 4:
								dst[0] = src[2];
								dst[1] = src[1];
								dst[2] = src[0];
								dst[3] = src[3];
								break;
						}
						dst += nplanes;
					}
				}
			}
		}

		/// <summary>
		/// Sets the filter padding.
		/// </summary>
		/// <param name="inData">The input data.</param>
		/// <param name="inRowBytes">The input stride.</param>
		/// <param name="rect">The input rect.</param>
		/// <param name="nplanes">The number of channels in the image.</param>
		/// <param name="ofs">The single channel offset to map to BGRA color space.</param>
		/// <param name="inputPadding">The input padding mode.</param>
		/// <param name="lockRect">The lock rect.</param>
		/// <param name="surface">The surface.</param>
		private static unsafe short SetFilterPadding(IntPtr inData, int inRowBytes, Rect16 rect, int nplanes, short ofs, short inputPadding, Rectangle lockRect, SurfaceBase surface)
		{
			if ((lockRect.Right > surface.Width || lockRect.Bottom > surface.Height) || (rect.top < 0 || rect.left < 0))
			{
				switch (inputPadding)
				{
					case PSConstants.Padding.plugInWantsEdgeReplication:

						switch (surface.BitsPerChannel)
						{
							case 16:
								SetFilterEdgePadding16(inData, inRowBytes, rect, nplanes, ofs, lockRect, surface);
								break;
							case 8:
								SetFilterEdgePadding8(inData, inRowBytes, rect, nplanes, ofs, lockRect, surface);
								break;
							default:
								break;
						}

						break;
					case PSConstants.Padding.plugInDoesNotWantPadding:
						break;
					case PSConstants.Padding.plugInWantsErrorOnBoundsException:
						return PSError.paramErr;
					default:

						// Any other padding value is a constant byte.
						if (inputPadding < 0 || inputPadding > 255)
						{
							return PSError.paramErr;
						}

						long size = Memory.Size(inData);
						SafeNativeMethods.memset(inData, inputPadding, new UIntPtr((ulong)size));
						break;
				}

			}

			return PSError.noErr;
		}

		private void SetupDisplaySurface(int width, int height, bool haveMask, int displayImageMode)
		{
			if ((displaySurface == null) || width != displaySurface.Width || height != displaySurface.Height)
			{
				if (displaySurface != null)
				{
					displaySurface.Dispose();
					displaySurface = null;
				}

				displaySurface = new SurfaceBGRA32(width, height);

				if (ignoreAlpha || !haveMask)
				{
					displaySurface.SetAlphaToOpaque();
				}

				// As some plug-ins may use planar order RGB data and the Windows Color System APIs do not support that format
				// we first have to convert the data to interleaved BGR(A) and then use a second surface for color correction. 
				if (displayImageMode == PSConstants.plugInModeRGBColor && colorProfileConverter.ColorCorrectionRequired)
				{
					if (colorCorrectedDisplaySurface != null)
					{
						colorCorrectedDisplaySurface.Dispose();
						colorCorrectedDisplaySurface = null;
					}

					colorCorrectedDisplaySurface = new SurfaceBGRA32(width, height);
				}
			}
		}

		/// <summary>
		/// Gets the preview bitmap for the RGB image modes and applies color correction if necessary.
		/// </summary>
		/// <returns>The resulting bitmap.</returns>
		private Bitmap GetRGBPreviewBitmap()
		{
			if (colorProfileConverter.ColorCorrectionRequired && colorProfileConverter.ColorCorrectBGRASurface(displaySurface, colorCorrectedDisplaySurface))
			{
				return colorCorrectedDisplaySurface.CreateAliasedBitmap();
			}
			else
			{
				return displaySurface.CreateAliasedBitmap();
			}
		}

		/// <summary>
		/// Renders the 32-bit bitmap to the HDC.
		/// </summary>
		/// <param name="gr">The Graphics object to render to.</param>
		/// <param name="dstCol">The column offset to render at.</param>
		/// <param name="dstRow">The row offset to render at.</param>
		/// <param name="allOpaque"><c>true</c> if the bitmap does not contain any transparency; otherwise, <c>false</c>.</param>
		/// <returns><see cref="PSError.noErr"/> on success; or any other PSError constant on failure.</returns>
		private short Display32BitBitmap(Graphics gr, int dstCol, int dstRow, bool allOpaque)
		{
			// Skip the rendering of the checker board if the surface does not contain any transparency.
			if (allOpaque)
			{
				using (Bitmap bmp = GetRGBPreviewBitmap())
				{
					gr.DrawImageUnscaled(bmp, dstCol, dstRow);
				}
			}
			else
			{
				int width = displaySurface.Width;
				int height = displaySurface.Height;

				try
				{
					if (checkerBoardBitmap == null)
					{
						DrawCheckerBoardBitmap();
					}

					// Use a temporary bitmap to prevent flickering when the image is rendered over the checker board.
					using (Bitmap temp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
					{
						Rectangle rect = new Rectangle(0, 0, width, height);

						using (Graphics tempGr = Graphics.FromImage(temp))
						{
							tempGr.DrawImageUnscaledAndClipped(checkerBoardBitmap, rect);
							using (Bitmap bmp = GetRGBPreviewBitmap())
							{
								tempGr.DrawImageUnscaled(bmp, rect);
							}
						}

						gr.DrawImageUnscaled(temp, dstCol, dstRow);
					}
				}
				catch (OutOfMemoryException)
				{
					return PSError.memFullErr;
				}
			}

			return PSError.noErr;
		}

		private unsafe short DisplayPixelsProc(ref PSPixelMap srcPixelMap, ref VRect srcRect, int dstRow, int dstCol, IntPtr platformContext)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DisplayPixels, string.Format("source: version = {0} bounds = {1}, ImageMode = {2}, colBytes = {3}, rowBytes = {4},planeBytes = {5}, BaseAddress = 0x{6}, mat = 0x{7}, masks = 0x{8}", new object[]{ srcPixelMap.version, srcPixelMap.bounds, ((ImageModes)srcPixelMap.imageMode).ToString("G"),
				srcPixelMap.colBytes, srcPixelMap.rowBytes, srcPixelMap.planeBytes, srcPixelMap.baseAddr.ToHexString(), srcPixelMap.mat.ToHexString(), srcPixelMap.masks.ToHexString()}));
			DebugUtils.Ping(DebugFlags.DisplayPixels, string.Format("srcRect = {0} dstCol (x, width) = {1}, dstRow (y, height) = {2}", srcRect, dstCol, dstRow));
#endif

			if (platformContext == IntPtr.Zero || srcPixelMap.rowBytes == 0 || srcPixelMap.baseAddr == IntPtr.Zero ||
				(srcPixelMap.imageMode != PSConstants.plugInModeRGBColor && srcPixelMap.imageMode != PSConstants.plugInModeGrayScale))
			{
				return PSError.filterBadParameters;
			}

			int width = srcRect.right - srcRect.left;
			int height = srcRect.bottom - srcRect.top;

			bool hasTransparencyMask = srcPixelMap.version >= 1 && srcPixelMap.masks != IntPtr.Zero;

			try
			{
				SetupDisplaySurface(width, height, hasTransparencyMask, srcPixelMap.imageMode);
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			byte* baseAddr = (byte*)srcPixelMap.baseAddr.ToPointer();

			int top = srcRect.top;
			int bottom = srcRect.bottom;
			int left = srcRect.left;
			// Some plug-ins set the srcRect incorrectly for 100% zoom.
			if (srcPixelMap.bounds.Equals(srcRect) && (top > 0 || left > 0))
			{
				top = left = 0;
				bottom = height;
			}

			if (srcPixelMap.imageMode == PSConstants.plugInModeGrayScale)
			{
				// Perform color correction if required and fall back to the uncorrected data if it fails.
				if (!colorProfileConverter.ColorCorrectionRequired || !colorProfileConverter.ColorCorrectGrayScale(srcPixelMap.baseAddr, srcPixelMap.rowBytes, displaySurface))
				{
					for (int y = top; y < bottom; y++)
					{
						byte* src = baseAddr + (y * srcPixelMap.rowBytes) + left;
						byte* dst = displaySurface.GetRowAddressUnchecked(y - top);

						for (int x = 0; x < width; x++)
						{
							dst[0] = dst[1] = dst[2] = *src;

							src += srcPixelMap.colBytes;
							dst += 4;
						}
					}
				}
			}
			else
			{
				if (srcPixelMap.colBytes == 1)
				{
					int greenPlaneOffset = srcPixelMap.planeBytes;
					int bluePlaneOffset = srcPixelMap.planeBytes * 2;
					for (int y = top; y < bottom; y++)
					{
						byte* redPlane = baseAddr + (y * srcPixelMap.rowBytes) + left;
						byte* greenPlane = redPlane + greenPlaneOffset;
						byte* bluePlane = redPlane + bluePlaneOffset;

						byte* dst = displaySurface.GetRowAddressUnchecked(y - top);

						for (int x = 0; x < width; x++)
						{
							dst[2] = *redPlane;
							dst[1] = *greenPlane;
							dst[0] = *bluePlane;

							redPlane++;
							greenPlane++;
							bluePlane++;
							dst += 4;
						}
					}
				}
				else
				{
					for (int y = top; y < bottom; y++)
					{
						byte* src = baseAddr + (y * srcPixelMap.rowBytes) + (left * srcPixelMap.colBytes);
						byte* dst = displaySurface.GetRowAddressUnchecked(y - top);

						for (int x = 0; x < width; x++)
						{
							dst[0] = src[2];
							dst[1] = src[1];
							dst[2] = src[0];

							src += srcPixelMap.colBytes;
							dst += 4;
						}
					}
				}
			}

			short error = PSError.noErr;
			using (Graphics gr = Graphics.FromHdc(platformContext))
			{
				if (srcPixelMap.imageMode == PSConstants.plugInModeRGBColor)
				{
					// Apply the transparency mask if present.
					if (hasTransparencyMask)
					{
						bool allOpaque = true;
						PSPixelMask* srcMask = (PSPixelMask*)srcPixelMap.masks.ToPointer();
						byte* maskData = (byte*)srcMask->maskData.ToPointer();

						for (int y = 0; y < height; y++)
						{
							byte* src = maskData + (y * srcMask->rowBytes);
							byte* dst = displaySurface.GetRowAddressUnchecked(y);

							for (int x = 0; x < width; x++)
							{
								dst[3] = *src;
								if (*src < 255)
								{
									allOpaque = false;
								}

								src += srcMask->colBytes;
								dst += 4;
							}
						}

						error = Display32BitBitmap(gr, dstCol, dstRow, allOpaque);
					}
					else
					{
						using (Bitmap bmp = GetRGBPreviewBitmap())
						{
							gr.DrawImageUnscaled(bmp, dstCol, dstRow);
						}
					}
				}
				else
				{
					using (Bitmap bmp = displaySurface.CreateAliasedBitmap())
					{
						gr.DrawImageUnscaled(bmp, dstCol, dstRow);
					}
				}
			}

			return error;
		}

		private unsafe void DrawCheckerBoardBitmap()
		{
			checkerBoardBitmap = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);

			BitmapData bd = checkerBoardBitmap.LockBits(new Rectangle(0, 0, checkerBoardBitmap.Width, checkerBoardBitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			try
			{
				byte* scan0 = (byte*)bd.Scan0.ToPointer();
				int stride = bd.Stride;

				for (int y = 0; y < checkerBoardBitmap.Height; y++)
				{
					byte* p = scan0 + (y * stride);
					for (int x = 0; x < checkerBoardBitmap.Width; x++)
					{
						byte v = (byte)((((x ^ y) & 8) * 8) + 191);

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

		private unsafe void DrawMask()
		{
			mask = new SurfaceGray8(source.Width, source.Height);

			SafeNativeMethods.memset(mask.Scan0.Pointer, 0, new UIntPtr((ulong)mask.Scan0.Length));

			Rectangle[] scans = this.selectedRegion.GetRegionScansReadOnlyInt();

			for (int i = 0; i < scans.Length; i++)
			{
				Rectangle rect = scans[i];

				for (int y = rect.Top; y < rect.Bottom; y++)
				{
					byte* ptr = mask.GetPointAddressUnchecked(rect.Left, y);
					byte* ptrEnd = ptr + rect.Width;

					while (ptr < ptrEnd)
					{
						*ptr = 255;
						ptr++;
					}
				}
			}
		}

		private unsafe void DrawFloatingSelectionMask()
		{
			int width = source.Width;
			int height = source.Height;
			mask = new SurfaceGray8(width, height);

			SafeNativeMethods.memset(mask.Scan0.Pointer, 0, new UIntPtr((ulong)mask.Scan0.Length));

			if (imageMode == ImageModes.RGB48)
			{
				for (int y = 0; y < height; y++)
				{
					ushort* src = (ushort*)source.GetRowAddressUnchecked(y);
					byte* dst = mask.GetRowAddressUnchecked(y);

					for (int x = 0; x < width; x++)
					{
						if (src[3] > 0)
						{
							*dst = 255;
						}

						src += 4;
						dst++;
					}
				}
			}
			else
			{
				for (int y = 0; y < height; y++)
				{
					byte* src = source.GetRowAddressUnchecked(y);
					byte* dst = mask.GetRowAddressUnchecked(y);

					for (int x = 0; x < width; x++)
					{
						if (src[3] > 0)
						{
							*dst = 255;
						}

						src += 4;
						dst++;
					}
				}
			}
		}

		private void HostProc(short selector, IntPtr data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.MiscCallbacks, string.Format("{0} : {1}", selector, data));
#endif
		}

#if USEIMAGESERVICES
		private short ImageServicesInterpolate1DProc(ref PSImagePlane source, ref PSImagePlane destination, ref Rect16 area, IntPtr coords, short method)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.ImageServices, string.Format("srcBounds: {0}, dstBounds: {1}, area: {2}, method: {3}", new object[] { source.bounds.ToString(), destination.bounds.ToString(), area.ToString(), ((InterpolationModes)method).ToString() }));
#endif
			return PSError.memFullErr;
		}

		private unsafe short ImageServicesInterpolate2DProc(ref PSImagePlane source, ref PSImagePlane destination, ref Rect16 area, IntPtr coords, short method)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.ImageServices, string.Format("srcBounds: {0}, dstBounds: {1}, area: {2}, method: {3}", new object[] { source.bounds.ToString(), destination.bounds.ToString(), area.ToString(), ((InterpolationModes)method).ToString() }));
#endif

			return PSError.memFullErr;
		}
#endif

		private void ProcessEvent(IntPtr @event)
		{
		}

		private void ProgressProc(int done, int total)
		{
			if (done < 0)
			{
				done = 0;
			}
#if DEBUG
			DebugUtils.Ping(DebugFlags.MiscCallbacks, string.Format("Done: {0}, Total: {1}, Progress: {2:N2} %", done, total, ((double)done / (double)total) * 100.0));
#endif
			if (progressFunc != null)
			{
				progressFunc.Invoke(done, total);
			}
		}

		private unsafe short PropertyGetProc(uint signature, uint key, int index, ref IntPtr simpleProperty, ref IntPtr complexProperty)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.PropertySuite, string.Format("Sig: {0}, Key: {1}, Index: {2}", DebugUtils.PropToString(signature), DebugUtils.PropToString(key), index));
#endif
			if (signature != PSConstants.kPhotoshopSignature)
			{
				return PSError.errPlugInPropertyUndefined;
			}

			byte[] bytes = null;

			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			short err = PSError.noErr;

			switch (key)
			{
				case PSProperties.BigNudgeH:
				case PSProperties.BigNudgeV:
					simpleProperty = new IntPtr(Int32ToFixed(PSConstants.Properties.BigNudgeDistance));
					break;
				case PSProperties.Caption:
					if (IPTCData.TryCreateCaptionRecord(hostInfo.Caption, out bytes))
					{
						complexProperty = HandleSuite.Instance.NewHandle(bytes.Length);

						if (complexProperty != IntPtr.Zero)
						{
							Marshal.Copy(bytes, 0, HandleSuite.Instance.LockHandle(complexProperty, 0), bytes.Length);
							HandleSuite.Instance.UnlockHandle(complexProperty);
						}
						else
						{
							err = PSError.memFullErr;
						}
					}
					else
					{
						if (complexProperty != IntPtr.Zero)
						{
							complexProperty = HandleSuite.Instance.NewHandle(0);
						}
					}
					break;
				case PSProperties.ChannelName:
					if (index < 0 || index > (filterRecord->planes - 1))
					{
						return PSError.errPlugInPropertyUndefined;
					}

					string name = string.Empty;

					if (imageMode == ImageModes.GrayScale || imageMode == ImageModes.Gray16)
					{
						switch (index)
						{
							case 0:
								name = Resources.GrayChannelName;
								break;
						}
					}
					else
					{
						switch (index)
						{
							case 0:
								name = Resources.RedChannelName;
								break;
							case 1:
								name = Resources.GreenChannelName;
								break;
							case 2:
								name = Resources.BlueChannelName;
								break;
							case 3:
								name = Resources.AlphaChannelName;
								break;
						}
					}

					bytes = Encoding.ASCII.GetBytes(name);

					complexProperty = HandleSuite.Instance.NewHandle(bytes.Length);
					if (complexProperty != IntPtr.Zero)
					{
						Marshal.Copy(bytes, 0, HandleSuite.Instance.LockHandle(complexProperty, 0), bytes.Length);
						HandleSuite.Instance.UnlockHandle(complexProperty);
					}
					else
					{
						err = PSError.memFullErr;
					}
					break;
				case PSProperties.Copyright:
				case PSProperties.Copyright2:
					simpleProperty = new IntPtr(hostInfo.Copyright ? 1 : 0);
					break;
				case PSProperties.EXIFData:
				case PSProperties.XMPData:
					if (imageMetaData.Extract(out bytes, key == PSProperties.EXIFData))
					{
						complexProperty = HandleSuite.Instance.NewHandle(bytes.Length);
						if (complexProperty != IntPtr.Zero)
						{
							Marshal.Copy(bytes, 0, HandleSuite.Instance.LockHandle(complexProperty, 0), bytes.Length);
							HandleSuite.Instance.UnlockHandle(complexProperty);
						}
						else
						{
							err = PSError.memFullErr;
						}
					}
					else
					{
						if (complexProperty != IntPtr.Zero)
						{
							// If the complexProperty is not IntPtr.Zero we return a valid zero byte handle, otherwise some filters will crash with an access violation.
							complexProperty = HandleSuite.Instance.NewHandle(0);
						}
					}
					break;
				case PSProperties.GridMajor:
					simpleProperty = new IntPtr(Int32ToFixed(PSConstants.Properties.GridMajor));
					break;
				case PSProperties.GridMinor:
					simpleProperty = new IntPtr(PSConstants.Properties.GridMinor);
					break;
				case PSProperties.ImageMode:
					simpleProperty = new IntPtr((int)filterRecord->imageMode);
					break;
				case PSProperties.InterpolationMethod:
					simpleProperty = new IntPtr(PSConstants.Properties.InterpolationMethod.NearestNeghbor);
					break;
				case PSProperties.NumberOfChannels:
					simpleProperty = new IntPtr(filterRecord->planes);
					break;
				case PSProperties.NumberOfPaths:
					simpleProperty = new IntPtr(0);
					break;
				case PSProperties.PathName:
					if (complexProperty != IntPtr.Zero)
					{
						complexProperty = HandleSuite.Instance.NewHandle(0);
					}
					break;
				case PSProperties.WorkPathIndex:
				case PSProperties.ClippingPathIndex:
				case PSProperties.TargetPathIndex:
					simpleProperty = new IntPtr(PSConstants.Properties.NoPathIndex);
					break;
				case PSProperties.RulerUnits:
					simpleProperty = new IntPtr((int)hostInfo.RulerUnit);
					break;
				case PSProperties.RulerOriginH:
				case PSProperties.RulerOriginV:
					simpleProperty = new IntPtr(Int32ToFixed(0));
					break;
				case PSProperties.Watermark:
					simpleProperty = new IntPtr(hostInfo.Watermark ? 1 : 0);
					break;
				case PSProperties.SerialString:
					bytes = Encoding.ASCII.GetBytes(filterRecord->serial.ToString(CultureInfo.InvariantCulture));
					complexProperty = HandleSuite.Instance.NewHandle(bytes.Length);

					if (complexProperty != IntPtr.Zero)
					{
						Marshal.Copy(bytes, 0, HandleSuite.Instance.LockHandle(complexProperty, 0), bytes.Length);
						HandleSuite.Instance.UnlockHandle(complexProperty);
					}
					else
					{
						err = PSError.memFullErr;
					}
					break;
				case PSProperties.URL:
					if (hostInfo.Url != null)
					{
						bytes = Encoding.ASCII.GetBytes(hostInfo.Url.ToString());
						complexProperty = HandleSuite.Instance.NewHandle(bytes.Length);

						if (complexProperty != IntPtr.Zero)
						{
							Marshal.Copy(bytes, 0, HandleSuite.Instance.LockHandle(complexProperty, 0), bytes.Length);
							HandleSuite.Instance.UnlockHandle(complexProperty);
						}
						else
						{
							err = PSError.memFullErr;
						}
					}
					else
					{
						if (complexProperty != IntPtr.Zero)
						{
							complexProperty = HandleSuite.Instance.NewHandle(0);
						}
					}
					break;
				case PSProperties.Title:
				case PSProperties.UnicodeTitle:
					string title;
					if (!string.IsNullOrEmpty(hostInfo.Title))
					{
						title = hostInfo.Title;
					}
					else
					{
						title = "temp.png";
					}

					if (key == PSProperties.UnicodeTitle)
					{
						bytes = Encoding.Unicode.GetBytes(title);
					}
					else
					{
						bytes = Encoding.ASCII.GetBytes(title);
					}
					complexProperty = HandleSuite.Instance.NewHandle(bytes.Length);

					if (complexProperty != IntPtr.Zero)
					{
						Marshal.Copy(bytes, 0, HandleSuite.Instance.LockHandle(complexProperty, 0), bytes.Length);
						HandleSuite.Instance.UnlockHandle(complexProperty);
					}
					else
					{
						err = PSError.memFullErr;
					}

					break;
				case PSProperties.WatchSuspension:
					simpleProperty = new IntPtr(0);
					break;
				case PSProperties.DocumentWidth:
					simpleProperty = new IntPtr(source.Width);
					break;
				case PSProperties.DocumentHeight:
					simpleProperty = new IntPtr(source.Height);
					break;
				case PSProperties.ToolTips:
					simpleProperty = new IntPtr(1);
					break;
				case PSProperties.HighDPI:
					simpleProperty = new IntPtr(hostInfo.HighDpi ? 1 : 0);
					break;

				default:
					return PSError.errPlugInPropertyUndefined;
			}

			return err;
		}

		private short PropertySetProc(uint signature, uint key, int index, IntPtr simpleProperty, IntPtr complexProperty)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.PropertySuite, string.Format("Sig: {0}, Key: {1}, Index: {2}", DebugUtils.PropToString(signature), DebugUtils.PropToString(key), index));
#endif
			if (signature != PSConstants.kPhotoshopSignature)
			{
				return PSError.errPlugInPropertyUndefined;
			}

			int size = 0;
			byte[] bytes = null;

			int simple = simpleProperty.ToInt32();

			switch (key)
			{
				case PSProperties.BigNudgeH:
					break;
				case PSProperties.BigNudgeV:
					break;
				case PSProperties.Caption:
					size = HandleSuite.Instance.GetHandleSize(complexProperty);
					if (size > 0)
					{
						string caption = IPTCData.CaptionFromMemory(HandleSuite.Instance.LockHandle(complexProperty, 0));
						HandleSuite.Instance.UnlockHandle(complexProperty);

						if (!string.IsNullOrEmpty(caption))
						{
							hostInfo.Caption = caption;
						}
					}
					break;
				case PSProperties.Copyright:
				case PSProperties.Copyright2:
					hostInfo.Copyright = simple != 0;
					break;
				case PSProperties.EXIFData:
				case PSProperties.XMPData:
					break;
				case PSProperties.GridMajor:
					break;
				case PSProperties.GridMinor:
					break;
				case PSProperties.RulerOriginH:
					break;
				case PSProperties.RulerOriginV:
					break;
				case PSProperties.URL:
					size = HandleSuite.Instance.GetHandleSize(complexProperty);
					if (size > 0)
					{
						bytes = new byte[size];
						Marshal.Copy(HandleSuite.Instance.LockHandle(complexProperty, 0), bytes, 0, size);
						HandleSuite.Instance.UnlockHandle(complexProperty);

						Uri temp;
						if (Uri.TryCreate(Encoding.ASCII.GetString(bytes, 0, size), UriKind.Absolute, out temp))
						{
							hostInfo.Url = temp;
						}
					}
					break;
				case PSProperties.WatchSuspension:
					break;
				case PSProperties.Watermark:
					hostInfo.Watermark = simple != 0;
					break;
				default:
					return PSError.errPlugInPropertyUndefined;
			}

			return PSError.noErr;
		}

		private int SPBasicAcquireSuite(IntPtr name, int version, ref IntPtr suite)
		{

			string suiteName = Marshal.PtrToStringAnsi(name);
			if (name == null)
			{
				return PSError.kSPBadParameterError;
			}
#if DEBUG
			DebugUtils.Ping(DebugFlags.SPBasicSuite, string.Format("name: {0}, version: {1}", suiteName, version));
#endif
			int error = PSError.kSPNoError;
			ActivePICASuites.PICASuiteKey suiteKey = new ActivePICASuites.PICASuiteKey(suiteName, version);

			if (activePICASuites.IsLoaded(suiteKey))
			{
				suite = this.activePICASuites.AddRef(suiteKey);
			}
			else
			{
				error = AllocatePICASuite(suiteKey, ref suite);
			}

			return error;
		}

		private unsafe int AllocatePICASuite(ActivePICASuites.PICASuiteKey suiteKey, ref IntPtr suite)
		{
			try
			{
				string suiteName = suiteKey.Name;
				int version = suiteKey.Version;

				if (suiteName.Equals(PSConstants.PICA.BufferSuite, StringComparison.Ordinal))
				{
					if (version != 1)
					{
						return PSError.kSPSuiteNotFoundError;
					}

					PSBufferSuite1 bufferSuite = this.picaSuites.CreateBufferSuite1();

					suite = this.activePICASuites.AllocateSuite(suiteKey, bufferSuite);
				}
				else if (suiteName.Equals(PSConstants.PICA.HandleSuite, StringComparison.Ordinal))
				{
					if (version == 1)
					{
						PSHandleSuite1 handleSuite = PICASuites.CreateHandleSuite1((HandleProcs*)handleProcsPtr.ToPointer());

						suite = this.activePICASuites.AllocateSuite(suiteKey, handleSuite);
					}
					else if (version == 2)
					{
						PSHandleSuite2 handleSuite = PICASuites.CreateHandleSuite2((HandleProcs*)handleProcsPtr.ToPointer());

						suite = this.activePICASuites.AllocateSuite(suiteKey, handleSuite);
					}
					else
					{
						return PSError.kSPSuiteNotFoundError;
					}
				}
				else if (suiteName.Equals(PSConstants.PICA.PropertySuite, StringComparison.Ordinal))
				{
					if (version != PSConstants.kCurrentPropertyProcsVersion)
					{
						return PSError.kSPSuiteNotFoundError;
					}

					PropertyProcs propertySuite = PICASuites.CreatePropertySuite((PropertyProcs*)propertyProcsPtr.ToPointer());

					suite = this.activePICASuites.AllocateSuite(suiteKey, propertySuite);
				}
				else if (suiteName.Equals(PSConstants.PICA.UIHooksSuite, StringComparison.Ordinal))
				{
					if (version != 1)
					{
						return PSError.kSPSuiteNotFoundError;
					}

					PSUIHooksSuite1 uiHooks = this.picaSuites.CreateUIHooksSuite1((FilterRecord*)filterRecordPtr.ToPointer());

					suite = this.activePICASuites.AllocateSuite(suiteKey, uiHooks);
				}
				else if (suiteName.Equals(PSConstants.PICA.ActionDescriptorSuite, StringComparison.Ordinal))
				{
					if (version != 2)
					{
						return PSError.kSPSuiteNotFoundError;
					}
					if (actionDescriptorSuite == null)
					{
						if (actionReferenceSuite == null)
						{
							this.actionReferenceSuite = new ActionReferenceSuite();
						}
						if (actionListSuite == null)
						{
							this.actionListSuite = new ActionListSuite(this.actionReferenceSuite);
						}
						this.actionDescriptorSuite = new ActionDescriptorSuite(this.descriptorSuite.Aete, this.actionListSuite, this.actionReferenceSuite);
						this.actionListSuite.ActionDescriptorSuite = this.actionDescriptorSuite;
						this.descriptorRegistrySuite.ActionDescriptorSuite = this.actionDescriptorSuite;
						if (scriptingData != null)
						{
							PIDescriptorParameters* descriptorParameters = (PIDescriptorParameters*)descriptorParametersPtr.ToPointer();
							this.actionDescriptorSuite.SetScriptingData(descriptorParameters->descriptor, scriptingData);
						}
					}

					PSActionDescriptorProc actionDescriptor = this.actionDescriptorSuite.CreateActionDescriptorSuite2();
					suite = this.activePICASuites.AllocateSuite(suiteKey, actionDescriptor);
				}
				else if (suiteName.Equals(PSConstants.PICA.ActionListSuite, StringComparison.Ordinal))
				{
					if (version != 1)
					{
						return PSError.kSPSuiteNotFoundError;
					}
					if (actionListSuite == null)
					{
						if (actionReferenceSuite == null)
						{
							this.actionReferenceSuite = new ActionReferenceSuite();
						}
						this.actionListSuite = new ActionListSuite(this.actionReferenceSuite);
					}

					PSActionListProcs listSuite = this.actionListSuite.CreateActionListSuite1();
					suite = this.activePICASuites.AllocateSuite(suiteKey, listSuite);
				}
				else if (suiteName.Equals(PSConstants.PICA.ActionReferenceSuite, StringComparison.Ordinal))
				{
					if (version != 2)
					{
						return PSError.kSPSuiteNotFoundError;
					}
					if (actionReferenceSuite == null)
					{
						this.actionReferenceSuite = new ActionReferenceSuite();
					}

					PSActionReferenceProcs referenceSuite = this.actionReferenceSuite.CreateActionReferenceSuite2();
					suite = this.activePICASuites.AllocateSuite(suiteKey, referenceSuite);
				}
				else if (suiteName.Equals(PSConstants.PICA.ASZStringSuite, StringComparison.Ordinal))
				{
					if (version != 1)
					{
						return PSError.kSPSuiteNotFoundError;
					}

					ASZStringSuite1 stringSuite = PICASuites.CreateASZStringSuite1();
					suite = this.activePICASuites.AllocateSuite(suiteKey, stringSuite);
				}
				else if (suiteName.Equals(PSConstants.PICA.ColorSpaceSuite, StringComparison.Ordinal))
				{
					if (version != 1)
					{
						return PSError.kSPSuiteNotFoundError;
					}

					PSColorSpaceSuite1 csSuite = this.picaSuites.CreateColorSpaceSuite1();

					suite = this.activePICASuites.AllocateSuite(suiteKey, csSuite);
				}
				else if (suiteName.Equals(PSConstants.PICA.DescriptorRegistrySuite, StringComparison.Ordinal))
				{
					if (version != 1)
					{
						return PSError.kSPSuiteNotFoundError;
					}

					PSDescriptorRegistryProcs registrySuite = this.descriptorRegistrySuite.CreateDescriptorRegistrySuite1();

					suite = this.activePICASuites.AllocateSuite(suiteKey, registrySuite);
				}
				else if (suiteName.Equals(PSConstants.PICA.ErrorSuite, StringComparison.Ordinal))
				{
					if (version != 1)
					{
						return PSError.kSPSuiteNotFoundError;
					}
					if (errorSuite == null)
					{
						this.errorSuite = new ErrorSuite();
					}

					PSErrorSuite1 errorProcs = this.errorSuite.CreateErrorSuite1();
					suite = this.activePICASuites.AllocateSuite(suiteKey, errorProcs);
				}
#if PICASUITEDEBUG
				else if (suiteName.Equals(PSConstants.PICA.SPPluginsSuite, StringComparison.Ordinal))
				{
					if (version != 4)
					{
						return PSError.kSPSuiteNotFoundError;
					}

					SPPluginsSuite4 plugs = PICASuites.CreateSPPlugs4();

					suite = this.activePICASuites.AllocateSuite(suiteKey, plugs);
				}
#endif
				else
				{
					return PSError.kSPSuiteNotFoundError;
				}
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.kSPNoError;
		}

		private int SPBasicReleaseSuite(IntPtr name, int version)
		{
			string suiteName = Marshal.PtrToStringAnsi(name);

#if DEBUG
			DebugUtils.Ping(DebugFlags.SPBasicSuite, string.Format("name: {0}, version: {1}", suiteName, version.ToString()));
#endif

			ActivePICASuites.PICASuiteKey suiteKey = new ActivePICASuites.PICASuiteKey(suiteName, version);

			this.activePICASuites.Release(suiteKey);

			return PSError.kSPNoError;
		}

		private unsafe int SPBasicIsEqual(IntPtr token1, IntPtr token2)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.SPBasicSuite, string.Format("token1: {0}, token2: {1}", Marshal.PtrToStringAnsi(token1), Marshal.PtrToStringAnsi(token2)));
#endif
			if (token1 == IntPtr.Zero)
			{
				if (token2 == IntPtr.Zero)
				{
					return 1;
				}

				return 0;
			}
			else if (token2 == IntPtr.Zero)
			{
				return 0;
			}

			// Compare two null-terminated ASCII strings for equality.
			byte* src = (byte*)token1.ToPointer();
			byte* dst = (byte*)token2.ToPointer();

			while (*dst != 0)
			{
				if ((*src - *dst) != 0)
				{
					return 0;
				}
				src++;
				dst++;
			}

			return 1;
		}

		private int SPBasicAllocateBlock(int size, ref IntPtr block)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.SPBasicSuite, string.Format("size: {0}", size));
#endif
			try
			{
				block = Memory.Allocate(size, false);
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.kSPNoError;
		}

		private int SPBasicFreeBlock(IntPtr block)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.SPBasicSuite, string.Format("block: 0x{0}", block.ToHexString()));
#endif
			Memory.Free(block);
			return PSError.kSPNoError;
		}

		private int SPBasicReallocateBlock(IntPtr block, int newSize, ref IntPtr newblock)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.SPBasicSuite, string.Format("block: 0x{0}, size: {1}", block.ToHexString(), newSize));
#endif
			try
			{
				newblock = Memory.ReAlloc(block, newSize);
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.kSPNoError;
		}

		private int SPBasicUndefined()
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.SPBasicSuite, string.Empty);
#endif

			return PSError.kSPNoError;
		}

		/// <summary>
		/// Converts an Int32 to a 16.16 fixed point value.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>The value converted to a 16.16 fixed point number.</returns>
		private static int Int32ToFixed(int value)
		{
			return (value << 16);
		}

		/// <summary>
		/// Converts a 16.16 fixed point value to an Int32.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>The value converted from a 16.16 fixed point number.</returns>
		private static int FixedToInt32(int value)
		{
			return (value >> 16);
		}

		/// <summary>
		/// Setup the FilterRecord image size data.
		/// </summary>
		private unsafe void SetupSizes()
		{
			if (sizesSetup)
			{
				return;
			}

			sizesSetup = true;

			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			short width = (short)source.Width;
			short height = (short)source.Height;

			filterRecord->imageSize.h = width;
			filterRecord->imageSize.v = height;

			switch (imageMode)
			{
				case ImageModes.GrayScale:
				case ImageModes.Gray16:
					filterRecord->planes = 1;
					break;
				case ImageModes.RGB:
				case ImageModes.RGB48:
					filterRecord->planes = ignoreAlpha ? (short)3 : (short)4;
					break;
			}


			filterRecord->floatCoord.h = 0;
			filterRecord->floatCoord.v = 0;
			filterRecord->filterRect.left = 0;
			filterRecord->filterRect.top = 0;
			filterRecord->filterRect.right = width;
			filterRecord->filterRect.bottom = height;

			filterRecord->imageHRes = Int32ToFixed((int)(dpiX + 0.5));
			filterRecord->imageVRes = Int32ToFixed((int)(dpiY + 0.5));

			filterRecord->wholeSize.h = width;
			filterRecord->wholeSize.v = height;
		}

		/// <summary>
		/// Setup the delegates for this instance.
		/// </summary>
		private void SetupDelegates()
		{
			// Misc Callbacks
			advanceProc = new AdvanceStateProc(AdvanceStateProc);
			colorProc = new ColorServicesProc(ColorServicesProc);
			displayPixelsProc = new DisplayPixelsProc(DisplayPixelsProc);
			hostProc = new HostProcs(HostProc);
			processEventProc = new ProcessEventProc(ProcessEvent);
			progressProc = new ProgressProc(ProgressProc);
			abortProc = new TestAbortProc(AbortProc);
			// ImageServicesProc
#if USEIMAGESERVICES
			resample1DProc = new PIResampleProc(ImageServicesInterpolate1DProc);
			resample2DProc = new PIResampleProc(ImageServicesInterpolate2DProc);
#endif
			// ChannelPorts
			readPixelsProc = new ReadPixelsProc(ReadPixelsProc);
			writeBasePixelsProc = new WriteBasePixelsProc(WriteBasePixels);
			readPortForWritePortProc = new ReadPortForWritePortProc(ReadPortForWritePort);

			// PropertyProc
			getPropertyProc = new GetPropertyProc(PropertyGetProc);
			setPropertyProc = new SetPropertyProc(PropertySetProc);

			// SPBasicSuite
			spAcquireSuite = new SPBasicAcquireSuite(SPBasicAcquireSuite);
			spReleaseSuite = new SPBasicReleaseSuite(SPBasicReleaseSuite);
			spIsEqual = new SPBasicIsEqual(SPBasicIsEqual);
			spAllocateBlock = new SPBasicAllocateBlock(SPBasicAllocateBlock);
			spFreeBlock = new SPBasicFreeBlock(SPBasicFreeBlock);
			spReallocateBlock = new SPBasicReallocateBlock(SPBasicReallocateBlock);
			spUndefined = new SPBasicUndefined(SPBasicUndefined);
		}

		/// <summary>
		/// Setup the API suites used within the FilterRecord.
		/// </summary>
		private unsafe void SetupSuites()
		{
			bufferProcsPtr = BufferSuite.Instance.CreateBufferProcs();
			handleProcsPtr = HandleSuite.Instance.CreateHandleProcs();

#if USEIMAGESERVICES
			imageServicesProcsPtr = Memory.Allocate(Marshal.SizeOf(typeof(ImageServicesProcs)), true);
			ImageServicesProcs* imageServices = (ImageServicesProcs*)imageServicesProcsPtr.ToPointer();

			imageServices->imageServicesProcsVersion = PSConstants.kCurrentImageServicesProcsVersion;
			imageServices->numImageServicesProcs = PSConstants.kCurrentImageServicesProcsCount;
			imageServices->interpolate1DProc = Marshal.GetFunctionPointerForDelegate(resample1DProc);
			imageServices->interpolate2DProc = Marshal.GetFunctionPointerForDelegate(resample2DProc);
#endif

			if (useChannelPorts)
			{
				channelPortsPtr = Memory.Allocate(Marshal.SizeOf(typeof(ChannelPortProcs)), true);
				ChannelPortProcs* channelPorts = (ChannelPortProcs*)channelPortsPtr.ToPointer();
				channelPorts->channelPortProcsVersion = PSConstants.kCurrentChannelPortProcsVersion;
				channelPorts->numChannelPortProcs = PSConstants.kCurrentChannelPortProcsCount;
				channelPorts->readPixelsProc = Marshal.GetFunctionPointerForDelegate(readPixelsProc);
				channelPorts->writeBasePixelsProc = Marshal.GetFunctionPointerForDelegate(writeBasePixelsProc);
				channelPorts->readPortForWritePortProc = Marshal.GetFunctionPointerForDelegate(readPortForWritePortProc);

				CreateReadImageDocument();
			}
			else
			{
				channelPortsPtr = IntPtr.Zero;
				readDocumentPtr = IntPtr.Zero;
			}

			propertyProcsPtr = Memory.Allocate(Marshal.SizeOf(typeof(PropertyProcs)), true);
			PropertyProcs* propertyProcs = (PropertyProcs*)propertyProcsPtr.ToPointer();
			propertyProcs->propertyProcsVersion = PSConstants.kCurrentPropertyProcsVersion;
			propertyProcs->numPropertyProcs = PSConstants.kCurrentPropertyProcsCount;
			propertyProcs->getPropertyProc = Marshal.GetFunctionPointerForDelegate(getPropertyProc);
			propertyProcs->setPropertyProc = Marshal.GetFunctionPointerForDelegate(setPropertyProc);


			resourceProcsPtr = pseudoResourceSuite.CreateResourceProcs();
			readDescriptorPtr = descriptorSuite.CreateReadDescriptor();
			writeDescriptorPtr = descriptorSuite.CreateWriteDescriptor();

			descriptorParametersPtr = Memory.Allocate(Marshal.SizeOf(typeof(PIDescriptorParameters)), true);
			PIDescriptorParameters* descriptorParameters = (PIDescriptorParameters*)descriptorParametersPtr.ToPointer();
			descriptorParameters->descriptorParametersVersion = PSConstants.kCurrentDescriptorParametersVersion;
			descriptorParameters->readDescriptorProcs = readDescriptorPtr;
			descriptorParameters->writeDescriptorProcs = writeDescriptorPtr;

			if (showUI)
			{
				descriptorParameters->recordInfo = RecordInfo.plugInDialogOptional;
			}
			else
			{
				descriptorParameters->recordInfo = RecordInfo.plugInDialogNone;
			}


			if (scriptingData != null)
			{
				descriptorParameters->descriptor = HandleSuite.Instance.NewHandle(0);
				if (descriptorParameters->descriptor == IntPtr.Zero)
				{
					throw new OutOfMemoryException(Resources.OutOfMemoryError);
				}
				descriptorSuite.SetScriptingData(descriptorParameters->descriptor, scriptingData);
				if (showUI)
				{
					descriptorParameters->playInfo = PlayInfo.plugInDialogDisplay;
				}
				else
				{
					descriptorParameters->playInfo = PlayInfo.plugInDialogDontDisplay;
				}
			}
			else
			{
				descriptorParameters->playInfo = PlayInfo.plugInDialogDisplay;
			}

			basicSuitePtr = Memory.Allocate(Marshal.SizeOf(typeof(SPBasicSuite)), true);
			SPBasicSuite* basicSuite = (SPBasicSuite*)basicSuitePtr.ToPointer();
			basicSuite->acquireSuite = Marshal.GetFunctionPointerForDelegate(spAcquireSuite);
			basicSuite->releaseSuite = Marshal.GetFunctionPointerForDelegate(spReleaseSuite);
			basicSuite->isEqual = Marshal.GetFunctionPointerForDelegate(spIsEqual);
			basicSuite->allocateBlock = Marshal.GetFunctionPointerForDelegate(spAllocateBlock);
			basicSuite->freeBlock = Marshal.GetFunctionPointerForDelegate(spFreeBlock);
			basicSuite->reallocateBlock = Marshal.GetFunctionPointerForDelegate(spReallocateBlock);
			basicSuite->undefined = Marshal.GetFunctionPointerForDelegate(spUndefined);
		}

		/// <summary>
		/// Setup the filter record for this instance.
		/// </summary>
		private unsafe void SetupFilterRecord()
		{
			filterRecordPtr = Memory.Allocate(Marshal.SizeOf(typeof(FilterRecord)), true);
			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			filterRecord->serial = 0;
			filterRecord->abortProc = Marshal.GetFunctionPointerForDelegate(abortProc);
			filterRecord->progressProc = Marshal.GetFunctionPointerForDelegate(progressProc);
			filterRecord->parameters = IntPtr.Zero;

			filterRecord->background.red = (ushort)((backgroundColor[0] * 65535) / 255);
			filterRecord->background.green = (ushort)((backgroundColor[1] * 65535) / 255);
			filterRecord->background.blue = (ushort)((backgroundColor[2] * 65535) / 255);

			filterRecord->foreground.red = (ushort)((foregroundColor[0] * 65535) / 255);
			filterRecord->foreground.green = (ushort)((foregroundColor[1] * 65535) / 255);
			filterRecord->foreground.blue = (ushort)((foregroundColor[2] * 65535) / 255);

			// The backColor and foreColor fields are always in the native color space of the image. 
			if (imageMode == ImageModes.GrayScale || imageMode == ImageModes.Gray16)
			{
				const int redLuma = 19595;
				const int greenLuma = 38470;
				const int blueLuma = 7471;

				filterRecord->backColor[0] = (byte)((backgroundColor[0] * redLuma + backgroundColor[1] * greenLuma + backgroundColor[2] * blueLuma) >> 16);
				filterRecord->foreColor[0] = (byte)((foregroundColor[0] * redLuma + foregroundColor[1] * greenLuma + foregroundColor[2] * blueLuma) >> 16);
			}
			else
			{
				for (int i = 0; i < 4; i++)
				{
					filterRecord->backColor[i] = backgroundColor[i];
					filterRecord->foreColor[i] = foregroundColor[i];
				}
			}

			filterRecord->bufferSpace = BufferSuite.Instance.AvailableSpace;
			filterRecord->maxSpace = filterRecord->bufferSpace;

			filterRecord->hostSig = HostSignature;
			filterRecord->hostProcs = Marshal.GetFunctionPointerForDelegate(hostProc);
			filterRecord->platformData = platFormDataPtr;
			filterRecord->bufferProcs = bufferProcsPtr;
			filterRecord->resourceProcs = resourceProcsPtr;
			filterRecord->processEvent = Marshal.GetFunctionPointerForDelegate(processEventProc);
			filterRecord->displayPixels = Marshal.GetFunctionPointerForDelegate(displayPixelsProc);
			filterRecord->handleProcs = handleProcsPtr;
			// New in 3.0
			filterRecord->supportsDummyChannels = 0;
			filterRecord->supportsAlternateLayouts = 0;
			filterRecord->wantLayout = PSConstants.Layout.Traditional;
			filterRecord->filterCase = filterCase;
			filterRecord->dummyPlaneValue = -1;
			filterRecord->premiereHook = IntPtr.Zero;
			filterRecord->advanceState = Marshal.GetFunctionPointerForDelegate(advanceProc);

			filterRecord->supportsAbsolute = 1;
			filterRecord->wantsAbsolute = 0;
			filterRecord->getPropertyObsolete = Marshal.GetFunctionPointerForDelegate(getPropertyProc);
			filterRecord->cannotUndo = 0;
			filterRecord->supportsPadding = 1;
			filterRecord->inputPadding = PSConstants.Padding.plugInWantsErrorOnBoundsException; // default to the error case for filters that do not set the padding fields.
			filterRecord->outputPadding = PSConstants.Padding.plugInWantsErrorOnBoundsException;
			filterRecord->maskPadding = PSConstants.Padding.plugInWantsErrorOnBoundsException;
			filterRecord->samplingSupport = PSConstants.SamplingSupport.hostSupportsIntegralSampling;
			filterRecord->reservedByte = 0;
			filterRecord->inputRate = Int32ToFixed(1);
			filterRecord->maskRate = Int32ToFixed(1);
			filterRecord->colorServices = Marshal.GetFunctionPointerForDelegate(colorProc);
			// New in 3.0.4
#if USEIMAGESERVICES
			filterRecord->imageServicesProcs = imageServicesProcsPtr;
#else
			filterRecord->imageServicesProcs = IntPtr.Zero;
#endif
			filterRecord->propertyProcs = propertyProcsPtr;
			filterRecord->inTileHeight = (short)source.Height;
			filterRecord->inTileWidth = (short)source.Width;
			filterRecord->inTileOrigin.h = 0;
			filterRecord->inTileOrigin.v = 0;
			filterRecord->absTileHeight = filterRecord->inTileHeight;
			filterRecord->absTileWidth = filterRecord->inTileWidth;
			filterRecord->absTileOrigin.h = 0;
			filterRecord->absTileOrigin.v = 0;
			filterRecord->outTileHeight = filterRecord->inTileHeight;
			filterRecord->outTileWidth = filterRecord->inTileWidth;
			filterRecord->outTileOrigin.h = 0;
			filterRecord->outTileOrigin.v = 0;
			filterRecord->maskTileHeight = filterRecord->inTileHeight;
			filterRecord->maskTileWidth = filterRecord->inTileWidth;
			filterRecord->maskTileOrigin.h = 0;
			filterRecord->maskTileOrigin.v = 0;

			// New in 4.0
			filterRecord->descriptorParameters = descriptorParametersPtr;

			// The errorStringPtr value is used so the filters cannot corrupt the pointer that we release when the class is disposed. 
			this.errorStringPtr = Memory.Allocate(256L, true);
			filterRecord->errorString = this.errorStringPtr;

			filterRecord->channelPortProcs = channelPortsPtr;
			filterRecord->documentInfo = readDocumentPtr;
			// New in 5.0
			filterRecord->sSPBasic = basicSuitePtr;
			filterRecord->plugInRef = IntPtr.Zero;

			switch (imageMode)
			{
				case ImageModes.GrayScale:
				case ImageModes.RGB:
					filterRecord->depth = 8;
					break;

				case ImageModes.Gray16:
				case ImageModes.RGB48:
					filterRecord->depth = 16;
					break;
			}

			if (documentColorProfile != null)
			{
				filterRecord->iCCprofileData = HandleSuite.Instance.NewHandle(documentColorProfile.Length);
				if (filterRecord->iCCprofileData != IntPtr.Zero)
				{
					Marshal.Copy(documentColorProfile, 0, HandleSuite.Instance.LockHandle(filterRecord->iCCprofileData, 0), documentColorProfile.Length);
					HandleSuite.Instance.UnlockHandle(filterRecord->parameters);
					filterRecord->iCCprofileSize = documentColorProfile.Length;
				}
			}
			else
			{
				filterRecord->iCCprofileData = IntPtr.Zero;
				filterRecord->iCCprofileSize = 0;
			}
			filterRecord->canUseICCProfiles = 1;
		}

		#region IDisposable Members

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
		/// <see cref="LoadPsFilter"/> is reclaimed by garbage collection.
		/// </summary>
		~LoadPsFilter()
		{
			Dispose(false);
		}

		private unsafe void Dispose(bool disposing)
		{
			if (!disposed)
			{
				disposed = true;

				if (disposing)
				{
					if (module != null)
					{
						module.Dispose();
						module = null;
					}

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

					if (checkerBoardBitmap != null)
					{
						checkerBoardBitmap.Dispose();
						checkerBoardBitmap = null;
					}

					if (tempSurface != null)
					{
						tempSurface.Dispose();
						tempSurface = null;
					}

					if (mask != null)
					{
						mask.Dispose();
						mask = null;
					}

					if (tempMask != null)
					{
						tempMask.Dispose();
						tempMask = null;
					}

					if (selectedRegion != null)
					{
						selectedRegion.Dispose();
						selectedRegion = null;
					}

					if (displaySurface != null)
					{
						displaySurface.Dispose();
						displaySurface = null;
					}

					if (scaledChannelSurface != null)
					{
						scaledChannelSurface.Dispose();
						scaledChannelSurface = null;
					}

					if (ditheredChannelSurface != null)
					{
						ditheredChannelSurface.Dispose();
						ditheredChannelSurface = null;
					}

					if (scaledSelectionMask != null)
					{
						scaledSelectionMask.Dispose();
						scaledSelectionMask = null;
					}

					if (activePICASuites != null)
					{
						activePICASuites.Dispose();
						activePICASuites = null;
					}

					if (picaSuites != null)
					{
						picaSuites.Dispose();
						picaSuites = null;
					}

					if (imageMetaData != null)
					{
						imageMetaData.Dispose();
						imageMetaData = null;
					}

					if (colorCorrectedDisplaySurface != null)
					{
						colorCorrectedDisplaySurface.Dispose();
						colorCorrectedDisplaySurface = null;
					}

					if (colorProfileConverter != null)
					{
						colorProfileConverter.Dispose();
						colorProfileConverter = null;
					}
				}

				if (platFormDataPtr != IntPtr.Zero)
				{
					Memory.Free(platFormDataPtr);
					platFormDataPtr = IntPtr.Zero;
				}

				if (bufferProcsPtr != IntPtr.Zero)
				{
					Memory.Free(bufferProcsPtr);
					bufferProcsPtr = IntPtr.Zero;
				}

				if (handleProcsPtr != IntPtr.Zero)
				{
					Memory.Free(handleProcsPtr);
					handleProcsPtr = IntPtr.Zero;
				}

#if USEIMAGESERVICES
				if (imageServicesProcsPtr != IntPtr.Zero)
				{
					Memory.Free(imageServicesProcsPtr);
					imageServicesProcsPtr = IntPtr.Zero;
				}
#endif
				if (propertyProcsPtr != IntPtr.Zero)
				{
					Memory.Free(propertyProcsPtr);
					propertyProcsPtr = IntPtr.Zero;
				}

				if (resourceProcsPtr != IntPtr.Zero)
				{
					Memory.Free(resourceProcsPtr);
					resourceProcsPtr = IntPtr.Zero;
				}

				if (channelPortsPtr != IntPtr.Zero)
				{
					Memory.Free(channelPortsPtr);
					channelPortsPtr = IntPtr.Zero;
				}

				if (readDocumentPtr != IntPtr.Zero)
				{
					Memory.Free(readDocumentPtr);
					readDocumentPtr = IntPtr.Zero;
				}

				if (channelReadDescPtrs != null)
				{
					foreach (var item in channelReadDescPtrs)
					{
						Marshal.FreeHGlobal(item.name);
						Memory.Free(item.address);
					}
					channelReadDescPtrs = null;
				}

				if (descriptorParametersPtr != IntPtr.Zero)
				{
					PIDescriptorParameters* descParam = (PIDescriptorParameters*)descriptorParametersPtr.ToPointer();

					if (descParam->descriptor != IntPtr.Zero)
					{
						HandleSuite.Instance.UnlockHandle(descParam->descriptor);
						HandleSuite.Instance.DisposeHandle(descParam->descriptor);
						descParam->descriptor = IntPtr.Zero;
					}

					Memory.Free(descriptorParametersPtr);
					descriptorParametersPtr = IntPtr.Zero;
				}

				if (readDescriptorPtr != IntPtr.Zero)
				{
					Memory.Free(readDescriptorPtr);
					readDescriptorPtr = IntPtr.Zero;
				}

				if (writeDescriptorPtr != IntPtr.Zero)
				{
					Memory.Free(writeDescriptorPtr);
					writeDescriptorPtr = IntPtr.Zero;
				}

				if (errorStringPtr != IntPtr.Zero)
				{
					Memory.Free(errorStringPtr);
					errorStringPtr = IntPtr.Zero;
				}

				if (basicSuitePtr != IntPtr.Zero)
				{
					Memory.Free(basicSuitePtr);
					basicSuitePtr = IntPtr.Zero;
				}

				if (filterRecordPtr != IntPtr.Zero)
				{
					FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

					if (filterRecord->parameters != IntPtr.Zero)
					{
						if (parameterDataRestored && !HandleSuite.Instance.AllocatedBySuite(filterRecord->parameters))
						{
							if (filterParametersHandle != IntPtr.Zero)
							{
								if (globalParameters.ParameterDataExecutable)
								{
									Memory.FreeExecutable(filterParametersHandle, globalParameters.GetParameterDataBytes().Length);
								}
								else
								{
									Memory.Free(filterParametersHandle);
								}
							}
							Memory.Free(filterRecord->parameters);
						}
						else if (BufferSuite.Instance.AllocatedBySuite(filterRecord->parameters))
						{
							BufferSuite.Instance.FreeBuffer(filterRecord->parameters);
						}
						else
						{
							HandleSuite.Instance.UnlockHandle(filterRecord->parameters);
							HandleSuite.Instance.DisposeHandle(filterRecord->parameters);
						}
						filterRecord->parameters = IntPtr.Zero;
						filterParametersHandle = IntPtr.Zero;
					}

					if (inDataPtr != IntPtr.Zero)
					{
						Memory.Free(inDataPtr);
						inDataPtr = IntPtr.Zero;
						filterRecord->inData = IntPtr.Zero;
					}

					if (outDataPtr != IntPtr.Zero)
					{
						Memory.Free(outDataPtr);
						outDataPtr = IntPtr.Zero;
						filterRecord->outData = IntPtr.Zero;
					}

					if (maskDataPtr != IntPtr.Zero)
					{
						Memory.Free(maskDataPtr);
						maskDataPtr = IntPtr.Zero;
						filterRecord->maskData = IntPtr.Zero;
					}

					if (filterRecord->iCCprofileData != IntPtr.Zero)
					{
						HandleSuite.Instance.UnlockHandle(filterRecord->iCCprofileData);
						HandleSuite.Instance.DisposeHandle(filterRecord->iCCprofileData);
						filterRecord->iCCprofileData = IntPtr.Zero;
					}

					Memory.Free(filterRecordPtr);
					filterRecordPtr = IntPtr.Zero;
				}

				if (dataPtr != IntPtr.Zero)
				{
					if (pluginDataRestored && !HandleSuite.Instance.AllocatedBySuite(dataPtr))
					{
						if (pluginDataHandle != IntPtr.Zero)
						{
							if (globalParameters.PluginDataExecutable)
							{
								Memory.FreeExecutable(pluginDataHandle, globalParameters.GetPluginDataBytes().Length);
							}
							else
							{
								Memory.Free(pluginDataHandle);
							}
						}
						Memory.Free(dataPtr);
					}
					else if (BufferSuite.Instance.AllocatedBySuite(dataPtr))
					{
						BufferSuite.Instance.FreeBuffer(dataPtr);
					}
					else
					{
						HandleSuite.Instance.UnlockHandle(dataPtr);
						HandleSuite.Instance.DisposeHandle(dataPtr);
					}
					dataPtr = IntPtr.Zero;
					pluginDataHandle = IntPtr.Zero;
				}

				BufferSuite.Instance.FreeRemainingBuffers();
				HandleSuite.Instance.FreeRemainingHandles();
				BGRASurfaceMemory.DestroyHeap();
			}
		}

		#endregion
	}
}
