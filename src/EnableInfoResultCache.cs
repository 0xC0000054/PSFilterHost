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

using PSFilterHostDll.EnableInfo;
using System;
using System.Collections.Generic;

namespace PSFilterHostDll
{
    internal sealed class EnableInfoResultCache
    {
        private readonly Dictionary<string, EnableInfoData> values;

        private EnableInfoResultCache()
        {
            values = new Dictionary<string, EnableInfoData>(StringComparer.OrdinalIgnoreCase);
        }

        public static EnableInfoResultCache Instance { get; } = new EnableInfoResultCache();

        public bool? TryGetValue(string enableInfo, EnableInfoVariables variables)
        {
            bool? result = null;

            try
            {
                EnableInfoData data;

                if (values.TryGetValue(enableInfo, out data))
                {
                    result = data.TryGetResult(variables);
                }
                else
                {
                    data = new EnableInfoData(enableInfo);

                    result = data.TryGetResult(variables);

                    values.Add(enableInfo, data);
                }
            }
            catch (EnableInfoException)
            {
                // Ignore any errors that occur when evaluating the enable info expression.
            }

            return result;
        }

        private sealed class EnableInfoData
        {
            private readonly Expression expression;
            private readonly Dictionary<EnableInfoVariables, bool> resultCache;

            internal EnableInfoData(string enableInfo)
            {
                try
                {
                    expression = EnableInfoParser.Parse(enableInfo);
                }
                catch (EnableInfoException)
                {
                    expression = null;
                }
                resultCache = new Dictionary<EnableInfoVariables, bool>();
            }

            internal bool? TryGetResult(EnableInfoVariables variables)
            {
                bool result;

                if (!resultCache.TryGetValue(variables, out result))
                {
                    if (expression != null)
                    {
                        result = new EnableInfoInterpreter(variables).Evaluate(expression);

                        resultCache.Add(variables, result);
                    }
                    else
                    {
                        return null;
                    }
                }

                return result;
            }
        }
    }
}
