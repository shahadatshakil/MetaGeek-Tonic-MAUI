using System;
using System.Collections.Concurrent;
using MetaGeek.WiFi.Core.Models;

namespace MetaGeek.WiFi.Core.Interfaces
{
    public interface IApRadioDetails
    {
        int ItsAccessPointId { get; set; }
        DateTime ItsFirstSeenDateTime { get; }
        ConcurrentDictionary<ulong, IBssidDetails> ItsBssidCollection { get; }
        PhyTypeInfo ItsPhyTypeInfo { get; }
        ChannelInfo ItsChannelInfo { get; }
        uint ItsMaxMcsIndex { get; }
        uint ItsSpacialStreamCount { get; }
        int? ItsMaxRssi { get; }
        double ItsAirTimePercentage { get; }
        string ItsBroadcastName { get; }
        string ItsAlias { get; }
        string ItsName { get; }
        string ItsTaxonomySignature { get; set; }
        void UpdateRadio();
        bool IsSameRadio(IBssidDetails bssid);

        /// <summary>
        /// Attempts to add BSSID to Radio IF the BSSID matches
        /// this radio (MAC, channel, etc)
        /// </summary>
        /// <param name="bssid"></param>
        /// <returns>true if added to the radio</returns>
        bool TryAddBssid(IBssidDetails bssid);

        void UpdateAlias(string bssidDetailsItsAlias);
    }
}
