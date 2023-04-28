using System;
using MetaGeek.WiFi.Core.Models;
using System.Collections.Generic;

namespace MetaGeek.WiFi.Core.Interfaces
{
    public interface IChannelDetails
    {
        uint ItsChannelNumber { get; }
        //bool ItsCanScanChannelFlag { get; set; }
        bool ItsValidInRegionFlag { get; set; }
        bool ItsBeaconDetectedFlag { get; set; }
        int ItsNonBeaconPacketCount { get; set; }
        bool ItsIsScanningFlag { get; set; }
        // Used for grouping in the view
        int Its80MhzChannel { get; set; }
        int Its40MhzChannel { get; set; }
        double ItsAirTimePercentage { get; set; }
        double ItsAirTime { get; set; }
        float ItsCenterFreqMhz { get; }
        float ItsMinFreqMhz { get; }
        float ItsMaxFreqMhz { get; }
        int? ItsMaxRssi { get; set; }
        IBssidDetails ItsLoudestBssid { get; set; }
        string ItsLoudestSsid { get; }
        double? ItsMaxAirTimePercentage { get; set; }
        IBssidDetails ItsMaxAirTimeBssid { get; set; }
        string ItsMaxAirTimeSsid { get; }
        double? ItsSpectrumUtilization { get; set; }
        bool ItsNonErpPresentFlag { get; set; }
        int ItsClientCount { get; set; }

        List<ChannelTimeSegment> GetChannelTimeSegmentsBetweenTime(DateTime startTime, DateTime endTime);

        void ProcessPacket(PacketMetaData packet);

        ChannelTimeSegment FinalizeAirtimeSegment(DateTime startTime, TimeSpan scanTimeSpan, int clientCount);

        void TrimTimeSegmentCollection(DateTime trimDateTime);

        void UpdateSelectedTimeSpan(TimeSpan timeSpan);

        void UpdateChannelStatsBasedOnTimeRange(DateTime startTime, DateTime endTime);
    }
}
