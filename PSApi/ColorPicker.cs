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

using System;
using System.Windows.Forms;

namespace PSFilterLoad.PSApi
{
    internal sealed class ColorPicker : ColorDialog
    {
        private string title = string.Empty;
        private const int WM_INITDIALOG = 0x0110;

        protected override IntPtr HookProc(IntPtr hWnd, int msg, IntPtr wparam, IntPtr lparam)
        {
            if (msg == WM_INITDIALOG)
            {
                if (!string.IsNullOrEmpty(title))
                {
                    SafeNativeMethods.SetWindowText(hWnd, this.title);
                }
            }

            return base.HookProc(hWnd, msg, wparam, lparam);
        }

        public ColorPicker(string title)
        {
            this.title = title;
            this.AllowFullOpen = true;
            this.AnyColor = true;
            this.SolidColorOnly = true;
            this.FullOpen = true;
        }

    }
}
