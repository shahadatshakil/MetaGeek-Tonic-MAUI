namespace MetaGeek.WiFi.Core.Models
{
    public class FrameTypeStats
    {
        public uint ItsFrameType { get; }
        private int _packetCount;
        private double _airTime;

        public string FrameSubtypeName { get; }

        public string FrameTypeName { get; }

        public int ItsPacketCount
        {
            get { return _packetCount; }
            set
            {
                _packetCount = value;
            }
        }

        /// <summary>
        /// Airtime in microseconds
        /// </summary>
        public double ItsAirTime
        {
            get { return _airTime; }
            set { _airTime = value; }
        }

        public FrameTypeStats(uint frameType)
        {
            ItsFrameType = frameType;
        }

    }
}
