/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2021 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterHostDll.Interop;
using System;
using System.Windows.Forms;

namespace PSFilterHostDll.PSApi
{
    internal sealed class ColorPicker : ColorDialog
    {
        private readonly string title = string.Empty;
        private const int WM_INITDIALOG = 0x0110;

        protected override IntPtr HookProc(IntPtr hWnd, int msg, IntPtr wparam, IntPtr lparam)
        {
            if (msg == WM_INITDIALOG)
            {
                if (!string.IsNullOrEmpty(title))
                {
                    SafeNativeMethods.SetWindowTextW(hWnd, title);
                }
            }

            return base.HookProc(hWnd, msg, wparam, lparam);
        }

        public ColorPicker(string title)
        {
            this.title = title;
            AllowFullOpen = true;
            AnyColor = true;
            SolidColorOnly = true;
            FullOpen = true;
        }
    }
}
