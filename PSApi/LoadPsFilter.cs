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
using System.Windows.Forms;
using PSFilterHostDll.BGRASurface;
using PSFilterHostDll.Properties;

#if !GDIPLUS
using System.Windows.Media.Imaging;
#endif

namespace PSFilterHostDll.PSApi
{
	internal sealed partial class LoadPsFilter : IDisposable
	{
#if DEBUG
		private static DebugFlags debugFlags;
		private static void Ping(DebugFlags flag, string message)
		{
			if ((debugFlags & flag) == flag)
			{
				System.Diagnostics.StackFrame sf = new System.Diagnostics.StackFrame(1);
				string name = sf.GetMethod().Name;
				System.Diagnostics.Debug.WriteLine(string.Format("Function: {0}, {1}\r\n", name, message));
			}
		}

		private static string PropToString(uint prop)
		{
			byte[] bytes = BitConverter.GetBytes(prop);
			return new string(new char[] { (char)bytes[3], (char)bytes[2], (char)bytes[1], (char)bytes[0] });
		}
#endif

		/// <summary>
		/// The Windows-1252 Western European encoding
		/// </summary>
		private static readonly Encoding Windows1252Encoding = Encoding.GetEncoding(1252);
		private static readonly char[] TrimChars = new char[] { ' ', '\0' };
		
		/// <summary>
		/// Reads a Pascal String into a string.
		/// </summary>
		/// <param name="PString">The PString to read.</param>
		/// <returns>The resulting string</returns>
		private static unsafe string StringFromPString(IntPtr PString)
		{
			if (PString == IntPtr.Zero)
			{
				return string.Empty;
			}
			byte* ptr = (byte*)PString.ToPointer();

			return StringFromPString(ptr);
		}

		/// <summary>
		/// Reads a Pascal String into a string.
		/// </summary>
		/// <param name="ptr">The PString to read.</param>
		/// <returns>The resulting string</returns>
		private static unsafe string StringFromPString(byte* ptr)
		{
			int length = (int)ptr[0];

			return new string((sbyte*)ptr, 1, length, Windows1252Encoding).Trim(TrimChars);
		}

		static bool RectNonEmpty(Rect16 rect)
		{
			return (rect.left < rect.right && rect.top < rect.bottom);
		}

		private static readonly int OTOFHandleSize = IntPtr.Size + 4;
		private const int OTOFSignature = 0x464f544f;
		private struct PSHandle
		{
			public IntPtr pointer;
			public int size;

			public static readonly int SizeOf = Marshal.SizeOf(typeof(PSHandle));
		}

		private struct ChannelDescPtrs
		{
			public IntPtr address;
			public IntPtr name;
		}
		
		private Dictionary<IntPtr, PSHandle> handles;
		private List<ChannelDescPtrs> channelReadDescPtrs;
		private List<IntPtr> bufferIDs;

		#region CallbackDelegates
		private AdvanceStateProc advanceProc;
		// BufferProcs
		private AllocateBufferProc allocProc;
		private FreeBufferProc freeProc;
		private LockBufferProc lockProc;
		private UnlockBufferProc unlockProc;
		private BufferSpaceProc spaceProc;
		// MiscCallbacks
		private ColorServicesProc colorProc;
		private DisplayPixelsProc displayPixelsProc;
		private HostProcs hostProc;
		private ProcessEventProc processEventProc;
		private ProgressProc progressProc;
		private TestAbortProc abortProc;
		// HandleProcs 
		private NewPIHandleProc handleNewProc;
		private DisposePIHandleProc handleDisposeProc;
		private GetPIHandleSizeProc handleGetSizeProc;
		private SetPIHandleSizeProc handleSetSizeProc;
		private LockPIHandleProc handleLockProc;
		private UnlockPIHandleProc handleUnlockProc;
		private RecoverSpaceProc handleRecoverSpaceProc;
		private DisposeRegularPIHandleProc handleDisposeRegularProc;
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
		// ResourceProcs
		private CountPIResourcesProc countResourceProc;
		private GetPIResourceProc getResourceProc;
		private DeletePIResourceProc deleteResourceProc;
		private AddPIResourceProc addResourceProc;

		// ReadDescriptorProcs
		private OpenReadDescriptorProc openReadDescriptorProc;
		private CloseReadDescriptorProc closeReadDescriptorProc;
		private GetKeyProc getKeyProc;
		private GetIntegerProc getIntegerProc;
		private GetFloatProc getFloatProc;
		private GetUnitFloatProc getUnitFloatProc;
		private GetBooleanProc getBooleanProc;
		private GetTextProc getTextProc;
		private GetAliasProc getAliasProc;
		private GetEnumeratedProc getEnumeratedProc;
		private GetClassProc getClassProc;
		private GetSimpleReferenceProc getSimpleReferenceProc;
		private GetObjectProc getObjectProc;
		private GetCountProc getCountProc;
		private GetStringProc getStringProc;
		private GetPinnedIntegerProc getPinnedIntegerProc;
		private GetPinnedFloatProc getPinnedFloatProc;
		private GetPinnedUnitFloatProc getPinnedUnitFloatProc;
		// WriteDescriptorProcs
		private OpenWriteDescriptorProc openWriteDescriptorProc;
		private CloseWriteDescriptorProc closeWriteDescriptorProc;
		private PutIntegerProc putIntegerProc;
		private PutFloatProc putFloatProc;
		private PutUnitFloatProc putUnitFloatProc;
		private PutBooleanProc putBooleanProc;
		private PutTextProc putTextProc;
		private PutAliasProc putAliasProc;
		private PutEnumeratedProc putEnumeratedProc;
		private PutClassProc putClassProc;
		private PutSimpleReferenceProc putSimpleReferenceProc;
		private PutObjectProc putObjectProc;
		private PutCountProc putCountProc;
		private PutStringProc putStringProc;
		private PutScopedClassProc putScopedClassProc;
		private PutScopedObjectProc putScopedObjectProc;
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

		private PluginAETE aete;
		private Dictionary<uint, AETEValue> aeteDict;
		private GlobalParameters globalParameters;
		private bool isRepeatEffect;

		private AbortFunc abortFunc;
		private ProgressProc progressFunc;
		private PickColor pickColor;

		private SurfaceBase source;
		private SurfaceBase dest;
		private Surface8 mask;
		private Surface32 tempDisplaySurface;
		private Surface8 tempMask;
		private SurfaceBase tempSurface;
		private Bitmap checkerBoardBitmap;

		private PluginPhase phase;
		private PluginModule module;

		private IntPtr dataPtr;
		private short result;
		private string errorMessage;
		private List<PSResource> pseudoResources;
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
		private SurfaceBase convertedChannelSurface;
		private Surface8 scaledSelectionMask;
		private ImageModes convertedChannelImageMode;

		private short descErr;
		private short descErrValue;
		private uint getKey;
		private int getKeyIndex;
		private List<uint> keys;
		private List<uint> subKeys;
		private bool isSubKey;
		private int subKeyIndex;
		private int subClassIndex;
		private Dictionary<uint, AETEValue> subClassDict;

		private bool disposed;
		private bool sizesSetup;
		private bool frValuesSetup;
		private bool copyToDest;
		private bool writesOutsideSelection;
		private bool useChannelPorts;
		private bool usePICASuites;
		private ActivePICASuites activePICASuites;

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
		/// The filter progress callback.
		/// </summary>
		internal void SetProgressFunc(ProgressProc value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}

			progressFunc = value;
		}

		/// <summary>
		/// The filter abort callback.
		/// </summary>
		internal void SetAbortFunc(AbortFunc value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}

			abortFunc = value;
		}

		internal void SetPickColor(PickColor value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}

			pickColor = value;
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
				return new ParameterData(this.globalParameters, this.aeteDict);
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
					this.aeteDict = value.ScriptingData;
				}
			}
		}
		/// <summary>
		/// Is the filter a repeat Effect.
		/// </summary>
		internal bool IsRepeatEffect
		{
			set
			{
				this.isRepeatEffect = value;
			}
		}

		internal List<PSResource> PseudoResources
		{
			get
			{
				return this.pseudoResources;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}

				this.pseudoResources = value;
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
			this.isRepeatEffect = false;
			this.globalParameters = new GlobalParameters();
			this.errorMessage = string.Empty;
			this.filterParametersHandle = IntPtr.Zero;
			this.pluginDataHandle = IntPtr.Zero;
			this.inputHandling = FilterDataHandling.None;
			this.outputHandling = FilterDataHandling.None;

			abortFunc = null;
			progressFunc = null;
			pickColor = null;
			this.pseudoResources = new List<PSResource>();
			this.handles = new Dictionary<IntPtr, PSHandle>();
			this.bufferIDs = new List<IntPtr>();

			this.keys = null;
			this.aete = null;
			this.aeteDict = new Dictionary<uint, AETEValue>();
			this.getKey = 0;
			this.getKeyIndex = 0;
			this.subKeys = null;
			this.subKeyIndex = 0;
			this.isSubKey = false;

			this.useChannelPorts = false;
			this.channelReadDescPtrs = new List<ChannelDescPtrs>();
			this.usePICASuites = false;
			this.activePICASuites = new ActivePICASuites();
			this.hostInfo = new HostInformation();

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
			debugFlags = DebugFlags.AdvanceState;
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
#endif
		}

		private bool IgnoreAlphaChannel(PluginData data)
		{
			if (filterCase < FilterCase.EditableTransparencyNoSelection)
			{
				return true; // Return true for the FlatImage cases as we do not have any transparency.
			}

			// Some filters do not handle the alpha channel correctly despite what their FilterInfo says.
			if ((data.FilterInfo == null) || data.Category == "Axion")
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

			int filterCaseIndex = this.filterCase - 1;

			// If the EditableTransparency cases are not supported use the other modes.
			if (data.FilterInfo[filterCaseIndex].inputHandling == FilterDataHandling.CantFilter)
			{
				// Use the FlatImage modes if the filter doesn't support the ProtectedTransparency cases or image does not have any transparency.

				if (data.FilterInfo[filterCaseIndex + 2].inputHandling == FilterDataHandling.CantFilter || !source.HasTransparency())
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

			FilterCaseInfo info = data.FilterInfo[this.filterCase - 1];
			this.inputHandling = info.inputHandling;
			this.outputHandling = info.outputHandling;

			return false;
		}

		/// <summary>
		/// Determines whether the specified pointer is not valid to read from.
		/// </summary>
		/// <param name="ptr">The pointer to check.</param>
		/// <returns>
		///   <c>true</c> if the pointer is invalid; otherwise, <c>false</c>.
		/// </returns>
		private static bool IsBadReadPtr(IntPtr ptr)
		{
			bool result = false;
			NativeStructs.MEMORY_BASIC_INFORMATION mbi = new NativeStructs.MEMORY_BASIC_INFORMATION();
			int mbiSize = Marshal.SizeOf(typeof(NativeStructs.MEMORY_BASIC_INFORMATION));

			if (SafeNativeMethods.VirtualQuery(ptr, ref mbi, new UIntPtr((ulong)mbiSize)) == UIntPtr.Zero)
			{
				return true;
			}

			result = ((mbi.Protect & NativeConstants.PAGE_READONLY) != 0 || (mbi.Protect & NativeConstants.PAGE_READWRITE) != 0 || (mbi.Protect & NativeConstants.PAGE_WRITECOPY) != 0 ||
				(mbi.Protect & NativeConstants.PAGE_EXECUTE_READ) != 0 || (mbi.Protect & NativeConstants.PAGE_EXECUTE_READWRITE) != 0 || (mbi.Protect & NativeConstants.PAGE_EXECUTE_WRITECOPY) != 0);

			if ((mbi.Protect & NativeConstants.PAGE_GUARD) != 0 || (mbi.Protect & NativeConstants.PAGE_NOACCESS) != 0)
			{
				result = false;
			}

			return !result;
		}

		/// <summary>
		/// Determines whether the specified pointer is not valid to write to.
		/// </summary>
		/// <param name="ptr">The pointer to check.</param>
		/// <returns>
		///   <c>true</c> if the pointer is invalid; otherwise, <c>false</c>.
		/// </returns>
		private static bool IsBadWritePtr(IntPtr ptr)
		{
			bool result = false;
			NativeStructs.MEMORY_BASIC_INFORMATION mbi = new NativeStructs.MEMORY_BASIC_INFORMATION();
			int mbiSize = Marshal.SizeOf(typeof(NativeStructs.MEMORY_BASIC_INFORMATION));

			if (SafeNativeMethods.VirtualQuery(ptr, ref mbi, new UIntPtr((ulong)mbiSize)) == UIntPtr.Zero)
			{
				return true;
			}

			result = ((mbi.Protect & NativeConstants.PAGE_READWRITE) != 0 || (mbi.Protect & NativeConstants.PAGE_WRITECOPY) != 0 ||
				(mbi.Protect & NativeConstants.PAGE_EXECUTE_READWRITE) != 0 || (mbi.Protect & NativeConstants.PAGE_EXECUTE_WRITECOPY) != 0);

			if ((mbi.Protect & NativeConstants.PAGE_GUARD) != 0 || (mbi.Protect & NativeConstants.PAGE_NOACCESS) != 0)
			{
				result = false;
			}

			return !result;
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

			if (SafeNativeMethods.VirtualQuery(ptr, ref mbi, new UIntPtr((ulong)mbiSize)) == UIntPtr.Zero)
			{
				return false;
			}

			bool result = ((mbi.Protect & NativeConstants.PAGE_EXECUTE) != 0 || (mbi.Protect & NativeConstants.PAGE_EXECUTE_READ) != 0 || (mbi.Protect & NativeConstants.PAGE_EXECUTE_READWRITE) != 0 ||
			(mbi.Protect & NativeConstants.PAGE_EXECUTE_WRITECOPY) != 0);

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
		/// Save the filter parameters for repeat runs.
		/// </summary>
		private unsafe void SaveParameters()
		{
			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			if (filterRecord->parameters != IntPtr.Zero)
			{
				if (IsHandleValid(filterRecord->parameters))
				{
					int handleSize = HandleGetSizeProc(filterRecord->parameters);

					byte[] buf = new byte[handleSize];
					Marshal.Copy(HandleLockProc(filterRecord->parameters, 0), buf, 0, buf.Length);
					HandleUnlockProc(filterRecord->parameters);

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
									byte[] buf = new byte[ps];
									Marshal.Copy(hPtr, buf, 0, (int)ps);
									this.globalParameters.SetParameterDataBytes(buf);
									this.globalParameters.ParameterDataStorageMethod = GlobalParameters.DataStorageMethod.OTOFHandle;
									// Some filters may store executable code in the parameter block.
									this.globalParameters.ParameterDataExecutable = IsMemoryExecutable(hPtr);
								}

							}
							else
							{
								if (!IsBadReadPtr(hPtr))
								{
									int ps = SafeNativeMethods.GlobalSize(hPtr).ToInt32();
									if (ps == 0)
									{
										ps = ((int)size - IntPtr.Size); // Some plug-ins do not use the pointer to a pointer trick.
									}

									byte[] buf = new byte[ps];

									Marshal.Copy(hPtr, buf, 0, ps);
									this.globalParameters.SetParameterDataBytes(buf);
									this.globalParameters.ParameterDataStorageMethod = GlobalParameters.DataStorageMethod.HandleSuite;
								}
								else
								{
									byte[] buf = new byte[(int)size];

									Marshal.Copy(parameters, buf, 0, (int)size);
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

				if (!IsHandleValid(dataPtr))
				{
					if (bufferIDs.Contains(dataPtr))
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
					if (IsHandleValid(pluginData))
					{
						int ps = HandleGetSizeProc(pluginData);
						byte[] dataBuf = new byte[ps];

						Marshal.Copy(HandleLockProc(pluginData, 0), dataBuf, 0, ps);
						HandleUnlockProc(pluginData);

						this.globalParameters.SetPluginDataBytes(dataBuf);
						this.globalParameters.PluginDataStorageMethod = GlobalParameters.DataStorageMethod.HandleSuite;
					}
					else if (pluginDataSize == OTOFHandleSize && Marshal.ReadInt32(pluginData, IntPtr.Size) == OTOFSignature)
					{
						IntPtr hPtr = Marshal.ReadIntPtr(pluginData);
						long ps = SafeNativeMethods.GlobalSize(hPtr).ToInt64();
						if (ps > 0L)
						{
							byte[] dataBuf = new byte[ps];
							Marshal.Copy(hPtr, dataBuf, 0, (int)ps);
							this.globalParameters.SetPluginDataBytes(dataBuf);
							this.globalParameters.PluginDataStorageMethod = GlobalParameters.DataStorageMethod.OTOFHandle;
							this.globalParameters.PluginDataExecutable = IsMemoryExecutable(hPtr);
						}

					}
					else if (pluginDataSize > 0)
					{
						byte[] dataBuf = new byte[pluginDataSize];
						Marshal.Copy(pluginData, dataBuf, 0, (int)pluginDataSize);
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
		/// Restore the filter parameters for repeat runs.
		/// </summary>
		private unsafe void RestoreParameters()
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

						filterRecord->parameters = HandleNewProc(parameterDataBytes.Length);
						if (filterRecord->parameters == IntPtr.Zero)
						{
							throw new OutOfMemoryException(Resources.OutOfMemoryError);
						}

						Marshal.Copy(parameterDataBytes, 0, HandleLockProc(filterRecord->parameters, 0), parameterDataBytes.Length);
						HandleUnlockProc(filterRecord->parameters);
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
			}

			byte[] pluginDataBytes = globalParameters.GetPluginDataBytes();

			if (pluginDataBytes != null)
			{
				switch (globalParameters.PluginDataStorageMethod)
				{
					case GlobalParameters.DataStorageMethod.HandleSuite:

						dataPtr = HandleNewProc(pluginDataBytes.Length);
						if (dataPtr == IntPtr.Zero)
						{
							throw new OutOfMemoryException(Resources.OutOfMemoryError);
						}

						Marshal.Copy(pluginDataBytes, 0, HandleLockProc(dataPtr, 0), pluginDataBytes.Length);
						HandleUnlockProc(dataPtr);
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
			}

		}

		private static bool PluginAbout(PluginData pdata, IntPtr owner, out string error)
		{
			short result = PSError.noErr;
			error = string.Empty;

			PlatformData platform = new PlatformData()
			{
				hwnd = owner
			};

			using (PluginModule module = new PluginModule(pdata.FileName, pdata.EntryPoint))
			{
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
						if (pdata.moduleEntryPoints == null)
						{
							module.entryPoint(FilterSelector.About, aboutRecordHandle.AddrOfPinnedObject(), ref dataPtr, ref result);
						}
						else
						{
							// Otherwise call about on all the entry points in the module, per the SDK docs only one of the entry points will display the about box.
							foreach (var entryPoint in pdata.moduleEntryPoints)
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
				Ping(DebugFlags.Error, string.Format("filterSelectorAbout returned: {0}({1})", error, result));
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
			Ping(DebugFlags.Call, "Before FilterSelectorStart");
#endif

			module.entryPoint(FilterSelector.Start, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
			Ping(DebugFlags.Call, "After FilterSelectorStart");
#endif

			if (result != PSError.noErr)
			{
				errorMessage = GetErrorMessage(result);

#if DEBUG
				Ping(DebugFlags.Error, string.Format("filterSelectorStart returned: {0}({1})", string.IsNullOrEmpty(errorMessage) ? "User Canceled" : errorMessage, result));
#endif
				return false;
			}

			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			while (RectNonEmpty(filterRecord->inRect) || RectNonEmpty(filterRecord->outRect) || RectNonEmpty(filterRecord->maskRect))
			{
				AdvanceStateProc();
				result = PSError.noErr;

#if DEBUG
				Ping(DebugFlags.Call, "Before FilterSelectorContinue");
#endif

				module.entryPoint(FilterSelector.Continue, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
				Ping(DebugFlags.Call, "After FilterSelectorContinue");
#endif

				filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

				if (result != PSError.noErr)
				{
					short savedResult = result;
					result = PSError.noErr;

#if DEBUG
					Ping(DebugFlags.Call, "Before FilterSelectorFinish");
#endif

					module.entryPoint(FilterSelector.Finish, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
					Ping(DebugFlags.Call, "After FilterSelectorFinish");
#endif

					errorMessage = GetErrorMessage(savedResult);

#if DEBUG
					Ping(DebugFlags.Error, string.Format("filterSelectorContinue returned: {0}({1})", string.IsNullOrEmpty(errorMessage) ? "User Canceled" : errorMessage, savedResult));
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
			Ping(DebugFlags.Call, "Before FilterSelectorFinish");
#endif

			module.entryPoint(FilterSelector.Finish, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
			Ping(DebugFlags.Call, "After FilterSelectorFinish");
#endif

			if (!isRepeatEffect && result == PSError.noErr)
			{
				SaveParameters();
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
#if DEBUG
			Ping(DebugFlags.Call, "Before filterSelectorParameters");
#endif

			module.entryPoint(FilterSelector.Parameters, filterRecordPtr, ref dataPtr, ref result);
#if DEBUG
			unsafe
			{
				FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

				Ping(DebugFlags.Call, string.Format("data: 0x{0},  parameters: 0x{1}", dataPtr.ToHexString(), filterRecord->parameters.ToHexString()));
			}

			Ping(DebugFlags.Call, "After filterSelectorParameters");
#endif

			if (result != PSError.noErr)
			{
				errorMessage = GetErrorMessage(result);
#if DEBUG
				Ping(DebugFlags.Error, string.Format("filterSelectorParameters returned: {0}({1})", string.IsNullOrEmpty(errorMessage) ? "User Canceled" : errorMessage, result));
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

			filterRecord->isFloating = 0;

			if (selectedRegion != null)
			{
				DrawMask();
				filterRecord->haveMask = 1;
				filterRecord->autoMask = 1;
			}
			else
			{
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
			RestoreParameters();
			SetFilterRecordValues();


			result = PSError.noErr;


#if DEBUG
			Ping(DebugFlags.Call, "Before filterSelectorPrepare");
#endif
			module.entryPoint(FilterSelector.Prepare, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
			Ping(DebugFlags.Call, "After filterSelectorPrepare");
#endif

			if (result != PSError.noErr)
			{
				errorMessage = GetErrorMessage(result);
#if DEBUG
				Ping(DebugFlags.Error, string.Format("filterSelectorPrepare returned: {0}({1})", string.IsNullOrEmpty(errorMessage) ? "User Canceled" : errorMessage, result));
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

		private static bool EnablePICASuites(PluginData data)
		{
#if !PICASUITEDEBUG
			if (data.Category == "Nik Collection")
			{
				return true;
			}

			return false;
#else
			return true;
#endif
		}

		/// <summary>
		/// Runs a filter from the specified PluginData
		/// </summary>
		/// <param name="pdata">The PluginData to run</param>
		/// <returns>True if successful otherwise false</returns>
		internal bool RunPlugin(PluginData pdata)
		{
			LoadFilter(pdata);

			this.useChannelPorts = pdata.Category == "Amico Perry"; // enable the channel ports suite for Luce 2
			this.usePICASuites = EnablePICASuites(pdata);

			this.ignoreAlpha = IgnoreAlphaChannel(pdata);

			if (pdata.FilterInfo != null)
			{
				int index = this.filterCase - 1;
				this.copyToDest = ((pdata.FilterInfo[index].flags1 & FilterCaseInfoFlags.DontCopyToDestination) == FilterCaseInfoFlags.None);
				this.writesOutsideSelection = ((pdata.FilterInfo[index].flags1 & FilterCaseInfoFlags.WritesOutsideSelection) != FilterCaseInfoFlags.None);

				bool worksWithBlankData = ((pdata.FilterInfo[index].flags1 & FilterCaseInfoFlags.WorksWithBlankData) != FilterCaseInfoFlags.None);

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
			else
			{
				DrawCheckerBoardBitmap();
			}

			this.aete = pdata.Aete;

			SetupDelegates();
			SetupSuites();
			SetupFilterRecord();

			PreProcessInputData();

			if (!isRepeatEffect)
			{
				if (!PluginParameters())
				{
#if DEBUG
					Ping(DebugFlags.Error, "PluginParameters failed");
#endif
					return false;
				}
			}

			if (!PluginPrepare())
			{
#if DEBUG
				Ping(DebugFlags.Error, "PluginPrepare failed");
#endif
				return false;
			}

			if (!PluginApply())
			{
#if DEBUG
				Ping(DebugFlags.Error, "PluginApply failed");
#endif
				return false;
			}

			return true;
		}

		internal static bool ShowAboutDialog(PluginData pdata, IntPtr owner, out string errorMessage)
		{
			errorMessage = string.Empty;

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
					case PSError.errReportString:
						message = StringFromPString(this.errorStringPtr);
						break;
					default:
						message = GetMacOSErrorMessage(error);
						break;
				}
			}

			return message;
		}

		private bool AbortProc()
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Empty);
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
			Ping(DebugFlags.AdvanceState, string.Format("inRect: {0}, outRect: {1}, maskRect: {2}", filterRecord->inRect, filterRecord->outRect, filterRecord->maskRect));
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
			Ping(DebugFlags.AdvanceState, string.Format("inRowBytes: {0}, Rect: {1}, loplane: {2}, hiplane: {3}, inputRate: {4}", new object[] { filterRecord->inRowBytes, filterRecord->inRect, 
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
			Ping(DebugFlags.AdvanceState, string.Format("outRowBytes: {0}, Rect: {1}, loplane: {2}, hiplane: {3}", new object[] { filterRecord->outRowBytes, filterRecord->outRect, filterRecord->outLoPlane, 
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

					tempMask = new Surface8(mask.Width, mask.Height);
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
					tempMask = new Surface8(scaleWidth, scaleHeight);
					tempMask.SuperSampleFitSurface(mask);
				}
				else
				{
					tempMask = new Surface8(mask.Width, mask.Height);
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
			Ping(DebugFlags.AdvanceState, string.Format("maskRowBytes: {0}, Rect: {1}, maskRate: {2}", new object[] { filterRecord->maskRowBytes, filterRecord->maskRect, FixedToInt32(filterRecord->maskRate) }));
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
			Ping(DebugFlags.AdvanceState, string.Format("inRowBytes = {0}, Rect = {1}, loplane = {2}, hiplane = {3}", new object[] { outRowBytes.ToString(), rect.ToString(), loplane.ToString(), hiplane.ToString() }));
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

		private short AllocateBufferProc(int size, ref IntPtr bufferID)
		{
#if DEBUG
			Ping(DebugFlags.BufferSuite, string.Format("Size: {0}", size));
#endif
			short err = PSError.noErr;
			try
			{
				bufferID = Memory.Allocate(size, false);

				this.bufferIDs.Add(bufferID);
			}
			catch (OutOfMemoryException)
			{
				err = PSError.memFullErr;
			}

			return err;
		}

		private void BufferFreeProc(IntPtr bufferID)
		{
#if DEBUG
			Ping(DebugFlags.BufferSuite, string.Format("Buffer: 0x{0}, Size: {1}", bufferID.ToHexString(), Memory.Size(bufferID)));
#endif
			Memory.Free(bufferID);

			this.bufferIDs.Remove(bufferID);
		}

		private IntPtr BufferLockProc(IntPtr bufferID, byte moveHigh)
		{
#if DEBUG
			Ping(DebugFlags.BufferSuite, string.Format("Buffer: 0x{0}", bufferID.ToHexString()));
#endif

			return bufferID;
		}

		private void BufferUnlockProc(IntPtr bufferID)
		{
#if DEBUG
			Ping(DebugFlags.BufferSuite, string.Format("Buffer: 0x{0}", bufferID.ToHexString()));
#endif
		}

		private int BufferSpaceProc()
		{
			// Assume that we have 1 GB of available space.
			int space = 1024 * 1024 * 1024;

			NativeStructs.MEMORYSTATUSEX buffer = new NativeStructs.MEMORYSTATUSEX();
			buffer.dwLength = (uint)Marshal.SizeOf(typeof(NativeStructs.MEMORYSTATUSEX));

			if (SafeNativeMethods.GlobalMemoryStatusEx(ref buffer))
			{
				if (buffer.ullAvailVirtual < int.MaxValue)
				{
					space = (int)buffer.ullAvailVirtual;
				}
			}

			return space;
		}

		private bool ShowColorPickerDialog(string prompt, ref short[] rgb)
		{
			bool colorPicked = false;

			if (pickColor != null)
			{
				ColorPickerResult color = pickColor(prompt, (byte)rgb[0], (byte)rgb[1], (byte)rgb[2]);

				if (color != null)
				{
					rgb[0] = color.R;
					rgb[1] = color.G;
					rgb[2] = color.B;
					colorPicked = true;
				}
			}
			else
			{
				using (ColorPicker picker = new ColorPicker(prompt))
				{
					picker.Color = Color.FromArgb(rgb[0], rgb[1], rgb[2]);

					if (picker.ShowDialog() == DialogResult.OK)
					{
						Color color = picker.Color;
						rgb[0] = color.R;
						rgb[1] = color.G;
						rgb[2] = color.B;
						colorPicked = true;
					}
				}
			}

			return colorPicked;
		}

		private unsafe short ColorServicesProc(ref ColorServicesInfo info)
		{
#if DEBUG
			Ping(DebugFlags.ColorServices, string.Format("selector: {0}", info.selector));
#endif
			short err = PSError.noErr;
			switch (info.selector)
			{
				case ColorServicesSelector.ChooseColor:

					string prompt = StringFromPString(info.selectorParameter.pickerPrompt);

					if (info.sourceSpace != ColorSpace.RGBSpace)
					{
						err = ColorServicesConvert.Convert(info.sourceSpace, ColorSpace.RGBSpace, ref info.colorComponents);

						if (err != PSError.noErr)
						{
							return err;
						}
					}

					if (ShowColorPickerDialog(prompt, ref info.colorComponents))
					{
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
						if (imageMode == ImageModes.Gray16 || imageMode == ImageModes.RGB48)
						{
							// As this function only handles 8-bit data return 255 if the source image is 16-bit, same as Adobe(R) Photoshop(R).
							if (imageMode == ImageModes.Gray16)
							{
								info.colorComponents[0] = 255;
								info.colorComponents[1] = 0;
								info.colorComponents[2] = 0;
								info.colorComponents[3] = 0;
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

							ColorSpace sourceSpace = ColorSpace.RGBSpace;

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
							
							err = ColorServicesConvert.Convert(sourceSpace, info.resultSpace, ref info.colorComponents);
						}
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

		private static unsafe void FillSelectionMask(PixelMemoryDesc destiniation, Surface8 source, VRect srcRect)
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

		private unsafe short ReadPixelsProc(IntPtr port, ref PSScaling scaling, ref VRect writeRect, ref PixelMemoryDesc destination, ref VRect wroteRect)
		{
#if DEBUG
			Ping(DebugFlags.ChannelPorts, string.Format("port: {0}, rect: {1}", port.ToString(), writeRect.ToString()));
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
							scaledSelectionMask = new Surface8(dstWidth, dstHeight);
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
							scaledSelectionMask = new Surface8(dstWidth, dstHeight);
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
				SurfaceBase temp = null;
				ImageModes tempMode = this.imageMode;

				if (source.BitsPerChannel == 16 && destination.depth == 8)
				{
					if (convertedChannelSurface == null)
					{
						int width = source.Width;
						int height = source.Height;

						try
						{
							switch (imageMode)
							{
								case ImageModes.Gray16:

									convertedChannelImageMode = ImageModes.GrayScale;
									convertedChannelSurface = SurfaceFactory.CreateFromImageMode(width, height, convertedChannelImageMode);

									for (int y = 0; y < height; y++)
									{
										ushort* src = (ushort*)source.GetRowAddressUnchecked(y);
										byte* dst = convertedChannelSurface.GetRowAddressUnchecked(y);
										for (int x = 0; x < width; x++)
										{
											*dst = (byte)((*src * 10) / 1285);

											src++;
											dst++;
										}
									}
									break;

								case ImageModes.RGB48:

									convertedChannelImageMode = ImageModes.RGB;
									convertedChannelSurface = SurfaceFactory.CreateFromImageMode(width, height, convertedChannelImageMode);

									for (int y = 0; y < height; y++)
									{
										ushort* src = (ushort*)source.GetRowAddressUnchecked(y);
										byte* dst = convertedChannelSurface.GetRowAddressUnchecked(y);
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



#if DEBUG
						using (Bitmap bmp = convertedChannelSurface.CreateAliasedBitmap())
						{
						}
#endif
					}

					temp = convertedChannelSurface;
					tempMode = convertedChannelImageMode;
				}
				else
				{
					temp = source;
				}

				if (srcWidth == dstWidth && srcHeight == dstHeight)
				{
					FillChannelData(channel, destination, temp, srcRect, tempMode);
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
							scaledChannelSurface = SurfaceFactory.CreateFromImageMode(dstWidth, dstHeight, tempMode);
							scaledChannelSurface.SuperSampleFitSurface(temp);
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

					FillChannelData(channel, destination, scaledChannelSurface, dstRect, tempMode);
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
							scaledChannelSurface = SurfaceFactory.CreateFromImageMode(dstWidth, dstHeight, tempMode);
							scaledChannelSurface.BicubicFitSurface(temp);
						}
						catch (OutOfMemoryException)
						{
							return PSError.memFullErr;
						}
					}

					FillChannelData(channel, destination, scaledChannelSurface, dstRect, tempMode);
				}
			}


			wroteRect = dstRect;

			return PSError.noErr;
		}

		private short WriteBasePixels(IntPtr port, ref VRect writeRect, PixelMemoryDesc srcDesc)
		{
#if DEBUG
			Ping(DebugFlags.ChannelPorts, string.Format("port: {0}, rect: {1}", port.ToString(), writeRect.ToString()));
#endif
			return PSError.memFullErr;
		}

		private short ReadPortForWritePort(ref IntPtr readPort, IntPtr writePort)
		{
#if DEBUG
			Ping(DebugFlags.ChannelPorts, string.Format("readPort: {0}, writePort: {1}", readPort.ToString(), writePort.ToString()));
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
			}
			catch (Exception)
			{
				Memory.Free(addressPtr);
				throw;
			}
			channelReadDescPtrs.Add(new ChannelDescPtrs() { address = addressPtr, name = namePtr });

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

		private void SetupTempDisplaySurface(int width, int height, bool haveMask)
		{
			if ((tempDisplaySurface == null) || width != tempDisplaySurface.Width || height != tempDisplaySurface.Height)
			{
				if (tempDisplaySurface != null)
				{
					tempDisplaySurface.Dispose();
					tempDisplaySurface = null;
				}

				tempDisplaySurface = new Surface32(width, height);

				if (ignoreAlpha || !haveMask)
				{
					tempDisplaySurface.SetAlphaToOpaque();
				}
			}
		}

		/// <summary>
		/// Renders the 32-bit bitmap to the HDC.
		/// </summary>
		/// <param name="gr">The Graphics object to render to.</param>
		/// <param name="dstCol">The column offset to render at.</param>
		/// <param name="dstRow">The row offset to render at.</param>
		private void Display32BitBitmap(Graphics gr, int dstCol, int dstRow)
		{
			int width = tempDisplaySurface.Width;
			int height = tempDisplaySurface.Height;

			if (checkerBoardBitmap == null)
			{
				DrawCheckerBoardBitmap();
			}

			using (Bitmap temp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
			{
				Rectangle rect = new Rectangle(0, 0, width, height);

				using (Graphics tempGr = Graphics.FromImage(temp))
				{
					tempGr.DrawImageUnscaledAndClipped(checkerBoardBitmap, rect);
					using (Bitmap bmp = tempDisplaySurface.CreateAliasedBitmap())
					{
						tempGr.DrawImageUnscaled(bmp, rect);
					}
				}

				gr.DrawImageUnscaled(temp, dstCol, dstRow);
			}
		}

		private unsafe short DisplayPixelsProc(ref PSPixelMap srcPixelMap, ref VRect srcRect, int dstRow, int dstCol, IntPtr platformContext)
		{
#if DEBUG
			Ping(DebugFlags.DisplayPixels, string.Format("source: version = {0} bounds = {1}, ImageMode = {2}, colBytes = {3}, rowBytes = {4},planeBytes = {5}, BaseAddress = 0x{6}, mat = 0x{7}, masks = 0x{8}", new object[]{ srcPixelMap.version, srcPixelMap.bounds, ((ImageModes)srcPixelMap.imageMode).ToString("G"),
				srcPixelMap.colBytes, srcPixelMap.rowBytes, srcPixelMap.planeBytes, srcPixelMap.baseAddr.ToHexString(), srcPixelMap.mat.ToHexString(), srcPixelMap.masks.ToHexString()}));
			Ping(DebugFlags.DisplayPixels, string.Format("srcRect = {0} dstCol (x, width) = {1}, dstRow (y, height) = {2}", srcRect, dstCol, dstRow));
#endif

			if (platformContext == IntPtr.Zero || srcPixelMap.rowBytes == 0 || srcPixelMap.baseAddr == IntPtr.Zero ||
				(srcPixelMap.imageMode != PSConstants.plugInModeRGBColor && srcPixelMap.imageMode != PSConstants.plugInModeGrayScale))
			{
				return PSError.filterBadParameters;
			}

			int width = srcRect.right - srcRect.left;
			int height = srcRect.bottom - srcRect.top;
			int nplanes = ((FilterRecord*)filterRecordPtr.ToPointer())->planes;

			bool hasTransparencyMask = srcPixelMap.version >= 1 && srcPixelMap.masks != IntPtr.Zero;

			// Ignore the alpha plane if the PSPixelMap does not have a transparency mask.  
			if (!hasTransparencyMask && nplanes == 4)
			{
				nplanes = 3;
			}

			SetupTempDisplaySurface(width, height, hasTransparencyMask);

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
				for (int y = top; y < bottom; y++)
				{
					byte* src = baseAddr + (y * srcPixelMap.rowBytes) + left;
					byte* dst = tempDisplaySurface.GetRowAddressUnchecked(y - top);

					for (int x = 0; x < width; x++)
					{
						dst[0] = dst[1] = dst[2] = *src;

						src += srcPixelMap.colBytes;
						dst += 4;
					}
				}
			}
			else
			{
				for (int y = top; y < bottom; y++)
				{
					int surfaceY = y - top;
					if (srcPixelMap.colBytes == 1)
					{
						byte* row = tempDisplaySurface.GetRowAddressUnchecked(surfaceY);
						int srcStride = y * srcPixelMap.rowBytes; // cache the destination row and source stride.
						for (int i = 0; i < nplanes; i++)
						{
							int ofs = i;
							switch (i) // Photoshop uses RGBA pixel order so map the Red and Blue channels to BGRA order
							{
								case 0:
									ofs = 2;
									break;
								case 2:
									ofs = 0;
									break;
							}
							byte* src = baseAddr + srcStride + (i * srcPixelMap.planeBytes) + left;
							byte* dst = row + ofs;

							for (int x = 0; x < width; x++)
							{
								*dst = *src;

								src += srcPixelMap.colBytes;
								dst += 4;
							}
						}

					}
					else
					{
						byte* src = baseAddr + (y * srcPixelMap.rowBytes) + left;
						byte* dst = tempDisplaySurface.GetRowAddressUnchecked(surfaceY);

						for (int x = 0; x < width; x++)
						{
							dst[0] = src[2];
							dst[1] = src[1];
							dst[2] = src[0];
							if (srcPixelMap.colBytes == 4)
							{
								dst[3] = src[3];
							}

							src += srcPixelMap.colBytes;
							dst += 4;
						}
					}
				}
			}


			using (Graphics gr = Graphics.FromHdc(platformContext))
			{
				if (srcPixelMap.colBytes == 4 || nplanes == 4 && srcPixelMap.colBytes == 1)
				{
					Display32BitBitmap(gr, dstCol, dstRow);
				}
				else
				{
					// Apply the transparency mask for the Protected Transparency cases.
					if (hasTransparencyMask && (this.filterCase == FilterCase.ProtectedTransparencyNoSelection || this.filterCase == FilterCase.ProtectedTransparencyWithSelection)) 
					{
						PSPixelMask* srcMask = (PSPixelMask*)srcPixelMap.masks.ToPointer();
						byte* maskData = (byte*)srcMask->maskData.ToPointer();

						for (int y = 0; y < height; y++)
						{
							byte* src = maskData + (y * srcMask->rowBytes);
							byte* dst = tempDisplaySurface.GetRowAddressUnchecked(y);

							for (int x = 0; x < width; x++)
							{
								dst[3] = *src;

								src += srcMask->colBytes;
								dst += 4;
							}
						}

						Display32BitBitmap(gr, dstCol, dstRow);
					}
					else
					{
						using (Bitmap bmp = tempDisplaySurface.CreateAliasedBitmap())
						{
							gr.DrawImageUnscaled(bmp, dstCol, dstRow);
						}
					}

				}
			}

			return PSError.noErr;
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
			mask = new Surface8(source.Width, source.Height);

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

		#region DescriptorParameters

		private unsafe IntPtr OpenReadDescriptorProc(IntPtr descriptor, IntPtr keyArray)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif
			if (aeteDict.Count > 0)
			{
				if (keys == null)
				{
					keys = new List<uint>();
					if (keyArray != IntPtr.Zero)
					{
						uint* ptr = (uint*)keyArray.ToPointer();
						while (*ptr != 0U)
						{
#if DEBUG
							Ping(DebugFlags.DescriptorParameters, string.Format("key = {0}", PropToString(*ptr)));
#endif

							keys.Add(*ptr);
							ptr++;
						}

						// trim the list to the actual values in the dictionary
						uint[] values = keys.ToArray();
						foreach (var item in values)
						{
							if (!aeteDict.ContainsKey(item))
							{
								keys.Remove(item);
							}
						}
					}

					if (keys.Count == 0)
					{
						keys.AddRange(aeteDict.Keys); // if the keyArray is a null pointer or if it does not contain any valid keys get them from the aeteDict.
					}

				}
				else
				{
					subKeys = new List<uint>();
					if (keyArray != IntPtr.Zero)
					{
						uint* ptr = (uint*)keyArray.ToPointer();
						while (*ptr != 0U)
						{
#if DEBUG
							Ping(DebugFlags.DescriptorParameters, string.Format("subKey = {0}", PropToString(*ptr)));
#endif

							subKeys.Add(*ptr);
							ptr++;
						}
					}
					isSubKey = true;
					subClassDict = null;
					subClassIndex = 0;

					if (aeteDict.ContainsKey(getKey) && aeteDict[getKey].Value is Dictionary<uint, AETEValue>)
					{
						subClassDict = (Dictionary<uint, AETEValue>)aeteDict[getKey].Value;
					}
					else
					{
						// trim the list to the actual values in the dictionary
						uint[] values = subKeys.ToArray();
						foreach (var item in values)
						{
							if (!aeteDict.ContainsKey(item))
							{
								subKeys.Remove(item);
							}
						}
					}

				}

				return HandleNewProc(0); // return a dummy handle to the key value pairs
			}

			return IntPtr.Zero;
		}
		private short CloseReadDescriptorProc(IntPtr descriptor)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif
			if (isSubKey)
			{
				isSubKey = false;
				subClassDict = null;
				subClassIndex = 0;
			}

			return descErrValue;
		}

		private byte GetKeyProc(IntPtr descriptor, ref uint key, ref uint type, ref int flags)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif
			if (descErr != PSError.noErr)
			{
				descErrValue = descErr;
			}

			if (aeteDict.Count > 0)
			{
				if (isSubKey)
				{
					if (subClassDict != null)
					{
						if (subClassIndex >= subClassDict.Count)
						{
							return 0;
						}

						getKey = key = subKeys[subClassIndex];
						AETEValue value = subClassDict[key];
						try
						{
							type = value.Type;
						}
						catch (NullReferenceException)
						{
						}

						try
						{
							flags = value.Flags;
						}
						catch (NullReferenceException)
						{
						}

						subClassIndex++;
					}
					else
					{
						if (subKeyIndex >= subKeys.Count)
						{
							return 0;
						}

						getKey = key = subKeys[subKeyIndex];

						AETEValue value = aeteDict[key];
						try
						{
							type = value.Type;
						}
						catch (NullReferenceException)
						{
						}

						try
						{
							flags = value.Flags;
						}
						catch (NullReferenceException)
						{
						}

						subKeyIndex++;
					}
				}
				else
				{
					if (getKeyIndex >= keys.Count)
					{
						return 0;
					}
					getKey = key = keys[getKeyIndex];

					AETEValue value = aeteDict[key];
					try
					{
						type = value.Type;
					}
					catch (NullReferenceException)
					{
					}

					try
					{
						flags = value.Flags;
					}
					catch (NullReferenceException)
					{
					}

					getKeyIndex++;
				}

				return 1;
			}

			return 0;
		}
		private short GetIntegerProc(IntPtr descriptor, ref int data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = aeteDict[getKey];
			}

			data = (int)item.Value;

			return PSError.noErr;
		}
		private short GetFloatProc(IntPtr descriptor, ref double data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = aeteDict[getKey];
			}

			data = (double)item.Value;

			return PSError.noErr;
		}
		private short GetUnitFloatProc(IntPtr descriptor, ref uint unit, ref double data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = aeteDict[getKey];
			}

			UnitFloat unitFloat = (UnitFloat)item.Value;

			try
			{
				unit = unitFloat.Unit;
			}
			catch (NullReferenceException)
			{
			}

			data = unitFloat.Value;

			return PSError.noErr;
		}
		private short GetBooleanProc(IntPtr descriptor, ref byte data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = aeteDict[getKey];
			}

			data = (byte)item.Value;

			return PSError.noErr;
		}
		private short GetTextProc(IntPtr descriptor, ref IntPtr data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = aeteDict[getKey];
			}

			int size = item.Size;
			data = HandleNewProc(size);

			if (data == IntPtr.Zero)
			{
				return PSError.memFullErr;
			}

			Marshal.Copy((byte[])item.Value, 0, HandleLockProc(data, 0), size);
			HandleUnlockProc(data);

			return PSError.noErr;
		}
		private short GetAliasProc(IntPtr descriptor, ref IntPtr data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = aeteDict[getKey];
			}

			int size = item.Size;
			data = HandleNewProc(size);

			if (data == IntPtr.Zero)
			{
				return PSError.memFullErr;
			}

			Marshal.Copy((byte[])item.Value, 0, HandleLockProc(data, 0), size);
			HandleUnlockProc(data);

			return PSError.noErr;
		}
		private short GetEnumeratedProc(IntPtr descriptor, ref uint type)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = aeteDict[getKey];
			}

			type = (uint)item.Value;

			return PSError.noErr;
		}
		private short GetClassProc(IntPtr descriptor, ref uint type)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = aeteDict[getKey];
			}

			type = (uint)item.Value;

			return PSError.noErr;
		}

		private short GetSimpleReferenceProc(IntPtr descriptor, ref PIDescriptorSimpleReference data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			if (aeteDict.ContainsKey(getKey))
			{
				data = (PIDescriptorSimpleReference)aeteDict[getKey].Value;
				return PSError.noErr;
			}
			return PSError.errPlugInHostInsufficient;
		}
		private short GetObjectProc(IntPtr descriptor, ref uint retType, ref IntPtr data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: {0}", PropToString(getKey)));
#endif
			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = aeteDict[getKey];
			}


			uint type = item.Type;

			try
			{
				retType = type;
			}
			catch (NullReferenceException)
			{
				// ignore it
			}

			switch (type)
			{

				case DescriptorTypes.classRGBColor:
				case DescriptorTypes.classCMYKColor:
				case DescriptorTypes.classGrayscale:
				case DescriptorTypes.classLabColor:
				case DescriptorTypes.classHSBColor:
				case DescriptorTypes.classPoint:
					data = HandleNewProc(0); // assign a zero byte handle to allow it to work correctly in the OpenReadDescriptorProc(). 
					break;

				case DescriptorTypes.typeAlias:
				case DescriptorTypes.typePath:
				case DescriptorTypes.typeChar:

					int size = item.Size;
					data = HandleNewProc(size);

					if (data == IntPtr.Zero)
					{
						return PSError.memFullErr;
					}

					Marshal.Copy((byte[])item.Value, 0, HandleLockProc(data, 0), size);
					HandleUnlockProc(data);
					break;
				case DescriptorTypes.typeBoolean:
					data = HandleNewProc(sizeof(Byte));

					if (data == IntPtr.Zero)
					{
						return PSError.memFullErr;
					}

					Marshal.WriteByte(HandleLockProc(data, 0), (byte)item.Value);
					HandleUnlockProc(data);
					break;
				case DescriptorTypes.typeInteger:
					data = HandleNewProc(sizeof(Int32));

					if (data == IntPtr.Zero)
					{
						return PSError.memFullErr;
					}

					Marshal.WriteInt32(HandleLockProc(data, 0), (int)item.Value);
					HandleUnlockProc(data);
					break;
				case DescriptorTypes.typeFloat:
				case DescriptorTypes.typeUintFloat:
					data = HandleNewProc(sizeof(Double));

					if (data == IntPtr.Zero)
					{
						return PSError.memFullErr;
					}

					double value;
					if (type == DescriptorTypes.typeUintFloat)
					{
						UnitFloat unitFloat = (UnitFloat)item.Value;
						value = unitFloat.Value;
					}
					else
					{
						value = (double)item.Value;
					}

					Marshal.Copy(new double[] { value }, 0, HandleLockProc(data, 0), 1);
					HandleUnlockProc(data);
					break;

				default:
					break;
			}

			return PSError.noErr;
		}
		private short GetCountProc(IntPtr descriptor, ref uint count)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			if (subClassDict != null)
			{
				count = (uint)subClassDict.Count;
			}
			else
			{
				count = (uint)aeteDict.Count;
			}
			return PSError.noErr;
		}
		private short GetStringProc(IntPtr descriptor, IntPtr data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = aeteDict[getKey];
			}
			int size = item.Size;

			Marshal.WriteByte(data, (byte)size);

			Marshal.Copy((byte[])item.Value, 0, new IntPtr(data.ToInt64() + 1L), size);
			return PSError.noErr;
		}
		private short GetPinnedIntegerProc(IntPtr descriptor, int min, int max, ref int intNumber)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			descErr = PSError.noErr;

			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = aeteDict[getKey];
			}

			int amount = (int)item.Value;
			if (amount < min)
			{
				amount = min;
				descErr = PSError.coercedParamErr;
			}
			else if (amount > max)
			{
				amount = max;
				descErr = PSError.coercedParamErr;
			}

			intNumber = amount;

			return descErr;
		}
		private short GetPinnedFloatProc(IntPtr descriptor, ref double min, ref double max, ref double floatNumber)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			descErr = PSError.noErr;
			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = aeteDict[getKey];
			}

			double amount = (double)item.Value;
			if (amount < min)
			{
				amount = min;
				descErr = PSError.coercedParamErr;
			}
			else if (amount > max)
			{
				amount = max;
				descErr = PSError.coercedParamErr;
			}
			floatNumber = amount;

			return descErr;
		}
		private short GetPinnedUnitFloatProc(IntPtr descriptor, ref double min, ref double max, ref uint units, ref double floatNumber)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			descErr = PSError.noErr;

			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = aeteDict[getKey];
			}

			UnitFloat unitFloat = (UnitFloat)item.Value;

			if (unitFloat.Unit != units)
			{
				descErr = PSError.paramErr;
			}

			double amount = unitFloat.Value;
			if (amount < min)
			{
				amount = min;
				descErr = PSError.coercedParamErr;
			}
			else if (amount > max)
			{
				amount = max;
				descErr = PSError.coercedParamErr;
			}
			floatNumber = amount;

			return descErr;
		}
		// WriteDescriptorProcs

		private IntPtr OpenWriteDescriptorProc()
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif
			return writeDescriptorPtr;
		}
		private short CloseWriteDescriptorProc(IntPtr descriptor, ref IntPtr descriptorHandle)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif
			if (isSubKey)
			{
				isSubKey = false;
			}

			descriptorHandle = HandleNewProc(0);

			return PSError.noErr;
		}

		private int GetAETEParamFlags(uint key)
		{
			if (aete != null)
			{
				foreach (var item in aete.scriptEvent.parameters)
				{
					if (item.key == key)
					{
						return item.flags;
					}
				}

			}

			return 0;
		}

		private short PutIntegerProc(IntPtr descriptor, uint key, int data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, PropToString(key)));
#endif
			aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeInteger, GetAETEParamFlags(key), 0, data));
			return PSError.noErr;
		}

		private short PutFloatProc(IntPtr descriptor, uint key, ref double data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeFloat, GetAETEParamFlags(key), 0, data));
			return PSError.noErr;
		}

		private short PutUnitFloatProc(IntPtr descriptor, uint key, uint unit, ref double data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			UnitFloat item = new UnitFloat(unit, data);

			aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeUintFloat, GetAETEParamFlags(key), 0, item));
			return PSError.noErr;
		}

		private short PutBooleanProc(IntPtr descriptor, uint key, byte data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeBoolean, GetAETEParamFlags(key), 0, data));
			return PSError.noErr;
		}

		private short PutTextProc(IntPtr descriptor, uint key, IntPtr textHandle)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif

			if (textHandle != IntPtr.Zero)
			{
				IntPtr hPtr = HandleLockProc(textHandle, 0);

				try
				{
					int size = HandleGetSizeProc(textHandle);
					byte[] data = new byte[size];
					Marshal.Copy(hPtr, data, 0, size);

					aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeChar, GetAETEParamFlags(key), size, data));
				}
				finally
				{
					HandleUnlockProc(textHandle);
				}
			}

			return PSError.noErr;
		}

		private short PutAliasProc(IntPtr descriptor, uint key, IntPtr aliasHandle)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			IntPtr hPtr = HandleLockProc(aliasHandle, 0);

			try
			{
				int size = HandleGetSizeProc(aliasHandle);
				byte[] data = new byte[size];
				Marshal.Copy(hPtr, data, 0, size);

				aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeAlias, GetAETEParamFlags(key), size, data));
			}
			finally
			{
				HandleUnlockProc(aliasHandle);
			}
			return PSError.noErr;
		}

		private short PutEnumeratedProc(IntPtr descriptor, uint key, uint type, uint data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			aeteDict.AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, data));
			return PSError.noErr;
		}

		private short PutClassProc(IntPtr descriptor, uint key, uint data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeClass, GetAETEParamFlags(key), 0, data));

			return PSError.noErr;
		}

		private short PutSimpleReferenceProc(IntPtr descriptor, uint key, ref PIDescriptorSimpleReference data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeObjectRefrence, GetAETEParamFlags(key), 0, data));
			return PSError.noErr;
		}

		private short PutObjectProc(IntPtr descriptor, uint key, uint type, IntPtr handle)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: {0}, type: {1}", PropToString(key), PropToString(type)));
#endif
			Dictionary<uint, AETEValue> classDict = null;
			// Only the built-in Photoshop classes are supported.
			switch (type)
			{
				case DescriptorTypes.classRGBColor:
					classDict = new Dictionary<uint, AETEValue>(3);
					classDict.Add(DescriptorKeys.keyRed, aeteDict[DescriptorKeys.keyRed]);
					classDict.Add(DescriptorKeys.keyGreen, aeteDict[DescriptorKeys.keyGreen]);
					classDict.Add(DescriptorKeys.keyBlue, aeteDict[DescriptorKeys.keyBlue]);

					aeteDict.Remove(DescriptorKeys.keyRed);// remove the existing keys
					aeteDict.Remove(DescriptorKeys.keyGreen);
					aeteDict.Remove(DescriptorKeys.keyBlue);

					aeteDict.AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, classDict));
					break;
				case DescriptorTypes.classCMYKColor:
					classDict = new Dictionary<uint, AETEValue>(4);
					classDict.Add(DescriptorKeys.keyCyan, aeteDict[DescriptorKeys.keyCyan]);
					classDict.Add(DescriptorKeys.keyMagenta, aeteDict[DescriptorKeys.keyMagenta]);
					classDict.Add(DescriptorKeys.keyYellow, aeteDict[DescriptorKeys.keyYellow]);
					classDict.Add(DescriptorKeys.keyBlack, aeteDict[DescriptorKeys.keyBlack]);

					aeteDict.Remove(DescriptorKeys.keyCyan);
					aeteDict.Remove(DescriptorKeys.keyMagenta);
					aeteDict.Remove(DescriptorKeys.keyYellow);
					aeteDict.Remove(DescriptorKeys.keyBlack);

					aeteDict.AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, classDict));
					break;
				case DescriptorTypes.classGrayscale:
					classDict = new Dictionary<uint, AETEValue>(1);
					classDict.Add(DescriptorKeys.keyGray, aeteDict[DescriptorKeys.keyGray]);

					aeteDict.Remove(DescriptorKeys.keyGray);

					aeteDict.AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, classDict));
					break;
				case DescriptorTypes.classLabColor:
					classDict = new Dictionary<uint, AETEValue>(3);
					classDict.Add(DescriptorKeys.keyLuminance, aeteDict[DescriptorKeys.keyLuminance]);
					classDict.Add(DescriptorKeys.keyA, aeteDict[DescriptorKeys.keyA]);
					classDict.Add(DescriptorKeys.keyB, aeteDict[DescriptorKeys.keyB]);

					aeteDict.Remove(DescriptorKeys.keyLuminance);
					aeteDict.Remove(DescriptorKeys.keyA);
					aeteDict.Remove(DescriptorKeys.keyB);

					aeteDict.AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, classDict));
					break;
				case DescriptorTypes.classHSBColor:
					classDict = new Dictionary<uint, AETEValue>(3);
					classDict.Add(DescriptorKeys.keyHue, aeteDict[DescriptorKeys.keyHue]);
					classDict.Add(DescriptorKeys.keySaturation, aeteDict[DescriptorKeys.keySaturation]);
					classDict.Add(DescriptorKeys.keyBrightness, aeteDict[DescriptorKeys.keyBrightness]);

					aeteDict.Remove(DescriptorKeys.keyHue);
					aeteDict.Remove(DescriptorKeys.keySaturation);
					aeteDict.Remove(DescriptorKeys.keyBrightness);

					aeteDict.AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, classDict));
					break;
				case DescriptorTypes.classPoint:
					classDict = new Dictionary<uint, AETEValue>(2);

					classDict.Add(DescriptorKeys.keyHorizontal, aeteDict[DescriptorKeys.keyHorizontal]);
					classDict.Add(DescriptorKeys.keyVertical, aeteDict[DescriptorKeys.keyVertical]);

					aeteDict.Remove(DescriptorKeys.keyHorizontal);
					aeteDict.Remove(DescriptorKeys.keyVertical);

					aeteDict.AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, classDict));

					break;

				default:
					return PSError.errPlugInHostInsufficient;
			}

			return PSError.noErr;
		}

		private short PutCountProc(IntPtr descriptor, uint key, uint count)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			return PSError.noErr;
		}

		private short PutStringProc(IntPtr descriptor, uint key, IntPtr stringHandle)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, PropToString(key)));
#endif
			int size = (int)Marshal.ReadByte(stringHandle);
			byte[] data = new byte[size];
			Marshal.Copy(new IntPtr(stringHandle.ToInt64() + 1L), data, 0, size);

			aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeChar, GetAETEParamFlags(key), size, data));

			return PSError.noErr;
		}

		private short PutScopedClassProc(IntPtr descriptor, uint key, uint data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeClass, GetAETEParamFlags(key), 0, data));

			return PSError.noErr;
		}

		private short PutScopedObjectProc(IntPtr descriptor, uint key, uint type, IntPtr handle)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif
			IntPtr hPtr = HandleLockProc(handle, 0);

			try
			{
				int size = HandleGetSizeProc(handle);
				byte[] data = new byte[size];
				Marshal.Copy(hPtr, data, 0, size);

				aeteDict.AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), size, data));
			}
			finally
			{
				HandleUnlockProc(handle);
			}

			return PSError.noErr;
		}
		#endregion

		/// <summary>
		/// Determines whether the handle was allocated using the handle suite.
		/// </summary>
		/// <param name="h">The handle to check.</param>
		/// <returns>
		///   <c>true</c> if the handle was allocated using the handle suite; otherwise, <c>false</c>.
		/// </returns>
		private bool IsHandleValid(IntPtr h)
		{
			return handles.ContainsKey(h);
		}

		private unsafe IntPtr HandleNewProc(int size)
		{
			IntPtr handle = IntPtr.Zero;
			try
			{
				handle = Memory.Allocate(PSHandle.SizeOf, true);

				PSHandle* hand = (PSHandle*)handle.ToPointer();

				hand->pointer = Memory.Allocate(size, true);
				hand->size = size;

				handles.Add(handle, *hand);
#if DEBUG
				Ping(DebugFlags.HandleSuite, string.Format("Handle: 0x{0}, pointer: 0x{1}, size: {2}", handle.ToHexString(), hand->pointer.ToHexString(), size));
#endif
			}
			catch (OutOfMemoryException)
			{
				if (handle != IntPtr.Zero)
				{
					Memory.Free(handle);
					handle = IntPtr.Zero;
				}

				return IntPtr.Zero;
			}

			return handle;
		}

		private unsafe void HandleDisposeProc(IntPtr h)
		{
			if (h != IntPtr.Zero && !IsBadReadPtr(h))
			{
#if DEBUG
				Ping(DebugFlags.HandleSuite, string.Format("Handle: 0x{0}", h.ToHexString()));
#endif
				if (!IsHandleValid(h))
				{
					if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
					{
						IntPtr hPtr = Marshal.ReadIntPtr(h);

						if (!IsBadReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
						{
							SafeNativeMethods.GlobalFree(hPtr);
						}

						SafeNativeMethods.GlobalFree(h);
					}

					return;
				}

				PSHandle* handle = (PSHandle*)h.ToPointer();

				Memory.Free(handle->pointer);
				Memory.Free(h);

				handles.Remove(h);
			}
		}

		private unsafe void HandleDisposeRegularProc(IntPtr h)
		{
#if DEBUG
			Ping(DebugFlags.HandleSuite, string.Format("Handle: 0x{0}", h.ToHexString()));
#endif
			// What is this supposed to do?
			if (!IsHandleValid(h))
			{
				if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					IntPtr hPtr = Marshal.ReadIntPtr(h);

					if (!IsBadReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
					{
						SafeNativeMethods.GlobalFree(hPtr);
					}

					SafeNativeMethods.GlobalFree(h);
				}
			}
		}

		private IntPtr HandleLockProc(IntPtr h, byte moveHigh)
		{
#if DEBUG
			Ping(DebugFlags.HandleSuite, string.Format("Handle: 0x{0}, moveHigh: {1}", h.ToHexString(), moveHigh));
#endif
			if (!IsHandleValid(h))
			{
				if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					IntPtr hPtr = Marshal.ReadIntPtr(h);

					if (!IsBadReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
					{
						return SafeNativeMethods.GlobalLock(hPtr);
					}

					return SafeNativeMethods.GlobalLock(h);
				}
				if (!IsBadReadPtr(h) && !IsBadWritePtr(h)) // Pointer to a pointer?
				{
					return h;
				}
				return IntPtr.Zero;
			}

#if DEBUG
			Ping(DebugFlags.HandleSuite, String.Format("Handle Pointer Address = 0x{0}", handles[h].pointer.ToHexString()));
#endif
			return handles[h].pointer;
		}

		private int HandleGetSizeProc(IntPtr h)
		{
#if DEBUG
			Ping(DebugFlags.HandleSuite, string.Format("Handle: 0x{0}", h.ToHexString()));
#endif
			if (!IsHandleValid(h))
			{
				if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					IntPtr hPtr = Marshal.ReadIntPtr(h);

					if (!IsBadReadPtr(hPtr))
					{
						return SafeNativeMethods.GlobalSize(hPtr).ToInt32();
					}
					else
					{
						return SafeNativeMethods.GlobalSize(h).ToInt32();
					}
				}
				return 0;
			}

			return handles[h].size;
		}

		private void HandleRecoverSpaceProc(int size)
		{
#if DEBUG
			Ping(DebugFlags.HandleSuite, string.Format("size: {0}", size));
#endif
		}

		private unsafe short HandleSetSizeProc(IntPtr h, int newSize)
		{
#if DEBUG
			Ping(DebugFlags.HandleSuite, string.Format("Handle: 0x{0}", h.ToHexString()));
#endif
			if (!IsHandleValid(h))
			{
				if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					IntPtr hPtr = Marshal.ReadIntPtr(h);

					if (!IsBadReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
					{
						IntPtr hMem = SafeNativeMethods.GlobalReAlloc(hPtr, new UIntPtr((uint)newSize), NativeConstants.GPTR);
						if (hMem == IntPtr.Zero)
						{
							return PSError.memFullErr;
						}
						Marshal.WriteIntPtr(h, hMem);
					}
					else
					{
						if (SafeNativeMethods.GlobalReAlloc(h, new UIntPtr((uint)newSize), NativeConstants.GPTR) == IntPtr.Zero)
						{
							return PSError.memFullErr;
						}
					}

					return PSError.noErr;
				}
				return PSError.nilHandleErr;
			}

			try
			{
				PSHandle* handle = (PSHandle*)h.ToPointer();
				IntPtr ptr = Memory.ReAlloc(handle->pointer, newSize);

				handle->pointer = ptr;
				handle->size = newSize;

				handles.AddOrUpdate(h, *handle);
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.noErr;
		}

		private void HandleUnlockProc(IntPtr h)
		{
#if DEBUG
			Ping(DebugFlags.HandleSuite, string.Format("Handle: 0x{0}", h.ToHexString()));
#endif
			if (!IsHandleValid(h))
			{
				if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					IntPtr hPtr = Marshal.ReadIntPtr(h);

					if (!IsBadReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
					{
						SafeNativeMethods.GlobalUnlock(hPtr);
					}
					else
					{
						SafeNativeMethods.GlobalUnlock(h);
					}
				}
			}

		}

		private void HostProc(short selector, IntPtr data)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Format("{0} : {1}", selector, data));
#endif
		}

#if USEIMAGESERVICES
		private short ImageServicesInterpolate1DProc(ref PSImagePlane source, ref PSImagePlane destination, ref Rect16 area, IntPtr coords, short method)
		{
#if DEBUG
			Ping(DebugFlags.ImageServices, string.Format("srcBounds: {0}, dstBounds: {1}, area: {2}, method: {3}", new object[] { source.bounds.ToString(), destination.bounds.ToString(), area.ToString(), ((InterpolationModes)method).ToString() }));
#endif
			return PSError.memFullErr;
		}

		private unsafe short ImageServicesInterpolate2DProc(ref PSImagePlane source, ref PSImagePlane destination, ref Rect16 area, IntPtr coords, short method)
		{
#if DEBUG
			Ping(DebugFlags.ImageServices, string.Format("srcBounds: {0}, dstBounds: {1}, area: {2}, method: {3}", new object[] { source.bounds.ToString(), destination.bounds.ToString(), area.ToString(), ((InterpolationModes)method).ToString() }));
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
			Ping(DebugFlags.MiscCallbacks, string.Format("Done: {0}, Total: {1}, Progress: {2:N2} %", done, total, ((double)done / (double)total) * 100.0));
#endif
			if (progressFunc != null)
			{
				progressFunc.Invoke(done, total);
			}
		}

		private unsafe short PropertyGetProc(uint signature, uint key, int index, ref IntPtr simpleProperty, ref IntPtr complexProperty)
		{
#if DEBUG
			Ping(DebugFlags.PropertySuite, string.Format("Sig: {0}, Key: {1}, Index: {2}", PropToString(signature), PropToString(key), index));
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
					if ((!string.IsNullOrEmpty(hostInfo.Caption)) && hostInfo.Caption.Length < IPTCData.MaxCaptionLength)
					{
						bytes = Encoding.ASCII.GetBytes(hostInfo.Caption);

						IPTCData.IPTCCaption caption = IPTCData.CreateCaptionRecord(bytes.Length);

						int captionSize = Marshal.SizeOf(typeof(IPTCData.IPTCCaption));

						complexProperty = HandleNewProc(captionSize + bytes.Length);

						if (complexProperty != IntPtr.Zero)
						{
							IntPtr ptr = HandleLockProc(complexProperty, 0);

							Marshal.Copy(caption.ToByteArray(), 0, ptr, captionSize); // Prefix the caption string with the IPTC-NAA record.

							Marshal.Copy(bytes, 0, new IntPtr(ptr.ToInt64() + (long)captionSize), bytes.Length);
							HandleUnlockProc(complexProperty);
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
							complexProperty = HandleNewProc(0);
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

					complexProperty = HandleNewProc(bytes.Length);
					if (complexProperty != IntPtr.Zero)
					{
						Marshal.Copy(bytes, 0, HandleLockProc(complexProperty, 0), bytes.Length);
						HandleUnlockProc(complexProperty);
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
						complexProperty = HandleNewProc(bytes.Length);
						if (complexProperty != IntPtr.Zero)
						{
							Marshal.Copy(bytes, 0, HandleLockProc(complexProperty, 0), bytes.Length);
							HandleUnlockProc(complexProperty);
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
							complexProperty = HandleNewProc(0);
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
						complexProperty = HandleNewProc(0);
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
					complexProperty = HandleNewProc(bytes.Length);

					if (complexProperty != IntPtr.Zero)
					{
						Marshal.Copy(bytes, 0, HandleLockProc(complexProperty, 0), bytes.Length);
						HandleUnlockProc(complexProperty);
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
						complexProperty = HandleNewProc(bytes.Length);

						if (complexProperty != IntPtr.Zero)
						{
							Marshal.Copy(bytes, 0, HandleLockProc(complexProperty, 0), bytes.Length);
							HandleUnlockProc(complexProperty);
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
							complexProperty = HandleNewProc(0);
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
					complexProperty = HandleNewProc(bytes.Length);

					if (complexProperty != IntPtr.Zero)
					{
						Marshal.Copy(bytes, 0, HandleLockProc(complexProperty, 0), bytes.Length);
						HandleUnlockProc(complexProperty);
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

				default:
					return PSError.errPlugInPropertyUndefined;
			}

			return err;
		}

		private short PropertySetProc(uint signature, uint key, int index, IntPtr simpleProperty, IntPtr complexProperty)
		{
#if DEBUG
			Ping(DebugFlags.PropertySuite, string.Format("Sig: {0}, Key: {1}, Index: {2}", PropToString(signature), PropToString(key), index));
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
					size = HandleGetSizeProc(complexProperty);
					if (size > 0)
					{
						IntPtr ptr = HandleLockProc(complexProperty, 0);

						IPTCData.IPTCCaption caption = IPTCData.CaptionFromMemory(ptr);

						if (caption.tag.length > 0)
						{
							bytes = new byte[caption.tag.length];
							Marshal.Copy(new IntPtr(ptr.ToInt64() + (long)Marshal.SizeOf(typeof(IPTCData.IPTCCaption))), bytes, 0, bytes.Length);
							hostInfo.Caption = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
						}
						HandleUnlockProc(complexProperty);
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
					size = HandleGetSizeProc(complexProperty);
					if (size > 0)
					{
						bytes = new byte[size];
						Marshal.Copy(HandleLockProc(complexProperty, 0), bytes, 0, size);
						HandleUnlockProc(complexProperty);
						
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

		private short ResourceAddProc(uint ofType, IntPtr data)
		{
#if DEBUG
			Ping(DebugFlags.ResourceSuite, PropToString(ofType));
#endif
			int size = HandleGetSizeProc(data);
			try
			{
				byte[] bytes = new byte[size];

				if (size > 0)
				{
					Marshal.Copy(HandleLockProc(data, 0), bytes, 0, size);
					HandleUnlockProc(data);
				}

				int index = ResourceCountProc(ofType) + 1;
				pseudoResources.Add(new PSResource(ofType, index, bytes));
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.noErr;
		}

		private short ResourceCountProc(uint ofType)
		{
#if DEBUG
			Ping(DebugFlags.ResourceSuite, PropToString(ofType));
#endif
			short count = 0;

			foreach (var item in pseudoResources)
			{
				if (item.Equals(ofType))
				{
					count++;
				}
			}

			return count;
		}

		private void ResourceDeleteProc(uint ofType, short index)
		{
#if DEBUG
			Ping(DebugFlags.ResourceSuite, string.Format("{0}, {1}", PropToString(ofType), index));
#endif
			PSResource res = pseudoResources.Find(delegate(PSResource r)
			{
				return r.Equals(ofType, index);
			});

			if (res != null)
			{
				pseudoResources.Remove(res);

				int i = index + 1;

				while (true) // renumber the index of subsequent items.
				{
					int next = pseudoResources.FindIndex(delegate(PSResource r)
					{
						return r.Equals(ofType, i);
					});

					if (next < 0) break;

					pseudoResources[next].Index = i - 1;

					i++;
				}
			}
		}
		private IntPtr ResourceGetProc(uint ofType, short index)
		{
#if DEBUG
			Ping(DebugFlags.ResourceSuite, string.Format("{0}, {1}", PropToString(ofType), index));
#endif
			PSResource res = pseudoResources.Find(delegate(PSResource r)
			{
				return r.Equals(ofType, index);
			});

			if (res != null)
			{
				byte[] data = res.GetData();

				IntPtr h = HandleNewProc(data.Length);
				if (h != IntPtr.Zero)
				{
					Marshal.Copy(data, 0, HandleLockProc(h, 0), data.Length);
					HandleUnlockProc(h);
				}

				return h;
			}

			return IntPtr.Zero;
		}

		private unsafe int SPBasicAcquireSuite(IntPtr name, int version, ref IntPtr suite)
		{

			string suiteName = Marshal.PtrToStringAnsi(name);
#if DEBUG
			Ping(DebugFlags.SPBasicSuite, string.Format("name: {0}, version: {1}", suiteName, version));
#endif

			string suiteKey = string.Format(CultureInfo.InvariantCulture, "{0},{1}", suiteName, version.ToString(CultureInfo.InvariantCulture));

			if (activePICASuites.IsLoaded(suiteKey))
			{
				suite = this.activePICASuites.AddRef(suiteKey);
			}
			else
			{
				try
				{
					if (suiteName == PSConstants.PICABufferSuite)
					{
						if (version > 1)
						{
							return PSError.kSPSuiteNotFoundError;
						}

						suite = this.activePICASuites.AllocateSuite<PSBufferSuite1>(suiteKey);

						PSBufferSuite1 bufferSuite = PICASuites.CreateBufferSuite1();

						Marshal.StructureToPtr(bufferSuite, suite, false);
					}
					else if (suiteName == PSConstants.PICAHandleSuite)
					{
						if (version > 2)
						{
							return PSError.kSPSuiteNotFoundError;
						}

						if (version == 1)
						{
							suite = this.activePICASuites.AllocateSuite<PSHandleSuite1>(suiteKey);

							PSHandleSuite1 handleSuite = PICASuites.CreateHandleSuite1((HandleProcs*)handleProcsPtr.ToPointer(), handleLockProc, handleUnlockProc);

							Marshal.StructureToPtr(handleSuite, suite, false);
						}
						else
						{
							suite = this.activePICASuites.AllocateSuite<PSHandleSuite2>(suiteKey);

							PSHandleSuite2 handleSuite = PICASuites.CreateHandleSuite2((HandleProcs*)handleProcsPtr.ToPointer(), handleLockProc, handleUnlockProc);

							Marshal.StructureToPtr(handleSuite, suite, false);
						}
					}
					else if (suiteName == PSConstants.PICAPropertySuite)
					{
						if (version > PSConstants.kCurrentPropertyProcsVersion)
						{
							return PSError.kSPSuiteNotFoundError;
						}

						suite = this.activePICASuites.AllocateSuite<PropertyProcs>(suiteKey);

						PropertyProcs propertySuite = PICASuites.CreatePropertySuite((PropertyProcs*)propertyProcsPtr.ToPointer());

						Marshal.StructureToPtr(propertySuite, suite, false);
					}
					else if (suiteName == PSConstants.PICAUIHooksSuite)
					{
						if (version > 1)
						{
							return PSError.kSPSuiteNotFoundError;
						}

						suite = this.activePICASuites.AllocateSuite<PSUIHooksSuite1>(suiteKey);

						PSUIHooksSuite1 uiHooks = PICASuites.CreateUIHooksSuite1((FilterRecord*)filterRecordPtr.ToPointer());

						Marshal.StructureToPtr(uiHooks, suite, false);
					}
#if PICASUITEDEBUG
					else if (suiteName == PSConstants.PICAColorSpaceSuite)
					{
						if (version > 1)
						{
							return PSError.kSPSuiteNotFoundError;
						}

						suite = this.activePICASuites.AllocateSuite<PSColorSpaceSuite1>(suiteKey);

						PSColorSpaceSuite1 csSuite = PICASuites.CreateColorSpaceSuite1();

						Marshal.StructureToPtr(csSuite, suite, false);
					}
					else if (suiteName == PSConstants.PICAPluginsSuite)
					{
						if (version > 4)
						{
							return PSError.kSPSuiteNotFoundError;
						}

						suite = this.activePICASuites.AllocateSuite<SPPluginsSuite4>(suiteKey);

						SPPluginsSuite4 plugs = PICASuites.CreateSPPlugs4();

						Marshal.StructureToPtr(plugs, suite, false);
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
			}

			return PSError.kSPNoErr;
		}

		private int SPBasicReleaseSuite(IntPtr name, int version)
		{
			string suiteName = Marshal.PtrToStringAnsi(name);

#if DEBUG
			Ping(DebugFlags.SPBasicSuite, string.Format("name: {0}, version: {1}", suiteName, version.ToString()));
#endif

			string suiteKey = string.Format(CultureInfo.InvariantCulture, "{0},{1}", suiteName, version.ToString(CultureInfo.InvariantCulture));

			this.activePICASuites.RemoveRef(suiteKey);

			return PSError.kSPNoErr;
		}

		private unsafe int SPBasicIsEqual(IntPtr token1, IntPtr token2)
		{
#if DEBUG
			Ping(DebugFlags.SPBasicSuite, string.Format("token1: {0}, token2: {1}", Marshal.PtrToStringAnsi(token1), Marshal.PtrToStringAnsi(token2)));
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
			Ping(DebugFlags.SPBasicSuite, string.Format("size: {0}", size));
#endif
			try
			{
				block = Memory.Allocate(size, false);
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.kSPNoErr;
		}

		private int SPBasicFreeBlock(IntPtr block)
		{
#if DEBUG
			Ping(DebugFlags.SPBasicSuite, string.Format("block: 0x{0}", block.ToHexString()));
#endif
			Memory.Free(block);
			return PSError.kSPNoErr;
		}

		private int SPBasicReallocateBlock(IntPtr block, int newSize, ref IntPtr newblock)
		{
#if DEBUG
			Ping(DebugFlags.SPBasicSuite, string.Format("block: 0x{0}, size: {1}", block.ToHexString(), newSize));
#endif
			try
			{
				newblock = Memory.ReAlloc(block, newSize);
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.kSPNoErr;
		}

		private int SPBasicUndefined()
		{
#if DEBUG
			Ping(DebugFlags.SPBasicSuite, string.Empty);
#endif

			return PSError.kSPNoErr;
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
			advanceProc = new AdvanceStateProc(AdvanceStateProc);
			// BufferProc
			allocProc = new AllocateBufferProc(AllocateBufferProc);
			freeProc = new FreeBufferProc(BufferFreeProc);
			lockProc = new LockBufferProc(BufferLockProc);
			unlockProc = new UnlockBufferProc(BufferUnlockProc);
			spaceProc = new BufferSpaceProc(BufferSpaceProc);
			// Misc Callbacks
			colorProc = new ColorServicesProc(ColorServicesProc);
			displayPixelsProc = new DisplayPixelsProc(DisplayPixelsProc);
			hostProc = new HostProcs(HostProc);
			processEventProc = new ProcessEventProc(ProcessEvent);
			progressProc = new ProgressProc(ProgressProc);
			abortProc = new TestAbortProc(AbortProc);
			// HandleProc
			handleNewProc = new NewPIHandleProc(HandleNewProc);
			handleDisposeProc = new DisposePIHandleProc(HandleDisposeProc);
			handleGetSizeProc = new GetPIHandleSizeProc(HandleGetSizeProc);
			handleSetSizeProc = new SetPIHandleSizeProc(HandleSetSizeProc);
			handleLockProc = new LockPIHandleProc(HandleLockProc);
			handleUnlockProc = new UnlockPIHandleProc(HandleUnlockProc);
			handleRecoverSpaceProc = new RecoverSpaceProc(HandleRecoverSpaceProc);
			handleDisposeRegularProc = new DisposeRegularPIHandleProc(HandleDisposeRegularProc);

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
			// ResourceProcs
			countResourceProc = new CountPIResourcesProc(ResourceCountProc);
			getResourceProc = new GetPIResourceProc(ResourceGetProc);
			deleteResourceProc = new DeletePIResourceProc(ResourceDeleteProc);
			addResourceProc = new AddPIResourceProc(ResourceAddProc);


			// ReadDescriptorProcs
			openReadDescriptorProc = new OpenReadDescriptorProc(OpenReadDescriptorProc);
			closeReadDescriptorProc = new CloseReadDescriptorProc(CloseReadDescriptorProc);
			getKeyProc = new GetKeyProc(GetKeyProc);
			getAliasProc = new GetAliasProc(GetAliasProc);
			getBooleanProc = new GetBooleanProc(GetBooleanProc);
			getClassProc = new GetClassProc(GetClassProc);
			getCountProc = new GetCountProc(GetCountProc);
			getEnumeratedProc = new GetEnumeratedProc(GetEnumeratedProc);
			getFloatProc = new GetFloatProc(GetFloatProc);
			getIntegerProc = new GetIntegerProc(GetIntegerProc);
			getObjectProc = new GetObjectProc(GetObjectProc);
			getPinnedFloatProc = new GetPinnedFloatProc(GetPinnedFloatProc);
			getPinnedIntegerProc = new GetPinnedIntegerProc(GetPinnedIntegerProc);
			getPinnedUnitFloatProc = new GetPinnedUnitFloatProc(GetPinnedUnitFloatProc);
			getSimpleReferenceProc = new GetSimpleReferenceProc(GetSimpleReferenceProc);
			getStringProc = new GetStringProc(GetStringProc);
			getTextProc = new GetTextProc(GetTextProc);
			getUnitFloatProc = new GetUnitFloatProc(GetUnitFloatProc);
			// WriteDescriptorProcs
			openWriteDescriptorProc = new OpenWriteDescriptorProc(OpenWriteDescriptorProc);
			closeWriteDescriptorProc = new CloseWriteDescriptorProc(CloseWriteDescriptorProc);
			putAliasProc = new PutAliasProc(PutAliasProc);
			putBooleanProc = new PutBooleanProc(PutBooleanProc);
			putClassProc = new PutClassProc(PutClassProc);
			putCountProc = new PutCountProc(PutCountProc);
			putEnumeratedProc = new PutEnumeratedProc(PutEnumeratedProc);
			putFloatProc = new PutFloatProc(PutFloatProc);
			putIntegerProc = new PutIntegerProc(PutIntegerProc);
			putObjectProc = new PutObjectProc(PutObjectProc);
			putScopedClassProc = new PutScopedClassProc(PutScopedClassProc);
			putScopedObjectProc = new PutScopedObjectProc(PutScopedObjectProc);
			putSimpleReferenceProc = new PutSimpleReferenceProc(PutSimpleReferenceProc);
			putStringProc = new PutStringProc(PutStringProc);
			putTextProc = new PutTextProc(PutTextProc);
			putUnitFloatProc = new PutUnitFloatProc(PutUnitFloatProc);

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
			bufferProcsPtr = Memory.Allocate(Marshal.SizeOf(typeof(BufferProcs)), true);
			BufferProcs* bufferProcs = (BufferProcs*)bufferProcsPtr.ToPointer();
			bufferProcs->bufferProcsVersion = PSConstants.kCurrentBufferProcsVersion;
			bufferProcs->numBufferProcs = PSConstants.kCurrentBufferProcsCount;
			bufferProcs->allocateProc = Marshal.GetFunctionPointerForDelegate(allocProc);
			bufferProcs->freeProc = Marshal.GetFunctionPointerForDelegate(freeProc);
			bufferProcs->lockProc = Marshal.GetFunctionPointerForDelegate(lockProc);
			bufferProcs->unlockProc = Marshal.GetFunctionPointerForDelegate(unlockProc);
			bufferProcs->spaceProc = Marshal.GetFunctionPointerForDelegate(spaceProc);

			handleProcsPtr = Memory.Allocate(Marshal.SizeOf(typeof(HandleProcs)), true);
			HandleProcs* handleProcs = (HandleProcs*)handleProcsPtr.ToPointer();
			handleProcs->handleProcsVersion = PSConstants.kCurrentHandleProcsVersion;
			handleProcs->numHandleProcs = PSConstants.kCurrentHandleProcsCount;
			handleProcs->newProc = Marshal.GetFunctionPointerForDelegate(handleNewProc);
			handleProcs->disposeProc = Marshal.GetFunctionPointerForDelegate(handleDisposeProc);
			handleProcs->getSizeProc = Marshal.GetFunctionPointerForDelegate(handleGetSizeProc);
			handleProcs->setSizeProc = Marshal.GetFunctionPointerForDelegate(handleSetSizeProc);
			handleProcs->lockProc = Marshal.GetFunctionPointerForDelegate(handleLockProc);
			handleProcs->unlockProc = Marshal.GetFunctionPointerForDelegate(handleUnlockProc);
			handleProcs->recoverSpaceProc = Marshal.GetFunctionPointerForDelegate(handleRecoverSpaceProc);
			handleProcs->disposeRegularHandleProc = Marshal.GetFunctionPointerForDelegate(handleDisposeRegularProc);

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

			resourceProcsPtr = Memory.Allocate(Marshal.SizeOf(typeof(ResourceProcs)), true);
			ResourceProcs* resourceProcs = (ResourceProcs*)resourceProcsPtr.ToPointer();
			resourceProcs->resourceProcsVersion = PSConstants.kCurrentResourceProcsVersion;
			resourceProcs->numResourceProcs = PSConstants.kCurrentResourceProcsCount;
			resourceProcs->addProc = Marshal.GetFunctionPointerForDelegate(addResourceProc);
			resourceProcs->countProc = Marshal.GetFunctionPointerForDelegate(countResourceProc);
			resourceProcs->deleteProc = Marshal.GetFunctionPointerForDelegate(deleteResourceProc);
			resourceProcs->getProc = Marshal.GetFunctionPointerForDelegate(getResourceProc);

			readDescriptorPtr = Memory.Allocate(Marshal.SizeOf(typeof(ReadDescriptorProcs)), true);
			ReadDescriptorProcs* readDescriptor = (ReadDescriptorProcs*)readDescriptorPtr.ToPointer();
			readDescriptor->readDescriptorProcsVersion = PSConstants.kCurrentReadDescriptorProcsVersion;
			readDescriptor->numReadDescriptorProcs = PSConstants.kCurrentReadDescriptorProcsCount;
			readDescriptor->openReadDescriptorProc = Marshal.GetFunctionPointerForDelegate(openReadDescriptorProc);
			readDescriptor->closeReadDescriptorProc = Marshal.GetFunctionPointerForDelegate(closeReadDescriptorProc);
			readDescriptor->getAliasProc = Marshal.GetFunctionPointerForDelegate(getAliasProc);
			readDescriptor->getBooleanProc = Marshal.GetFunctionPointerForDelegate(getBooleanProc);
			readDescriptor->getClassProc = Marshal.GetFunctionPointerForDelegate(getClassProc);
			readDescriptor->getCountProc = Marshal.GetFunctionPointerForDelegate(getCountProc);
			readDescriptor->getEnumeratedProc = Marshal.GetFunctionPointerForDelegate(getEnumeratedProc);
			readDescriptor->getFloatProc = Marshal.GetFunctionPointerForDelegate(getFloatProc);
			readDescriptor->getIntegerProc = Marshal.GetFunctionPointerForDelegate(getIntegerProc);
			readDescriptor->getKeyProc = Marshal.GetFunctionPointerForDelegate(getKeyProc);
			readDescriptor->getObjectProc = Marshal.GetFunctionPointerForDelegate(getObjectProc);
			readDescriptor->getPinnedFloatProc = Marshal.GetFunctionPointerForDelegate(getPinnedFloatProc);
			readDescriptor->getPinnedIntegerProc = Marshal.GetFunctionPointerForDelegate(getPinnedIntegerProc);
			readDescriptor->getPinnedUnitFloatProc = Marshal.GetFunctionPointerForDelegate(getPinnedUnitFloatProc);
			readDescriptor->getSimpleReferenceProc = Marshal.GetFunctionPointerForDelegate(getSimpleReferenceProc);
			readDescriptor->getStringProc = Marshal.GetFunctionPointerForDelegate(getStringProc);
			readDescriptor->getTextProc = Marshal.GetFunctionPointerForDelegate(getTextProc);
			readDescriptor->getUnitFloatProc = Marshal.GetFunctionPointerForDelegate(getUnitFloatProc);

			writeDescriptorPtr = Memory.Allocate(Marshal.SizeOf(typeof(WriteDescriptorProcs)), true);
			WriteDescriptorProcs* writeDescriptor = (WriteDescriptorProcs*)writeDescriptorPtr.ToPointer();
			writeDescriptor->writeDescriptorProcsVersion = PSConstants.kCurrentWriteDescriptorProcsVersion;
			writeDescriptor->numWriteDescriptorProcs = PSConstants.kCurrentWriteDescriptorProcsCount;
			writeDescriptor->openWriteDescriptorProc = Marshal.GetFunctionPointerForDelegate(openWriteDescriptorProc);
			writeDescriptor->closeWriteDescriptorProc = Marshal.GetFunctionPointerForDelegate(closeWriteDescriptorProc);
			writeDescriptor->putAliasProc = Marshal.GetFunctionPointerForDelegate(putAliasProc);
			writeDescriptor->putBooleanProc = Marshal.GetFunctionPointerForDelegate(putBooleanProc);
			writeDescriptor->putClassProc = Marshal.GetFunctionPointerForDelegate(putClassProc);
			writeDescriptor->putCountProc = Marshal.GetFunctionPointerForDelegate(putCountProc);
			writeDescriptor->putEnumeratedProc = Marshal.GetFunctionPointerForDelegate(putEnumeratedProc);
			writeDescriptor->putFloatProc = Marshal.GetFunctionPointerForDelegate(putFloatProc);
			writeDescriptor->putIntegerProc = Marshal.GetFunctionPointerForDelegate(putIntegerProc);
			writeDescriptor->putObjectProc = Marshal.GetFunctionPointerForDelegate(putObjectProc);
			writeDescriptor->putScopedClassProc = Marshal.GetFunctionPointerForDelegate(putScopedClassProc);
			writeDescriptor->putScopedObjectProc = Marshal.GetFunctionPointerForDelegate(putScopedObjectProc);
			writeDescriptor->putSimpleReferenceProc = Marshal.GetFunctionPointerForDelegate(putSimpleReferenceProc);
			writeDescriptor->putStringProc = Marshal.GetFunctionPointerForDelegate(putStringProc);
			writeDescriptor->putTextProc = Marshal.GetFunctionPointerForDelegate(putTextProc);
			writeDescriptor->putUnitFloatProc = Marshal.GetFunctionPointerForDelegate(putUnitFloatProc);

			descriptorParametersPtr = Memory.Allocate(Marshal.SizeOf(typeof(PIDescriptorParameters)), true);
			PIDescriptorParameters* descriptorParameters = (PIDescriptorParameters*)descriptorParametersPtr.ToPointer();
			descriptorParameters->descriptorParametersVersion = PSConstants.kCurrentDescriptorParametersVersion;
			descriptorParameters->readDescriptorProcs = readDescriptorPtr;
			descriptorParameters->writeDescriptorProcs = writeDescriptorPtr;

			if (isRepeatEffect)
			{
				descriptorParameters->recordInfo = RecordInfo.plugInDialogNone;
			}
			else
			{
				descriptorParameters->recordInfo = RecordInfo.plugInDialogOptional;
			}


			if (aeteDict.Count > 0)
			{
				descriptorParameters->descriptor = HandleNewProc(0);
				if (isRepeatEffect)
				{
					descriptorParameters->playInfo = PlayInfo.plugInDialogDontDisplay;
				}
				else
				{
					descriptorParameters->playInfo = PlayInfo.plugInDialogDisplay;
				}
			}
			else
			{
				descriptorParameters->playInfo = PlayInfo.plugInDialogDisplay;
			}

			if (usePICASuites)
			{
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
			else
			{
				basicSuitePtr = IntPtr.Zero;
			}
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

			filterRecord->bufferSpace = BufferSpaceProc();
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

					if (tempDisplaySurface != null)
					{
						tempDisplaySurface.Dispose();
						tempDisplaySurface = null;
					}

					if (scaledChannelSurface != null)
					{
						scaledChannelSurface.Dispose();
						scaledChannelSurface = null;
					}

					if (convertedChannelSurface != null)
					{
						convertedChannelSurface.Dispose();
						convertedChannelSurface = null;
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

					if (imageMetaData != null)
					{
						imageMetaData.Dispose();
						imageMetaData = null;
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

				if (channelReadDescPtrs.Count > 0)
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
						HandleUnlockProc(descParam->descriptor);
						HandleDisposeProc(descParam->descriptor);
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
						if (isRepeatEffect && !IsHandleValid(filterRecord->parameters))
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
						else if (bufferIDs.Contains(filterRecord->parameters))
						{
							BufferFreeProc(filterRecord->parameters);
						}
						else
						{
							HandleUnlockProc(filterRecord->parameters);
							HandleDisposeProc(filterRecord->parameters);
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

					Memory.Free(filterRecordPtr);
					filterRecordPtr = IntPtr.Zero;
				}

				if (dataPtr != IntPtr.Zero)
				{
					if (isRepeatEffect && !IsHandleValid(dataPtr))
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
					else if (bufferIDs.Contains(dataPtr))
					{
						BufferFreeProc(dataPtr);
					}
					else
					{
						HandleUnlockProc(dataPtr);
						HandleDisposeProc(dataPtr);
					}
					dataPtr = IntPtr.Zero;
					pluginDataHandle = IntPtr.Zero;
				}

				// free any remaining buffer suite memory.
				if (bufferIDs.Count > 0)
				{
					for (int i = 0; i < bufferIDs.Count; i++)
					{
						Memory.Free(bufferIDs[i]);
					}
					bufferIDs = null;
				}

				// free any remaining handles
				if (handles.Count > 0)
				{
					foreach (var item in handles)
					{
						Memory.Free(item.Value.pointer);
						Memory.Free(item.Key);
					}
					handles = null;
				}
			}
		}

		#endregion
	}
}
