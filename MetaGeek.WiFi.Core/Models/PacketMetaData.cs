using MetaGeek.WiFi.Core.Enums;
using MetaGeek.WiFi.Core.Interfaces;
using System;
using System.Collections.Generic;


namespace MetaGeek.WiFi.Core.Models
{
    public class PacketMetaData
    {
        private DateTime _dateTime = DateTime.MinValue;

        #region Properties

        public byte[] ItsPacketBytes { get; set; }
        public IMacAddress ItsRxAddress { get; set; }
        public IMacAddress ItsTxAddress { get; set; }
        public IClientDetails ItsClient { get; set; }
        public bool ItsFromClientFlag { get; set; }
        public string ItsSSID { get; set; }

        public double ItsRate { get; set; }
        public int ItsLength { get; set; } 
        public int ItsOriginalLength { get; set; }
        public uint ItsChannel { get; set; }
        public int ItsSignal { get; set; }
        public int ItsNoise { get; set; }
        public double ItsAirTimeUsec { get; set; }
        public double ItsDurationTimeUsec { get; set; }
        public ChannelScanInfo ItsChannelScanInfo { get; set; }
        public uint ItsFrameType { get; set; }

        public ushort ItsMcsIndex { get; set; }

        public ushort ItsSpatialStreams { get; set; }

        public ChannelWidth ItsChannelWidth { get; set; }

        public bool ItsShortGuardFlag { get; set; }

        public DateTime ItsDateTime { get; set; }

        public string ItsContextualLabel { get; set; }

        public string ItsContextualInfo { get; set; }

        public bool ItsRetryFlag { get; set; }

        public byte ItsAdapterId { get; set; }

        public int? ItsStatusCode { get; set; }

        public int? ItsReasonCode { get; set; }

        public bool? ItsPowerManagementFlag { get; set; }

        public List<string> ItsBSSIDs { get; set; }

        public double? ItsSNR { get; set; }

        public EapResponseCodes ItsEapResponseCode { get; set; }

        #endregion

        #region Constructors

        public PacketMetaData(){}

        public PacketMetaData(byte[] packetBytes, double rate, int length, int originalLength, uint channel, int signal, int noise, uint seconds, uint microSeconds)
        {
            ItsSignal = signal;
            ItsNoise = noise;
            ItsPacketBytes = packetBytes;
            ItsRate = rate;
            ItsLength = length;
            ItsOriginalLength = originalLength;
            ItsChannel = channel;
            ItsDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(seconds)
                .AddMilliseconds(microSeconds / 1000);
        }

        public PacketMetaData(PacketMetaData packet)
        {
            this.ItsRate = packet.ItsRate;
            this.ItsNoise = packet.ItsNoise;
            this.ItsSignal = packet.ItsSignal;
            this.ItsShortGuardFlag = packet.ItsShortGuardFlag;
            this.ItsChannel = packet.ItsChannel;
            this.ItsChannelWidth = packet.ItsChannelWidth;
            this.ItsChannelScanInfo = packet.ItsChannelScanInfo;
            this.ItsAdapterId = packet.ItsAdapterId;
            this.ItsMcsIndex = packet.ItsMcsIndex;
            this.ItsSpatialStreams = packet.ItsSpatialStreams;
            this.ItsDateTime = packet.ItsDateTime;
            this.ItsOriginalLength = packet.ItsOriginalLength;
            this.ItsLength = packet.ItsLength;
            this.ItsPacketBytes = packet.ItsPacketBytes;
            this.ItsFrameType = packet.ItsFrameType;
        }

        #endregion

        #region Methods

        public PacketMetaData Clone()
        {
            return new PacketMetaData(this);
        }

        #endregion
    }
}