/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2020 Nicholas Hayes
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
using PSFilterHostDll.Imaging;
using PSFilterHostDll.Properties;
using PSFilterHostDll.Interop;

#if !GDIPLUS
using System.Windows.Media.Imaging;
#endif

namespace PSFilterHostDll.PSApi
{
    internal sealed class LoadPsFilter : IDisposable, IFilterImageProvider, IPICASuiteDataProvider
    {
        private static bool RectNonEmpty(Rect16 rect)
        {
            return rect.left < rect.right && rect.top < rect.bottom;
        }

        private static readonly int OTOFHandleSize = IntPtr.Size + 4;
        private const int OTOFSignature = 0x464f544f;

        #region CallbackDelegates
        // MiscCallbacks
        private readonly AdvanceStateProc advanceStateProc;
        private readonly ColorServicesProc colorServicesProc;
        private readonly DisplayPixelsProc displayPixelsProc;
        private readonly HostProcs hostProc;
        private readonly ProcessEventProc processEventProc;
        private readonly ProgressProc progressProc;
        private readonly TestAbortProc abortProc;
        #endregion
        private readonly IntPtr parentWindowHandle;

        private IntPtr filterRecordPtr;
        private IntPtr platformDataPtr;
        private IntPtr bufferProcsPtr;
        private IntPtr handleProcsPtr;
        private IntPtr imageServicesProcsPtr;
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
        private IDisplayPixelsSurface displaySurface;
        private SurfaceGray8 tempMask;
        private SurfaceBase tempSurface;
        private Bitmap checkerBoardBitmap;

        private PluginPhase previousPhase;
        private PluginModule module;

        private IntPtr dataPtr;
        private short result;
        private string errorMessage;

        private FilterCase filterCase;
        private Region selectedRegion;

        private ImageModes imageMode;
        private HostRGBColor backgroundColor;
        private HostRGBColor foregroundColor;

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

        private bool disposed;
        private bool sizesSetup;
        private bool frValuesSetup;
        private bool copyToDest;
        private bool writesOutsideSelection;
        private bool useChannelPorts;
        private ColorProfileConverter colorProfileConverter;
        private byte[] documentColorProfile;
        private IColorPicker colorPicker;

        private ChannelPortsSuite channelPortsSuite;
        private DescriptorSuite descriptorSuite;
        private ImageServicesSuite imageServicesSuite;
        private PropertySuite propertySuite;
        private ReadImageDocument readImageDocument;
        private ResourceSuite resourceSuite;
        private SPBasicSuiteProvider basicSuiteProvider;

        /// <summary>
        /// The host signature of this library - '.NET'
        /// </summary>
        private const uint HostSignature = 0x2e4e4554;

        public SurfaceBase Dest => dest;

        /// <summary>
        /// Sets the filter progress callback.
        /// </summary>
        /// <param name="value">The progress callback delegate.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        internal void SetProgressFunc(ProgressProc value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
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
                throw new ArgumentNullException(nameof(value));
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
                throw new ArgumentNullException(nameof(value));
            }

            colorPicker = new CallbackColorPicker(value);
            basicSuiteProvider.ColorPicker = colorPicker;
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
                throw new ArgumentNullException(nameof(colorProfiles));
            }

            colorProfileConverter.Initialize(colorProfiles);
            documentColorProfile = colorProfiles.GetDocumentColorProfile();
        }

        /// <summary>
        /// Gets the plug-in settings for the current session.
        /// </summary>
        /// <returns>
        /// The plug-in settings for the current session.
        /// </returns>
        internal PluginSettingsRegistry GetPluginSettings()
        {
            return basicSuiteProvider.GetPluginSettings();
        }

        /// <summary>
        /// Sets the plug-in settings for the current session.
        /// </summary>
        /// <returns>
        /// The plug-in settings for the current session.
        /// </returns>
        internal void SetPluginSettings(PluginSettingsRegistry value)
        {
            basicSuiteProvider.SetPluginSettings(value);
        }

        public string ErrorMessage => errorMessage;

        internal ParameterData ParameterData
        {
            get => new ParameterData(globalParameters, scriptingData);
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                globalParameters = value.GlobalParameters;
                if (value.ScriptingData != null)
                {
                    scriptingData = value.ScriptingData;
                }
            }
        }

        /// <summary>
        /// Determines whether the filter should show its user interface.
        /// </summary>
        internal bool ShowUI
        {
            set => showUI = value;
        }

        internal PseudoResourceCollection PseudoResources
        {
            get => resourceSuite.PseudoResources;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                resourceSuite.PseudoResources = value;
            }
        }

        internal HostInformation HostInformation
        {
            get => propertySuite.HostInformation;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                propertySuite.HostInformation = value;
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
                throw new ArgumentNullException(nameof(sourceImage));
            }

            dataPtr = IntPtr.Zero;
            previousPhase = PluginPhase.None;
            disposed = false;
            copyToDest = true;
            writesOutsideSelection = false;
            sizesSetup = false;
            frValuesSetup = false;
            showUI = true;
            parameterDataRestored = false;
            pluginDataRestored = false;
            globalParameters = new GlobalParameters();
            scriptingData = null;
            errorMessage = string.Empty;
            filterParametersHandle = IntPtr.Zero;
            pluginDataHandle = IntPtr.Zero;
            inputHandling = FilterDataHandling.None;
            outputHandling = FilterDataHandling.None;
            parentWindowHandle = owner;

            abortFunc = null;
            progressFunc = null;
            colorPicker = new BuiltInColorPicker();
            descriptorSuite = new DescriptorSuite();
            resourceSuite = new ResourceSuite();

            useChannelPorts = false;
            colorProfileConverter = new ColorProfileConverter();
            documentColorProfile = null;

            lastInRect = Rect16.Empty;
            lastOutRect = Rect16.Empty;
            lastMaskRect = Rect16.Empty;
            lastInLoPlane = -1;
            lastOutRowBytes = 0;
            lastOutHiPlane = 0;
            lastOutLoPlane = -1;

#if GDIPLUS
            source = SurfaceFactory.CreateFromGdipBitmap(sourceImage, out imageMode);
#else
            source = SurfaceFactory.CreateFromBitmapSource(sourceImage, out imageMode);
#endif
            dest = SurfaceFactory.CreateFromImageMode(source.Width, source.Height, source.DpiX, source.DpiY, imageMode);

            advanceStateProc = new AdvanceStateProc(AdvanceStateProc);
            colorServicesProc = new ColorServicesProc(ColorServicesProc);
            displayPixelsProc = new DisplayPixelsProc(DisplayPixelsProc);
            hostProc = new HostProcs(HostProc);
            processEventProc = new ProcessEventProc(ProcessEvent);
            progressProc = new ProgressProc(ProgressProc);
            abortProc = new TestAbortProc(AbortProc);

            channelPortsSuite = new ChannelPortsSuite(this, imageMode);
            imageServicesSuite = new ImageServicesSuite();
            propertySuite = new PropertySuite(sourceImage, imageMode);
            readImageDocument = new ReadImageDocument(source.Width, source.Height, source.DpiX, source.DpiY, imageMode);
            basicSuiteProvider = new SPBasicSuiteProvider(this, propertySuite, colorPicker);

            selectedRegion = null;

            if (selection != null)
            {
                selection.Intersect(source.Bounds);
                Rectangle selectionBounds = selection.GetBoundsInt();

                if (!selectionBounds.IsEmpty && selectionBounds != source.Bounds)
                {
                    selectedRegion = selection.Clone();
                }
            }

            foregroundColor = new HostRGBColor(primary);
            backgroundColor = new HostRGBColor(secondary);

            unsafe
            {
                platformDataPtr = Memory.Allocate(Marshal.SizeOf(typeof(PlatformData)), true);
                ((PlatformData*)platformDataPtr.ToPointer())->hwnd = owner;
            }

#if DEBUG
            DebugFlags debugFlags = DebugFlags.AdvanceState;
            debugFlags |= DebugFlags.BufferSuite;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadPsFilter"/> class.
        /// This overload is only used when showing the about box.
        /// </summary>
        /// <param name="owner">The parent window handle.</param>
        private LoadPsFilter(IntPtr owner)
        {
            parentWindowHandle = owner;

            advanceStateProc = new AdvanceStateProc(AdvanceStateProc);
            colorServicesProc = new ColorServicesProc(ColorServicesProc);
            displayPixelsProc = new DisplayPixelsProc(DisplayPixelsProc);
            hostProc = new HostProcs(HostProc);
            processEventProc = new ProcessEventProc(ProcessEvent);
            progressProc = new ProgressProc(ProgressProc);
            abortProc = new TestAbortProc(AbortProc);

            basicSuiteProvider = new SPBasicSuiteProvider(this);
            basicSuitePtr = basicSuiteProvider.CreateSPBasicSuitePointer();

            unsafe
            {
                platformDataPtr = Memory.Allocate(Marshal.SizeOf(typeof(PlatformData)), true);
                ((PlatformData*)platformDataPtr.ToPointer())->hwnd = owner;
            }
        }

        SurfaceBase IFilterImageProvider.Source => source;

        SurfaceBase IFilterImageProvider.Destination => dest;

        SurfaceGray8 IFilterImageProvider.Mask => mask;

        IntPtr IPICASuiteDataProvider.ParentWindowHandle => parentWindowHandle;

        DisplayPixelsProc IPICASuiteDataProvider.DisplayPixels => displayPixelsProc;

        ProcessEventProc IPICASuiteDataProvider.ProcessEvent => processEventProc;

        ProgressProc IPICASuiteDataProvider.Progress => progressProc;

        TestAbortProc IPICASuiteDataProvider.TestAbort => abortProc;

        private void SetFilterTransparencyMode(PluginData data)
        {
            filterCase = data.GetFilterTransparencyMode(imageMode, selectedRegion != null, source.HasTransparency);
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
            NativeStructs.MEMORY_BASIC_INFORMATION mbi;
            int mbiSize = Marshal.SizeOf(typeof(NativeStructs.MEMORY_BASIC_INFORMATION));

            if (SafeNativeMethods.VirtualQuery(ptr, out mbi, new UIntPtr((ulong)mbiSize)) == UIntPtr.Zero)
            {
                return false;
            }

            const int ExecuteProtect = NativeConstants.PAGE_EXECUTE |
                                       NativeConstants.PAGE_EXECUTE_READ |
                                       NativeConstants.PAGE_EXECUTE_READWRITE |
                                       NativeConstants.PAGE_EXECUTE_WRITECOPY;

            return (mbi.Protect & ExecuteProtect) != 0;
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
                if (basicSuiteProvider.TryGetScriptingData(descriptorParameters->descriptor, out data))
                {
                    scriptingData = data;
                }
                else if (descriptorSuite.TryGetScriptingData(descriptorParameters->descriptor, out data))
                {
                    scriptingData = data;
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

                    globalParameters.SetParameterDataBytes(buf);
                    globalParameters.ParameterDataStorageMethod = GlobalParameters.DataStorageMethod.HandleSuite;
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
                                    globalParameters.SetParameterDataBytes(buf);
                                    globalParameters.ParameterDataStorageMethod = GlobalParameters.DataStorageMethod.OTOFHandle;
                                    // Some filters may store executable code in the parameter block.
                                    globalParameters.ParameterDataExecutable = IsMemoryExecutable(hPtr);
                                }
                            }
                            else
                            {
                                long pointerSize = SafeNativeMethods.GlobalSize(hPtr).ToInt64();
                                if (pointerSize > 0L || IsFakeIndirectPointer(hPtr, parameters, size, out pointerSize))
                                {
                                    byte[] buf = new byte[(int)pointerSize];

                                    Marshal.Copy(hPtr, buf, 0, buf.Length);
                                    globalParameters.SetParameterDataBytes(buf);
                                    globalParameters.ParameterDataStorageMethod = GlobalParameters.DataStorageMethod.HandleSuite;
                                }
                                else
                                {
                                    byte[] buf = new byte[(int)size];

                                    Marshal.Copy(parameters, buf, 0, buf.Length);
                                    globalParameters.SetParameterDataBytes(buf);
                                    globalParameters.ParameterDataStorageMethod = GlobalParameters.DataStorageMethod.RawBytes;
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
                if (HandleSuite.Instance.AllocatedBySuite(dataPtr))
                {
                    int ps = HandleSuite.Instance.GetHandleSize(dataPtr);
                    byte[] dataBuf = new byte[ps];

                    Marshal.Copy(HandleSuite.Instance.LockHandle(dataPtr, 0), dataBuf, 0, dataBuf.Length);
                    HandleSuite.Instance.UnlockHandle(dataPtr);

                    globalParameters.SetPluginDataBytes(dataBuf);
                    globalParameters.PluginDataStorageMethod = GlobalParameters.DataStorageMethod.HandleSuite;
                }
                else
                {
                    long pluginDataSize;
                    bool allocatedByBufferSuite;
                    IntPtr ptr;

                    if (BufferSuite.Instance.AllocatedBySuite(dataPtr))
                    {
                        pluginDataSize = BufferSuite.Instance.GetBufferSize(dataPtr);
                        allocatedByBufferSuite = true;
                        ptr = BufferSuite.Instance.LockBuffer(dataPtr);
                    }
                    else
                    {
                        pluginDataSize = SafeNativeMethods.GlobalSize(dataPtr).ToInt64();
                        allocatedByBufferSuite = false;
                        ptr = SafeNativeMethods.GlobalLock(dataPtr);
                    }

                    try
                    {
                        if (pluginDataSize == OTOFHandleSize && Marshal.ReadInt32(ptr, IntPtr.Size) == OTOFSignature)
                        {
                            IntPtr hPtr = Marshal.ReadIntPtr(ptr);
                            long ps = SafeNativeMethods.GlobalSize(hPtr).ToInt64();
                            if (ps > 0L)
                            {
                                byte[] dataBuf = new byte[(int)ps];
                                Marshal.Copy(hPtr, dataBuf, 0, dataBuf.Length);
                                globalParameters.SetPluginDataBytes(dataBuf);
                                globalParameters.PluginDataStorageMethod = GlobalParameters.DataStorageMethod.OTOFHandle;
                                globalParameters.PluginDataExecutable = IsMemoryExecutable(hPtr);
                            }
                        }
                        else if (pluginDataSize > 0)
                        {
                            byte[] dataBuf = new byte[(int)pluginDataSize];
                            Marshal.Copy(ptr, dataBuf, 0, dataBuf.Length);
                            globalParameters.SetPluginDataBytes(dataBuf);
                            globalParameters.PluginDataStorageMethod = GlobalParameters.DataStorageMethod.RawBytes;
                        }
                    }
                    finally
                    {
                        if (allocatedByBufferSuite)
                        {
                            BufferSuite.Instance.UnlockBuffer(dataPtr);
                        }
                        else
                        {
                            SafeNativeMethods.GlobalUnlock(ptr);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Restore the filter parameter handles for repeat runs.
        /// </summary>
        private unsafe void RestoreParameterHandles()
        {
            if (previousPhase == PluginPhase.Parameters)
            {
                return;
            }

            byte[] parameterDataBytes = globalParameters.GetParameterDataBytes();
            if (parameterDataBytes != null)
            {
                parameterDataRestored = true;
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
            }

            byte[] pluginDataBytes = globalParameters.GetPluginDataBytes();

            if (pluginDataBytes != null)
            {
                pluginDataRestored = true;
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
            }
        }

        private bool PluginAbout(PluginData pdata)
        {
            short result = PSError.noErr;

            using (PluginModule module = new PluginModule(pdata.FileName, pdata.EntryPoint))
            {
                IntPtr aboutRecordPtr = Memory.Allocate(Marshal.SizeOf(typeof(AboutRecord)), true);

                try
                {
                    unsafe
                    {
                        AboutRecord* about = (AboutRecord*)aboutRecordPtr.ToPointer();
                        about->platformData = platformDataPtr;
                        about->sSPBasic = basicSuitePtr;
                        about->plugInRef = IntPtr.Zero;
                    }
                    IntPtr dataPtr = IntPtr.Zero;

                    // If the filter only has one entry point call about on it.
                    if (pdata.ModuleEntryPoints == null)
                    {
                        module.entryPoint(FilterSelector.About, aboutRecordPtr, ref dataPtr, ref result);
                    }
                    else
                    {
                        // Otherwise call about on all the entry points in the module, per the SDK docs only one of the entry points will display the about box.
                        foreach (var entryPoint in pdata.ModuleEntryPoints)
                        {
                            PluginEntryPoint ep = module.GetEntryPoint(entryPoint);

                            ep(FilterSelector.About, aboutRecordPtr, ref dataPtr, ref result);

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
                    if (aboutRecordPtr != IntPtr.Zero)
                    {
                        Memory.Free(aboutRecordPtr);
                        aboutRecordPtr = IntPtr.Zero;
                    }
                }
            }

            if (result != PSError.noErr)
            {
                errorMessage = GetErrorMessage(result);
#if DEBUG
                DebugUtils.Ping(DebugFlags.Error, string.Format("filterSelectorAbout returned: {0}({1})", errorMessage, result));
#endif
                return false;
            }

            return true;
        }

        private unsafe bool PluginApply()
        {
#if DEBUG
            System.Diagnostics.Debug.Assert(previousPhase == PluginPhase.Prepare);
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

            if (filterRecord->autoMask)
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

            previousPhase = PluginPhase.Parameters;

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

            switch (filterCase)
            {
                case FilterCase.FloatingSelection:
                    DrawFloatingSelectionMask();
                    filterRecord->isFloating = true;
                    filterRecord->haveMask = true;
                    filterRecord->autoMask = false;
                    break;
                case FilterCase.FlatImageWithSelection:
                case FilterCase.EditableTransparencyWithSelection:
                case FilterCase.ProtectedTransparencyWithSelection:
                    DrawSelectionMask();
                    filterRecord->isFloating = false;
                    filterRecord->haveMask = true;
                    filterRecord->autoMask = !writesOutsideSelection;
                    break;
                case FilterCase.FlatImageNoSelection:
                case FilterCase.EditableTransparencyNoSelection:
                case FilterCase.ProtectedTransparencyNoSelection:
                    filterRecord->isFloating = false;
                    filterRecord->haveMask = false;
                    filterRecord->autoMask = false;
                    break;
                default:
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Unsupported filter case: {0}", filterCase));
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
            else if (imageMode == ImageModes.CMYK)
            {
                filterRecord->inLayerPlanes = 0;
                filterRecord->inTransparencyMask = 0;
                filterRecord->inNonLayerPlanes = 4;

                filterRecord->inColumnBytes = 1;

                filterRecord->outLayerPlanes = filterRecord->inLayerPlanes;
                filterRecord->outTransparencyMask = filterRecord->inTransparencyMask;
                filterRecord->outNonLayerPlanes = filterRecord->inNonLayerPlanes;
                filterRecord->outColumnBytes = filterRecord->inColumnBytes;
            }
            else if (imageMode == ImageModes.RGB || imageMode == ImageModes.RGB48)
            {
                switch (filterCase)
                {
                    case FilterCase.FlatImageNoSelection:
                    case FilterCase.FlatImageWithSelection:
                    case FilterCase.FloatingSelection:
                        filterRecord->inLayerPlanes = 0;
                        filterRecord->inTransparencyMask = 0;
                        filterRecord->inNonLayerPlanes = 3;
                        filterRecord->inColumnBytes = imageMode == ImageModes.RGB48 ? 6 : 3;
                        break;
                    case FilterCase.EditableTransparencyNoSelection:
                    case FilterCase.EditableTransparencyWithSelection:
                    case FilterCase.ProtectedTransparencyNoSelection:
                    case FilterCase.ProtectedTransparencyWithSelection:
                        filterRecord->inLayerPlanes = 3;
                        filterRecord->inTransparencyMask = 1;
                        filterRecord->inNonLayerPlanes = 0;
                        filterRecord->inColumnBytes = imageMode == ImageModes.RGB48 ? 8 : 4;
                        break;
                    default:
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Unsupported filter case: {0}", filterCase));
                }

                if (filterCase == FilterCase.ProtectedTransparencyNoSelection ||
                    filterCase == FilterCase.ProtectedTransparencyWithSelection)
                {
                    filterRecord->outLayerPlanes = 0;
                    filterRecord->outTransparencyMask = 0;
                    filterRecord->outNonLayerPlanes = 3;
                    filterRecord->outColumnBytes = imageMode == ImageModes.RGB48 ? 6 : 3;
                }
                else
                {
                    filterRecord->outLayerPlanes = filterRecord->inLayerPlanes;
                    filterRecord->outTransparencyMask = filterRecord->inTransparencyMask;
                    filterRecord->outNonLayerPlanes = filterRecord->inNonLayerPlanes;
                    filterRecord->outColumnBytes = filterRecord->inColumnBytes;
                }
            }
            else
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Image mode {0} is not supported.", imageMode));
            }

            filterRecord->inLayerMasks = 0;
            filterRecord->inInvertedLayerMasks = 0;

            filterRecord->outInvertedLayerMasks = filterRecord->inInvertedLayerMasks;
            filterRecord->outLayerMasks = filterRecord->inLayerMasks;

            filterRecord->absLayerPlanes = filterRecord->inLayerPlanes;
            filterRecord->absTransparencyMask = filterRecord->inTransparencyMask;
            filterRecord->absLayerMasks = filterRecord->inLayerMasks;
            filterRecord->absInvertedLayerMasks = filterRecord->inInvertedLayerMasks;
            filterRecord->absNonLayerPlanes = filterRecord->inNonLayerPlanes;

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
            previousPhase = PluginPhase.Prepare;
#endif

            return true;
        }

        /// <summary>
        /// Copies the source transparency to the destination when the filter ignores transparency.
        /// </summary>
        private unsafe void CopySourceTransparency()
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

            useChannelPorts = EnableChannelPorts(pdata);
            basicSuiteProvider.SetPluginName(pdata.Title.TrimEnd('.'));

            SetFilterTransparencyMode(pdata);

            if (pdata.FilterInfo != null)
            {
                FilterCaseInfo info = pdata.FilterInfo[(int)filterCase - 1];
                inputHandling = info.inputHandling;
                outputHandling = info.outputHandling;

                FilterCaseInfoFlags filterCaseFlags = info.flags1;

                copyToDest = (filterCaseFlags & FilterCaseInfoFlags.DontCopyToDestination) == FilterCaseInfoFlags.None;
                writesOutsideSelection = (filterCaseFlags & FilterCaseInfoFlags.WritesOutsideSelection) != FilterCaseInfoFlags.None;

                bool worksWithBlankData = (filterCaseFlags & FilterCaseInfoFlags.WorksWithBlankData) != FilterCaseInfoFlags.None;

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
            else if (filterCase != FilterCase.EditableTransparencyNoSelection && filterCase != FilterCase.EditableTransparencyWithSelection)
            {
                // Copy the source transparency to the destination if the filter does not modify it.
                CopySourceTransparency();
            }

            descriptorSuite.Aete = pdata.Aete;
            basicSuiteProvider.Aete = pdata.Aete;

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
            bool result = false;
            errorMessage = string.Empty;

            using (LoadPsFilter lps = new LoadPsFilter(owner))
            {
                result = lps.PluginAbout(pdata);
                if (!result)
                {
                    errorMessage = lps.errorMessage;
                }
            }

            return result;
        }

        private static string GetImageModeString(ImageModes mode)
        {
            string imageMode;
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
                case ImageModes.CMYK:
                    return Resources.CMYKMode;
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
                    message = basicSuiteProvider.ErrorSuiteMessage ?? StringUtil.FromPascalString(errorStringPtr, string.Empty);
                }
                else
                {
                    switch (error)
                    {
                        case PSError.filterBadMode:
                            message = string.Format(CultureInfo.CurrentCulture, Resources.FilterBadModeFormat, GetImageModeString(imageMode));
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
            return (hiPlane - loPlane + 1) == 1;
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

            long bufferSize = width * nplanes * height;

            return bufferSize != size;
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

            if (filterRecord->haveMask && RectNonEmpty(filterRecord->maskRect))
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
        private unsafe void ScaleTempSurface(Fixed16 inputRate, Rectangle lockRect)
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

            int scaleFactor = inputRate.ToInt32();
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
                    tempSurface.FitSurface(source);
                }
                else
                {
                    tempSurface = SurfaceFactory.CreateFromImageMode(source.Width, source.Height, imageMode);
                    tempSurface.CopySurface(source);
                }
            }
        }

        private static FilterPadding GetFilterPadding(Rect16 inRect, int requestedWidth, int requestedHeight, SurfaceBase surface, Fixed16? scaling)
        {
            int left = 0;
            int top = 0;
            int right = 0;
            int bottom = 0;

            if (inRect.left < 0)
            {
                left = -inRect.left;
                requestedWidth -= left;
            }

            if (inRect.top < 0)
            {
                top = -inRect.top;
                requestedHeight -= top;
            }

            int surfaceWidth;
            int surfaceHeight;

            if (scaling.HasValue)
            {
                int scaleFactor = scaling.Value.ToInt32();
                if (scaleFactor == 0)
                {
                    scaleFactor = 1;
                }

                surfaceWidth = surface.Width / scaleFactor;
                surfaceHeight = surface.Height / scaleFactor;
            }
            else
            {
                surfaceWidth = surface.Width;
                surfaceHeight = surface.Height;
            }

            if (requestedWidth > surfaceWidth)
            {
                right = requestedWidth - surfaceWidth;
            }

            if (requestedHeight > surfaceHeight)
            {
                bottom = requestedHeight - surfaceHeight;
            }

            return new FilterPadding(left, top, right, bottom);
        }

        /// <summary>
        /// Fills the input buffer with data from the source image.
        /// </summary>
        /// <param name="filterRecord">The filter record.</param>
        private unsafe short FillInputBuffer(FilterRecord* filterRecord)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.AdvanceState, string.Format("inRowBytes: {0}, Rect: {1}, loplane: {2}, hiplane: {3}, inputRate: {4}", new object[] { filterRecord->inRowBytes, filterRecord->inRect,
            filterRecord->inLoPlane, filterRecord->inHiPlane, filterRecord->inputRate.ToInt32() }));
#endif
            Rect16 inRect = filterRecord->inRect;

            int nplanes = filterRecord->inHiPlane - filterRecord->inLoPlane + 1;
            int width = inRect.right - inRect.left;
            int height = inRect.bottom - inRect.top;

            FilterPadding padding = GetFilterPadding(inRect, width, height, source, filterRecord->inputRate);

            Rectangle lockRect = new Rectangle(inRect.left + padding.left, inRect.top + padding.top, width - padding.Horizontal, height - padding.Vertical);

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

            bool validImageBounds = inRect.left < source.Width && inRect.top < source.Height;
            short padErr = SetFilterPadding(inDataPtr, stride, inRect, nplanes, channelOffset, filterRecord->inputPadding, padding, tempSurface);
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
                        byte* dst = (byte*)ptr + ((y - top + padding.top) * stride) + padding.left;

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
                        ushort* dst = (ushort*)ptr + ((y - top + padding.top) * stride) + padding.left;

                        for (int x = left; x < right; x++)
                        {
                            *dst = *src;

                            src++;
                            dst++;
                        }
                    }

                    break;
                case ImageModes.CMYK:

                    for (int y = top; y < bottom; y++)
                    {
                        byte* src = tempSurface.GetPointAddressUnchecked(left, y);
                        byte* dst = (byte*)ptr + ((y - top + padding.top) * stride) + padding.left;

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
                                    dst[0] = src[0];
                                    dst[1] = src[1];
                                    dst[2] = src[2];
                                    break;
                                case 4:
                                    dst[0] = src[0];
                                    dst[1] = src[1];
                                    dst[2] = src[2];
                                    dst[3] = src[3];
                                    break;
                            }

                            src += 4;
                            dst += nplanes;
                        }
                    }

                    break;
                case ImageModes.RGB:

                    for (int y = top; y < bottom; y++)
                    {
                        byte* src = tempSurface.GetPointAddressUnchecked(left, y);
                        byte* dst = (byte*)ptr + ((y - top + padding.top) * stride) + padding.left;

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
                        ushort* dst = (ushort*)ptr + ((y - top + padding.top) * stride) + padding.left;

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
#endif
            Rect16 outRect = filterRecord->outRect;
            int nplanes = filterRecord->outHiPlane - filterRecord->outLoPlane + 1;
            int width = outRect.right - outRect.left;
            int height = outRect.bottom - outRect.top;

            FilterPadding padding = GetFilterPadding(outRect, width, height, dest, null);

            Rectangle lockRect = new Rectangle(outRect.left + padding.left, outRect.top + padding.top, width - padding.Horizontal, height - padding.Vertical);
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

            short padErr = SetFilterPadding(outDataPtr, stride, outRect, nplanes, channelOffset, filterRecord->outputPadding, padding, dest);
            if (padErr != PSError.noErr || outRect.left >= dest.Width || outRect.top >= dest.Height)
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
                        byte* dst = (byte*)ptr + ((y - top + padding.top) * stride) + padding.left;

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
                        ushort* dst = (ushort*)ptr + ((y - top + padding.top) * stride) + padding.left;

                        for (int x = left; x < right; x++)
                        {
                            *dst = *src;

                            src++;
                            dst++;
                        }
                    }

                    break;
                case ImageModes.CMYK:

                    for (int y = top; y < bottom; y++)
                    {
                        byte* src = dest.GetPointAddressUnchecked(left, y);
                        byte* dst = (byte*)ptr + ((y - top + padding.top) * stride) + padding.left;

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
                                    dst[0] = src[0];
                                    dst[1] = src[1];
                                    dst[2] = src[2];
                                    break;
                                case 4:
                                    dst[0] = src[0];
                                    dst[1] = src[1];
                                    dst[2] = src[2];
                                    dst[3] = src[3];
                                    break;
                            }

                            src += 4;
                            dst += nplanes;
                        }
                    }

                    break;
                case ImageModes.RGB:

                    for (int y = top; y < bottom; y++)
                    {
                        byte* src = dest.GetPointAddressUnchecked(left, y);
                        byte* dst = (byte*)ptr + ((y - top + padding.top) * stride) + padding.left;

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
                        ushort* dst = (ushort*)ptr + ((y - top + padding.top) * stride) + padding.left;

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

        private unsafe void ScaleTempMask(Fixed16 maskRate, Rectangle lockRect)
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

            int scaleFactor = maskRate.ToInt32();

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
                    tempMask.FitSurface(mask);
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
            DebugUtils.Ping(DebugFlags.AdvanceState, string.Format("maskRowBytes: {0}, Rect: {1}, maskRate: {2}", new object[] { filterRecord->maskRowBytes, filterRecord->maskRect, filterRecord->maskRate.ToInt32() }));
#endif
            Rect16 maskRect = filterRecord->maskRect;
            int width = maskRect.right - maskRect.left;
            int height = maskRect.bottom - maskRect.top;

            FilterPadding padding = GetFilterPadding(maskRect, width, height, mask, filterRecord->maskRate);

            Rectangle lockRect = new Rectangle(maskRect.left + padding.left, maskRect.top + padding.top, width - padding.Horizontal, height - padding.Vertical);

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

            bool validImageBounds = maskRect.left < source.Width && maskRect.top < source.Height;
            short err = SetFilterPadding(maskDataPtr, width, maskRect, 1, 0, filterRecord->maskPadding, padding, mask);
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
                byte* dst = ptr + ((y - top + padding.top) * width) + padding.left;
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
                int width = rect.right - rect.left;
                int height = rect.bottom - rect.top;

                int ofs = loplane;
                if (imageMode == ImageModes.RGB || imageMode == ImageModes.RGB48)
                {
                    switch (loplane)
                    {
                        case 0:
                            ofs = 2;
                            break;
                        case 2:
                            ofs = 0;
                            break;
                    }
                }

                FilterPadding padding = GetFilterPadding(rect, width, height, dest, null);

                Rectangle lockRect = new Rectangle(rect.left + padding.left, rect.top + padding.top, width - padding.Horizontal, height - padding.Vertical);

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
                            byte* src = (byte*)ptr + ((y - top + padding.top) * outRowBytes) + padding.left;
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
                            ushort* src = (ushort*)ptr + ((y - top + padding.top) * stride16) + padding.left;
                            ushort* dst = (ushort*)dest.GetPointAddressUnchecked(left, y);

                            for (int x = left; x < right; x++)
                            {
                                *dst = *src;

                                src++;
                                dst++;
                            }
                        }

                        break;
                    case ImageModes.CMYK:

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
                                        dst[0] = src[0];
                                        dst[1] = src[1];
                                        dst[2] = src[2];
                                        break;
                                    case 4:
                                        dst[0] = src[0];
                                        dst[1] = src[1];
                                        dst[2] = src[2];
                                        dst[3] = src[3];
                                        break;
                                }

                                src += nplanes;
                                dst += 4;
                            }
                        }

                        break;
                    case ImageModes.RGB:

                        for (int y = top; y < bottom; y++)
                        {
                            byte* src = (byte*)ptr + ((y - top + padding.top) * outRowBytes) + padding.left;
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
                            ushort* src = (ushort*)ptr + ((y - top + padding.top) * stride16) + padding.left;
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
                                        ptr[2] = backgroundColor.R;
                                        ptr[1] = backgroundColor.G;
                                        ptr[0] = backgroundColor.B;
                                        break;
                                    case FilterDataHandling.ForegroundZap:
                                        ptr[2] = foregroundColor.R;
                                        ptr[1] = foregroundColor.G;
                                        ptr[0] = foregroundColor.B;
                                        break;
                                    default:
                                        break;
                                }
                            }

                            ptr += 4;
                        }
                    }
                }
                else if (imageMode == ImageModes.RGB)
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
                                        ptr[2] = backgroundColor.R;
                                        ptr[1] = backgroundColor.G;
                                        ptr[0] = backgroundColor.B;
                                        break;
                                    case FilterDataHandling.ForegroundZap:
                                        ptr[2] = foregroundColor.R;
                                        ptr[1] = foregroundColor.G;
                                        ptr[0] = foregroundColor.B;
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
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Unsupported image mode: {0}", imageMode));
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

                    if (colorPicker.ShowDialog(prompt, ref red, ref green, ref blue))
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

                            info.colorComponents[0] = backgroundColor.R;
                            info.colorComponents[1] = backgroundColor.G;
                            info.colorComponents[2] = backgroundColor.B;
                            info.colorComponents[3] = 0;
                            break;
                        case SpecialColorID.ForegroundColor:

                            info.colorComponents[0] = foregroundColor.R;
                            info.colorComponents[1] = foregroundColor.G;
                            info.colorComponents[2] = foregroundColor.B;
                            info.colorComponents[3] = 0;
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

                    if (point->h >= 0 && point->h < source.Width && point->v >= 0 && point->v < source.Height)
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
                                case ImageModes.CMYK:
                                    info.colorComponents[0] = pixel[0];
                                    info.colorComponents[1] = pixel[1];
                                    info.colorComponents[2] = pixel[2];
                                    info.colorComponents[3] = pixel[3];
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

        private static unsafe void SetFilterEdgePadding8(IntPtr inData, int inRowBytes, Rect16 rect, int nplanes, short ofs, FilterPadding padding, SurfaceBase surface)
        {
            int height = rect.bottom - rect.top;
            int width = rect.right - rect.left;

            int row, col;

            byte* ptr = (byte*)inData.ToPointer();

            int srcChannelCount = surface.ChannelCount;

            if (padding.top > 0)
            {
                for (int y = 0; y < padding.top; y++)
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

            if (padding.left > 0)
            {
                for (int y = 0; y < height; y++)
                {
                    byte* src = surface.GetPointAddressUnchecked(0, y);
                    byte* dst = ptr + (y * inRowBytes);

                    for (int x = 0; x < padding.left; x++)
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

            if (padding.bottom > 0)
            {
                col = surface.Height - 1;
                int lockBottom = height - 1;
                for (int y = 0; y < padding.bottom; y++)
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

            if (padding.right > 0)
            {
                row = surface.Width - 1;
                int rowEnd = width - padding.right;
                for (int y = 0; y < height; y++)
                {
                    byte* src = surface.GetPointAddressUnchecked(row, y);
                    byte* dst = ptr + (y * inRowBytes) + rowEnd;

                    for (int x = 0; x < padding.right; x++)
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

        private static unsafe void SetFilterEdgePadding16(IntPtr inData, int inRowBytes, Rect16 rect, int nplanes, short ofs, FilterPadding padding, SurfaceBase surface)
        {
            int height = rect.bottom - rect.top;
            int width = rect.right - rect.left;

            int row, col;

            byte* ptr = (byte*)inData.ToPointer();

            int srcChannelCount = surface.ChannelCount;

            if (padding.top > 0)
            {
                for (int y = 0; y < padding.top; y++)
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

            if (padding.left > 0)
            {
                for (int y = 0; y < height; y++)
                {
                    ushort* src = (ushort*)surface.GetPointAddressUnchecked(0, y);
                    ushort* dst = (ushort*)ptr + (y * inRowBytes);

                    for (int x = 0; x < padding.left; x++)
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

            if (padding.bottom > 0)
            {
                col = surface.Height - 1;
                int lockBottom = height - 1;
                for (int y = 0; y < padding.bottom; y++)
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

            if (padding.right > 0)
            {
                row = surface.Width - 1;
                int rowEnd = width - padding.right;
                for (int y = 0; y < height; y++)
                {
                    ushort* src = (ushort*)surface.GetPointAddressUnchecked(row, y);
                    ushort* dst = (ushort*)ptr + (y * inRowBytes) + rowEnd;

                    for (int x = 0; x < padding.right; x++)
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
        /// <param name="paddingBounds">The padding bounds.</param>
        /// <param name="surface">The surface.</param>
        private static unsafe short SetFilterPadding(IntPtr inData, int inRowBytes, Rect16 rect, int nplanes, short ofs, short inputPadding, FilterPadding paddingBounds, SurfaceBase surface)
        {
            if (!paddingBounds.IsEmpty)
            {
                switch (inputPadding)
                {
                    case PSConstants.Padding.plugInWantsEdgeReplication:

                        switch (surface.BitsPerChannel)
                        {
                            case 16:
                                SetFilterEdgePadding16(inData, inRowBytes, rect, nplanes, ofs, paddingBounds, surface);
                                break;
                            case 8:
                                SetFilterEdgePadding8(inData, inRowBytes, rect, nplanes, ofs, paddingBounds, surface);
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

        private void SetupDisplaySurface(int width, int height, bool haveTransparencyMask)
        {
            if ((displaySurface == null) ||
                width != displaySurface.Width ||
                height != displaySurface.Height ||
                haveTransparencyMask != displaySurface.SupportsTransparency)
            {
                if (displaySurface != null)
                {
                    displaySurface.Dispose();
                    displaySurface = null;
                }


                if (haveTransparencyMask)
                {
                    displaySurface = new SurfaceBGRA32(width, height);
                }
                else
                {
                    displaySurface = new SurfaceBGR24(width, height);
                }
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
                using (Bitmap bmp = displaySurface.CreateAliasedBitmap())
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
                    if (checkerBoardBitmap == null ||
                        checkerBoardBitmap.Width != width ||
                        checkerBoardBitmap.Height != height)
                    {
                        DrawCheckerBoardBitmap(width, height);
                    }

                    // Use a temporary bitmap to prevent flickering when the image is rendered over the checker board.
                    using (Bitmap temp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                    {
                        Rectangle rect = new Rectangle(0, 0, width, height);

                        using (Graphics tempGr = Graphics.FromImage(temp))
                        {
                            tempGr.DrawImageUnscaled(checkerBoardBitmap, rect);
                            using (Bitmap bmp = displaySurface.CreateAliasedBitmap())
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
                (srcPixelMap.imageMode != PSConstants.plugInModeRGBColor &&
                srcPixelMap.imageMode != PSConstants.plugInModeGrayScale &&
                srcPixelMap.imageMode != PSConstants.plugInModeCMYKColor))
            {
                return PSError.filterBadParameters;
            }

            int width = srcRect.right - srcRect.left;
            int height = srcRect.bottom - srcRect.top;

            bool hasTransparencyMask = srcPixelMap.version >= 1 && srcPixelMap.masks != IntPtr.Zero;

            try
            {
                SetupDisplaySurface(width, height, hasTransparencyMask);
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

            int destColBytes = displaySurface.ChannelCount;

            if (srcPixelMap.imageMode == PSConstants.plugInModeGrayScale)
            {
                // Perform color correction if required and fall back to the uncorrected data if it fails.
                if (!colorProfileConverter.ColorCorrectionRequired ||
                    !colorProfileConverter.ColorCorrectGrayScale(srcPixelMap.baseAddr, srcPixelMap.rowBytes, new Point(left, top), displaySurface))
                {
                    for (int y = top; y < bottom; y++)
                    {
                        byte* src = baseAddr + (y * srcPixelMap.rowBytes) + left;
                        byte* dst = displaySurface.GetRowAddressUnchecked(y - top);

                        for (int x = 0; x < width; x++)
                        {
                            dst[0] = dst[1] = dst[2] = *src;

                            src += srcPixelMap.colBytes;
                            dst += destColBytes;
                        }
                    }
                }
            }
            else if (srcPixelMap.imageMode == PSConstants.plugInModeCMYKColor)
            {
                // Perform color correction if required and fall back to the uncorrected data if it fails.
                if (!colorProfileConverter.ColorCorrectionRequired ||
                    !colorProfileConverter.ColorCorrectCMYK(srcPixelMap.baseAddr, srcPixelMap.rowBytes, srcPixelMap.colBytes,
                                                            srcPixelMap.planeBytes, new Point(left, top), displaySurface))
                {
                    if (srcPixelMap.colBytes == 1)
                    {
                        int magentaPlaneOffset = srcPixelMap.planeBytes;
                        int yellowPlaneOffset = magentaPlaneOffset + srcPixelMap.planeBytes;
                        int blackPlaneOffset = yellowPlaneOffset + srcPixelMap.planeBytes;

                        for (int y = top; y < bottom; y++)
                        {
                            byte* cyanPlane = baseAddr + (y * srcPixelMap.rowBytes) + left;
                            byte* magentaPlane = cyanPlane + magentaPlaneOffset;
                            byte* yellowPlane = cyanPlane + yellowPlaneOffset;
                            byte* blackPlane = cyanPlane + blackPlaneOffset;

                            byte* dst = displaySurface.GetRowAddressUnchecked(y - top);

                            for (int x = 0; x < width; x++)
                            {
                                byte cyan = *cyanPlane;
                                byte magenta = *magentaPlane;
                                byte yellow = *yellowPlane;
                                byte black = *blackPlane;

                                int nRed = 255 - Math.Min(255, cyan * (255 - black) / 255 + black);
                                int nGreen = 255 - Math.Min(255, magenta * (255 - black) / 255 + black);
                                int nBlue = 255 - Math.Min(255, yellow * (255 - black) / 255 + black);

                                dst[2] = (byte)nRed;
                                dst[1] = (byte)nGreen;
                                dst[0] = (byte)nBlue;

                                cyanPlane++;
                                magentaPlane++;
                                yellowPlane++;
                                blackPlane++;
                                dst += destColBytes;
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
                                byte cyan = src[0];
                                byte magenta = src[1];
                                byte yellow = src[2];
                                byte black = src[3];

                                int nRed = 255 - Math.Min(255, cyan * (255 - black) / 255 + black);
                                int nGreen = 255 - Math.Min(255, magenta * (255 - black) / 255 + black);
                                int nBlue = 255 - Math.Min(255, yellow * (255 - black) / 255 + black);

                                dst[2] = (byte)nRed;
                                dst[1] = (byte)nGreen;
                                dst[0] = (byte)nBlue;

                                src += srcPixelMap.colBytes;
                                dst += destColBytes;
                            }
                        }
                    }
                }
            }
            else
            {
                // Perform color correction if required and fall back to the uncorrected data if it fails.
                if (!colorProfileConverter.ColorCorrectionRequired ||
                    !colorProfileConverter.ColorCorrectRGB(srcPixelMap.baseAddr,
                                                           srcPixelMap.rowBytes,
                                                           srcPixelMap.colBytes,
                                                           srcPixelMap.planeBytes,
                                                           new Point(left, top),
                                                           displaySurface))
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
                                dst += destColBytes;
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
                                dst += destColBytes;
                            }
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
                        if (srcMask->maskData != IntPtr.Zero && srcMask->colBytes != 0 && srcMask->rowBytes != 0)
                        {
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
                                    dst += destColBytes;
                                }
                            }
                        }

                        error = Display32BitBitmap(gr, dstCol, dstRow, allOpaque);
                    }
                    else
                    {
                        using (Bitmap bmp = displaySurface.CreateAliasedBitmap())
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

        private unsafe void DrawCheckerBoardBitmap(int width, int height)
        {
            checkerBoardBitmap?.Dispose();
            checkerBoardBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

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

        private unsafe void DrawSelectionMask()
        {
            mask = new SurfaceGray8(source.Width, source.Height);

            SafeNativeMethods.memset(mask.Scan0.Pointer, 0, new UIntPtr((ulong)mask.Scan0.Length));

            Rectangle[] scans = selectedRegion.GetRegionScansReadOnlyInt();

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
            progressFunc?.Invoke(done, total);
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

            if (imageMode == ImageModes.RGB || imageMode == ImageModes.RGB48)
            {
                switch (filterCase)
                {
                    case FilterCase.FlatImageNoSelection:
                    case FilterCase.FlatImageWithSelection:
                    case FilterCase.FloatingSelection:
                    case FilterCase.ProtectedTransparencyNoSelection:
                    case FilterCase.ProtectedTransparencyWithSelection:
                        filterRecord->planes = 3;
                        break;
                    case FilterCase.EditableTransparencyNoSelection:
                    case FilterCase.EditableTransparencyWithSelection:
                        filterRecord->planes = 4;
                        break;
                    default:
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Unsupported filter case: {0}", filterCase));
                }
            }
            else
            {
                switch (imageMode)
                {
                    case ImageModes.GrayScale:
                    case ImageModes.Gray16:
                        filterRecord->planes = 1;
                        break;
                    case ImageModes.CMYK:
                        filterRecord->planes = 4;
                        break;
                    default:
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Unsupported image mode: {0}", imageMode));
                }
            }

            propertySuite.NumberOfChannels = filterRecord->planes;
            filterRecord->floatCoord.h = 0;
            filterRecord->floatCoord.v = 0;
            filterRecord->filterRect.left = 0;
            filterRecord->filterRect.top = 0;
            filterRecord->filterRect.right = width;
            filterRecord->filterRect.bottom = height;

            filterRecord->imageHRes = new Fixed16((int)(source.DpiX + 0.5));
            filterRecord->imageVRes = new Fixed16((int)(source.DpiY + 0.5));

            filterRecord->wholeSize.h = width;
            filterRecord->wholeSize.v = height;
        }

        /// <summary>
        /// Setup the API suites used within the FilterRecord.
        /// </summary>
        private unsafe void SetupSuites()
        {
            bufferProcsPtr = BufferSuite.Instance.CreateBufferProcsPointer();
            handleProcsPtr = HandleSuite.Instance.CreateHandleProcsPointer();

            imageServicesProcsPtr = imageServicesSuite.CreateImageServicesSuitePointer();

            if (useChannelPorts)
            {
                channelPortsPtr = channelPortsSuite.CreateChannelPortsSuitePointer();
                readDocumentPtr = readImageDocument.CreateReadImageDocumentPointer(filterCase, selectedRegion != null);
            }
            else
            {
                channelPortsPtr = IntPtr.Zero;
                readDocumentPtr = IntPtr.Zero;
            }

            propertyProcsPtr = propertySuite.CreatePropertySuitePointer();

            resourceProcsPtr = resourceSuite.CreateResourceProcsPointer();
            readDescriptorPtr = descriptorSuite.CreateReadDescriptorPointer();
            writeDescriptorPtr = descriptorSuite.CreateWriteDescriptorPointer();

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
                basicSuiteProvider.SetScriptingData(descriptorParameters->descriptor, scriptingData);
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

            basicSuitePtr = basicSuiteProvider.CreateSPBasicSuitePointer();
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

            // The RGBColor structure uses the range of [0, 65535] instead of [0, 255].
            // Dividing 65535 by 255 produces a integer value of 257, floating point math is not required.
            const int RGBColorMultiplier = 257;

            filterRecord->background.red = (ushort)(backgroundColor.R * RGBColorMultiplier);
            filterRecord->background.green = (ushort)(backgroundColor.G * RGBColorMultiplier);
            filterRecord->background.blue = (ushort)(backgroundColor.B * RGBColorMultiplier);

            filterRecord->foreground.red = (ushort)(foregroundColor.R * RGBColorMultiplier);
            filterRecord->foreground.green = (ushort)(foregroundColor.G * RGBColorMultiplier);
            filterRecord->foreground.blue = (ushort)(foregroundColor.B * RGBColorMultiplier);

            // The backColor and foreColor fields are always in the native color space of the image.
            if (imageMode == ImageModes.RGB || imageMode == ImageModes.RGB48)
            {
                filterRecord->backColor[0] = backgroundColor.R;
                filterRecord->backColor[1] = backgroundColor.G;
                filterRecord->backColor[2] = backgroundColor.B;
                filterRecord->backColor[3] = 0;

                filterRecord->foreColor[0] = foregroundColor.R;
                filterRecord->foreColor[1] = foregroundColor.G;
                filterRecord->foreColor[2] = foregroundColor.B;
                filterRecord->foreColor[3] = 0;
            }
            else
            {
                ColorSpace nativeSpace;
                switch (imageMode)
                {
                    case ImageModes.GrayScale:
                    case ImageModes.Gray16:
                        nativeSpace = ColorSpace.GraySpace;
                        break;
                    case ImageModes.CMYK:
                        nativeSpace = ColorSpace.CMYKSpace;
                        break;
                    default:
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Unsupported image mode: {0}", imageMode));
                }

                if (!backgroundColor.ConvertToNativeSpace(nativeSpace, filterRecord->backColor))
                {
                    throw new FilterRunException(string.Format(CultureInfo.InvariantCulture, "Cannot convert the background color to {0}", nativeSpace));
                }

                if (!foregroundColor.ConvertToNativeSpace(nativeSpace, filterRecord->foreColor))
                {
                    throw new FilterRunException(string.Format(CultureInfo.InvariantCulture, "Cannot convert the foreground color to {0}", nativeSpace));
                }
            }

            filterRecord->bufferSpace = BufferSuite.Instance.AvailableSpace;
            filterRecord->maxSpace = filterRecord->bufferSpace;

            filterRecord->hostSig = HostSignature;
            filterRecord->hostProcs = Marshal.GetFunctionPointerForDelegate(hostProc);
            filterRecord->platformData = platformDataPtr;
            filterRecord->bufferProcs = bufferProcsPtr;
            filterRecord->resourceProcs = resourceProcsPtr;
            filterRecord->processEvent = Marshal.GetFunctionPointerForDelegate(processEventProc);
            filterRecord->displayPixels = Marshal.GetFunctionPointerForDelegate(displayPixelsProc);
            filterRecord->handleProcs = handleProcsPtr;
            // New in 3.0
            filterRecord->supportsDummyChannels = false;
            filterRecord->supportsAlternateLayouts = false;
            filterRecord->wantLayout = PSConstants.Layout.Traditional;
            filterRecord->filterCase = filterCase;
            filterRecord->dummyPlaneValue = -1;
            filterRecord->premiereHook = IntPtr.Zero;
            filterRecord->advanceState = Marshal.GetFunctionPointerForDelegate(advanceStateProc);

            filterRecord->supportsAbsolute = true;
            filterRecord->wantsAbsolute = false;
            filterRecord->getPropertyObsolete = propertySuite.GetPropertyCallback;
            filterRecord->cannotUndo = false;
            filterRecord->supportsPadding = true;
            filterRecord->inputPadding = PSConstants.Padding.plugInWantsErrorOnBoundsException; // default to the error case for filters that do not set the padding fields.
            filterRecord->outputPadding = PSConstants.Padding.plugInWantsErrorOnBoundsException;
            filterRecord->maskPadding = PSConstants.Padding.plugInWantsErrorOnBoundsException;
            filterRecord->samplingSupport = PSConstants.SamplingSupport.hostSupportsIntegralSampling;
            filterRecord->reservedByte = 0;
            filterRecord->inputRate = new Fixed16(1);
            filterRecord->maskRate = new Fixed16(1);
            filterRecord->colorServices = Marshal.GetFunctionPointerForDelegate(colorServicesProc);
            // New in 3.0.4
            filterRecord->imageServicesProcs = imageServicesProcsPtr;
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
            errorStringPtr = Memory.Allocate(256L, true);
            filterRecord->errorString = errorStringPtr;

            filterRecord->channelPortProcs = channelPortsPtr;
            filterRecord->documentInfo = readDocumentPtr;
            // New in 5.0
            filterRecord->sSPBasic = basicSuitePtr;
            filterRecord->plugInRef = IntPtr.Zero;

            switch (imageMode)
            {
                case ImageModes.GrayScale:
                case ImageModes.RGB:
                case ImageModes.CMYK:
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

                    if (channelPortsSuite != null)
                    {
                        channelPortsSuite.Dispose();
                        channelPortsSuite = null;
                    }

                    if (readImageDocument != null)
                    {
                        readImageDocument.Dispose();
                        readImageDocument = null;
                    }

                    if (propertySuite != null)
                    {
                        propertySuite.Dispose();
                        propertySuite = null;
                    }

                    if (colorProfileConverter != null)
                    {
                        colorProfileConverter.Dispose();
                        colorProfileConverter = null;
                    }

                    if (descriptorSuite != null)
                    {
                        descriptorSuite.Dispose();
                        descriptorSuite = null;
                    }

                    if (basicSuiteProvider != null)
                    {
                        basicSuiteProvider.Dispose();
                        basicSuiteProvider = null;
                    }
                }

                if (platformDataPtr != IntPtr.Zero)
                {
                    Memory.Free(platformDataPtr);
                    platformDataPtr = IntPtr.Zero;
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

                if (imageServicesProcsPtr != IntPtr.Zero)
                {
                    Memory.Free(imageServicesProcsPtr);
                    imageServicesProcsPtr = IntPtr.Zero;
                }

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
                ImageSurfaceMemory.DestroyHeap();
            }
        }

        #endregion

        private readonly struct FilterPadding
        {
            public readonly int left;
            public readonly int top;
            public readonly int right;
            public readonly int bottom;

            public FilterPadding(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }

            public int Horizontal => left + right;

            public bool IsEmpty => left == 0 && top == 0 && right == 0 && bottom == 0;

            public int Vertical => top + bottom;
        }

        private readonly struct HostRGBColor
        {
#if GDIPLUS
            public HostRGBColor(Color color)
#else
            public HostRGBColor(System.Windows.Media.Color color)
#endif
            {
                R = color.R;
                G = color.G;
                B = color.B;
            }

            public byte R { get; }

            public byte G { get; }

            public byte B { get; }

            public unsafe bool ConvertToNativeSpace(ColorSpace nativeSpace, byte* nativeSpaceBuffer)
            {
                byte c0 = R;
                byte c1 = G;
                byte c2 = B;
                byte c3 = 0;

                int error = ColorServicesConvert.Convert(ColorSpace.RGBSpace, nativeSpace, ref c0, ref c1, ref c2, ref c3);
                if (error != PSError.kSPNoError)
                {
                    return false;
                }

                nativeSpaceBuffer[0] = c0;
                nativeSpaceBuffer[1] = c1;
                nativeSpaceBuffer[2] = c2;
                nativeSpaceBuffer[3] = c3;

                return true;
            }
        }
    }
}
