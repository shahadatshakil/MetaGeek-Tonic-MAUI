using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MetaGeek.WiFi.Core.Interfaces
{
    public interface IWiFiCollectionsService
    {
        ConcurrentDictionary<ulong, IBssidDetails> ItsBssidCollection { get; }
        ConcurrentDictionary<string, IEssidDetails> ItsEssidCollection { get; }
        ConcurrentDictionary<uint, IChannelDetails> ItsChannelCollection { get; }
        ConcurrentDictionary<ulong, IClientDetails> ItsClientCollection { get; }
        List<IApRadioDetails> ItsRadioCollection { get; }

        IBssidDetails GetBssid(IMacAddress macAddress);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="macAddress"></param>
        /// <param name="addIfNotFound">Creates a new IClientDetails if one does not exist with this MAC Address</param>
        /// <returns></returns>
        IClientDetails GetClient(IMacAddress macAddress, bool addIfNotFound);

        IChannelDetails GetChannel(uint channel);

        List<IApRadioDetails> GetRadiosOnChannel(uint channel);

        void AddRadio(IApRadioDetails radio);

        void AddBssid(IBssidDetails bssid);

        /// <summary>
        /// If a hidden SSID is on a radio with a broadcast SSID
        /// name the hidden SSID after the broadcast SSID
        /// </summary>
        void AttachHiddenSsidsToKnownSsids();

        void AttachBssidToEssid(IBssidDetails bssid, bool okayToSwitchEssids = false, bool isHiddenEssid = false);

        /// <summary>
        /// Trims time segment collections in clients and BSSIDS
        /// </summary>
        /// <param name="trimDateTime">The oldest DateTime to keep</param>
        void TrimOldData(DateTime trimDateTime);

        void TrimOldCollection(DateTime trimDateTime);

        void UpdateWiFiCollectionsOnTimeSpanChanged(TimeSpan timeSpan);

        void UpdateWiFiCollectionOnTimeRangeChanged(DateTime startTime, DateTime endTime);
    }
}
