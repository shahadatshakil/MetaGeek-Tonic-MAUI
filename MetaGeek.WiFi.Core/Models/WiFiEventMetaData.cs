using MetaGeek.WiFi.Core.Enums;
using MetaGeek.WiFi.Core.Helpers;
using MetaGeek.WiFi.Core.Interfaces;
using Prism.Mvvm;
using System;
using System.Collections.Generic;

namespace MetaGeek.WiFi.Core.Models
{
    public class WiFiEventMetaData: BindableBase
    {
        #region Fields

        private uint _occurrences;
        private bool _showOccurrencesFlag;
        public bool _isEnabled;
        private DateTime _dateTime;
        private string _lastSeenTimeSpanString;
        private static ulong _eventId;

        #endregion

        #region Properties

        public IClientDetails ItsClient { get; }

        public ClientWiFiEvents ItsWiFiEvent { get; }

        public string ItsChannelsString { get; }

        public DateTime ItsDateTime
        {
            get { return _dateTime; }
            private set
            {
                if (value == _dateTime) return;
                _dateTime = value;
                SetProperty(ref _dateTime, value);
            }
        }


        public string ItsLastSeenTimeSpanString
        {
            get { return _lastSeenTimeSpanString; }
            set
            {
                if (value == _lastSeenTimeSpanString) return;
                _lastSeenTimeSpanString = value;
                SetProperty(ref _lastSeenTimeSpanString, value);
            }
        }

        public string ItsLocalTimeStringValue
        {
            get { return Utils.GetLocalTimeFormatStringLongValue(ItsDateTime); }
        }

        public LinkedListNode<PacketMetaData> ItsStartingPacket { get; private set; }
        
        public uint ItsOccurrences
        {
            get
            {
                return _occurrences;
            }
            private set
            {
                if (value == _occurrences) return;
                _occurrences = value;
                SetProperty(ref _occurrences, value);
            }
        }

        public bool ItsShowOccurrencesFlag
        {
            get { return _showOccurrencesFlag; }
            set
            {
                if(value == _showOccurrencesFlag) return;
                _showOccurrencesFlag = value;
                SetProperty(ref _showOccurrencesFlag, value);
            }
        }

        public bool ItsIsEnabledFlag
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                if (value == _isEnabled) return;
                _isEnabled = value;
                SetProperty(ref _isEnabled, value);
            }
        }

        public int? ItsRecentReasonCode { get; }

        public bool ItsHasReasonCodeFlag
        {
            get
            {
                return (ItsWiFiEvent == ClientWiFiEvents.FailedConnection ||
                  ItsWiFiEvent == ClientWiFiEvents.Disassociated) && ItsRecentReasonCode != null;
            }
        }

        public bool ItsIsRoamedFlag
        {
            get
            {
                return ItsWiFiEvent == ClientWiFiEvents.Roamed || ItsWiFiEvent == ClientWiFiEvents.AssumedRoam;
            }
        }

        public IBssidDetails ItsRoamedBssid { get; private set; }

        public WiFiEventTypes ItsEventType { get; }

        public WiFiEventSeverity ItsEventSeverity { get; }

        public ulong ItsEventId { get; }

        #endregion

        #region Constructors

        public WiFiEventMetaData(IClientDetails client, ClientWiFiEvents wifiEvent, DateTime timestamp, LinkedListNode<PacketMetaData> startingPacket = null, int? recentReasonCode = null, IBssidDetails roamedBssid = null)
        {
            ItsEventId = ++_eventId;
            ItsClient = client;
            ItsWiFiEvent = wifiEvent;
            ItsDateTime = timestamp;
            ItsStartingPacket = startingPacket;
            ItsShowOccurrencesFlag = false;
            ItsOccurrences = 1;
            ItsIsEnabledFlag = true;
            ItsRecentReasonCode = recentReasonCode;
            ItsRoamedBssid = roamedBssid;
            ItsEventType = GetWiFiEventType(wifiEvent);
            ItsEventSeverity = GetWiFiEventSeverity(wifiEvent);
            ItsChannelsString = GetChannelsString(client, roamedBssid);
        }

        #endregion

        #region Methods

        private string GetChannelsString(IClientDetails client, IBssidDetails roamedBssid)
        {
            string channelsString = string.Empty;

            if (client.ItsBssid != null)
            {
                channelsString += client.ItsBssid.ItsChannelInfo?.ItsPrimaryChannel.ToString();
            }

            if (roamedBssid != null)
            {
                channelsString += ", " + roamedBssid.ItsChannelInfo.ItsPrimaryChannel.ToString();
            }

            return channelsString;
        }

        public void AddOccurrence()
        {
            ItsOccurrences++;
            ItsShowOccurrencesFlag = true;
        }

        public void UpdateEvent(DateTime newTimeStamp, LinkedListNode<PacketMetaData> newStartingPacket)
        {
            ItsDateTime = newTimeStamp;
            ItsStartingPacket = newStartingPacket;
        }

        public override string ToString()
        {
            // TODO Format this string
            var name = ItsClient.ItsName.Replace(":", ".");

            var action = ActionToString(ItsWiFiEvent);

            return $"{name}-{action}";
        }

        private static string ActionToString(ClientWiFiEvents action)
        {
            switch (action)
            {
                case ClientWiFiEvents.Associated:
                    return "Associated";

                case ClientWiFiEvents.Roamed:
                    return "Roamed";

                case ClientWiFiEvents.AssumedRoam:
                    return "Assumed Roam";

                case ClientWiFiEvents.Beamforming:
                    return "Beamforming";

                case ClientWiFiEvents.SpectrumPowerReport:
                    return "Spectrum Power Report";

                case ClientWiFiEvents.NeighborReport:
                    return "Neighbor Report";

                case ClientWiFiEvents.SecurityHandshake:
                    return "Secure Handshake";

                case ClientWiFiEvents.ClientDiscovered:
                    return "Client Discovered";

                case ClientWiFiEvents.Reassociated:
                    return "Reassociated";

                case ClientWiFiEvents.Successful8021X:
                    return "Successful WPA2/3 Enterprise";

                case ClientWiFiEvents.Failed8021X:
                    return "Failed WPA2/3 Enterprise";

                case ClientWiFiEvents.SuccessfulWPA:
                    return "Successful WPA2/3";

                case ClientWiFiEvents.AssumedSuccessfullWPA:
                    return "Assumed Successful WPA2/3";

                case ClientWiFiEvents.FailedConnection:
                    return "Failed Connection";

                default:
                    return "Unknown";
            }
        }

        private WiFiEventTypes GetWiFiEventType(ClientWiFiEvents wiFiEvents)
        {
            switch (wiFiEvents)
            {
                case ClientWiFiEvents.Failed8021X:
                case ClientWiFiEvents.FailedConnection:
                    return WiFiEventTypes.Network;

                case ClientWiFiEvents.Disassociated:
                    return WiFiEventTypes.Bssid;

                case ClientWiFiEvents.Roamed:
                case ClientWiFiEvents.AssumedRoam:
                case ClientWiFiEvents.SuccessfulWPA:
                case ClientWiFiEvents.Successful8021X:
                case ClientWiFiEvents.TargetedProbeRequest:
                case ClientWiFiEvents.WildcardProbeRequest:
                    return WiFiEventTypes.Client;

                case ClientWiFiEvents.Associated:
                case ClientWiFiEvents.Beamforming:
                case ClientWiFiEvents.SpectrumPowerReport:
                case ClientWiFiEvents.NeighborReport:
                case ClientWiFiEvents.SecurityHandshake:
                case ClientWiFiEvents.ClientDiscovered:
                case ClientWiFiEvents.Reassociated:
                case ClientWiFiEvents.AssumedSuccessfullWPA:
                    return WiFiEventTypes.ClientDetails;
            }

            return WiFiEventTypes.None;
        }

        private WiFiEventSeverity GetWiFiEventSeverity(ClientWiFiEvents wiFiEvent)
        {
            switch (wiFiEvent)
            {
                case ClientWiFiEvents.Roamed:
                case ClientWiFiEvents.AssumedRoam:
                case ClientWiFiEvents.SuccessfulWPA:
                case ClientWiFiEvents.Successful8021X:
                case ClientWiFiEvents.FailedConnection:
                case ClientWiFiEvents.TargetedProbeRequest:
                case ClientWiFiEvents.WildcardProbeRequest:
                    return WiFiEventSeverity.Informational;

                case ClientWiFiEvents.Failed8021X:
                case ClientWiFiEvents.Disassociated:
                    return WiFiEventSeverity.Warning;

                default:
                    return WiFiEventSeverity.Unknown;
            }
        }

        #endregion
    }
}
