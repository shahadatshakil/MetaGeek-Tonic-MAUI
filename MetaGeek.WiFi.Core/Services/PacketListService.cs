using MetaGeek.WiFi.Core.Enums;
using MetaGeek.WiFi.Core.Events;
using MetaGeek.WiFi.Core.Helpers;
using MetaGeek.WiFi.Core.Interfaces;
using MetaGeek.WiFi.Core.Models;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MetaGeek.WiFi.Core.Services
{
    public class PacketListService : IPacketListService
    {
        #region Fields
        private readonly IEventAggregator _eventAggregator;

        private LinkedList<PacketMetaData> _packetList;
        private LinkedList<WiFiEventMetaData> _eventList;
        private object _packetListLock;
        private object _eventListLock;
        #endregion

        #region Properties

        // TODO THIS IS NOT THREAD-SAFE
        public LinkedList<PacketMetaData> ItsPacketList
        {
            get { return _packetList; }
        }

        // TODO THIS IS NOT THREAD-SAFE! Do we need public access to the entire list?
        public LinkedList<WiFiEventMetaData> ItsEventList
        {
            get { return _eventList; }
        }
        #endregion

        #region Constructors 
        public PacketListService(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _packetList = new LinkedList<PacketMetaData>();
            _eventList = new LinkedList<WiFiEventMetaData>();

            _packetListLock = new object();
            _eventListLock = new object();

            HookEvents();
        }
        #endregion

        #region Methods
        private void HookEvents()
        {
            _eventAggregator.GetEvent<ClearDataRequestEvent>().Subscribe(ClearDataRequestEventHandler);
            _eventAggregator.GetEvent<AllChannelsScanCompletedEvent>().Subscribe(AllChannelsScanCompletedEventHandler);
            _eventAggregator.GetEvent<ChannelScanCompletedEvent>().Subscribe(ChannelScanCompletedEventHandler);
        }

        private void ChannelScanCompletedEventHandler(ChannelScanInfo scanInfo)
        {
            UpdateEventListBasedOnScanTime(scanInfo.ItsStartTime);
        }

        private void UpdateEventListBasedOnScanTime(DateTime scanTime)
        {
            lock (_eventListLock)
            {
                foreach (var wifiEvent in _eventList)
                {
                    if (wifiEvent.ItsDateTime > DateTime.MinValue)
                    {
                        var lastSeenTimeSpan = scanTime - wifiEvent.ItsDateTime;
                        wifiEvent.ItsLastSeenTimeSpanString = Utils.TimeSpanToString(lastSeenTimeSpan);
                    }
                }
            }
        }

        private void AllChannelsScanCompletedEventHandler(ScanCompletedEventData scanCompletedEventData)
        {
            UpdateEventListBasedOnScanTime(scanCompletedEventData.ItsScanDateTime);
        }

        private void ClearDataRequestEventHandler(EventArgs eventArgs)
        {
            lock (_packetListLock)
            {
                _packetList.Clear();
            }
            lock (_eventListLock)
            {
                _eventList.Clear();
            }
        }

        public LinkedListNode<PacketMetaData> AddPacket(PacketMetaData packet)
        {
            lock (_packetListLock)
            {
                return _packetList.AddLast(packet);
            }
        }

        public void AddEvent(WiFiEventMetaData wifiEvent)
        {
            lock (_eventListLock)
            {
                var incremented = IncrementRecentEventIfDuplicate(wifiEvent);

                if (!incremented)
                {
                    _eventList.AddFirst(wifiEvent);
                    _eventAggregator.GetEvent<WiFiEventAddedEvent>().Publish(wifiEvent);
                }
            }
        }

        private bool IncrementRecentEventIfDuplicate(WiFiEventMetaData wifiEvent)
        {
            if (wifiEvent.ItsWiFiEvent == ClientWiFiEvents.AssumedRoam || wifiEvent.ItsWiFiEvent == ClientWiFiEvents.Roamed)
                return false;

            // starting at the head, check for a recent duplicate
            var listNode = _eventList.First;

            while (listNode != null && listNode.Value.ItsClient != wifiEvent.ItsClient)
            {
                listNode = listNode.Next;
            }

            if (listNode != null)
            {
                var listEvent = listNode.Value;
                if (listEvent != null && listEvent.ItsWiFiEvent == wifiEvent.ItsWiFiEvent
                    && listEvent.ItsRecentReasonCode == wifiEvent.ItsRecentReasonCode)
                {
                    listEvent.AddOccurrence();
                    listEvent.UpdateEvent(wifiEvent.ItsDateTime, wifiEvent.ItsStartingPacket);
                    _eventAggregator.GetEvent<WiFiEventUpdatedEvent>().Publish(listEvent);

                    return true;
                }
            }
            return false;
        }

        public List<WiFiEventMetaData> GetEventsCollection()
        {
            var clientEventList = new List<WiFiEventMetaData>();

            if (_eventList == null || _eventList.Count < 1) return clientEventList;

            lock (_eventListLock)
            {
                foreach (var wifiEvent in _eventList)
                {
                    clientEventList.Add(wifiEvent);
                }
            }

            return clientEventList;
        }

        public List<WiFiEventMetaData> GetEventsCollection(IClientDetails client)
        {
            var clientEventList = new List<WiFiEventMetaData>();

            if (client == null) 
                return clientEventList;

            lock (_eventListLock)
            {
                clientEventList = _eventList.Where(e => e.ItsClient != null && e.ItsClient.ItsId == client.ItsId).ToList();
            }

            return clientEventList;
        }

        public List<WiFiEventMetaData> GetEventsCollection(IBssidDetails bssid)
        {
            var clientEventList = new List<WiFiEventMetaData>();

            if (bssid == null) 
                return clientEventList;

            lock (_eventListLock)
            {
                clientEventList = _eventList.Where(e => e.ItsClient?.ItsBssid != null && e.ItsClient?.ItsBssid == bssid).ToList();
            }

            return clientEventList;
        }

        public List<WiFiEventMetaData> GetEventsCollection(IEssidDetails essid)
        {
            var clientEventList = new List<WiFiEventMetaData>();

            if (essid == null) 
                return clientEventList;

            lock (_eventListLock)
            {
                clientEventList = _eventList.Where(e => e.ItsClient?.ItsBssid?.ItsEssid != null && e.ItsClient.ItsBssid.ItsEssid == essid).ToList();
            }

            return clientEventList;
        }


        public List<PacketMetaData> GetPacketsForEvent(WiFiEventMetaData wiFiEvent, uint packetsToReturn)
        {
            var eventPacketList = new List<PacketMetaData>();
            var totalPacketsChecked = 0;

            var clientAddress = wiFiEvent.ItsClient?.ItsMacAddress;
            if (clientAddress == null) return eventPacketList;

            var listNode = wiFiEvent.ItsStartingPacket;

            lock (_packetListLock)
            {
                while (listNode != null && listNode.Value != null && eventPacketList.Count < packetsToReturn)
                {
                    var packet = listNode.Value;
                    // TODO check packet address(es) against client address
                    if ((packet.ItsRxAddress != null && packet.ItsRxAddress.Equals(clientAddress)) || (packet.ItsTxAddress != null && packet.ItsTxAddress.Equals(clientAddress)))
                    {
                        eventPacketList.Add(packet);
                    }

                    listNode = listNode.Next;
                    totalPacketsChecked++;
                }
            }

            Debug.WriteLine($"Checked {totalPacketsChecked} packets and found {eventPacketList.Count} that match the Packet Flow");

            return eventPacketList;
        }

        public PacketMetaData[] GetAllPackets()
        {
            lock (_packetListLock)
            {
                PacketMetaData[] packetArray = new PacketMetaData[_packetList.Count];
                _packetList.CopyTo(packetArray, 0);

                return packetArray;
            }
        }

        public void TrimListsToTime(DateTime trimDateTime)
        {
            lock (_packetListLock)
            {
                while (_packetList.First?.Value != null && _packetList.First.Value.ItsDateTime < trimDateTime)
                {
                    _packetList.RemoveFirst();
                }
            }

            // Setting events beyond the 10 minute window to disabled...
            lock (_eventListLock)
            {
                var eventNode = _eventList.Last;
                while (eventNode != null && eventNode.Value.ItsDateTime < trimDateTime)
                {
                    eventNode.Value.ItsIsEnabledFlag = false;
                    eventNode = eventNode.Previous;
                }
            }

            _eventAggregator.GetEvent<PacketListUpdatedEvent>().Publish(EventArgs.Empty);
        }
        #endregion
    }
}
