/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2014 Nicholas Hayes
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

		private struct QueryAETE
		{
			public IntPtr resourceID;
			public PluginAETE enumAETE;
		}

		private struct QueryFilter
		{
			public string fileName;
			public List<PluginData> plugins;
		}

		private static unsafe bool EnumAETE(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam)
		{
			GCHandle handle = GCHandle.FromIntPtr(lParam);
			QueryAETE query = (QueryAETE)handle.Target;
			if (lpszName == query.resourceID) // is the resource id the one we want
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

				query.enumAETE = new PluginAETE();

				byte* ptr = (byte*)lockRes.ToPointer() + 2;
				short version = *(short*)ptr;
				ptr += 2;

				query.enumAETE.major = (version & 0xff);
				query.enumAETE.minor = ((version >> 8) & 0xff);

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
					query.enumAETE.suiteLevel = *(short*)propPtr;
					propPtr += 2;
					query.enumAETE.suiteVersion = *(short*)propPtr;
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
						query.enumAETE.scriptEvent = evnt;

					}

				}

				if ((query.enumAETE.scriptEvent != null) && query.enumAETE.scriptEvent.parameters != null)
				{
					handle.Target = query;
					lParam = GCHandle.ToIntPtr(handle);
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
				System.Diagnostics.Debug.WriteLine(string.Format("Invalid PiPL version in {0}: {1},  Expected version {2}", query.fileName, version, PSConstants.latestPIPLVersion));
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
						System.Diagnostics.Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} requires newer filter interface version {1}.{2} and only version {3}.{4} is supported", new object[] { query.fileName, filterVersion[1], filterVersion[0], PSConstants.latestFilterVersion, PSConstants.latestFilterSubVersion }));
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
						dataPtr += 4;
					}
					enumData.FilterInfo = filterInfo;
				}
				else if (propKey == PIPropertyID.PIHasTerminologyProperty)
				{
					PITerminology* term = (PITerminology*)dataPtr;
#if DEBUG
					string aeteName = Marshal.PtrToStringAnsi(new IntPtr(dataPtr + PITerminology.SizeOf)).TrimEnd('\0');
#endif
					QueryAETE queryAETE = new QueryAETE()
					{
						resourceID = new IntPtr(term->terminologyID),
						enumAETE = null
					};

					GCHandle aeteHandle = GCHandle.Alloc(queryAETE, GCHandleType.Normal);
					try
					{
						IntPtr callback = GCHandle.ToIntPtr(aeteHandle);
						while (UnsafeNativeMethods.EnumResourceNamesW(hModule, "AETE", new UnsafeNativeMethods.EnumResNameDelegate(EnumAETE), callback))
						{
							// do nothing
						}
						queryAETE = (QueryAETE)GCHandle.FromIntPtr(callback).Target;

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
						if (queryAETE.enumAETE.IsValid())
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
				query.plugins.Add(enumData); // add each plug-in found in the file to the query list
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
				enumData.Category = Resources.PiMIDefaultCategoryName;
			}

			PlugInInfo* info = (PlugInInfo*)ptr;

			if (info->version > PSConstants.latestFilterVersion ||
			   (info->version == PSConstants.latestFilterVersion && info->subVersion > PSConstants.latestFilterSubVersion))
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("{0} requires newer filter interface version {1}.{2} and only version {3}.{4} is supported", new object[] { query.fileName, info->version.ToString(CultureInfo.CurrentCulture), info->subVersion.ToString(CultureInfo.CurrentCulture), PSConstants.latestFilterVersion.ToString(CultureInfo.CurrentCulture), PSConstants.latestFilterSubVersion.ToString(CultureInfo.CurrentCulture) }));
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
				filterRes = UnsafeNativeMethods.FindResourceW(hModule, lpszName, type); // load the _8BFM resource to get the filter title
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

			// the entry point number is the same as the resource number
			enumData.EntryPoint = "ENTRYPOINT" + lpszName.ToInt32().ToString(CultureInfo.InvariantCulture);
			enumData.FilterInfo = null;

			if (enumData.IsValid())
			{
				query.plugins.Add(enumData); // add each plug-in found in the file to the query list
				handle.Target = query;
				lParam = GCHandle.ToIntPtr(handle);
			}

			return true;
		}

		#endregion

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
					QueryFilter queryFilter = new QueryFilter()
					{
						fileName = pluginFileName,
						plugins = new List<PluginData>()
					};

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
		}

		private Dictionary<IntPtr, PSHandle> handles;

		private struct ChannelDescPtrs
		{
			public IntPtr address;
			public IntPtr name;
		}

		private List<ChannelDescPtrs> channelReadDescPtrs;

		#region CallbackDelegates
		private static AdvanceStateProc advanceProc;
		// BufferProcs
		private static AllocateBufferProc allocProc;
		private static FreeBufferProc freeProc;
		private static LockBufferProc lockProc;
		private static UnlockBufferProc unlockProc;
		private static BufferSpaceProc spaceProc;
		// MiscCallbacks
		private static ColorServicesProc colorProc;
		private static DisplayPixelsProc displayPixelsProc;
		private static HostProcs hostProc;
		private static ProcessEventProc processEventProc;
		private static ProgressProc progressProc;
		private static TestAbortProc abortProc;
		// HandleProcs 
		private static NewPIHandleProc handleNewProc;
		private static DisposePIHandleProc handleDisposeProc;
		private static GetPIHandleSizeProc handleGetSizeProc;
		private static SetPIHandleSizeProc handleSetSizeProc;
		private static LockPIHandleProc handleLockProc;
		private static UnlockPIHandleProc handleUnlockProc;
		private static RecoverSpaceProc handleRecoverSpaceProc;
		private static DisposeRegularPIHandleProc handleDisposeRegularProc;
		// ImageServicesProc
#if USEIMAGESERVICES
		private static PIResampleProc resample1DProc;
		private static PIResampleProc resample2DProc;
#endif
		// ChannelPorts
		private static ReadPixelsProc readPixelsProc;
		private static WriteBasePixelsProc writeBasePixelsProc;
		private static ReadPortForWritePortProc readPortForWritePortProc;
		// PropertyProcs
		private static GetPropertyProc getPropertyProc;
		private static SetPropertyProc setPropertyProc;
		// ResourceProcs
		private static CountPIResourcesProc countResourceProc;
		private static GetPIResourceProc getResourceProc;
		private static DeletePIResourceProc deleteResourceProc;
		private static AddPIResourceProc addResourceProc;

		// ReadDescriptorProcs
		private static OpenReadDescriptorProc openReadDescriptorProc;
		private static CloseReadDescriptorProc closeReadDescriptorProc;
		private static GetKeyProc getKeyProc;
		private static GetIntegerProc getIntegerProc;
		private static GetFloatProc getFloatProc;
		private static GetUnitFloatProc getUnitFloatProc;
		private static GetBooleanProc getBooleanProc;
		private static GetTextProc getTextProc;
		private static GetAliasProc getAliasProc;
		private static GetEnumeratedProc getEnumeratedProc;
		private static GetClassProc getClassProc;
		private static GetSimpleReferenceProc getSimpleReferenceProc;
		private static GetObjectProc getObjectProc;
		private static GetCountProc getCountProc;
		private static GetStringProc getStringProc;
		private static GetPinnedIntegerProc getPinnedIntegerProc;
		private static GetPinnedFloatProc getPinnedFloatProc;
		private static GetPinnedUnitFloatProc getPinnedUnitFloatProc;
		// WriteDescriptorProcs
		private static OpenWriteDescriptorProc openWriteDescriptorProc;
		private static CloseWriteDescriptorProc closeWriteDescriptorProc;
		private static PutIntegerProc putIntegerProc;
		private static PutFloatProc putFloatProc;
		private static PutUnitFloatProc putUnitFloatProc;
		private static PutBooleanProc putBooleanProc;
		private static PutTextProc putTextProc;
		private static PutAliasProc putAliasProc;
		private static PutEnumeratedProc putEnumeratedProc;
		private static PutClassProc putClassProc;
		private static PutSimpleReferenceProc putSimpleReferenceProc;
		private static PutObjectProc putObjectProc;
		private static PutCountProc putCountProc;
		private static PutStringProc putStringProc;
		private static PutScopedClassProc putScopedClassProc;
		private static PutScopedObjectProc putScopedObjectProc;
		// SPBasic
		private static SPBasicAcquireSuite spAcquireSuite;
		private static SPBasicAllocateBlock spAllocateBlock;
		private static SPBasicFreeBlock spFreeBlock;
		private static SPBasicIsEqual spIsEqual;
		private static SPBasicReallocateBlock spReallocateBlock;
		private static SPBasicReleaseSuite spReleaseSuite;
		private static SPBasicUndefined spUndefined;
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

		private static AbortFunc abortFunc;
		private static ProgressProc progressFunc;
		private static PickColor pickColor;

		private SurfaceBase source;
		private SurfaceBase dest;
		private Surface8 mask;
		private Surface32 tempDisplaySurface;
		private Surface8 tempMask;
		private SurfaceBase tempSurface;
		private Bitmap checkerBoardBitmap;

		private PluginPhase phase;

		private IntPtr dataPtr;
		private short result;
		private string errorMessage;
		private List<PSResource> pseudoResources;
		private HostInformation hostInfo;

		private short filterCase;
		private double dpiX;
		private double dpiY;
		private Region selectedRegion;
#if GDIPLUS
		private Bitmap exifBitmap;
#else
		private BitmapSource exifBitmap;
#endif
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
			   
			this.exifBitmap = (Bitmap)sourceImage.Clone();
#else
			this.dpiX = sourceImage.DpiX;
			this.dpiY = sourceImage.DpiY;

			this.exifBitmap = sourceImage.Clone();
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
		/// <returns>True if successful, otherwise false.</returns>
		/// <exception cref="System.IO.FileNotFoundException">The file in the PluginData.FileName cannot be found.</exception>
		private static bool LoadFilter(ref PluginData pdata)
		{
			bool loaded = false;

			if (!string.IsNullOrEmpty(pdata.EntryPoint))
			{
				new FileIOPermission(FileIOPermissionAccess.PathDiscovery | FileIOPermissionAccess.Read, pdata.FileName).Demand();

				pdata.module.dll = UnsafeNativeMethods.LoadLibraryExW(pdata.FileName, IntPtr.Zero, 0U);

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
		/// <param name="pdata">The PluginData to  free.</param>
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

			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			byte[] parameterDataBytes = globalParameters.GetParameterDataBytes();
			if (parameterDataBytes != null)
			{
				switch (globalParameters.ParameterDataStorageMethod)
				{
					case GlobalParameters.DataStorageMethod.HandleSuite:

						filterRecord->parameters = HandleNewProc(parameterDataBytes.Length);

						if (filterRecord->parameters != IntPtr.Zero)
						{
							Marshal.Copy(parameterDataBytes, 0, HandleLockProc(filterRecord->parameters, 0), parameterDataBytes.Length);
							HandleUnlockProc(filterRecord->parameters);
						}
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

						if (dataPtr != IntPtr.Zero)
						{
							Marshal.Copy(pluginDataBytes, 0, HandleLockProc(dataPtr, 0), pluginDataBytes.Length);
							HandleUnlockProc(dataPtr);
						}
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
						pdata.module.entryPoint(FilterSelector.About, aboutRecordHandle.AddrOfPinnedObject(), ref dataPtr, ref result);
					}
					else
					{
						// otherwise call about on all the entry points in the module, per the SDK docs only one of the entry points will display the about box.
						foreach (var entryPoint in pdata.moduleEntryPoints)
						{
							IntPtr ptr = UnsafeNativeMethods.GetProcAddress(pdata.module.dll, entryPoint);

							pluginEntryPoint ep = (pluginEntryPoint)Marshal.GetDelegateForFunctionPointer(ptr, typeof(pluginEntryPoint));

							ep(FilterSelector.About, aboutRecordHandle.AddrOfPinnedObject(), ref dataPtr, ref result);

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


			if (result != PSError.noErr)
			{
				FreeLibrary(ref pdata);

				if (result < 0 && result != PSError.userCanceledErr)
				{
					switch (result)
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
						default:
							error = string.Format(CultureInfo.CurrentCulture, Resources.UnknownErrorCodeFormat, result);
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

		private unsafe bool PluginApply(PluginData pdata)
		{
#if DEBUG
			System.Diagnostics.Debug.Assert(phase == PluginPhase.Prepare);
#endif
			result = PSError.noErr;

#if DEBUG
			Ping(DebugFlags.Call, "Before FilterSelectorStart");
#endif

			pdata.module.entryPoint(FilterSelector.Start, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
			Ping(DebugFlags.Call, "After FilterSelectorStart");
#endif

			if (result != PSError.noErr)
			{
				FreeLibrary(ref pdata);
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

				pdata.module.entryPoint(FilterSelector.Continue, filterRecordPtr, ref dataPtr, ref result);

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

					pdata.module.entryPoint(FilterSelector.Finish, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
					Ping(DebugFlags.Call, "After FilterSelectorFinish");
#endif

					FreeLibrary(ref pdata);
					errorMessage = GetErrorMessage(savedResult);

#if DEBUG
					Ping(DebugFlags.Error, string.Format("filterSelectorContinue returned: {0}({1})", string.IsNullOrEmpty(errorMessage) ? "User Canceled" : errorMessage, savedResult));
#endif
					return false;
				}

				if (AbortProc()) // Per the SDK the host can call filterSelectorFinish in between filterSelectorContinue calls if it detects a cancel request.
				{
					result = PSError.noErr;
					pdata.module.entryPoint(FilterSelector.Finish, filterRecordPtr, ref dataPtr, ref result);

					if (result != PSError.noErr)
					{
						errorMessage = GetErrorMessage(result);
					}

					FreeLibrary(ref pdata);
					return false;
				}
			}
			AdvanceStateProc();


			result = PSError.noErr;

#if DEBUG
			Ping(DebugFlags.Call, "Before FilterSelectorFinish");
#endif

			pdata.module.entryPoint(FilterSelector.Finish, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
			Ping(DebugFlags.Call, "After FilterSelectorFinish");
#endif

			if (!isRepeatEffect && result == PSError.noErr)
			{
				SaveParameters();
			}

			PostProcessOutputData();

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

			pdata.module.entryPoint(FilterSelector.Parameters, filterRecordPtr, ref dataPtr, ref result);
#if DEBUG
			unsafe
			{
				FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

				Ping(DebugFlags.Call, string.Format("data = {0:X8},  parameters = {1:X8}", dataPtr.ToInt64(), filterRecord->parameters.ToInt64()));
			}

			Ping(DebugFlags.Call, "After filterSelectorParameters");
#endif

			if (result != PSError.noErr)
			{
				FreeLibrary(ref pdata);
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

		private bool PluginPrepare(PluginData pdata)
		{
			SetupSizes();
			RestoreParameters();
			SetFilterRecordValues();


			result = PSError.noErr;


#if DEBUG
			Ping(DebugFlags.Call, "Before filterSelectorPrepare");
#endif
			pdata.module.entryPoint(FilterSelector.Prepare, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
			Ping(DebugFlags.Call, "After filterSelectorPrepare");
#endif

			if (result != PSError.noErr)
			{
				FreeLibrary(ref pdata);
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
			if (!LoadFilter(ref pdata))
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine("LoadFilter failed");
#endif
				return false;
			}

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

			FreeLibrary(ref pdata);

			return true;
		}

		internal static bool ShowAboutDialog(PluginData pdata, IntPtr owner, out string errorMessage)
		{
			errorMessage = string.Empty;

			if (!LoadFilter(ref pdata))
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine("LoadFilter failed");
#endif
				return false;
			}

			bool retVal = PluginAbout(pdata, owner, out errorMessage);


			FreeLibrary(ref pdata);


			return retVal;
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
				error = StringFromPString(this.errorStringPtr);
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
						error = Resources.FilterBadParameters;
						break;
					case PSError.filterBadMode:
						error = string.Format(CultureInfo.CurrentCulture, Resources.FilterBadModeFormat, GetImageModeString(this.imageMode));
						break;
					case PSError.errPlugInHostInsufficient:
						error = Resources.PlugInHostInsufficient;
						break;
					case PSError.errPlugInPropertyUndefined:
						error = Resources.PlugInPropertyUndefined;
						break;
					case PSError.errHostDoesNotSupportColStep:
						error = Resources.HostDoesNotSupportColStep;
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

		/// <summary>
		/// Determines whether the filter uses planar order processing.
		/// </summary>
		/// <param name="loPlane">The lo plane.</param>
		/// <param name="hiPlane">The hi plane.</param>
		/// <returns>
		///   <c>true</c> if a single plane of data is requested; otherwise, <c>false</c>.
		/// </returns>
		private static unsafe bool IsSinglePlane(short loPlane, short hiPlane)
		{
			return (((hiPlane - loPlane) + 1) == 1);
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

			if (outDataPtr != IntPtr.Zero && RectNonEmpty(lastOutRect))
			{
				StoreOutputBuffer(outDataPtr, lastOutRowBytes, lastOutRect, lastOutLoPlane, lastOutHiPlane);
			}

#if DEBUG
			Ping(DebugFlags.AdvanceState, string.Format("Inrect = {0}, Outrect = {1}, maskRect = {2}", filterRecord->inRect.ToString(), filterRecord->outRect.ToString(), filterRecord->maskRect.ToString()));
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

					error = FillMaskBuffer(ref filterRecord->maskData, ref filterRecord->maskRowBytes, filterRecord->maskRect, filterRecord->maskRate, filterRecord->maskPadding);

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
				lastMaskRect.left = lastMaskRect.right = lastMaskRect.bottom = lastMaskRect.top = 0;
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

					error = FillInputBuffer(ref filterRecord->inData, ref filterRecord->inRowBytes, filterRecord->inRect, filterRecord->inLoPlane, filterRecord->inHiPlane, filterRecord->inputRate, filterRecord->inputPadding);
					if (error != PSError.noErr)
					{
						return error;
					}

					lastInRect = filterRecord->inRect;
					lastInLoPlane = filterRecord->inLoPlane;
					filterRecord->inColumnBytes = (filterRecord->inHiPlane - filterRecord->inLoPlane) + 1;

					if (imageMode == ImageModes.RGB48 || imageMode == ImageModes.Gray16)
					{
						filterRecord->inColumnBytes *= 2; // 2 bytes per plane
					}
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
				lastInRect.left = lastInRect.top = lastInRect.right = lastInRect.bottom = 0;
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

					error = FillOutputBuffer(ref filterRecord->outData, ref filterRecord->outRowBytes, filterRecord->outRect, filterRecord->outLoPlane, filterRecord->outHiPlane, filterRecord->outputPadding);
					if (error != PSError.noErr)
					{
						return error;
					}

#if DEBUG
					System.Diagnostics.Debug.WriteLine(string.Format("outRowBytes = {0}", filterRecord->outRowBytes));
#endif
					// store previous values
					lastOutRowBytes = filterRecord->outRowBytes;
					lastOutRect = filterRecord->outRect;
					lastOutLoPlane = filterRecord->outLoPlane;
					lastOutHiPlane = filterRecord->outHiPlane;
					filterRecord->outColumnBytes = (filterRecord->outHiPlane - filterRecord->outLoPlane) + 1;

					if (imageMode == ImageModes.RGB48 || imageMode == ImageModes.Gray16)
					{
						filterRecord->outColumnBytes *= 2;
					}
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
				lastOutRect.left = lastOutRect.top = lastOutRect.right = lastOutRect.bottom = 0;
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
			int scaleFactor = FixedToInt32(inputRate);
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

			if ((tempSurface == null) || scalew != tempSurface.Width || scaleh != tempSurface.Height)
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
			Ping(DebugFlags.AdvanceState, string.Format("inRowBytes: {0}, Rect: {1}, loplane: {2}, hiplane: {3}, inputRate: {4}", new object[] { inRowBytes, rect, loplane, hiplane, FixedToInt32(inputRate) }));
#endif

			int nplanes = hiplane - loplane + 1;
			int width = (rect.right - rect.left);
			int height = (rect.bottom - rect.top);


			Rectangle lockRect = Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);


			int stride = (width * nplanes);

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
			if (imageMode == ImageModes.RGB48 || imageMode == ImageModes.RGB)
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

					inRowBytes *= 2;

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

							src += 4;
							dst += nplanes;
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
			Ping(DebugFlags.AdvanceState, string.Format("outRowBytes: {0}, Rect: {1}, loplane: {2}, hiplane: {3}", new object[] { outRowBytes, rect, loplane, hiplane }));
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
			if (imageMode == ImageModes.RGB48 || imageMode == ImageModes.RGB)
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

					outRowBytes *= 2;

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

							src += 4;
							dst += nplanes;
						}
					}

					outRowBytes *= 2;

					break;
			}

			return PSError.noErr;
		}

		private unsafe void ScaleTempMask(int maskRate, Rectangle lockRect)
		{
			int scaleFactor = FixedToInt32(maskRate);

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
			if ((tempMask == null) || scalew != tempMask.Width || scaleh != tempMask.Height)
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
			Ping(DebugFlags.AdvanceState, string.Format("maskRowBytes: {0}, Rect: {1}, maskRate: {2}", new object[] { maskRowBytes, rect, FixedToInt32(maskRate) }));
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
			if ((filterCase == FilterCase.EditableTransparencyNoSelection || filterCase == FilterCase.EditableTransparencyWithSelection) &&
				outputHandling == FilterDataHandling.FillMask)
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
			Ping(DebugFlags.BufferSuite, string.Format("Buffer address = {0:X8}, Size = {1}", bufferID.ToInt64(), Memory.Size(bufferID)));
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
			Ping(DebugFlags.ColorServices, string.Format("selector: {0}", info.selector));
#endif
			short err = PSError.noErr;
			switch (info.selector)
			{
				case ColorServicesSelector.ChooseColor:

					string prompt = StringFromPString(info.selectorParameter.pickerPrompt);

					if (pickColor != null)
					{
						ColorPickerResult color = pickColor(prompt, (byte)info.colorComponents[0], (byte)info.colorComponents[1], (byte)info.colorComponents[2]);

						if (color != null)
						{
							info.colorComponents[0] = color.R;
							info.colorComponents[1] = color.G;
							info.colorComponents[2] = color.B;

							if (info.resultSpace == ColorSpace.ChosenSpace)
							{
								info.resultSpace = ColorSpace.RGBSpace;
							}

							err = ColorServicesConvert.Convert(info.sourceSpace, info.resultSpace, ref info.colorComponents);
						}
						else
						{
							err = PSError.userCanceledErr;
						}
					}
					else
					{
						using (ColorPicker picker = new ColorPicker(prompt))
						{
							picker.Color = Color.FromArgb(info.colorComponents[0], info.colorComponents[1], info.colorComponents[2]);

							if (picker.ShowDialog() == DialogResult.OK)
							{
								info.colorComponents[0] = picker.Color.R;
								info.colorComponents[1] = picker.Color.G;
								info.colorComponents[2] = picker.Color.B;

								if (info.resultSpace == ColorSpace.ChosenSpace)
								{
									info.resultSpace = ColorSpace.RGBSpace;
								}

								err = ColorServicesConvert.Convert(info.sourceSpace, info.resultSpace, ref info.colorComponents);
							}
							else
							{
								err = PSError.userCanceledErr;
							}
						}
					}

					break;
				case ColorServicesSelector.ConvertColor:

					err = ColorServicesConvert.Convert(info.sourceSpace, info.resultSpace, ref info.colorComponents);

					break;
				case ColorServicesSelector.GetSpecialColor:

					FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();
					switch (info.selectorParameter.specialColorID)
					{
						case SpecialColorID.BackgroundColor:

							for (int i = 0; i < 4; i++)
							{
								info.colorComponents[i] = (short)filterRecord->backColor[i];
							}
							break;
						case SpecialColorID.ForegroundColor:

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

							switch (imageMode)
							{
								case ImageModes.GrayScale:
									info.colorComponents[0] = pixel[0];
									info.colorComponents[1] = 0;
									info.colorComponents[2] = 0;
									info.colorComponents[3] = 0;
									break;
								case ImageModes.RGB:
									info.colorComponents[0] = pixel[2];
									info.colorComponents[1] = pixel[1];
									info.colorComponents[2] = pixel[0];
									info.colorComponents[3] = 0;
									break;
							}

						}

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

				if (source.BitsPerChannel == 16 && destination.depth == 8)
				{
					if (convertedChannelSurface == null)
					{
						int width = source.Width;
						int height = source.Height;

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

			desc->port = new IntPtr(channel);
			switch (channel)
			{
				case 0:
					desc->channelType = name != Resources.GrayChannelName ? ChannelTypes.ctRed : ChannelTypes.ctBlack;
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

			desc->name = namePtr;

			channelReadDescPtrs.Add(new ChannelDescPtrs() { address = addressPtr, name = namePtr });

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

		private unsafe short DisplayPixelsProc(ref PSPixelMap source, ref VRect srcRect, int dstRow, int dstCol, IntPtr platformContext)
		{
#if DEBUG
			Ping(DebugFlags.DisplayPixels, string.Format("source: version = {0} bounds = {1}, ImageMode = {2}, colBytes = {3}, rowBytes = {4},planeBytes = {5}, BaseAddress = {6}, mat = {7}, masks = {8}", new object[]{ source.version.ToString(), source.bounds.ToString(), ((ImageModes)source.imageMode).ToString("G"),
				source.colBytes.ToString(), source.rowBytes.ToString(), source.planeBytes.ToString(), source.baseAddr.ToString("X8"), source.mat.ToString("X8"), source.masks.ToString("X8")}));
			Ping(DebugFlags.DisplayPixels, string.Format("srcRect = {0} dstCol (x, width) = {1}, dstRow (y, height) = {2}", srcRect.ToString(), dstCol, dstRow));
#endif

			if (platformContext == IntPtr.Zero || source.rowBytes == 0 || source.baseAddr == IntPtr.Zero ||
				(source.imageMode != PSConstants.plugInModeRGBColor && source.imageMode != PSConstants.plugInModeGrayScale))
			{
				return PSError.filterBadParameters;
			}

			int width = srcRect.right - srcRect.left;
			int height = srcRect.bottom - srcRect.top;
			int nplanes = ((FilterRecord*)filterRecordPtr.ToPointer())->planes;

			bool hasTransparencyMask = source.version >= 1 && source.masks != IntPtr.Zero;

			// Ignore the alpha plane if the PSPixelMap does not have a transparency mask.  
			if (!hasTransparencyMask && nplanes == 4)
			{
				nplanes = 3;
			}

			SetupTempDisplaySurface(width, height, hasTransparencyMask);

			byte* baseAddr = (byte*)source.baseAddr.ToPointer();

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
					byte* src = tempDisplaySurface.GetRowAddressUnchecked(y - top);
					byte* dst = baseAddr + (y * source.rowBytes) + left;

					for (int x = 0; x < width; x++)
					{
						src[0] = src[1] = src[2] = *dst;

						src += 4;
						dst += source.colBytes;
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
							byte* src = baseAddr + srcStride + (i * source.planeBytes) + left;
							byte* dst = row + ofs;

							for (int x = 0; x < width; x++)
							{
								*dst = *src;

								src += source.colBytes;
								dst += 4;
							}
						}

					}
					else
					{
						byte* src = baseAddr + (y * source.rowBytes) + left;
						byte* dst = tempDisplaySurface.GetRowAddressUnchecked(surfaceY);

						for (int x = 0; x < width; x++)
						{
							dst[0] = src[2];
							dst[1] = src[1];
							dst[2] = src[0];
							if (source.colBytes == 4)
							{
								dst[3] = src[3];
							}

							src += source.colBytes;
							dst += 4;
						}
					}
				}
			}


			using (Graphics gr = Graphics.FromHdc(platformContext))
			{
				if (source.colBytes == 4 || nplanes == 4 && source.colBytes == 1)
				{
					Display32BitBitmap(gr, dstCol, dstRow);
				}
				else
				{
					// Apply the transparency mask for the Protected Transparency cases.
					if (hasTransparencyMask && (this.filterCase == FilterCase.ProtectedTransparencyNoSelection || this.filterCase == FilterCase.ProtectedTransparencyWithSelection)) 
					{
						PSPixelMask* srcMask = (PSPixelMask*)source.masks.ToPointer();
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
				unit = unitFloat.unit;
			}
			catch (NullReferenceException)
			{
			}

			data = unitFloat.value;

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
						value = unitFloat.value;
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

			if (unitFloat.unit != units)
			{
				descErr = PSError.paramErr;
			}

			double amount = unitFloat.value;
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
			UnitFloat item = new UnitFloat() { unit = unit, value = data };

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
				handle = Memory.Allocate(Marshal.SizeOf(typeof(PSHandle)), true);

				PSHandle* hand = (PSHandle*)handle.ToPointer();

				hand->pointer = Memory.Allocate(size, true);
				hand->size = size;

				handles.Add(handle, *hand);
#if DEBUG
				Ping(DebugFlags.HandleSuite, string.Format("Handle: {0:X8}, pointer: {1:X8}, size: {1}", handle.ToInt64(), hand->pointer.ToInt64(), size));
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
				Ping(DebugFlags.HandleSuite, string.Format("Handle: {0:X8}", h.ToInt64()));
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
			Ping(DebugFlags.HandleSuite, string.Format("Handle: {0:X8}", h.ToInt64()));
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
			Ping(DebugFlags.HandleSuite, string.Format("Handle: {0:X8}, moveHigh: {1}", h.ToInt64(), moveHigh));
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
			Ping(DebugFlags.HandleSuite, String.Format("Handle Pointer Address = 0x{0:X}", handles[h].pointer.ToInt64()));
#endif
			return handles[h].pointer;
		}

		private int HandleGetSizeProc(IntPtr h)
		{
#if DEBUG
			Ping(DebugFlags.HandleSuite, string.Format("Handle: {0:X8}", h.ToInt64()));
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
			Ping(DebugFlags.HandleSuite, string.Format("Handle: {0:X8}", h.ToInt64()));
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
			Ping(DebugFlags.HandleSuite, string.Format("Handle address = {0:X8}", h.ToInt64()));
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

		/// <summary>
		/// Reads the JPEG APP1 section to extract EXIF or XMP meta data.
		/// </summary>
		/// <param name="jpegData">The JPEG image byte array.</param>
		/// <param name="exif">if set to <c>true</c> extract the EXIF meta data; otherwise extract the XMP meta data.</param>
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
				while ((p[0] == 0xff && (p[1] >= 0xe0 && p[1] <= 0xef)) && bytes == null) // APP sections
				{

					sectionLength = (ushort)((p[2] << 8) | p[3]); // JPEG uses big-endian   

					if (p[0] == 0xff && p[1] == 0xe1) // APP1
					{
						p += 2; // skip the header bytes

						string sig;

						if (exif)
						{
							sig = new string((sbyte*)p + 2, 0, 6, Windows1252Encoding);

							if (sig == "Exif\0\0")
							{
								int exifLength = sectionLength - 8; // subtract the signature and section length size to get the data length. 
								bytes = new byte[exifLength];

								Marshal.Copy(new IntPtr(p + 8), bytes, 0, exifLength);
							}

							p += sectionLength;

						}
						else
						{
							sig = new string((sbyte*)p + 2, 0, 29, Windows1252Encoding);

							if (sig == "http://ns.adobe.com/xap/1.0/\0")
							{
								// TODO: The XMP extension packets are not supported, so the XMP data must be less that 65502 bytes in size.
								int xmpLength = sectionLength - 31;
								bytes = new byte[xmpLength];

								Marshal.Copy(new IntPtr(p + 31), bytes, 0, xmpLength);
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
		/// Extracts the meta data from the image.
		/// </summary>
		/// <param name="bytes">The output bytes.</param>
		/// <param name="exif">set to <c>true</c> if the EXIF data is requested.</param>
		/// <returns><c>true</c> if the meta data was extracted otherwise <c>false</c></returns>
		private bool ExtractMetadata(out byte[] bytes, bool exif)
		{
			bytes = null;
			if (exifBitmap == null)
			{
				return false;
			}

#if !GDIPLUS
			BitmapMetadata metaData = null;

			try
			{
				metaData = exifBitmap.Metadata as BitmapMetadata;
			}
			catch (NotSupportedException)
			{
			}

			if (metaData == null)
			{
				return false;
			}

			if (MetaDataConverter.IsJPEGMetaData(metaData))
			{
				try
				{
					if (exif)
					{
						if (metaData.GetQuery("/app1/ifd/exif") == null)
						{
							return false;
						}
					}
					else
					{
						if (metaData.GetQuery("/xmp") == null)
						{
							return false;
						}
					}
				}
				catch (System.IO.IOException)
				{
					return false; // WINCODEC_ERR_INVALIDQUERYREQUEST
				}
			}
			else
			{
				metaData = MetaDataConverter.ConvertMetaDataToJPEG(metaData, exif);

				if (metaData == null)
				{
					return false;
				}
			}
#endif

			using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
			{
#if GDIPLUS
				exifBitmap.Save(ms, ImageFormat.Jpeg);
#else
				JpegBitmapEncoder enc = new JpegBitmapEncoder();
				enc.Frames.Add(BitmapFrame.Create(exifBitmap, null, metaData, null));
				enc.Save(ms);
#endif

				bytes = ReadJpegAPP1(ms.GetBuffer(), exif);
			}

			return (bytes != null);
		}

		private unsafe short PropertyGetProc(uint signature, uint key, int index, ref IntPtr simpleProperty, ref IntPtr complexProperty)
		{
#if DEBUG
			Ping(DebugFlags.PropertySuite, string.Format("Sig: {0}, Key: {1}, Index: {2}", PropToString(signature), PropToString(key), index));
#endif
			if (signature != PSConstants.kPhotoshopSignature)
			{
				return PSError.errPlugInHostInsufficient;
			}

			byte[] bytes = null;

			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			short err = PSError.noErr;

			switch (key)
			{
				case PSProperties.propBigNudgeH:
				case PSProperties.propBigNudgeV:
					simpleProperty = new IntPtr(Int32ToFixed(10));
					break;
				case PSProperties.propCaption:
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
				case PSProperties.propChannelName:
					if (index < 0 || index > (filterRecord->planes - 1))
						return PSError.errPlugInPropertyUndefined;

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
				case PSProperties.propCopyright:
				case PSProperties.propCopyright2:
					simpleProperty = new IntPtr(hostInfo.Copyright ? 1 : 0);
					break;
				case PSProperties.propEXIFData:
				case PSProperties.propXMPData:
					if (ExtractMetadata(out bytes, key == PSProperties.propEXIFData))
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
				case PSProperties.propGridMajor:
					simpleProperty = new IntPtr(Int32ToFixed(1));
					break;
				case PSProperties.propGridMinor:
					simpleProperty = new IntPtr(4);
					break;
				case PSProperties.propImageMode:
					simpleProperty = new IntPtr((int)filterRecord->imageMode);
					break;
				case PSProperties.propInterpolationMethod:
					simpleProperty = new IntPtr(PSConstants.InterpolationMethod.NearestNeghbor);
					break;
				case PSProperties.propNumberOfChannels:
					simpleProperty = new IntPtr(filterRecord->planes);
					break;
				case PSProperties.propNumberOfPaths:
					simpleProperty = new IntPtr(0);
					break;
				case PSProperties.propPathName:
					if (complexProperty != IntPtr.Zero)
					{
						complexProperty = HandleNewProc(0);
					}
					break;
				case PSProperties.propWorkPathIndex:
				case PSProperties.propClippingPathIndex:
				case PSProperties.propTargetPathIndex:
					simpleProperty = new IntPtr(-1);
					break;
				case PSProperties.propRulerUnits:
					simpleProperty = new IntPtr((int)hostInfo.RulerUnit);
					break;
				case PSProperties.propRulerOriginH:
				case PSProperties.propRulerOriginV:
					simpleProperty = new IntPtr(Int32ToFixed(0));
					break;
				case PSProperties.propWatermark:
					simpleProperty = new IntPtr(hostInfo.Watermark ? 1 : 0);
					break;
				case PSProperties.propSerialString:
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
				case PSProperties.propURL:
					if ((hostInfo.Url != null) && !string.IsNullOrEmpty(hostInfo.Url.OriginalString))
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
				case PSProperties.propTitle:
					string title;
					if (!string.IsNullOrEmpty(hostInfo.Title))
					{
						title = hostInfo.Title;
					}
					else
					{
						title = "temp.png";
					}

					bytes = Encoding.ASCII.GetBytes(title);
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
				case PSProperties.propWatchSuspension:
					simpleProperty = new IntPtr(0);
					break;
				case PSProperties.propDocumentWidth:
					simpleProperty = new IntPtr(source.Width);
					break;
				case PSProperties.propDocumentHeight:
					simpleProperty = new IntPtr(source.Height);
					break;
				case PSProperties.propToolTips:
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
				return PSError.errPlugInHostInsufficient;
			}

			int size = 0;
			byte[] bytes = null;

			int simple = simpleProperty.ToInt32();

			switch (key)
			{
				case PSProperties.propBigNudgeH:
					break;
				case PSProperties.propBigNudgeV:
					break;
				case PSProperties.propCaption:
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
				case PSProperties.propCopyright:
				case PSProperties.propCopyright2:
					hostInfo.Copyright = simple != 0;
					break;
				case PSProperties.propEXIFData:
				case PSProperties.propXMPData:
					break;
				case PSProperties.propGridMajor:
					break;
				case PSProperties.propGridMinor:
					break;
				case PSProperties.propRulerOriginH:
					break;
				case PSProperties.propRulerOriginV:
					break;
				case PSProperties.propURL:
					size = HandleGetSizeProc(complexProperty);
					if (size > 0)
					{
						bytes = new byte[size];
						Marshal.Copy(HandleLockProc(complexProperty, 0), bytes, 0, size);
						HandleUnlockProc(complexProperty);

						hostInfo.Url = new Uri(Encoding.ASCII.GetString(bytes, 0, size));
					}
					break;
				case PSProperties.propWatchSuspension:
					break;
				case PSProperties.propWatermark:
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
			short count = ResourceCountProc(ofType);

			int size = HandleGetSizeProc(data);
			try
			{
				byte[] bytes = new byte[size];

				if (size > 0)
				{
					Marshal.Copy(HandleLockProc(data, 0), bytes, 0, size);
					HandleUnlockProc(data);
				}

				pseudoResources.Add(new PSResource(ofType, count, bytes));
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
			Ping(DebugFlags.SPBasicSuite, string.Format("block: {0:X8}", block.ToInt64()));
#endif
			Memory.Free(block);
			return PSError.kSPNoErr;
		}

		private int SPBasicReallocateBlock(IntPtr block, int newSize, ref IntPtr newblock)
		{
#if DEBUG
			Ping(DebugFlags.SPBasicSuite, string.Format("block: {0:X8}, size: {1}", block.ToInt64(), newSize));
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
				}

				for (int i = 0; i < 4; i++)
				{
					filterRecord->foreColor[i] = foregroundColor[i];
				}
			}

			filterRecord->bufferSpace = BufferSpaceProc();
			filterRecord->maxSpace = 1000000000;

			filterRecord->hostSig = BitConverter.ToUInt32(Encoding.ASCII.GetBytes(".NET"), 0);
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

				if (activePICASuites != null)
				{
					// free any remaining suites
					activePICASuites.Dispose();
					activePICASuites = null;
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

				disposed = true;

			}
		}

		#endregion
	}
}
