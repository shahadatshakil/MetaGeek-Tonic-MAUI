using MetaGeek.WiFi.Core.Models;
using System;
using System.Collections.Generic;

namespace MetaGeek.WiFi.Core.Interfaces
{
/// <summary>
/// Monolith packet list interface
/// </summary>
    public interface IPacketListService
    {
        LinkedListNode<PacketMetaData> AddPacket(PacketMetaData packet);

        void AddEvent(WiFiEventMetaData wifiEvent);
        List<WiFiEventMetaData> GetEventsCollection();
        List<WiFiEventMetaData> GetEventsCollection(IClientDetails client);
        List<WiFiEventMetaData> GetEventsCollection(IBssidDetails bssid);
        List<WiFiEventMetaData> GetEventsCollection(IEssidDetails essid);

        List<PacketMetaData> GetPacketsForEvent(WiFiEventMetaData wiFiEvent, uint packetsToReturn);

        PacketMetaData[] GetAllPackets();

        void TrimListsToTime(DateTime trimDateTime);
    }
}
