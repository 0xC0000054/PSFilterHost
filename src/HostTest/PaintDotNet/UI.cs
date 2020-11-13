/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See License-pdn.txt for full licensing and attribution details.             //
//                                                                             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace PaintDotNet.SystemLayer
{
    /// <summary>
    /// Contains static methods related to the user interface.
    /// </summary>
    internal static class UI
    {
        private static bool initScales = false;
        private static float xScale;
        private static float yScale;

        private static void InitScaleFactors(Control c)
        {
            if (c == null)
            {
                xScale = 1.0f;
                yScale = 1.0f;
            }
            else
            {
                using (Graphics g = c.CreateGraphics())
                {
                    xScale = g.DpiX / 96.0f;
                    yScale = g.DpiY / 96.0f;
                }
            }

            initScales = true;
        }

        public static void InitScaling(Control c)
        {
            if (!initScales)
            {
                InitScaleFactors(c);
            }
        }

        public static float ScaleWidth(float width)
        {
            return (float)Math.Round(width * GetXScaleFactor());
        }

        public static int ScaleWidth(int width)
        {
            return (int)Math.Round((float)width * GetXScaleFactor());
        }

        public static int ScaleHeight(int height)
        {
            return (int)Math.Round((float)height * GetYScaleFactor());
        }

        public static float ScaleHeight(float height)
        {
            return (float)Math.Round(height * GetYScaleFactor());
        }

        public static Size ScaleSize(Size size)
        {
            return new Size(ScaleWidth(size.Width), ScaleHeight(size.Height));
        }

        public static Point ScalePoint(Point pt)
        {
            return new Point(ScaleWidth(pt.X), ScaleHeight(pt.Y));
        }

        public static float GetXScaleFactor()
        {
            if (!initScales)
            {
                throw new InvalidOperationException("Must call InitScaling() first");
            }

            return xScale;
        }

        public static float GetYScaleFactor()
        {
            if (!initScales)
            {
                throw new InvalidOperationException("Must call InitScaling() first");
            }

            return yScale;
        }

        public static void DrawCommandButton(
            Graphics g,
            PushButtonState state,
            Rectangle rect,
            Color backColor,
            Control childControl)
        {
            VisualStyleElement element = null;
            int alpha = 255;

            if (Environment.OSVersion.Version.Major >= 6)
            {
                const string className = "BUTTON";
                const int partID = NativeConstants.BP_COMMANDLINK;
                int stateID;

                switch (state)
                {
                    case PushButtonState.Default:
                        stateID = NativeConstants.CMDLS_DEFAULTED;
                        break;

                    case PushButtonState.Disabled:
                        stateID = NativeConstants.CMDLS_DISABLED;
                        break;

                    case PushButtonState.Hot:
                        stateID = NativeConstants.CMDLS_HOT;
                        break;

                    case PushButtonState.Normal:
                        stateID = NativeConstants.CMDLS_NORMAL;
                        break;

                    case PushButtonState.Pressed:
                        stateID = NativeConstants.CMDLS_PRESSED;
                        break;

                    default:
                        throw new InvalidEnumArgumentException();
                }

                try
                {
                    element = VisualStyleElement.CreateElement(className, partID, stateID);

                    if (!VisualStyleRenderer.IsElementDefined(element))
                    {
                        element = null;
                    }
                }
                catch (InvalidOperationException)
                {
                    element = null;
                }
            }

            if (element == null)
            {
                switch (state)
                {
                    case PushButtonState.Default:
                        element = VisualStyleElement.Button.PushButton.Default;
                        alpha = 95;
                        break;

                    case PushButtonState.Disabled:
                        element = VisualStyleElement.Button.PushButton.Disabled;
                        break;

                    case PushButtonState.Hot:
                        element = VisualStyleElement.Button.PushButton.Hot;
                        break;

                    case PushButtonState.Normal:
                        alpha = 0;
                        element = VisualStyleElement.Button.PushButton.Normal;
                        break;
                    case PushButtonState.Pressed:
                        element = VisualStyleElement.Button.PushButton.Pressed;
                        break;

                    default:
                        throw new InvalidEnumArgumentException();
                }
            }

            if (element != null)
            {
                try
                {
                    VisualStyleRenderer renderer = new VisualStyleRenderer(element);
                    renderer.DrawParentBackground(g, rect, childControl);
                    renderer.DrawBackground(g, rect);
                }
                catch (Exception)
                {
                    element = null;
                }
            }

            if (element == null)
            {
                ButtonRenderer.DrawButton(g, rect, state);
            }

            if (alpha != 255)
            {
                using (Brush backBrush = new SolidBrush(Color.FromArgb(255 - alpha, backColor)))
                {
                    CompositingMode oldCM = g.CompositingMode;

                    try
                    {
                        g.CompositingMode = CompositingMode.SourceOver;
                        g.FillRectangle(backBrush, rect);
                    }

                    finally
                    {
                        g.CompositingMode = oldCM;
                    }
                }
            }
        }

        private static VisualStyleClass DetermineVisualStyleClass()
        {
            try
            {
                return DetermineVisualStyleClassImpl();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static VisualStyleClass DetermineVisualStyleClassImpl()
        {
            VisualStyleClass vsClass;

            if (!VisualStyleInformation.IsSupportedByOS)
            {
                vsClass = VisualStyleClass.Classic;
            }
            else if (!VisualStyleInformation.IsEnabledByUser)
            {
                vsClass = VisualStyleClass.Classic;
            }
            else if (0 == string.Compare(VisualStyleInformation.Author, "MSX", StringComparison.InvariantCulture) &&
                     0 == string.Compare(VisualStyleInformation.DisplayName, "Aero style", StringComparison.InvariantCulture))
            {
                vsClass = VisualStyleClass.Aero;
            }
            else if (0 == string.Compare(VisualStyleInformation.Company, "Microsoft Corporation", StringComparison.InvariantCulture) &&
                     0 == string.Compare(VisualStyleInformation.Author, "Microsoft Design Team", StringComparison.InvariantCulture))
            {
                if (0 == string.Compare(VisualStyleInformation.DisplayName, "Windows XP style", StringComparison.InvariantCulture) ||  // Luna
                    0 == string.Compare(VisualStyleInformation.DisplayName, "Zune Style", StringComparison.InvariantCulture) ||        // Zune
                    0 == string.Compare(VisualStyleInformation.DisplayName, "Media Center style", StringComparison.InvariantCulture))  // Royale
                {
                    vsClass = VisualStyleClass.Luna;
                }
                else
                {
                    vsClass = VisualStyleClass.Other;
                }
            }
            else
            {
                vsClass = VisualStyleClass.Other;
            }

            return vsClass;
        }

        public static VisualStyleClass VisualStyleClass
        {
            get
            {
                return DetermineVisualStyleClass();
            }
        }
    }
}
