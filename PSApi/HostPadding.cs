/////////////////////////////////////////////////////////////////////////////////
//
// Adobe(R) Photoshop(R) filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2013 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/* Adapted from PIGeneral.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// The padding values used by the FilterRecord inputPadding and maskPadding.
    /// </summary>
    internal static class HostPadding 
    {
        public const short plugInWantsEdgeReplication = -1;
        public const short plugInDoesNotWantPadding = -2;
        public const short plugInWantsErrorOnBoundsException = -3;
    }

}
