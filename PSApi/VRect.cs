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
            return left == rect.left && top == rect.top && right == rect.right && bottom == rect.bottom;
        }

        public override int GetHashCode()
        {
            return HashCodeHelper.GetHashCode(left, top, right, bottom);
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
            return "Top=" + top.ToString() + ",Bottom=" + bottom.ToString() + ",Left=" + left.ToString() + ",Right=" + right.ToString();
        }
#endif
    }
}