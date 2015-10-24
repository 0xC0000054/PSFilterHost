﻿/////////////////////////////////////////////////////////////////////////////////
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

/* Adapted from PITypes.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/
using System.Runtime.InteropServices;
namespace PSFilterHostDll.PSApi
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct VRect : System.IEquatable<VRect>
    {
        public int top;
        public int left;
        public int bottom;
        public int right;

        public override bool Equals(object obj)
        {
            if (obj is VRect)
            {
                return Equals((VRect)obj);
            }

            return false;
        }

        public bool Equals(VRect rect)
        {
            return (this.left == rect.left && this.top == rect.top && this.right == rect.right && this.bottom == rect.bottom);
        }

        public override int GetHashCode()
        {
            return (this.left.GetHashCode() ^ this.top.GetHashCode() ^ this.bottom.GetHashCode() ^ this.right.GetHashCode());
        }

        public static bool operator ==(VRect left, VRect right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VRect left, VRect right)
        {
            return !left.Equals(right);
        }

#if DEBUG
        public override string ToString()
        {
            return ("Top=" + this.top.ToString() + ",Bottom=" + this.bottom.ToString() + ",Left=" + this.left.ToString() + ",Right=" + this.right.ToString());
        }
#endif
    }
}