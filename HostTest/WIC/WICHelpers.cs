/////////////////////////////////////////////////////////////////////////////////
//
// Adobe(R) Photoshop(R) filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2014 Nicholas Hayes
// 
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved. 
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using Microsoft.Win32;

namespace HostTest
{
    internal static class WICHelpers
    {
        private static readonly char[] splitChars = new char[] { ' ', ',' };
        delegate uint GetStringMethod(uint cch, StringBuilder wz);

        private static string GetString(GetStringMethod method)
        {
            uint size = method(0, null);
            if (size > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.Length = (int)size;
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


        private static List<string> GetFilterStrings(List<string[]> extList)
        {
            List<string> filters = new List<string>();
            Dictionary<string, List<string>> extDict = new Dictionary<string, List<string>>();
            foreach (var list in extList)
            {                    
                string fileExtName = string.Empty;

                for (int i = 0; i < list.Length; i++)
                {
                    string fileExtension = list[i];
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
                                fileExtName = rk.GetValue(null, null) as string;
                                if (!string.IsNullOrEmpty(fileExtName))
                                {
                                    if (!extDict.ContainsKey(fileExtName))
                                    {
                                        List<string> exts = new List<string>();
                                        exts.Add(fileExtension);
                                        extDict.Add(fileExtName, exts);
                                    }
                                    else
                                    {
                                        extDict[fileExtName].Add(fileExtension);
                                    }
                                }
#if DEBUG
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine(fileExtension);
                                } 
#endif
                            }
                        }
                    }
                    else
                    {
                        extDict[fileExtName].Add(fileExtension);
                    }
                   
                }
            }


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

            return filters;
        }

        public static string GetOpenDialogFilterString()
        {
            StringBuilder formats = new StringBuilder();
            StringBuilder formatDesc = new StringBuilder("Images (");
            List<string[]> extList = new List<string[]>();

            foreach (IWICBitmapCodecInfo info in GetComponentInfos(WICComponentType.WICDecoder))
            {
                string extString = GetString(info.GetFileExtensions);

                string[] exts = extString.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

                extList.Add(exts);

                foreach (var item in exts)
                {
                    formats.Append("*");
                    formatDesc.Append("*");

                    formats.Append(item);
                    formatDesc.Append(item);

                    formats.Append(";");
                    formatDesc.Append(", ");
                } 

            }

            formatDesc.Remove(formatDesc.Length - 2, 2); // remove the final comma
            formatDesc.Append(")");

            StringBuilder filters = new StringBuilder(formatDesc.ToString() + "|" + formats.ToString());
            List<string> data = GetFilterStrings(extList);

            foreach (var item in data)
            {
                filters.Append("|");
                filters.Append(item);
            }


            return filters.ToString();
        }

        public static string[] GetDecoderFileExtensions()
        {
            List<string> extensions = new List<string>();

            foreach (IWICBitmapCodecInfo info in GetComponentInfos(WICComponentType.WICDecoder))
            {
                string extString = GetString(info.GetFileExtensions);
                string[] exts = extString.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

                extensions.AddRange(exts);
            }

            return extensions.ToArray();
        }

    }
}
