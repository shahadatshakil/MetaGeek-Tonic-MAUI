using MetaGeek.WiFi.Core.Models;
using System.Collections.Generic;

namespace MetaGeek.WiFi.Core.Interfaces
{
    /// <summary>
    /// Interface to be implemented by classes that handle scanning
    /// bool return values indicate whether scanner service is able to also scan all channels
    /// </summary>
    public interface IPacketScannerService
    {
        bool ItsInitializedFlag { get; }

        void SetDataSourceInfo(DataSourceInfo dataSourceInfo);

        void ScanAllChannels();

        void ScanEssid(IEssidDetails essid);

        void MonitorBssid(IBssidDetails bssid);

        void MonitorClient(IClientDetails client);
        
        List<uint> GetCurrentScanningChannels();

        void UpdateCurrentScanningChannelsList(List<uint> channelList);

        void StopScanning();
    }
}
