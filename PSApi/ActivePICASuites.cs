﻿/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2016 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.PSApi
{
    internal sealed class ActivePICASuites : IDisposable
    {
        private sealed class PICASuite
        {
            public readonly IntPtr suitePointer;
            public int refCount;

            public PICASuite(IntPtr suite)
            {
                this.suitePointer = suite;
                this.refCount = 1;
            }
        }
        
        private Dictionary<string, PICASuite> activeSuites;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivePICASuites"/> class.
        /// </summary>
        public ActivePICASuites()
        {
            this.activeSuites = new Dictionary<string, PICASuite>();
            this.disposed = false;
        }

        /// <summary>
        /// Allocates a new PICA suite.
        /// </summary>
        /// <typeparam name="TSuite">The type of the suite.</typeparam>
        /// <param name="key">The string specifying the suite name and version.</param>
        /// <returns>The pointer to the allocated suite.</returns>
        public IntPtr AllocateSuite<TSuite>(string key)
        {
            IntPtr suite = Memory.Allocate(Marshal.SizeOf(typeof(TSuite)), false);
            this.activeSuites.Add(key, new PICASuite(suite));

            return suite;
        }

        /// <summary>
        /// Determines whether the specified suite is loaded.
        /// </summary>
        /// <param name="key">The string specifying the suite name and version.</param>
        /// <returns>
        ///   <c>true</c> if the specified suite is loaded; otherwise, <c>false</c>.
        /// </returns>
        public bool IsLoaded(string key)
        {
            return this.activeSuites.ContainsKey(key);
        }

        /// <summary>
        /// Increments the reference count on the specified suite.
        /// </summary>
        /// <param name="key">The string specifying the suite name and version.</param>
        /// <returns>The pointer to the suite instance.</returns>
        public IntPtr AddRef(string key)
        {
            PICASuite suite = this.activeSuites[key];
            suite.refCount++;
            this.activeSuites[key] = suite;

            return suite.suitePointer;
        }

        /// <summary>
        /// Decrements the reference count and removes the specified suite if it is zero.
        /// </summary>
        /// <param name="key">The string specifying the suite name and version.</param>
        public void RemoveRef(string key)
        {
            PICASuite suite;
            if (this.activeSuites.TryGetValue(key, out suite))
            {
                suite.refCount--;

                if (suite.refCount == 0)
                {
                    Memory.Free(suite.suitePointer);
                    this.activeSuites.Remove(key);
                }
                else
                {
                    this.activeSuites[key] = suite;
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ActivePICASuites()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;

                if (disposing)
                {
                }

                foreach (PICASuite item in this.activeSuites.Values)
                {
                    Memory.Free(item.suitePointer);
                }
            }
        }
    }
}
