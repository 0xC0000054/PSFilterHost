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

using System;

namespace PSFilterHostDll.PSApi
{
    /// /// <summary>
    /// Provides the data required by the various PICA suites.
    /// </summary>
    internal interface IPICASuiteDataProvider
    {
        /// <summary>
        /// Gets the parent window handle.
        /// </summary>
        /// <value>
        /// The parent window handle.
        /// </value>
        IntPtr ParentWindowHandle
        {
            get;
        }

        /// <summary>
        /// Gets the display pixels callback delegate.
        /// </summary>
        /// <value>
        /// The display pixels callback delegate.
        /// </value>
        DisplayPixelsProc DisplayPixels
        {
            get;
        }

        /// <summary>
        /// Gets the process event callback delegate.
        /// </summary>
        /// <value>
        /// The process event callback delegate.
        /// </value>
        ProcessEventProc ProcessEvent
        {
            get;
        }

        /// <summary>
        /// Gets the progress callback delegate.
        /// </summary>
        /// <value>
        /// The progress callback delegate.
        /// </value>
        ProgressProc Progress
        {
            get;
        }

        /// <summary>
        /// Gets the test abort callback delegate.
        /// </summary>
        /// <value>
        /// The test abort callback delegate.
        /// </value>
        TestAbortProc TestAbort
        {
            get;
        }
    }
}
