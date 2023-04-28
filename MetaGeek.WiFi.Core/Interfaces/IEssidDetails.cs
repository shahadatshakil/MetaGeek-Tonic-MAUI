using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MetaGeek.WiFi.Core.Enums;
using MetaGeek.WiFi.Core.Models;

namespace MetaGeek.WiFi.Core.Interfaces
{
    public interface IEssidDetails
    {
        string ItsSsid { get; }
        bool ItsConnectedFlag { get; set; }
        Color ItsColor { get; set; }
        ConcurrentDictionary<ulong, IBssidDetails> ItsBssidCollection { get; }
        ConcurrentDictionary<ulong, IClientDetails> ItsClientCollection { get; }
        int ItsBssCount { get; set; }
        int ItsTotalClientCount { get; set; }
        ClientWiFiEvents ItsClientActions { get; set; }
        int? ItsMaxRssi { get; set; }
        double ItsMaxAirTimePercentage { get; set; }
        PhyTypeInfo ItsAggregatePhyTypeInfo { get; }
        SecurityInfo ItsAggregateSecurityInfo { get; }
        string ItsChannelsString { get; }        
        SortedList<uint, ChannelInfo> ItsChannels { get; }
        double ItsMaxDataRate { get; }
        List<double> ItsBasicRates { get; }
        int ItsPacketCount { get; set; }
        string ItsVendorsString { get; }
        bool ItsFirstSessionSeenFlag { get; set; }
        bool ItsHasNewBssidFlag { get; set; }    
        bool ItsHiddenFlag { get; set; }
        void AddBssid(IBssidDetails bssidDetails);
        void Update(DateTime scanDateTime);
        void UpdateValuesOnTimeRangeChanged();
        void TrimTimeSegmentCollection(DateTime trimDateTime);
        List<DeviceTimeSegment> GetAllTimeSegments();
        DateTime ItsLastSeenDateTime { get; set; }
    }
}
