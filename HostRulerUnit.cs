/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2014 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

namespace PSFilterHostDll
{
    /// <summary>
    /// The ruler measurement unit used by the host application
    /// </summary>
    public enum HostRulerUnit : int
    {
        /// <summary>
        /// Measurement unit in pixels.
        /// </summary>
        Pixels = 0,
        /// <summary>
        /// Measurement unit in inches.
        /// </summary>
        Inches,
        /// <summary>
        /// Measurement unit in centimeters.
        /// </summary>
        Centimeters,
        /// <summary>
        /// Measurement unit in points.
        /// </summary>
        Points,
        /// <summary>
        /// Measurement unit in picas.
        /// </summary>
        Picas,
        /// <summary>
        /// Measurement unit in percent.
        /// </summary>
        Percent
    }
}