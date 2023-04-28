namespace MetaGeek.WiFi.Core.Models
{
    public class Channel
    {
        #region Properties

        public float ItsCenterFreqMhz
        {
            get;
            private set;
        }

        public uint ItsChannelNumber
        {
            get;
            private set;
        }

        public float ItsChannelWidthMhz
        {
            get;
            private set;
        }

        public float ItsMaxFreqMhz
        {
            get;
            private set;
        }

        public float ItsMinFreqMhz
        {
            get;
            private set;
        }

        #endregion Properties

        #region Constructors

        public Channel(uint channelNumber, float centerFreqMhz, float channelWidth)
        {
            ItsChannelNumber = channelNumber;
            ItsCenterFreqMhz = centerFreqMhz;
            ItsChannelWidthMhz = channelWidth;
            ItsMinFreqMhz = centerFreqMhz - (channelWidth / 2.0f);
            ItsMaxFreqMhz = centerFreqMhz + (channelWidth / 2.0f);
        }

        #endregion Constructors
    }
}