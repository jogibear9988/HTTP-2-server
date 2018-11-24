﻿using lib.Frames;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using static lib.Bytes;

[assembly: InternalsVisibleTo("UnitTesting")]
namespace lib.HTTPObjects
{
    internal class HTTP2Frame
    {
        public const int headerSize = 9;

        #region constants
        //Frame Types
        public const byte DATA = 0x0;
        public const byte HEADERS = 0x1;
        public const byte PRIORITY_TYPE = 0x2;
        public const byte RST_STREAM = 0x3;
        public const byte SETTINGS = 0x4;
        public const byte PUSH_PROMISE = 0x5;
        public const byte PING = 0x6;
        public const byte GOAWAY = 0x7;
        public const byte WINDOW_UPDATE = 0x8;
        public const byte CONTINUATION = 0x9;

        //Flags
        public const byte NO_FLAG = 0x0;
        public const byte FLAG_END_STREAM = 0x1;
        public const byte FLAG_END_HEADERS = 0x4;
        public const byte FLAG_PADDED = 0x8;
        public const byte FLAG_PRIORITY= 0x20;
        public const byte FLAG_ACK = 0x1;

        //Settings Parameters
        public const ushort SETTINGS_HEADER_TABLE_SIZE = 0x1;
        public const ushort SETTINGS_ENABLE_PUSH = 0x2;
        public const ushort SETTINGS_MAX_CONCURRENT_STREAMS = 0x3;
        public const ushort SETTINGS_INITIAL_WINDOW_SIZE = 0x4;
        public const ushort SETTINGS_MAX_FRAME_SIZE = 0x5;
        public const ushort SETTINGS_MAX_HEADER_LIST_SIZE = 0x6;

        //Error codes
        public const byte NO_ERROR = 0x0;
        public const byte PROTOCOL_ERROR = 0x1;
        public const byte INTERNAL_ERROR = 0x2;
        public const byte FLOW_CONTROL_ERROR = 0x3;
        public const byte SETTINGS_TIMEOUT = 0x4;
        public const byte STREAM_CLOSED = 0x5;
        public const byte FRAME_SIZE_ERROR = 0x6;
        public const byte REFUSED_STREAM = 0x7;
        public const byte CANCEL = 0x8;
        public const byte COMPRESSION_ERROR = 0x9;
        public const byte CONNECT_ERROR = 0xa;
        public const byte ENHANCE_YOUR_CALM = 0xb;
        public const byte INADEQUATE_SECURITY = 0xc;
        public const byte HTTP_1_1_REQUIRED = 0xd;
        #endregion
        //Object Variables
        public const ushort MaxFrameSize = 16384; //todo fix
        private static int maxFrameSize = 16384;
        private byte[] byteArray;

        //Properties
        public int FrameLength
        {
            get
            {
                return byteArray.Length;
            }
        }
        public byte Type
        {
            get {
                return byteArray[3];
            }
            private set
            {
                byteArray[3] = value;
            }
        }
        public byte Flag
        {
            get
            {
                return byteArray[4];
            }
            private set
            {
                byteArray[4] = value;
            }
        }

        public bool FlagEndStream {
            get
            {
                return ((Flag & FLAG_END_STREAM) > 0);
            }
        }

        public bool FlagEndHeaders
        {
            get
            {
                return ((Flag & FLAG_END_HEADERS) > 0);
            }
        }

        public bool FlagPadded
        {
            get
            {
                return ((Flag & FLAG_PADDED) > 0);
            }
        }

        public bool FlagPriority
        {
            get
            {
                return ((Flag & FLAG_PRIORITY) > 0);
            }
        }

        public bool FlagAck
        {
            get
            {
                return ((Flag & FLAG_ACK) > 0);
            }
        }
        public int PayloadLength
        {
            get
            {
                return ConvertFromIncompleteByteArray(GetPartOfByteArray(0, 3));
            }
        }
        public int StreamIdentifier
        {
            get
            {
                return ConvertFromIncompleteByteArray(GetPartOfByteArray(5, 9));
            }
        }
        public byte[] Header
        {
            get
            {
                return GetPartOfByteArray(0, 9);
            }
        }
        public byte[] Payload
        {
            get
            {
                return GetPartOfByteArray(9, 9 + this.PayloadLength);
            }
            private set
            {
                if (value.Length > maxFrameSize)
                    throw new Exception($"Cannot create frame larger than maxFrameSize, {maxFrameSize}"); //TODO error handling?
                byte[] b = ConvertToByteArray(value.Length, 3);
                for (int i = 0; i < 3; i++)
                    byteArray[i] = b[i];

                byte[] header = this.Header;
                byte[] frame = new byte[headerSize + value.Length];
                for (int i = 0; i < headerSize + value.Length; i++)
                    frame[i] = (i < headerSize) ? header[i] : value[i - headerSize];
                byteArray = frame;
            }
        }
        
        public byte[] GetBytes()
        {
            var b = new byte[byteArray.Length];
            Array.Copy(byteArray, b, byteArray.Length);
            return b;
        }

        //Constructors
        public HTTP2Frame(byte[] byteArray)
        {
            this.byteArray = byteArray;
            if(FrameLength != PayloadLength + 9)
            {
                throw new ArgumentException("Frame data is invalid");
            }

        }


        public HTTP2Frame(int streamIdentifier)
        {
            var array = new byte[headerSize];
            var streamIdentifierArr = ExtractBytes(streamIdentifier);
            for (int i = 0; i < 4; i++)
                array[5 + i] = streamIdentifierArr[i];
            this.byteArray = array;
        }

        //Add Payload
        public HTTP2Frame AddSettingsPayload((ushort identifier, uint value)[] settings, bool ack = false)
        {
            Type = SETTINGS;
            Flag = ack ? FLAG_ACK : NO_FLAG;

            var array = new byte[settings.Length * 6];
            int i = 0;
            foreach (var (identifier, value) in settings)
            {
                var item1bytes = ConvertToByteArray(identifier, 2);
                foreach (byte b in item1bytes)
                    array[i++] = b;

                var item2bytes = ConvertToByteArray(value, 4);
                foreach (byte b in item2bytes)
                    array[i++] = b;
            }
            Payload = array;
            return this;
        }
        
        public HTTP2Frame AddDataPayload(byte[] data, byte paddingLength = 0x0, bool endStream = false)
        {
            Type = DATA;
            bool padded = paddingLength > 0;
            Flag = (byte) ((padded ? FLAG_PADDED : NO_FLAG) | (endStream ? FLAG_END_STREAM : NO_FLAG));

            if (!padded)
                Payload = data;
            else
            {
                var array = new byte[1 + data.Length + paddingLength];
                array[0] = paddingLength;
                for (int i = 0; i < data.Length; i++)
                    array[i + 1] = data[i];
                Payload = array;
            }
            return this;
        }

        public HTTP2Frame AddHeaderPayload(byte[] data, byte paddingLength = 0x0, bool end_headers = false, bool end_stream = false)
        {
            byte flag = (byte)((end_stream ? FLAG_END_STREAM : NO_FLAG) | (end_headers ? FLAG_END_HEADERS : NO_FLAG) | ((paddingLength != 0x0) ? FLAG_PADDED : NO_FLAG));
            Type = HEADERS;
            Flag = flag;
            var array = new byte[data.Length + paddingLength + ((paddingLength > 0) ? 1 : 0)];
            if (paddingLength > 0)
                array[0] = paddingLength;

            for (int i = 0; i < data.Length; i++)
                array[i + ((paddingLength > 0) ? 1 : 0)] = data[i];

            Payload = array;
            return this;
        }

        public HTTP2Frame AddHeaderPayload(byte[] data, uint streamDependency, byte weight, bool exclusive, byte paddingLength = 0x0, bool end_headers = false, bool end_stream = false)
        {
            byte flag = (byte)((end_stream ? FLAG_END_STREAM : NO_FLAG) | (end_headers ? FLAG_END_HEADERS : NO_FLAG) | ((paddingLength != 0x0) ? FLAG_PADDED : NO_FLAG) | FLAG_PRIORITY);
            Type = HEADERS;
            Flag = flag;
            var array = new byte[data.Length + 5 + paddingLength + ((paddingLength > 0) ? 1 : 0)];
            int EStreamDependency = (int)(exclusive ? streamDependency | 0x80000000 : streamDependency & 0x7fffffff);
            byte[] ESDArr = ConvertToByteArray(EStreamDependency);
            int i = 0;
            if (paddingLength > 0)
                array[i++] = paddingLength;
            for (int j = 0; j < 4; j++)
                array[i++] = ESDArr[j];
            array[i++] = weight;
            for (int j = 0; j < data.Length; j++)
            {
                array[i++] = data[j];
            }
            Payload = array;
            return this;
        }
        
        public HTTP2Frame AddPriorityPayload(bool streamDependencyIsExclusive, int streamDependency, byte weight = 1)
        {
            int first32 = PutBoolAndIntTo32bitInt(streamDependencyIsExclusive, streamDependency);
            byte[] first32Arr = ConvertToByteArray(first32);
            byte[] array = new byte[5];
            for (int i = 0; i < 5; i++)
            {
                array[i] = (i < 4) ? first32Arr[i] : weight;
            }
            Payload = array;
            Flag = NO_FLAG;
            Type = PRIORITY_TYPE;
            return this;
        }

        public HTTP2Frame AddRSTStreamPayload(uint errorcode)
        {
            Flag = NO_FLAG;
            Type = RST_STREAM;
            Payload = ExtractBytes(errorcode);
            return this;
        }

        public HTTP2Frame AddPushPromisePayload(int promisedStreamId, byte[] data, byte paddingLength = 0x0, bool endHeaders = false)
        {
            bool padded = paddingLength > 0;
            Flag = (byte)((padded ? FLAG_PADDED : NO_FLAG) | (endHeaders ? FLAG_END_HEADERS : NO_FLAG));
            Type = PUSH_PROMISE;
            Payload = CombineByteArrays(((padded) ? new byte[] { paddingLength } : new byte[] { }),ExtractBytes(promisedStreamId),data);
            return this;
        }

        public HTTP2Frame AddPingPayload(long opaqueData = 0, bool ack = false)
        {
            return this.AddPingPayload(ExtractBytes(opaqueData),ack);
        }

        public HTTP2Frame AddPingPayload(byte[] opaqueData, bool ack = false)
        {
            Type = PING;
            Flag = ack? FLAG_ACK : NO_FLAG;
            Payload = opaqueData;
            return this;
        }

        public HTTP2Frame AddGoawayPayload(int lastStreamID, int errorCode, byte[] additionalDebugData)
        {
            Type = GOAWAY;
            Flag = NO_FLAG;
            Payload = CombineByteArrays(ExtractBytes(lastStreamID), ExtractBytes(errorCode), additionalDebugData);
            return this;
        }

        public HTTP2Frame AddWindowUpdateFrame(int windowSizeIncrement)
        {
            Type = WINDOW_UPDATE;
            Flag = NO_FLAG;
            Payload = ExtractBytes(windowSizeIncrement);
            return this;
        }

        public HTTP2Frame AddContinuationFrame(byte[] headerBlockFragment, bool endHeaders = false)
        {
            Type = CONTINUATION;
            Flag = endHeaders ? FLAG_END_HEADERS : NO_FLAG;
            Payload = headerBlockFragment;
            return this;
        }

        //Retrieve information
        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            StringBuilder payload = new StringBuilder();
            s.Append("Type: ");
            switch (byteArray[3])
            {
                case DATA:
                    s.AppendLine("Data");
                    break;
                case HEADERS:
                    s.AppendLine("Headers");
                    break;
                case PRIORITY_TYPE:
                    s.AppendLine("Priority");
                    break;
                case RST_STREAM:
                    s.AppendLine("RST Stream");
                    break;
                case SETTINGS:
                    s.AppendLine("Settings");
                    break;
                case PUSH_PROMISE:
                    s.AppendLine("Push Promise");
                    break;
                case PING:
                    s.AppendLine("Ping");
                    break;
                case GOAWAY:
                    s.AppendLine("GoAway");
                    break;
                case WINDOW_UPDATE:
                    s.AppendLine("Window Update");
                    break;
                case CONTINUATION:
                    s.AppendLine("Continuation");
                    break;
                default:
                    s.AppendLine("Undefined");
                    break;
            }
            s.AppendLine($"Flag: {this.Flag} => End Headers={FlagEndHeaders}, End Stream={FlagEndStream}, Ack={FlagAck}");
            s.AppendLine($"Length: {this.PayloadLength}");
            s.AppendLine($"Stream identifier: {this.StreamIdentifier}");

            return s.ToString();

        }

        public DataPayload GetDataPayloadDecoded()
        {
            if (Type != DATA)
                throw new Exception("wrong type of frame requested");
            DataPayload dp = new DataPayload();
            bool padded = ((Flag & FLAG_PADDED) > 0);
            dp.PadLength = (byte)(padded ? GetPartOfPayload(0, 1)[0] : 0x0);
            byte[] data = GetPartOfPayload(padded?1:0, padded?PayloadLength-dp.PadLength:PayloadLength);
            dp.Data = data;
            return dp;
        }

        public HeaderPayload GetHeaderPayloadDecoded()
        {
            if(Type != HEADERS)
            {
                // todo
                throw new Exception("wrong type of frame requested");
            }
            byte[] headerPayload = Payload;
            int i = 0;
            bool padded = (Flag & FLAG_PADDED) > 0;
            if (padded)
                i++;
            bool priority = (Flag & FLAG_PRIORITY) > 0;
            if (priority)
                i += 5;
            int dataLength = headerPayload.Length - i - (padded ? headerPayload[0] : 0);
            byte[] data = new byte[dataLength];
            for (int j = 0; j < dataLength; j++)
            {
                data[j] = headerPayload[i++];
            }
            HeaderPayload hp = new HeaderPayload();
            if (padded) hp.PadLength = GetPartOfPayload(0, 1)[0];
            if (priority)
            {
                var (bit32, int31) = Split32BitToBoolAnd31bitInt(ConvertFromIncompleteByteArray(GetPartOfPayload(1, 4)));
                hp.StreamDependencyIsExclusive = bit32;
                hp.StreamDependency = int31;
                hp.Weight = GetPartOfPayload(5, 6)[0];
            }
            hp.HeaderBlockFragment.Bytearray = data;
            return hp;
        }

        public PriorityPayload GetPriorityPayloadDecoded()
        {
            if(Type != PRIORITY_TYPE)
            {
                //todo
                throw new Exception("wrong type of frame requested");
            }
            var (bit32, int31) = Split32BitToBoolAnd31bitInt(ConvertFromIncompleteByteArray(GetPartOfPayload(0, 4)));
            PriorityPayload pp = new PriorityPayload
            {
                StreamDependencyIsExclusive = bit32,
                StreamDependency = int31,
                Weight = GetPartOfPayload(4, 5)[0]
            };
            return pp;
        }

        public GoAwayPayload GetGoAwayPayloadDecoded()
        {
            GoAwayPayload gp = new GoAwayPayload();
            var (bit32, int31) = Split32BitToBoolAnd31bitInt(ConvertFromIncompleteByteArray(GetPartOfPayload(0, 4)));
            gp.r = bit32;
            gp.LastStreamId = int31;
            gp.ErrorCode = ConvertFromIncompleteByteArray(GetPartOfPayload(4, 8));
            gp.AdditionalDebugData = Encoding.ASCII.GetString(GetPartOfPayload(8, PayloadLength));
            return gp;
        }

        public RSTStreamPayload GetRSTStreamPayloadDecoded()
        {
            if (Type != RST_STREAM)
                throw new Exception("wrong type of frame requested");
            var rp = new RSTStreamPayload
            {
                ErrorCode = ConvertFromIncompleteByteArrayUnsigned(Payload)
            };
            return rp;
        }

        public SettingsPayload GetSettingsPayloadDecoded()
        {
            if (Type != SETTINGS)
                throw new Exception("wrong type of frame requested");
            SettingsPayload sp = new SettingsPayload();
            byte[] payload = Payload;
            int settingsLength = payload.Length / 6;
            var settings = new(ushort identifier, uint value)[settingsLength];
            for(int i = 0; i < settingsLength;i++)
            {
                int offset = i * 6;
                ushort identifier = (ushort)ConvertFromIncompleteByteArrayUnsigned(Bytes.GetPartOfByteArray(offset, offset + 2, payload));
                uint value = ConvertFromIncompleteByteArrayUnsigned(Bytes.GetPartOfByteArray(offset+2, offset + 6, payload));
                settings[i] = (identifier, value);
            }
            sp.Settings = settings;
            return sp;
        }

        public PushPromisePayload GetPushPromisePayloadDecoded()
        {
            if(Type != PUSH_PROMISE)
                throw new Exception("wrong type of frame requested");
            
            bool padded = ((Flag & FLAG_PADDED) > 0);
            int i = padded ? 1 : 0;

            var pp = new PushPromisePayload
            {
                PadLength = (byte)(padded ? GetPartOfPayload(0, 1)[0] : 0x0),
                PromisedStreamID = ConvertFromIncompleteByteArray(GetPartOfPayload(i, i + 4))
            };
            pp.HeaderBlockFragment = GetPartOfPayload(i + 4, PayloadLength - pp.PadLength);
            return pp;
        }

        public WindowUpdatePayload GetWindowUpdatePayloadDecoded()
        {
            if (Type != WINDOW_UPDATE)
                throw new Exception("wrong type of frame requested");
            var split = Split32BitToBoolAnd31bitInt(ConvertFromIncompleteByteArray(GetPartOfPayload(0, 4)));
            WindowUpdatePayload wup = new WindowUpdatePayload
            {
                WindowSizeIncrement = split.int31
            };
            return wup;
        }

        //Static methods
        public static byte[] CombineHeaderPayloads(params HTTP2Frame[] frames)
        {
            if (frames[0].Type != HEADERS)
                throw new ArgumentException("Header must consist of 1 header frame and 0..* continuation frames. Frame #1 is not of type header");
            for (int j = 1; j < frames.Length; j++)
                if (frames[j].Type != CONTINUATION)
                    throw new ArgumentException($"Header must consist of 1 header frame and 0..* continuation frames. frame #{j + 1} is not of type continuation");
            List<byte> bytes = new List<byte>();
            byte headerFlags = frames[0].Flag;
            byte[] headerPayload = frames[0].Payload;
            int i = 0;
            bool padded = (headerFlags & FLAG_PADDED) > 0;
            if (padded)
                i++;
            if ((headerFlags & FLAG_PRIORITY) > 0)
                i += 5;
            for (; i < headerPayload.Length - (padded ? headerPayload[0] : 0); i++)
            {
                bytes.Add(headerPayload[i]);
            }

            for (int j = 1; j < frames.Length; j++)
            {
                bytes.AddRange(frames[j].Payload);
            }
            return bytes.ToArray();
        }

        public static byte[] CombineDataPayloads(params HTTP2Frame[] frames)
        {
            if (frames is null || frames.Length < 1) return new byte[0];

            List<byte> bytes = new List<byte>();
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i].Type != DATA) throw new ArgumentException("CombineDataPayloads() does not support frames of a different type than data");
                DataPayload dp = frames[i].GetDataPayloadDecoded();
                bytes.AddRange(dp.Data);
            }
            return bytes.ToArray();
        }

        private byte[] GetPartOfByteArray(int start, int end)
        {
            return Bytes.GetPartOfByteArray(start, end, byteArray);
        }

        private byte[] GetPartOfPayload(int start, int end)
        {
            if ((end + headerSize) > byteArray.Length) return null;  // todo sjekk denne
            return Bytes.GetPartOfByteArray(start + headerSize, end + headerSize, byteArray);
        }
    }
}

