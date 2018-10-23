﻿using System;
using System.Collections.Generic;
using System.Text;

namespace lib
{
    public enum SettingId : short
    {
        HeaderTableSize = 0x1,
        EnablePush = 0x2,
        MaxConcurrentStreams = 0x3,
        InitialWindowSize = 0x4,
        MaxFrameSize = 0x5,
        MaxHeaderListSize = 0x6,
    }

    public struct Settings
    {
        public uint HeaderTableSize;
        public Boolean EnablePush;
        public uint MaxConcurrentStreams;
        public uint InitialWindowSize;
        public uint MaxFrameSize;
        public uint MaxHeaderListSize;

        public void ApplySettings((ushort identifier, uint value)[] changes)
        {
            foreach(var (identifier, value) in changes)
            {
                switch (identifier)
                {
                    case (ushort)SettingId.HeaderTableSize:
                        HeaderTableSize = value;
                        break;
                    case (ushort)SettingId.EnablePush:
                        EnablePush = value>0;
                        break;
                    case (ushort)SettingId.MaxConcurrentStreams:
                        MaxConcurrentStreams = value;
                        break;
                    case (ushort)SettingId.InitialWindowSize:
                        InitialWindowSize = value;
                        break;
                    case (ushort)SettingId.MaxFrameSize:
                        MaxFrameSize = value;
                        break;
                    case (ushort)SettingId.MaxHeaderListSize:
                        MaxHeaderListSize = value;
                        break;
                    default:
                        throw new Exception("Bad setting identifier");

                }
            }
        }


        //Default values as in RFC7540 [6.5.2]
        public readonly static Settings Default = new Settings
        {
            HeaderTableSize = 4096,
            EnablePush = true,
            MaxConcurrentStreams = uint.MaxValue,
            InitialWindowSize = 65535,
            MaxFrameSize = 16384,
            MaxHeaderListSize = uint.MaxValue,
        };
        public readonly static Settings Min = new Settings
        {
            HeaderTableSize = 0,
            EnablePush = false,
            MaxConcurrentStreams = 0,
            InitialWindowSize = 0,
            MaxFrameSize = 16384,
            MaxHeaderListSize = 0,
        };

        public readonly static Settings Max = new Settings
        {
            HeaderTableSize = uint.MaxValue,
            EnablePush = true,
            MaxConcurrentStreams = uint.MaxValue,
            InitialWindowSize = int.MaxValue,
            MaxFrameSize = 16777215,
            MaxHeaderListSize = uint.MaxValue,
        };


    }
}
