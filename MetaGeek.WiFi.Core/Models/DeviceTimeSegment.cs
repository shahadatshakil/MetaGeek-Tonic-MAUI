using System;
using System.Collections.Generic;
using MetaGeek.WiFi.Core.Enums;
using MetaGeek.WiFi.Core.Resources;

namespace MetaGeek.WiFi.Core.Models
{
    public class DeviceTimeSegment : TimeSegment
    {
        #region Fields
        private const int MAX_SUBFRAME_TYPE = 64;

        private double _airtimeUsec;

        private int _clientTransmitCount;
        private int _rssiSegmentTotal;
        private int _noiseSegmentTotal;

        #endregion

        #region Properties

        public List<FrameTypeStats> ItsFrameTypeStats { get; private set; }

        public double? ItsMaxClientRate { get; set; }

        public double ItsMaxBssidRate { get; set; }

        public ushort? ItsMaxClientMcs { get; set; }

        public ushort ItsMaxBssidMcs { get; set; }

        public ushort ItsSpatialStreams { get; set; }

        public ChannelWidth ItsChannelWidth { get; set; }

        public int? ItsRssi { get; set; }

        public int? ItsNoise { get; set; }

        public double? ItsSNR { get; set; }

        public int? ItsBssidRssi { get; set; }

        #endregion

        #region Constructors

        public DeviceTimeSegment()
        {
        }

        #endregion

        #region Methods
        public void AddPacket(PacketMetaData packet, bool clientTransmitted)
        {
            var frameType = packet.ItsFrameType;

            var stats = GetStatsForThisFrameType(frameType);

            stats.ItsPacketCount++;
            stats.ItsAirTime += packet.ItsAirTimeUsec;
            ItsPacketCount++;
            _airtimeUsec += packet.ItsAirTimeUsec;

            // Check data packets for retries
            if ((frameType & 0x0F) == 0x08)
            {
                ItsDataPacketCount++;
                if ((packet.ItsPacketBytes[1] & 0x08) == 0x08)
                {
                    ItsRetryCount++;
                }
            }

            if (clientTransmitted)
            {
                _clientTransmitCount++;
                _rssiSegmentTotal += packet.ItsSignal;
                _noiseSegmentTotal += packet.ItsNoise;

                if ((!ItsMaxClientMcs.HasValue && packet.ItsMcsIndex > 0) || (ItsMaxClientMcs.HasValue && packet.ItsMcsIndex > ItsMaxClientMcs.Value))
                {
                    ItsMaxClientMcs = packet.ItsMcsIndex;
                }
                if (packet.ItsRate > ItsMaxClientRate.GetValueOrDefault())
                {
                    ItsMaxClientRate = packet.ItsRate;
                }
            }
            else
            {
                if (packet.ItsMcsIndex > ItsMaxBssidMcs)
                {
                    ItsMaxBssidMcs = packet.ItsMcsIndex;
                }
                if (packet.ItsRate > ItsMaxBssidRate)
                {
                    ItsMaxBssidRate = packet.ItsRate;
                }
            }

            if (packet.ItsSpatialStreams > ItsSpatialStreams)
            {
                ItsSpatialStreams = packet.ItsSpatialStreams;
            }

            if (packet.ItsChannelWidth > ItsChannelWidth)
            {
                ItsChannelWidth = packet.ItsChannelWidth;
            }

            if (packet.ItsSNR != null)
            {
                ItsSNR = packet.ItsSNR;
            }
        }

        public void AddInferredDataPacket(double inferredAirtime)
        {
            var stats = GetStatsForThisFrameType(FrameSubType.InferredData);

            stats.ItsPacketCount++;
            stats.ItsAirTime += inferredAirtime;
        }

        public void FinalizeTimeSegment(TimeSpan scanTime)
        {
            if (scanTime == TimeSpan.Zero) return;

            ItsScanTime = scanTime;
            ItsAirTime = TimeSpan.FromTicks((long)(TimeSpan.TicksPerMillisecond * (_airtimeUsec / 1000.0)));
            ItsAirTimePercentage = _airtimeUsec / 1000.0 / scanTime.TotalMilliseconds;

            ItsRssi = _clientTransmitCount > 0 ? _rssiSegmentTotal / _clientTransmitCount : (int?)null;
            ItsNoise = _clientTransmitCount > 0 ? _noiseSegmentTotal / _clientTransmitCount : (int?)null;

            if (ItsDataPacketCount > WiFIConstants.MIN_DATA_COUNT_FOR_RETRY_CALC)
            {
                ItsRetryPercentage = ItsRetryCount / (double)ItsDataPacketCount;
            }
        }

        private FrameTypeStats GetStatsForThisFrameType(uint frameType)
        {
            FrameTypeStats stats = null;
            if (ItsFrameTypeStats == null)
            {
                ItsFrameTypeStats = new List<FrameTypeStats>(MAX_SUBFRAME_TYPE);
            }
            else
            {
                stats = ItsFrameTypeStats.Find(f => f.ItsFrameType == frameType);
            }
            if (stats == null)
            {
                stats = new FrameTypeStats(frameType);
                ItsFrameTypeStats.Add(stats);
            }

            return stats;
        }
        #endregion
    }
}
