/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2018 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace HostTest
{
    /// <summary>
    /// This class extends the functionality of the System.Windows.Forms.MenuStrip.
    /// </summary>
    internal sealed class MenuStripEx : MenuStrip
    {
        private bool clickThrough;

        public MenuStripEx()
        {
            this.clickThrough = false;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the MenuStrip honors item clicks when its containing form does not have input focus.
        /// </summary>
        /// <remarks>Default value is false, which is the same behavior provided by the base MenuStrip class.</remarks>
        /// <value>
        ///   <c>true</c> if the MenuStrip honors item clicks when its containing form does not have input focus; otherwise, <c>false</c>.
        /// </value>
        [Description("Determines whether the MenuStrip honors item clicks when its containing form does not have input focus."), Category("Behavior"), DefaultValue(false)]
        public bool ClickThrough
        {
            get
            {
                return clickThrough;
            }
            set
            {
                clickThrough = value;
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (clickThrough && m.Msg == NativeConstants.WM_MOUSEACTIVATE && m.Result == (IntPtr)NativeConstants.MA_ACTIVATEANDEAT)
            {
                m.Result = (IntPtr)NativeConstants.MA_ACTIVATE;
            }
        }
    }
}
