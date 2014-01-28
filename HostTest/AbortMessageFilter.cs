/////////////////////////////////////////////////////////////////////////////////
//
// Adobe(R) Photoshop(R) filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft public License:
//   Copyright (C) 2012-2014 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Windows.Forms;

namespace HostTest
{
    internal class AbortMessageFilter : IMessageFilter
    {
        private bool escapePressed = false;
        private static readonly IntPtr escapeKey = new IntPtr(0x1b);
        private const int WM_CHAR = 0x102;
        private const int WM_SYSKEYCHAR = 0x106;
        private const int WM_IME_CHAR = 0x286;

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_CHAR || m.Msg == WM_SYSKEYCHAR || m.Msg == WM_IME_CHAR)
            {
                this.escapePressed = (m.WParam == escapeKey);
            }

            return false;
        }

        public void Reset()
        {
            this.escapePressed = false;
        }

        public bool AbortFilter()
        {
            return escapePressed;
        }
    }
}
