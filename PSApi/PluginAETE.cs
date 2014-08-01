/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2014 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterLoad.PSApi
{
    [Serializable]
    internal struct AETEParameter
    {
        public string name;
        public uint key;
        public uint type;
        public string desc;
        public short flags;
    }

    [Serializable]
    internal struct AETEEnums
    {
        public uint type;
        public short count;
        public AETEEnum[] enums;
    }

    [Serializable]
    internal struct AETEEnum
    {
        public string name;
        public uint type;
        public string desc;
    }

    [Serializable]
    internal sealed class AETEEvent
    {
        public string vendor;
        public string desc;
        public int eventClass;
        public int type;
        public uint replyType;
        public uint paramType;
        public short flags;
        public AETEParameter[] parameters;
        public AETEEnums[] enums;
    }

    [Serializable]
    internal sealed class PluginAETE
    {
        public int major;
        public int minor;
        public short suiteLevel;
        public short suiteVersion;
        public AETEEvent scriptEvent;

        public bool IsValid()
        {
            return (this.major == 1 && this.minor == 0 && this.suiteLevel == 1 && this.suiteVersion == 1);
        }
    } 
}
