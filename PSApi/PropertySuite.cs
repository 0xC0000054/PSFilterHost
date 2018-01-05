/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2018 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterHostDll.Properties;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PSFilterHostDll.PSApi
{
    internal sealed class PropertySuite : IDisposable, IPropertySuite
    {
        private readonly GetPropertyProc getPropertyProc;
        private readonly SetPropertyProc setPropertyProc;
        private readonly ImageModes imageMode;
        private readonly int documentWidth;
        private readonly int documentHeight;
        private ImageMetaData imageMetaData;
        private HostInformation hostInfo;
        private int numberOfChannels;
        private string hostSerial;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertySuite"/> class.
        /// </summary>
        /// <param name="source">The source image.</param>
        /// <param name="imageMode">The image mode.</param>
#if GDIPLUS
        public PropertySuite(System.Drawing.Bitmap source, ImageModes imageMode)
#else
        public PropertySuite(System.Windows.Media.Imaging.BitmapSource source, ImageModes imageMode)
#endif

        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            this.getPropertyProc = new GetPropertyProc(PropertyGetProc);
            this.setPropertyProc = new SetPropertyProc(PropertySetProc);
            this.imageMode = imageMode;
#if GDIPLUS
            this.documentWidth = source.Width;
            this.documentHeight = source.Height;
#else
            this.documentWidth = source.PixelWidth;
            this.documentHeight = source.PixelHeight;
#endif
            this.imageMetaData = new ImageMetaData(source);
            this.hostInfo = new HostInformation();
            this.numberOfChannels = 0;
            this.hostSerial = "0";
            this.disposed = false;
        }

        /// <summary>
        /// Gets the get property callback pointer.
        /// </summary>
        /// <value>
        /// The get property callback pointer.
        /// </value>
        public IntPtr GetPropertyCallback
        {
            get
            {
                return Marshal.GetFunctionPointerForDelegate(this.getPropertyProc);
            }
        }

        /// <summary>
        /// Gets or sets the host information.
        /// </summary>
        /// <value>
        /// The host information.
        /// </value>
        /// <exception cref="ArgumentNullException">Value is null.</exception>
        public HostInformation HostInformation
        {
            get
            {
                return this.hostInfo;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                this.hostInfo = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of channels.
        /// </summary>
        /// <value>
        /// The number of channels.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">The number of channels is less than one.</exception>
        public int NumberOfChannels
        {
            get
            {
                return this.numberOfChannels;
            }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException("value", value, "The value must be at least one.");
                }

                this.numberOfChannels = value;
            }
        }

        PropertyProcs IPropertySuite.CreatePropertySuite()
        {
            PropertyProcs suite = new PropertyProcs
            {
                propertyProcsVersion = PSConstants.kCurrentPropertyProcsVersion,
                numPropertyProcs = PSConstants.kCurrentPropertyProcsCount,
                getPropertyProc = Marshal.GetFunctionPointerForDelegate(getPropertyProc),
                setPropertyProc = Marshal.GetFunctionPointerForDelegate(setPropertyProc)
            };

            return suite;
        }

        public unsafe IntPtr CreatePropertySuitePointer()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException("PropertySuite");
            }

            IntPtr propertyProcsPtr = Memory.Allocate(Marshal.SizeOf(typeof(PropertyProcs)), true);

            PropertyProcs* propertyProcs = (PropertyProcs*)propertyProcsPtr.ToPointer();
            propertyProcs->propertyProcsVersion = PSConstants.kCurrentPropertyProcsVersion;
            propertyProcs->numPropertyProcs = PSConstants.kCurrentPropertyProcsCount;
            propertyProcs->getPropertyProc = Marshal.GetFunctionPointerForDelegate(getPropertyProc);
            propertyProcs->setPropertyProc = Marshal.GetFunctionPointerForDelegate(setPropertyProc);

            return propertyProcsPtr;
        }

        public void Dispose()
        {
            if (!this.disposed)
            {
                this.disposed = true;

                if (imageMetaData != null)
                {
                    imageMetaData.Dispose();
                    imageMetaData = null;
                }
            }
        }

        private unsafe short PropertyGetProc(uint signature, uint key, int index, ref IntPtr simpleProperty, ref IntPtr complexProperty)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.PropertySuite, string.Format("Sig: {0}, Key: {1}, Index: {2}", DebugUtils.PropToString(signature), DebugUtils.PropToString(key), index));
#endif
            if (signature != PSConstants.kPhotoshopSignature)
            {
                return PSError.errPlugInPropertyUndefined;
            }

            byte[] bytes = null;

            short err = PSError.noErr;
            try
            {
                switch (key)
                {
                    case PSProperties.BigNudgeH:
                    case PSProperties.BigNudgeV:
                        simpleProperty = new IntPtr(new Fixed16(PSConstants.Properties.BigNudgeDistance).Value);
                        break;
                    case PSProperties.Caption:
                        if (IPTCData.TryCreateCaptionRecord(hostInfo.Caption, out bytes))
                        {
                            complexProperty = HandleSuite.Instance.NewHandle(bytes.Length);

                            if (complexProperty != IntPtr.Zero)
                            {
                                Marshal.Copy(bytes, 0, HandleSuite.Instance.LockHandle(complexProperty, 0), bytes.Length);
                                HandleSuite.Instance.UnlockHandle(complexProperty);
                            }
                            else
                            {
                                err = PSError.memFullErr;
                            }
                        }
                        else
                        {
                            if (complexProperty != IntPtr.Zero)
                            {
                                complexProperty = HandleSuite.Instance.NewHandle(0);
                            }
                        }
                        break;
                    case PSProperties.ChannelName:
                        if (index < 0 || index >= numberOfChannels)
                        {
                            return PSError.errPlugInPropertyUndefined;
                        }

                        string name = string.Empty;

                        if (imageMode == ImageModes.GrayScale || imageMode == ImageModes.Gray16)
                        {
                            switch (index)
                            {
                                case 0:
                                    name = Resources.GrayChannelName;
                                    break;
                            }
                        }
                        else
                        {
                            switch (index)
                            {
                                case 0:
                                    name = Resources.RedChannelName;
                                    break;
                                case 1:
                                    name = Resources.GreenChannelName;
                                    break;
                                case 2:
                                    name = Resources.BlueChannelName;
                                    break;
                                case 3:
                                    name = Resources.AlphaChannelName;
                                    break;
                            }
                        }

                        bytes = Encoding.ASCII.GetBytes(name);

                        complexProperty = HandleSuite.Instance.NewHandle(bytes.Length);
                        if (complexProperty != IntPtr.Zero)
                        {
                            Marshal.Copy(bytes, 0, HandleSuite.Instance.LockHandle(complexProperty, 0), bytes.Length);
                            HandleSuite.Instance.UnlockHandle(complexProperty);
                        }
                        else
                        {
                            err = PSError.memFullErr;
                        }
                        break;
                    case PSProperties.Copyright:
                    case PSProperties.Copyright2:
                        simpleProperty = new IntPtr(hostInfo.Copyright ? 1 : 0);
                        break;
                    case PSProperties.EXIFData:
                    case PSProperties.XMPData:

                        if (imageMetaData.Extract(out bytes, key == PSProperties.EXIFData))
                        {
                            complexProperty = HandleSuite.Instance.NewHandle(bytes.Length);
                            if (complexProperty != IntPtr.Zero)
                            {
                                Marshal.Copy(bytes, 0, HandleSuite.Instance.LockHandle(complexProperty, 0), bytes.Length);
                                HandleSuite.Instance.UnlockHandle(complexProperty);
                            }
                            else
                            {
                                err = PSError.memFullErr;
                            }
                        }
                        else
                        {
                            if (complexProperty != IntPtr.Zero)
                            {
                                // If the complexProperty is not IntPtr.Zero we return a valid zero byte handle,
                                // otherwise some filters will crash with an access violation.
                                complexProperty = HandleSuite.Instance.NewHandle(0);
                            }
                        }
                        break;
                    case PSProperties.GridMajor:
                        simpleProperty = new IntPtr(new Fixed16(PSConstants.Properties.GridMajor).Value);
                        break;
                    case PSProperties.GridMinor:
                        simpleProperty = new IntPtr(PSConstants.Properties.GridMinor);
                        break;
                    case PSProperties.ImageMode:
                        simpleProperty = new IntPtr((int)imageMode);
                        break;
                    case PSProperties.InterpolationMethod:
                        simpleProperty = new IntPtr(PSConstants.Properties.InterpolationMethod.NearestNeghbor);
                        break;
                    case PSProperties.NumberOfChannels:
                        simpleProperty = new IntPtr(numberOfChannels);
                        break;
                    case PSProperties.NumberOfPaths:
                        simpleProperty = new IntPtr(0);
                        break;
                    case PSProperties.PathName:
                        if (complexProperty != IntPtr.Zero)
                        {
                            complexProperty = HandleSuite.Instance.NewHandle(0);
                        }
                        break;
                    case PSProperties.WorkPathIndex:
                    case PSProperties.ClippingPathIndex:
                    case PSProperties.TargetPathIndex:
                        simpleProperty = new IntPtr(PSConstants.Properties.NoPathIndex);
                        break;
                    case PSProperties.RulerUnits:
                        simpleProperty = new IntPtr((int)hostInfo.RulerUnit);
                        break;
                    case PSProperties.RulerOriginH:
                    case PSProperties.RulerOriginV:
                        simpleProperty = new IntPtr(new Fixed16(0).Value);
                        break;
                    case PSProperties.Watermark:
                        simpleProperty = new IntPtr(hostInfo.Watermark ? 1 : 0);
                        break;
                    case PSProperties.SerialString:
                        bytes = Encoding.ASCII.GetBytes(hostSerial);
                        complexProperty = HandleSuite.Instance.NewHandle(bytes.Length);

                        if (complexProperty != IntPtr.Zero)
                        {
                            Marshal.Copy(bytes, 0, HandleSuite.Instance.LockHandle(complexProperty, 0), bytes.Length);
                            HandleSuite.Instance.UnlockHandle(complexProperty);
                        }
                        else
                        {
                            err = PSError.memFullErr;
                        }
                        break;
                    case PSProperties.URL:
                        if (hostInfo.Url != null)
                        {
                            bytes = Encoding.ASCII.GetBytes(hostInfo.Url.ToString());
                            complexProperty = HandleSuite.Instance.NewHandle(bytes.Length);

                            if (complexProperty != IntPtr.Zero)
                            {
                                Marshal.Copy(bytes, 0, HandleSuite.Instance.LockHandle(complexProperty, 0), bytes.Length);
                                HandleSuite.Instance.UnlockHandle(complexProperty);
                            }
                            else
                            {
                                err = PSError.memFullErr;
                            }
                        }
                        else
                        {
                            if (complexProperty != IntPtr.Zero)
                            {
                                complexProperty = HandleSuite.Instance.NewHandle(0);
                            }
                        }
                        break;
                    case PSProperties.Title:
                    case PSProperties.UnicodeTitle:
                        string title;
                        if (!string.IsNullOrEmpty(hostInfo.Title))
                        {
                            title = hostInfo.Title;
                        }
                        else
                        {
                            title = "temp.png";
                        }

                        if (key == PSProperties.UnicodeTitle)
                        {
                            bytes = Encoding.Unicode.GetBytes(title);
                        }
                        else
                        {
                            bytes = Encoding.ASCII.GetBytes(title);
                        }
                        complexProperty = HandleSuite.Instance.NewHandle(bytes.Length);

                        if (complexProperty != IntPtr.Zero)
                        {
                            Marshal.Copy(bytes, 0, HandleSuite.Instance.LockHandle(complexProperty, 0), bytes.Length);
                            HandleSuite.Instance.UnlockHandle(complexProperty);
                        }
                        else
                        {
                            err = PSError.memFullErr;
                        }

                        break;
                    case PSProperties.WatchSuspension:
                        simpleProperty = new IntPtr(0);
                        break;
                    case PSProperties.DocumentWidth:
                        simpleProperty = new IntPtr(documentWidth);
                        break;
                    case PSProperties.DocumentHeight:
                        simpleProperty = new IntPtr(documentHeight);
                        break;
                    case PSProperties.ToolTips:
                        simpleProperty = new IntPtr(1);
                        break;
                    case PSProperties.HighDPI:
                        simpleProperty = new IntPtr(hostInfo.HighDpi ? 1 : 0);
                        break;

                    default:
                        return PSError.errPlugInPropertyUndefined;
                }
            }
            catch (OutOfMemoryException)
            {
                err = PSError.memFullErr;
            }

            return err;
        }

        private short PropertySetProc(uint signature, uint key, int index, IntPtr simpleProperty, IntPtr complexProperty)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.PropertySuite, string.Format("Sig: {0}, Key: {1}, Index: {2}", DebugUtils.PropToString(signature), DebugUtils.PropToString(key), index));
#endif
            if (signature != PSConstants.kPhotoshopSignature)
            {
                return PSError.errPlugInPropertyUndefined;
            }

            int size = 0;
            byte[] bytes = null;

            int simple = simpleProperty.ToInt32();

            try
            {
                switch (key)
                {
                    case PSProperties.BigNudgeH:
                        break;
                    case PSProperties.BigNudgeV:
                        break;
                    case PSProperties.Caption:
                        size = HandleSuite.Instance.GetHandleSize(complexProperty);
                        if (size > 0)
                        {
                            string caption = IPTCData.CaptionFromMemory(HandleSuite.Instance.LockHandle(complexProperty, 0));
                            HandleSuite.Instance.UnlockHandle(complexProperty);

                            if (!string.IsNullOrEmpty(caption))
                            {
                                hostInfo.Caption = caption;
                            }
                        }
                        break;
                    case PSProperties.Copyright:
                    case PSProperties.Copyright2:
                        hostInfo.Copyright = simple != 0;
                        break;
                    case PSProperties.EXIFData:
                    case PSProperties.XMPData:
                        break;
                    case PSProperties.GridMajor:
                        break;
                    case PSProperties.GridMinor:
                        break;
                    case PSProperties.RulerOriginH:
                        break;
                    case PSProperties.RulerOriginV:
                        break;
                    case PSProperties.URL:
                        size = HandleSuite.Instance.GetHandleSize(complexProperty);
                        if (size > 0)
                        {
                            bytes = new byte[size];
                            Marshal.Copy(HandleSuite.Instance.LockHandle(complexProperty, 0), bytes, 0, size);
                            HandleSuite.Instance.UnlockHandle(complexProperty);

                            Uri temp;
                            if (Uri.TryCreate(Encoding.ASCII.GetString(bytes, 0, size), UriKind.Absolute, out temp))
                            {
                                hostInfo.Url = temp;
                            }
                        }
                        break;
                    case PSProperties.WatchSuspension:
                        break;
                    case PSProperties.Watermark:
                        hostInfo.Watermark = simple != 0;
                        break;
                    default:
                        return PSError.errPlugInPropertyUndefined;
                }
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.noErr;
        }
    }
}
