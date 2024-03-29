﻿/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterHostDll.PSApi.PICA;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.PSApi
{
    internal sealed class SPBasicSuiteProvider : IDisposable
    {
        private readonly IPICASuiteDataProvider picaSuiteData;
        private readonly IPropertySuite propertySuite;
        private readonly SPBasicAcquireSuite spAcquireSuite;
        private readonly SPBasicAllocateBlock spAllocateBlock;
        private readonly SPBasicFreeBlock spFreeBlock;
        private readonly SPBasicIsEqual spIsEqual;
        private readonly SPBasicReallocateBlock spReallocateBlock;
        private readonly SPBasicReleaseSuite spReleaseSuite;
        private readonly SPBasicUndefined spUndefined;

        private ActionSuiteProvider actionSuites;
        private PICABufferSuite bufferSuite;
        private PICAColorSpaceSuite colorSpaceSuite;
        private DescriptorRegistrySuite descriptorRegistrySuite;
        private ErrorSuite errorSuite;
        private PICAHandleSuite handleSuite;
        private PICAUIHooksSuite uiHooksSuite;
        private ASZStringSuite zstringSuite;

        private ActivePICASuites activePICASuites;
        private IColorPicker colorPicker;
        private string pluginName;
        private IntPtr descriptorHandle;
        private Dictionary<uint, AETEValue> scriptingData;
        private PluginAETE aete;
        private PluginSettingsRegistry pluginSettings;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SPBasicSuiteProvider"/> class.
        /// </summary>
        /// <param name="picaSuiteData">The filter record provider.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="picaSuiteData"/> is null.
        /// </exception>
        public SPBasicSuiteProvider(IPICASuiteDataProvider picaSuiteData) : this(picaSuiteData, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SPBasicSuiteProvider"/> class.
        /// </summary>
        /// <param name="picaSuiteData">The filter record provider.</param>
        /// <param name="propertySuite">The property suite.</param>
        /// <param name="colorPicker">The color picker.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="picaSuiteData"/> is null.
        /// </exception>
        public unsafe SPBasicSuiteProvider(IPICASuiteDataProvider picaSuiteData, IPropertySuite propertySuite, IColorPicker colorPicker)
        {
            if (picaSuiteData == null)
            {
                throw new ArgumentNullException(nameof(picaSuiteData));
            }

            this.picaSuiteData = picaSuiteData;
            this.propertySuite = propertySuite;
            this.colorPicker = colorPicker;
            spAcquireSuite = new SPBasicAcquireSuite(SPBasicAcquireSuite);
            spReleaseSuite = new SPBasicReleaseSuite(SPBasicReleaseSuite);
            spIsEqual = new SPBasicIsEqual(SPBasicIsEqual);
            spAllocateBlock = new SPBasicAllocateBlock(SPBasicAllocateBlock);
            spFreeBlock = new SPBasicFreeBlock(SPBasicFreeBlock);
            spReallocateBlock = new SPBasicReallocateBlock(SPBasicReallocateBlock);
            spUndefined = new SPBasicUndefined(SPBasicUndefined);
            actionSuites = new ActionSuiteProvider();
            activePICASuites = new ActivePICASuites();
            descriptorRegistrySuite = null;
            bufferSuite = null;
            colorSpaceSuite = null;
            errorSuite = null;
            handleSuite = null;
            disposed = false;
        }

        /// <summary>
        /// Gets the error suite message.
        /// </summary>
        /// <value>
        /// The error suite message.
        /// </value>
        public string ErrorSuiteMessage => errorSuite?.ErrorMessage;

        /// <summary>
        /// Sets the scripting information used by the plug-in.
        /// </summary>
        /// <value>
        /// The scripting information used by the plug-in.
        /// </value>
        public PluginAETE Aete
        {
            set => aete = value;
        }

        /// <summary>
        /// Sets the color picker.
        /// </summary>
        /// <value>
        /// The color picker.
        /// </value>
        /// <exception cref="ArgumentNullException">value is null.</exception>
        public IColorPicker ColorPicker
        {
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                colorPicker = value;
            }
        }

        private ASZStringSuite ASZStringSuite
        {
            get
            {
                if (zstringSuite == null)
                {
                    zstringSuite = new ASZStringSuite();
                }

                return zstringSuite;
            }
        }

        /// <summary>
        /// Creates the SPBasic suite pointer.
        /// </summary>
        /// <returns>An unmanaged pointer containing the SPBasic suite structure.</returns>
        public unsafe IntPtr CreateSPBasicSuitePointer()
        {
            IntPtr basicSuitePtr = Memory.Allocate(Marshal.SizeOf(typeof(SPBasicSuite)), true);

            SPBasicSuite* basicSuite = (SPBasicSuite*)basicSuitePtr.ToPointer();
            basicSuite->acquireSuite = Marshal.GetFunctionPointerForDelegate(spAcquireSuite);
            basicSuite->releaseSuite = Marshal.GetFunctionPointerForDelegate(spReleaseSuite);
            basicSuite->isEqual = Marshal.GetFunctionPointerForDelegate(spIsEqual);
            basicSuite->allocateBlock = Marshal.GetFunctionPointerForDelegate(spAllocateBlock);
            basicSuite->freeBlock = Marshal.GetFunctionPointerForDelegate(spFreeBlock);
            basicSuite->reallocateBlock = Marshal.GetFunctionPointerForDelegate(spReallocateBlock);
            basicSuite->undefined = Marshal.GetFunctionPointerForDelegate(spUndefined);

            return basicSuitePtr;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                if (actionSuites != null)
                {
                    actionSuites.Dispose();
                    actionSuites = null;
                }

                if (activePICASuites != null)
                {
                    activePICASuites.Dispose();
                    activePICASuites = null;
                }

                if (bufferSuite != null)
                {
                    bufferSuite.Dispose();
                    bufferSuite = null;
                }
            }
        }

        /// <summary>
        /// Gets the plug-in settings for the current session.
        /// </summary>
        /// <returns>
        /// A <see cref="PluginSettingsRegistry"/> containing the plug-in settings.
        /// </returns>
        public PluginSettingsRegistry GetPluginSettings()
        {
            if (descriptorRegistrySuite != null)
            {
                return descriptorRegistrySuite.GetPluginSettings();
            }

            return pluginSettings;
        }

        /// <summary>
        /// Sets the name of the plug-in.
        /// </summary>
        /// <param name="name">The name of the plug-in.</param>
        /// <exception cref="ArgumentNullException">name</exception>
        public void SetPluginName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            pluginName = name;
        }

        /// <summary>
        /// Sets the plug-in settings for the current session.
        /// </summary>
        /// <param name="settings">The plug-in settings.</param>
        public void SetPluginSettings(PluginSettingsRegistry settings)
        {
            pluginSettings = settings;
        }

        /// <summary>
        /// Sets the scripting data.
        /// </summary>
        /// <param name="descriptorHandle">The descriptor handle.</param>
        /// <param name="scriptingData">The scripting data.</param>
        public void SetScriptingData(IntPtr descriptorHandle, Dictionary<uint, AETEValue> scriptingData)
        {
            this.descriptorHandle = descriptorHandle;
            this.scriptingData = scriptingData;
        }

        /// <summary>
        /// Gets the scripting data associated with the specified descriptor handle.
        /// </summary>
        /// <param name="descriptorHandle">The descriptor handle.</param>
        /// <param name="scriptingData">The scripting data.</param>
        /// <returns><c>true</c> if the descriptor handle contains scripting data; otherwise, <c>false</c></returns>
        public bool TryGetScriptingData(IntPtr descriptorHandle, out Dictionary<uint, AETEValue> scriptingData)
        {
            if (actionSuites.DescriptorSuiteCreated)
            {
                return actionSuites.DescriptorSuite.TryGetScriptingData(descriptorHandle, out scriptingData);
            }

            scriptingData = null;
            return false;
        }

        private unsafe int SPBasicAcquireSuite(IntPtr name, int version, IntPtr* suite)
        {
            if (name == IntPtr.Zero || suite == null)
            {
                return PSError.kSPBadParameterError;
            }

            string suiteName = StringUtil.FromCString(name);
            if (suiteName == null)
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
                *suite = activePICASuites.AddRef(suiteKey);
            }
            else
            {
                error = AllocatePICASuite(suiteKey, ref *suite);
            }

            return error;
        }

        private unsafe void CreateActionDescriptorSuite()
        {
            if (!actionSuites.DescriptorSuiteCreated)
            {
                actionSuites.CreateDescriptorSuite(
                    aete,
                    descriptorHandle,
                    scriptingData,
                    ASZStringSuite);
            }
        }

        private int AllocatePICASuite(ActivePICASuites.PICASuiteKey suiteKey, ref IntPtr suitePointer)
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

                    if (bufferSuite == null)
                    {
                        bufferSuite = new PICABufferSuite();
                    }

                    PSBufferSuite1 suite = bufferSuite.CreateBufferSuite1();
                    suitePointer = activePICASuites.AllocateSuite(suiteKey, suite);
                }
                else if (suiteName.Equals(PSConstants.PICA.HandleSuite, StringComparison.Ordinal))
                {
                    if (version != 1 && version != 2)
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    if (handleSuite == null)
                    {
                        handleSuite = new PICAHandleSuite();
                    }

                    if (version == 1)
                    {
                        PSHandleSuite1 suite = handleSuite.CreateHandleSuite1();
                        suitePointer = activePICASuites.AllocateSuite(suiteKey, suite);
                    }
                    else if (version == 2)
                    {
                        PSHandleSuite2 suite = handleSuite.CreateHandleSuite2();
                        suitePointer = activePICASuites.AllocateSuite(suiteKey, suite);
                    }
                }
                else if (suiteName.Equals(PSConstants.PICA.PropertySuite, StringComparison.Ordinal))
                {
                    // The property suite can be null when the filter is showing its about box.
                    if (propertySuite == null || version != PSConstants.kCurrentPropertyProcsVersion)
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    PropertyProcs suite = propertySuite.CreatePropertySuite();
                    suitePointer = activePICASuites.AllocateSuite(suiteKey, suite);
                }
                else if (suiteName.Equals(PSConstants.PICA.UIHooksSuite, StringComparison.Ordinal))
                {
                    if (version != 1)
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    if (uiHooksSuite == null)
                    {
                        uiHooksSuite = new PICAUIHooksSuite(picaSuiteData.ParentWindowHandle, pluginName, ASZStringSuite);
                    }

                    PSUIHooksSuite1 suite = uiHooksSuite.CreateUIHooksSuite1(picaSuiteData);
                    suitePointer = activePICASuites.AllocateSuite(suiteKey, suite);
                }
                else if (suiteName.Equals(PSConstants.PICA.ActionDescriptorSuite, StringComparison.Ordinal))
                {
                    if (version != 2)
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }
                    if (!actionSuites.DescriptorSuiteCreated)
                    {
                        CreateActionDescriptorSuite();
                    }

                    PSActionDescriptorProc actionDescriptor = actionSuites.DescriptorSuite.CreateActionDescriptorSuite2();
                    suitePointer = activePICASuites.AllocateSuite(suiteKey, actionDescriptor);
                }
                else if (suiteName.Equals(PSConstants.PICA.ActionListSuite, StringComparison.Ordinal))
                {
                    if (version != 1)
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }
                    if (!actionSuites.ListSuiteCreated)
                    {
                        actionSuites.CreateListSuite(ASZStringSuite);
                    }

                    PSActionListProcs listSuite = actionSuites.ListSuite.CreateActionListSuite1();
                    suitePointer = activePICASuites.AllocateSuite(suiteKey, listSuite);
                }
                else if (suiteName.Equals(PSConstants.PICA.ActionReferenceSuite, StringComparison.Ordinal))
                {
                    if (version != 2)
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }
                    if (!actionSuites.ReferenceSuiteCreated)
                    {
                        actionSuites.CreateReferenceSuite();
                    }

                    PSActionReferenceProcs referenceSuite = actionSuites.ReferenceSuite.CreateActionReferenceSuite2();
                    suitePointer = activePICASuites.AllocateSuite(suiteKey, referenceSuite);
                }
                else if (suiteName.Equals(PSConstants.PICA.ASZStringSuite, StringComparison.Ordinal))
                {
                    if (version != 1)
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    ASZStringSuite1 stringSuite = ASZStringSuite.CreateASZStringSuite1();
                    suitePointer = activePICASuites.AllocateSuite(suiteKey, stringSuite);
                }
                else if (suiteName.Equals(PSConstants.PICA.ColorSpaceSuite, StringComparison.Ordinal))
                {
                    // The color picker can be null when the filter is showing its about box.
                    if (colorPicker == null || version != 1)
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    if (colorSpaceSuite == null)
                    {
                        colorSpaceSuite = new PICAColorSpaceSuite(ASZStringSuite, colorPicker);
                    }

                    PSColorSpaceSuite1 csSuite = colorSpaceSuite.CreateColorSpaceSuite1();
                    suitePointer = activePICASuites.AllocateSuite(suiteKey, csSuite);
                }
                else if (suiteName.Equals(PSConstants.PICA.DescriptorRegistrySuite, StringComparison.Ordinal))
                {
                    if (version != 1)
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    if (descriptorRegistrySuite == null)
                    {
                        if (!actionSuites.DescriptorSuiteCreated)
                        {
                            CreateActionDescriptorSuite();
                        }

                        descriptorRegistrySuite = new DescriptorRegistrySuite(actionSuites.DescriptorSuite);
                        if (pluginSettings != null)
                        {
                            descriptorRegistrySuite.SetPluginSettings(pluginSettings);
                        }
                    }

                    PSDescriptorRegistryProcs registrySuite = descriptorRegistrySuite.CreateDescriptorRegistrySuite1();
                    suitePointer = activePICASuites.AllocateSuite(suiteKey, registrySuite);
                }
                else if (suiteName.Equals(PSConstants.PICA.ErrorSuite, StringComparison.Ordinal))
                {
                    if (version != 1)
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    if (errorSuite == null)
                    {
                        errorSuite = new ErrorSuite(ASZStringSuite);
                    }

                    PSErrorSuite1 errorProcs = errorSuite.CreateErrorSuite1();
                    suitePointer = activePICASuites.AllocateSuite(suiteKey, errorProcs);
                }
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
            string suiteName = StringUtil.FromCString(name);

#if DEBUG
            DebugUtils.Ping(DebugFlags.SPBasicSuite, string.Format("name: {0}, version: {1}", suiteName, version.ToString()));
#endif

            ActivePICASuites.PICASuiteKey suiteKey = new ActivePICASuites.PICASuiteKey(suiteName, version);

            activePICASuites.Release(suiteKey);

            return PSError.kSPNoError;
        }

        private unsafe bool SPBasicIsEqual(IntPtr token1, IntPtr token2)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.SPBasicSuite, string.Format("token1: {0}, token2: {1}", StringUtil.FromCString(token1), StringUtil.FromCString(token2)));
#endif
            if (token1 == IntPtr.Zero)
            {
                if (token2 == IntPtr.Zero)
                {
                    return true;
                }

                return false;
            }
            else if (token2 == IntPtr.Zero)
            {
                return false;
            }

            // Compare two null-terminated ASCII strings for equality.
            byte* src = (byte*)token1.ToPointer();
            byte* dst = (byte*)token2.ToPointer();

            while (*dst != 0)
            {
                if ((*src - *dst) != 0)
                {
                    return false;
                }
                src++;
                dst++;
            }

            return true;
        }

        private unsafe int SPBasicAllocateBlock(int size, IntPtr* block)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.SPBasicSuite, string.Format("size: {0}", size));
#endif
            if (block == null)
            {
                return PSError.kSPBadParameterError;
            }

            try
            {
                *block = Memory.Allocate(size, false);
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

        private unsafe int SPBasicReallocateBlock(IntPtr block, int newSize, IntPtr* newblock)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.SPBasicSuite, string.Format("block: 0x{0}, size: {1}", block.ToHexString(), newSize));
#endif
            if (newblock == null)
            {
                return PSError.kSPBadParameterError;
            }

            try
            {
                *newblock = Memory.ReAlloc(block, newSize);
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
    }
}
