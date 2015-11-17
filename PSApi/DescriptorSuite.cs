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

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.PSApi
{
	internal sealed class DescriptorSuite
	{
		private readonly OpenReadDescriptorProc openReadDescriptorProc;
		private readonly CloseReadDescriptorProc closeReadDescriptorProc;
		private readonly GetKeyProc getKeyProc;
		private readonly GetIntegerProc getIntegerProc;
		private readonly GetFloatProc getFloatProc;
		private readonly GetUnitFloatProc getUnitFloatProc;
		private readonly GetBooleanProc getBooleanProc;
		private readonly GetTextProc getTextProc;
		private readonly GetAliasProc getAliasProc;
		private readonly GetEnumeratedProc getEnumeratedProc;
		private readonly GetClassProc getClassProc;
		private readonly GetSimpleReferenceProc getSimpleReferenceProc;
		private readonly GetObjectProc getObjectProc;
		private readonly GetCountProc getCountProc;
		private readonly GetStringProc getStringProc;
		private readonly GetPinnedIntegerProc getPinnedIntegerProc;
		private readonly GetPinnedFloatProc getPinnedFloatProc;
		private readonly GetPinnedUnitFloatProc getPinnedUnitFloatProc;
		private readonly OpenWriteDescriptorProc openWriteDescriptorProc;
		private readonly CloseWriteDescriptorProc closeWriteDescriptorProc;
		private readonly PutIntegerProc putIntegerProc;
		private readonly PutFloatProc putFloatProc;
		private readonly PutUnitFloatProc putUnitFloatProc;
		private readonly PutBooleanProc putBooleanProc;
		private readonly PutTextProc putTextProc;
		private readonly PutAliasProc putAliasProc;
		private readonly PutEnumeratedProc putEnumeratedProc;
		private readonly PutClassProc putClassProc;
		private readonly PutSimpleReferenceProc putSimpleReferenceProc;
		private readonly PutObjectProc putObjectProc;
		private readonly PutCountProc putCountProc;
		private readonly PutStringProc putStringProc;
		private readonly PutScopedClassProc putScopedClassProc;
		private readonly PutScopedObjectProc putScopedObjectProc;

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
		private Dictionary<uint, AETEValue> scriptingData;
		private PluginAETE aete;

		public Dictionary<uint, AETEValue> ScriptingData
		{
			get
			{
				return this.scriptingData;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				this.scriptingData = value;
			}
		}

		public PluginAETE Aete
		{
			set
			{
				this.aete = value;
			}
		}

		public bool HasScriptingData
		{
			get
			{
				return this.scriptingData.Count > 0;
			}
		}

		public DescriptorSuite()
		{
			this.openReadDescriptorProc = new OpenReadDescriptorProc(OpenReadDescriptorProc);
			this.closeReadDescriptorProc = new CloseReadDescriptorProc(CloseReadDescriptorProc);
			this.getKeyProc = new GetKeyProc(GetKeyProc);
			this.getAliasProc = new GetAliasProc(GetAliasProc);
			this.getBooleanProc = new GetBooleanProc(GetBooleanProc);
			this.getClassProc = new GetClassProc(GetClassProc);
			this.getCountProc = new GetCountProc(GetCountProc);
			this.getEnumeratedProc = new GetEnumeratedProc(GetEnumeratedProc);
			this.getFloatProc = new GetFloatProc(GetFloatProc);
			this.getIntegerProc = new GetIntegerProc(GetIntegerProc);
			this.getObjectProc = new GetObjectProc(GetObjectProc);
			this.getPinnedFloatProc = new GetPinnedFloatProc(GetPinnedFloatProc);
			this.getPinnedIntegerProc = new GetPinnedIntegerProc(GetPinnedIntegerProc);
			this.getPinnedUnitFloatProc = new GetPinnedUnitFloatProc(GetPinnedUnitFloatProc);
			this.getSimpleReferenceProc = new GetSimpleReferenceProc(GetSimpleReferenceProc);
			this.getStringProc = new GetStringProc(GetStringProc);
			this.getTextProc = new GetTextProc(GetTextProc);
			this.getUnitFloatProc = new GetUnitFloatProc(GetUnitFloatProc);
			this.openWriteDescriptorProc = new OpenWriteDescriptorProc(OpenWriteDescriptorProc);
			this.closeWriteDescriptorProc = new CloseWriteDescriptorProc(CloseWriteDescriptorProc);
			this.putAliasProc = new PutAliasProc(PutAliasProc);
			this.putBooleanProc = new PutBooleanProc(PutBooleanProc);
			this.putClassProc = new PutClassProc(PutClassProc);
			this.putCountProc = new PutCountProc(PutCountProc);
			this.putEnumeratedProc = new PutEnumeratedProc(PutEnumeratedProc);
			this.putFloatProc = new PutFloatProc(PutFloatProc);
			this.putIntegerProc = new PutIntegerProc(PutIntegerProc);
			this.putObjectProc = new PutObjectProc(PutObjectProc);
			this.putScopedClassProc = new PutScopedClassProc(PutScopedClassProc);
			this.putScopedObjectProc = new PutScopedObjectProc(PutScopedObjectProc);
			this.putSimpleReferenceProc = new PutSimpleReferenceProc(PutSimpleReferenceProc);
			this.putStringProc = new PutStringProc(PutStringProc);
			this.putTextProc = new PutTextProc(PutTextProc);
			this.putUnitFloatProc = new PutUnitFloatProc(PutUnitFloatProc);

			this.descErr = PSError.noErr;
			this.scriptingData = new Dictionary<uint, AETEValue>();
			this.keys = null;
			this.aete = null;
			this.getKey = 0;
			this.getKeyIndex = 0;
			this.subKeys = null;
			this.subKeyIndex = 0;
			this.isSubKey = false;
		}

		public IntPtr CreateReadDescriptor()
		{
			IntPtr readDescriptorPtr = Memory.Allocate(Marshal.SizeOf(typeof(ReadDescriptorProcs)), true);

			unsafe
			{
				ReadDescriptorProcs* readDescriptor = (ReadDescriptorProcs*)readDescriptorPtr.ToPointer();
				readDescriptor->readDescriptorProcsVersion = PSConstants.kCurrentReadDescriptorProcsVersion;
				readDescriptor->numReadDescriptorProcs = PSConstants.kCurrentReadDescriptorProcsCount;
				readDescriptor->openReadDescriptorProc = Marshal.GetFunctionPointerForDelegate(this.openReadDescriptorProc);
				readDescriptor->closeReadDescriptorProc = Marshal.GetFunctionPointerForDelegate(this.closeReadDescriptorProc);
				readDescriptor->getAliasProc = Marshal.GetFunctionPointerForDelegate(this.getAliasProc);
				readDescriptor->getBooleanProc = Marshal.GetFunctionPointerForDelegate(this.getBooleanProc);
				readDescriptor->getClassProc = Marshal.GetFunctionPointerForDelegate(this.getClassProc);
				readDescriptor->getCountProc = Marshal.GetFunctionPointerForDelegate(this.getCountProc);
				readDescriptor->getEnumeratedProc = Marshal.GetFunctionPointerForDelegate(this.getEnumeratedProc);
				readDescriptor->getFloatProc = Marshal.GetFunctionPointerForDelegate(this.getFloatProc);
				readDescriptor->getIntegerProc = Marshal.GetFunctionPointerForDelegate(this.getIntegerProc);
				readDescriptor->getKeyProc = Marshal.GetFunctionPointerForDelegate(this.getKeyProc);
				readDescriptor->getObjectProc = Marshal.GetFunctionPointerForDelegate(this.getObjectProc);
				readDescriptor->getPinnedFloatProc = Marshal.GetFunctionPointerForDelegate(this.getPinnedFloatProc);
				readDescriptor->getPinnedIntegerProc = Marshal.GetFunctionPointerForDelegate(this.getPinnedIntegerProc);
				readDescriptor->getPinnedUnitFloatProc = Marshal.GetFunctionPointerForDelegate(this.getPinnedUnitFloatProc);
				readDescriptor->getSimpleReferenceProc = Marshal.GetFunctionPointerForDelegate(this.getSimpleReferenceProc);
				readDescriptor->getStringProc = Marshal.GetFunctionPointerForDelegate(this.getStringProc);
				readDescriptor->getTextProc = Marshal.GetFunctionPointerForDelegate(this.getTextProc);
				readDescriptor->getUnitFloatProc = Marshal.GetFunctionPointerForDelegate(this.getUnitFloatProc); 
			}

			return readDescriptorPtr;
		}

		public IntPtr CreateWriteDescriptor()
		{
			IntPtr writeDescriptorPtr = Memory.Allocate(Marshal.SizeOf(typeof(WriteDescriptorProcs)), true);

			unsafe
			{
				WriteDescriptorProcs* writeDescriptor = (WriteDescriptorProcs*)writeDescriptorPtr.ToPointer();
				writeDescriptor->writeDescriptorProcsVersion = PSConstants.kCurrentWriteDescriptorProcsVersion;
				writeDescriptor->numWriteDescriptorProcs = PSConstants.kCurrentWriteDescriptorProcsCount;
				writeDescriptor->openWriteDescriptorProc = Marshal.GetFunctionPointerForDelegate(this.openWriteDescriptorProc);
				writeDescriptor->closeWriteDescriptorProc = Marshal.GetFunctionPointerForDelegate(this.closeWriteDescriptorProc);
				writeDescriptor->putAliasProc = Marshal.GetFunctionPointerForDelegate(this.putAliasProc);
				writeDescriptor->putBooleanProc = Marshal.GetFunctionPointerForDelegate(this.putBooleanProc);
				writeDescriptor->putClassProc = Marshal.GetFunctionPointerForDelegate(this.putClassProc);
				writeDescriptor->putCountProc = Marshal.GetFunctionPointerForDelegate(this.putCountProc);
				writeDescriptor->putEnumeratedProc = Marshal.GetFunctionPointerForDelegate(this.putEnumeratedProc);
				writeDescriptor->putFloatProc = Marshal.GetFunctionPointerForDelegate(this.putFloatProc);
				writeDescriptor->putIntegerProc = Marshal.GetFunctionPointerForDelegate(this.putIntegerProc);
				writeDescriptor->putObjectProc = Marshal.GetFunctionPointerForDelegate(this.putObjectProc);
				writeDescriptor->putScopedClassProc = Marshal.GetFunctionPointerForDelegate(this.putScopedClassProc);
				writeDescriptor->putScopedObjectProc = Marshal.GetFunctionPointerForDelegate(this.putScopedObjectProc);
				writeDescriptor->putSimpleReferenceProc = Marshal.GetFunctionPointerForDelegate(this.putSimpleReferenceProc);
				writeDescriptor->putStringProc = Marshal.GetFunctionPointerForDelegate(this.putStringProc);
				writeDescriptor->putTextProc = Marshal.GetFunctionPointerForDelegate(this.putTextProc);
				writeDescriptor->putUnitFloatProc = Marshal.GetFunctionPointerForDelegate(this.putUnitFloatProc);
			}

			return writeDescriptorPtr;
		}

		#region ReadDescriptorProcs
		private unsafe IntPtr OpenReadDescriptorProc(IntPtr descriptor, IntPtr keyArray)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif
			if (scriptingData.Count > 0)
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
							DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key = {0}", DebugUtils.PropToString(*ptr)));
#endif

							keys.Add(*ptr);
							ptr++;
						}

						// trim the list to the actual values in the dictionary
						uint[] values = keys.ToArray();
						foreach (var item in values)
						{
							if (!scriptingData.ContainsKey(item))
							{
								keys.Remove(item);
							}
						}
					}

					if (keys.Count == 0)
					{
						keys.AddRange(scriptingData.Keys); // if the keyArray is a null pointer or if it does not contain any valid keys get them from the scriptingData.
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
							DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("subKey = {0}", DebugUtils.PropToString(*ptr)));
#endif

							subKeys.Add(*ptr);
							ptr++;
						}
					}
					isSubKey = true;
					subClassDict = null;
					subClassIndex = 0;

					if (scriptingData.ContainsKey(getKey) && scriptingData[getKey].Value is Dictionary<uint, AETEValue>)
					{
						subClassDict = (Dictionary<uint, AETEValue>)scriptingData[getKey].Value;
					}
					else
					{
						// trim the list to the actual values in the dictionary
						uint[] values = subKeys.ToArray();
						foreach (var item in values)
						{
							if (!scriptingData.ContainsKey(item))
							{
								subKeys.Remove(item);
							}
						}
					}

				}

				return HandleSuite.Instance.NewHandle(0); // return a dummy handle to the key value pairs
			}

			return IntPtr.Zero;
		}

		private short CloseReadDescriptorProc(IntPtr descriptor)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Empty);
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
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif
			if (descErr != PSError.noErr)
			{
				descErrValue = descErr;
			}

			if (scriptingData.Count > 0)
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

						AETEValue value = scriptingData[key];
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

					AETEValue value = scriptingData[key];
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
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = scriptingData[getKey];
			}

			data = (int)item.Value;

			return PSError.noErr;
		}

		private short GetFloatProc(IntPtr descriptor, ref double data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = scriptingData[getKey];
			}

			data = (double)item.Value;

			return PSError.noErr;
		}

		private short GetUnitFloatProc(IntPtr descriptor, ref uint unit, ref double data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = scriptingData[getKey];
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
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = scriptingData[getKey];
			}

			data = (byte)item.Value;

			return PSError.noErr;
		}

		private short GetTextProc(IntPtr descriptor, ref IntPtr data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = scriptingData[getKey];
			}

			int size = item.Size;
			data = HandleSuite.Instance.NewHandle(size);

			if (data == IntPtr.Zero)
			{
				return PSError.memFullErr;
			}

			Marshal.Copy((byte[])item.Value, 0, HandleSuite.Instance.LockHandle(data, 0), size);
			HandleSuite.Instance.UnlockHandle(data);

			return PSError.noErr;
		}

		private short GetAliasProc(IntPtr descriptor, ref IntPtr data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = scriptingData[getKey];
			}

			int size = item.Size;
			data = HandleSuite.Instance.NewHandle(size);

			if (data == IntPtr.Zero)
			{
				return PSError.memFullErr;
			}

			Marshal.Copy((byte[])item.Value, 0, HandleSuite.Instance.LockHandle(data, 0), size);
			HandleSuite.Instance.UnlockHandle(data);

			return PSError.noErr;
		}

		private short GetEnumeratedProc(IntPtr descriptor, ref uint type)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = scriptingData[getKey];
			}

			type = (uint)item.Value;

			return PSError.noErr;
		}

		private short GetClassProc(IntPtr descriptor, ref uint type)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = scriptingData[getKey];
			}

			type = (uint)item.Value;

			return PSError.noErr;
		}

		private short GetSimpleReferenceProc(IntPtr descriptor, ref PIDescriptorSimpleReference data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			if (scriptingData.ContainsKey(getKey))
			{
				data = (PIDescriptorSimpleReference)scriptingData[getKey].Value;
				return PSError.noErr;
			}
			return PSError.errPlugInHostInsufficient;
		}

		private short GetObjectProc(IntPtr descriptor, ref uint retType, ref IntPtr data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0}", DebugUtils.PropToString(getKey)));
#endif
			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = scriptingData[getKey];
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
					data = HandleSuite.Instance.NewHandle(0); // assign a zero byte handle to allow it to work correctly in the OpenReadDescriptorProc(). 
					break;

				case DescriptorTypes.typeAlias:
				case DescriptorTypes.typePath:
				case DescriptorTypes.typeChar:

					int size = item.Size;
					data = HandleSuite.Instance.NewHandle(size);

					if (data == IntPtr.Zero)
					{
						return PSError.memFullErr;
					}

					Marshal.Copy((byte[])item.Value, 0, HandleSuite.Instance.LockHandle(data, 0), size);
					HandleSuite.Instance.UnlockHandle(data);
					break;
				case DescriptorTypes.typeBoolean:
					data = HandleSuite.Instance.NewHandle(sizeof(Byte));

					if (data == IntPtr.Zero)
					{
						return PSError.memFullErr;
					}

					Marshal.WriteByte(HandleSuite.Instance.LockHandle(data, 0), (byte)item.Value);
					HandleSuite.Instance.UnlockHandle(data);
					break;
				case DescriptorTypes.typeInteger:
					data = HandleSuite.Instance.NewHandle(sizeof(Int32));

					if (data == IntPtr.Zero)
					{
						return PSError.memFullErr;
					}

					Marshal.WriteInt32(HandleSuite.Instance.LockHandle(data, 0), (int)item.Value);
					HandleSuite.Instance.UnlockHandle(data);
					break;
				case DescriptorTypes.typeFloat:
				case DescriptorTypes.typeUintFloat:
					data = HandleSuite.Instance.NewHandle(sizeof(Double));

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

					Marshal.Copy(new double[] { value }, 0, HandleSuite.Instance.LockHandle(data, 0), 1);
					HandleSuite.Instance.UnlockHandle(data);
					break;

				default:
					break;
			}

			return PSError.noErr;
		}

		private short GetCountProc(IntPtr descriptor, ref uint count)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			if (subClassDict != null)
			{
				count = (uint)subClassDict.Count;
			}
			else
			{
				count = (uint)scriptingData.Count;
			}
			return PSError.noErr;
		}

		private short GetStringProc(IntPtr descriptor, IntPtr data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = scriptingData[getKey];
			}
			int size = item.Size;

			Marshal.WriteByte(data, (byte)size);

			Marshal.Copy((byte[])item.Value, 0, new IntPtr(data.ToInt64() + 1L), size);
			return PSError.noErr;
		}

		private short GetPinnedIntegerProc(IntPtr descriptor, int min, int max, ref int intNumber)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			descErr = PSError.noErr;

			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = scriptingData[getKey];
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
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			descErr = PSError.noErr;
			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = scriptingData[getKey];
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
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			descErr = PSError.noErr;

			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = scriptingData[getKey];
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
		#endregion

		#region  WriteDescriptorProcs
		private IntPtr OpenWriteDescriptorProc()
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif
			IntPtr handle = IntPtr.Zero;

			try
			{
				handle = CreateWriteDescriptor();
			}
			catch (OutOfMemoryException)
			{
				return IntPtr.Zero;
			}

			return handle;
		}

		private short CloseWriteDescriptorProc(IntPtr descriptor, ref IntPtr descriptorHandle)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif
			if (isSubKey)
			{
				isSubKey = false;
			}

			descriptorHandle = HandleSuite.Instance.NewHandle(0);

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
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
			scriptingData.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeInteger, GetAETEParamFlags(key), 0, data));
			return PSError.noErr;
		}

		private short PutFloatProc(IntPtr descriptor, uint key, ref double data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			scriptingData.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeFloat, GetAETEParamFlags(key), 0, data));
			return PSError.noErr;
		}

		private short PutUnitFloatProc(IntPtr descriptor, uint key, uint unit, ref double data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			UnitFloat item = new UnitFloat(unit, data);

			scriptingData.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeUintFloat, GetAETEParamFlags(key), 0, item));
			return PSError.noErr;
		}

		private short PutBooleanProc(IntPtr descriptor, uint key, byte data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			scriptingData.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeBoolean, GetAETEParamFlags(key), 0, data));
			return PSError.noErr;
		}

		private short PutTextProc(IntPtr descriptor, uint key, IntPtr textHandle)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif

			if (textHandle != IntPtr.Zero)
			{
				IntPtr hPtr = HandleSuite.Instance.LockHandle(textHandle, 0);

				try
				{
					int size = HandleSuite.Instance.GetHandleSize(textHandle);
					byte[] data = new byte[size];
					Marshal.Copy(hPtr, data, 0, size);

					scriptingData.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeChar, GetAETEParamFlags(key), size, data));
				}
				finally
				{
					HandleSuite.Instance.UnlockHandle(textHandle);
				}
			}

			return PSError.noErr;
		}

		private short PutAliasProc(IntPtr descriptor, uint key, IntPtr aliasHandle)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			IntPtr hPtr = HandleSuite.Instance.LockHandle(aliasHandle, 0);

			try
			{
				int size = HandleSuite.Instance.GetHandleSize(aliasHandle);
				byte[] data = new byte[size];
				Marshal.Copy(hPtr, data, 0, size);

				scriptingData.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeAlias, GetAETEParamFlags(key), size, data));
			}
			finally
			{
				HandleSuite.Instance.UnlockHandle(aliasHandle);
			}
			return PSError.noErr;
		}

		private short PutEnumeratedProc(IntPtr descriptor, uint key, uint type, uint data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			scriptingData.AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, data));
			return PSError.noErr;
		}

		private short PutClassProc(IntPtr descriptor, uint key, uint data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			scriptingData.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeClass, GetAETEParamFlags(key), 0, data));

			return PSError.noErr;
		}

		private short PutSimpleReferenceProc(IntPtr descriptor, uint key, ref PIDescriptorSimpleReference data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			scriptingData.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeObjectRefrence, GetAETEParamFlags(key), 0, data));
			return PSError.noErr;
		}

		private short PutObjectProc(IntPtr descriptor, uint key, uint type, IntPtr handle)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0}, type: {1}", DebugUtils.PropToString(key), DebugUtils.PropToString(type)));
#endif
			Dictionary<uint, AETEValue> classDict = null;
			// Only the built-in Photoshop classes are supported.
			switch (type)
			{
				case DescriptorTypes.classRGBColor:
					classDict = new Dictionary<uint, AETEValue>(3);
					classDict.Add(DescriptorKeys.keyRed, scriptingData[DescriptorKeys.keyRed]);
					classDict.Add(DescriptorKeys.keyGreen, scriptingData[DescriptorKeys.keyGreen]);
					classDict.Add(DescriptorKeys.keyBlue, scriptingData[DescriptorKeys.keyBlue]);

					scriptingData.Remove(DescriptorKeys.keyRed);// remove the existing keys
					scriptingData.Remove(DescriptorKeys.keyGreen);
					scriptingData.Remove(DescriptorKeys.keyBlue);

					scriptingData.AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, classDict));
					break;
				case DescriptorTypes.classCMYKColor:
					classDict = new Dictionary<uint, AETEValue>(4);
					classDict.Add(DescriptorKeys.keyCyan, scriptingData[DescriptorKeys.keyCyan]);
					classDict.Add(DescriptorKeys.keyMagenta, scriptingData[DescriptorKeys.keyMagenta]);
					classDict.Add(DescriptorKeys.keyYellow, scriptingData[DescriptorKeys.keyYellow]);
					classDict.Add(DescriptorKeys.keyBlack, scriptingData[DescriptorKeys.keyBlack]);

					scriptingData.Remove(DescriptorKeys.keyCyan);
					scriptingData.Remove(DescriptorKeys.keyMagenta);
					scriptingData.Remove(DescriptorKeys.keyYellow);
					scriptingData.Remove(DescriptorKeys.keyBlack);

					scriptingData.AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, classDict));
					break;
				case DescriptorTypes.classGrayscale:
					classDict = new Dictionary<uint, AETEValue>(1);
					classDict.Add(DescriptorKeys.keyGray, scriptingData[DescriptorKeys.keyGray]);

					scriptingData.Remove(DescriptorKeys.keyGray);

					scriptingData.AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, classDict));
					break;
				case DescriptorTypes.classLabColor:
					classDict = new Dictionary<uint, AETEValue>(3);
					classDict.Add(DescriptorKeys.keyLuminance, scriptingData[DescriptorKeys.keyLuminance]);
					classDict.Add(DescriptorKeys.keyA, scriptingData[DescriptorKeys.keyA]);
					classDict.Add(DescriptorKeys.keyB, scriptingData[DescriptorKeys.keyB]);

					scriptingData.Remove(DescriptorKeys.keyLuminance);
					scriptingData.Remove(DescriptorKeys.keyA);
					scriptingData.Remove(DescriptorKeys.keyB);

					scriptingData.AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, classDict));
					break;
				case DescriptorTypes.classHSBColor:
					classDict = new Dictionary<uint, AETEValue>(3);
					classDict.Add(DescriptorKeys.keyHue, scriptingData[DescriptorKeys.keyHue]);
					classDict.Add(DescriptorKeys.keySaturation, scriptingData[DescriptorKeys.keySaturation]);
					classDict.Add(DescriptorKeys.keyBrightness, scriptingData[DescriptorKeys.keyBrightness]);

					scriptingData.Remove(DescriptorKeys.keyHue);
					scriptingData.Remove(DescriptorKeys.keySaturation);
					scriptingData.Remove(DescriptorKeys.keyBrightness);

					scriptingData.AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, classDict));
					break;
				case DescriptorTypes.classPoint:
					classDict = new Dictionary<uint, AETEValue>(2);

					classDict.Add(DescriptorKeys.keyHorizontal, scriptingData[DescriptorKeys.keyHorizontal]);
					classDict.Add(DescriptorKeys.keyVertical, scriptingData[DescriptorKeys.keyVertical]);

					scriptingData.Remove(DescriptorKeys.keyHorizontal);
					scriptingData.Remove(DescriptorKeys.keyVertical);

					scriptingData.AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, classDict));

					break;

				default:
					return PSError.errPlugInHostInsufficient;
			}

			return PSError.noErr;
		}

		private short PutCountProc(IntPtr descriptor, uint key, uint count)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			return PSError.noErr;
		}

		private short PutStringProc(IntPtr descriptor, uint key, IntPtr stringHandle)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
			int size = (int)Marshal.ReadByte(stringHandle);
			byte[] data = new byte[size];
			Marshal.Copy(new IntPtr(stringHandle.ToInt64() + 1L), data, 0, size);

			scriptingData.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeChar, GetAETEParamFlags(key), size, data));

			return PSError.noErr;
		}

		private short PutScopedClassProc(IntPtr descriptor, uint key, uint data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			scriptingData.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeClass, GetAETEParamFlags(key), 0, data));

			return PSError.noErr;
		}

		private short PutScopedObjectProc(IntPtr descriptor, uint key, uint type, IntPtr handle)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif
			IntPtr hPtr = HandleSuite.Instance.LockHandle(handle, 0);

			try
			{
				int size = HandleSuite.Instance.GetHandleSize(handle);
				byte[] data = new byte[size];
				Marshal.Copy(hPtr, data, 0, size);

				scriptingData.AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), size, data));
			}
			finally
			{
				HandleSuite.Instance.UnlockHandle(handle);
			}

			return PSError.noErr;
		}
		#endregion
	}
}
