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

namespace PSFilterHostDll
{
    /// <summary>
    /// Encapsulates the International Color Consortium (ICC) color profiles used by the host application.
    /// </summary>
    /// <threadsafety static="true" instance="false"/>
    public sealed class HostColorManagement
    {
        private byte[] documentProfileBytes;
        private byte[] monitorProfileBytes;

        /// <summary>
        /// Initializes a new instance of the <see cref="HostColorManagement"/> class, with a <see cref="Byte"/> array containing the document color profile.
        /// </summary>
        /// <param name="documentProfile">The byte array that contains the color profile of the document.</param>
        /// <overloads>Initializes a new instance of the <see cref="HostColorManagement"/> class.</overloads>
        /// <exception cref="ArgumentNullException"><paramref name="documentProfile"/> is null.</exception>
        public HostColorManagement(byte[] documentProfile) : this(documentProfile, ColorProfileHelper.GetPrimaryMonitorProfile(), false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HostColorManagement"/> class, with a <see cref="String"/> containing the path of the document color profile.
        /// </summary>
        /// <param name="documentProfilePath">The path of the document color profile.</param>
        /// <exception cref="ArgumentNullException"><paramref name="documentProfilePath"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="documentProfilePath"/> is a 0 length string, contains only white-space, or contains one or more invalid characters.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="documentProfilePath"/> is invalid, such as referring to an unmapped drive.</exception>
        /// <exception cref="FileNotFoundException"><paramref name="documentProfilePath"/> could not be found.</exception>
        /// <exception cref="IOException">An I/O error occurred when opening the file.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public HostColorManagement(string documentProfilePath) : this(documentProfilePath, ColorProfileHelper.GetPrimaryMonitorProfile(), false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HostColorManagement"/> class, with a <see cref="Byte"/> array containing the document color profile
        /// and a <see cref="String"/> containing the path of the monitor color profile.
        /// </summary>
        /// <param name="documentProfile">The byte array that contains the color profile of the document.</param>
        /// <param name="monitorProfilePath">The path of the monitor color profile.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="documentProfile"/> is null
        /// or
        /// <paramref name="monitorProfilePath"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="monitorProfilePath"/> is a 0 length string, contains only white-space, or contains one or more invalid characters.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="monitorProfilePath"/> is invalid, such as referring to an unmapped drive.</exception>
        /// <exception cref="FileNotFoundException"><paramref name="monitorProfilePath"/> could not be found.</exception>
        /// <exception cref="IOException">An I/O error occurred when opening the file.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public HostColorManagement(byte[] documentProfile, string monitorProfilePath) : this(documentProfile, monitorProfilePath, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HostColorManagement"/> class, with a <see cref="String"/> containing the path of the document color profile
        /// and a <see cref="String"/> containing the path of the monitor color profile.
        /// </summary>
        /// <param name="documentProfilePath">The path of the document color profile.</param>
        /// <param name="monitorProfilePath">The path of the monitor color profile.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="documentProfilePath"/> is null.
        /// or
        /// <paramref name="monitorProfilePath"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="documentProfilePath"/> is a 0 length string, contains only white-space, or contains one or more invalid characters.
        /// or
        /// <paramref name="monitorProfilePath"/> is a 0 length string, contains only white-space, or contains one or more invalid characters.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// <paramref name="documentProfilePath"/> is invalid, such as referring to an unmapped drive.
        /// or
        /// <paramref name="monitorProfilePath"/> is invalid, such as referring to an unmapped drive.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// <paramref name="documentProfilePath"/> could not be found.
        /// or
        /// <paramref name="monitorProfilePath"/> could not be found.
        /// </exception>
        /// <exception cref="IOException">An I/O error occurred when opening the file.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public HostColorManagement(string documentProfilePath, string monitorProfilePath) : this(documentProfilePath, monitorProfilePath, true)
        {
        }

        private HostColorManagement(byte[] documentProfile, string monitorProfilePath, bool monitorProfileFromUser)
        {
            if (documentProfile == null)
            {
                throw new ArgumentNullException(nameof(documentProfile));
            }

            documentProfileBytes = (byte[])documentProfile.Clone();
            monitorProfileBytes = InitializeMonitorProfile(monitorProfilePath, monitorProfileFromUser);
        }

        private HostColorManagement(string documentProfilePath, string monitorProfilePath, bool monitorProfileFromUser)
        {
            if (documentProfilePath == null)
            {
                throw new ArgumentNullException(nameof(documentProfilePath));
            }

            documentProfileBytes = ReadProfileFromFile(documentProfilePath);
            monitorProfileBytes = InitializeMonitorProfile(monitorProfilePath, monitorProfileFromUser);
        }

        private static byte[] InitializeMonitorProfile(string monitorProfilePath, bool monitorProfileFromUser)
        {
            // Only throw an exception if the monitor profile is from the user.
            // If the profile is not from the user the embedded sRGB profile will be used for a null or empty string.
            if (monitorProfilePath == null && monitorProfileFromUser)
            {
                throw new ArgumentNullException(nameof(monitorProfilePath));
            }

            byte[] profileBytes = null;

            if (!monitorProfileFromUser && string.IsNullOrEmpty(monitorProfilePath))
            {
                // If the OS does not have a monitor profile set, use the embedded sRGB resource.
                using (Stream stream = typeof(HostColorManagement).Assembly.GetManifestResourceStream("PSFilterHostDll.Resources.sRGB.icm"))
                {
                    int length = (int)stream.Length;

                    profileBytes = new byte[length];

                    int numBytesToRead = length;
                    int numBytesRead = 0;
                    do
                    {
                        int n = stream.Read(profileBytes, numBytesRead, numBytesToRead);
                        numBytesRead += n;
                        numBytesToRead -= n;
                    } while (numBytesToRead > 0);
                }
            }
            else
            {
                profileBytes = ReadProfileFromFile(monitorProfilePath);
            }

            return profileBytes;
        }

        private static byte[] ReadProfileFromFile(string path)
        {
            return File.ReadAllBytes(path);
        }

        /// <summary>
        /// Gets the document profile without copying the byte array.
        /// </summary>
        /// <returns>>A byte array that contains the color profile of the document.</returns>
        internal byte[] GetDocumentProfileReadOnly()
        {
            return documentProfileBytes;
        }

        /// <summary>
        /// Gets a byte array containing the document color profile.
        /// </summary>
        /// <returns>>A byte array that contains the color profile of the document.</returns>
        public byte[] GetDocumentColorProfile()
        {
            return (byte[])documentProfileBytes.Clone();
        }

        /// <summary>
        /// Gets the monitor profile without copying the byte array.
        /// </summary>
        /// <returns>>A byte array that contains the color profile of the monitor.</returns>
        internal byte[] GetMonitorProfileReadOnly()
        {
            return monitorProfileBytes;
        }

        /// <summary>
        /// Gets a byte array containing the monitor color profile.
        /// </summary>
        /// <returns>A byte array that contains the color profile of the monitor.</returns>
        public byte[] GetMonitorProfile()
        {
            return (byte[])monitorProfileBytes.Clone();
        }
    }
}
