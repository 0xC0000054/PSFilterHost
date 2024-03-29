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

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.PSApi.PICA
{
    internal sealed class ActionDescriptorSuite : IActionDescriptorSuite, IDisposable
    {
        [Serializable]
        private sealed class ScriptingParameters
        {
            private Dictionary<uint, AETEValue> parameters;
            private List<uint> keys;

            public ScriptingParameters()
            {
                parameters = new Dictionary<uint, AETEValue>();
                keys = new List<uint>();
            }

            public ScriptingParameters(IDictionary<uint, AETEValue> items)
            {
                parameters = new Dictionary<uint, AETEValue>(items);
                keys = new List<uint>(items.Keys);
            }

            private ScriptingParameters(ScriptingParameters cloneMe)
            {
                parameters = new Dictionary<uint, AETEValue>(cloneMe.parameters);
                keys = new List<uint>(cloneMe.keys);
            }

            public int Count => parameters.Count;

            public void Add(uint key, AETEValue value)
            {
                if (parameters.ContainsKey(key))
                {
                    parameters[key] = value;
                }
                else
                {
                    parameters.Add(key, value);
                    keys.Add(key);
                }
            }

            public bool ContainsKey(uint key)
            {
                return parameters.ContainsKey(key);
            }

            public void Clear()
            {
                parameters.Clear();
                keys.Clear();
            }

            public ScriptingParameters Clone()
            {
                return new ScriptingParameters(this);
            }

            public uint GetKeyAtIndex(int index)
            {
                return keys[index];
            }

            public void Remove(uint key)
            {
                parameters.Remove(key);
                keys.Remove(key);
            }

            public bool TryGetValue(uint key, out AETEValue value)
            {
                return parameters.TryGetValue(key, out value);
            }

            public Dictionary<uint, AETEValue> ToDictionary()
            {
                return new Dictionary<uint, AETEValue>(parameters);
            }
        }

        private readonly PluginAETE aete;
        private readonly IActionListSuite actionListSuite;
        private readonly IActionReferenceSuite actionReferenceSuite;
        private readonly IASZStringSuite zstringSuite;

        private Dictionary<PIActionDescriptor, ScriptingParameters> actionDescriptors;
        private Dictionary<IntPtr, ScriptingParameters> descriptorHandles;
        private int actionDescriptorsIndex;
        private bool disposed;

        #region Callbacks
        private readonly ActionDescriptorMake make;
        private readonly ActionDescriptorFree free;
        private readonly ActionDescriptorHandleToDescriptor handleToDescriptor;
        private readonly ActionDescriptorAsHandle asHandle;
        private readonly ActionDescriptorGetType getType;
        private readonly ActionDescriptorGetKey getKey;
        private readonly ActionDescriptorHasKey hasKey;
        private readonly ActionDescriptorGetCount getCount;
        private readonly ActionDescriptorIsEqual isEqual;
        private readonly ActionDescriptorErase erase;
        private readonly ActionDescriptorClear clear;
        private readonly ActionDescriptorHasKeys hasKeys;
        private readonly ActionDescriptorPutInteger putInteger;
        private readonly ActionDescriptorPutFloat putFloat;
        private readonly ActionDescriptorPutUnitFloat putUnitFloat;
        private readonly ActionDescriptorPutString putString;
        private readonly ActionDescriptorPutBoolean putBoolean;
        private readonly ActionDescriptorPutList putList;
        private readonly ActionDescriptorPutObject putObject;
        private readonly ActionDescriptorPutGlobalObject putGlobalObject;
        private readonly ActionDescriptorPutEnumerated putEnumerated;
        private readonly ActionDescriptorPutReference putReference;
        private readonly ActionDescriptorPutClass putClass;
        private readonly ActionDescriptorPutGlobalClass putGlobalClass;
        private readonly ActionDescriptorPutAlias putAlias;
        private readonly ActionDescriptorPutIntegers putIntegers;
        private readonly ActionDescriptorPutZString putZString;
        private readonly ActionDescriptorPutData putData;
        private readonly ActionDescriptorGetInteger getInteger;
        private readonly ActionDescriptorGetFloat getFloat;
        private readonly ActionDescriptorGetUnitFloat getUnitFloat;
        private readonly ActionDescriptorGetStringLength getStringLength;
        private readonly ActionDescriptorGetString getString;
        private readonly ActionDescriptorGetBoolean getBoolean;
        private readonly ActionDescriptorGetList getList;
        private readonly ActionDescriptorGetObject getObject;
        private readonly ActionDescriptorGetGlobalObject getGlobalObject;
        private readonly ActionDescriptorGetEnumerated getEnumerated;
        private readonly ActionDescriptorGetReference getReference;
        private readonly ActionDescriptorGetClass getClass;
        private readonly ActionDescriptorGetGlobalClass getGlobalClass;
        private readonly ActionDescriptorGetAlias getAlias;
        private readonly ActionDescriptorGetIntegers getIntegers;
        private readonly ActionDescriptorGetZString getZString;
        private readonly ActionDescriptorGetDataLength getDataLength;
        private readonly ActionDescriptorGetData getData;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionDescriptorSuite"/> class.
        /// </summary>
        /// <param name="aete">The AETE scripting information.</param>
        /// <param name="actionListSuite">The action list suite instance.</param>
        /// <param name="actionReferenceSuite">The action reference suite instance.</param>
        /// <param name="zstringSuite">The ASZString suite instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="actionListSuite"/> is null.
        /// or
        /// <paramref name="actionReferenceSuite"/> is null.
        /// or
        /// <paramref name="zstringSuite"/> is null.
        /// </exception>
        public unsafe ActionDescriptorSuite(PluginAETE aete, IActionListSuite actionListSuite, IActionReferenceSuite actionReferenceSuite,
            IASZStringSuite zstringSuite)
        {
            if (actionListSuite == null)
            {
                throw new ArgumentNullException(nameof(actionListSuite));
            }
            if (actionReferenceSuite == null)
            {
                throw new ArgumentNullException(nameof(actionReferenceSuite));
            }
            if (zstringSuite == null)
            {
                throw new ArgumentNullException(nameof(zstringSuite));
            }

            make = new ActionDescriptorMake(Make);
            free = new ActionDescriptorFree(Free);
            handleToDescriptor = new ActionDescriptorHandleToDescriptor(HandleToDescriptor);
            asHandle = new ActionDescriptorAsHandle(AsHandle);
            getType = new ActionDescriptorGetType(GetType);
            getKey = new ActionDescriptorGetKey(GetKey);
            hasKey = new ActionDescriptorHasKey(HasKey);
            getCount = new ActionDescriptorGetCount(GetCount);
            isEqual = new ActionDescriptorIsEqual(IsEqual);
            erase = new ActionDescriptorErase(Erase);
            clear = new ActionDescriptorClear(Clear);
            hasKeys = new ActionDescriptorHasKeys(HasKeys);
            putInteger = new ActionDescriptorPutInteger(PutInteger);
            putFloat = new ActionDescriptorPutFloat(PutFloat);
            putUnitFloat = new ActionDescriptorPutUnitFloat(PutUnitFloat);
            putString = new ActionDescriptorPutString(PutString);
            putBoolean = new ActionDescriptorPutBoolean(PutBoolean);
            putList = new ActionDescriptorPutList(PutList);
            putObject = new ActionDescriptorPutObject(PutObject);
            putGlobalObject = new ActionDescriptorPutGlobalObject(PutGlobalObject);
            putEnumerated = new ActionDescriptorPutEnumerated(PutEnumerated);
            putReference = new ActionDescriptorPutReference(PutReference);
            putClass = new ActionDescriptorPutClass(PutClass);
            putGlobalClass = new ActionDescriptorPutGlobalClass(PutGlobalClass);
            putAlias = new ActionDescriptorPutAlias(PutAlias);
            putIntegers = new ActionDescriptorPutIntegers(PutIntegers);
            putZString = new ActionDescriptorPutZString(PutZString);
            putData = new ActionDescriptorPutData(PutData);
            getInteger = new ActionDescriptorGetInteger(GetInteger);
            getFloat = new ActionDescriptorGetFloat(GetFloat);
            getUnitFloat = new ActionDescriptorGetUnitFloat(GetUnitFloat);
            getStringLength = new ActionDescriptorGetStringLength(GetStringLength);
            getString = new ActionDescriptorGetString(GetString);
            getBoolean = new ActionDescriptorGetBoolean(GetBoolean);
            getList = new ActionDescriptorGetList(GetList);
            getObject = new ActionDescriptorGetObject(GetObject);
            getGlobalObject = new ActionDescriptorGetGlobalObject(GetGlobalObject);
            getEnumerated = new ActionDescriptorGetEnumerated(GetEnumerated);
            getReference = new ActionDescriptorGetReference(GetReference);
            getClass = new ActionDescriptorGetClass(GetClass);
            getGlobalClass = new ActionDescriptorGetGlobalClass(GetGlobalClass);
            getAlias = new ActionDescriptorGetAlias(GetAlias);
            getIntegers = new ActionDescriptorGetIntegers(GetIntegers);
            getZString = new ActionDescriptorGetZString(GetZString);
            getDataLength = new ActionDescriptorGetDataLength(GetDataLength);
            getData = new ActionDescriptorGetData(GetData);

            this.aete = aete;
            this.actionListSuite = actionListSuite;
            this.actionReferenceSuite = actionReferenceSuite;
            this.zstringSuite = zstringSuite;
            actionDescriptors = new Dictionary<PIActionDescriptor, ScriptingParameters>();
            descriptorHandles = new Dictionary<IntPtr, ScriptingParameters>(IntPtrEqualityComparer.Instance);
            actionDescriptorsIndex = 0;
            HandleSuite.Instance.SuiteHandleDisposed += SuiteHandleDisposed;
            disposed = false;
        }

        bool IActionDescriptorSuite.TryGetDescriptorValues(PIActionDescriptor descriptor, out ReadOnlyDictionary<uint, AETEValue> values)
        {
            values = null;
            ScriptingParameters scriptingData;
            if (actionDescriptors.TryGetValue(descriptor, out scriptingData))
            {
                values = new ReadOnlyDictionary<uint, AETEValue>(scriptingData.ToDictionary());
                return true;
            }

            return false;
        }

        PIActionDescriptor IActionDescriptorSuite.CreateDescriptor(ReadOnlyDictionary<uint, AETEValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            PIActionDescriptor descriptor = GenerateDictionaryKey();
            actionDescriptors.Add(descriptor, new ScriptingParameters(values));

            return descriptor;
        }

        /// <summary>
        /// Creates the action descriptor suite version 2 structure.
        /// </summary>
        /// <returns>A <see cref="PSActionDescriptorProc"/> containing the action descriptor suite callbacks.</returns>
        public PSActionDescriptorProc CreateActionDescriptorSuite2()
        {
            PSActionDescriptorProc suite = new PSActionDescriptorProc
            {
                Make = Marshal.GetFunctionPointerForDelegate(make),
                Free = Marshal.GetFunctionPointerForDelegate(free),
                GetType = Marshal.GetFunctionPointerForDelegate(getType),
                GetKey = Marshal.GetFunctionPointerForDelegate(getKey),
                HasKey = Marshal.GetFunctionPointerForDelegate(hasKey),
                GetCount = Marshal.GetFunctionPointerForDelegate(getCount),
                IsEqual = Marshal.GetFunctionPointerForDelegate(isEqual),
                Erase = Marshal.GetFunctionPointerForDelegate(erase),
                Clear = Marshal.GetFunctionPointerForDelegate(clear),
                PutInteger = Marshal.GetFunctionPointerForDelegate(putInteger),
                PutFloat = Marshal.GetFunctionPointerForDelegate(putFloat),
                PutUnitFloat = Marshal.GetFunctionPointerForDelegate(putUnitFloat),
                PutString = Marshal.GetFunctionPointerForDelegate(putString),
                PutBoolean = Marshal.GetFunctionPointerForDelegate(putBoolean),
                PutList = Marshal.GetFunctionPointerForDelegate(putList),
                PutObject = Marshal.GetFunctionPointerForDelegate(putObject),
                PutGlobalObject = Marshal.GetFunctionPointerForDelegate(putGlobalObject),
                PutEnumerated = Marshal.GetFunctionPointerForDelegate(putEnumerated),
                PutReference = Marshal.GetFunctionPointerForDelegate(putReference),
                PutClass = Marshal.GetFunctionPointerForDelegate(putClass),
                PutGlobalClass = Marshal.GetFunctionPointerForDelegate(putGlobalClass),
                PutAlias = Marshal.GetFunctionPointerForDelegate(putAlias),
                GetInteger = Marshal.GetFunctionPointerForDelegate(getInteger),
                GetFloat = Marshal.GetFunctionPointerForDelegate(getFloat),
                GetUnitFloat = Marshal.GetFunctionPointerForDelegate(getUnitFloat),
                GetStringLength = Marshal.GetFunctionPointerForDelegate(getStringLength),
                GetString = Marshal.GetFunctionPointerForDelegate(getString),
                GetBoolean = Marshal.GetFunctionPointerForDelegate(getBoolean),
                GetList = Marshal.GetFunctionPointerForDelegate(getList),
                GetObject = Marshal.GetFunctionPointerForDelegate(getObject),
                GetGlobalObject = Marshal.GetFunctionPointerForDelegate(getGlobalObject),
                GetEnumerated = Marshal.GetFunctionPointerForDelegate(getEnumerated),
                GetReference = Marshal.GetFunctionPointerForDelegate(getReference),
                GetClass = Marshal.GetFunctionPointerForDelegate(getClass),
                GetGlobalClass = Marshal.GetFunctionPointerForDelegate(getGlobalClass),
                GetAlias = Marshal.GetFunctionPointerForDelegate(getAlias),
                HasKeys = Marshal.GetFunctionPointerForDelegate(hasKeys),
                PutIntegers = Marshal.GetFunctionPointerForDelegate(putIntegers),
                GetIntegers = Marshal.GetFunctionPointerForDelegate(getIntegers),
                AsHandle = Marshal.GetFunctionPointerForDelegate(asHandle),
                HandleToDescriptor = Marshal.GetFunctionPointerForDelegate(handleToDescriptor),
                PutZString = Marshal.GetFunctionPointerForDelegate(putZString),
                GetZString = Marshal.GetFunctionPointerForDelegate(getZString),
                PutData = Marshal.GetFunctionPointerForDelegate(putData),
                GetDataLength = Marshal.GetFunctionPointerForDelegate(getDataLength),
                GetData = Marshal.GetFunctionPointerForDelegate(getData)
            };

            return suite;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                HandleSuite.Instance.SuiteHandleDisposed -= SuiteHandleDisposed;
            }
        }

        /// <summary>
        /// Tries to the get the scripting data from the specified descriptor handle.
        /// </summary>
        /// <param name="descriptorHandle">The descriptor handle.</param>
        /// <param name="scriptingData">The scripting data.</param>
        /// <returns><c>true</c> if the descriptor handle contains scripting data; otherwise, <c>false</c></returns>
        public bool TryGetScriptingData(IntPtr descriptorHandle, out Dictionary<uint, AETEValue> scriptingData)
        {
            scriptingData = null;

            ScriptingParameters parameters;
            if (descriptorHandles.TryGetValue(descriptorHandle, out parameters))
            {
                scriptingData = parameters.ToDictionary();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the scripting data.
        /// </summary>
        /// <param name="descriptorHandle">The descriptor handle.</param>
        /// <param name="scriptingData">The scripting data.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="descriptorHandle"/> is null.
        /// or
        /// <paramref name="scriptingData"/> is null.
        /// </exception>
        public void SetScriptingData(IntPtr descriptorHandle, Dictionary<uint, AETEValue> scriptingData)
        {
            if (descriptorHandle == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(descriptorHandle));
            }
            if (scriptingData == null)
            {
                throw new ArgumentNullException(nameof(scriptingData));
            }

            descriptorHandles.Add(descriptorHandle, new ScriptingParameters(scriptingData));
        }

        private void SuiteHandleDisposed(object sender, HandleDisposedEventArgs e)
        {
            descriptorHandles.Remove(e.Handle);
        }

        private PIActionDescriptor GenerateDictionaryKey()
        {
            actionDescriptorsIndex++;

            return new PIActionDescriptor(actionDescriptorsIndex);
        }

        private unsafe int Make(PIActionDescriptor* descriptor)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif
            if (descriptor == null)
            {
                return PSError.kSPBadParameterError;
            }

            try
            {
                *descriptor = GenerateDictionaryKey();
                actionDescriptors.Add(*descriptor, new ScriptingParameters());
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int Free(PIActionDescriptor descriptor)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("descriptor: {0}", descriptor.Index));
#endif
            actionDescriptors.Remove(descriptor);
            if (actionDescriptorsIndex == descriptor.Index)
            {
                actionDescriptorsIndex--;
            }

            return PSError.kSPNoError;
        }

        private unsafe int HandleToDescriptor(IntPtr handle, PIActionDescriptor* descriptor)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("handle: 0x{0}", handle.ToHexString()));
#endif
            if (descriptor == null)
            {
                return PSError.kSPBadParameterError;
            }

            ScriptingParameters parameters;
            if (descriptorHandles.TryGetValue(handle, out parameters))
            {
                try
                {
                    *descriptor = GenerateDictionaryKey();
                    actionDescriptors.Add(*descriptor, parameters);
                }
                catch (OutOfMemoryException)
                {
                    return PSError.memFullErr;
                }

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private unsafe int AsHandle(PIActionDescriptor descriptor, IntPtr* handle)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("descriptor: {0}", descriptor.Index));
#endif
            if (handle == null)
            {
                return PSError.kSPBadParameterError;
            }

            *handle = HandleSuite.Instance.NewHandle(0);
            if (*handle == IntPtr.Zero)
            {
                return PSError.memFullErr;
            }
            try
            {
                descriptorHandles.Add(*handle, actionDescriptors[descriptor].Clone());
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private unsafe int GetType(PIActionDescriptor descriptor, uint key, uint* type)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            if (type == null)
            {
                return PSError.kSPBadParameterError;
            }

            AETEValue item = null;
            if (actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                // If the value is a sub-descriptor it must be retrieved with GetObject.
                *type = item.Value is ScriptingParameters ? DescriptorTypes.Object : item.Type;

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private unsafe int GetKey(PIActionDescriptor descriptor, uint index, uint* key)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("index: {0}", index));
#endif
            if (key == null)
            {
                return PSError.kSPBadParameterError;
            }

            ScriptingParameters parameters = actionDescriptors[descriptor];

            if (index < parameters.Count)
            {
                *key = parameters.GetKeyAtIndex((int)index);

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private unsafe int HasKey(PIActionDescriptor descriptor, uint key, byte* value)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            if (value == null)
            {
                return PSError.kSPBadParameterError;
            }

            *value = actionDescriptors[descriptor].ContainsKey(key) ? (byte)1 : (byte)0;

            return PSError.kSPNoError;
        }

        private unsafe int GetCount(PIActionDescriptor descriptor, uint* count)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("descriptor: {0}", descriptor.Index));
#endif
            if (count == null)
            {
                return PSError.kSPBadParameterError;
            }

            ScriptingParameters parameters = actionDescriptors[descriptor];

            *count = (uint)parameters.Count;

            return PSError.kSPNoError;
        }

        private unsafe int IsEqual(PIActionDescriptor firstDescriptor, PIActionDescriptor secondDescriptor, byte* equal)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("first: {0}, second: {1}", firstDescriptor.Index, secondDescriptor.Index));
#endif
            if (equal == null)
            {
                return PSError.kSPBadParameterError;
            }

            *equal = 0;

            return PSError.kSPUnimplementedError;
        }

        private int Erase(PIActionDescriptor descriptor, uint key)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            actionDescriptors[descriptor].Remove(key);

            return PSError.kSPNoError;
        }

        private int Clear(PIActionDescriptor descriptor)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("descriptor: {0}", descriptor.Index));
#endif
            actionDescriptors[descriptor].Clear();

            return PSError.kSPNoError;
        }

        private unsafe int HasKeys(PIActionDescriptor descriptor, IntPtr keyArray, byte* value)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("descriptor: {0}", descriptor.Index));
#endif
            if (value == null)
            {
                return PSError.kSPBadParameterError;
            }

            if (keyArray != IntPtr.Zero)
            {
                ScriptingParameters parameters = actionDescriptors[descriptor];
                *value = 1;

                uint* key = (uint*)keyArray.ToPointer();

                while (*key != 0U)
                {
                    if (!parameters.ContainsKey(*key))
                    {
                        *value = 0;
                        break;
                    }

                    key++;
                }
            }
            else
            {
                return PSError.kSPBadParameterError;
            }

            return PSError.kSPNoError;
        }

        #region  Descriptor write methods
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

        private int PutInteger(PIActionDescriptor descriptor, uint key, int data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            try
            {
                actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.Integer, GetAETEParamFlags(key), 0, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutFloat(PIActionDescriptor descriptor, uint key, double data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            try
            {
                actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.Float, GetAETEParamFlags(key), 0, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutUnitFloat(PIActionDescriptor descriptor, uint key, uint unit, double data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            try
            {
                UnitFloat item = new UnitFloat(unit, data);

                actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.UintFloat, GetAETEParamFlags(key), 0, item));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutString(PIActionDescriptor descriptor, uint key, IntPtr cstrValue)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            if (cstrValue == IntPtr.Zero)
            {
                return PSError.kSPBadParameterError;
            }

            try
            {
                if (StringUtil.TryGetCStringLength(cstrValue, out int length))
                {
                    byte[] data = new byte[length];
                    Marshal.Copy(cstrValue, data, 0, length);

                    actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.Char, GetAETEParamFlags(key), length, data));
                }
                else
                {
                    // The string length exceeds int.MaxValue.
                    return PSError.memFullErr;
                }
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutBoolean(PIActionDescriptor descriptor, uint key, byte data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            try
            {
                actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.Boolean, GetAETEParamFlags(key), 0, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutList(PIActionDescriptor descriptor, uint key, PIActionList data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            try
            {
                ActionDescriptorList value;
                if (actionListSuite.ConvertToActionDescriptor(data, out value))
                {
                    actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.ValueList, GetAETEParamFlags(key), 0, value));
                }
                else
                {
                    return PSError.kSPBadParameterError;
                }
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutObject(PIActionDescriptor descriptor, uint key, uint type, PIActionDescriptor descriptorHandle)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            ScriptingParameters subKeys;
            if (actionDescriptors.TryGetValue(descriptorHandle, out subKeys))
            {
                try
                {
                    actionDescriptors[descriptor].Add(key, new AETEValue(type, GetAETEParamFlags(key), 0, subKeys.Clone()));
                }
                catch (OutOfMemoryException)
                {
                    return PSError.memFullErr;
                }
            }
            else
            {
                return PSError.kSPBadParameterError;
            }

            return PSError.kSPNoError;
        }

        private int PutGlobalObject(PIActionDescriptor descriptor, uint key, uint type, PIActionDescriptor descriptorHandle)
        {
            return PutObject(descriptor, key, type, descriptorHandle);
        }

        private int PutEnumerated(PIActionDescriptor descriptor, uint key, uint type, uint data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            try
            {
                EnumeratedValue item = new EnumeratedValue(type, data);

                actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.Enumerated, GetAETEParamFlags(key), 0, item));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutReference(PIActionDescriptor descriptor, uint key, PIActionReference reference)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            try
            {
                ActionDescriptorReference value;
                if (actionReferenceSuite.ConvertToActionDescriptor(reference, out value))
                {
                    actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.ObjectReference, GetAETEParamFlags(key), 0, value));
                }
                else
                {
                    return PSError.kSPBadParameterError;
                }
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutClass(PIActionDescriptor descriptor, uint key, uint data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            try
            {
                actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.Class, GetAETEParamFlags(key), 0, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutGlobalClass(PIActionDescriptor descriptor, uint key, uint data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            try
            {
                actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.GlobalClass, GetAETEParamFlags(key), 0, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutAlias(PIActionDescriptor descriptor, uint key, IntPtr aliasHandle)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            try
            {
                IntPtr hPtr = HandleSuite.Instance.LockHandle(aliasHandle, 0);

                try
                {
                    int size = HandleSuite.Instance.GetHandleSize(aliasHandle);
                    byte[] data = new byte[size];
                    Marshal.Copy(hPtr, data, 0, size);

                    actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.Alias, GetAETEParamFlags(key), size, data));
                }
                finally
                {
                    HandleSuite.Instance.UnlockHandle(aliasHandle);
                }
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutIntegers(PIActionDescriptor descriptor, uint key, uint count, IntPtr arrayPointer)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            if (arrayPointer == IntPtr.Zero)
            {
                return PSError.kSPBadParameterError;
            }

            try
            {
                int[] data = new int[count];

                unsafe
                {
                    int* ptr = (int*)arrayPointer;

                    for (int i = 0; i < count; i++)
                    {
                        data[i] = *ptr;
                        ptr++;
                    }
                }

                actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.Integer, GetAETEParamFlags(key), 0, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutZString(PIActionDescriptor descriptor, uint key, ASZString zstring)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            try
            {
                ActionDescriptorZString value;
                if (zstringSuite.ConvertToActionDescriptor(zstring, out value))
                {
                    actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.Char, GetAETEParamFlags(key), 0, value));

                    return PSError.kSPNoError;
                }
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPBadParameterError;
        }

        private int PutData(PIActionDescriptor descriptor, uint key, int length, IntPtr blob)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            if (blob == IntPtr.Zero || length < 0)
            {
                return PSError.kSPBadParameterError;
            }

            try
            {
                byte[] data = new byte[length];

                Marshal.Copy(blob, data, 0, length);

                actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.RawData, GetAETEParamFlags(key), length, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }
        #endregion

        #region Descriptor read methods
        private unsafe int GetInteger(PIActionDescriptor descriptor, uint key, int* data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            if (data == null)
            {
                return PSError.kSPBadParameterError;
            }

            AETEValue item;
            if (actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                *data = (int)item.Value;

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private unsafe int GetFloat(PIActionDescriptor descriptor, uint key, double* data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            if (data == null)
            {
                return PSError.kSPBadParameterError;
            }

            AETEValue item;
            if (actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                *data = (double)item.Value;

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private unsafe int GetUnitFloat(PIActionDescriptor descriptor, uint key, uint* unit, double* data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            if (data == null)
            {
                return PSError.kSPBadParameterError;
            }

            AETEValue item;
            if (actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                UnitFloat unitFloat = (UnitFloat)item.Value;

                if (unit != null)
                {
                    *unit = unitFloat.Unit;
                }

                *data = unitFloat.Value;

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private unsafe int GetStringLength(PIActionDescriptor descriptor, uint key, uint* length)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            if (length == null)
            {
                return PSError.kSPBadParameterError;
            }

            AETEValue item;
            if (actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                byte[] bytes = (byte[])item.Value;

                *length = (uint)bytes.Length;

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private int GetString(PIActionDescriptor descriptor, uint key, IntPtr cstrValue, uint maxLength)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            if (cstrValue == IntPtr.Zero)
            {
                return PSError.kSPBadParameterError;
            }

            AETEValue item;
            if (actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                if (maxLength > 0)
                {
                    byte[] bytes = (byte[])item.Value;

                    // Ensure that the buffer has room for the null terminator.
                    int length = (int)Math.Min(bytes.Length, maxLength - 1);

                    Marshal.Copy(bytes, 0, cstrValue, length);
                    Marshal.WriteByte(cstrValue, length, 0);
                }

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private unsafe int GetBoolean(PIActionDescriptor descriptor, uint key, byte* data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            if (data == null)
            {
                return PSError.kSPBadParameterError;
            }

            AETEValue item;
            if (actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                *data = (byte)item.Value;

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private unsafe int GetList(PIActionDescriptor descriptor, uint key, PIActionList* data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            if (data == null)
            {
                return PSError.kSPBadParameterError;
            }

            AETEValue item;
            if (actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                ActionDescriptorList value = (ActionDescriptorList)item.Value;

                try
                {
                    *data = actionListSuite.CreateFromActionDescriptor(value);
                }
                catch (OutOfMemoryException)
                {
                    return PSError.memFullErr;
                }

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private unsafe int GetObject(PIActionDescriptor descriptor, uint key, uint* retType, PIActionDescriptor* outputDescriptor)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            if (outputDescriptor == null)
            {
                return PSError.kSPBadParameterError;
            }

            AETEValue item;
            if (actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                if (retType != null)
                {
                    *retType = item.Type;
                }

                ScriptingParameters parameters = item.Value as ScriptingParameters;
                if (parameters != null)
                {
                    try
                    {
                        *outputDescriptor = GenerateDictionaryKey();
                        actionDescriptors.Add(*outputDescriptor, parameters);
                    }
                    catch (OutOfMemoryException)
                    {
                        return PSError.memFullErr;
                    }

                    return PSError.kSPNoError;
                }
            }

            return PSError.errMissingParameter;
        }

        private unsafe int GetGlobalObject(PIActionDescriptor descriptor, uint key, uint* retType, PIActionDescriptor* outputDescriptor)
        {
            return GetObject(descriptor, key, retType, outputDescriptor);
        }

        private unsafe int GetEnumerated(PIActionDescriptor descriptor, uint key, uint* type, uint* data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            if (data == null)
            {
                return PSError.kSPBadParameterError;
            }

            AETEValue item;
            if (actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                EnumeratedValue enumerated = (EnumeratedValue)item.Value;
                if (type != null)
                {
                    *type = enumerated.Type;
                }

                *data = enumerated.Value;

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private unsafe int GetReference(PIActionDescriptor descriptor, uint key, PIActionReference* reference)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            if (reference == null)
            {
                return PSError.kSPBadParameterError;
            }

            AETEValue item;
            if (actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                ActionDescriptorReference value = (ActionDescriptorReference)item.Value;

                try
                {
                    *reference = actionReferenceSuite.CreateFromActionDescriptor(value);
                }
                catch (OutOfMemoryException)
                {
                    return PSError.memFullErr;
                }

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private unsafe int GetClass(PIActionDescriptor descriptor, uint key, uint* data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            if (data == null)
            {
                return PSError.kSPBadParameterError;
            }

            AETEValue item;
            if (actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                *data = (uint)item.Value;

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private unsafe int GetGlobalClass(PIActionDescriptor descriptor, uint key, uint* data)
        {
            return GetClass(descriptor, key, data);
        }

        private unsafe int GetAlias(PIActionDescriptor descriptor, uint key, IntPtr* data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            if (data == null)
            {
                return PSError.kSPBadParameterError;
            }

            AETEValue item;
            if (actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                int size = item.Size;
                *data = HandleSuite.Instance.NewHandle(size);

                if (*data == IntPtr.Zero)
                {
                    return PSError.memFullErr;
                }

                Marshal.Copy((byte[])item.Value, 0, HandleSuite.Instance.LockHandle(*data, 0), size);
                HandleSuite.Instance.UnlockHandle(*data);

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private int GetIntegers(PIActionDescriptor descriptor, uint key, uint count, IntPtr data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            if (data == IntPtr.Zero)
            {
                return PSError.kSPBadParameterError;
            }

            AETEValue item;
            if (actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                int[] values = (int[])item.Value;
                if (count > values.Length)
                {
                    return PSError.kSPBadParameterError;
                }

                Marshal.Copy(values, 0, data, (int)count);

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private unsafe int GetZString(PIActionDescriptor descriptor, uint key, ASZString* zstring)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            if (zstring == null)
            {
                return PSError.kSPBadParameterError;
            }

            AETEValue item;
            if (actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                ActionDescriptorZString value = (ActionDescriptorZString)item.Value;

                try
                {
                    *zstring = zstringSuite.CreateFromActionDescriptor(value);
                }
                catch (OutOfMemoryException)
                {
                    return PSError.memFullErr;
                }

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private unsafe int GetDataLength(PIActionDescriptor descriptor, uint key, int* length)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            if (length == null)
            {
                return PSError.kSPBadParameterError;
            }

            AETEValue item;
            if (actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                byte[] bytes = (byte[])item.Value;

                *length = bytes.Length;

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private int GetData(PIActionDescriptor descriptor, uint key, IntPtr blob)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            if (blob == IntPtr.Zero)
            {
                return PSError.kSPBadParameterError;
            }

            AETEValue item;
            if (actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                byte[] data = (byte[])item.Value;

                Marshal.Copy(data, 0, blob, data.Length);

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }
        #endregion
    }
}
