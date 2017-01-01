/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft public License:
//   Copyright (C) 2012-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Windows.Forms;

namespace HostTest
{
    internal sealed class AbortMessageFilter : IMessageFilter
    {
        private bool escapePressed;
        private static readonly IntPtr EscapeKey = new IntPtr(VK_ESCAPE);
        private const int VK_ESCAPE = 0x1b;
        private const int WM_CHAR = 0x102;
        private const int WM_SYSKEYCHAR = 0x106;
        private const int WM_IME_CHAR = 0x286;

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_CHAR || m.Msg == WM_SYSKEYCHAR || m.Msg == WM_IME_CHAR)
            {
                this.escapePressed = (m.WParam == EscapeKey);
            }

            return false;
        }

        public AbortMessageFilter()
        {
            this.escapePressed = false;
        }

        public void Reset()
        {
            this.escapePressed = false;
        }

        public bool AbortFilterCallback()
        {
            return escapePressed;
        }
    }
}
