/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.PSApi.PICA
{
    internal sealed class PICAUIHooksSuite
    {
        private readonly IntPtr hwnd;
        private readonly string pluginName;
        private readonly UISuiteMainWindowHandle uiWindowHandle;
        private readonly UISuiteHostSetCursor uiSetCursor;
        private readonly UISuiteHostTickCount uiTickCount;
        private readonly UISuiteGetPluginName uiPluginName;
        private readonly IASZStringSuite zstringSuite;

        /// <summary>
        /// Initializes a new instance of the <see cref="PICAUIHooksSuite"/> class.
        /// </summary>
        /// <param name="filterRecord">The filter record.</param>
        /// <param name="name">The plug-in name.</param>
        /// <param name="zstringSuite">The ASZString suite.</param>
        /// <exception cref="ArgumentNullException"><paramref name="zstringSuite"/> is null.</exception>
        public unsafe PICAUIHooksSuite(FilterRecord* filterRecord, string name, IASZStringSuite zstringSuite)
        {
            if (zstringSuite == null)
            {
                throw new ArgumentNullException("zstringSuite");
            }

            this.hwnd = ((PlatformData*)filterRecord->platformData.ToPointer())->hwnd;
            this.pluginName = name ?? string.Empty;
            this.uiWindowHandle = new UISuiteMainWindowHandle(MainWindowHandle);
            this.uiSetCursor = new UISuiteHostSetCursor(HostSetCursor);
            this.uiTickCount = new UISuiteHostTickCount(HostTickCount);
            this.uiPluginName = new UISuiteGetPluginName(GetPluginName);
            this.zstringSuite = zstringSuite;
        }

        private IntPtr MainWindowHandle()
        {
            return hwnd;
        }

        private int HostSetCursor(IntPtr cursor)
        {
            return PSError.kSPUnimplementedError;
        }

        private uint HostTickCount()
        {
            return 60U;
        }

        private int GetPluginName(IntPtr pluginRef, ref IntPtr name)
        {
            name = zstringSuite.CreateFromString(this.pluginName);

            return PSError.kSPNoError;
        }

        public unsafe PSUIHooksSuite1 CreateUIHooksSuite1(FilterRecord* filterRecord)
        {
            PSUIHooksSuite1 suite = new PSUIHooksSuite1
            {
                processEvent = filterRecord->processEvent,
                displayPixels = filterRecord->displayPixels,
                progressBar = filterRecord->progressProc,
                testAbort = filterRecord->abortProc,
                MainAppWindow = Marshal.GetFunctionPointerForDelegate(this.uiWindowHandle),
                SetCursor = Marshal.GetFunctionPointerForDelegate(this.uiSetCursor),
                TickCount = Marshal.GetFunctionPointerForDelegate(this.uiTickCount),
                GetPluginName = Marshal.GetFunctionPointerForDelegate(this.uiPluginName)
            };

            return suite;
        }
    }
}
