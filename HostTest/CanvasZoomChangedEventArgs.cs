/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace HostTest
{
    internal sealed class CanvasZoomChangedEventArgs : EventArgs
    {
        private readonly float newScale;

        public float NewZoom
        {
            get
            {
                return newScale;
            }
        }
       
        public CanvasZoomChangedEventArgs(float scale)
        {
            this.newScale = scale;
        }
    }
}
