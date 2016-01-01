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

namespace HostTest
{
    internal sealed class CanvasDirtyChangedEventArgs : EventArgs
    {
        private readonly bool dirty;

        public bool Dirty
        {
            get
            {
                return dirty;
            }
        }

        public CanvasDirtyChangedEventArgs(bool isDirty)
        {
            this.dirty = isDirty;
        }

    }
}
