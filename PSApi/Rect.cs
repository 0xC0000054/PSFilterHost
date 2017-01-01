/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2017 Nicholas Hayes
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
    internal struct Rect16 : System.IEquatable<Rect16>
    {
        public short top;
        public short left;
        public short bottom;
        public short right;

        public override bool Equals(object obj)
        {
            if (obj is Rect16)
            {
                Rect16 rect = (Rect16)obj;
                return Equals(rect);
            }
            
            return false;
        }
        public bool Equals(Rect16 rect)
        {
            return (this.left == rect.left && this.top == rect.top && this.right == rect.right && this.bottom == rect.bottom);
        }

        public override int GetHashCode()
        {
            return HashCodeHelper.GetHashCode(this.left, this.top, this.right, this.bottom);
        }

        public static bool operator ==(Rect16 left, Rect16 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Rect16 left, Rect16 right)
        {
            return !left.Equals(right);
        }

#if DEBUG
        public override string ToString()
        {
            return ("Top=" + this.top.ToString() + ",Bottom=" + this.bottom.ToString() + ",Left=" + this.left.ToString() + ",Right=" + this.right.ToString()); 
        }
#endif
        public static readonly Rect16 Empty = new Rect16();

    }
}
