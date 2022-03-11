/////////////////////////////////////////////////////////////////////////////////
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
        /// <param name="parentWindowHandle">The parent window handle.</param>
        /// <param name="name">The plug-in name.</param>
        /// <param name="zstringSuite">The ASZString suite.</param>
        /// <exception cref="ArgumentNullException"><paramref name="zstringSuite"/> is null.</exception>
        public unsafe PICAUIHooksSuite(IntPtr parentWindowHandle, string name, IASZStringSuite zstringSuite)
        {
            if (zstringSuite == null)
            {
                throw new ArgumentNullException(nameof(zstringSuite));
            }

            hwnd = parentWindowHandle;
            pluginName = name ?? string.Empty;
            uiWindowHandle = new UISuiteMainWindowHandle(MainWindowHandle);
            uiSetCursor = new UISuiteHostSetCursor(HostSetCursor);
            uiTickCount = new UISuiteHostTickCount(HostTickCount);
            uiPluginName = new UISuiteGetPluginName(GetPluginName);
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

        private unsafe int GetPluginName(IntPtr pluginRef, ASZString* name)
        {
            if (name == null)
            {
                return PSError.kSPBadParameterError;
            }

            try
            {
                *name = zstringSuite.CreateFromString(pluginName);
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        public PSUIHooksSuite1 CreateUIHooksSuite1(IPICASuiteDataProvider suiteDataProvider)
        {
            PSUIHooksSuite1 suite = new PSUIHooksSuite1
            {
                processEvent = Marshal.GetFunctionPointerForDelegate(suiteDataProvider.ProcessEvent),
                displayPixels = Marshal.GetFunctionPointerForDelegate(suiteDataProvider.DisplayPixels),
                progressBar = Marshal.GetFunctionPointerForDelegate(suiteDataProvider.Progress),
                testAbort = Marshal.GetFunctionPointerForDelegate(suiteDataProvider.TestAbort),
                MainAppWindow = Marshal.GetFunctionPointerForDelegate(uiWindowHandle),
                SetCursor = Marshal.GetFunctionPointerForDelegate(uiSetCursor),
                TickCount = Marshal.GetFunctionPointerForDelegate(uiTickCount),
                GetPluginName = Marshal.GetFunctionPointerForDelegate(uiPluginName)
            };

            return suite;
        }
    }
}
