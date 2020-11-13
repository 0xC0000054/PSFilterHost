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

namespace HostTest
{
    internal sealed class CanvasDirtyChangedEventArgs : EventArgs
    {
        public bool Dirty { get; }

        public CanvasDirtyChangedEventArgs(bool isDirty)
        {
            Dirty = isDirty;
        }
    }
}
