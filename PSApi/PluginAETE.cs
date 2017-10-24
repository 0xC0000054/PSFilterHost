/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2017 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterHostDll.PSApi
{
    [Serializable]
    internal sealed class AETEParameter
    {
        public string name;
        public uint key;
        public uint type;
        public string desc;
        public short flags;

        public AETEParameter(string name, uint key, uint type, string description, short flags)
        {
            this.name = name;
            this.key = key;
            this.type = type;
            this.desc = description;
            this.flags = flags;
        }
    }

    [Serializable]
    internal sealed class AETEEnums
    {
        public uint type;
        public short count;
        public AETEEnum[] enums;

        public AETEEnums(uint type, short count, AETEEnum[] enums)
        {
            this.type = type;
            this.count = count;
            this.enums = enums;
        }
    }

    [Serializable]
    internal sealed class AETEEnum
    {
        public string name;
        public uint type;
        public string desc;

        public AETEEnum(string name, uint type, string description)
        {
            this.name = name;
            this.type = type;
            this.desc = description;
        }
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

        public PluginAETE(int major, int minor, short suiteLevel, short suiteVersion, AETEEvent scriptEvent)
        {
            this.major = major;
            this.minor = minor;
            this.suiteLevel = suiteLevel;
            this.suiteVersion = suiteVersion;
            this.scriptEvent = scriptEvent;
        }
    }
}
