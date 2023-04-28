using MetaGeek.WiFi.Core.Enums;
using MetaGeek.WiFi.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MetaGeek.WiFi.Core.Interfaces
{
    public interface IClientDetails : IBaseDeviceDetails
    {
        uint ItsId { get; }
        DeviceCategories ItsDeviceCategory { get; set; }
        IBssidDetails ItsBssid { get; }
        IBssidDetails ItsPreviousBssid { get; set; }
        string ItsDisplaySsid { get; set; }
        int? ItsDisplayRssi { get; set; }
        uint? ItsChannel { get; }
        List<string> ItsProbedNetworks { get; }
        string ItsChannelsString { get; }
        string ItsCapabilitiesString { get; }
        DateTime ItsConnectionDateTime { get; set; }

        /// <summary>
        /// Total packets sent and received
        /// </summary>
        int ItsPacketCount { get; }
        int ItsCurrentDataCount { get; }
        int ItsCurrentRetryCount { get; }
        double ItsConnectedRate { get; set; }
        double ItsClientTransmitRate { get; set; }
        double ItsClientReceiveRate { get; set; }
        double ItsPercentageOfBss { get; set; }
        double? ItsSNR { get; set; }
        double? ItsDisplaySNR { get; set; }
        double? ItsDisplayRetryRate { get; set; }
        int? ItsRecentMaxClientMcs { get; }
        int? ItsDisplayMaxClientMcs { get; }
        int ItsRecentSpatialStreams { get; }
        ChannelWidth ItsRecentChannelWidth { get; }

        /// <summary>
        /// timestamp of last Association Request
        /// </summary>
        DateTime ItsAssociationRequestTimestamp { get; }
        ClientWiFiEvents ItsClientActions { get; }
        ClientConnectionActivities ItsConnectionActivities { get; }
        string ItsProbedNetworksString { get; }
        string Its24GhzProbeSignature { get; set; }
        string Its5GhzProbeSignature { get; set; }
        Dictionary<uint, FrameTypeStats> ItsFrameTypeStats { get; }
        IIpNetworkInfo ItsIpNetworkInfo { get; set; }
        string ItsIpAddress { get; set; }
        bool ItsIsNewClientFlag { get; }
        double ItsCurrentAirtimePercentage { get; }
        bool ItsHasKnownCapabilities { get; set; }
        ConcurrentDictionary<ulong, AuthenticationInfo> ItsAllBssidsAuthInfoMap { get; }
        LinkedListNode<PacketMetaData> ItsLastAssociationRequestPacketNode { get; set; }
        void UpdateSelectedTimeSpan(TimeSpan timeSpan);
        DeviceTimeSegment FinalizeAirtimeSegment(DateTime startTime, TimeSpan scanTimeSpan);
        void AddClientAction(ClientWiFiEvents clientAction, DateTime timestamp);
        void AddConnectionActivity(ClientConnectionActivities activity);
        void AddProbedNetwork(string ssid);

        /// <summary>
        /// Updates packet counts, frame type stats, airtime, max RSSI, etc.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="clientTransmitted"></param>
        void ProcessPacket(PacketMetaData packet, bool clientTransmitted = false);
        void ProcessInferredDataPacket(double inferredAirtime);
        void AttachToBssid(IBssidDetails bssidDetails);
        void SaveAssociationRequestDetails(DateTime dateTime, ushort capabilities, ushort interval,
            byte[] informationElements);
        void UpdateClientStatsBasedOnTimeRange(DateTime startTime, DateTime endTime);
    }
}
