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

#if PICASUITEDEBUG
using System;
using System.Runtime.InteropServices;

namespace PSFilterHostDll.PSApi.PICA
{
    internal static class PICAColorSpaceSuite
    {
        private static CSMake csMake = new CSMake(CSMake);
        private static CSDelete csDelete = new CSDelete(CSDelete);
        private static CSStuffComponents csStuffComponent = new CSStuffComponents(CSStuffComponents);
        private static CSExtractComponents csExtractComponent = new CSExtractComponents(CSExtractComponents);
        private static CSStuffXYZ csStuffXYZ = new CSStuffXYZ(CSStuffXYZ);
        private static CSExtractXYZ csExtractXYZ = new CSExtractXYZ(CSExtractXYZ);
        private static CSConvert8 csConvert8 = new CSConvert8(CSConvert8);
        private static CSConvert16 csConvert16 = new CSConvert16(CSConvert16);
        private static CSGetNativeSpace csGetNativeSpace = new CSGetNativeSpace(CSGetNativeSpace);
        private static CSIsBookColor csIsBookColor = new CSIsBookColor(CSIsBookColor);
        private static CSExtractColorName csExtractColorName = new CSExtractColorName(CSExtractColorName);
        private static CSPickColor csPickColor = new CSPickColor(CSPickColor);
        private static CSConvert csConvert8to16 = new CSConvert(CSConvert8to16);
        private static CSConvert csConvert16to8 = new CSConvert(CSConvert16to8);

        private static int CSMake(ref IntPtr colorID)
        {
            return PSError.kSPNotImplmented;
        }
        private static int CSDelete(ref IntPtr colorID)
        {
            return PSError.kSPNotImplmented;
        }
        private static int CSStuffComponents(IntPtr colorID, ColorSpace colorSpace, byte c0, byte c1, byte c2, byte c3)
        {
            return PSError.kSPNotImplmented;
        }
        private static int CSExtractComponents(IntPtr colorID, ColorSpace colorSpace, ref byte c0, ref byte c1, ref byte c2, ref byte c3, ref byte gamutFlag)
        {
            return PSError.kSPNotImplmented;
        }
        private static int CSStuffXYZ(IntPtr colorID, CS_XYZ xyz)
        {
            return PSError.kSPNotImplmented;
        }
        private static int CSExtractXYZ(IntPtr colorID, ref CS_XYZ xyz)
        {
            return PSError.kSPNotImplmented;
        }
        private unsafe static int CSConvert8(ColorSpace inputCSpace, ColorSpace outputCSpace, IntPtr colorArray, short count)
        {
            int error = PSError.kSPNoError;
            byte c0 = 0;
            byte c1 = 0;
            byte c2 = 0;
            byte c3 = 0;
            CS_Color8* color = (CS_Color8*)colorArray.ToPointer();

            for (int i = 0; i < count; i++)
            {
                // 0RGB, CMYK, 0HSB , 0HSL, 0LAB, 0XYZ, 000Gray 
                // all modes except CMYK and GrayScale begin at the second byte
                switch (inputCSpace)
                {
                    case ColorSpace.GraySpace:
                        c0 = color->c3;
                        break;
                    case ColorSpace.CMYKSpace:
                        c0 = color->c0;
                        c1 = color->c1;
                        c2 = color->c2;
                        c3 = color->c3;
                        break;
                    case ColorSpace.RGBSpace:
                    case ColorSpace.HSBSpace:
                    case ColorSpace.HSLSpace:
                    case ColorSpace.LabSpace:
                    case ColorSpace.XYZSpace:
                    default:
                        c0 = color->c1;
                        c1 = color->c2;
                        c2 = color->c3;
                        break;
                }


                error = ColorServicesConvert.Convert(inputCSpace, outputCSpace, ref c0, ref c1, ref c2, ref c3);
                if (error != PSError.kSPNoError)
                {
                    break;
                }

                switch (outputCSpace)
                {
                    case ColorSpace.CMYKSpace:
                        color->c0 = c0;
                        color->c1 = c1;
                        color->c2 = c2;
                        color->c3 = c3;
                        break;
                    case ColorSpace.GraySpace:
                        color->c3 = c0;
                        break;
                    case ColorSpace.RGBSpace:
                    case ColorSpace.HSBSpace:
                    case ColorSpace.HSLSpace:
                    case ColorSpace.LabSpace:
                    case ColorSpace.XYZSpace:
                    default:
                        color->c1 = c0;
                        color->c2 = c1;
                        color->c3 = c2;
                        break;
                }

                color++;
            }

            return error;
        }
        private static int CSConvert16(ColorSpace inputCSpace, ColorSpace outputCSpace, IntPtr colorArray, short count)
        {
            return PSError.kSPNotImplmented;
        }
        private static int CSGetNativeSpace(IntPtr colorID, ref ColorSpace nativeSpace)
        {
            nativeSpace = 0;

            return PSError.kSPNotImplmented;
        }
        private static int CSIsBookColor(IntPtr colorID, ref byte isBookColor)
        {
            isBookColor = 0;

            return PSError.kSPNotImplmented;
        }
        private static int CSExtractColorName(IntPtr colorID, ref IntPtr colorName)
        {
            return PSError.kSPNotImplmented;
        }
        private static int CSPickColor(ref IntPtr colorID, IntPtr promptString)
        {
            return PSError.kSPNotImplmented;
        }

        private static int CSConvert8to16(IntPtr inputData, IntPtr outputData, short count)
        {
            return PSError.kSPNotImplmented;
        }

        private static int CSConvert16to8(IntPtr inputData, IntPtr outputData, short count)
        {
            return PSError.kSPNotImplmented;
        }

        public static PSColorSpaceSuite1 CreateColorSpaceSuite1()
        {
            PSColorSpaceSuite1 suite = new PSColorSpaceSuite1();
            suite.Make = Marshal.GetFunctionPointerForDelegate(csMake);
            suite.Delete = Marshal.GetFunctionPointerForDelegate(csDelete);
            suite.StuffComponents = Marshal.GetFunctionPointerForDelegate(csStuffComponent);
            suite.ExtractComponents = Marshal.GetFunctionPointerForDelegate(csExtractComponent);
            suite.StuffXYZ = Marshal.GetFunctionPointerForDelegate(csStuffXYZ);
            suite.ExtractXYZ = Marshal.GetFunctionPointerForDelegate(csExtractXYZ);
            suite.Convert8 = Marshal.GetFunctionPointerForDelegate(csConvert8);
            suite.Convert16 = Marshal.GetFunctionPointerForDelegate(csConvert16);
            suite.GetNativeSpace = Marshal.GetFunctionPointerForDelegate(csGetNativeSpace);
            suite.IsBookColor = Marshal.GetFunctionPointerForDelegate(csIsBookColor);
            suite.ExtractColorName = Marshal.GetFunctionPointerForDelegate(csExtractColorName);
            suite.PickColor = Marshal.GetFunctionPointerForDelegate(csPickColor);
            suite.Convert8to16 = Marshal.GetFunctionPointerForDelegate(csConvert8to16);
            suite.Convert16to8 = Marshal.GetFunctionPointerForDelegate(csConvert16to8);

            return suite;
        }
    }
}
#endif