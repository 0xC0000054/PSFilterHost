/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2019 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace PSFilterHostDll.PSApi.PICA
{
    [Serializable]
    internal sealed class ActionDescriptorZString
    {
        private readonly string value;

        public string Value => value;

        public ActionDescriptorZString(string value)
        {
            this.value = value;
        }
    }

    internal sealed class ASZStringSuite : IASZStringSuite
    {
        private enum ZStringFormat
        {
            /// <summary>
            /// A C-style null-terminated string.
            /// </summary>
            Ansi = 0,
            /// <summary>
            /// A C-style null-terminated Unicode string.
            /// </summary>
            Unicode,
            /// <summary>
            /// A Pascal-style length prefixed string.
            /// </summary>
            Pascal
        }

        private sealed class ZString
        {
            private int refCount;
            private string data;

            /// <summary>
            /// Gets or sets the reference count.
            /// </summary>
            /// <value>
            /// The reference count.
            /// </value>
            public int RefCount
            {
                get => refCount;
                set => refCount = value;
            }

            /// <summary>
            /// Gets or sets the data.
            /// </summary>
            /// <value>
            /// The data.
            /// </value>
            public string Data
            {
                get => data;
                set => data = value;
            }

            private static unsafe string PtrToStringPascal(IntPtr ptr, int length)
            {
                if (length > 0)
                {
                    int stringLength = Marshal.ReadByte(ptr);

                    return new string((sbyte*)ptr.ToPointer(), 1, stringLength);
                }

                return string.Empty;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ZString"/> class.
            /// </summary>
            /// <param name="ptr">The pointer to the native string.</param>
            /// <param name="length">The length of the native string.</param>
            /// <param name="format">The format of the native string.</param>
            /// <exception cref="ArgumentNullException"><paramref name="ptr"/> is null.</exception>
            /// <exception cref="InvalidEnumArgumentException"><paramref name="format"/> does not specify a valid <see cref="ZStringFormat"/> value.</exception>
            /// <exception cref="OutOfMemoryException">Insufficient memory to create the ZString.</exception>
            public ZString(IntPtr ptr, int length, ZStringFormat format)
            {
                if (ptr == IntPtr.Zero)
                {
                    throw new ArgumentNullException(nameof(ptr));
                }

                refCount = 1;

                switch (format)
                {
                    case ZStringFormat.Ansi:
                        data = Marshal.PtrToStringAnsi(ptr, length).TrimEnd('\0');
                        break;
                    case ZStringFormat.Unicode:
                        data = Marshal.PtrToStringUni(ptr, length).TrimEnd('\0');
                        break;
                    case ZStringFormat.Pascal:
                        data = PtrToStringPascal(ptr, length);
                        break;
                    default:
                        throw new InvalidEnumArgumentException(nameof(format), (int)format, typeof(ZStringFormat));
                }
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ZString"/> class.
            /// </summary>
            /// <param name="data">The data.</param>
            public ZString(string data)
            {
                refCount = 1;
                this.data = data;
            }
        }

        private readonly ASZStringMakeFromUnicode makeFromUnicode;
        private readonly ASZStringMakeFromCString makeFromCString;
        private readonly ASZStringMakeFromPascalString makeFromPascalString;
        private readonly ASZStringMakeRomanizationOfInteger makeRomanizationOfInteger;
        private readonly ASZStringMakeRomanizationOfFixed makeRomanizationOfFixed;
        private readonly ASZStringMakeRomanizationOfDouble makeRomanizationOfDouble;
        private readonly ASZStringGetEmpty getEmpty;
        private readonly ASZStringCopy copy;
        private readonly ASZStringReplace replace;
        private readonly ASZStringTrimEllipsis trimEllpsis;
        private readonly ASZStringTrimSpaces trimSpaces;
        private readonly ASZStringRemoveAccelerators removeAccelerators;
        private readonly ASZStringAddRef addRef;
        private readonly ASZStringRelease release;
        private readonly ASZStringIsAllWhiteSpace isAllWhitespace;
        private readonly ASZStringIsEmpty isEmpty;
        private readonly ASZStringWillReplace willReplace;
        private readonly ASZStringLengthAsUnicodeCString lengthAsUnicodeCString;
        private readonly ASZStringAsUnicodeCString asUnicodeCString;
        private readonly ASZStringLengthAsCString lengthAsCString;
        private readonly ASZStringAsCString asCString;
        private readonly ASZStringLengthAsPascalString lengthAsPascalString;
        private readonly ASZStringAsPascalString asPascalString;

        private Dictionary<IntPtr, ZString> strings;
        private int stringsIndex;

        private static readonly IntPtr Empty = IntPtr.Zero;

        /// <summary>
        /// Initializes a new instance of the <see cref="ASZStringSuite"/> class.
        /// </summary>
        public ASZStringSuite()
        {
            makeFromUnicode = new ASZStringMakeFromUnicode(MakeFromUnicode);
            makeFromCString = new ASZStringMakeFromCString(MakeFromCString);
            makeFromPascalString = new ASZStringMakeFromPascalString(MakeFromPascalString);
            makeRomanizationOfInteger = new ASZStringMakeRomanizationOfInteger(MakeRomanizationOfInteger);
            makeRomanizationOfFixed = new ASZStringMakeRomanizationOfFixed(MakeRomanizationOfFixed);
            makeRomanizationOfDouble = new ASZStringMakeRomanizationOfDouble(MakeRomanizationOfDouble);
            getEmpty = new ASZStringGetEmpty(GetEmpty);
            copy = new ASZStringCopy(Copy);
            replace = new ASZStringReplace(Replace);
            trimEllpsis = new ASZStringTrimEllipsis(TrimEllipsis);
            trimSpaces = new ASZStringTrimSpaces(TrimSpaces);
            removeAccelerators = new ASZStringRemoveAccelerators(RemoveAccelerators);
            addRef = new ASZStringAddRef(AddRef);
            release = new ASZStringRelease(Release);
            isAllWhitespace = new ASZStringIsAllWhiteSpace(IsAllWhiteSpace);
            isEmpty = new ASZStringIsEmpty(IsEmpty);
            willReplace = new ASZStringWillReplace(WillReplace);
            lengthAsUnicodeCString = new ASZStringLengthAsUnicodeCString(LengthAsUnicodeCString);
            asUnicodeCString = new ASZStringAsUnicodeCString(AsUnicodeCString);
            lengthAsCString = new ASZStringLengthAsCString(LengthAsCString);
            asCString = new ASZStringAsCString(AsCString);
            lengthAsPascalString = new ASZStringLengthAsPascalString(LengthAsPascalString);
            asPascalString = new ASZStringAsPascalString(AsPascalString);

            strings = new Dictionary<IntPtr, ZString>(IntPtrEqualityComparer.Instance);
            stringsIndex = 0;
        }

        /// <summary>
        /// Creates the <see cref="ASZStringSuite1"/> structure.
        /// </summary>
        /// <returns>An ASZStringSuite1 structure containing the AS Z String suite callbacks.</returns>
        public ASZStringSuite1 CreateASZStringSuite1()
        {
            ASZStringSuite1 suite = new ASZStringSuite1
            {
                MakeFromUnicode = Marshal.GetFunctionPointerForDelegate(makeFromUnicode),
                MakeFromCString = Marshal.GetFunctionPointerForDelegate(makeFromCString),
                MakeFromPascalString = Marshal.GetFunctionPointerForDelegate(makeFromPascalString),
                MakeRomanizationOfInteger = Marshal.GetFunctionPointerForDelegate(makeRomanizationOfInteger),
                MakeRomanizationOfFixed = Marshal.GetFunctionPointerForDelegate(makeRomanizationOfFixed),
                MakeRomanizationOfDouble = Marshal.GetFunctionPointerForDelegate(makeRomanizationOfDouble),
                GetEmpty = Marshal.GetFunctionPointerForDelegate(getEmpty),
                Copy = Marshal.GetFunctionPointerForDelegate(copy),
                Replace = Marshal.GetFunctionPointerForDelegate(replace),
                TrimEllipsis = Marshal.GetFunctionPointerForDelegate(trimEllpsis),
                TrimSpaces = Marshal.GetFunctionPointerForDelegate(trimSpaces),
                RemoveAccelerators = Marshal.GetFunctionPointerForDelegate(removeAccelerators),
                AddRef = Marshal.GetFunctionPointerForDelegate(addRef),
                Release = Marshal.GetFunctionPointerForDelegate(release),
                IsAllWhiteSpace = Marshal.GetFunctionPointerForDelegate(isAllWhitespace),
                IsEmpty = Marshal.GetFunctionPointerForDelegate(isEmpty),
                WillReplace = Marshal.GetFunctionPointerForDelegate(willReplace),
                LengthAsUnicodeCString = Marshal.GetFunctionPointerForDelegate(lengthAsUnicodeCString),
                AsUnicodeCString = Marshal.GetFunctionPointerForDelegate(asUnicodeCString),
                LengthAsCString = Marshal.GetFunctionPointerForDelegate(lengthAsCString),
                AsCString = Marshal.GetFunctionPointerForDelegate(asCString),
                LengthAsPascalString = Marshal.GetFunctionPointerForDelegate(lengthAsPascalString),
                AsPascalString = Marshal.GetFunctionPointerForDelegate(asPascalString)
            };

            return suite;
        }

        bool IASZStringSuite.ConvertToActionDescriptor(IntPtr zstring, out ActionDescriptorZString descriptor)
        {
            descriptor = null;

            if (zstring != Empty)
            {
                ZString value;
                if (strings.TryGetValue(zstring, out value))
                {
                    descriptor = new ActionDescriptorZString(value.Data);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        IntPtr IASZStringSuite.CreateFromActionDescriptor(ActionDescriptorZString descriptor)
        {
            IntPtr newZString = Empty;

            if (descriptor != null)
            {
                newZString = GenerateDictionaryKey();
                ZString zstring = new ZString(descriptor.Value);
                strings.Add(newZString, zstring);
            }

            return newZString;
        }

        bool IASZStringSuite.ConvertToString(IntPtr zstring, out string value)
        {
            value = null;

            if (zstring == Empty)
            {
                value = string.Empty;
            }
            else
            {
                ZString item;
                if (strings.TryGetValue(zstring, out item))
                {
                    value = item.Data;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        IntPtr IASZStringSuite.CreateFromString(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            IntPtr newZString;

            if (value.Length == 0)
            {
                newZString = Empty;
            }
            else
            {
                ZString zstring = new ZString(value);
                newZString = GenerateDictionaryKey();
                strings.Add(newZString, zstring);
            }

            return newZString;
        }

        private IntPtr GenerateDictionaryKey()
        {
            stringsIndex++;

            return new IntPtr(stringsIndex);
        }

        private int MakeString(IntPtr src, UIntPtr byteCount, ref IntPtr newZString, ZStringFormat format)
        {
            if (src != IntPtr.Zero)
            {
                ulong stringLength = byteCount.ToUInt64();

                if (stringLength == 0)
                {
                    newZString = Empty;
                }
                else
                {
                    // The framework cannot create a string longer than Int32.MaxValue.
                    if (stringLength > int.MaxValue)
                    {
                        return PSError.kASOutOfMemory;
                    }

                    try
                    {
                        ZString zstring = new ZString(src, (int)stringLength, format);
                        newZString = GenerateDictionaryKey();
                        strings.Add(newZString, zstring);
                    }
                    catch (OutOfMemoryException)
                    {
                        return PSError.kASOutOfMemory;
                    }
                }

                return PSError.kASNoError;
            }

            return PSError.kASBadParameter;
        }

        private int MakeFromUnicode(IntPtr src, UIntPtr byteCount, ref IntPtr newZString)
        {
            return MakeString(src, byteCount, ref newZString, ZStringFormat.Unicode);
        }

        private int MakeFromCString(IntPtr src, UIntPtr byteCount, ref IntPtr newZString)
        {
            return MakeString(src, byteCount, ref newZString, ZStringFormat.Ansi);
        }

        private int MakeFromPascalString(IntPtr src, UIntPtr byteCount, ref IntPtr newZString)
        {
            return MakeString(src, byteCount, ref newZString, ZStringFormat.Pascal);
        }

        private int MakeRomanizationOfInteger(int value, ref IntPtr newZString)
        {
            try
            {
                ZString zstring = new ZString(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                newZString = GenerateDictionaryKey();
                strings.Add(newZString, zstring);
            }
            catch (OutOfMemoryException)
            {
                return PSError.kASOutOfMemory;
            }

            return PSError.kASNoError;
        }

        private int MakeRomanizationOfFixed(int value, short places, bool trim, bool isSigned, ref IntPtr newZString)
        {
            return PSError.kASNotImplmented;
        }

        private int MakeRomanizationOfDouble(double value, ref IntPtr newZString)
        {
            return PSError.kASNotImplmented;
        }

        private IntPtr GetEmpty()
        {
            return Empty;
        }

        private int Copy(IntPtr source, ref IntPtr zstrCopy)
        {
            if (source == Empty)
            {
                zstrCopy = Empty;
            }
            else
            {
                ZString existing;
                if (strings.TryGetValue(source, out existing))
                {
                    try
                    {
                        ZString zstring = new ZString(string.Copy(existing.Data));
                        zstrCopy = GenerateDictionaryKey();
                        strings.Add(zstrCopy, zstring);
                    }
                    catch (OutOfMemoryException)
                    {
                        return PSError.kASOutOfMemory;
                    }
                }
                else
                {
                    return PSError.kASBadParameter;
                }
            }

            return PSError.kASNoError;
        }

        private int Replace(IntPtr zstr, uint index, IntPtr replacement)
        {
            return PSError.kASNotImplmented;
        }

        private int TrimEllipsis(IntPtr zstr)
        {
            if (zstr != Empty)
            {
                ZString item;
                if (strings.TryGetValue(zstr, out item))
                {
                    string value = item.Data;

                    if (value != null && value.EndsWith("...", StringComparison.Ordinal))
                    {
                        item.Data = value.Substring(0, value.Length - 3);
                        strings[zstr] = item;
                    }
                }
                else
                {
                    return PSError.kASBadParameter;
                }
            }

            return PSError.kASNoError;
        }

        private int TrimSpaces(IntPtr zstr)
        {
            if (zstr != Empty)
            {
                ZString item;
                if (strings.TryGetValue(zstr, out item))
                {
                    string value = item.Data;

                    if (value != null)
                    {
                        item.Data = value.Trim(' ');
                        strings[zstr] = item;
                    }
                }
                else
                {
                    return PSError.kASBadParameter;
                }
            }

            return PSError.kASNoError;
        }

        private int RemoveAccelerators(IntPtr zstr)
        {
            if (zstr != Empty)
            {
                ZString item;
                if (strings.TryGetValue(zstr, out item))
                {
                    string value = item.Data;

                    if (value != null && value.IndexOf('&') >= 0)
                    {
                        try
                        {
                            StringBuilder sb = new StringBuilder(value.Length);
                            bool escapedAmpersand = false;

                            for (int i = 0; i < value.Length; i++)
                            {
                                char c = value[i];
                                if (c == '&')
                                {
                                    // Retain any ampersands that have been escaped.
                                    if (escapedAmpersand)
                                    {
                                        sb.Append("&&");
                                        escapedAmpersand = false;
                                    }
                                    else
                                    {
                                        int next = i + 1;
                                        if (next < value.Length && value[next] == '&')
                                        {
                                            escapedAmpersand = true;
                                        }
                                    }
                                }
                                else
                                {
                                    sb.Append(c);
                                }
                            }

                            item.Data = sb.ToString();
                            strings[zstr] = item;
                        }
                        catch (OutOfMemoryException)
                        {
                            return PSError.kASOutOfMemory;
                        }
                    }
                }
                else
                {
                    return PSError.kASBadParameter;
                }
            }

            return PSError.kASNoError;
        }

        private int AddRef(IntPtr zstr)
        {
            if (zstr != Empty)
            {
                ZString item;
                if (strings.TryGetValue(zstr, out item))
                {
                    item.RefCount += 1;
                    strings[zstr] = item;
                }
                else
                {
                    return PSError.kASBadParameter;
                }
            }

            return PSError.kASNoError;
        }

        private int Release(IntPtr zstr)
        {
            if (zstr != Empty)
            {
                ZString item;
                if (strings.TryGetValue(zstr, out item))
                {
                    item.RefCount -= 1;

                    if (item.RefCount == 0)
                    {
                        strings.Remove(zstr);
                        if (stringsIndex == zstr.ToInt32())
                        {
                            stringsIndex--;
                        }
                    }
                    else
                    {
                        strings[zstr] = item;
                    }
                }
                else
                {
                    return PSError.kASBadParameter;
                }
            }

            return PSError.kASNoError;
        }

        private bool IsAllWhiteSpace(IntPtr zstr)
        {
            if (zstr != Empty)
            {
                ZString item;
                if (strings.TryGetValue(zstr, out item))
                {
                    string value = item.Data;

                    if (value != null)
                    {
                        for (int i = 0; i < value.Length; i++)
                        {
                            if (!char.IsWhiteSpace(value[i]))
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        private bool IsEmpty(IntPtr zstr)
        {
            return zstr == Empty;
        }

        private bool WillReplace(IntPtr zstr, uint index)
        {
            return false;
        }

        private uint LengthAsUnicodeCString(IntPtr zstr)
        {
            if (zstr == Empty)
            {
                // If the string is empty return only the length of the null terminator.
                return 1;
            }
            else
            {
                ZString item;
                if (strings.TryGetValue(zstr, out item))
                {
                    string value = item.Data;
                    if (value != null)
                    {
                        // This method returns a length in UTF-16 characters not bytes.
                        int charLength = Encoding.Unicode.GetByteCount(value) / UnicodeEncoding.CharSize;

                        // Add the null terminator to the total length.
                        return (uint)(charLength + 1);
                    }
                }
            }

            return 0;
        }

        private int AsUnicodeCString(IntPtr zstr, IntPtr str, uint strSize, bool checkStrSize)
        {
            if (str != IntPtr.Zero)
            {
                string value = string.Empty;
                if (zstr != Empty)
                {
                    ZString item;
                    if (strings.TryGetValue(zstr, out item))
                    {
                        value = item.Data;
                    }
                    else
                    {
                        return PSError.kASBadParameter;
                    }
                }

                try
                {
                    byte[] bytes = Encoding.Unicode.GetBytes(value);

                    int lengthInChars = bytes.Length / UnicodeEncoding.CharSize;
                    int lengthWithTerminator = lengthInChars + 1;

                    if (strSize < lengthWithTerminator)
                    {
                        return PSError.kASBufferTooSmallErr;
                    }

                    Marshal.Copy(bytes, 0, str, bytes.Length);
                    Marshal.WriteInt16(str, bytes.Length, 0);
                }
                catch (OutOfMemoryException)
                {
                    return PSError.kASOutOfMemory;
                }

                return PSError.kASNoError;
            }

            return PSError.kASBadParameter;
        }

        private uint LengthAsCString(IntPtr zstr)
        {
            if (zstr == Empty)
            {
                // If the string is empty return only the length of the null terminator.
                return 1;
            }
            else
            {
                ZString item;
                if (strings.TryGetValue(zstr, out item))
                {
                    string value = item.Data;
                    if (value != null)
                    {
                        // Add the null terminator to the total length.
                        int length = Encoding.ASCII.GetByteCount(value) + 1;

                        return (uint)length;
                    }
                }
            }

            return 0;
        }

        private int AsCString(IntPtr zstr, IntPtr str, uint strSize, bool checkStrSize)
        {
            if (str != IntPtr.Zero)
            {
                string value = string.Empty;
                if (zstr != Empty)
                {
                    ZString item;
                    if (strings.TryGetValue(zstr, out item))
                    {
                        value = item.Data;
                    }
                    else
                    {
                        return PSError.kASBadParameter;
                    }
                }

                try
                {
                    byte[] bytes = Encoding.ASCII.GetBytes(value);

                    int lengthWithTerminator = bytes.Length + 1;

                    if (strSize < lengthWithTerminator)
                    {
                        return PSError.kASBufferTooSmallErr;
                    }

                    Marshal.Copy(bytes, 0, str, bytes.Length);
                    Marshal.WriteByte(str, bytes.Length, 0);
                }
                catch (OutOfMemoryException)
                {
                    return PSError.kASOutOfMemory;
                }

                return PSError.kASNoError;
            }

            return PSError.kASBadParameter;
        }

        private uint LengthAsPascalString(IntPtr zstr)
        {
            if (zstr == Empty)
            {
                // If the string is empty return only the length of the prefix byte.
                return 1;
            }
            else
            {
                ZString item;
                if (strings.TryGetValue(zstr, out item))
                {
                    string value = item.Data;
                    if (value != null)
                    {
                        // Add the length prefix byte to the total length.
                        int length = Encoding.ASCII.GetByteCount(value) + 1;

                        return (uint)length;
                    }
                }
            }

            return 0;
        }

        private int AsPascalString(IntPtr zstr, IntPtr str, uint strSize, bool checkStrSize)
        {
            if (str != IntPtr.Zero)
            {
                string value = string.Empty;
                if (zstr != Empty)
                {
                    ZString item;
                    if (strings.TryGetValue(zstr, out item))
                    {
                        value = item.Data;
                    }
                    else
                    {
                        return PSError.kASBadParameter;
                    }
                }

                try
                {
                    byte[] bytes = Encoding.ASCII.GetBytes(value);

                    int lengthWithPrefixByte = bytes.Length + 1;

                    if (strSize < lengthWithPrefixByte)
                    {
                        return PSError.kASBufferTooSmallErr;
                    }

                    Marshal.WriteByte(str, (byte)bytes.Length);
                    if (bytes.Length > 0)
                    {
                        Marshal.Copy(bytes, 0, new IntPtr(str.ToInt64() + 1L), bytes.Length);
                    }
                }
                catch (OutOfMemoryException)
                {
                    return PSError.kASOutOfMemory;
                }

                return PSError.kASNoError;
            }

            return PSError.kASBadParameter;
        }
    }
}
