/////////////////////////////////////////////////////////////////////////////////
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
        private sealed class PICASuite : IDisposable
        {
            private IntPtr suitePointer;
            private int refCount;
            private bool disposed;

            public IntPtr SuitePointer
            {
                get
                {
                    return this.suitePointer;
                }
            }

            public int RefCount
            {
                get
                {
                    return this.refCount;
                }
                set
                {
                    this.refCount = value;
                }
            }

            public PICASuite(IntPtr suite)
            {
                this.suitePointer = suite;
                this.refCount = 1;
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            ~PICASuite()
            {
                Dispose(false);
            }

            private void Dispose(bool disposing)
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                    }

                    if (this.suitePointer != IntPtr.Zero)
                    {
                        Memory.Free(this.suitePointer);
                        this.suitePointer = IntPtr.Zero;
                    }

                    disposed = true;
                }
            }
        }
        
        private Dictionary<string, PICASuite> activeSuites;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivePICASuites"/> class.
        /// </summary>
        public ActivePICASuites()
        {
            this.activeSuites = new Dictionary<string, PICASuite>(StringComparer.Ordinal);
            this.disposed = false;
        }

        /// <summary>
        /// Allocates a new PICA suite.
        /// </summary>
        /// <typeparam name="TSuite">The type of the suite.</typeparam>
        /// <param name="key">The string specifying the suite name and version.</param>
        /// <param name="suite">The suite to be marshaled to unmanaged memory.</param>
        /// <returns>The pointer to the allocated suite.</returns>
        public IntPtr AllocateSuite<TSuite>(string key, TSuite suite)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException("ActivePICASuites");
            }

            IntPtr suitePointer = Memory.Allocate(Marshal.SizeOf(typeof(TSuite)), false);
            Marshal.StructureToPtr(suite, suitePointer, false);

            this.activeSuites.Add(key, new PICASuite(suitePointer));

            return suitePointer;
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
            if (this.disposed)
            {
                throw new ObjectDisposedException("ActivePICASuites");
            }

            return this.activeSuites.ContainsKey(key);
        }

        /// <summary>
        /// Increments the reference count on the specified suite.
        /// </summary>
        /// <param name="key">The string specifying the suite name and version.</param>
        /// <returns>The pointer to the suite instance.</returns>
        public IntPtr AddRef(string key)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException("ActivePICASuites");
            }

            PICASuite suite = this.activeSuites[key];
            suite.RefCount += 1;
            this.activeSuites[key] = suite;

            return suite.SuitePointer;
        }

        /// <summary>
        /// Decrements the reference count and removes the specified suite if it is zero.
        /// </summary>
        /// <param name="key">The string specifying the suite name and version.</param>
        public void RemoveRef(string key)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException("ActivePICASuites");
            }

            PICASuite suite;
            if (this.activeSuites.TryGetValue(key, out suite))
            {
                suite.RefCount -= 1;

                if (suite.RefCount == 0)
                {
                    suite.Dispose();
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
            if (!this.disposed)
            {
                this.disposed = true;

                foreach (PICASuite item in this.activeSuites.Values)
                {
                    item.Dispose();
                }
                this.activeSuites = null;
            }
        }
    }
}
