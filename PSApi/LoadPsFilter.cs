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

/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See License-pdn.txt for full licensing and attribution details.             //
//                                                                             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using PSFilterHostDll;
using PSFilterHostDll.BGRASurface;
using PSFilterHostDll.Properties;

#if !GDIPLUS
using System.Windows.Media.Imaging;
#endif

namespace PSFilterLoad.PSApi
{

	internal sealed class LoadPsFilter : IDisposable
	{
		#region EnumRes
#if DEBUG
		private static DebugFlags dbgFlags;
		private static void Ping(DebugFlags dbg, string message)
		{
			if ((dbgFlags & dbg) != 0)
			{
				System.Diagnostics.StackFrame sf = new System.Diagnostics.StackFrame(1);
				string name = sf.GetMethod().Name;
				System.Diagnostics.Debug.WriteLine(string.Format("Function: {0} {1}\r\n", name, ", " + message));
			}
		}

		private static bool IS_INTRESOURCE(IntPtr value)
		{
			if (((uint)value) > ushort.MaxValue)
			{
				return false;
			}
			return true;
		}
		private static string GET_RESOURCE_NAME(IntPtr value)
		{
			if (IS_INTRESOURCE(value))
				return value.ToString();
			return Marshal.PtrToStringUni(value);
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
		private static readonly Encoding windows1252Encoding = Encoding.GetEncoding(1252);
		private static readonly char[] trimChars = new char[] { ' ', '\0' };
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

			return new string((sbyte*)ptr, 1, length, windows1252Encoding).Trim(trimChars);
		}


		/// <summary>
		/// Reads a Pascal String into a string.
		/// </summary>
		/// <param name="ptr">The pointer to read from.</param>
		/// <param name="length">The length of the resulting Pascal String.</param>
		/// <returns>The resulting string</returns>
		private static unsafe string StringFromPString(byte* ptr, out int length)
		{
			length = (int)ptr[0] + 1; // skip the first byte
			return StringFromPString(ptr);
		}

		private static PluginAETE enumAETE;
		private static unsafe bool EnumAETE(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam)
		{
			if (lpszName == lParam) // is the resource id the one we want
			{
				IntPtr hRes = UnsafeNativeMethods.FindResourceW(hModule, lpszName, lpszType);
				if (hRes == IntPtr.Zero)
				{
#if DEBUG
					System.Diagnostics.Debug.WriteLine(Marshal.GetLastWin32Error().ToString());
#endif
					return true;
				}

				IntPtr loadRes = UnsafeNativeMethods.LoadResource(hModule, hRes);
				if (loadRes == IntPtr.Zero)
				{
					return true;
				}

				IntPtr lockRes = UnsafeNativeMethods.LockResource(loadRes);
				if (lockRes == IntPtr.Zero)
				{
					return true;
				}

				enumAETE = new PluginAETE();

				byte* ptr = (byte*)lockRes.ToPointer() + 2;
				short version = *(short*)ptr;
				ptr += 2;

				enumAETE.major = (short)(version & 0xffff);
				enumAETE.minor = (short)((version >> 16) & 0xffff);

				short lang = *(short*)ptr;
				ptr += 2;
				short script = *(short*)ptr;
				ptr += 2;
				short count = *(short*)ptr;
				ptr += 2;
				byte* propPtr = ptr;

				int stringLength = 0;

				for (int i = 0; i < count; i++)
				{
					string vend = StringFromPString(propPtr, out stringLength);
					propPtr += stringLength;
					string desc = StringFromPString(propPtr, out stringLength);
					propPtr += stringLength;
					uint suiteID = *(uint*)propPtr;
					propPtr += 4;
					enumAETE.suiteLevel = *(short*)propPtr;
					propPtr += 2;
					enumAETE.suiteVersion = *(short*)propPtr;
					propPtr += 2;
					short evntCount = *(short*)propPtr;
					propPtr += 2;
					enumAETE.events = new AETEEvent[evntCount];

					for (int eventc = 0; eventc < evntCount; eventc++)
					{

						string vend2 = StringFromPString(propPtr, out stringLength);
						propPtr += stringLength;
						string desc2 = StringFromPString(propPtr, out stringLength);
						propPtr += stringLength;
						int evntClass = *(int*)propPtr;
						propPtr += 4;
						int evntType = *(int*)propPtr;
						propPtr += 4;



						uint replyType = *(uint*)propPtr;
						propPtr += 7;
						byte[] bytes = new byte[4];

						int idx = 0;
						while (*propPtr != 0)
						{
							if (*propPtr != 0x27) // the ' char
							{
								bytes[idx] = *propPtr;
								idx++;
							}
							propPtr++;
						}
						propPtr++; // skip the second null byte

						uint parmType = BitConverter.ToUInt32(bytes, 0);

						short flags = *(short*)propPtr;
						propPtr += 2;
						short parmCount = *(short*)propPtr;
						propPtr += 2;

						AETEEvent evnt = new AETEEvent()
						{
							vendor = vend2,
							desc = desc2,
							evntClass = evntClass,
							type = evntType,
							replyType = replyType,
							parmType = parmType,
							flags = flags
						};

						AETEParm[] parms = new AETEParm[parmCount];
						for (int p = 0; p < parmCount; p++)
						{
							parms[p].name = StringFromPString(propPtr, out stringLength);
							propPtr += stringLength;

							parms[p].key = *(uint*)propPtr;
							propPtr += 4;

							parms[p].type = *(uint*)propPtr;
							propPtr += 4;

							parms[p].desc = StringFromPString(propPtr, out stringLength);
							propPtr += stringLength;
							parms[p].flags = *(short*)propPtr;
							propPtr += 2;

						}
						evnt.parms = parms;

						short classCount = *(short*)propPtr;
						propPtr += 2;
						if (classCount == 0)
						{
							short compOps = *(short*)propPtr;
							propPtr += 2;
							short enumCount = *(short*)propPtr;
							propPtr += 2;
							if (enumCount > 0)
							{
								AETEEnums[] enums = new AETEEnums[enumCount];
								for (int enc = 0; enc < enumCount; enc++)
								{
									AETEEnums en = new AETEEnums();
									en.type = *(uint*)propPtr;
									propPtr += 4;
									en.count = *(short*)propPtr;
									propPtr += 2;
									en.enums = new AETEEnum[en.count];

									for (int e = 0; e < en.count; e++)
									{
										en.enums[e].name = StringFromPString(propPtr, out stringLength);
										propPtr += stringLength;
										en.enums[e].type = *(uint*)propPtr;
										propPtr += 4;
										en.enums[e].desc = StringFromPString(propPtr, out stringLength);
										propPtr += stringLength;
									}
									enums[enc] = en;

								}
								evnt.enums = enums;
							}
						}
						enumAETE.events[eventc] = evnt;


					}

				}


				return false;
			}
			
				
			return true;
		}

		private static unsafe bool EnumPiPL(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam)
		{
			PluginData enumData = new PluginData(enumFileName);

			IntPtr hRes = UnsafeNativeMethods.FindResourceW(hModule, lpszName, lpszType);
			if (hRes == IntPtr.Zero)
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("FindResource failed for {0} in {1}", GET_RESOURCE_NAME(lpszName), enumFileName));
#endif
				return true;
			}

			IntPtr loadRes = UnsafeNativeMethods.LoadResource(hModule, hRes);
			if (loadRes == IntPtr.Zero)
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("LoadResource failed for {0} in {1}", GET_RESOURCE_NAME(lpszName), enumFileName));
#endif
				return true;
			}

			IntPtr lockRes = UnsafeNativeMethods.LockResource(loadRes);
			if (lockRes == IntPtr.Zero)
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("LockResource failed for {0} in {1}", GET_RESOURCE_NAME(lpszName), enumFileName));
#endif

				return true;
			}

#if DEBUG
			short fb = Marshal.ReadInt16(lockRes); // PiPL Resources always start with 1, this seems to be Photoshop's signature.
#endif
			int version = Marshal.ReadInt32(lockRes, 2);

			if (version != 0)
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("Invalid PiPL version in {0}: {1},  Expected version 0", enumFileName, version));
#endif
				return true;
			}

			int count = Marshal.ReadInt32(lockRes, 6);

			byte* propPtr = (byte*)lockRes.ToPointer() + 10L;

			for (int i = 0; i < count; i++)
			{
				PIProperty* pipp = (PIProperty*)propPtr;
				uint propKey = pipp->propertyKey;
#if DEBUG
				Ping(DebugFlags.PiPL, string.Format("prop {0}: {1}", i, PropToString(propKey)));
#endif
				byte* dataPtr = propPtr + 16;
				if (propKey == PIPropertyID.PIKindProperty)
				{
					if (*((uint*)dataPtr) != PSConstants.filterKind)
					{
#if DEBUG
						System.Diagnostics.Debug.WriteLine(string.Format("{0} is not a valid Photoshop Filter.", enumFileName));
#endif
						return true;
					}
				}
				else if ((IntPtr.Size == 8 && propKey == PIPropertyID.PIWin64X86CodeProperty) || propKey == PIPropertyID.PIWin32X86CodeProperty) // the entrypoint for the current platform, this filters out incompatible processors architectures
				{
					enumData.EntryPoint = Marshal.PtrToStringAnsi((IntPtr)dataPtr, pipp->propertyLength).TrimEnd('\0');
				}
				else if (propKey == PIPropertyID.PIVersionProperty)
				{
					short* fltrVersion = (short*)dataPtr;
					if (fltrVersion[1] > PSConstants.latestFilterVersion ||
						(fltrVersion[1] == PSConstants.latestFilterVersion && fltrVersion[0] > PSConstants.latestFilterSubVersion))
					{
#if DEBUG
						System.Diagnostics.Debug.WriteLine(string.Format("{0} requires newer filter interface version {1}.{2} and only version {3}.{4} is supported", new object[] { enumFileName, fltrVersion[1].ToString(CultureInfo.CurrentCulture), fltrVersion[0].ToString(CultureInfo.CurrentCulture), PSConstants.latestFilterVersion.ToString(CultureInfo.CurrentCulture), PSConstants.latestFilterSubVersion.ToString(CultureInfo.CurrentCulture) }));
#endif
						return true;
					}
				}
				else if (propKey == PIPropertyID.PIImageModesProperty)
				{
#if GDIPLUS
					// All GDI+ images are converted to 8-bit RGB(A) for processing.  
					if ((dataPtr[0] & PSConstants.flagSupportsRGBColor) != PSConstants.flagSupportsRGBColor)
					{
						return true;
					}
#else

					if ((dataPtr[0] & PSConstants.flagSupportsRGBColor) != PSConstants.flagSupportsRGBColor &&
						(dataPtr[0] & PSConstants.flagSupportsGrayScale) != PSConstants.flagSupportsGrayScale)
					{
#if DEBUG
						System.Diagnostics.Debug.WriteLine(string.Format("{0} does not support the plugInModeRGBColor or plugInModeGrayScale image modes.", enumFileName));
#endif
						return true;
					}
					enumData.supportedModes = *(ushort*)dataPtr; 

#endif
				}
				else if (propKey == PIPropertyID.PICategoryProperty)
				{
					enumData.Category = StringFromPString(dataPtr);
				}
				else if (propKey == PIPropertyID.PINameProperty)
				{
					enumData.Title = StringFromPString(dataPtr);
				}
				else if (propKey == PIPropertyID.PIFilterCaseInfoProperty)
				{
					FilterCaseInfo[] filterInfo = new FilterCaseInfo[7];
					for (int j = 0; j < 7; j++)
					{
						filterInfo[j] = *(FilterCaseInfo*)dataPtr;
						dataPtr += 4;
					}
					enumData.FilterInfo = filterInfo;
				}
				else if (propKey == PIPropertyID.PIHasTerminologyProperty)
				{
#if DEBUG
					int vers = *(int*)dataPtr;
					dataPtr += 4;
					int classId = *(int*)dataPtr;
					dataPtr += 4;
					int eventId = *(int*)dataPtr;
					dataPtr += 4;
					short termId = *(short*)dataPtr;
					dataPtr += 2;
#else
					short termId = *(short*)(dataPtr + 12);
#endif

#if DEBUG
					string aeteName = string.Empty;
					StringBuilder sb = new StringBuilder();
					while (*dataPtr != 0)
					{
						sb.Append((char)*dataPtr);
						dataPtr++;
					}
					aeteName = sb.ToString().TrimEnd('\0');
#endif
					enumAETE = null;

					while (UnsafeNativeMethods.EnumResourceNamesW(hModule, "AETE", new UnsafeNativeMethods.EnumResNameDelegate(EnumAETE), (IntPtr)termId))
					{
						// do nothing
					}


					if (enumAETE != null)
					{
						if ((enumAETE.major == 1 && enumAETE.minor == 0) && enumAETE.suiteLevel == 1 && enumAETE.suiteVersion == 1)
						{
							enumData.Aete = enumAETE; // Only use the current version
						}
					}


				}
				else if (propKey == PIPropertyID.EnableInfo)
				{
					enumData.enableInfo = Marshal.PtrToStringAnsi((IntPtr)dataPtr, pipp->propertyLength).TrimEnd('\0');
				}


				int propertyDataPaddedLength = (pipp->propertyLength + 3) & ~3;
				propPtr += (16 + propertyDataPaddedLength);
			}

			if (enumData.IsValid())
			{
				enumResList.Add(enumData); // add each plug-in found in the file to the query list
			} 

			return true;
		}

		/// <summary>
		/// Reads a C string from a pointer.
		/// </summary>
		/// <param name="ptr">The pointer to read from.</param>
		/// <param name="length">The length of the resulting string.</param>
		/// <returns>The resulting string</returns>
		private static string StringFromCString(IntPtr ptr, out int length)
		{
			string data = Marshal.PtrToStringAnsi(ptr);
			length = data.Length + 1; // skip the trailing null

			return data.Trim(trimChars);
		}

		private static unsafe bool EnumPiMI(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam)
		{
			PluginData enumData = new PluginData(enumFileName);

			IntPtr hRes = UnsafeNativeMethods.FindResourceW(hModule, lpszName, lpszType);
			if (hRes == IntPtr.Zero)
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("FindResource failed for {0} in {1}", GET_RESOURCE_NAME(lpszName), enumFileName));
#endif
				return true;
			}

			IntPtr loadRes = UnsafeNativeMethods.LoadResource(hModule, hRes);
			if (loadRes == IntPtr.Zero)
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("LoadResource failed for {0} in {1}", GET_RESOURCE_NAME(lpszName), enumFileName));
#endif
				return true;
			}

			IntPtr lockRes = UnsafeNativeMethods.LockResource(loadRes);
			if (lockRes == IntPtr.Zero)
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("LockResource failed for {0} in {1}", GET_RESOURCE_NAME(lpszName), enumFileName));
#endif
				return true;
			}
			int length = 0;
			byte* ptr = (byte*)lockRes.ToPointer() + 2L;

			enumData.Category = StringFromCString((IntPtr)ptr, out length);

			ptr += length;


			if (string.IsNullOrEmpty(enumData.Category))
			{
				enumData.Category = Resources.PiMIDefaultCategoryName;
			}

			PlugInInfo* info = (PlugInInfo*)ptr;

			if (info->version > PSConstants.latestFilterVersion ||
			   (info->version == PSConstants.latestFilterVersion && info->subVersion > PSConstants.latestFilterSubVersion))
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("{0} requires newer filter interface version {1}.{2} and only version {3}.{4} is supported", new object[] { enumFileName, info->version.ToString(CultureInfo.CurrentCulture), info->subVersion.ToString(CultureInfo.CurrentCulture), PSConstants.latestFilterVersion.ToString(CultureInfo.CurrentCulture), PSConstants.latestFilterSubVersion.ToString(CultureInfo.CurrentCulture) }));
#endif
				return true;
			}

#if GDIPLUS
			// All GDI+ images are converted to 8-bit RGB(A) for processing.  
			if ((info->supportsMode & PSConstants.supportsRGBColor) != PSConstants.supportsRGBColor)
			{
				return true;
			}
#else
			
			if ((info->supportsMode & PSConstants.supportsRGBColor) != PSConstants.supportsRGBColor &&
				(info->supportsMode & PSConstants.supportsGrayScale) != PSConstants.supportsGrayScale)
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("{0} does not support the plugInModeRGBColor or plugInModeGrayScale image modes.", enumFileName));
#endif
				return true;
			}

			// add the supported modes to the plugin data as it can be used later to disable filters that do not support the image type.
			if ((info->supportsMode & PSConstants.supportsGrayScale) == PSConstants.supportsGrayScale)
			{
				enumData.supportedModes |= PSConstants.flagSupportsGrayScale;
			}

			if ((info->supportsMode & PSConstants.supportsRGBColor) == PSConstants.supportsRGBColor)
			{
				enumData.supportedModes |= PSConstants.flagSupportsRGBColor;
			} 

#endif

			IntPtr filterRes = IntPtr.Zero;

			IntPtr type = Marshal.StringToHGlobalUni("_8BFM");
			try
			{
				filterRes = UnsafeNativeMethods.FindResourceW(hModule, lpszName, type); // load the _8BFM resource to get the filter title
			}
			finally
			{
				Marshal.FreeHGlobal(type);
			}

			if (filterRes == IntPtr.Zero)
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("FindResource failed for {0} in {1}", "_8BFM", enumData.FileName));
#endif
				return true;
			}

			IntPtr filterLoad = UnsafeNativeMethods.LoadResource(hModule, filterRes);

			if (filterLoad == IntPtr.Zero)
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("LoadResource failed for {0} in {1}", "_8BFM", enumData.FileName));
#endif
				return true;
			}

			IntPtr filterLock = UnsafeNativeMethods.LockResource(filterLoad);

			if (filterLock == IntPtr.Zero)
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("LockResource failed for {0} in {1}", "_8BFM", enumData.FileName));
#endif
				return true;
			}

			IntPtr resPtr = new IntPtr(filterLock.ToInt64() + 2L);

			enumData.Title = StringFromCString(resPtr, out length);

			// the entry point number is the same as the resource number
			enumData.EntryPoint = "ENTRYPOINT" + lpszName.ToInt32().ToString(CultureInfo.InvariantCulture);
			enumData.FilterInfo = null;

			if (enumData.IsValid())
			{
				enumResList.Add(enumData); // add each plug-in found in the file to the query list 
			}

			return true;
		}

		#endregion
		private static string enumFileName;
		private static List<PluginData> enumResList;
		

		/// <summary>
		/// Queries an 8bf plug-in
		/// </summary>
		/// <param name="fileName">The fileName to query.</param>
		/// <param name="pluginData">The list filters within the plug-in.</param>
		/// <returns>
		/// True if successful; otherwise false.
		/// </returns>
		internal static bool QueryPlugin(string fileName, out List<PluginData> pluginData)
		{
			if (string.IsNullOrEmpty(fileName))
				throw new ArgumentException("fileName is null or empty.", "fileName");

			pluginData = new List<PluginData>();
#if DEBUG
			dbgFlags |= DebugFlags.PiPL;
#endif
			SafeLibraryHandle dll = UnsafeNativeMethods.LoadLibraryW(fileName);
			try
			{
				if (!dll.IsInvalid)
				{
					enumResList = new List<PluginData>();
					enumFileName = fileName;
					bool needsRelease = false;
					System.Runtime.CompilerServices.RuntimeHelpers.PrepareConstrainedRegions();
					try
					{

						dll.DangerousAddRef(ref needsRelease);
						if (UnsafeNativeMethods.EnumResourceNamesW(dll.DangerousGetHandle(), "PiPl", new UnsafeNativeMethods.EnumResNameDelegate(EnumPiPL), IntPtr.Zero))
						{
							pluginData.AddRange(enumResList);

						}// if there are no PiPL resources scan for Photoshop 2.5's PiMI resources. 
						else if (UnsafeNativeMethods.EnumResourceNamesW(dll.DangerousGetHandle(), "PiMI", new UnsafeNativeMethods.EnumResNameDelegate(EnumPiMI), IntPtr.Zero))
						{
							pluginData.AddRange(enumResList);
						}
#if DEBUG
						else
						{
							Ping(DebugFlags.Error, string.Format("EnumResourceNames(PiPL, PiMI) failed for {0}", fileName));
						}
#endif

					}
					finally
					{
						if (needsRelease)
						{
							dll.DangerousRelease();
						}
					}

				}
#if DEBUG
				else
				{
					System.Diagnostics.Debug.WriteLine(string.Format("LoadLibrary() returned 0x{0:X8}", Marshal.GetLastWin32Error()));
				}
#endif                    

			}
			finally
			{
				if (!dll.IsInvalid && !dll.IsClosed)
				{
					dll.Dispose();
					dll = null;
				}
			}
			

			return (pluginData.Count > 0);
		}



		static bool RectNonEmpty(Rect16 rect)
		{
			return (rect.left < rect.right && rect.top < rect.bottom);
		}


		struct PSHandle
		{
			public IntPtr pointer;
			public int size;
		}

		#region CallbackDelegates

		// AdvanceState
		static AdvanceStateProc advanceProc;
		// BufferProcs
		static AllocateBufferProc allocProc;
		static FreeBufferProc freeProc;
		static LockBufferProc lockProc;
		static UnlockBufferProc unlockProc;
		static BufferSpaceProc spaceProc;
		// MiscCallbacks
		static ColorServicesProc colorProc;
		static DisplayPixelsProc displayPixelsProc;
		static HostProcs hostProc;
		static ProcessEventProc processEventProc;
		static ProgressProc progressProc;
		static TestAbortProc abortProc;
		// HandleProcs 
		static NewPIHandleProc handleNewProc;
		static DisposePIHandleProc handleDisposeProc;
		static GetPIHandleSizeProc handleGetSizeProc;
		static SetPIHandleSizeProc handleSetSizeProc;
		static LockPIHandleProc handleLockProc;
		static UnlockPIHandleProc handleUnlockProc;
		static RecoverSpaceProc handleRecoverSpaceProc;
		static DisposeRegularPIHandleProc handleDisposeRegularProc;
		// ImageServicesProc
#if USEIMAGESERVICES
		static PIResampleProc resample1DProc;
		static PIResampleProc resample2DProc;
#endif
		// ChannelPorts
		static ReadPixelsProc readPixelsProc;
		static WriteBasePixelsProc writeBasePixelsProc;
		static ReadPortForWritePortProc readPortForWritePortProc;
		// PropertyProcs
		static GetPropertyProc getPropertyProc;
		static SetPropertyProc setPropertyProc;
		// ResourceProcs
		static CountPIResourcesProc countResourceProc;
		static GetPIResourceProc getResourceProc;
		static DeletePIResourceProc deleteResourceProc;
		static AddPIResourceProc addResourceProc;

		// ReadDescriptorProcs
		static OpenReadDescriptorProc openReadDescriptorProc;
		static CloseReadDescriptorProc closeReadDescriptorProc;
		static GetKeyProc getKeyProc;
		static GetIntegerProc getIntegerProc;
		static GetFloatProc getFloatProc;
		static GetUnitFloatProc getUnitFloatProc;
		static GetBooleanProc getBooleanProc;
		static GetTextProc getTextProc;
		static GetAliasProc getAliasProc;
		static GetEnumeratedProc getEnumeratedProc;
		static GetClassProc getClassProc;
		static GetSimpleReferenceProc getSimpleReferenceProc;
		static GetObjectProc getObjectProc;
		static GetCountProc getCountProc;
		static GetStringProc getStringProc;
		static GetPinnedIntegerProc getPinnedIntegerProc;
		static GetPinnedFloatProc getPinnedFloatProc;
		static GetPinnedUnitFloatProc getPinnedUnitFloatProc;
		// WriteDescriptorProcs
		static OpenWriteDescriptorProc openWriteDescriptorProc;
		static CloseWriteDescriptorProc closeWriteDescriptorProc;
		static PutIntegerProc putIntegerProc;
		static PutFloatProc putFloatProc;
		static PutUnitFloatProc putUnitFloatProc;
		static PutBooleanProc putBooleanProc;
		static PutTextProc putTextProc;
		static PutAliasProc putAliasProc;
		static PutEnumeratedProc putEnumeratedProc;
		static PutClassProc putClassProc;
		static PutSimpleReferenceProc putSimpleReferenceProc;
		static PutObjectProc putObjectProc;
		static PutCountProc putCountProc;
		static PutStringProc putStringProc;
		static PutScopedClassProc putScopedClassProc;
		static PutScopedObjectProc putScopedObjectProc;

#if PICASUITES
		static SPBasicSuite_AcquireSuite spAcquireSuite;
		static SPBasicSuite_AllocateBlock spAllocateBlock;
		static SPBasicSuite_FreeBlock spFreeBlock;
		static SPBasicSuite_IsEqual spIsEqual;
		static SPBasicSuite_ReallocateBlock spReallocateBlock;
		static SPBasicSuite_ReleaseSuite spReleaseSuite;
		static SPBasicSuite_Undefined spUndefined;
#endif
		#endregion

		private Dictionary<IntPtr, PSHandle> handles;

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

#if PICASUITES
		private IntPtr basicSuitePtr;
#endif

		private PluginAETE aete;
		private Dictionary<uint, AETEValue> aeteDict;


		public SurfaceBase Dest
		{
			get
			{
				return dest;
			}
		}

		/// <summary>
		/// The filter progress callback.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
		internal void SetProgressFunc(ProgressProc value)
		{
			if (value == null)
				throw new ArgumentNullException("value", "value is null.");

			progressFunc = value;
		}

		/// <summary>
		/// The filter abort callback.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
		internal void SetAbortFunc(AbortFunc value)
		{
			if (value == null)
				throw new ArgumentNullException("value", "value is null.");

			abortFunc = value;
		}

		static AbortFunc abortFunc;
		static ProgressProc progressFunc;


		private SurfaceBase source = null;
		private SurfaceBase dest = null;
		private PluginPhase phase;

		private IntPtr dataPtr;
		private short result;

		private string errorMessage = string.Empty;

		public string ErrorMessage
		{
			get
			{
				return errorMessage;
			}
		}

		private GlobalParameters globalParms;
		private bool isRepeatEffect;

		internal ParameterData ParmData
		{
			get
			{
				return new ParameterData(globalParms, aeteDict);
			}
			set
			{
				globalParms = value.GlobalParameters;
				aeteDict = value.ScriptingData.ToDictionary();
			}
		}
		/// <summary>
		/// Is the filter a repeat Effect.
		/// </summary>
		internal bool IsRepeatEffect
		{
			set
			{
				isRepeatEffect = value;
			}
		}

		private List<PSResource> pseudoResources;

		internal List<PSResource> PseudoResources
		{
			get
			{
				return pseudoResources;
			}
			set
			{
				pseudoResources = value;
			}
		}

		private short filterCase;

		private double dpiX;
		private double dpiY;

		private Region selectedRegion;

#if GDIPLUS
		private Bitmap exifBitmap;
#else
		private System.Windows.Media.Imaging.BitmapSource exifBitmap;
#endif		
		private ImageModes imageMode;

		

		/// <summary>
		/// Loads Adobe® Photoshop® filters to show the about dialog.
		/// </summary>
		/// <param name="owner">The handle of the parent window</param>
#if GDIPLUS
		internal LoadPsFilter(IntPtr owner)
			: this(null, Color.Black, Color.White, null, owner)
		{
		} 
#else
		internal LoadPsFilter(IntPtr owner) : this(null, System.Windows.Media.Colors.Black, System.Windows.Media.Colors.White, null, owner)
		{
		} 
#endif

		/// <summary>
		/// Loads and runs Adobe® Photoshop® filters
		/// </summary>
		/// <param name="sourceImage">The file name of the source image.</param>
		/// <param name="primary">The selected primary color.</param>
		/// <param name="secondary">The selected secondary color.</param>
		/// <param name="selection">The <see cref="System.Drawing.Region"/> that gives the shape of the selection.</param>
		/// <param name="owner">The handle of the parent window</param>
		/// <exception cref="System.ArgumentException">The sourceImage is null.</exception>
		/// <exception cref="ImageSizeTooLargeException">The sourceImage is greater that 32000 pixels in width or height.</exception>
#if GDIPLUS
		internal LoadPsFilter(Bitmap sourceImage, Color primary, Color secondary, Region selection, IntPtr owner)
#else
		internal LoadPsFilter(BitmapSource sourceImage, System.Windows.Media.Color primary, System.Windows.Media.Color secondary, Region selection, IntPtr owner)
#endif
		{
			this.dataPtr = IntPtr.Zero;
			this.phase = PluginPhase.None;
			this.disposed = false;
			this.copyToDest = true;
			this.sizesSetup = false;
			this.frValuesSetup = false;
			this.isRepeatEffect = false;
			this.globalParms = new GlobalParameters();

			this.outputHandling = FilterDataHandling.filterDataHandlingNone;

			abortFunc = null;
			progressFunc = null;
			this.pseudoResources = new List<PSResource>();
			this.handles = new Dictionary<IntPtr, PSHandle>();
			this.bufferIDs = new List<IntPtr>();

			this.keys = null;
			this.aete = null;
			this.aeteDict = new Dictionary<uint, AETEValue>();
			this.aeteKeyIndex = new List<uint>();
			this.getKey = 0;
			this.getKeyIndex = 0;
			this.subKeys = null;
			this.subKeyIndex = 0;
			this.isSubKey = false;

			this.channelReadDescPtrs = new List<ChannelDescPtrs>();

			outRect.left = outRect.top = outRect.right = outRect.bottom = 0;
			inRect.left = inRect.top = inRect.right = inRect.bottom = 0;
			maskRect.left = maskRect.right = maskRect.bottom = maskRect.top = 0;

			if (sourceImage != null)
			{
				if (sourceImage.Width > 32000 || sourceImage.Height > 32000)
				{
					string message = string.Empty;
					if (sourceImage.Width > 32000 && sourceImage.Height > 32000)
					{
						message = Resources.ImageSizeTooLarge;
					}
					else
					{
						if (sourceImage.Width > 32000)
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
				this.source = SurfaceFactory.CreateFromGdipBitmap(sourceImage, out this.imageMode);
#else      
				this.source = SurfaceFactory.CreateFromBitmapSource(sourceImage, out this.imageMode);
#endif
				this.dest = SurfaceFactory.CreateFromImageMode(source.Width, source.Height, this.imageMode);

#if GDIPLUS
				dpiX = sourceImage.HorizontalResolution;
				dpiY = sourceImage.VerticalResolution;
			   
				this.exifBitmap = (Bitmap)sourceImage.Clone();
#else
				this.dpiX = sourceImage.DpiX;
				this.dpiY = sourceImage.DpiY;

				this.exifBitmap = sourceImage.Clone(); 
#endif

				this.selectedRegion = null;
				this.filterCase = FilterCase.filterCaseEditableTransparencyNoSelection;

				if (selection != null)
				{
					selection.Intersect(source.Bounds);
					Rectangle selectionBounds = selection.GetBoundsInt();

					if (!selectionBounds.IsEmpty && selectionBounds != source.Bounds)
					{
						this.selectedRegion = selection.Clone();
						this.filterCase = FilterCase.filterCaseEditableTransparencyWithSelection;
					}
				}

				if (imageMode == ImageModes.plugInModeGrayScale || imageMode == ImageModes.plugInModeGray16)
				{
					switch (filterCase)
					{
						case FilterCase.filterCaseEditableTransparencyNoSelection:
							filterCase = FilterCase.filterCaseFlatImageNoSelection;
							break;
						case FilterCase.filterCaseEditableTransparencyWithSelection:
							filterCase = FilterCase.filterCaseFlatImageWithSelection;
							break;
					}
				}
			}

			unsafe
			{
				this.platFormDataPtr = Memory.Allocate(Marshal.SizeOf(typeof(PlatformData)), true);
				((PlatformData*)platFormDataPtr.ToPointer())->hwnd = owner;
			}

			this.outRowBytes = 0;
			this.outHiPlane = 0;
			this.outLoPlane = 0;

			this.primaryColor = new byte[4] { primary.R, primary.G, primary.B, 0 };
			this.secondaryColor = new byte[4] { secondary.R, secondary.G, secondary.B, 0 };

#if DEBUG
			dbgFlags = DebugFlags.AdvanceState;
			dbgFlags |= DebugFlags.Call;
			dbgFlags |= DebugFlags.ColorServices;
			dbgFlags |= DebugFlags.DisplayPixels;
			dbgFlags |= DebugFlags.Error;
			dbgFlags |= DebugFlags.HandleSuite;
			dbgFlags |= DebugFlags.ImageServices;
			dbgFlags |= DebugFlags.MiscCallbacks;
#if PICASUITES
			dbgFlags |= DebugFlags.SPBasicSuite;
#endif
#endif
		}


		/// <summary>
		/// The Secondary (background) color in the host
		/// </summary>
		private byte[] secondaryColor;
		/// <summary>
		/// The Primary (foreground) color in the host
		/// </summary>
		private byte[] primaryColor;

		private bool ignoreAlpha;

		private FilterDataHandling outputHandling;

		private bool IgnoreAlphaChannel(PluginData data)
		{
			if (filterCase < FilterCase.filterCaseEditableTransparencyNoSelection)
			{
				return true; // Return true for the FlatImage cases as we do not have any transparency.
			}

			// some filters do not handle the alpha channel correctly despite what their FilterInfo says.
			if ((data.FilterInfo == null) || data.Category == "L'amico Perry" || data.Category == "Imagenomic" ||
				data.Category.Contains("Vizros") && data.Title.Contains("Lake") || data.Category == "PictureCode" ||
				data.Category == "PictoColor" || data.Category == "Axion")
			{
				switch (filterCase)
				{
					case FilterCase.filterCaseEditableTransparencyNoSelection:
						filterCase = FilterCase.filterCaseFlatImageNoSelection;
						break;
					case FilterCase.filterCaseEditableTransparencyWithSelection:
						filterCase = FilterCase.filterCaseFlatImageWithSelection;
						break;
				}

				if (data.FilterInfo != null)				{					outputHandling = data.FilterInfo[(filterCase - 1)].outputHandling;				}  				return true;
			}

			int filterCaseIndex = filterCase - 1;

			outputHandling = data.FilterInfo[filterCaseIndex].outputHandling;
			// if the EditableTransparency cases are not supported use the other modes.
			if (data.FilterInfo[filterCaseIndex].inputHandling == FilterDataHandling.filterDataHandlingCantFilter)
			{
				/* use the FlatImage modes if the filter doesn't support the ProtectedTransparency cases 				* or image does not have any transparency */

				if (data.FilterInfo[filterCaseIndex + 2].inputHandling == FilterDataHandling.filterDataHandlingCantFilter || !source.HasTransparency())
				{
					switch (filterCase)
					{
						case FilterCase.filterCaseEditableTransparencyNoSelection:
							filterCase = FilterCase.filterCaseFlatImageNoSelection;
							break;
						case FilterCase.filterCaseEditableTransparencyWithSelection:
							filterCase = FilterCase.filterCaseFlatImageWithSelection;
							break;
					}
					return true;
				}
				else
				{
					switch (filterCase)
					{
						case FilterCase.filterCaseEditableTransparencyNoSelection:
							filterCase = FilterCase.filterCaseProtectedTransparencyNoSelection;
							break;
						case FilterCase.filterCaseEditableTransparencyWithSelection:
							filterCase = FilterCase.filterCaseProtectedTransparencyWithSelection;
							break;
					}

				}

			}

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

			if (SafeNativeMethods.VirtualQuery(ptr, ref mbi, new IntPtr(mbiSize)) == IntPtr.Zero)
				return true;

			result = ((mbi.Protect & NativeConstants.PAGE_READONLY) != 0 || (mbi.Protect & NativeConstants.PAGE_READWRITE) != 0 ||
			(mbi.Protect & NativeConstants.PAGE_WRITECOPY) != 0 || (mbi.Protect & NativeConstants.PAGE_EXECUTE_READ) != 0 || (mbi.Protect & NativeConstants.PAGE_EXECUTE_READWRITE) != 0 ||
			(mbi.Protect & NativeConstants.PAGE_EXECUTE_WRITECOPY) != 0);

			if ((mbi.Protect & NativeConstants.PAGE_GUARD) != 0 || (mbi.Protect & NativeConstants.PAGE_NOACCESS) != 0)
				result = false;

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

			if (SafeNativeMethods.VirtualQuery(ptr, ref mbi, new IntPtr(mbiSize)) == IntPtr.Zero)
				return true;

			result = ((mbi.Protect & NativeConstants.PAGE_READWRITE) != 0 || (mbi.Protect & NativeConstants.PAGE_WRITECOPY) != 0 ||
				(mbi.Protect & NativeConstants.PAGE_EXECUTE_READWRITE) != 0 || (mbi.Protect & NativeConstants.PAGE_EXECUTE_WRITECOPY) != 0);

			if ((mbi.Protect & NativeConstants.PAGE_GUARD) != 0 || (mbi.Protect & NativeConstants.PAGE_NOACCESS) != 0)
				result = false;

			return !result;
		}

		/// <summary>
		/// Loads a filter from the PluginData.
		/// </summary>
		/// <param name="pdata">The PluginData of the filter to load.</param>
		/// <returns>True if successful, otherwise false.</returns>
		/// <exception cref="System.IO.FileNotFoundException">The file in the PluginData.FileName cannot be found.</exception>
		private static bool LoadFilter(ref PluginData pdata)
		{
			bool loaded = false;

			if (!string.IsNullOrEmpty(pdata.EntryPoint)) 
			{
				pdata.module.dll = UnsafeNativeMethods.LoadLibraryW(pdata.FileName);

				if (!pdata.module.dll.IsInvalid)
				{
					IntPtr entryPoint = UnsafeNativeMethods.GetProcAddress(pdata.module.dll, pdata.EntryPoint);

					if (entryPoint != IntPtr.Zero)
					{
						pdata.module.entryPoint = (pluginEntryPoint)Marshal.GetDelegateForFunctionPointer(entryPoint, typeof(pluginEntryPoint));
						loaded = true;
					}
				}
				else
				{
					int hr = Marshal.GetHRForLastWin32Error();
					Marshal.ThrowExceptionForHR(hr);
				}
			}

			return loaded;
		}

		/// <summary>
		/// Free the loaded PluginData.
		/// </summary>
		/// <param name="pdata">The PluginData to  free/</param>
		private static void FreeLibrary(ref PluginData pdata)
		{
			if (pdata.module.dll != null)
			{
				pdata.module.dll.Dispose();
				pdata.module.dll = null;
				pdata.module.entryPoint = null;
			}
		}

		/// <summary>
		/// Save the filter parameters for repeat runs.
		/// </summary>
		private unsafe void SaveParameters()
		{
			int handleSize = IntPtr.Size + 4;

			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			if (filterRecord->parameters != IntPtr.Zero)
			{
				long size = 0;
				globalParms.ParameterDataIsPSHandle = false;
				if (handle_valid(filterRecord->parameters))
				{
					globalParms.ParameterDataSize = HandleGetSizeProc(filterRecord->parameters);


					IntPtr ptr = HandleLockProc(filterRecord->parameters, 0);

					byte[] buf = new byte[globalParms.ParameterDataSize];
					Marshal.Copy(ptr, buf, 0, buf.Length);
					globalParms.SetParameterDataBytes(buf);
					globalParms.ParameterDataIsPSHandle = true;

					HandleUnlockProc(filterRecord->parameters);


					globalParms.StoreMethod = 0;
				}
				else if ((size = SafeNativeMethods.GlobalSize(filterRecord->parameters).ToInt64()) > 0L)
				{
					IntPtr ptr = SafeNativeMethods.GlobalLock(filterRecord->parameters);

					try
					{
						IntPtr hPtr = Marshal.ReadIntPtr(filterRecord->parameters);
						
						if (size == handleSize && Marshal.ReadInt32(filterRecord->parameters, IntPtr.Size) == 0x464f544f)
						{
							long ps = 0;
							if ((ps = SafeNativeMethods.GlobalSize(hPtr).ToInt64()) > 0L)
							{
								byte[] buf = new byte[ps];
								Marshal.Copy(hPtr, buf, 0, (int)ps);
								globalParms.SetParameterDataBytes(buf);
								globalParms.ParameterDataIsPSHandle = true;
							}

						}
						else
						{
							if (!IsBadReadPtr(hPtr))
							{
								int ps = SafeNativeMethods.GlobalSize(hPtr).ToInt32();
								if (ps == 0)
								{
									ps = ((int)size - IntPtr.Size); // some plug-ins do not use the pointer to a pointer trick.
								}

								byte[] buf = new byte[ps];

								Marshal.Copy(hPtr, buf, 0, ps);
								globalParms.SetParameterDataBytes(buf);
								globalParms.ParameterDataIsPSHandle = true;
							}
							else
							{
								byte[] buf = new byte[(int)size];

								Marshal.Copy(filterRecord->parameters, buf, 0, (int)size);
								globalParms.SetParameterDataBytes(buf);
							}

						}
					}
					finally
					{
						SafeNativeMethods.GlobalUnlock(filterRecord->parameters);
					}

					globalParms.ParameterDataSize = size;
					globalParms.StoreMethod = 1;
				}

			}

			if (dataPtr != IntPtr.Zero)
			{    				
				globalParms.PluginDataIsPSHandle = false;
			
				long pluginDataSize = 0L;

				if (!handle_valid(dataPtr))
				{
					pluginDataSize = SafeNativeMethods.GlobalSize(dataPtr).ToInt64();
				}
				
				IntPtr pluginData = SafeNativeMethods.GlobalLock(dataPtr);

				try
				{
					if (pluginDataSize == handleSize && Marshal.ReadInt32(pluginData, IntPtr.Size) == 0x464f544f) 
					{
						IntPtr hPtr = Marshal.ReadIntPtr(pluginData);
						long ps = 0;
						if (!IsBadReadPtr(hPtr) && (ps = SafeNativeMethods.GlobalSize(hPtr).ToInt64()) > 0L)
						{
							Byte[] dataBuf = new byte[ps];
							Marshal.Copy(hPtr, dataBuf, 0, (int)ps);
							globalParms.SetPluginDataBytes(dataBuf);
							globalParms.PluginDataIsPSHandle = true;
						}
						globalParms.PluginDataSize = pluginDataSize;

					}
					else if (handle_valid(pluginData))
					{
						int ps = HandleGetSizeProc(pluginData);
						byte[] dataBuf = new byte[ps];

						IntPtr hPtr = HandleLockProc(pluginData, 0);
						Marshal.Copy(hPtr, dataBuf, 0, ps);
						HandleUnlockProc(pluginData);
						globalParms.SetPluginDataBytes(dataBuf);
						globalParms.PluginDataSize = ps;
						globalParms.PluginDataIsPSHandle = true;
					}
					else if (pluginDataSize > 0)
					{
						byte[] dataBuf = new byte[pluginDataSize];
						Marshal.Copy(pluginData, dataBuf, 0, (int)pluginDataSize);
						globalParms.SetPluginDataBytes(dataBuf);
						globalParms.PluginDataSize = pluginDataSize;
					}
					
				}
				finally
				{
					SafeNativeMethods.GlobalUnlock(pluginData);
				}

			}
		}
		private IntPtr parmDataHandle;
		private IntPtr filterParametersHandle;
		/// <summary>
		/// Restore the filter parameters for repeat runs.
		/// </summary>
		private unsafe void RestoreParameters()
		{
			if (phase == PluginPhase.Parameters)
				return;

			byte[] sig = new byte[4] { 0x4f, 0x54, 0x4f, 0x46 }; // OTOF
			int handleSize = IntPtr.Size + 4;

			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			byte[] parameterDataBytes = globalParms.GetParameterDataBytes();
			if (parameterDataBytes != null)
			{

				switch (globalParms.StoreMethod)
				{
					case 0:

						filterRecord->parameters = HandleNewProc((int)globalParms.ParameterDataSize);
						IntPtr hPtr = HandleLockProc(filterRecord->parameters, 0);

						Marshal.Copy(parameterDataBytes, 0, hPtr, parameterDataBytes.Length);

						HandleUnlockProc(filterRecord->parameters);
						break;
					case 1:

						if (globalParms.ParameterDataSize == handleSize && globalParms.ParameterDataIsPSHandle)
						{
							filterRecord->parameters = SafeNativeMethods.GlobalAlloc(NativeConstants.GPTR, new UIntPtr((uint)globalParms.ParameterDataSize));

							int parameterDataLength = parameterDataBytes.Length;

							filterParametersHandle = SafeNativeMethods.GlobalAlloc(NativeConstants.GPTR, new UIntPtr((uint)parameterDataLength));

							Marshal.Copy(parameterDataBytes, 0, filterParametersHandle, parameterDataBytes.Length);


							Marshal.WriteIntPtr(filterRecord->parameters, filterParametersHandle);
							Marshal.Copy(sig, 0, new IntPtr(filterRecord->parameters.ToInt64() + (long)IntPtr.Size), 4);

						}
						else
						{

							if (globalParms.ParameterDataIsPSHandle)
							{
#if DEBUG
								System.Diagnostics.Debug.Assert((globalParms.ParameterDataSize == (parameterDataBytes.Length + IntPtr.Size)));
#endif
								filterRecord->parameters = SafeNativeMethods.GlobalAlloc(NativeConstants.GPTR, new UIntPtr((uint)globalParms.ParameterDataSize));

								IntPtr ptr = new IntPtr(filterRecord->parameters.ToInt64() + (long)IntPtr.Size);

								Marshal.Copy(parameterDataBytes, 0, ptr, parameterDataBytes.Length);

								Marshal.WriteIntPtr(filterRecord->parameters, ptr);
							}
							else
							{
								filterRecord->parameters = SafeNativeMethods.GlobalAlloc(NativeConstants.GPTR, new UIntPtr((ulong)parameterDataBytes.Length));
								Marshal.Copy(parameterDataBytes, 0, filterRecord->parameters, parameterDataBytes.Length);
							}

						}


						break;
					default:
						filterRecord->parameters = IntPtr.Zero;
						break;
				}
			}

			byte[] pluginDataBytes = globalParms.GetPluginDataBytes();

			if (pluginDataBytes != null)
			{
				if (globalParms.PluginDataSize == handleSize && globalParms.PluginDataIsPSHandle)
				{
					dataPtr = SafeNativeMethods.GlobalAlloc(NativeConstants.GPTR, new UIntPtr((uint)globalParms.PluginDataSize));
					parmDataHandle = SafeNativeMethods.GlobalAlloc(NativeConstants.GPTR, new UIntPtr((uint)pluginDataBytes.Length));


					Marshal.Copy(pluginDataBytes, 0, parmDataHandle, pluginDataBytes.Length);

					Marshal.WriteIntPtr(dataPtr, parmDataHandle);
					Marshal.Copy(sig, 0, new IntPtr(dataPtr.ToInt64() + IntPtr.Size), 4);

				}
				else
				{
					if (globalParms.PluginDataIsPSHandle)
					{
						dataPtr = HandleNewProc(pluginDataBytes.Length);

						IntPtr ptr = HandleLockProc(dataPtr, 0);

						Marshal.Copy(pluginDataBytes, 0, ptr, pluginDataBytes.Length);
						HandleUnlockProc(dataPtr);
					}
					else
					{
						dataPtr = SafeNativeMethods.GlobalAlloc(NativeConstants.GPTR, new UIntPtr((uint)pluginDataBytes.Length));
						Marshal.Copy(pluginDataBytes, 0, dataPtr, pluginDataBytes.Length);
					}

				}
			}

		}

		private bool PluginAbout(PluginData pdata)
		{
			AboutRecord about = new AboutRecord()
			{
				platformData = platFormDataPtr,
				sSPBasic = IntPtr.Zero,
				plugInRef = IntPtr.Zero
			};

			result = PSError.noErr;

			GCHandle gch = GCHandle.Alloc(about, GCHandleType.Pinned);

			try
			{
				// If the filter only has one entry point call about on it.
				if (pdata.moduleEntryPoints == null) 
				{
					pdata.module.entryPoint(FilterSelector.filterSelectorAbout, gch.AddrOfPinnedObject(), ref dataPtr, ref result);
				}
				else
				{ 
					// otherwise call about on all the entry points in the module, per the SDK Docs only one of the entry points will display the about box.
					foreach (var entryPoint in pdata.moduleEntryPoints)
					{
						IntPtr ptr = UnsafeNativeMethods.GetProcAddress(pdata.module.dll, entryPoint);

						pluginEntryPoint ep = (pluginEntryPoint)Marshal.GetDelegateForFunctionPointer(ptr, typeof(pluginEntryPoint));
						
						ep(FilterSelector.filterSelectorAbout, gch.AddrOfPinnedObject(), ref dataPtr, ref result);

						GC.KeepAlive(ep);
					}

				}
			}
			finally
			{
				gch.Free();
			}


			if (result != PSError.noErr)
			{
				FreeLibrary(ref pdata);

				errorMessage = GetErrorMessage(result);
#if DEBUG
				Ping(DebugFlags.Error, string.Format("filterSelectorAbout returned result code {0}({1})", errorMessage, result));
#endif
				return false;
			}

			return true;
		}

		private unsafe bool PluginApply(PluginData pdata)
		{
#if DEBUG
			System.Diagnostics.Debug.Assert(phase == PluginPhase.Prepare);
#endif
			result = PSError.noErr;

#if DEBUG
			Ping(DebugFlags.Call, "Before FilterSelectorStart");
#endif

			pdata.module.entryPoint(FilterSelector.filterSelectorStart, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
			Ping(DebugFlags.Call, "After FilterSelectorStart");
#endif

			if (result != PSError.noErr)
			{
				FreeLibrary(ref pdata);
				errorMessage = GetErrorMessage(result);

#if DEBUG
				string message = string.IsNullOrEmpty(errorMessage) ? "User Canceled" : errorMessage;
				Ping(DebugFlags.Error, string.Format("filterSelectorStart returned result code: {0}({1})", message, result));
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

				pdata.module.entryPoint(FilterSelector.filterSelectorContinue, filterRecordPtr, ref dataPtr, ref result);

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

					pdata.module.entryPoint(FilterSelector.filterSelectorFinish, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
					Ping(DebugFlags.Call, "After FilterSelectorFinish");
#endif


					FreeLibrary(ref pdata);

					errorMessage = GetErrorMessage(savedResult);

#if DEBUG
					string message = string.IsNullOrEmpty(errorMessage) ? "User Canceled" : errorMessage;
					Ping(DebugFlags.Error, string.Format("filterSelectorContinue returned result code: {0}({1})", message, savedResult));
#endif

					return false;
				}
			}
			AdvanceStateProc();


			result = PSError.noErr;

#if DEBUG
			Ping(DebugFlags.Call, "Before FilterSelectorFinish");
#endif

			pdata.module.entryPoint(FilterSelector.filterSelectorFinish, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
			Ping(DebugFlags.Call, "After FilterSelectorFinish");
#endif
			if (!isRepeatEffect && result == PSError.noErr)
			{
				SaveParameters(); 
			}

			filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			if (filterRecord->autoMask == 1) 
			{
				ClipToSelection(); // Clip the rendered image to the selection if the filter does not do it for us.
			}

			return true;
		}

		private bool PluginParameters(PluginData pdata)
		{
			result = PSError.noErr;

			// Photoshop sets the size info before the filterSelectorParameters call even though the documentation says it does not.
			SetupSizes();
			SetFilterRecordValues();
#if DEBUG
			Ping(DebugFlags.Call, "Before filterSelectorParameters");
#endif

			pdata.module.entryPoint(FilterSelector.filterSelectorParameters, filterRecordPtr, ref dataPtr, ref result);
#if DEBUG
			unsafe
			{
				FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

				Ping(DebugFlags.Call, string.Format("data = {0:X},  parameters = {1:X}", dataPtr, filterRecord->parameters));
			}

			Ping(DebugFlags.Call, "After filterSelectorParameters");
#endif

			if (result != PSError.noErr)
			{
				FreeLibrary(ref pdata);
				errorMessage = GetErrorMessage(result);
#if DEBUG
				string message = string.IsNullOrEmpty(errorMessage) ? "User Canceled" : errorMessage;
				Ping(DebugFlags.Error, string.Format("filterSelectorParameters failed result code: {0}({1})", message, result));
#endif
				return false;
			}

			phase = PluginPhase.Parameters;

			return true;
		}

		private bool frValuesSetup;
		private unsafe void SetFilterRecordValues()
		{
			if (frValuesSetup)
				return;

			frValuesSetup = true;

			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

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
			// maskRect
			filterRecord->maskData = IntPtr.Zero;
			filterRecord->maskRowBytes = 0;

			filterRecord->imageMode = imageMode;

			if (imageMode == ImageModes.plugInModeGrayScale || imageMode == ImageModes.plugInModeGray16)
			{
				filterRecord->inLayerPlanes = 0;
				filterRecord->inTransparencyMask = 0; 	
				filterRecord->inNonLayerPlanes = 1;

				filterRecord->inColumnBytes = imageMode == ImageModes.plugInModeGray16 ? 2 : 1;

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

				if (imageMode == ImageModes.plugInModeRGB48)
				{
					filterRecord->inColumnBytes = ignoreAlpha ? 6 : 8;
				}
				else
				{
					filterRecord->inColumnBytes = ignoreAlpha ? 3 : 4;
				}

				if (filterCase == FilterCase.filterCaseProtectedTransparencyNoSelection ||
					filterCase == FilterCase.filterCaseProtectedTransparencyWithSelection)
				{
					filterRecord->planes = 3;
					filterRecord->outLayerPlanes = 0;
					filterRecord->outTransparencyMask = 0;
					filterRecord->outNonLayerPlanes = 3;
					filterRecord->outColumnBytes = imageMode == ImageModes.plugInModeRGB48 ? 6 : 3;

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

			if (ignoreAlpha && (imageMode == ImageModes.plugInModeRGBColor || imageMode == ImageModes.plugInModeRGB48))
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

			if (imageMode == ImageModes.plugInModeRGB48 || imageMode == ImageModes.plugInModeGray16)
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

		private bool PluginPrepare(PluginData pdata)
		{
			SetupSizes();
			RestoreParameters();
			SetFilterRecordValues();


			result = PSError.noErr;


#if DEBUG
			Ping(DebugFlags.Call, "Before filterSelectorPrepare");
#endif
			pdata.module.entryPoint(FilterSelector.filterSelectorPrepare, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
			Ping(DebugFlags.Call, "After filterSelectorPrepare");
#endif

			if (result != PSError.noErr)
			{
				FreeLibrary(ref pdata);
				errorMessage = GetErrorMessage(result);
#if DEBUG
				string message = string.IsNullOrEmpty(errorMessage) ? "User Canceled" : errorMessage;
				Ping(DebugFlags.Error, string.Format("filterSelectorParameters failed result code: {0}({1})", message, result));
#endif
				return false;
			}

#if DEBUG
			phase = PluginPhase.Prepare;
#endif

			return true;
		}

		/// <summary>
		/// True if the source image is copied to the dest image, otherwise false.
		/// </summary>
		private bool copyToDest;
		/// <summary>
		/// Sets the dest alpha to the source alpha for the protected transparency cases.
		/// </summary>
		private unsafe void CopySourceAlpha()
		{
			if (!copyToDest)
			{
				int width = dest.Width;
				int height = dest.Height;

				if (imageMode == ImageModes.plugInModeRGB48)
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

		private bool writesOutsideSelection;
		/// <summary>
		/// Clips the output image to the selection.
		/// </summary>
		private unsafe void ClipToSelection()
		{
			if ((selectedRegion != null) && !writesOutsideSelection)
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
		/// Runs a filter from the specified PluginData
		/// </summary>
		/// <param name="pdata">The PluginData to run</param>
		/// <returns>True if successful otherwise false</returns>
		internal bool RunPlugin(PluginData pdata)
		{
			if (!LoadFilter(ref pdata))
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine("LoadFilter failed");
#endif
				return false;
			}

			useChannelPorts = pdata.Category == "Amico Perry"; // enable the channel ports suite for Luce 2

			ignoreAlpha = IgnoreAlphaChannel(pdata);

			if (pdata.FilterInfo != null)
			{
				int index = filterCase - 1;
				copyToDest = ((pdata.FilterInfo[index].flags1 & FilterCaseInfoFlags.PIFilterDontCopyToDestinationBit) == 0);
				writesOutsideSelection = ((pdata.FilterInfo[index].flags1 & FilterCaseInfoFlags.PIFilterWritesOutsideSelectionBit) != 0);
			}

			if (copyToDest)
			{
				dest.CopySurface(source); // copy the source image to the dest image if the filter does not write to all the pixels.
			}

			if (ignoreAlpha)
			{
				dest.SetAlphaToOpaque();
			}
			else
			{
				DrawCheckerBoardBitmap();
			}

			if ((pdata.Aete != null) && pdata.Aete.events != null)
			{
				foreach (var evnt in pdata.Aete.events)
				{
					if (evnt.parms != null)
					{
						foreach (var item in evnt.parms)
						{
							aeteKeyIndex.Add(item.key);
						} 
					}
				}

				if (aeteKeyIndex.Count > 0)
				{
					aete = pdata.Aete;
				}
				
			}

			SetupDelegates();
			SetupSuites();
			SetupFilterRecord();


			if (!isRepeatEffect)
			{
				if (!PluginParameters(pdata))
				{
#if DEBUG
					Ping(DebugFlags.Error, "PluginParameters failed");
#endif
					return false;
				}
			}


			if (!PluginPrepare(pdata))
			{
#if DEBUG
				Ping(DebugFlags.Error, "PluginPrepare failed");
#endif
				return false;
			}

			if (!PluginApply(pdata))
			{
#if DEBUG
				Ping(DebugFlags.Error, "PluginApply failed");
#endif
				return false;
			}

			try
			{
				FreeLibrary(ref pdata);
			}
			catch (Exception)
			{
			}

			return true;
		}

		internal bool ShowAboutDialog(PluginData pdata)
		{
			if (!LoadFilter(ref pdata))
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine("LoadFilter failed");
#endif
				return false;
			}

			bool retVal = PluginAbout(pdata);

			
			FreeLibrary(ref pdata);
			

			return retVal;
		}

		private string GetErrorMessage(short err)
		{
			string error = string.Empty;

			// Any positive integer is a plug-in handled error message.
			if (err == PSError.userCanceledErr || err >= 1)
			{
				return string.Empty;
			}
			else if (err == PSError.errReportString)
			{
				error = StringFromPString(errorStringPtr);
			}
			else
			{
				switch (err)
				{
					case PSError.readErr:
						error = Resources.FileReadError;
						break;
					case PSError.writErr:
						error = Resources.FileWriteError;
						break;
					case PSError.openErr:
						error = Resources.FileOpenError;
						break;
					case PSError.dskFulErr:
						error = Resources.DiskFullError;
						break;
					case PSError.ioErr:
						error = Resources.FileIOError;
						break;
					case PSError.memFullErr:
						error = Resources.OutOfMemoryError;
						break;
					case PSError.nilHandleErr:
						error = Resources.NullHandleError;
						break;
					case PSError.filterBadParameters:
						error = Resources.BadParameters;
						break;
					case PSError.filterBadMode:
						error = Resources.UnsupportedImageMode;
						break;
					case PSError.errPlugInHostInsufficient:
						error = Resources.errPlugInHostInsufficient;
						break;
					case PSError.errPlugInPropertyUndefined:
						error = Resources.errPlugInPropertyUndefined;
						break;
					case PSError.errHostDoesNotSupportColStep:
						error = Resources.errHostDoesNotSupportColStep;
						break;
					case PSError.errInvalidSamplePoint:
						error = Resources.InvalidSamplePoint;
						break;
					case PSError.errUnknownPort:
						error = Resources.UnknownChannelPort;
						break;
					case PSError.errUnsupportedBitOffset:
						error = Resources.UnsupportedChannelBitOffset;
						break;
					case PSError.errUnsupportedColBits:
						error = Resources.UnsupportedChannelColumnBits;
						break;
					case PSError.errUnsupportedDepth:
						error = Resources.UnsupportedChannelDepth;
						break;
					case PSError.errUnsupportedDepthConversion:
						error = Resources.UnsupportedChannelDepthConversion;
						break;
					case PSError.errUnsupportedRowBits:
						error = Resources.UnsupportedChannelRowBits;
						break;                      

					default:
						error = string.Format(CultureInfo.CurrentCulture, Resources.UnknownErrorCodeFormat, err);
						break;
				}
			}
			return error;
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

		private Rect16 outRect;
		private int outRowBytes;
		private int outLoPlane;
		private int outHiPlane;
		private Rect16 inRect;
		private Rect16 maskRect;
		private IntPtr maskDataPtr;
		private IntPtr inDataPtr;
		private IntPtr outDataPtr;
		private int lastInLoPlane;
		private int lastOutLoPlane;

		/// <summary>
		/// Determines whether the filter uses planar order processing.
		/// </summary>
		/// <param name="fr">The FilterRecord to check.</param>
		/// <param name="outData">if set to <c>true</c> check the output data.</param>
		/// <returns>
		///   <c>true</c> if a single plane of data is requested; otherwise, <c>false</c>.
		/// </returns>
		private static unsafe bool IsSinglePlane(FilterRecord* fr, bool outData)
		{
			if (outData)
			{
				return (((fr->outHiPlane - fr->outLoPlane) + 1) == 1);
			}

			return (((fr->inHiPlane - fr->inLoPlane) + 1) == 1);
		}

		/// <summary>
		/// Determines whether the data buffer needs to be resized.
		/// </summary>
		/// <param name="inData">The buffer to check.</param>
		/// <param name="inRect">The new source rectangle.</param>
		/// <param name="loplane">The loplane.</param>
		/// <param name="hiplane">The hiplane.</param>
		/// <returns> <c>true</c> if a the buffer needs to be resized; otherwise, <c>false</c></returns>
		private static bool ResizeBuffer(IntPtr inData, Rect16 inRect, int loplane, int hiplane)
		{
			long size = Memory.Size(inData);

			int width = inRect.right - inRect.left;
			int height = inRect.bottom - inRect.top;
			int nplanes = hiplane - loplane + 1;

			long bufferSize = ((width * nplanes) * height);

			return (bufferSize != size);
		}

		private unsafe short AdvanceStateProc()
		{
			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			if (outDataPtr != IntPtr.Zero && RectNonEmpty(outRect))
			{
				StoreOutputBuffer(outDataPtr, outRowBytes, outRect, outLoPlane, outHiPlane);
			}

#if DEBUG
			Ping(DebugFlags.AdvanceState, string.Format("Inrect = {0}, Outrect = {1}, maskRect = {2}", filterRecord->inRect.ToString(), filterRecord->outRect.ToString(), filterRecord->maskRect.ToString()));
#endif
			short error;

			if (filterRecord->haveMask == 1 && RectNonEmpty(filterRecord->maskRect))
			{
				if (!maskRect.Equals(filterRecord->maskRect))
				{
					if (maskDataPtr != IntPtr.Zero && ResizeBuffer(maskDataPtr, filterRecord->maskRect, 0, 0))
					{
						Memory.Free(maskDataPtr);
						maskDataPtr = IntPtr.Zero;
						filterRecord->maskData = IntPtr.Zero;
					}

					error = FillMaskBuffer(ref filterRecord->maskData, ref filterRecord->maskRowBytes, filterRecord->maskRect, filterRecord->maskRate, filterRecord->maskPadding);

					if (error != PSError.noErr)
					{
						return error;
					}

					maskRect = filterRecord->maskRect;
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
				maskRect.left = maskRect.right = maskRect.bottom = maskRect.top = 0;
			}


			if (RectNonEmpty(filterRecord->inRect))
			{
				if (!inRect.Equals(filterRecord->inRect) || (IsSinglePlane(filterRecord, false) && lastInLoPlane != filterRecord->inLoPlane))
				{
					if (inDataPtr != IntPtr.Zero &&	ResizeBuffer(inDataPtr, filterRecord->inRect, filterRecord->inLoPlane, filterRecord->inHiPlane))
					{
						try
						{
							Memory.Free(inDataPtr);
						}
						catch (Exception)
						{
						}
						finally
						{
							inDataPtr = IntPtr.Zero;
							filterRecord->inData = IntPtr.Zero;
						}
					}

					error = FillInputBuffer(ref filterRecord->inData, ref filterRecord->inRowBytes, filterRecord->inRect, filterRecord->inLoPlane, filterRecord->inHiPlane, filterRecord->inputRate, filterRecord->inputPadding);
					if (error != PSError.noErr)
					{
						return error;
					}

					inRect = filterRecord->inRect;
					lastInLoPlane = filterRecord->inLoPlane;
					filterRecord->inColumnBytes = (filterRecord->inHiPlane - filterRecord->inLoPlane) + 1;

					if (imageMode == ImageModes.plugInModeRGB48 || imageMode == ImageModes.plugInModeGray16)
					{
						filterRecord->inColumnBytes *= 2; // 2 bytes per plane
					}
				}
			}
			else
			{
				if (filterRecord->inData != IntPtr.Zero)
				{
					try
					{
						Memory.Free(inDataPtr);
					}
					catch (Exception)
					{
					}
					finally
					{
						inDataPtr = IntPtr.Zero;
						filterRecord->inData = IntPtr.Zero;
					}
				}
				filterRecord->inRowBytes = 0;
				inRect.left = inRect.top = inRect.right = inRect.bottom = 0;
			}

			if (RectNonEmpty(filterRecord->outRect))
			{
				if (!outRect.Equals(filterRecord->outRect) || (IsSinglePlane(filterRecord, true) && lastOutLoPlane != filterRecord->outLoPlane))
				{
					if (outDataPtr != IntPtr.Zero && ResizeBuffer(outDataPtr, filterRecord->outRect, filterRecord->outLoPlane, filterRecord->outHiPlane))
					{
						try
						{
							Memory.Free(outDataPtr);
						}
						catch (Exception)
						{
						}
						finally
						{
							outDataPtr = IntPtr.Zero;
							filterRecord->outData = IntPtr.Zero;
						}
					}

					error = FillOutputBuffer(ref filterRecord->outData, ref filterRecord->outRowBytes, filterRecord->outRect, filterRecord->outLoPlane, filterRecord->outHiPlane, filterRecord->outputPadding);
					if (error != PSError.noErr)
					{
						return error;
					}

					filterRecord->outColumnBytes = (filterRecord->outHiPlane - filterRecord->outLoPlane) + 1;
					lastOutLoPlane = filterRecord->outLoPlane;
					if (imageMode == ImageModes.plugInModeRGB48 || imageMode == ImageModes.plugInModeGray16)
					{
						filterRecord->outColumnBytes *= 2;
					}
				}
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("outRowBytes = {0}", filterRecord->outRowBytes));
#endif
				// store previous values
				outRowBytes = filterRecord->outRowBytes;
				outRect = filterRecord->outRect;
				outLoPlane = filterRecord->outLoPlane;
				outHiPlane = filterRecord->outHiPlane;
			}
			else
			{
				if (filterRecord->outData != IntPtr.Zero)
				{
					try
					{
						Memory.Free(outDataPtr);
					}
					catch (Exception)
					{
					}
					finally
					{
						outDataPtr = IntPtr.Zero;
						filterRecord->outData = IntPtr.Zero;
					}
				}
				filterRecord->outRowBytes = 0;
				outRowBytes = 0;
				outRect.left = outRect.top = outRect.right = outRect.bottom = 0;
				outLoPlane = 0;
				outHiPlane = 0;

			}

			return PSError.noErr;
		}

		private SurfaceBase tempSurface;
		/// <summary>
		/// Scales the temp surface.
		/// </summary>
		/// <param name="inputRate">The FilterRecord.inputRate to use to scale the image.</param>
		/// <param name="lockRect">The rectangle to clamp the size to.</param>
		private unsafe void ScaleTempSurface(int inputRate, Rectangle lockRect)
		{
			int scaleFactor = fixed2int(inputRate);
			if (scaleFactor == 0)
			{
				scaleFactor = 1;
			}

			int scalew = source.Width / scaleFactor;
			int scaleh = source.Height / scaleFactor;

			if (lockRect.Width > scalew)
			{
				scalew = lockRect.Width;
			}

			if (lockRect.Height > scaleh)
			{
				scaleh = lockRect.Height;
			}

			if ((tempSurface == null) || scalew != tempSurface.Width && scaleh != tempSurface.Height)
			{
				if (tempSurface != null)
				{
					tempSurface.Dispose();
					tempSurface = null;
				}

				if (scaleFactor > 1) // Filter preview
				{
					tempSurface = SurfaceFactory.CreateFromImageMode(scalew, scaleh, imageMode);
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
		/// <param name="inData">The input buffer to fill.</param>
		/// <param name="inRowBytes">The stride of the input buffer.</param>
		/// <param name="rect">The rectangle of interest within the image.</param>
		/// <param name="loplane">The input loPlane.</param>
		/// <param name="hiplane">The input hiPlane.</param>
		/// <param name="inputRate">The FilterRecord.inputRate to use to scale the image.</param>
		/// <param name="inputPadding">The mode to use if the image needs padding.</param>
		private unsafe short FillInputBuffer(ref IntPtr inData, ref int inRowBytes, Rect16 rect, short loplane, short hiplane, int inputRate, short inputPadding)
		{
#if DEBUG
			Ping(DebugFlags.AdvanceState, string.Format("inRowBytes = {0}, Rect = {1}, loplane = {2}, hiplane = {3}", new object[] { inRowBytes.ToString(), rect.ToString(), loplane.ToString(), hiplane.ToString() }));
			Ping(DebugFlags.AdvanceState, string.Format("inputRate = {0}", fixed2int(inputRate)));
#endif

			int nplanes = hiplane - loplane + 1;
			int width = (rect.right - rect.left);
			int height = (rect.bottom - rect.top);

		   
			Rectangle lockRect = Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);


			int stride = (width * nplanes);
			int len = stride * height;

			if (imageMode == ImageModes.plugInModeRGB48 || imageMode == ImageModes.plugInModeGray16)
			{
				len *= 2; // 2 bytes per plane
			}

			if (inDataPtr == IntPtr.Zero)
			{
				try
				{
					inDataPtr = Memory.Allocate(len, false);
				}
				catch (OutOfMemoryException)
				{
					return PSError.memFullErr;
				}
			}
			inData = inDataPtr;
			inRowBytes = stride;

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
				else
				{
					lockRect.Y = 0;
					lockRect.Height -= -rect.top;
				}
			}

			bool validImageBounds = (rect.left < source.Width && rect.top < source.Height);

			if (validImageBounds)
			{
				ScaleTempSurface(inputRate, lockRect); 
			}


			short ofs = loplane;
			if (imageMode == ImageModes.plugInModeRGB48 || imageMode == ImageModes.plugInModeRGBColor)
			{
				switch (loplane) // Photoshop uses RGBA pixel order so map the Red and Blue channels to BGRA order
				{
					case 0:
						ofs = 2;
						break;
					case 2:
						ofs = 0;
						break;
				}
			}

			short padErr = SetFilterPadding(inData, inRowBytes, rect, nplanes, ofs, inputPadding, lockRect, tempSurface);
			if (padErr != PSError.noErr || !validImageBounds)
			{
				return padErr;
			}
			void* ptr = inData.ToPointer();
			int top = lockRect.Top;
			int left = lockRect.Left;
			int bottom = Math.Min(lockRect.Bottom, tempSurface.Height);
			int right = Math.Min(lockRect.Right, tempSurface.Width);


			switch (imageMode)
			{
				case ImageModes.plugInModeGrayScale:

					for (int y = top; y < bottom; y++)
					{
						byte* p = tempSurface.GetPointAddressUnchecked(left, y);
						byte* q = (byte*)ptr + ((y - top) * stride);

						for (int x = left; x < right; x++)
						{
							*q = *p;

							p++;
							q++;
						}
					}

					break;
				case ImageModes.plugInModeGray16:

					for (int y = top; y < bottom; y++)
					{
						ushort* p = (ushort*)tempSurface.GetPointAddressUnchecked(left, y);
						ushort* q = (ushort*)ptr + ((y - top) * stride);

						for (int x = left; x < right; x++)
						{
							*q = *p;

							p++;
							q++;
						}
					}

					inRowBytes *= 2;

					break;
				case ImageModes.plugInModeRGBColor:

					for (int y = top; y < bottom; y++)
					{
						byte* p = tempSurface.GetPointAddressUnchecked(left, y);
						byte* q = (byte*)ptr + ((y - top) * stride);

						for (int x = left; x < right; x++)
						{
							switch (nplanes)
							{
								case 1:
									*q = p[ofs];
									break;
								case 2:
									q[0] = p[ofs];
									q[1] = p[ofs + 1];
									break;
								case 3:
									q[0] = p[2];
									q[1] = p[1];
									q[2] = p[0];
									break;
								case 4:
									q[0] = p[2];
									q[1] = p[1];
									q[2] = p[0];
									q[3] = p[3];
									break;

							}

							p += 4;
							q += nplanes;
						}
					}

					break;
				case ImageModes.plugInModeRGB48:

					for (int y = top; y < bottom; y++)
					{
						ushort* p = (ushort*)tempSurface.GetPointAddressUnchecked(left, y);
						ushort* q = (ushort*)ptr + ((y - top) * stride);

						for (int x = left; x < right; x++)
						{
							switch (nplanes)
							{
								case 1:
									*q = p[ofs];
									break;
								case 2:
									q[0] = p[ofs];
									q[1] = p[ofs + 1];
									break;
								case 3:
									q[0] = p[2];
									q[1] = p[1];
									q[2] = p[0];
									break;
								case 4:
									q[0] = p[2];
									q[1] = p[1];
									q[2] = p[0];
									q[3] = p[3];
									break;

							}

							p += 4;
							q += nplanes;
						}
					}

					inRowBytes *= 2;

					break;
			}

			return PSError.noErr;
		}

		private unsafe short FillOutputBuffer(ref IntPtr outData, ref int outRowBytes, Rect16 rect, short loplane, short hiplane, short outputPadding)
		{

#if DEBUG
			Ping(DebugFlags.AdvanceState, string.Format("outRowBytes = {0}, Rect = {1}, loplane = {2}, hiplane = {3}", new object[] { outRowBytes.ToString(), rect.ToString(), loplane.ToString(), hiplane.ToString() }));
#endif


#if DEBUG
			using (Bitmap dst = dest.CreateAliasedBitmap())
			{

			}
#endif

			int nplanes = hiplane - loplane + 1;
			int width = (rect.right - rect.left);
			int height = (rect.bottom - rect.top);

			Rectangle lockRect = Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);


			int stride = width * nplanes;
			int len = stride * height;

			if (imageMode == ImageModes.plugInModeRGB48 || imageMode == ImageModes.plugInModeGray16)
			{
				len *= 2;
			}

			if (outDataPtr == IntPtr.Zero)
			{
				try
				{
					outDataPtr = Memory.Allocate(len, false);
				}
				catch (OutOfMemoryException)
				{
					return PSError.memFullErr;
				}
			}
			outData = outDataPtr;
			outRowBytes = stride;

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
				else
				{
					lockRect.Y = 0;
					lockRect.Height -= -rect.top;
				}
			}

			short ofs = loplane;
			if (imageMode == ImageModes.plugInModeRGB48 || imageMode == ImageModes.plugInModeRGBColor)
			{
				switch (loplane) // Photoshop uses RGBA pixel order so map the Red and Blue channels to BGRA order
				{
					case 0:
						ofs = 2;
						break;
					case 2:
						ofs = 0;
						break;
				}
			}

			short padErr = SetFilterPadding(outData, outRowBytes, rect, nplanes, ofs, outputPadding, lockRect, dest);
			if (padErr != PSError.noErr || (rect.left >= dest.Width || rect.top >= dest.Height))
			{
				return padErr;
			}
			void* ptr = outData.ToPointer();
			int top = lockRect.Top;
			int left = lockRect.Left;
			int bottom = Math.Min(lockRect.Bottom, dest.Height);
			int right = Math.Min(lockRect.Right, dest.Width);


			switch (imageMode)
			{
				case ImageModes.plugInModeGrayScale:

					for (int y = top; y < bottom; y++)
					{
						byte* p = dest.GetPointAddressUnchecked(left, y);
						byte* q = (byte*)ptr + ((y - top) * stride);

						for (int x = left; x < right; x++)
						{
							*q = *p;

							p++;
							q++;
						}
					}

					break;
				case ImageModes.plugInModeGray16:

					for (int y = top; y < bottom; y++)
					{
						ushort* p = (ushort*)dest.GetPointAddressUnchecked(left, y);
						ushort* q = (ushort*)ptr + ((y - top) * stride);

						for (int x = left; x < right; x++)
						{
							*q = *p;

							p++;
							q++;
						}
					}

					outRowBytes *= 2;

					break;
				case ImageModes.plugInModeRGBColor:

					for (int y = top; y < bottom; y++)
					{
						byte* p = dest.GetPointAddressUnchecked(left, y);
						byte* q = (byte*)ptr + ((y - top) * stride);

						for (int x = left; x < right; x++)
						{
							switch (nplanes)
							{
								case 1:
									*q = p[ofs];
									break;
								case 2:
									q[0] = p[ofs];
									q[1] = p[ofs + 1];
									break;
								case 3:
									q[0] = p[2];
									q[1] = p[1];
									q[2] = p[0];
									break;
								case 4:
									q[0] = p[2];
									q[1] = p[1];
									q[2] = p[0];
									q[3] = p[3];
									break;

							}

							p += 4;
							q += nplanes;
						}
					}

					break;
				case ImageModes.plugInModeRGB48:

					for (int y = top; y < bottom; y++)
					{
						ushort* p = (ushort*)dest.GetPointAddressUnchecked(left, y);
						ushort* q = (ushort*)ptr + ((y - top) * stride);

						for (int x = left; x < right; x++)
						{
							switch (nplanes)
							{
								case 1:
									*q = p[ofs];
									break;
								case 2:
									q[0] = p[ofs];
									q[1] = p[ofs + 1];
									break;
								case 3:
									q[0] = p[2];
									q[1] = p[1];
									q[2] = p[0];
									break;
								case 4:
									q[0] = p[2];
									q[1] = p[1];
									q[2] = p[0];
									q[3] = p[3];
									break;

							}

							p += 4;
							q += nplanes;
						}
					}

					outRowBytes *= 2;

					break;
			}

			return PSError.noErr;
		}

		private Surface8 tempMask;

		private unsafe void ScaleTempMask(int maskRate, Rectangle lockRect)
		{
			int scaleFactor = fixed2int(maskRate);

			if (scaleFactor == 0)
			{
				scaleFactor = 1;
			}
			int scalew = source.Width / scaleFactor;
			int scaleh = source.Height / scaleFactor;

			if (lockRect.Width > scalew)
			{
				scalew = lockRect.Width;
			}

			if (lockRect.Height > scaleh)
			{
				scaleh = lockRect.Height;
			}
			if ((tempMask == null) || scalew != tempMask.Width && scaleh != tempMask.Height)
			{
				if (tempMask != null)
				{
					tempMask.Dispose();
					tempMask = null;
				}

				if (scaleFactor > 1)
				{
					tempMask = new Surface8(scalew, scaleh);
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
		/// <param name="maskData">The input buffer to fill.</param>
		/// <param name="maskRowBytes">The stride of the input buffer.</param>
		/// <param name="rect">The rectangle of interest within the image.</param>
		/// <param name="maskRate">The FilterRecord.maskRate to use to scale the image.</param>
		/// <param name="maskPadding">The mode to use if the image needs padding.</param>
		private unsafe short FillMaskBuffer(ref IntPtr maskData, ref int maskRowBytes, Rect16 rect, int maskRate, short maskPadding)
		{
#if DEBUG
			Ping(DebugFlags.AdvanceState, string.Format("maskRowBytes = {0}, Rect = {1}", new object[] { maskRowBytes.ToString(), rect.ToString() }));
			Ping(DebugFlags.AdvanceState, string.Format("maskRate = {0}", fixed2int(maskRate)));
#endif
			int width = (rect.right - rect.left);
			int height = (rect.bottom - rect.top);

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
				else
				{
					lockRect.Y = 0;
					lockRect.Height -= -rect.top;
				}
			}


			bool validImageBounds = (rect.left < source.Width && rect.top < source.Height);
			if (validImageBounds)
			{
				ScaleTempMask(maskRate, lockRect);
			}
			

			int len = width * height;

			if (maskDataPtr == IntPtr.Zero)
			{
				try
				{
					maskDataPtr = Memory.Allocate(len, false);
				}
				catch (OutOfMemoryException)
				{
					return PSError.memFullErr;
				}
			}
			maskData = maskDataPtr;
			maskRowBytes = width;

			short err = SetFilterPadding(maskDataPtr, width, rect, 1, 0, maskPadding, lockRect, mask);
			if (err != PSError.noErr || !validImageBounds)
			{
				return err;
			}
			
			byte* ptr = (byte*)maskData.ToPointer();

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

			int nplanes = hiplane - loplane + 1;

			if (RectNonEmpty(rect))
			{
				if (rect.left < source.Width && rect.top < source.Height)
				{
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
					
					int uStride = outRowBytes / 2;

					switch (imageMode)
					{
						case ImageModes.plugInModeGrayScale:

							for (int y = top; y < bottom; y++)
							{
								byte* p = (byte*)ptr + ((y - top) * outRowBytes);
								byte* q = dest.GetPointAddressUnchecked(left, y);

								for (int x = left; x < right; x++)
								{
									*q = *p;

									p++;
									q++;
								}
							}

							break;
						case ImageModes.plugInModeGray16:

							for (int y = top; y < bottom; y++)
							{
								ushort* p = (ushort*)ptr + ((y - top) * uStride);
								ushort* q = (ushort*)dest.GetPointAddressUnchecked(left, y);

								for (int x = left; x < right; x++)
								{
									*q = *p;

									p++;
									q++;
								}
							}

							break;    
						case ImageModes.plugInModeRGBColor:

							for (int y = top; y < bottom; y++)
							{
								byte* p = (byte*)ptr + ((y - top) * outRowBytes);
								byte* q = dest.GetPointAddressUnchecked(left, y);

								for (int x = left; x < right; x++)
								{
									switch (nplanes)
									{
										case 1:
											q[ofs] = *p;
											break;
										case 2:
											q[ofs] = p[0];
											q[ofs + 1] = p[1];
											break;
										case 3:
											q[0] = p[2];
											q[1] = p[1];
											q[2] = p[0];
											break;
										case 4:
											q[0] = p[2];
											q[1] = p[1];
											q[2] = p[0];
											q[3] = p[3];
											break;

									}

									p += nplanes;
									q += 4;
								}
							}

							break;
						case ImageModes.plugInModeRGB48:

							for (int y = top; y < bottom; y++)
							{
								ushort* p = (ushort*)ptr + ((y - top) * uStride);
								ushort* q = (ushort*)dest.GetPointAddressUnchecked(left, y);

								for (int x = left; x < right; x++)
								{
									switch (nplanes)
									{
										case 1:
											q[ofs] = p[0];
											break;
										case 2:
											q[ofs] = p[0];
											q[ofs + 1] = p[1];
											break;
										case 3:
											q[0] = p[2];
											q[1] = p[1];
											q[2] = p[0];
											break;
										case 4:
											q[0] = p[2];
											q[1] = p[1];
											q[2] = p[0];
											q[3] = p[3];
											break;
									}

									p += nplanes;
									q += 4;
								}
							}

							break;
					}

					// set the alpha channel to opaque in the area affected by the filter if it needs it
					if ((filterCase == FilterCase.filterCaseEditableTransparencyNoSelection || filterCase == FilterCase.filterCaseEditableTransparencyWithSelection) &&
						outputHandling == FilterDataHandling.filterDataHandlingFillMask)
					{
						if (imageMode == ImageModes.plugInModeRGB48)
						{
							for (int y = top; y < bottom; y++)
							{
								ushort* p = (ushort*)dest.GetPointAddressUnchecked(left, y);

								for (int x = left; x < right; x++)
								{
									p[3] = 32768;
									p += 4;
								}
							}
						}
						else
						{
							for (int y = top; y < bottom; y++)
							{
								byte* p = dest.GetPointAddressUnchecked(left, y);

								for (int x = left; x < right; x++)
								{
									p[3] = 255;
									p += 4;
								}
							}
						}
					}

#if DEBUG
					using (Bitmap bmp = dest.CreateAliasedBitmap())
					{

					}
#endif

				}
			}
		}

		private List<IntPtr> bufferIDs;

		private short AllocateBufferProc(int size, ref IntPtr bufferID)
		{
#if DEBUG
			Ping(DebugFlags.BufferSuite, string.Format("Size = {0}", size));
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
			long size = Memory.Size(bufferID);
			Ping(DebugFlags.BufferSuite, string.Format("Buffer address = {0:X8}, Size = {1}", bufferID.ToInt64(), size));
#endif
			Memory.Free(bufferID);

			this.bufferIDs.Remove(bufferID);

		}
		private IntPtr BufferLockProc(IntPtr bufferID, byte moveHigh)
		{
#if DEBUG
			Ping(DebugFlags.BufferSuite, string.Format("Buffer address = {0:X8}", bufferID.ToInt64()));
#endif

			return bufferID;
		}
		private void BufferUnlockProc(IntPtr bufferID)
		{
#if DEBUG
			Ping(DebugFlags.BufferSuite, string.Format("Buffer address = {0:X8}", bufferID.ToInt64()));
#endif
		}
		private int BufferSpaceProc()
		{
			return 1000000000;
		}

		private unsafe short ColorServicesProc(ref ColorServicesInfo info)
		{
#if DEBUG
			Ping(DebugFlags.ColorServices, string.Format("selector = {0}", info.selector));
#endif
			short err = PSError.noErr;
			switch (info.selector)
			{
				case ColorServicesSelector.plugIncolorServicesChooseColor:

					string title = StringFromPString(info.selectorParameter.pickerPrompt);

					using (ColorPicker picker = new ColorPicker(title))
					{
						picker.Color = Color.FromArgb(info.colorComponents[0], info.colorComponents[1], info.colorComponents[2]);

						if (picker.ShowDialog() == DialogResult.OK)
						{
							info.colorComponents[0] = picker.Color.R;
							info.colorComponents[1] = picker.Color.G;
							info.colorComponents[2] = picker.Color.B;					
							
							err = ColorServicesConvert.Convert(info.sourceSpace, info.resultSpace, ref info.colorComponents);
						}
						else
						{
							err = PSError.userCanceledErr;
						}
					}


					break;
				case ColorServicesSelector.plugIncolorServicesConvertColor:

					err = ColorServicesConvert.Convert(info.sourceSpace, info.resultSpace, ref info.colorComponents);

					break;
				case ColorServicesSelector.plugIncolorServicesGetSpecialColor:

					FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();
					switch (info.selectorParameter.specialColorID)
					{
						case ColorServicesConstants.plugIncolorServicesBackgroundColor:


							for (int i = 0; i < 4; i++)
							{
								info.colorComponents[i] = (short)filterRecord->backColor[i];
							}


							break;
						case ColorServicesConstants.plugIncolorServicesForegroundColor:


							for (int i = 0; i < 4; i++)
							{
								info.colorComponents[i] = (short)filterRecord->foreColor[i];
							}

							break;
						default:
							err = PSError.paramErr;
							break;
					}
					
					break;
				case ColorServicesSelector.plugIncolorServicesSamplePoint:
					
					Point16* point = (Point16*)info.selectorParameter.globalSamplePoint.ToPointer();

					if ((point->h >= 0 && point-> h < source.Width) && (point->v >= 0 && point->v < source.Height))
					{
						if (imageMode == ImageModes.plugInModeGray16 || imageMode == ImageModes.plugInModeRGB48)
						{
							return PSError.paramErr; // TODO: Sample 16-bit data and scale to 8-bit? Photoshop appears to return 255 for all 16-bit data.
						}
						
						byte* pixel = source.GetPointAddressUnchecked(point->h, point->v);
						info.colorComponents[0] = pixel[2];
						info.colorComponents[1] = pixel[1];
						info.colorComponents[2] = pixel[0];
						info.colorComponents[3] = 0;			 
								
						
						err = ColorServicesConvert.Convert(info.sourceSpace, info.resultSpace, ref info.colorComponents);
					}
					else
					{
						err = PSError.errInvalidSamplePoint;
					}

					break;

			}
			return err;
		}

		private SurfaceBase scaledChannelSurface;
		private SurfaceBase convertedChannelSurface;
		private Surface8 scaledSelectionMask;
		private ImageModes convertedChannelImageMode;

		private unsafe void FillChannelData(int channel, PixelMemoryDesc destiniation, SurfaceBase source, VRect srcRect, ImageModes mode)
		{
			byte* dstPtr = (byte*)destiniation.data.ToPointer();
			int stride = destiniation.rowBits / 8;
			int bpp = destiniation.colBits / 8;
			int offset = destiniation.bitOffset / 8;

			switch (mode)
			{
			  
				case ImageModes.plugInModeGrayScale:


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
				case ImageModes.plugInModeRGBColor:

					for (int y = srcRect.top; y < srcRect.bottom; y++)
					{
						byte* src = source.GetPointAddressUnchecked(srcRect.left, y);
						byte* dst = dstPtr + (y * stride) + offset;
						for (int x = srcRect.left; x < srcRect.right; x++)
						{
							switch (channel)
							{
								case 0:
									*dst = src[2];
									break;
								case 1:
									*dst = src[1];
									break;
								case 2:
									*dst = src[0];
									break;
								case 3:
									*dst = src[3];
									break;
							}
							src += 4;
							dst += bpp;
						}
					}

					break;
				case ImageModes.plugInModeGray16:

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
				case ImageModes.plugInModeRGB48:

					for (int y = srcRect.top; y < srcRect.bottom; y++)
					{
						ushort* src = (ushort*)source.GetPointAddressUnchecked(srcRect.left, y);
						ushort* dst = (ushort*)(dstPtr + (y * stride) + offset);
						for (int x = srcRect.left; x < srcRect.right; x++)
						{
							switch (channel)
							{
								case 0:
									*dst = src[2];
									break;
								case 1:
									*dst = src[1];
									break;
								case 2:
									*dst = src[0];
									break;
								case 3:
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

		private unsafe void FillSelectionMask(PixelMemoryDesc destiniation, Surface8 source, VRect srcRect)
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

		private bool useChannelPorts;
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

			if (channel < 0 || channel > 4)
			{
				return PSError.errUnknownPort;
			}
			
			VRect srcRect = scaling.sourceRect;
			VRect dstRect = scaling.destinationRect;

			int srcWidth = srcRect.right - srcRect.left;
			int srcHeight = srcRect.bottom - srcRect.top;
			int dstWidth = dstRect.right - dstRect.left;
			int dstHeight = dstRect.bottom - dstRect.top;
			bool isSelection = channel == 4;

			if ((imageMode == ImageModes.plugInModeRGBColor || isSelection) && destination.depth == 16)
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

						scaledSelectionMask = new Surface8(dstWidth, dstHeight);
						scaledSelectionMask.SuperSampleFitSurface(mask);
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

						scaledSelectionMask = new Surface8(dstWidth, dstHeight);
						scaledSelectionMask.BicubicFitSurface(mask);
					}

					FillSelectionMask(destination, scaledSelectionMask, dstRect);
				} 

			}
			else
			{
				SurfaceBase temp = null;
				ImageModes tempMode = this.imageMode;

				if ((imageMode == ImageModes.plugInModeGray16 || imageMode == ImageModes.plugInModeRGB48) && !isSelection && destination.depth == 8)
				{
					if (convertedChannelSurface == null)
					{
						int width = source.Width;
						int height = source.Height;

						switch (imageMode)
						{
							case ImageModes.plugInModeGray16:
								
								convertedChannelImageMode = ImageModes.plugInModeGrayScale;
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

							case ImageModes.plugInModeRGB48:

								convertedChannelImageMode = ImageModes.plugInModeRGBColor;
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

						scaledChannelSurface = SurfaceFactory.CreateFromImageMode(dstWidth, dstHeight, tempMode);
						scaledChannelSurface.SuperSampleFitSurface(temp);

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

						scaledChannelSurface = SurfaceFactory.CreateFromImageMode(dstWidth, dstHeight, tempMode);
						scaledChannelSurface.BicubicFitSurface(temp);
					}

					FillChannelData(channel, destination, scaledChannelSurface, dstRect, tempMode);
				} 
			}


			wroteRect = dstRect;

			return PSError.noErr;
		}

		private short WriteBasePixels(IntPtr port, ref VRect writeRect, PixelMemoryDesc source)
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

		struct ChannelDescPtrs
		{
			public IntPtr address;
			public IntPtr name;
		}

		private List<ChannelDescPtrs> channelReadDescPtrs;

		private unsafe void CreateReadImageDocument()
		{
			readDocumentPtr = Memory.Allocate(Marshal.SizeOf(typeof(ReadImageDocumentDesc)), true);
			ReadImageDocumentDesc* doc = (ReadImageDocumentDesc*)readDocumentPtr.ToPointer();
			doc->minVersion = PSConstants.kCurrentMinVersReadImageDocDesc;
			doc->maxVersion = PSConstants.kCurrentMaxVersReadImageDocDesc;
			doc->imageMode = (int)imageMode;
			doc->depth = imageMode == ImageModes.plugInModeRGB48 ? 16 : 8;
			doc->bounds.top = 0;
			doc->bounds.left = 0;
			doc->bounds.right = source.Width;
			doc->bounds.bottom = source.Height;
			doc->hResolution = int2fixed((int)(dpiX + 0.5));
			doc->vResolution = int2fixed((int)(dpiY + 0.5));

			if (imageMode == ImageModes.plugInModeRGBColor || imageMode == ImageModes.plugInModeRGB48)
			{
				string[] names = new string[3] { Resources.RedChannelName, Resources.GreenChannelName, Resources.BlueChannelName };
				IntPtr channel = CreateReadChannelDesc(0, names[0], doc->depth, doc->bounds);

				ReadChannelDesc* ch = (ReadChannelDesc*)channel.ToPointer();

				for (int i = 1; i < 3; i++)
				{
					IntPtr ptr = CreateReadChannelDesc(i, names[i], doc->depth, doc->bounds);

					ch->next = ptr;

					ch = (ReadChannelDesc*)ptr.ToPointer();
				}

				doc->targetCompositeChannels = doc->mergedCompositeChannels = channel;

				if (!ignoreAlpha)
				{
					IntPtr alphaPtr = CreateReadChannelDesc(3, Resources.AlphaChannelName, doc->depth, doc->bounds);
					doc->targetTransparency = doc->mergedTransparency = alphaPtr;
				} 
			}
			else
			{
				IntPtr channel = CreateReadChannelDesc(0, Resources.GrayChannelName, doc->depth, doc->bounds);
				doc->targetCompositeChannels = doc->mergedCompositeChannels = channel;
			}

			if (selectedRegion != null)
			{
				IntPtr selectionPtr = CreateReadChannelDesc(4, Resources.MaskChannelName, doc->depth, doc->bounds);
				doc->selection = selectionPtr;
			}
		}

		private unsafe IntPtr CreateReadChannelDesc(int channel, string name, int depth, VRect bounds)
		{
			IntPtr addressPtr = Memory.Allocate(Marshal.SizeOf(typeof(ReadChannelDesc)), true);
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

			bool rgb = name != Resources.GrayChannelName;


			desc->port = new IntPtr(channel);
			switch (channel)
			{
				case 0:
					desc->channelType = rgb ? ChannelTypes.ctRed : ChannelTypes.ctBlack;
					break;
				case 1:
					desc->channelType = ChannelTypes.ctGreen;
					break;
				case 2:
					desc->channelType = ChannelTypes.ctBlue;
					break;
				case 3:
					desc->channelType = ChannelTypes.ctTransparency;
					break;
				case 4:
					desc->channelType = ChannelTypes.ctSelectionMask;
					break;
			}
			IntPtr namePtr = Marshal.StringToHGlobalAnsi(name);

			channelReadDescPtrs.Add(new ChannelDescPtrs() { address = addressPtr, name = namePtr });

			return addressPtr;
		}

		/// <summary>
		/// Sets the filter padding.
		/// </summary>
		/// <param name="inData">The input data.</param>
		/// <param name="inRowBytes">The input row bytes (stride).</param>
		/// <param name="rect">The input rect.</param>
		/// <param name="nplanes">The number of channels in the image.</param>
		/// <param name="ofs">The single channel offset to map to BGRA color space.</param>
		/// <param name="inputPadding">The input padding mode.</param>
		/// <param name="lockRect">The lock rect.</param>
		/// <param name="surface">The surface.</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		private unsafe short SetFilterPadding(IntPtr inData, int inRowBytes, Rect16 rect, int nplanes, short ofs, short inputPadding, Rectangle lockRect, SurfaceBase surface)
		{
			if ((lockRect.Right > surface.Width || lockRect.Bottom > surface.Height) || (rect.top < 0 || rect.left < 0))
			{
				switch (inputPadding)
				{
					case HostPadding.plugInWantsEdgeReplication:
						
						
						int top = rect.top < 0 ? -rect.top : 0;
						int left = rect.left < 0 ? -rect.left : 0;

						int right = lockRect.Right - surface.Width;
						int bottom = lockRect.Bottom - surface.Height;

						int height = rect.bottom - rect.top;
						int width = rect.right - rect.left;

						int sWidth = surface.Width;
						int sHeight = surface.Height;
						int row, col;

						byte* ptr = (byte*)inData.ToPointer();

						int srcChannelCount = surface.ChannelCount;

						#region Padding code

						if (surface.BitsPerChannel == 16)
						{
							if (top > 0)
							{
								for (int y = 0; y < top; y++)
								{
									ushort* p = (ushort*)surface.GetRowAddressUnchecked(0);
									ushort* q = (ushort*)ptr + (y * inRowBytes);

									for (int x = 0; x < width; x++)
									{
										switch (nplanes)
										{
											case 1:
												*q = p[ofs];
												break;
											case 2:
												q[0] = p[ofs];
												q[1] = p[ofs + 1];
												break;
											case 3:
												q[0] = p[2];
												q[1] = p[1];
												q[2] = p[0];
												break;
											case 4:
												q[0] = p[2];
												q[1] = p[1];
												q[2] = p[0];
												q[3] = p[3];
												break;
										}

										p += srcChannelCount;
										q += nplanes;
									}
								}
							}

							if (left > 0)
							{
								for (int y = 0; y < height; y++)
								{                                   
									ushort* p = (ushort*)surface.GetPointAddressUnchecked(0, y);
									ushort* q = (ushort*)ptr + (y * inRowBytes);

									for (int x = 0; x < left; x++)
									{
										switch (nplanes)
										{
											case 1:
												*q = p[ofs];
												break;
											case 2:
												q[0] = p[ofs];
												q[1] = p[ofs + 1];
												break;
											case 3:
												q[0] = p[2];
												q[1] = p[1];
												q[2] = p[0];
												break;
											case 4:
												q[0] = p[2];
												q[1] = p[1];
												q[2] = p[0];
												q[3] = p[3];
												break;
										}
										q += nplanes;
									}
								}
							}


							if (bottom > 0)
							{
								col = sHeight - 1;
								int lockBottom = height - 1;
								for (int y = 0; y < bottom; y++)
								{
									ushort* p = (ushort*)surface.GetRowAddressUnchecked(col);
									ushort* q = (ushort*)ptr + ((lockBottom - y) * inRowBytes);

									for (int x = 0; x < width; x++)
									{
										switch (nplanes)
										{
											case 1:
												*q = p[ofs];
												break;
											case 2:
												q[0] = p[ofs];
												q[1] = p[ofs + 1];
												break;
											case 3:
												q[0] = p[2];
												q[1] = p[1];
												q[2] = p[0];
												break;
											case 4:
												q[0] = p[2];
												q[1] = p[1];
												q[2] = p[0];
												q[3] = p[3];
												break;
										}

										p += srcChannelCount;
										q += nplanes;
									}

								}
							}

							if (right > 0)
							{
								row = sWidth - 1;
								int rowEnd = width - right;
								for (int y = 0; y < height; y++)
								{
									ushort* q = (ushort*)ptr + (y * inRowBytes) + rowEnd;

									ushort* p = (ushort*)surface.GetPointAddressUnchecked(row, y);

									for (int x = 0; x < right; x++)
									{
										switch (nplanes)
										{
											case 1:
												*q = p[ofs];
												break;
											case 2:
												q[0] = p[ofs];
												q[1] = p[ofs + 1];
												break;
											case 3:
												q[0] = p[2];
												q[1] = p[1];
												q[2] = p[0];
												break;
											case 4:
												q[0] = p[2];
												q[1] = p[1];
												q[2] = p[0];
												q[3] = p[3];
												break;
										}
										q += nplanes;
									}
								}
							}
						}
						else
						{
							if (top > 0)
							{
								for (int y = 0; y < top; y++)
								{
									byte* p = surface.GetRowAddressUnchecked(0);
									byte* q = ptr + (y * inRowBytes);

									for (int x = 0; x < width; x++)
									{
										switch (nplanes)
										{
											case 1:
												*q = p[ofs];
												break;
											case 2:
												q[0] = p[ofs];
												q[1] = p[ofs + 1];
												break;
											case 3:
												q[0] = p[2];
												q[1] = p[1];
												q[2] = p[0];
												break;
											case 4:
												q[0] = p[2];
												q[1] = p[1];
												q[2] = p[0];
												q[3] = p[3];
												break;
										}

										p += srcChannelCount;
										q += nplanes;
									}
								}
							}


							if (left > 0)
							{
								for (int y = 0; y < height; y++)
								{
									byte* q = ptr + (y * inRowBytes);

									byte* p = surface.GetPointAddressUnchecked(0, y);

									for (int x = 0; x < left; x++)
									{
										switch (nplanes)
										{
											case 1:
												*q = p[ofs];
												break;
											case 2:
												q[0] = p[ofs];
												q[1] = p[ofs + 1];
												break;
											case 3:
												q[0] = p[2];
												q[1] = p[1];
												q[2] = p[0];
												break;
											case 4:
												q[0] = p[2];
												q[1] = p[1];
												q[2] = p[0];
												q[3] = p[3];
												break;
										}
										q += nplanes;
									}
								}
							}


							if (bottom > 0)
							{
								col = sHeight - 1;
								int lockBottom = height - 1;
								for (int y = 0; y < bottom; y++)
								{
									byte* p = surface.GetRowAddressUnchecked(col);
									byte* q = ptr + ((lockBottom - y) * inRowBytes);

									for (int x = 0; x < width; x++)
									{
										switch (nplanes)
										{
											case 1:
												*q = p[ofs];
												break;
											case 2:
												q[0] = p[ofs];
												q[1] = p[ofs + 1];
												break;
											case 3:
												q[0] = p[2];
												q[1] = p[1];
												q[2] = p[0];
												break;
											case 4:
												q[0] = p[2];
												q[1] = p[1];
												q[2] = p[0];
												q[3] = p[3];
												break;
										}

										p += srcChannelCount;
										q += nplanes;
									}

								}
							}

							if (right > 0)
							{
								row = sWidth - 1;
								int rowEnd = width - right;
								for (int y = 0; y < height; y++)
								{
									byte* q = ptr + (y * inRowBytes) + rowEnd;

									byte* p = surface.GetPointAddressUnchecked(row, y);

									for (int x = 0; x < right; x++)
									{
										switch (nplanes)
										{
											case 1:
												*q = p[ofs];
												break;
											case 2:
												q[0] = p[ofs];
												q[1] = p[ofs + 1];
												break;
											case 3:
												q[0] = p[2];
												q[1] = p[1];
												q[2] = p[0];
												break;
											case 4:
												q[0] = p[2];
												q[1] = p[1];
												q[2] = p[0];
												q[3] = p[3];
												break;
										}
										q += nplanes;
									}
								}
							}
						}

						#endregion

						break;
					case HostPadding.plugInDoesNotWantPadding:
						break;
					case HostPadding.plugInWantsErrorOnBoundsException:
						return PSError.paramErr;
					default:
						long size = Memory.Size(inData);
						SafeNativeMethods.memset(inData, inputPadding, new UIntPtr((ulong)size));
						break;
				}

			}

			return PSError.noErr;
		}

		private Surface32 tempDisplaySurface;
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

		private unsafe short DisplayPixelsProc(ref PSPixelMap source, ref VRect srcRect, int dstRow, int dstCol, IntPtr platformContext)
		{
#if DEBUG
			Ping(DebugFlags.DisplayPixels, string.Format("source: version = {0} bounds = {1}, ImageMode = {2}, colBytes = {3}, rowBytes = {4},planeBytes = {5}, BaseAddress = {6}, mat = {7}, masks = {8}", new object[]{ source.version.ToString(), source.bounds.ToString(), ((ImageModes)source.imageMode).ToString("G"),				source.colBytes.ToString(), source.rowBytes.ToString(), source.planeBytes.ToString(), source.baseAddr.ToString("X8"), source.mat.ToString("X8"), source.masks.ToString("X8")}));
			Ping(DebugFlags.DisplayPixels, string.Format("srcRect = {0} dstCol (x, width) = {1}, dstRow (y, height) = {2}", srcRect.ToString(), dstCol, dstRow));
#endif

			if (platformContext == IntPtr.Zero || source.rowBytes == 0 || source.baseAddr == IntPtr.Zero ||
				(source.imageMode != PSConstants.plugInModeRGBColor && source.imageMode != PSConstants.plugInModeGrayScale))
				return PSError.filterBadParameters;

			int width = srcRect.right - srcRect.left;
			int height = srcRect.bottom - srcRect.top;
			int nplanes = ((FilterRecord*)filterRecordPtr.ToPointer())->planes;

			SetupTempDisplaySurface(width, height, (source.version >= 1 && source.masks != IntPtr.Zero));

			void* baseAddr = source.baseAddr.ToPointer();

			int top = srcRect.top;
			int bottom = srcRect.bottom;
			int left = srcRect.left;
			// Some plug-ins set the srcRect incorrectly for 100% zoom.
			if (source.bounds.Equals(srcRect) && (top > 0 || left > 0))
			{
				top = left = 0;
				bottom = height;
			}

			if (source.imageMode == PSConstants.plugInModeGrayScale)
			{
				for (int y = top; y < bottom; y++)
				{
					byte* p = tempDisplaySurface.GetRowAddressUnchecked(y - top);
					byte* q = (byte*)baseAddr + (y * source.rowBytes) + left;
					for (int x = 0; x < width; x++)
					{
						p[0] = p[1] = p[2] = *q;

						p += 4;
						q += source.colBytes;
					}

				}
			}
			else
			{
				for (int y = top; y < bottom; y++)
				{
					int surfaceY = y - top;
					if (source.colBytes == 1)
					{
						byte* row = tempDisplaySurface.GetRowAddressUnchecked(surfaceY);
						int srcStride = y * source.rowBytes; // cache the destination row and source stride.
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
							byte* p = row + ofs;
							byte* q = (byte*)baseAddr + srcStride + (i * source.planeBytes) + left;

							for (int x = 0; x < width; x++)
							{
								*p = *q;

								p += 4;
								q += source.colBytes;
							}
						}

					}
					else
					{
						byte* p = tempDisplaySurface.GetRowAddressUnchecked(surfaceY);
						byte* q = (byte*)baseAddr + (y * source.rowBytes) + left;
						for (int x = 0; x < width; x++)
						{
							p[0] = q[2];
							p[1] = q[1];
							p[2] = q[0];
							if (source.colBytes == 4)
							{
								p[3] = q[3];
							}

							p += 4;
							q += source.colBytes;
						}
					}
				} 
			}


			using (Graphics gr = Graphics.FromHdc(platformContext))
			{
				if (source.colBytes == 4)
				{
					Display32BitBitmap(gr, dstCol, dstRow);
				}
				else
				{
					if ((source.version >= 1) && source.masks != IntPtr.Zero) // use the mask for the Protected Transparency cases 
					{
						PSPixelMask* mask = (PSPixelMask*)source.masks.ToPointer();
						byte* maskData = (byte*)mask->maskData.ToPointer();

						for (int y = 0; y < height; y++)
						{
							byte* p = tempDisplaySurface.GetRowAddressUnchecked(y);
							byte* q = maskData + (y * mask->rowBytes);
							for (int x = 0; x < width; x++)
							{
								p[3] = *q;

								p += 4;
								q += mask->colBytes;
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

		private Bitmap checkerBoardBitmap;
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

		/// <summary>
		/// The selection mask for the image
		/// </summary>
		private Surface8 mask;
		private unsafe void DrawMask()
		{
			mask = new Surface8(source.Width, source.Height);

			for (int y = 0; y < mask.Height; y++)
			{
				byte* p = mask.GetRowAddressUnchecked(y);
				for (int x = 0; x < mask.Width; x++)
				{
					if (selectedRegion.IsVisible(x, y))
					{
						*p = 255; 
					}
					else
					{
						*p = 0;
					}

					p++;
				}
			}

		}



		#region DescriptorParameters

		private short descErr;
		private short descErrValue;
		private uint getKey;
		private int getKeyIndex;
		private List<uint> keys;
		private List<uint> subKeys;
		private List<uint> aeteKeyIndex;
		private bool isSubKey;
		private int subKeyIndex;
		private int subClassIndex;
		private Dictionary<uint, AETEValue> subClassDict;


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
						while (true)
						{
							if (*ptr == 0)
							{
								break;
							}
#if DEBUG
							Ping(DebugFlags.DescriptorParameters, string.Format("key = {0}", PropToString(*ptr)));
#endif

							keys.Add(*ptr);
							ptr++;
						}
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
				else
				{
					subKeys = new List<uint>();
					if (keyArray != IntPtr.Zero)
					{
						uint* ptr = (uint*)keyArray.ToPointer();
						while (true)
						{
							if (*ptr == 0)
							{
								break;
							}
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

					if ((handle_valid(descriptor) && HandleGetSizeProc(descriptor) == 0) ||
						aeteDict.ContainsKey(getKey) && aeteDict[getKey].Value is Dictionary<uint, AETEValue>)
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

				if ((keys != null) && keys.Count == 0)
				{
					keys.AddRange(aeteDict.Keys); // if the keys are not passed to us grab them from the aeteDict.
				}

				return HandleNewProc(1); // return a dummy handle to the key value pairs
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
						if (subClassIndex > (subClassDict.Count - 1))
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
						if (subKeyIndex > (subKeys.Count - 1))
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
					if (getKeyIndex > (keys.Count - 1))
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
			if (subClassDict != null)
			{
				data = (int)subClassDict[getKey].Value;
			}
			else
			{
				data = (int)aeteDict[getKey].Value;
			}
			return PSError.noErr;
		}
		private short GetFloatProc(IntPtr descriptor, ref double data)
		{
			if (subClassDict != null)
			{
				data = (double)subClassDict[getKey].Value;
			}
			else
			{
				data = (double)aeteDict[getKey].Value;
			}
			return PSError.noErr;
		}
		private short GetUnitFloatProc(IntPtr descriptor, ref uint type, ref double data)
		{
			AETEValue item = null;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = aeteDict[getKey];
			}

			try
			{
				type = item.Type;
			}
			catch (NullReferenceException)
			{
			}

			data = (double)item.Value;
			return PSError.noErr;
		}
		private short GetBooleanProc(IntPtr descriptor, ref byte data)
		{
			data = (byte)aeteDict[getKey].Value;

			return PSError.noErr;
		}
		private short GetTextProc(IntPtr descriptor, ref IntPtr data)
		{
			AETEValue item = aeteDict[getKey];

			int size = item.Size;
			data = HandleNewProc(size);
			IntPtr hPtr = HandleLockProc(data, 0);
			Marshal.Copy((byte[])item.Value, 0, hPtr, size);
			HandleUnlockProc(data);

			return PSError.noErr;
		}
		private short GetAliasProc(IntPtr descriptor, ref IntPtr data)
		{
			AETEValue item = aeteDict[getKey];

			int size = item.Size;
			data = HandleNewProc(size);
			IntPtr hPtr = HandleLockProc(data, 0);
			Marshal.Copy((byte[])item.Value, 0, hPtr, size);
			HandleUnlockProc(data);
			return PSError.noErr;
		}
		private short GetEnumeratedProc(IntPtr descriptor, ref uint type)
		{
			type = (uint)aeteDict[getKey].Value;

			return PSError.noErr;
		}
		private short GetClassProc(IntPtr descriptor, ref uint type)
		{
			return PSError.errPlugInHostInsufficient;
		}

		private short GetSimpleReferenceProc(IntPtr descriptor, ref PIDescriptorSimpleReference data)
		{
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

			byte[] bytes = null;
			IntPtr hPtr = IntPtr.Zero;
			switch (type)
			{

				case DescriptorTypes.classRGBColor:
				case DescriptorTypes.classCMYKColor:
				case DescriptorTypes.classGrayscale:
				case DescriptorTypes.classLabColor:
				case DescriptorTypes.classHSBColor:
					data = HandleNewProc(0); // assign a zero byte handle to allow it to work correctly in the OpenReadDescriptorProc(). 
					break;

				case DescriptorTypes.typeAlias:
				case DescriptorTypes.typePath:
				case DescriptorTypes.typeChar:

					int size = item.Size;
					data = HandleNewProc(size);
					hPtr = HandleLockProc(data, 0);
					Marshal.Copy((byte[])item.Value, 0, hPtr, size);
					HandleUnlockProc(data);
					break;
				case DescriptorTypes.typeBoolean:
					data = HandleNewProc(1);
					hPtr = HandleLockProc(data, 0);
					bytes = new byte[1] { (byte)item.Value };
					Marshal.Copy(bytes, 0, hPtr, bytes.Length);
					HandleUnlockProc(data);
					break;
				case DescriptorTypes.typeInteger:
					data = HandleNewProc(Marshal.SizeOf(typeof(Int32)));
					hPtr = HandleLockProc(data, 0);
					bytes = BitConverter.GetBytes((int)item.Value);
					Marshal.Copy(bytes, 0, hPtr, bytes.Length);
					HandleUnlockProc(data);
					break;
				case DescriptorTypes.typeFloat:
				case DescriptorTypes.typeUintFloat:
					data = HandleNewProc(Marshal.SizeOf(typeof(double)));
					hPtr = HandleLockProc(data, 0);
					bytes = BitConverter.GetBytes((double)item.Value);
					Marshal.Copy(bytes, 0, hPtr, bytes.Length);
					HandleUnlockProc(data);
					break;

				default:
					break;
			}

			return PSError.noErr;
		}
		private short GetCountProc(IntPtr descriptor, ref uint count)
		{
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
			AETEValue item = aeteDict[getKey];
			int size = item.Size;

			Marshal.WriteByte(data, (byte)size);

			Marshal.Copy((byte[])item.Value, 0, new IntPtr(data.ToInt64() + 1L), size);
			return PSError.noErr;
		}
		private short GetPinnedIntegerProc(IntPtr descriptor, int min, int max, ref int intNumber)
		{
			descErr = PSError.noErr;
			int amount = (int)aeteDict[getKey].Value;
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
			descErr = PSError.noErr;
			double amount = (double)aeteDict[getKey].Value;
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
			descErr = PSError.noErr;

			double amount = (double)aeteDict[getKey].Value;
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

			descriptorHandle = HandleNewProc(1);

			return PSError.noErr;
		}

		private int GetAETEParmFlags(uint key)
		{
			if (aete != null)
			{
				foreach (var evnt in aete.events)
				{
					foreach (var item in evnt.parms)
					{
						if (item.key == key)
						{
							return item.flags;
						}
					} 
				}
			}

			return 0;
		}

		private short PutIntegerProc(IntPtr descriptor, uint key, int param2)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, PropToString(key)));
#endif
			aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeInteger, GetAETEParmFlags(key), 0, param2));
			return PSError.noErr;
		}

		private short PutFloatProc(IntPtr descriptor, uint key, ref double data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeFloat, GetAETEParmFlags(key), 0, data));
			return PSError.noErr;

		}

		private short PutUnitFloatProc(IntPtr descriptor, uint key, uint unit, ref double data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeUintFloat, GetAETEParmFlags(key), 0, data));
			return PSError.noErr;
		}

		private short PutBooleanProc(IntPtr descriptor, uint key, byte data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeBoolean, GetAETEParmFlags(key), 0, data));
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
					if (handle_valid(textHandle))
					{

						int size = HandleGetSizeProc(textHandle);
						byte[] data = new byte[size];
						Marshal.Copy(hPtr, data, 0, size);

						aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeChar, GetAETEParmFlags(key), size, data));
					}
					else
					{
						byte[] data = null;
						int size = 0;
						if (!IsBadReadPtr(hPtr))
						{
							size = SafeNativeMethods.GlobalSize(hPtr).ToInt32();
							data = new byte[size];
							Marshal.Copy(hPtr, data, 0, size);
						}
						else
						{
							size = SafeNativeMethods.GlobalSize(textHandle).ToInt32();
							data = new byte[size];
							Marshal.Copy(textHandle, data, 0, size);
						}

						aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeChar, GetAETEParmFlags(key), size, data));
					}
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
				if (handle_valid(aliasHandle))
				{
					int size = HandleGetSizeProc(aliasHandle);
					byte[] data = new byte[size];
					Marshal.Copy(hPtr, data, 0, size);

					aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeAlias, GetAETEParmFlags(key), size, data));
				}
				else
				{
					int size = SafeNativeMethods.GlobalSize(aliasHandle).ToInt32();
					byte[] data = new byte[size];
					if (!IsBadReadPtr(hPtr))
					{
						size = SafeNativeMethods.GlobalSize(hPtr).ToInt32();
						data = new byte[size];
						Marshal.Copy(hPtr, data, 0, size);
					}
					else
					{
						size = SafeNativeMethods.GlobalSize(aliasHandle).ToInt32();
						data = new byte[size];
						Marshal.Copy(aliasHandle, data, 0, size);
					}
					aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeAlias, GetAETEParmFlags(key), size, data));

				}
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
			aeteDict.AddOrUpdate(key, new AETEValue(type, GetAETEParmFlags(key), 0, data));
			return PSError.noErr;
		}

		private short PutClassProc(IntPtr descriptor, uint key, uint data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif

			// What does the PutClassProc function do?
			return PSError.errPlugInHostInsufficient;
		}

		private short PutSimpleReferenceProc(IntPtr descriptor, uint key, ref PIDescriptorSimpleReference data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeObjectRefrence, GetAETEParmFlags(key), 0, data));
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

					aeteDict.AddOrUpdate(key, new AETEValue(type, GetAETEParmFlags(key), 0, classDict));
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

					aeteDict.AddOrUpdate(key, new AETEValue(type, GetAETEParmFlags(key), 0, classDict));
					break;
				case DescriptorTypes.classGrayscale:
					classDict = new Dictionary<uint, AETEValue>(1);
					classDict.Add(DescriptorKeys.keyGray, aeteDict[DescriptorKeys.keyGray]);

					aeteDict.Remove(DescriptorKeys.keyGray);

					aeteDict.AddOrUpdate(key, new AETEValue(type, GetAETEParmFlags(key), 0, classDict));
					break;
				case DescriptorTypes.classLabColor:
					classDict = new Dictionary<uint, AETEValue>(3);
					classDict.Add(DescriptorKeys.keyLuminance, aeteDict[DescriptorKeys.keyLuminance]);
					classDict.Add(DescriptorKeys.keyA, aeteDict[DescriptorKeys.keyA]);
					classDict.Add(DescriptorKeys.keyB, aeteDict[DescriptorKeys.keyB]);

					aeteDict.Remove(DescriptorKeys.keyLuminance);
					aeteDict.Remove(DescriptorKeys.keyA);
					aeteDict.Remove(DescriptorKeys.keyB);

					aeteDict.AddOrUpdate(key, new AETEValue(type, GetAETEParmFlags(key), 0, classDict));
					break;
				case DescriptorTypes.classHSBColor:
					classDict = new Dictionary<uint, AETEValue>(3);
					classDict.Add(DescriptorKeys.keyHue, aeteDict[DescriptorKeys.keyHue]);
					classDict.Add(DescriptorKeys.keySaturation, aeteDict[DescriptorKeys.keySaturation]);
					classDict.Add(DescriptorKeys.keyBrightness, aeteDict[DescriptorKeys.keyBrightness]);

					aeteDict.Remove(DescriptorKeys.keyHue);
					aeteDict.Remove(DescriptorKeys.keySaturation);
					aeteDict.Remove(DescriptorKeys.keyBrightness);

					aeteDict.AddOrUpdate(key, new AETEValue(type, GetAETEParmFlags(key), 0, classDict));
					break;
				case DescriptorTypes.classPoint:
					classDict = new Dictionary<uint, AETEValue>(2);

					classDict.Add(DescriptorKeys.keyHorizontal, aeteDict[DescriptorKeys.keyHorizontal]);
					classDict.Add(DescriptorKeys.keyVertical, aeteDict[DescriptorKeys.keyVertical]);

					aeteDict.Remove(DescriptorKeys.keyHorizontal);
					aeteDict.Remove(DescriptorKeys.keyVertical);

					aeteDict.AddOrUpdate(key, new AETEValue(type, GetAETEParmFlags(key), 0, classDict));

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

			aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeChar, GetAETEParmFlags(key), size, data));

			return PSError.noErr;
		}

		private short PutScopedClassProc(IntPtr descriptor, uint key, uint data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			return PSError.errPlugInHostInsufficient;
		}
		private short PutScopedObjectProc(IntPtr descriptor, uint key, uint type, ref IntPtr handle)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif
			IntPtr hPtr = HandleLockProc(handle, 0);

			try
			{
				if (handle_valid(handle))
				{
					int size = HandleGetSizeProc(handle);
					byte[] data = new byte[size];
					Marshal.Copy(hPtr, data, 0, size);

					aeteDict.AddOrUpdate(key, new AETEValue(type, GetAETEParmFlags(key), size, data));
				}
				else
				{
					byte[] data = null;
					int size = 0;
					if (!IsBadReadPtr(hPtr))
					{
						size = SafeNativeMethods.GlobalSize(hPtr).ToInt32();
						data = new byte[size];
						Marshal.Copy(hPtr, data, 0, size);
					}
					else
					{
						size = SafeNativeMethods.GlobalSize(handle).ToInt32();
						data = new byte[size];
						Marshal.Copy(handle, data, 0, size);
					}


					aeteDict.AddOrUpdate(key, new AETEValue(type, GetAETEParmFlags(key), size, data));
				}
			}
			finally
			{
				HandleUnlockProc(handle);
			}



			return PSError.noErr;
		}

		#endregion


		private bool handle_valid(IntPtr h)
		{
			return handles.ContainsKey(h);
		}

		private unsafe IntPtr HandleNewProc(int size)
		{
			try
			{
				IntPtr handle = Memory.Allocate(Marshal.SizeOf(typeof(PSHandle)), true);

				PSHandle* hand = (PSHandle*)handle.ToPointer();
				hand->pointer = Memory.Allocate(size, true);
				hand->size = size;

				handles.Add(handle, *hand);
#if DEBUG
				Ping(DebugFlags.HandleSuite, string.Format("Handle address = {0:X8}, size = {1}", hand->pointer.ToInt64(), size));
#endif
				return handle;
			}
			catch (OutOfMemoryException)
			{
				return IntPtr.Zero;
			}
		}

		private unsafe void HandleDisposeProc(IntPtr h)
		{
			if (h != IntPtr.Zero && !IsBadReadPtr(h))
			{
#if DEBUG
				Ping(DebugFlags.HandleSuite, string.Format("Handle address = {0:X8}", h.ToInt64()));
#endif
				if (!handle_valid(h))
				{
					if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
					{
						IntPtr hPtr = Marshal.ReadIntPtr(h);

						if (!IsBadReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
						{
							SafeNativeMethods.GlobalFree(hPtr);
						}

						SafeNativeMethods.GlobalFree(h);
						return;
					}
					else
					{
						return;
					}
				}



				PSHandle* handle = (PSHandle*)h.ToPointer();

				Memory.Free(handle->pointer);
				Memory.Free(h);				
				
				handles.Remove(h);
			}
		}

		private unsafe void HandleDisposeRegularProc(IntPtr h)
		{
			// What is this supposed to do?
			if (!handle_valid(h))
			{
				if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					IntPtr hPtr = Marshal.ReadIntPtr(h);

					if (!IsBadReadPtr(hPtr))
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
			Ping(DebugFlags.HandleSuite, string.Format("Handle address = {0:X8}, moveHigh = {1:X1}", h.ToInt64(), moveHigh));
#endif
			if (!handle_valid(h))
			{
				if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					return SafeNativeMethods.GlobalLock(h);
				}
				if (!IsBadReadPtr(h) && IsBadWritePtr(h))
				{
					return h;
				}
				return IntPtr.Zero;
			}

#if DEBUG
			Ping(DebugFlags.HandleSuite, String.Format("Handle Pointer Address = 0x{0:X}", handles[h].pointer.ToInt64()));
#endif
			return handles[h].pointer;
		}

		private int HandleGetSizeProc(IntPtr h)
		{
#if DEBUG
			Ping(DebugFlags.HandleSuite, string.Format("Handle address = {0:X8}", h.ToInt64()));
#endif
			if (!handle_valid(h))
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
				else
				{
					return 0;
				}
			}

			return handles[h].size;
		}

		private void HandleRecoverSpaceProc(int size)
		{
#if DEBUG
			Ping(DebugFlags.HandleSuite, string.Format("size = {0}", size));
#endif
		}

		private unsafe short HandleSetSizeProc(IntPtr h, int newSize)
		{
#if DEBUG
			Ping(DebugFlags.HandleSuite, string.Format("Handle address = {0:X8}", h.ToInt64()));
#endif
			if (!handle_valid(h))
			{
				if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					IntPtr hPtr = Marshal.ReadIntPtr(h);

					if (!IsBadReadPtr(hPtr))
					{
						hPtr = SafeNativeMethods.GlobalReAlloc(hPtr, new UIntPtr((uint)newSize), NativeConstants.GPTR);
						if (hPtr == IntPtr.Zero)
						{
							return PSError.nilHandleErr;
						}
						Marshal.WriteIntPtr(h, hPtr);
					}
					else
					{
						if ((h = SafeNativeMethods.GlobalReAlloc(h, new UIntPtr((uint)newSize), NativeConstants.GPTR)) == IntPtr.Zero)
							return PSError.nilHandleErr;
					}

					return PSError.noErr;
				}
				else
				{
					return PSError.nilHandleErr;
				}
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
			Ping(DebugFlags.HandleSuite, string.Format("Handle address = {0:X8}", h.ToInt64()));
#endif
			if (!handle_valid(h))
			{
				if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					SafeNativeMethods.GlobalUnlock(h);
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
				done = 0;
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Format("Done = {0}, Total = {1}", done, total));
			Ping(DebugFlags.MiscCallbacks, string.Format("progress = {0}%", (((double)done / (double)total) * 100d).ToString()));
#endif
			if (progressFunc != null)
			{
				progressFunc.Invoke(done, total);
			}
		}

		/// <summary>
		/// Reads the JPEG APP1 section to extract EXIF or XMP metadata.
		/// </summary>
		/// <param name="jpegData">The JPEG image byte array.</param>
		/// <param name="exif">if set to <c>true</c> extract the EXIF metadata; otherwise extract the XMP metadata.</param>
		/// <returns>The extracted data or null.</returns>
		private static unsafe byte[] ReadJpegAPP1(byte[] jpegData, bool exif)
		{
			byte[] bytes = null;
			fixed (byte* ptr = jpegData)
			{
				byte* p = ptr;
				if (p[0] != 0xff && p[1] != 0xd8) // JPEG file signature
				{
					return null;
				}
				p += 2;

				ushort sectionLength = 0;
				while (p[0] == 0xff && (p[1] >= 0xe0 && p[1] <= 0xef)) // APP sections
				{

					sectionLength = (ushort)((p[2] << 8) | p[3]); // JPEG uses big-endian   

					if (p[0] == 0xff && p[1] == 0xe1) // APP1
					{
						p += 2; // skip the header bytes

						string sig;

						if (exif)
						{
							sig = new string((sbyte*)p + 2, 0, 6, windows1252Encoding);

							if (sig == "Exif\0\0")
							{
								int exifLen = sectionLength - 8; // subtract the signature and section length size to get the data length. 
								bytes = new byte[exifLen];

								Marshal.Copy((IntPtr)(p + 8), bytes, 0, exifLen);
							}
							
							p += sectionLength;
							
						}
						else
						{
							sig = new string((sbyte*)p + 2, 0, 29, windows1252Encoding);

							if (sig == "http://ns.adobe.com/xap/1.0/\0")
							{
								int xmpLen = sectionLength - 31;
								bytes = new byte[xmpLen];
								Marshal.Copy((IntPtr)(p + 31), bytes, 0, xmpLen);

							}
							
							p += sectionLength;
						}

					}
					else
					{
						p += sectionLength + 2;
					}

				}
			}

			return bytes;
		}

		/// <summary>
		/// Extracts the JPEG metadata.
		/// </summary>
		/// <param name="bytes">The output bytes.</param>
		/// <param name="exif">set to <c>true</c> if the EXIF data is requested.</param>
		/// <returns></returns>
		private bool ExtractJPEGMetadata(out byte[] bytes, bool exif)
		{
			bytes = null;
			if (exifBitmap == null)
			{
				return false;
			}

			using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
			{
#if GDIPLUS
				exifBitmap.Save(ms, ImageFormat.Jpeg);
#else
				JpegBitmapEncoder enc = new JpegBitmapEncoder();
				enc.Frames.Add(BitmapFrame.Create(exifBitmap));
				enc.Save(ms); 
#endif

				bytes = ReadJpegAPP1(ms.GetBuffer(), exif);
			}

			return (bytes != null);
		}

		private unsafe short PropertyGetProc(uint signature, uint key, int index, ref int simpleProperty, ref IntPtr complexProperty)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Format("Sig: {0}, Key: {1}, Index: {2}", PropToString(signature), PropToString(key), index.ToString()));
#endif
			if (signature != PSConstants.kPhotoshopSignature)
				return PSError.errPlugInHostInsufficient;

			byte[] bytes = null;


			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			switch (key)
			{
				case PSProperties.propBigNudgeH:
				case PSProperties.propBigNudgeV:
					simpleProperty = int2fixed(10);
					break;
				case PSProperties.propCaption:
					complexProperty = HandleNewProc(0);
					break;
				case PSProperties.propChannelName:
					if (index < 0 || index > (filterRecord->planes - 1))
						return PSError.errPlugInPropertyUndefined;

					string name = string.Empty;
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

					bytes = Encoding.ASCII.GetBytes(name);

					complexProperty = HandleNewProc(bytes.Length);
					Marshal.Copy(bytes, 0, HandleLockProc(complexProperty, 0), bytes.Length);
					HandleUnlockProc(complexProperty);
					break;
				case PSProperties.propCopyright:
					simpleProperty = 0;  // no copyright
					break;
				case PSProperties.propEXIFData:
				case PSProperties.propXMPData:
					if (ExtractJPEGMetadata(out bytes, key == PSProperties.propEXIFData))
					{
						complexProperty = HandleNewProc(bytes.Length);
						Marshal.Copy(bytes, 0, HandleLockProc(complexProperty, 0), bytes.Length);
						HandleUnlockProc(complexProperty);
					}
					else
					{
						complexProperty = HandleNewProc(0);
					}
					break;
				case PSProperties.propGridMajor:
					simpleProperty = int2fixed(1);
					break;
				case PSProperties.propGridMinor:
					simpleProperty = 4;
					break;
				case PSProperties.propImageMode:
					simpleProperty = (int)filterRecord->imageMode;
					break;
				case PSProperties.propInterpolationMethod:
					simpleProperty = PSConstants.InterpolationMethod.NearestNeghbor;
					break;
				case PSProperties.propNumberOfChannels:
					simpleProperty = filterRecord->planes;
					break;
				case PSProperties.propNumberOfPaths:
					simpleProperty = 0;
					break;
				case PSProperties.propPathName:
					complexProperty = HandleNewProc(0);
					break;
				case PSProperties.propWorkPathIndex:
				case PSProperties.propClippingPathIndex:
				case PSProperties.propTargetPathIndex:
					simpleProperty = -1;
					break;
				case PSProperties.propRulerUnits:
					simpleProperty = PSConstants.RulerUnits.Pixels;
					break;
				case PSProperties.propRulerOriginH:
				case PSProperties.propRulerOriginV:
					simpleProperty = int2fixed(2);
					break;
				case PSProperties.propSerialString:
					bytes = Encoding.ASCII.GetBytes(filterRecord->serial.ToString(CultureInfo.InvariantCulture));
					complexProperty = HandleNewProc(bytes.Length);
					Marshal.Copy(bytes, 0, HandleLockProc(complexProperty, 0), bytes.Length);
					HandleUnlockProc(complexProperty);
					break;
				case PSProperties.propURL:
					complexProperty = HandleNewProc(0);
					break;
				case PSProperties.propTitle:
					bytes = Encoding.ASCII.GetBytes("temp.png"); // some filters just want a non empty string
					complexProperty = HandleNewProc(bytes.Length);
					Marshal.Copy(bytes, 0, HandleLockProc(complexProperty, 0), bytes.Length);
					HandleUnlockProc(complexProperty);
					break;
				case PSProperties.propWatchSuspension:
					simpleProperty = 0;
					break;
				default:
					return PSError.errPlugInPropertyUndefined;
			}


			return PSError.noErr;
		}

		private short PropertySetProc(uint signature, uint key, int index, int simpleProperty, ref IntPtr complexProperty)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Format("Sig: {0}, Key: {1}, Index: {2}", PropToString(signature), PropToString(key), index.ToString()));
#endif
			if (signature != PSConstants.kPhotoshopSignature)
				return PSError.errPlugInHostInsufficient;

			switch (key)
			{
				case PSProperties.propBigNudgeH:
				case PSProperties.propBigNudgeV:
				case PSProperties.propCaption:
				case PSProperties.propCopyright:
				case PSProperties.propGridMajor:
				case PSProperties.propGridMinor:
				case PSProperties.propRulerOriginH:
				case PSProperties.propRulerOriginV:
				case PSProperties.propURL:
				case PSProperties.propWatchSuspension:
					break;
				default:
					return PSError.errPlugInPropertyUndefined;
			}

			return PSError.noErr;
		}

		private short ResourceAddProc(uint ofType, IntPtr data)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, PropToString(ofType));
#endif
			short count = ResourceCountProc(ofType);

			int size = HandleGetSizeProc(data);
			byte[] bytes = new byte[size];

			Marshal.Copy(HandleLockProc(data, 0), bytes, 0, size);
			HandleUnlockProc(data);

			pseudoResources.Add(new PSResource(ofType, count, bytes));

			return PSError.noErr;
		}

		private short ResourceCountProc(uint ofType)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, PropToString(ofType));
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
			Ping(DebugFlags.MiscCallbacks, string.Format("{0}, {1}", PropToString(ofType), index));
#endif
			PSResource res = pseudoResources.Find(delegate(PSResource r)
			{
				return r.Equals(ofType, index);
			});
			if (res != null)
			{
				pseudoResources.Remove(res);
			}
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
		private IntPtr ResourceGetProc(uint ofType, short index)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Format("{0}, {1}", PropToString(ofType), index));
#endif
			int length = pseudoResources.Count;

			PSResource res = pseudoResources.Find(delegate(PSResource r)
			{
				return r.Equals(ofType, index);
			});

			if (res != null)
			{
				byte[] data = res.GetData();

				IntPtr h = HandleNewProc(data.Length);
				Marshal.Copy(data, 0, HandleLockProc(h, 0), data.Length);
				HandleUnlockProc(h);

				return h;
			}

			return IntPtr.Zero;
		}

#if PICASUITES
		private Dictionary<string, IntPtr> activePICASuites = new Dictionary<string, IntPtr>();

		private unsafe int SPBasicAcquireSuite(IntPtr name, int version, ref IntPtr suite)
		{

			string suiteName = Marshal.PtrToStringAnsi(name);
#if DEBUG
			Ping(DebugFlags.SPBasicSuite, string.Format("name={0}, version={1}", suiteName, version.ToString()));
#endif
			
			string suiteKey = string.Format(CultureInfo.InvariantCulture, "{0},{1}", suiteName, version.ToString(CultureInfo.InvariantCulture));
			try
			{
				if (suiteName == PSConstants.PICABufferSuite)
				{
					if (version > 1)
					{
						return PSError.errPlugInHostInsufficient;
					}

					suite = Memory.Allocate(Marshal.SizeOf(typeof(PSBufferSuite1)), false);


					activePICASuites.AddOrUpdate(suiteKey, suite);

					PSBufferSuite1 bufferSuite = PICASuites.CreateBufferSuite1();

					Marshal.StructureToPtr(bufferSuite, suite, false);
				}
				else if (suiteName == PSConstants.PICAColorSpaceSuite)
				{
					if (version > 1)
					{
						return PSError.errPlugInHostInsufficient;
					}


					suite = Memory.Allocate(Marshal.SizeOf(typeof(PSColorSpaceSuite1)), false);

					activePICASuites.AddOrUpdate(suiteKey, suite);

					PSColorSpaceSuite1 csSuite = PICASuites.CreateColorSpaceSuite1();

					Marshal.StructureToPtr(csSuite, suite, false);
				}
				else if (suiteName == PSConstants.PICAHandleSuite)
				{
					if (version > 2)
					{
						return PSError.errPlugInHostInsufficient;
					}

					if (version == 1)
					{
						suite = Memory.Allocate(Marshal.SizeOf(typeof(PSHandleSuite1)), false);

						activePICASuites.AddOrUpdate(suiteKey, suite);

						PSHandleSuite1 handleSuite = PICASuites.CreateHandleSuite1((HandleProcs*)handleProcsPtr.ToPointer());

						Marshal.StructureToPtr(handleSuite, suite, false);
					}
					else
					{
						suite = Memory.Allocate(Marshal.SizeOf(typeof(PSHandleSuite2)), false);

						activePICASuites.AddOrUpdate(suiteKey, suite);

						PSHandleSuite2 handleSuite = PICASuites.CreateHandleSuite2((HandleProcs*)handleProcsPtr.ToPointer());

						Marshal.StructureToPtr(handleSuite, suite, false);
					}
				}
				else if (suiteName == PSConstants.PICAPropertySuite)
				{
					if (version > PSConstants.kCurrentPropertyProcsVersion)
					{
						return PSError.errPlugInHostInsufficient;
					}

					suite = Memory.Allocate(Marshal.SizeOf(typeof(PropertyProcs)), false);

					activePICASuites.Add(suiteKey, suite);

					PropertyProcs propertySuite = PICASuites.CreatePropertySuite((PropertyProcs*)propertyProcsPtr.ToPointer());

					Marshal.StructureToPtr(propertySuite, suite, false);
				}
				else if (suiteName == PSConstants.PICAUIHooksSuite)
				{
					if (version > 1)
					{
						return PSError.errPlugInHostInsufficient;
					}

					suite = Memory.Allocate(Marshal.SizeOf(typeof(PSUIHooksSuite1)), false);
					activePICASuites.Add(suiteKey, suite);

					PSUIHooksSuite1 uiHooks = PICASuites.CreateUIHooksSuite1((FilterRecord*)filterRecordPtr.ToPointer());

					Marshal.StructureToPtr(uiHooks, suite, false);
				}
				else if (suiteName == PSConstants.PICAPluginsSuite)
				{
					if (version > 4)
					{
						return PSError.errPlugInHostInsufficient;
					}

					suite = Memory.Allocate(Marshal.SizeOf(typeof(SPPlugs)), false);
					activePICASuites.Add(suiteKey, suite);

					SPPlugs plugs = PICASuites.CreateSPPlugs4();

					Marshal.StructureToPtr(plugs, suite, false);
				}
				else
				{
					return PSError.errPlugInHostInsufficient;
				}
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.noErr;
		}

		private int SPBasicReleaseSuite(IntPtr name, int version)
		{
			string suiteName = Marshal.PtrToStringAnsi(name);

			string suiteKey = string.Format(CultureInfo.InvariantCulture, "{0},{1}", suiteName, version.ToString(CultureInfo.InvariantCulture));

			if (activePICASuites.ContainsKey(suiteKey))
			{
				Memory.Free(activePICASuites[suiteKey]);
				activePICASuites.Remove(suiteKey);
			}

			return PSError.noErr;
		}

		private unsafe int SPBasicIsEqual(IntPtr token1, IntPtr token2)
		{

			// compare two null-terminated strings for equality.
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
			try
			{
				block = Memory.Allocate(size, false);
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.noErr;
		}

		private int SPBasicFreeBlock(IntPtr block)
		{
			Memory.Free(block);
			return PSError.noErr;
		}

		private int SPBasicReallocateBlock(IntPtr block, int newSize, ref IntPtr newblock)
		{
			try
			{
				newblock = Memory.ReAlloc(block, newSize);
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			} 
			
			return PSError.noErr;
		}

		private int SPBasicUndefined()
		{
			return PSError.noErr;
		}

#endif

		/// <summary>
		/// Converts an Int32 to Photoshop's 'Fixed' type.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>The converted value</returns>
		private static int int2fixed(int value)
		{
			return (value << 16);
		}

		/// <summary>
		/// Converts Photoshop's 'Fixed' type to an Int32.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>The converted value</returns>
		private static int fixed2int(int value)
		{
			return (value >> 16);
		}

		private bool sizesSetup;
		/// <summary>
		/// Setup the FilterRecord image size data.
		/// </summary>
		private unsafe void SetupSizes()
		{
			if (sizesSetup)
				return;

			sizesSetup = true;

			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			short width = (short)source.Width;
			short height = (short)source.Height;

			filterRecord->imageSize.h = width;
			filterRecord->imageSize.v = height;

			switch (imageMode)
			{
				case ImageModes.plugInModeGrayScale:
				case ImageModes.plugInModeGray16:
					filterRecord->planes = 1; 
					break;
				default:

					filterRecord->planes = ignoreAlpha ? (short)3 :(short)4;

					break;
			}


			filterRecord->floatCoord.h = 0;
			filterRecord->floatCoord.v = 0;
			filterRecord->filterRect.left = 0;
			filterRecord->filterRect.top = 0;
			filterRecord->filterRect.right = width;
			filterRecord->filterRect.bottom = height;

			filterRecord->imageHRes = int2fixed((int)(dpiX + 0.5));
			filterRecord->imageVRes = int2fixed((int)(dpiY + 0.5));

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

#if PICASUITES
			spAcquireSuite = new SPBasicSuite_AcquireSuite(SPBasicAcquireSuite);
			spReleaseSuite = new SPBasicSuite_ReleaseSuite(SPBasicReleaseSuite);
			spIsEqual = new SPBasicSuite_IsEqual(SPBasicIsEqual);
			spAllocateBlock = new SPBasicSuite_AllocateBlock(SPBasicAllocateBlock);
			spFreeBlock = new SPBasicSuite_FreeBlock(SPBasicFreeBlock);
			spReallocateBlock = new SPBasicSuite_ReallocateBlock(SPBasicReallocateBlock);
			spUndefined = new SPBasicSuite_Undefined(SPBasicUndefined);
#endif
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
				descriptorParameters->descriptor = HandleNewProc(1);
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

#if PICASUITES
			basicSuitePtr = Memory.Allocate(Marshal.SizeOf(typeof(SPBasicSuite)), true);
			SPBasicSuite* basicSuite = (SPBasicSuite*)basicSuitePtr.ToPointer();
			basicSuite->acquireSuite = Marshal.GetFunctionPointerForDelegate(spAcquireSuite);
			basicSuite->releaseSuite = Marshal.GetFunctionPointerForDelegate(spReleaseSuite);
			basicSuite->isEqual = Marshal.GetFunctionPointerForDelegate(spIsEqual);
			basicSuite->allocateBlock = Marshal.GetFunctionPointerForDelegate(spAllocateBlock);
			basicSuite->freeBlock = Marshal.GetFunctionPointerForDelegate(spFreeBlock);
			basicSuite->reallocateBlock = Marshal.GetFunctionPointerForDelegate(spReallocateBlock);
			basicSuite->undefined = Marshal.GetFunctionPointerForDelegate(spUndefined);
#endif
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

			filterRecord->background.red = (ushort)((secondaryColor[0] * 65535) / 255);
			filterRecord->background.green = (ushort)((secondaryColor[1] * 65535) / 255);
			filterRecord->background.blue = (ushort)((secondaryColor[2] * 65535) / 255);

			for (int i = 0; i < 4; i++)
			{
				filterRecord->backColor[i] = secondaryColor[i];
			}

			filterRecord->foreground.red = (ushort)((primaryColor[0] * 65535) / 255);
			filterRecord->foreground.green = (ushort)((primaryColor[1] * 65535) / 255);
			filterRecord->foreground.blue = (ushort)((primaryColor[2] * 65535) / 255);

			for (int i = 0; i < 4; i++)
			{
				filterRecord->foreColor[i] = primaryColor[i];
			}

			filterRecord->bufferSpace = BufferSpaceProc();
			filterRecord->maxSpace = 1000000000;

			filterRecord->hostSig = BitConverter.ToUInt32(Encoding.ASCII.GetBytes("PSFH"), 0);
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
			filterRecord->wantLayout = 0;
			filterRecord->filterCase = filterCase;
			filterRecord->dummyPlaneValue = -1;
			/* premiereHook */
			filterRecord->advanceState = Marshal.GetFunctionPointerForDelegate(advanceProc);

			filterRecord->supportsAbsolute = 1;
			filterRecord->wantsAbsolute = 0;
			filterRecord->getPropertyObsolete = Marshal.GetFunctionPointerForDelegate(getPropertyProc);
			/* cannotUndo */
			filterRecord->supportsPadding = 1;
			/* inputPadding */
			/* outputPadding */
			/* maskPadding */
			filterRecord->samplingSupport = PSConstants.SamplingSupport.hostSupportsIntegralSampling;
			/* reservedByte */
			/* inputRate */
			/* maskRate */
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
			
			errorStringPtr = Memory.Allocate(256, true);
			filterRecord->errorString = errorStringPtr; // some filters trash the filterRecord->errorString pointer so the errorStringPtr value is used instead. 
			
			filterRecord->channelPortProcs = channelPortsPtr;
			filterRecord->documentInfo = readDocumentPtr;
			// New in 5.0
#if PICASUITES
			filterRecord->sSPBasic = basicSuitePtr;
			filterRecord->plugInRef = new IntPtr(1);
#else
			filterRecord->sSPBasic = IntPtr.Zero;
			filterRecord->plugInRef = IntPtr.Zero;
#endif
			switch (imageMode)
			{
				case ImageModes.plugInModeGrayScale:
				case ImageModes.plugInModeRGBColor:
					filterRecord->depth = 8;
					break;
		  
				case ImageModes.plugInModeGray16:
				case ImageModes.plugInModeRGB48:
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

		private bool disposed;
		private unsafe void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
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

#if PICASUITES
				if (basicSuitePtr != IntPtr.Zero)
				{
					Memory.Free(basicSuitePtr);
					basicSuitePtr = IntPtr.Zero;
				}

				if (activePICASuites != null)
				{
					// free any remaining suites
					foreach (var item in activePICASuites)
					{
						Memory.Free(item.Value);
					}
					activePICASuites = null;
				}
#endif

				if (filterRecordPtr != IntPtr.Zero)
				{
					FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

					if (filterRecord->parameters != IntPtr.Zero)
					{
						if (handle_valid(filterRecord->parameters))
						{
							HandleUnlockProc(filterRecord->parameters);
							HandleDisposeProc(filterRecord->parameters);
						}
						else
						{
							SafeNativeMethods.GlobalUnlock(filterRecord->parameters);
							SafeNativeMethods.GlobalFree(filterRecord->parameters);
						}
						filterRecord->parameters = IntPtr.Zero;
					}


					Memory.Free(filterRecordPtr);
					filterRecordPtr = IntPtr.Zero;
				}

				if (parmDataHandle != IntPtr.Zero)
				{

					try
					{
						SafeNativeMethods.GlobalUnlock(parmDataHandle);
						SafeNativeMethods.GlobalFree(parmDataHandle);
					}
					finally
					{
						parmDataHandle = IntPtr.Zero;
					}
				}

				if (dataPtr != IntPtr.Zero)
				{
					if (handle_valid(dataPtr))
					{
						HandleUnlockProc(dataPtr);
						HandleDisposeProc(dataPtr);
					}
					else if (SafeNativeMethods.GlobalSize(dataPtr).ToInt64() > 0L)
					{
						SafeNativeMethods.GlobalUnlock(dataPtr);
						SafeNativeMethods.GlobalFree(dataPtr);
					}
					dataPtr = IntPtr.Zero;
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

				disposed = true;

			}
		}

		#endregion
	}
}
