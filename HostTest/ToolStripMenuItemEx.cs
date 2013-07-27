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

using System.Drawing;
using System.Windows.Forms;

namespace HostTest
{
    /// <summary>
    /// This class stops the sub-menu from expanding when the ToolStripMenuItem is disabled.
    /// </summary>
    internal class ToolStripMenuItemEx : ToolStripMenuItem
    {
        public ToolStripMenuItemEx(string text, Image icon, ToolStripItem[] dropDownItems) : base(text, icon, dropDownItems)
        {
        }

        // See https://connect.microsoft.com/VisualStudio/feedback/details/235911/possible-to-select-the-dropdownitems-of-a-disabled-toolstripmenuitem
        public override bool CanSelect
        {
            get
            {
                return base.Enabled; 
            }
        }
        
    }
}
