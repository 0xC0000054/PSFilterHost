﻿/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2018 Nicholas Hayes
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
    internal sealed class ActionDescriptorSuite : IActionDescriptorSuite, IDisposable
    {
        [Serializable]
        private sealed class ScriptingParameters
        {
            private Dictionary<uint, AETEValue> parameters;
            private List<uint> keys;

            public ScriptingParameters()
            {
                this.parameters = new Dictionary<uint, AETEValue>();
                this.keys = new List<uint>();
            }

            public ScriptingParameters(IDictionary<uint, AETEValue> items)
            {
                this.parameters = new Dictionary<uint, AETEValue>(items);
                this.keys = new List<uint>(items.Keys);
            }

            private ScriptingParameters(ScriptingParameters cloneMe)
            {
                this.parameters = new Dictionary<uint, AETEValue>(cloneMe.parameters);
                this.keys = new List<uint>(cloneMe.keys);
            }

            public int Count
            {
                get
                {
                    return this.parameters.Count;
                }
            }

            public void Add(uint key, AETEValue value)
            {
                if (this.parameters.ContainsKey(key))
                {
                    this.parameters[key] = value;
                }
                else
                {
                    this.parameters.Add(key, value);
                    this.keys.Add(key);
                }
            }

            public bool ContainsKey(uint key)
            {
                return this.parameters.ContainsKey(key);
            }

            public void Clear()
            {
                this.parameters.Clear();
                this.keys.Clear();
            }

            public ScriptingParameters Clone()
            {
                return new ScriptingParameters(this);
            }

            public uint GetKeyAtIndex(int index)
            {
                return this.keys[index];
            }

            public void Remove(uint key)
            {
                this.parameters.Remove(key);
                this.keys.Remove(key);
            }

            public bool TryGetValue(uint key, out AETEValue value)
            {
                return this.parameters.TryGetValue(key, out value);
            }

            public Dictionary<uint, AETEValue> ToDictionary()
            {
                return new Dictionary<uint, AETEValue>(this.parameters);
            }
        }

        private readonly PluginAETE aete;
        private readonly IActionListSuite actionListSuite;
        private readonly IActionReferenceSuite actionReferenceSuite;
        private readonly IASZStringSuite zstringSuite;

        private Dictionary<IntPtr, ScriptingParameters> actionDescriptors;
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
        public ActionDescriptorSuite(PluginAETE aete, IActionListSuite actionListSuite, IActionReferenceSuite actionReferenceSuite,
            IASZStringSuite zstringSuite)
        {
            if (actionListSuite == null)
            {
                throw new ArgumentNullException("actionListSuite");
            }
            if (actionReferenceSuite == null)
            {
                throw new ArgumentNullException("actionReferenceSuite");
            }
            if (zstringSuite == null)
            {
                throw new ArgumentNullException("zstringSuite");
            }

            this.make = new ActionDescriptorMake(Make);
            this.free = new ActionDescriptorFree(Free);
            this.handleToDescriptor = new ActionDescriptorHandleToDescriptor(HandleToDescriptor);
            this.asHandle = new ActionDescriptorAsHandle(AsHandle);
            this.getType = new ActionDescriptorGetType(GetType);
            this.getKey = new ActionDescriptorGetKey(GetKey);
            this.hasKey = new ActionDescriptorHasKey(HasKey);
            this.getCount = new ActionDescriptorGetCount(GetCount);
            this.isEqual = new ActionDescriptorIsEqual(IsEqual);
            this.erase = new ActionDescriptorErase(Erase);
            this.clear = new ActionDescriptorClear(Clear);
            this.hasKeys = new ActionDescriptorHasKeys(HasKeys);
            this.putInteger = new ActionDescriptorPutInteger(PutInteger);
            this.putFloat = new ActionDescriptorPutFloat(PutFloat);
            this.putUnitFloat = new ActionDescriptorPutUnitFloat(PutUnitFloat);
            this.putString = new ActionDescriptorPutString(PutString);
            this.putBoolean = new ActionDescriptorPutBoolean(PutBoolean);
            this.putList = new ActionDescriptorPutList(PutList);
            this.putObject = new ActionDescriptorPutObject(PutObject);
            this.putGlobalObject = new ActionDescriptorPutGlobalObject(PutGlobalObject);
            this.putEnumerated = new ActionDescriptorPutEnumerated(PutEnumerated);
            this.putReference = new ActionDescriptorPutReference(PutReference);
            this.putClass = new ActionDescriptorPutClass(PutClass);
            this.putGlobalClass = new ActionDescriptorPutGlobalClass(PutGlobalClass);
            this.putAlias = new ActionDescriptorPutAlias(PutAlias);
            this.putIntegers = new ActionDescriptorPutIntegers(PutIntegers);
            this.putZString = new ActionDescriptorPutZString(PutZString);
            this.putData = new ActionDescriptorPutData(PutData);
            this.getInteger = new ActionDescriptorGetInteger(GetInteger);
            this.getFloat = new ActionDescriptorGetFloat(GetFloat);
            this.getUnitFloat = new ActionDescriptorGetUnitFloat(GetUnitFloat);
            this.getStringLength = new ActionDescriptorGetStringLength(GetStringLength);
            this.getString = new ActionDescriptorGetString(GetString);
            this.getBoolean = new ActionDescriptorGetBoolean(GetBoolean);
            this.getList = new ActionDescriptorGetList(GetList);
            this.getObject = new ActionDescriptorGetObject(GetObject);
            this.getGlobalObject = new ActionDescriptorGetGlobalObject(GetGlobalObject);
            this.getEnumerated = new ActionDescriptorGetEnumerated(GetEnumerated);
            this.getReference = new ActionDescriptorGetReference(GetReference);
            this.getClass = new ActionDescriptorGetClass(GetClass);
            this.getGlobalClass = new ActionDescriptorGetGlobalClass(GetGlobalClass);
            this.getAlias = new ActionDescriptorGetAlias(GetAlias);
            this.getIntegers = new ActionDescriptorGetIntegers(GetIntegers);
            this.getZString = new ActionDescriptorGetZString(GetZString);
            this.getDataLength = new ActionDescriptorGetDataLength(GetDataLength);
            this.getData = new ActionDescriptorGetData(GetData);

            this.aete = aete;
            this.actionListSuite = actionListSuite;
            this.actionReferenceSuite = actionReferenceSuite;
            this.zstringSuite = zstringSuite;
            this.actionDescriptors = new Dictionary<IntPtr, ScriptingParameters>(IntPtrEqualityComparer.Instance);
            this.descriptorHandles = new Dictionary<IntPtr, ScriptingParameters>(IntPtrEqualityComparer.Instance);
            this.actionDescriptorsIndex = 0;
            HandleSuite.Instance.SuiteHandleDisposed += SuiteHandleDisposed;
            this.disposed = false;
        }

        bool IActionDescriptorSuite.TryGetDescriptorValues(IntPtr descriptor, out ReadOnlyDictionary<uint, AETEValue> values)
        {
            values = null;
            ScriptingParameters scriptingData;
            if (this.actionDescriptors.TryGetValue(descriptor, out scriptingData))
            {
                values = new ReadOnlyDictionary<uint, AETEValue>(scriptingData.ToDictionary());
                return true;
            }

            return false;
        }

        IntPtr IActionDescriptorSuite.CreateDescriptor(ReadOnlyDictionary<uint, AETEValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            IntPtr descriptor = GenerateDictionaryKey();
            this.actionDescriptors.Add(descriptor, new ScriptingParameters(values));

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
                Make = Marshal.GetFunctionPointerForDelegate(this.make),
                Free = Marshal.GetFunctionPointerForDelegate(this.free),
                GetType = Marshal.GetFunctionPointerForDelegate(this.getType),
                GetKey = Marshal.GetFunctionPointerForDelegate(this.getKey),
                HasKey = Marshal.GetFunctionPointerForDelegate(this.hasKey),
                GetCount = Marshal.GetFunctionPointerForDelegate(this.getCount),
                IsEqual = Marshal.GetFunctionPointerForDelegate(this.isEqual),
                Erase = Marshal.GetFunctionPointerForDelegate(this.erase),
                Clear = Marshal.GetFunctionPointerForDelegate(this.clear),
                PutInteger = Marshal.GetFunctionPointerForDelegate(this.putInteger),
                PutFloat = Marshal.GetFunctionPointerForDelegate(this.putFloat),
                PutUnitFloat = Marshal.GetFunctionPointerForDelegate(this.putUnitFloat),
                PutString = Marshal.GetFunctionPointerForDelegate(this.putString),
                PutBoolean = Marshal.GetFunctionPointerForDelegate(this.putBoolean),
                PutList = Marshal.GetFunctionPointerForDelegate(this.putList),
                PutObject = Marshal.GetFunctionPointerForDelegate(this.putObject),
                PutGlobalObject = Marshal.GetFunctionPointerForDelegate(this.putGlobalObject),
                PutEnumerated = Marshal.GetFunctionPointerForDelegate(this.putEnumerated),
                PutReference = Marshal.GetFunctionPointerForDelegate(this.putReference),
                PutClass = Marshal.GetFunctionPointerForDelegate(this.putClass),
                PutGlobalClass = Marshal.GetFunctionPointerForDelegate(this.putGlobalClass),
                PutAlias = Marshal.GetFunctionPointerForDelegate(this.putAlias),
                GetInteger = Marshal.GetFunctionPointerForDelegate(this.getInteger),
                GetFloat = Marshal.GetFunctionPointerForDelegate(this.getFloat),
                GetUnitFloat = Marshal.GetFunctionPointerForDelegate(this.getUnitFloat),
                GetStringLength = Marshal.GetFunctionPointerForDelegate(this.getStringLength),
                GetString = Marshal.GetFunctionPointerForDelegate(this.getString),
                GetBoolean = Marshal.GetFunctionPointerForDelegate(this.getBoolean),
                GetList = Marshal.GetFunctionPointerForDelegate(this.getList),
                GetObject = Marshal.GetFunctionPointerForDelegate(this.getObject),
                GetGlobalObject = Marshal.GetFunctionPointerForDelegate(this.getGlobalObject),
                GetEnumerated = Marshal.GetFunctionPointerForDelegate(this.getEnumerated),
                GetReference = Marshal.GetFunctionPointerForDelegate(this.getReference),
                GetClass = Marshal.GetFunctionPointerForDelegate(this.getClass),
                GetGlobalClass = Marshal.GetFunctionPointerForDelegate(this.getGlobalClass),
                GetAlias = Marshal.GetFunctionPointerForDelegate(this.getAlias),
                HasKeys = Marshal.GetFunctionPointerForDelegate(this.hasKeys),
                PutIntegers = Marshal.GetFunctionPointerForDelegate(this.putIntegers),
                GetIntegers = Marshal.GetFunctionPointerForDelegate(this.getIntegers),
                AsHandle = Marshal.GetFunctionPointerForDelegate(this.asHandle),
                HandleToDescriptor = Marshal.GetFunctionPointerForDelegate(this.handleToDescriptor),
                PutZString = Marshal.GetFunctionPointerForDelegate(this.putZString),
                GetZString = Marshal.GetFunctionPointerForDelegate(this.getZString),
                PutData = Marshal.GetFunctionPointerForDelegate(this.putData),
                GetDataLength = Marshal.GetFunctionPointerForDelegate(this.getDataLength),
                GetData = Marshal.GetFunctionPointerForDelegate(this.getData)
            };

            return suite;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!this.disposed)
            {
                this.disposed = true;

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
            if (this.descriptorHandles.TryGetValue(descriptorHandle, out parameters))
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
                throw new ArgumentNullException("descriptorHandle");
            }
            if (scriptingData == null)
            {
                throw new ArgumentNullException("scriptingData");
            }

            this.descriptorHandles.Add(descriptorHandle, new ScriptingParameters(scriptingData));
        }

        private void SuiteHandleDisposed(object sender, HandleDisposedEventArgs e)
        {
            this.descriptorHandles.Remove(e.Handle);
        }

        private IntPtr GenerateDictionaryKey()
        {
            this.actionDescriptorsIndex++;

            return new IntPtr(this.actionDescriptorsIndex);
        }

        private int Make(ref IntPtr descriptor)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif
            try
            {
                descriptor = GenerateDictionaryKey();
                this.actionDescriptors.Add(descriptor, new ScriptingParameters());
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int Free(IntPtr descriptor)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("descriptor: 0x{0}", descriptor.ToHexString()));
#endif
            this.actionDescriptors.Remove(descriptor);
            if (this.actionDescriptorsIndex == descriptor.ToInt32())
            {
                this.actionDescriptorsIndex--;
            }

            return PSError.kSPNoError;
        }

        private int HandleToDescriptor(IntPtr handle, ref IntPtr descriptor)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("handle: 0x{0}", handle.ToHexString()));
#endif
            ScriptingParameters parameters;
            if (this.descriptorHandles.TryGetValue(handle, out parameters))
            {
                try
                {
                    descriptor = GenerateDictionaryKey();
                    this.actionDescriptors.Add(descriptor, parameters);
                }
                catch (OutOfMemoryException)
                {
                    return PSError.memFullErr;
                }

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int AsHandle(IntPtr descriptor, ref IntPtr handle)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("descriptor: 0x{0}", descriptor.ToHexString()));
#endif
            handle = HandleSuite.Instance.NewHandle(0);
            if (handle == IntPtr.Zero)
            {
                return PSError.memFullErr;
            }
            try
            {
                this.descriptorHandles.Add(handle, this.actionDescriptors[descriptor].Clone());
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int GetType(IntPtr descriptor, uint key, ref uint type)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            AETEValue item = null;
            if (this.actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                // If the value is a sub-descriptor it must be retrieved with GetObject.
                if (item.Value is ScriptingParameters)
                {
                    type = DescriptorTypes.typeObject;
                }
                else
                {
                    type = item.Type;
                }

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private int GetKey(IntPtr descriptor, uint index, ref uint key)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("index: {0}", index));
#endif
            ScriptingParameters parameters = this.actionDescriptors[descriptor];

            if (index < parameters.Count)
            {
                key = parameters.GetKeyAtIndex((int)index);

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private int HasKey(IntPtr descriptor, uint key, ref bool value)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            value = this.actionDescriptors[descriptor].ContainsKey(key);

            return PSError.kSPNoError;
        }

        private int GetCount(IntPtr descriptor, ref uint count)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("descriptor: 0x{0}", descriptor.ToHexString()));
#endif
            ScriptingParameters parameters = this.actionDescriptors[descriptor];

            count = (uint)parameters.Count;

            return PSError.kSPNoError;
        }

        private int IsEqual(IntPtr firstDescriptor, IntPtr secondDescriptor, ref bool equal)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("first: 0x{0}, second: 0x{1}", firstDescriptor.ToHexString(), secondDescriptor.ToHexString()));
#endif
            equal = false;

            return PSError.kSPUnimplementedError;
        }

        private int Erase(IntPtr descriptor, uint key)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            this.actionDescriptors[descriptor].Remove(key);

            return PSError.kSPNoError;
        }

        private int Clear(IntPtr descriptor)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("descriptor: 0x{0}", descriptor.ToHexString()));
#endif
            this.actionDescriptors[descriptor].Clear();

            return PSError.kSPNoError;
        }

        private int HasKeys(IntPtr descriptor, IntPtr keyArray, ref bool value)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("descriptor: 0x{0}", descriptor.ToHexString()));
#endif

            if (keyArray != IntPtr.Zero)
            {
                ScriptingParameters parameters = this.actionDescriptors[descriptor];
                bool result = true;
                unsafe
                {
                    uint* key = (uint*)keyArray.ToPointer();

                    while (*key != 0U)
                    {
                        if (!parameters.ContainsKey(*key))
                        {
                            result = false;
                            break;
                        }

                        key++;
                    }
                }
                value = result;
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

        private int PutInteger(IntPtr descriptor, uint key, int data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            try
            {
                this.actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.typeInteger, GetAETEParamFlags(key), 0, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutFloat(IntPtr descriptor, uint key, double data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            try
            {
                this.actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.typeFloat, GetAETEParamFlags(key), 0, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutUnitFloat(IntPtr descriptor, uint key, uint unit, double data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            try
            {
                UnitFloat item = new UnitFloat(unit, data);

                this.actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.typeUintFloat, GetAETEParamFlags(key), 0, item));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }


        private int PutString(IntPtr descriptor, uint key, IntPtr cstrValue)
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
                int length = SafeNativeMethods.lstrlenA(cstrValue);
                byte[] data = new byte[length];
                Marshal.Copy(cstrValue, data, 0, length);

                this.actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.typeChar, GetAETEParamFlags(key), length, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutBoolean(IntPtr descriptor, uint key, byte data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            try
            {
                this.actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.typeBoolean, GetAETEParamFlags(key), 0, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutList(IntPtr descriptor, uint key, IntPtr data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            try
            {
                ActionDescriptorList value;
                if (this.actionListSuite.ConvertToActionDescriptor(data, out value))
                {
                    this.actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.typeValueList, GetAETEParamFlags(key), 0, value));
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

        private int PutObject(IntPtr descriptor, uint key, uint type, IntPtr descriptorHandle)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            ScriptingParameters subKeys;
            if (this.actionDescriptors.TryGetValue(descriptorHandle, out subKeys))
            {
                try
                {
                    this.actionDescriptors[descriptor].Add(key, new AETEValue(type, GetAETEParamFlags(key), 0, subKeys.Clone()));
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

        private int PutGlobalObject(IntPtr descriptor, uint key, uint type, IntPtr descriptorHandle)
        {
            return PutObject(descriptor, key, type, descriptorHandle);
        }

        private int PutEnumerated(IntPtr descriptor, uint key, uint type, uint data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            try
            {
                EnumeratedValue item = new EnumeratedValue(type, data);

                this.actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.typeEnumerated, GetAETEParamFlags(key), 0, item));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutReference(IntPtr descriptor, uint key, IntPtr reference)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            try
            {
                ActionDescriptorReference value;
                if (this.actionReferenceSuite.ConvertToActionDescriptor(reference, out value))
                {
                    this.actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.typeObjectReference, GetAETEParamFlags(key), 0, value));
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

        private int PutClass(IntPtr descriptor, uint key, uint data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            try
            {
                this.actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.typeClass, GetAETEParamFlags(key), 0, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutGlobalClass(IntPtr descriptor, uint key, uint data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            try
            {
                this.actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.typeGlobalClass, GetAETEParamFlags(key), 0, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutAlias(IntPtr descriptor, uint key, IntPtr aliasHandle)
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

                    this.actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.typeAlias, GetAETEParamFlags(key), size, data));
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

        private int PutIntegers(IntPtr descriptor, uint key, uint count, IntPtr arrayPointer)
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

                this.actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.typeInteger, GetAETEParamFlags(key), 0, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutZString(IntPtr descriptor, uint key, IntPtr zstring)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            try
            {
                ActionDescriptorZString value;
                if (zstringSuite.ConvertToActionDescriptor(zstring, out value))
                {
                    this.actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.typeChar, GetAETEParamFlags(key), 0, value));

                    return PSError.kSPNoError;
                }
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPBadParameterError;
        }

        private int PutData(IntPtr descriptor, uint key, int length, IntPtr blob)
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

                this.actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.typeRawData, GetAETEParamFlags(key), length, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }
        #endregion

        #region Descriptor read methods
        private int GetInteger(IntPtr descriptor, uint key, ref int data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            AETEValue item;
            if (this.actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                data = (int)item.Value;

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private int GetFloat(IntPtr descriptor, uint key, ref double data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            AETEValue item;
            if (this.actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                data = (double)item.Value;

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private int GetUnitFloat(IntPtr descriptor, uint key, ref uint unit, ref double data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            AETEValue item;
            if (this.actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                UnitFloat unitFloat = (UnitFloat)item.Value;

                try
                {
                    unit = unitFloat.Unit;
                }
                catch (NullReferenceException)
                {
                }

                data = unitFloat.Value;

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private int GetStringLength(IntPtr descriptor, uint key, ref uint length)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            AETEValue item;
            if (this.actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                byte[] bytes = (byte[])item.Value;

                length = (uint)bytes.Length;

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private int GetString(IntPtr descriptor, uint key, IntPtr cstrValue, uint maxLength)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            if (cstrValue == IntPtr.Zero)
            {
                return PSError.kSPBadParameterError;
            }

            AETEValue item;
            if (this.actionDescriptors[descriptor].TryGetValue(key, out item))
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

        private int GetBoolean(IntPtr descriptor, uint key, ref byte data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            AETEValue item;
            if (this.actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                data = (byte)item.Value;

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private int GetList(IntPtr descriptor, uint key, ref IntPtr data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            AETEValue item;
            if (this.actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                ActionDescriptorList value = (ActionDescriptorList)item.Value;

                try
                {
                    data = this.actionListSuite.CreateFromActionDescriptor(value);
                }
                catch (OutOfMemoryException)
                {
                    return PSError.memFullErr;
                }

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private int GetObject(IntPtr descriptor, uint key, ref uint retType, ref IntPtr outputDescriptor)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            AETEValue item;
            if (this.actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                uint type = item.Type;

                try
                {
                    retType = type;
                }
                catch (NullReferenceException)
                {
                    // ignore it
                }

                ScriptingParameters parameters = item.Value as ScriptingParameters;
                if (parameters != null)
                {
                    try
                    {
                        outputDescriptor = GenerateDictionaryKey();
                        this.actionDescriptors.Add(outputDescriptor, parameters);
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

        private int GetGlobalObject(IntPtr descriptor, uint key, ref uint retType, ref IntPtr outputDescriptor)
        {
            return GetObject(descriptor, key, ref retType, ref outputDescriptor);
        }

        private int GetEnumerated(IntPtr descriptor, uint key, ref uint type, ref uint data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            AETEValue item;
            if (this.actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                EnumeratedValue enumerated = (EnumeratedValue)item.Value;
                try
                {
                    type = enumerated.Type;
                }
                catch (NullReferenceException)
                {
                }

                data = enumerated.Value;

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private int GetReference(IntPtr descriptor, uint key, ref IntPtr reference)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            AETEValue item;
            if (this.actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                ActionDescriptorReference value = (ActionDescriptorReference)item.Value;

                try
                {
                    reference = this.actionReferenceSuite.CreateFromActionDescriptor(value);
                }
                catch (OutOfMemoryException)
                {
                    return PSError.memFullErr;
                }

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private int GetClass(IntPtr descriptor, uint key, ref uint data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            AETEValue item;
            if (this.actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                data = (uint)item.Value;

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private int GetGlobalClass(IntPtr descriptor, uint key, ref uint data)
        {
            return GetClass(descriptor, key, ref data);
        }

        private int GetAlias(IntPtr descriptor, uint key, ref IntPtr data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            AETEValue item;
            if (this.actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                int size = item.Size;
                data = HandleSuite.Instance.NewHandle(size);

                if (data == IntPtr.Zero)
                {
                    return PSError.memFullErr;
                }

                Marshal.Copy((byte[])item.Value, 0, HandleSuite.Instance.LockHandle(data, 0), size);
                HandleSuite.Instance.UnlockHandle(data);

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }


        private int GetIntegers(IntPtr descriptor, uint key, uint count, IntPtr data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            if (data == IntPtr.Zero)
            {
                return PSError.kSPBadParameterError;
            }

            AETEValue item;
            if (this.actionDescriptors[descriptor].TryGetValue(key, out item))
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

        private int GetZString(IntPtr descriptor, uint key, ref IntPtr zstring)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif

            AETEValue item;
            if (this.actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                ActionDescriptorZString value = (ActionDescriptorZString)item.Value;

                try
                {
                    zstring = zstringSuite.CreateFromActionDescriptor(value);
                }
                catch (OutOfMemoryException)
                {
                    return PSError.memFullErr;
                }

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private int GetDataLength(IntPtr descriptor, uint key, ref int length)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif

            AETEValue item;
            if (this.actionDescriptors[descriptor].TryGetValue(key, out item))
            {
                byte[] bytes = (byte[])item.Value;

                length = bytes.Length;

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private int GetData(IntPtr descriptor, uint key, IntPtr blob)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
            if (blob == IntPtr.Zero)
            {
                return PSError.kSPBadParameterError;
            }

            AETEValue item;
            if (this.actionDescriptors[descriptor].TryGetValue(key, out item))
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
