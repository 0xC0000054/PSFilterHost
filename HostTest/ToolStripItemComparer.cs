/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2015 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace HostTest
{
    class ToolStripItemComparer : IComparer<ToolStripItem>
    {
        public int Compare(ToolStripItem x, ToolStripItem y)
        {
            return StringLogicalComparer.Compare(x.Text, y.Text);
        }
    }

}
