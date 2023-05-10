using MetaGeek.WiFi.Core.Enums;
using MetaGeek.WiFi.Core.Interfaces;
using MetaGeek.WiFi.Core.Services;

namespace MetaGeek.Tonic.Common.Helpers
{
    public static class NetworkChannelExtensions
    {
        public static bool IsInBand(this IChannelDetails channel, ChannelBand band)
        {
            if (channel == null) return false;
            return ((uint?)channel.ItsChannelNumber).IsInBand(band);
        }

        public static bool IsInBand(this uint? channel, ChannelBand band)
        {
            if (channel == null) return false;
            if (band == ChannelBand.Both) return true;
            var networkChannelBand = WiFiChannelFactory.DetermineChannelBand(channel.Value);
            return networkChannelBand == band;
        }
    }
}
