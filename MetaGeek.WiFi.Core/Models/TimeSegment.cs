using System;

namespace MetaGeek.WiFi.Core.Models
{
    public class TimeSegment
    {
        public DateTime ItsTimestamp { get; set; }
        public TimeSpan ItsAirTime { get; set; }
        public TimeSpan ItsScanTime { get; set; }
        public double? ItsAirTimePercentage { get; set; }
        public int ItsPacketCount { get; set; }

        // Retry information
        public int ItsDataPacketCount { get; set; }
        public int ItsRetryCount { get; set; }
        public double? ItsRetryPercentage { get; set; }

        public TimeSegment()
        {
        }
    }
}
