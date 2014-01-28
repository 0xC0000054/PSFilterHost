/////////////////////////////////////////////////////////////////////////////////
//
// Adobe(R) Photoshop(R) filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2014 Nicholas Hayes
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
            return ns.StringLogicalComparer.Compare(x.Text, y.Text);
        }
    }

}
