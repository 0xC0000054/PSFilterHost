/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2020 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;

namespace HostTest
{
    internal sealed class TempDirectory : IDisposable
    {
        private readonly string path;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TempDirectory"/> class.
        /// </summary>
        public TempDirectory()
        {
            path = CreateTempDirectory();
        }

        private static string CreateTempDirectory()
        {
            string basePath = Path.GetTempPath();

            while (true)
            {
                string tempDirectoryPath = Path.Combine(basePath, Path.GetRandomFileName());

                try
                {
                    Directory.CreateDirectory(tempDirectoryPath);
                    return tempDirectoryPath;
                }
                catch (IOException)
                {
                    // Try again if the directory already exists.
                }
            }
        }

        /// <summary>
        /// Gets a random file name in the directory.
        /// </summary>
        /// <returns>A random file name in the directory.</returns>
        public string GetRandomFileName()
        {
            return Path.Combine(path, Path.GetRandomFileName());
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                try
                {
                    Directory.Delete(path, true);
                }
                catch (ArgumentException)
                {
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
    }
}
