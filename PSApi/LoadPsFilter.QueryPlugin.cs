﻿/////////////////////////////////////////////////////////////////////////////////
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
using System.Globalization;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.PSApi
{
	internal partial class LoadPsFilter
	{
		/// <summary>
		/// Reads a Pascal String into a string.
		/// </summary>
		/// <param name="ptr">The pointer to read from.</param>
		/// <param name="length">The length of the resulting Pascal String.</param>
		/// <returns>The resulting string</returns>
		private static unsafe string StringFromPString(byte* ptr, out int length)
		{
			length = (int)ptr[0] + 1; // skip the first byte

			return new string((sbyte*)ptr, 1, ptr[0], Windows1252Encoding);
		}

		private sealed class QueryAETE
		{
			public readonly IntPtr resourceID;
			public PluginAETE enumAETE;

			public QueryAETE(int terminologyID)
			{
				this.resourceID = new IntPtr(terminologyID);
				this.enumAETE = null;
			}
		}

		private sealed class QueryFilter
		{
			public readonly string fileName;
			public List<PluginData> plugins;

			public QueryFilter(string fileName)
			{
				this.fileName = fileName;
				this.plugins = new List<PluginData>();
			}
		}

		private static unsafe bool EnumAETE(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam)
		{
			GCHandle handle = GCHandle.FromIntPtr(lParam);
			QueryAETE query = (QueryAETE)handle.Target;
			if (lpszName == query.resourceID)
			{
				IntPtr hRes = UnsafeNativeMethods.FindResourceW(hModule, lpszName, lpszType);
				if (hRes == IntPtr.Zero)
				{
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

				byte* ptr = (byte*)lockRes.ToPointer() + 2;
				short version = *(short*)ptr;
				ptr += 2;

				int major = (version & 0xff);
				int minor = ((version >> 8) & 0xff);

				short lang = *(short*)ptr;
				ptr += 2;
				short script = *(short*)ptr;
				ptr += 2;
				short suiteCount = *(short*)ptr;
				ptr += 2;
				byte* propPtr = ptr;

				int stringLength = 0;

				if (suiteCount == 1) // There should only be one scripting event
				{
					string vend = StringFromPString(propPtr, out stringLength);
					propPtr += stringLength;
					string desc = StringFromPString(propPtr, out stringLength);
					propPtr += stringLength;
					uint suiteID = *(uint*)propPtr;
					propPtr += 4;
					short suiteLevel = *(short*)propPtr;
					propPtr += 2;
					short suiteVersion = *(short*)propPtr;
					propPtr += 2;
					short eventCount = *(short*)propPtr;
					propPtr += 2;

					if (eventCount == 1) // There should only be one vendor suite
					{
						string vend2 = StringFromPString(propPtr, out stringLength);
						propPtr += stringLength;
						string desc2 = StringFromPString(propPtr, out stringLength);
						propPtr += stringLength;
						int eventClass = *(int*)propPtr;
						propPtr += 4;
						int eventType = *(int*)propPtr;
						propPtr += 4;

						uint replyType = *(uint*)propPtr;
						propPtr += 7;
						byte[] bytes = new byte[4];

						int idx = 0;
						while (*propPtr != 0)
						{
							if (*propPtr != 0x27) // The ' char, some filters encode the #ImR parameter type as '#'ImR.
							{
								bytes[idx] = *propPtr;
								idx++;
							}
							propPtr++;
						}
						propPtr++; // skip the second null byte

						uint paramType = BitConverter.ToUInt32(bytes, 0);

						short flags = *(short*)propPtr;
						propPtr += 2;
						short paramCount = *(short*)propPtr;
						propPtr += 2;

						AETEEvent evnt = new AETEEvent()
						{
							vendor = vend2,
							desc = desc2,
							eventClass = eventClass,
							type = eventType,
							replyType = replyType,
							paramType = paramType,
							flags = flags
						};

						if (paramCount > 0)
						{
							AETEParameter[] parameters = new AETEParameter[paramCount];
							for (int p = 0; p < paramCount; p++)
							{
								parameters[p] = new AETEParameter();
								parameters[p].name = StringFromPString(propPtr, out stringLength);
								propPtr += stringLength;

								parameters[p].key = *(uint*)propPtr;
								propPtr += 4;

								parameters[p].type = *(uint*)propPtr;
								propPtr += 4;

								parameters[p].desc = StringFromPString(propPtr, out stringLength);
								propPtr += stringLength;

								parameters[p].flags = *(short*)propPtr;
								propPtr += 2;
							}
							evnt.parameters = parameters;
						}

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
										en.enums[e] = new AETEEnum();
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

						if (evnt.parameters != null &&
							major == PSConstants.AETEMajorVersion &&
							minor == PSConstants.AETEMinorVersion &&
							suiteLevel == PSConstants.AETESuiteLevel &&
							suiteVersion == PSConstants.AETESuiteVersion)
						{
							query.enumAETE = new PluginAETE(major, minor, suiteLevel, suiteVersion, evnt);
							handle.Target = query;
							lParam = GCHandle.ToIntPtr(handle);
						}
					}
				}

				return false;
			}

			return true;
		}

		private static unsafe bool EnumPiPL(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam)
		{
			GCHandle handle = GCHandle.FromIntPtr(lParam);
			QueryFilter query = (QueryFilter)handle.Target;

			IntPtr hRes = UnsafeNativeMethods.FindResourceW(hModule, lpszName, lpszType);
			if (hRes == IntPtr.Zero)
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("FindResource failed for PiPL in {0}", query.fileName));
#endif
				return true;
			}

			IntPtr loadRes = UnsafeNativeMethods.LoadResource(hModule, hRes);
			if (loadRes == IntPtr.Zero)
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("LoadResource failed for PiPL in {0}", query.fileName));
#endif
				return true;
			}

			IntPtr lockRes = UnsafeNativeMethods.LockResource(loadRes);
			if (lockRes == IntPtr.Zero)
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("LockResource failed for PiPL in {0}", query.fileName));
#endif

				return true;
			}

#if DEBUG
			short fb = Marshal.ReadInt16(lockRes); // PiPL Resources always start with 1, this seems to be Photoshop's signature.
#endif
			int version = Marshal.ReadInt32(lockRes, 2);

			if (version != PSConstants.latestPIPLVersion)
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("Invalid PiPL version in {0}: {1}, Expected version {2}", query.fileName, version, PSConstants.latestPIPLVersion));
#endif
				return true;
			}

			int count = Marshal.ReadInt32(lockRes, 6);

			byte* propPtr = (byte*)lockRes.ToPointer() + 10L;

			PluginData enumData = new PluginData(query.fileName);

			uint platformEntryPoint = IntPtr.Size == 8 ? PIPropertyID.PIWin64X86CodeProperty : PIPropertyID.PIWin32X86CodeProperty;

			for (int i = 0; i < count; i++)
			{
				PIProperty* pipp = (PIProperty*)propPtr;
				uint propKey = pipp->propertyKey;
#if DEBUG
				Ping(DebugFlags.PiPL, string.Format("prop {0}: {1}", i, PropToString(propKey)));
#endif
				byte* dataPtr = propPtr + PIProperty.SizeOf;
				if (propKey == PIPropertyID.PIKindProperty)
				{
					if (*((uint*)dataPtr) != PSConstants.filterKind)
					{
#if DEBUG
						System.Diagnostics.Debug.WriteLine(string.Format("{0} is not a valid Photoshop filter.", query.fileName));
#endif
						return true;
					}
				}
				else if (propKey == platformEntryPoint)
				{
					enumData.EntryPoint = Marshal.PtrToStringAnsi((IntPtr)dataPtr, pipp->propertyLength).TrimEnd('\0');
				}
				else if (propKey == PIPropertyID.PIVersionProperty)
				{
					short* filterVersion = (short*)dataPtr;
					if (filterVersion[1] > PSConstants.latestFilterVersion ||
						(filterVersion[1] == PSConstants.latestFilterVersion && filterVersion[0] > PSConstants.latestFilterSubVersion))
					{
#if DEBUG
						System.Diagnostics.Debug.WriteLine(string.Format("{0} requires newer filter interface version {1}.{2} and only version {3}.{4} is supported",
							new object[] { query.fileName, filterVersion[1], filterVersion[0], PSConstants.latestFilterVersion, PSConstants.latestFilterSubVersion }));
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
						System.Diagnostics.Debug.WriteLine(string.Format("{0} does not support the plugInModeRGBColor or plugInModeGrayScale image modes.", query.fileName));
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
						dataPtr += FilterCaseInfo.SizeOf;
					}
					enumData.FilterInfo = filterInfo;
				}
				else if (propKey == PIPropertyID.PIHasTerminologyProperty)
				{
					PITerminology* term = (PITerminology*)dataPtr;
					if (term->version == PSConstants.LatestTerminologyVersion)
					{
#if DEBUG
						string aeteName = Marshal.PtrToStringAnsi(new IntPtr(dataPtr + PITerminology.SizeOf)).TrimEnd('\0');
#endif
						QueryAETE queryAETE = new QueryAETE(term->terminologyID);

						GCHandle aeteHandle = GCHandle.Alloc(queryAETE, GCHandleType.Normal);
						try
						{
							IntPtr callback = GCHandle.ToIntPtr(aeteHandle);
							if (!UnsafeNativeMethods.EnumResourceNamesW(hModule, "AETE", new UnsafeNativeMethods.EnumResNameDelegate(EnumAETE), callback))
							{
								queryAETE = (QueryAETE)GCHandle.FromIntPtr(callback).Target;
							}
						}
						finally
						{
							if (aeteHandle.IsAllocated)
							{
								aeteHandle.Free();
							}
						}

						if (queryAETE.enumAETE != null)
						{
							enumData.Aete = queryAETE.enumAETE;
						}
					}
				}
				else if (propKey == PIPropertyID.EnableInfo)
				{
					enumData.enableInfo = Marshal.PtrToStringAnsi((IntPtr)dataPtr, pipp->propertyLength).TrimEnd('\0');
				}
				else if (propKey == PIPropertyID.PIRequiredHostProperty)
				{
					uint host = *(uint*)dataPtr;
					if (host != PSConstants.kPhotoshopSignature && host != PSConstants.noRequiredHost)
					{
#if DEBUG
						System.Diagnostics.Debug.WriteLine(string.Format("{0} requires host '{1}'.", query.fileName, PropToString(host)));
#endif
						return true;
					}
				}
				else if (propKey == PIPropertyID.NoAboutBox)
				{
					enumData.HasAboutBox = false;
				}

				int propertyDataPaddedLength = (pipp->propertyLength + 3) & ~3;
				propPtr += (PIProperty.SizeOf + propertyDataPaddedLength);
			}

			if (enumData.IsValid())
			{
				query.plugins.Add(enumData);
				handle.Target = query;
				lParam = GCHandle.ToIntPtr(handle);
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

			return data.Trim(TrimChars);
		}

		private static unsafe bool EnumPiMI(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam)
		{
			GCHandle handle = GCHandle.FromIntPtr(lParam);
			QueryFilter query = (QueryFilter)handle.Target;

			IntPtr hRes = UnsafeNativeMethods.FindResourceW(hModule, lpszName, lpszType);
			if (hRes == IntPtr.Zero)
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("FindResource failed for PiMI in {0}", query.fileName));
#endif
				return true;
			}

			IntPtr loadRes = UnsafeNativeMethods.LoadResource(hModule, hRes);
			if (loadRes == IntPtr.Zero)
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("LoadResource failed for PiMI in {0}", query.fileName));
#endif
				return true;
			}

			IntPtr lockRes = UnsafeNativeMethods.LockResource(loadRes);
			if (lockRes == IntPtr.Zero)
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("LockResource failed for PiMI in {0}", query.fileName));
#endif
				return true;
			}
			int length = 0;
			byte* ptr = (byte*)lockRes.ToPointer() + 2L;

			PluginData enumData = new PluginData(query.fileName);

			enumData.Category = StringFromCString((IntPtr)ptr, out length);

			ptr += length;

			if (string.IsNullOrEmpty(enumData.Category))
			{
				enumData.Category = PSFilterHostDll.Properties.Resources.PiMIDefaultCategoryName;
			}

			PlugInInfo* info = (PlugInInfo*)ptr;

			if (info->version > PSConstants.latestFilterVersion ||
			   (info->version == PSConstants.latestFilterVersion && info->subVersion > PSConstants.latestFilterSubVersion))
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("{0} requires newer filter interface version {1}.{2} and only version {3}.{4} is supported",
					new object[] { query.fileName, info->version, info->subVersion, PSConstants.latestFilterVersion, PSConstants.latestFilterSubVersion }));
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
				System.Diagnostics.Debug.WriteLine(string.Format("{0} does not support the plugInModeRGBColor or plugInModeGrayScale image modes.", query.fileName));
#endif
				return true;
			}

			// add the supported modes to the plug-in data as it can be used later to disable filters that do not support the image type.
			enumData.supportedModes = 0;
			if ((info->supportsMode & PSConstants.supportsGrayScale) == PSConstants.supportsGrayScale)
			{
				enumData.supportedModes |= PSConstants.flagSupportsGrayScale;
			}

			if ((info->supportsMode & PSConstants.supportsRGBColor) == PSConstants.supportsRGBColor)
			{
				enumData.supportedModes |= PSConstants.flagSupportsRGBColor;
			}
#endif

			if (info->requireHost != PSConstants.kPhotoshopSignature && info->requireHost != PSConstants.noRequiredHost)
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("{0} requires host '{1}'.", query.fileName, PropToString(info->requireHost)));
#endif
				return true;
			}

			IntPtr filterRes = IntPtr.Zero;

			IntPtr type = Marshal.StringToHGlobalUni("_8BFM");
			try
			{
				// Load the _8BFM resource to get the filter title.
				filterRes = UnsafeNativeMethods.FindResourceW(hModule, lpszName, type);
			}
			finally
			{
				Marshal.FreeHGlobal(type);
			}

			if (filterRes == IntPtr.Zero)
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("FindResource failed for _8BFM in {0}", query.fileName));
#endif
				return true;
			}

			IntPtr filterLoad = UnsafeNativeMethods.LoadResource(hModule, filterRes);

			if (filterLoad == IntPtr.Zero)
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("LoadResource failed for _8BFM in {0}", query.fileName));
#endif
				return true;
			}

			IntPtr filterLock = UnsafeNativeMethods.LockResource(filterLoad);

			if (filterLock == IntPtr.Zero)
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("LockResource failed for _8BFM in {0}", query.fileName));
#endif
				return true;
			}

			IntPtr resPtr = new IntPtr(filterLock.ToInt64() + 2L);

			enumData.Title = StringFromCString(resPtr, out length);

			// The entry point number is the same as the resource number.
			enumData.EntryPoint = "ENTRYPOINT" + lpszName.ToInt32().ToString(CultureInfo.InvariantCulture);
			enumData.FilterInfo = null;

			if (enumData.IsValid())
			{
				query.plugins.Add(enumData);
				handle.Target = query;
				lParam = GCHandle.ToIntPtr(handle);
			}

			return true;
		}

		/// <summary>
		/// Queries an 8bf plug-in
		/// </summary>
		/// <param name="pluginFileName">The fileName to query.</param>
		/// <returns>
		/// An enumerable collection containing the filters within the plug-in.
		/// </returns>
		internal static IEnumerable<PluginData> QueryPlugin(string pluginFileName)
		{
			if (pluginFileName == null)
			{
				throw new ArgumentNullException("pluginFileName");
			}

			if (!PEFile.CheckProcessorArchitecture(pluginFileName))
			{
				return System.Linq.Enumerable.Empty<PluginData>();
			}

			List<PluginData> pluginData = new List<PluginData>();

#if DEBUG
			debugFlags |= DebugFlags.PiPL;
#endif
			SafeLibraryHandle dll = UnsafeNativeMethods.LoadLibraryExW(pluginFileName, IntPtr.Zero, NativeConstants.LOAD_LIBRARY_AS_DATAFILE);
			try
			{
				if (!dll.IsInvalid)
				{
					QueryFilter queryFilter = new QueryFilter(pluginFileName);

					GCHandle handle = GCHandle.Alloc(queryFilter, GCHandleType.Normal);
					bool needsRelease = false;
					System.Runtime.CompilerServices.RuntimeHelpers.PrepareConstrainedRegions();
					try
					{

						dll.DangerousAddRef(ref needsRelease);
						IntPtr callback = GCHandle.ToIntPtr(handle);
						if (UnsafeNativeMethods.EnumResourceNamesW(dll.DangerousGetHandle(), "PiPl", new UnsafeNativeMethods.EnumResNameDelegate(EnumPiPL), callback))
						{
							queryFilter = (QueryFilter)GCHandle.FromIntPtr(callback).Target;

							pluginData.AddRange(queryFilter.plugins);
						}
						else if (UnsafeNativeMethods.EnumResourceNamesW(dll.DangerousGetHandle(), "PiMI", new UnsafeNativeMethods.EnumResNameDelegate(EnumPiMI), callback))
						{
							// If there are no PiPL resources scan for Photoshop 2.5's PiMI resources.
							queryFilter = (QueryFilter)GCHandle.FromIntPtr(callback).Target;

							pluginData.AddRange(queryFilter.plugins);
						}
#if DEBUG
						else
						{
							Ping(DebugFlags.Error, string.Format("EnumResourceNames(PiPL, PiMI) failed for {0}", pluginFileName));
						}
#endif

					}
					finally
					{
						if (handle.IsAllocated)
						{
							handle.Free();
						}

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

			if (pluginData.Count > 1)
			{
				/* If the DLL contains more than one filter, add a list of all the entry points to each individual filter. 
				 * Per the SDK only one entry point in a module will display the about box the rest are dummy calls so we must call all of them. 
				 */
				string[] entryPoints = new string[pluginData.Count];

				for (int i = 0; i < entryPoints.Length; i++)
				{
					entryPoints[i] = pluginData[i].EntryPoint;
				}

				for (int i = 0; i < entryPoints.Length; i++)
				{
					pluginData[i].moduleEntryPoints = entryPoints;
				}
			}

			return pluginData;
		}
	}
}