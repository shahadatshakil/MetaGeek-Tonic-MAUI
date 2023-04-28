using MetaGeek.WiFi.Core.Enums;
using MetaGeek.WiFi.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MetaGeek.WiFi.Core.Interfaces
{
    public interface IBssidDetails : IBaseDeviceDetails
    {
        DateTime ItsLastFullBeaconParseTime { get; set; }
        string ItsSsid { get; set; }
        string ItsDisplaySsid { get; set; }
        IEssidDetails ItsEssid { get; set; }
        IApRadioDetails ItsRadioGroup { get; set; }
        double ItsMaxConnectedRate { get; set; }
        bool ItsConnectedFlag { get; set; }
        NetworkType ItsNetworkType { get; set; }
        string ItsCountryCode { get; set; }
        ChannelInfo ItsChannelInfo { get; set; }
        double ItsChannelAirTimePercentage { get; set; }
        int ItsClientCount { get; }
        int ItsBssClientCount { get; set; }
        ClientWiFiEvents ItsClientActions { get; set; }
        ConcurrentDictionary<ulong, IClientDetails> ItsClientCollection { get; }
        List<IMacAddress> ItsNeighborBssidMacList { get; set; }
        int ItsPacketCount { get; set; }
        bool ItsFirstSessionSeenFlag { get; set; }
        bool ItsHasRSNElement { get; set; }
        IClientDetails ItsBroadcastClient { get; }
        void AttachClient(IClientDetails clientDetails);
        void DetachClient(IClientDetails clientDetails);
        void UpdateClientsSsid();
        void ProcessScanResponse(DateTime scanTime, int rssi);
        void ProcessBeacon(PacketMetaData packet);
        void ProcessBroadcastPacket(PacketMetaData packet, bool bssidTranmitted);
        void FinalizeChannelScan(DateTime startTime, TimeSpan scanTimeSpan);
        void UpdateValuesOnTimeRangeChanged();
        void UpdateBroadcastClientTimeSpan(TimeSpan selectedTimeSpan);
        void UpdateBroadcastClientBasedOnTimeRange(DateTime startTime, DateTime endTime);
    }
}
