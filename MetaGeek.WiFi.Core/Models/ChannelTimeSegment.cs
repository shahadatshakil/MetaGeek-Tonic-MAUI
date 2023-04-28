using System;
using MetaGeek.WiFi.Core.Resources;

namespace MetaGeek.WiFi.Core.Models
{
    public class ChannelTimeSegment : TimeSegment
    {
        #region Fields
        private double _airtimeUsec;
        #endregion
        #region Properties

        public int ItsClientCount { get; set; }

        #endregion

        #region Constructors

        public ChannelTimeSegment()
        {
        }

        #endregion

        #region Methods

        public void AddPacket(PacketMetaData packet)
        {
            _airtimeUsec += packet.ItsAirTimeUsec;
            ItsPacketCount++;

            var frameType = packet.ItsFrameType;

            // Check data packets for retries
            if ((frameType & 0x0F) == 0x08)
            {
                ItsDataPacketCount++;
                if ((packet.ItsPacketBytes[1] & 0x08) == 0x08)
                {
                    ItsRetryCount++;
                }
            }
        }

        public void FinalizeTimeSegment(TimeSpan scanTime)
        {
            if (scanTime == TimeSpan.Zero) return;

            ItsScanTime = scanTime;
            ItsAirTime = TimeSpan.FromTicks((long)(TimeSpan.TicksPerMillisecond * (_airtimeUsec / 1000.0)));
            ItsAirTimePercentage = _airtimeUsec / 1000.0 / scanTime.TotalMilliseconds;


            if (ItsDataPacketCount > WiFIConstants.MIN_DATA_COUNT_FOR_RETRY_CALC)
            {
                ItsRetryPercentage = ItsRetryCount / (double)ItsDataPacketCount;
            }
        }

        #endregion
    }
}
