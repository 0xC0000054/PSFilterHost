/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2017 Nicholas Hayes
// 
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved. 
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using Microsoft.Win32;

namespace HostTest
{
    internal static class WICHelpers
    {
        private static readonly char[] SplitChars = new char[] { ' ', ',' };
        delegate uint GetStringMethod(uint cch, StringBuilder wz);

        private static string GetString(GetStringMethod method)
        {
            uint size = method(0, null);
            if (size > 0)
            {
                StringBuilder sb = new StringBuilder
                {
                    Length = (int)size
                };
                method(size, sb);

                return sb.ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        private static IEnumerable<IWICComponentInfo> GetComponentInfos(WICComponentType type)
        {
            IWICImagingFactory factory = (IWICImagingFactory)new WICImagingFactoryCoClass();
            IEnumUnknown eu = null;
            object[] o = new object[1];

            try
            {
                eu = factory.CreateComponentEnumerator(type, WICComponentEnumerateOptions.WICComponentEnumerateDefault);

                int hr = 0;
                while (hr == 0)
                {
                    uint fetched = 0;
                    hr = eu.Next(1, o, ref fetched);
                    Marshal.ThrowExceptionForHR(hr);
                    if (fetched == 1)
                    {
                        IWICComponentInfo ci = (IWICComponentInfo)o[0];
                        try
                        {
                            yield return ci;
                        }
                        finally
                        {
                            Marshal.ReleaseComObject(ci);
                        }
                    }
                }
            }
            finally
            {
                if (eu != null)
                {
                    Marshal.ReleaseComObject(eu);
                } 
                Marshal.ReleaseComObject(factory);
            }
        }

        private static IEnumerable<string> GetFilterStrings(IEnumerable<string> extensions)
        {
            Dictionary<string, List<string>> extDict = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (string fileExtension in extensions)
            {
                string friendlyName = null;
                string progid = null;

                using (RegistryKey rk = Registry.ClassesRoot.OpenSubKey(fileExtension))
                {
                    if (rk != null)
                    {
                        progid = rk.GetValue(null, null) as string;
                    }
                }

                if (!string.IsNullOrEmpty(progid))
                {
                    using (RegistryKey rk = Registry.ClassesRoot.OpenSubKey(progid))
                    {
                        if (rk != null)
                        {
                            friendlyName = rk.GetValue(null, null) as string;
                        }
                    }
                }

                if (string.IsNullOrEmpty(friendlyName))
                {
                    // Set the names for the built-in formats if Windows does not.
                    switch (fileExtension)
                    {
                        case ".exif":
                            friendlyName = "JPEG Image";
                            break;
                        case ".icon":
                            friendlyName = "Icon";
                            break;
                        case ".tif":
                        case ".tiff":
                            friendlyName = "TIFF Image";
                            break;
                        default:
                            friendlyName = fileExtension.TrimStart('.').ToUpperInvariant() + " File";
                            break;
                    }
                }

                if (extDict.ContainsKey(friendlyName))
                {
                    extDict[friendlyName].Add(fileExtension);
                }
                else
                {
                    List<string> exts = new List<string>
                    {
                        fileExtension
                    };
                    extDict.Add(friendlyName, exts);
                }
            }

            List<string> filters = new List<string>(extDict.Keys.Count);

            foreach (var item in extDict)
            {
                List<string> exts = item.Value;

                StringBuilder formats = new StringBuilder();
                StringBuilder desc = new StringBuilder(item.Key);
                desc.Append(" (");
                int length = exts.Count;
                int stopLength = length - 1;

                for (int i = 0; i < length; i++)
                {
                    formats.Append("*");
                    desc.Append("*");

                    formats.Append(exts[i]);
                    desc.Append(exts[i]);

                    if (i < stopLength)
                    {
                        formats.Append(";");
                        desc.Append(", ");
                    }
                }
                desc.Append(")");

                string filter = desc.ToString() + "|" + formats.ToString();
                filters.Add(filter);
            }
            // Sort the filters by name in ascending order.
            filters.Sort(StringComparer.InvariantCulture);

            return filters;
        }

        public static string GetOpenDialogFilterString()
        {
            HashSet<string> fileExtensions = new HashSet<string>(StringComparer.Ordinal);

            foreach (IWICBitmapCodecInfo info in GetComponentInfos(WICComponentType.WICDecoder))
            {
                string extString = GetString(info.GetFileExtensions).ToLowerInvariant();

                fileExtensions.UnionWith(extString.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries));
            }

            StringBuilder formats = new StringBuilder();
            StringBuilder description = new StringBuilder("Images (");

            foreach (var item in fileExtensions.OrderBy(x => x, StringComparer.Ordinal))
            {
                formats.Append("*");
                description.Append("*");

                formats.Append(item);
                description.Append(item);

                formats.Append(";");
                description.Append(", ");
            }
            // Remove the last separator. 
            formats.Remove(formats.Length - 1, 1);
            description.Remove(description.Length - 2, 2);
            description.Append(")");

            StringBuilder filters = new StringBuilder(description.ToString() + "|" + formats.ToString());

            foreach (var item in GetFilterStrings(fileExtensions))
            {
                filters.Append("|");
                filters.Append(item);
            }

            return filters.ToString();
        }

        public static System.Collections.ObjectModel.ReadOnlyCollection<string> GetDecoderFileExtensions()
        {
            HashSet<string> extensions = new HashSet<string>(StringComparer.Ordinal);

            foreach (IWICBitmapCodecInfo info in GetComponentInfos(WICComponentType.WICDecoder))
            {
                string extString = GetString(info.GetFileExtensions).ToLowerInvariant();
                
                extensions.UnionWith(extString.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries));
            }

            return new System.Collections.ObjectModel.ReadOnlyCollection<string>(extensions.ToList());
        }
    }
}
