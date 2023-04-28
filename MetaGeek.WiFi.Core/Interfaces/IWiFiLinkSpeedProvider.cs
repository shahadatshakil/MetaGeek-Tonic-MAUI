using MetaGeek.WiFi.Core.Enums;

namespace MetaGeek.WiFi.Core.Interfaces
{
    public interface IWiFiLinkSpeedProvider
    {
        WiFiConnectionStatus CanReturnLinkSpeed();
        double? GetLinkSpeedMbps();
    }
}
