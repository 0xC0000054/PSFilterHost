/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2015 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterHostDll.BGRASurface;
using PSFilterHostDll.Properties;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.PSApi
{
    /// <summary>
    /// Contains methods for converting the image to the monitor color profile when displaying the filter preview.
    /// </summary>
    internal sealed class ColorProfileConverter : IDisposable
    {
        private SafeProfileHandle documentProfile;
        private SafeProfileHandle monitorProfile;
        private SafeTransformHandle transform;
        private bool colorCorrectionRequired;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorProfileConverter"/> class.
        /// </summary>
        public ColorProfileConverter()
        {
            this.documentProfile = null;
            this.monitorProfile = null;
            this.transform = null;
            this.colorCorrectionRequired = false;
            this.disposed = false;
        }

        /// <summary>
        /// Initializes the color profile converter.
        /// </summary>
        /// <param name="colorProfiles">The <see cref="HostColorManagement"/> containing the document and monitor profiles.</param>
        /// <exception cref="ArgumentNullException"><paramref name="colorProfiles"/> is null.</exception>
        public void Initialize(HostColorManagement colorProfiles)
        {
            if (colorProfiles == null)
            {
                throw new ArgumentNullException("colorProfiles");
            }

            int result = OpenColorProfile(colorProfiles.GetDocumentProfileReadOnly(), out this.documentProfile);
            if (result != NativeConstants.ERROR_SUCCESS)
            {
                HandleError(result, Resources.OpenDocumentColorProfileError);
            }

            result = OpenColorProfile(colorProfiles.GetMonitorProfileReadOnly(), out this.monitorProfile);
            if (result != NativeConstants.ERROR_SUCCESS)
            {
                HandleError(result, Resources.OpenMonitorColorProfileError);
            }

            this.colorCorrectionRequired = ColorProfilesAreDifferent();
            if (this.colorCorrectionRequired)
            {
                result = CreateColorTransform(this.documentProfile, this.monitorProfile, out this.transform);
                if (result != NativeConstants.ERROR_SUCCESS)
                {
                    HandleError(result, Resources.CreateTransformError);
                }
            }
            else
            {
                this.documentProfile.Dispose();
                this.documentProfile = null;

                this.monitorProfile.Dispose();
                this.monitorProfile = null;
            }
        }

        /// <summary>
        /// Opens a color profile from the specified byte array.
        /// </summary>
        /// <param name="profileBytes">The byte array containing the color profile.</param>
        /// <param name="handle">The <see cref="SafeProfileHandle"/> of the opened color profile.</param>
        /// <returns>A Win32 error code indicating if the profile was opened successfully.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="profileBytes" /> is null.</exception>
        private static int OpenColorProfile(byte[] profileBytes, out SafeProfileHandle handle)
        {
            if (profileBytes == null)
            {
                throw new ArgumentNullException("profileBytes");
            }

            handle = null;
            int error = NativeConstants.ERROR_SUCCESS;

            unsafe
            {
                fixed (byte* ptr = profileBytes)
                {
                    NativeStructs.Mscms.PROFILE profile = new NativeStructs.Mscms.PROFILE
                    {
                        dwType = NativeEnums.Mscms.ProfileType.MemoryBuffer,
                        pProfileData = (void*)ptr,
                        cbDataSize = (uint)profileBytes.Length
                    };

                    handle = UnsafeNativeMethods.Mscms.OpenColorProfileW(
                        ref profile,
                        NativeEnums.Mscms.ProfileAccess.Read,
                        NativeEnums.FileShare.Read,
                        NativeEnums.CreateDisposition.OpenExisting
                        );

                    if (handle == null || handle.IsInvalid)
                    {
                        error = Marshal.GetLastWin32Error();
                    }
                }
            }

            return error;
        }

        /// <summary>
        /// Creates the transform for converting between the specified color profiles.
        /// </summary>
        /// <param name="input">The input color profile.</param>
        /// <param name="output">The output color profile.</param>
        /// <param name="handle">The handle to the transform for the specified profiles.</param>
        /// <returns>A Win32 error code indicating if the transform was successfully created.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> is null.
        /// or
        /// <paramref name="output"/> is null.
        /// </exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Reliability",
            "CA2001:AvoidCallingProblematicMethods",
            Justification = "SafeHandle.DangerousGetHandle() is required as the runtime does not support marshaling SafeHandle arrays.")]
        private static int CreateColorTransform(SafeProfileHandle input, SafeProfileHandle output, out SafeTransformHandle handle)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
#if DEBUG
            System.Diagnostics.Debug.Assert(!input.IsInvalid, "Input handle is invalid.");
            System.Diagnostics.Debug.Assert(!output.IsInvalid, "Output handle is invalid.");
#endif

            handle = null;

            int error = NativeConstants.ERROR_SUCCESS;

            bool inputNeedsRelease = false;
            bool outputNeedsRelease = false;
            System.Runtime.CompilerServices.RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                input.DangerousAddRef(ref inputNeedsRelease);
                output.DangerousAddRef(ref outputNeedsRelease);

                IntPtr[] profiles = new IntPtr[2] { input.DangerousGetHandle(), output.DangerousGetHandle() };
                uint[] intents = new uint[2]
                {
                    (uint)NativeEnums.Mscms.RenderingIntent.Perceptual,
                    (uint)NativeEnums.Mscms.RenderingIntent.Perceptual
                };

                handle = UnsafeNativeMethods.Mscms.CreateMultiProfileTransform(
                    profiles,
                    (uint)profiles.Length,
                    intents,
                    (uint)intents.Length,
                    NativeEnums.Mscms.TransformFlags.BestMode,
                    NativeConstants.CMM_FROM_PROFILE
                    );

                if (handle == null || handle.IsInvalid)
                {
                    error = Marshal.GetLastWin32Error();
                }
            }
            finally
            {
                if (inputNeedsRelease)
                {
                    input.DangerousRelease();
                }
                if (outputNeedsRelease)
                {
                    output.DangerousRelease();
                }
            }

            return error;
        }

        /// <summary>
        /// Determines whether the document and monitor use different color profiles.
        /// </summary>
        /// <returns><c>true</c> if the document and monitor use different color profiles; otherwise, <c>false</c>.</returns>
        private bool ColorProfilesAreDifferent()
        {
#if DEBUG
            System.Diagnostics.Debug.Assert(this.documentProfile != null && !this.documentProfile.IsInvalid, "Document profile is null or invalid.");
            System.Diagnostics.Debug.Assert(this.monitorProfile != null && !this.monitorProfile.IsInvalid, "Monitor profile is null or invalid.");
#endif
            bool result = false;

            NativeStructs.Mscms.PROFILEHEADER header1;
            NativeStructs.Mscms.PROFILEHEADER header2;

            if (UnsafeNativeMethods.Mscms.GetColorProfileHeader(this.documentProfile, out header1) &&
                UnsafeNativeMethods.Mscms.GetColorProfileHeader(this.monitorProfile, out header2))
            {
                result = (
                    header1.phSize != header2.phSize ||
                    header1.phCMMType != header2.phCMMType ||
                    header1.phVersion != header2.phVersion ||
                    header1.phClass != header2.phClass ||
                    header1.phDataColorSpace != header2.phDataColorSpace ||
                    header1.phConnectionSpace != header2.phConnectionSpace ||
                    header1.phDateTime1 != header2.phDateTime1 ||
                    header1.phDateTime2 != header2.phDateTime2 ||
                    header1.phDateTime3 != header2.phDateTime3 ||
                    header1.phSignature != header2.phSignature ||
                    header1.phPlatform != header2.phPlatform ||
                    header1.phProfileFlags != header2.phProfileFlags ||
                    header1.phManufacturer != header2.phManufacturer ||
                    header1.phModel != header2.phModel ||
                    header1.phAttributes1 != header2.phAttributes1 ||
                    header1.phAttributes2 != header2.phAttributes2 ||
                    header1.phRenderingIntent != header2.phRenderingIntent ||
                    header1.phIlluminantX != header2.phIlluminantX ||
                    header1.phIlluminantY != header2.phIlluminantY ||
                    header1.phIlluminantZ != header2.phIlluminantZ ||
                    header1.phCreator != header2.phCreator
                    );
            }

            return result;
        }

        private void HandleError(int win32Error, string message)
        {
            Dispose();

            string win32Message = new Win32Exception(win32Error).Message;

            throw new FilterRunException(message + Environment.NewLine + win32Message);
        }

        /// <summary>
        /// Gets a value indicating whether color correction is required.
        /// </summary>
        /// <value>
        /// <c>true</c> if color correction is required; otherwise, <c>false</c>.
        /// </value>
        public bool ColorCorrectionRequired
        {
            get
            {
                return this.colorCorrectionRequired;
            }
        }

        /// <summary>
        /// Performs color correction on the specified 8-bit gray scale image data and converts it to RGB.
        /// </summary>
        /// <param name="srcPtr">The pointer to the start of the gray scale image data.</param>
        /// <param name="srcStride">The stride of the gray scale image data.</param>
        /// <param name="destSurface">The destination surface.</param>
        /// <returns><c>true</c> if the image data was successfully converted; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="destSurface"/> is null.</exception>
        public bool ColorCorrectGrayScale(IntPtr srcPtr, int srcStride, Surface32 destSurface)
        {
            if (destSurface == null)
            {
                throw new ArgumentNullException("destSurface");
            }

            if (this.transform == null)
            {
                return false;
            }

            return UnsafeNativeMethods.Mscms.TranslateBitmapBits(
                this.transform,
                srcPtr,
                NativeEnums.Mscms.BMFORMAT.BM_GRAY,
                (uint)destSurface.Width,
                (uint)destSurface.Height,
                (uint)srcStride,
                destSurface.Scan0.Pointer,
                NativeEnums.Mscms.BMFORMAT.BM_xBGRQUADS,
                (uint)destSurface.Stride,
                IntPtr.Zero,
                IntPtr.Zero
                );
        }

        /// <summary>
        /// Performs color correction on the specified 8-bit BGRA image data.
        /// </summary>
        /// <param name="sourceSurface">The source surface.</param>
        /// <param name="destinationSurface">The destination surface.</param>
        /// <returns><c>true</c> if the image data was successfully converted; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="sourceSurface"/> is null.
        /// or
        /// <paramref name="destinationSurface"/> is null.
        /// </exception>
        public bool ColorCorrectBGRASurface(Surface32 sourceSurface, Surface32 destinationSurface)
        {
            if (sourceSurface == null)
            {
                throw new ArgumentNullException("sourceSurface");
            }
            if (destinationSurface == null)
            {
                throw new ArgumentNullException("destinationSurface");
            }

            if (this.transform == null)
            {
                return false;
            }

            return UnsafeNativeMethods.Mscms.TranslateBitmapBits(
                this.transform,
                sourceSurface.Scan0.Pointer,
                NativeEnums.Mscms.BMFORMAT.BM_xRGBQUADS,
                (uint)sourceSurface.Width,
                (uint)sourceSurface.Height,
                (uint)sourceSurface.Stride,
                destinationSurface.Scan0.Pointer,
                NativeEnums.Mscms.BMFORMAT.BM_xRGBQUADS,
                (uint)destinationSurface.Stride,
                IntPtr.Zero,
                IntPtr.Zero
                );
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!this.disposed)
            {
                if (this.documentProfile != null)
                {
                    this.documentProfile.Dispose();
                    this.documentProfile = null;
                }
                if (this.monitorProfile != null)
                {
                    this.monitorProfile.Dispose();
                    this.monitorProfile = null;
                }
                if (this.transform != null)
                {
                    this.transform.Dispose();
                    this.transform = null;
                }

                this.disposed = true;
            }
        }
    }
}
