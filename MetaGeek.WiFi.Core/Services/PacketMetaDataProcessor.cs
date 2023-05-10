using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MetaGeek.WiFi.Core.Enums;
using MetaGeek.WiFi.Core.Interfaces;
using MetaGeek.WiFi.Core.Models;
using MetaGeek.WiFi.Core.Events;
using MetaGeek.WiFi.Core.Services;
using MetaGeek.WiFi.Core.Helpers;
using Prism.Events;

namespace MetaGeek.WiFi.Core.Services
{
    public class PacketMetaDataProcessor
    {
        #region Fields 
        private const byte RETRY_BIT = 0x08;
        private const byte FRAME_TYPE_BIT = 0x0c;
        private const int BEACON_FIXED_PARAMETERS_SIZE = 12;
        private const int BEACON_INTERVAL_INDEX = 8;
        private const int BEACON_CAPABILITIES_INDEX = 10;
        private const int MANAGEMENT_FRAME_MAC_HEADER_EXCLUDING_HT_CONTROL = 24;
        private const int HT_CONTROL_SIZE = 4;
        private const int ASSUMED_FAILED_CONNECTION_COUNT = 12;

        private const ushort EAP_RESPONSE_CODE = 2;
        private const ushort LLC_HEADER_LENGTH = 8;
        private const ushort EAP_IDENTITY_TYPE = 1;
        private const int QOS_DATA_HEADER_LENGTH = 26;
        private const ushort BASIC_INFO_LENGTH_802_1X = 4;
        private const ulong EAPOL_LLC_HEADER = 0x8E8800000003AAAA;

        private const int ASSOC_REQ_FIXED_PARAMETERS_SIZE = 4;
        private const int REASSOC_REQ_FIXED_PARAMETERS_SIZE = 10;
        private const int ASSOCIATION_RESPONSE_FIXED_PARAMETERS_SIZE = 6;
        private const int AUTHENTICATION_FIXED_PARAMETERS_SIZE = 6;
        private const int ASSOCIATION_RESPONSE_STATUS_CODE_OFFSET = 2;
        private const int AUTHENTICATION_STATUS_CODE_OFFSET = 4;
        private const int REASON_CODE_LENGTH = 2;
        private const int POWER_MANAGEMENT_FLAG_OFFSET = 1;
        private const byte POWER_MANAGEMENT_FLAG_BITMASK = 0x10;
        private const int NEIGHBOR_REPORT_FIXED_PARAMETER_SIZE = 3;
        private const int MAC_ADDRESS_START_INDEX = 4;
        private const ushort MANAGEMENT_FRAME_TYPE_CODE = 0;
        private const ushort CONTROL_FRAME_TYPE_CODE = 1;
        private const ushort DATA_FRAME_TYPE_CODE = 2;

        private const ushort MANAGEMENT_FRAME_ADDRESS_COUNT = 3;
        private const ushort CONTROL_FRAME_ADDRESS_COUNT = 2;
        private const ushort DATA_FRAME_ADDRESS_COUNT = 3;
        private const ushort WDS_ADDRESS_COUNT = 4;
        private IWiFiCollectionsService _wiFiCollections;
        private IPacketListService _packetListService;
        private IEventAggregator _eventAggregator;
        private IAnonymousInfoService _anonymousInfoService;

        private Random _random;
        protected PacketMetaData _previousPacket;
        protected bool _previousPacketUnresolved;

        private bool[] _beaconDetectedArray = new bool[166];  // allows direct indexing into the array by channel number. This wastes a few bytes of space, but it's fast
        //private uint _durationTimeUsec;
        //private ulong _arbitrationTimestampUsec;

        #endregion

        #region Constructors
        public PacketMetaDataProcessor(IEventAggregator eventAggregator, IWiFiCollectionsService wiFiCollections,
            IPacketListService packetListService, IAnonymousInfoService anonymizeInfoService)
        {
            _eventAggregator = eventAggregator;
            _wiFiCollections = wiFiCollections;
            _packetListService = packetListService;
            _anonymousInfoService = anonymizeInfoService;
            _random = new Random();
        }
        #endregion

        #region Methods

        public void ProcessPacket(PacketMetaData packet)
        {
            var packetBytes = packet.ItsPacketBytes;

            // Minimum packet length: FrameControl + Duration + RecieveAddress
            if (packetBytes.Length < 10) return;

            var flagsByte = packetBytes[1];
            packet.ItsRetryFlag = (flagsByte & RETRY_BIT) > 0;

            var packetListNode = _packetListService.AddPacket(packet);

            var receiveAddress = MacAddressCollection.GetMacAddress(packet.ItsPacketBytes, MAC_ADDRESS_START_INDEX);
            packet.ItsRxAddress = receiveAddress;

            DetermineFrameType(packet);

            // Is packet missing transmit address?
            if (packetBytes.Length < 16)
            {
                packet.ItsTxAddress = ResolveTransmitAddress(packet, receiveAddress);

                if (packet.ItsTxAddress == null)
                {
                    // We can't resolve this packet based on the previous packet so there's nothing else to do for this packet...
                    _previousPacket = packet;
                    _previousPacketUnresolved = true;
                    return;
                }
            }
            else
            {
                packet.ItsTxAddress = MacAddressCollection.GetMacAddress(packet.ItsPacketBytes, 10);
            }

            // Was the previous packet unresolved?
            if (_previousPacketUnresolved && _previousPacket != null &&
                _previousPacket.ItsChannel == packet.ItsChannel)
            {
                ResolvePreviousPacket(packet, packet.ItsTxAddress, receiveAddress, packetListNode);
            }

            // This is ACK, previous packet was CTS, both with same Receive Address. Therefore - there is a missing data packet
            if (_previousPacket != null && (packet.ItsFrameType == FrameSubType.Ack || packet.ItsFrameType == FrameSubType.BlockAck) &&
                _previousPacket.ItsFrameType == FrameSubType.ClearToSend && packet.ItsRxAddress.Equals(_previousPacket.ItsRxAddress))
            {
                AttachInferredDataPacketToClient(_previousPacket.ItsDurationTimeUsec, packet.ItsTxAddress, receiveAddress);
            }

            // point previous packet to this packet for the next time in the loop
            _previousPacketUnresolved = false;
            _previousPacket = packet;

            if (packet.ItsFrameType == FrameSubType.Beacon)
            {
                ProcessBeacon(packet, packet.ItsTxAddress);
            }

            CalculatePacketAirTime(packet);
            AttachPacketToClient(packet, packet.ItsTxAddress, receiveAddress, packetListNode);
            AttachPacketToChannel(packet);
            ProcessNeighborReportPacket(packet);
        }

        private void ProcessNeighborReportPacket(PacketMetaData packet)
        {
            var packetBytes = packet.ItsPacketBytes;
            var startIndex = GetPacketStartIndex(packetBytes);

            // CHECK FOR MALFORMED PACKETS
            if (packetBytes.Length < startIndex + NEIGHBOR_REPORT_FIXED_PARAMETER_SIZE) return;

            var ieBytes = ExtractIeBytes(packet.ItsPacketBytes, startIndex + NEIGHBOR_REPORT_FIXED_PARAMETER_SIZE);

            if (packet.ItsFrameType == FrameSubType.ActionNeighborReportRequest)
            {
                packet.ItsSSID = IEParser.GetPacketSsid(ieBytes);
            }
            else if (packet.ItsFrameType == FrameSubType.ActionNeighborReportResponse)
            {
                var bssidMacList = IEParser.GetPacketBssidMacList(ieBytes);
                var bssid = _wiFiCollections.GetBssid(packet.ItsTxAddress);

                packet.ItsBSSIDs = bssidMacList?.Select(x => x.ItsStringValue).ToList();

                if (bssid != null)
                {
                    bssid.ItsNeighborBssidMacList = bssidMacList;
                }
            }
        }

        private void ResolvePreviousPacket(PacketMetaData packet, IMacAddress transmitAddress, IMacAddress receiveAddress, LinkedListNode<PacketMetaData> packetListNode)
        {
            var packetFrameType = packet.ItsFrameType;
            var previousPacketFrameType = _previousPacket.ItsFrameType;

            CalculatePacketAirTime(_previousPacket);

            // check if this packet is Data and previous packet was unresolved CTS - CTS and Data have opposite receiver and transmitter
            if (((packetFrameType >> 2) & 0x03) == 0x02 && previousPacketFrameType == FrameSubType.ClearToSend &&
                _previousPacket.ItsRxAddress.Equals(transmitAddress))
            {
                AttachPacketToClient(_previousPacket, receiveAddress, transmitAddress, packetListNode);
                AttachPacketToChannel(_previousPacket);
            }
            // check if this packet is Block ACK and previous was CTS. CTS and Block ACK have same receiver
            else if (packetFrameType == FrameSubType.BlockAck && previousPacketFrameType == FrameSubType.ClearToSend &&
                     _previousPacket.ItsRxAddress.Equals(receiveAddress))
            {
                AttachPacketToClient(_previousPacket, transmitAddress, receiveAddress, packetListNode);
                AttachPacketToChannel(_previousPacket);
            }
        }

        protected virtual IMacAddress ResolveTransmitAddress(PacketMetaData packet, IMacAddress receiveAddress)
        {
            var packetFrameType = packet.ItsFrameType;

            // is previous packet available?
            if (_previousPacket != null && _previousPacket.ItsLength >= 16 &&
                _previousPacket.ItsChannel == packet.ItsChannel)
            {
                var previousTransmitter = MacAddressCollection.GetMacAddress(_previousPacket.ItsPacketBytes, 10);
                var previousPacketFrameType = _previousPacket.ItsFrameType;

                // Was this packet received by previous transmitter?
                if (previousTransmitter.Equals(receiveAddress))
                {
                    // this packet is ACK and previous packet was Data packet on this channel
                    if (packetFrameType == 0xD4 && ((previousPacketFrameType >> 2) & 0x03) == 0x02)
                    {
                        return MacAddressCollection.GetMacAddress(_previousPacket.ItsPacketBytes, 4);
                    }
                    // this packet is CTS and previous packet was RTS
                    if (packetFrameType == FrameSubType.ClearToSend && previousPacketFrameType == FrameSubType.RequestToSend)
                    {
                        return MacAddressCollection.GetMacAddress(_previousPacket.ItsPacketBytes, 4);
                    }
                }
            }

            // EXPERIMENTAL! Using knowledge of client connection to infer transmitter of CTS and ACK to known client is its BSS
            var client = _wiFiCollections.GetClient(receiveAddress, false);
            if (client != null && client.ItsBssid != null)
            {
                return client.ItsBssid.ItsMacAddress;
            }

            return null;
        }

        private void ProcessBeacon(PacketMetaData packet, IMacAddress transmitAddress)
        {
            var packetBytes = packet.ItsPacketBytes;
            var bssid = _wiFiCollections.GetBssid(transmitAddress);

            var beaconStartIndex = GetPacketStartIndex(packetBytes);
            if (bssid == null)
            {
                // Add new BSSID
                bssid = new BssidDetails(transmitAddress);
                try
                {
                    bssid.ItsInterval = GetBeaconInterval(packetBytes, beaconStartIndex);
                    bssid.ItsCapabilitiesInformation = GetBeaconCapabilities(packetBytes, beaconStartIndex);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    return;
                }

                _wiFiCollections.AddBssid(bssid);
            }
            // Beacon has not been parsed yet
            if (bssid.ItsEssid == null)
            {
                var ieBytes = ExtractIeBytes(packetBytes, beaconStartIndex + BEACON_FIXED_PARAMETERS_SIZE);
                if (ieBytes.Length > 0)
                {
                    bssid.ItsInformationElementBytes = ieBytes;
                    IEParser.ParseAllBeaconInformationElements(bssid, ieBytes, packet.ItsDateTime);

                    if (BssidAppearsValid(bssid))
                    {
                        _beaconDetectedArray[packet.ItsChannel] = true;
                        _wiFiCollections.AttachBssidToEssid(bssid);
                        bssid.UpdateClientsSsid();
                    }
                }
            }
            else
            {
                // Update transient values
                var ieBytes = ExtractIeBytes(packetBytes, beaconStartIndex + BEACON_FIXED_PARAMETERS_SIZE);
                if (ieBytes.Length > 0)
                {
                    IEParser.UpdateBeaconInformationElements(bssid, ieBytes, packet.ItsDateTime);
                }
            }

            CalculatePacketAirTime(packet);
            bssid.ProcessBeacon(packet);
        }

        private bool BssidAppearsValid(IBssidDetails bssid)
        {
            if (bssid.ItsChannelInfo.ItsChannel == 0) return false;

            return true;
        }

        internal void AttachPacketToChannel(PacketMetaData packet)
        {
            var channelDetails = _wiFiCollections.GetChannel(packet.ItsChannel);
            channelDetails?.ProcessPacket(packet);
        }

        internal void AttachPacketToClient(PacketMetaData packet, IMacAddress transmitAddress, IMacAddress receiveAddress, LinkedListNode<PacketMetaData> packetListNode)
        {
            AdjustPacketTypeForEapFrame(packet);

            var bssid = _wiFiCollections.GetBssid(transmitAddress);

            if (packet.ItsFrameType == FrameSubType.ActionVhtBeamforming)
            {
                var startIndex = GetPacketStartIndex(packet.ItsPacketBytes);
                if (packet.ItsLength > startIndex + 5)
                {
                    double? snr;
                    var snrByteValue = packet.ItsPacketBytes[startIndex + 5];

                    if (snrByteValue < 128)
                    {
                        snr = 22f + (double)snrByteValue * 0.25;
                    }
                    else
                    {
                        snr = 22f - (double)(256 - snrByteValue) * 0.25;
                    }

                    packet.ItsContextualLabel = "Signal-to-Noise Ratio";
                    packet.ItsContextualInfo = $"{snr} dB";
                    packet.ItsSNR = snr;
                }
            }

            // packet was broadcast of some kind
            if (receiveAddress.ItsType != MacAddressType.Normal)
            {
                if (bssid != null)
                {
                    bssid.ProcessBroadcastPacket(packet, false);
                }
                // only creating clients from broadcast packet if it is a Probe Request
                else if (packet.ItsFrameType == FrameSubType.ProbeRequest)
                {
                    var client = _wiFiCollections.GetClient(transmitAddress, true);
                    client.ProcessPacket(packet, true);

                    ProcessProbeRequest(client, packet);

                    ProcessClientConnectionActivity(packet, client, packetListNode);
                }
            }
            else if (transmitAddress.ItsType != MacAddressType.Normal)
            {
                // This occurs on ACKs to multicast packets
                bssid = _wiFiCollections.GetBssid(receiveAddress);
                if (bssid != null)
                {
                    bssid.ProcessBroadcastPacket(packet, false);
                }
            }
            else
            {
                // Check for data type
                if ((packet.ItsFrameType == FrameSubType.Data || packet.ItsFrameType == FrameSubType.QosData) && packet.ItsMcsIndex > 0)
                {
                    packet.ItsContextualLabel = "MCS";
                    packet.ItsContextualInfo = $"{packet.ItsMcsIndex}";
                }

                // BSSID is transmitter
                if (bssid != null)
                {
                    // Don't add client if this is an AP responding to a probe that we didn't hear...
                    var addifNotFound = packet.ItsFrameType != FrameSubType.ProbeResponse;
                    var client = _wiFiCollections.GetClient(receiveAddress, addifNotFound);

                    if (client != null)
                    {
                        if (client.ItsIsNewClientFlag)
                        {
                            _packetListService.AddEvent(new WiFiEventMetaData(client, ClientWiFiEvents.ClientDiscovered, packet.ItsDateTime, packetListNode));
                        }

                        if (packet.ItsFrameType != FrameSubType.ProbeResponse && client.ItsBssid != bssid)
                        {
                            AttachClientToBssid(bssid, client, packet, packetListNode);
                        }

                        client.ProcessPacket(packet);
                    }

                    ProcessClientConnectionActivity(packet, client, packetListNode);
                }

                // Client is likely the transmitter unless it was a Probe Response from an unknown BSSID
                else if (packet.ItsFrameType != FrameSubType.ProbeResponse)
                {
                    var client = _wiFiCollections.GetClient(transmitAddress, true);

                    if (client.ItsIsNewClientFlag)
                    {
                        _packetListService.AddEvent(new WiFiEventMetaData(client, ClientWiFiEvents.ClientDiscovered, packet.ItsDateTime, packetListNode));
                    }

                    if (client.ItsBssid == null || client.ItsBssid.ItsMacAddress.ItsUlongValue != receiveAddress.ItsUlongValue)
                    {
                        bssid = _wiFiCollections.GetBssid(receiveAddress);
                        // EXPERIMENTAL! RECEIVER OF RTS MUST BE AP
                        //if (bssid == null && packet.ItsFrameType == FrameSubType.RequestToSend)
                        //{
                        //    bssid = new BssidDetails(receiveAddress)
                        //    { ItsChannelInfo = new ChannelInfo() { ItsChannel = packet.ItsChannel } };
                        //    _wiFiCollections.AddBssid(bssid);
                        //}
                        if (bssid != null)
                        {
                            AttachClientToBssid(bssid, client, packet, packetListNode);
                        }
                    }

                    // TODO is this where we want to check for associations?
                    if (packet.ItsFrameType == FrameSubType.AssociationRequest)
                    {
                        ProcessAssociationRequest(client, packet);
                    }

                    client.ProcessPacket(packet, true);
                    CheckForClientActionEvents(packet, client, packetListNode);
                    ProcessClientConnectionActivity(packet, client, packetListNode);
                }
            }
        }

        private void ProcessClientConnectionActivity(PacketMetaData packet, IClientDetails client, LinkedListNode<PacketMetaData> packetListNode)
        {
            var packetBytes = packet.ItsPacketBytes;
            var basicFrameType = (packetBytes[0] & FRAME_TYPE_BIT) >> 2; // LSB 3rd and 4th bit

            if (basicFrameType == DATA_FRAME_TYPE_CODE)
            {
                if (IsEapHeaderExists(packetBytes))
                {
                    if (client != null && client.ItsBssid != null)
                    {
                        var macAddressUlong = client.ItsBssid.ItsMacAddress.ItsUlongValue;

                        client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsEapFrameFlag = true;
                        client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsAuthenticationFlag = false;

                        if (client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsStartingPacket == null)
                        {
                            client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsStartingPacket = packetListNode;
                        }
                    }

                    if (packet.ItsEapResponseCode == EapResponseCodes.Failure)
                    {
                        client.AddClientAction(ClientWiFiEvents.Failed8021X, packet.ItsDateTime);
                        _packetListService.AddEvent(new WiFiEventMetaData(client, ClientWiFiEvents.Failed8021X, packet.ItsDateTime, packetListNode));
                    }
                }
                else if (client != null && client.ItsBssid != null && !client.ItsAllBssidsAuthInfoMap[client.ItsBssid.ItsMacAddress.ItsUlongValue].ItsRoamingFlag)
                {
                    var macAddressUlong = client.ItsBssid.ItsMacAddress.ItsUlongValue;
                    var startingPacket = client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsStartingPacket ?? packetListNode;

                    if (client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsReassociationFrameFlag)
                    {
                        client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsRoamingFlag = true;
                        client.AddClientAction(ClientWiFiEvents.Roamed, packet.ItsDateTime);

                        _packetListService.AddEvent(new WiFiEventMetaData(client, ClientWiFiEvents.Roamed, packet.ItsDateTime, startingPacket, roamedBssid: client.ItsBssid));
                    }
                    else if (client.ItsPreviousBssid != null && client.ItsPreviousBssid != client.ItsBssid &&
                         client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsAuthCount == 0 &&
                         !client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsEapFrameFlag)
                    {
                        client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsRoamingFlag = true;
                        client.AddClientAction(ClientWiFiEvents.AssumedRoam, packet.ItsDateTime);

                        var clientAction = new WiFiEventMetaData(client, ClientWiFiEvents.AssumedRoam, packet.ItsDateTime, packetListNode, roamedBssid: client.ItsBssid);
                        _packetListService.AddEvent(clientAction);
                        _eventAggregator.GetEvent<ClientWiFiEvent>().Publish(clientAction);
                    }
                    else if (!client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsAuthenticationFlag)
                    {
                        var isAssumedWPA = client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsAuthCount == 0
                            && !client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsEapFrameFlag;

                        if (isAssumedWPA)
                        {
                            client.AddClientAction(ClientWiFiEvents.AssumedSuccessfullWPA, packet.ItsDateTime);
                            _packetListService.AddEvent(new WiFiEventMetaData(client, ClientWiFiEvents.AssumedSuccessfullWPA,
                                packet.ItsDateTime, startingPacket));
                        }
                        else
                        {
                            AddSuccessfulWpaEvent(client, packet.ItsDateTime, startingPacket);
                        }
                    }

                    client.ItsPreviousBssid = client.ItsBssid;
                    client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsAuthCount = 0;
                    client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsEapFrameFlag = false;
                    client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsReassociationFrameFlag = false;
                    client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsAuthenticationFlag = true;
                    client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsStartingPacket = null;
                }
            }

            switch (packet.ItsFrameType)
            {
                // Already handled in ProcessProbeRequest() and ProcessAssociationRequest()
                case FrameSubType.ProbeRequest:
                    // If SSID is empty the client sent a wildcard probe request...
                    var wifiEvent = string.IsNullOrEmpty(client.ItsDisplaySsid)
                        ? ClientWiFiEvents.WildcardProbeRequest
                        : ClientWiFiEvents.TargetedProbeRequest;
                    _packetListService.AddEvent(new WiFiEventMetaData(client, wifiEvent, packet.ItsDateTime, packetListNode));
                    var activity = string.IsNullOrEmpty(client.ItsDisplaySsid)
                        ? ClientConnectionActivities.WildcardProbeRequest
                        : ClientConnectionActivities.TargetedProbeRequest;
                    client.AddConnectionActivity(activity);
                    break;

                case FrameSubType.ReassociationRequest:
                    if (client != null && client.ItsBssid != null)
                    {
                        client.ItsAllBssidsAuthInfoMap[client.ItsBssid.ItsMacAddress.ItsUlongValue].ItsReassociationFrameFlag = true;
                    }
                    client.AddConnectionActivity(ClientConnectionActivities.ReassociationRequest);
                    break;

                case FrameSubType.ReassociationResponse:
                    if (client != null && client.ItsBssid != null)
                    {
                        client.ItsAllBssidsAuthInfoMap[client.ItsBssid.ItsMacAddress.ItsUlongValue].ItsReassociationFrameFlag = true;
                    }
                    client.AddConnectionActivity(ClientConnectionActivities.Reassociated);
                    break;

                case FrameSubType.AssociationRequest:
                    if (client != null && client.ItsBssid != null)
                    {
                        var macAddressUlong = client.ItsBssid.ItsMacAddress.ItsUlongValue;

                        client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsAuthCount++;
                        client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsRoamingFlag = false;
                        client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsAuthenticationFlag = false;

                        if (client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsStartingPacket == null)
                        {
                            client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsStartingPacket = packetListNode;
                        }

                        if (client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsAuthCount == ASSUMED_FAILED_CONNECTION_COUNT)
                        {
                            client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsAuthCount = 0;
                            client.AddClientAction(ClientWiFiEvents.FailedConnection, packet.ItsDateTime);
                            _packetListService.AddEvent(new WiFiEventMetaData(client, ClientWiFiEvents.FailedConnection, packet.ItsDateTime, packetListNode));
                        }
                    }

                    client.AddConnectionActivity(ClientConnectionActivities.AssociationRequest);
                    client.ItsLastAssociationRequestPacketNode = packetListNode;
                    break;

                case FrameSubType.AssociationResponse:
                    if (client != null && client.ItsBssid != null && client.ItsAllBssidsAuthInfoMap[client.ItsBssid.ItsMacAddress.ItsUlongValue].ItsStartingPacket == null)
                    {
                        client.ItsAllBssidsAuthInfoMap[client.ItsBssid.ItsMacAddress.ItsUlongValue].ItsStartingPacket = packetListNode;
                    }

                    packet.ItsStatusCode = GetAssociationResponseStatusCode(packet.ItsPacketBytes);
                    client.AddConnectionActivity(ClientConnectionActivities.Associated);
                    break;

                case FrameSubType.Authentication:
                    if (client != null && client.ItsBssid != null)
                    {
                        var macAddressUlong = client.ItsBssid.ItsMacAddress.ItsUlongValue;

                        client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsAuthCount++;
                        client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsRoamingFlag = false;
                        client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsAuthenticationFlag = false;

                        if (client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsStartingPacket == null)
                        {
                            client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsStartingPacket = packetListNode;
                        }

                        if (client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsAuthCount == ASSUMED_FAILED_CONNECTION_COUNT)
                        {
                            client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsAuthCount = 0;
                            client.AddClientAction(ClientWiFiEvents.FailedConnection, packet.ItsDateTime);
                            _packetListService.AddEvent(new WiFiEventMetaData(client, ClientWiFiEvents.FailedConnection, packet.ItsDateTime, packetListNode));
                        }
                    }

                    packet.ItsStatusCode = GetAuthenticationStatusCode(packet.ItsPacketBytes);
                    client.AddConnectionActivity(ClientConnectionActivities.Authenticated);
                    break;

                case FrameSubType.Disassociation:
                    packet.ItsReasonCode = GetReasonCode(packet.ItsPacketBytes);

                    if (client != null && client.ItsBssid != null)
                    {
                        var macAddressUlong = client.ItsBssid.ItsMacAddress.ItsUlongValue;

                        client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsRoamingFlag = false;

                        if (client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsAuthenticationFlag)
                        {
                            _packetListService.AddEvent(new WiFiEventMetaData(client, ClientWiFiEvents.Disassociated, packet.ItsDateTime, packetListNode, recentReasonCode: packet.ItsReasonCode));
                            client.AddConnectionActivity(ClientConnectionActivities.Disassociated);
                        }
                        else
                        {
                            client.AddClientAction(ClientWiFiEvents.FailedConnection, packet.ItsDateTime);
                            _packetListService.AddEvent(new WiFiEventMetaData(client, ClientWiFiEvents.FailedConnection, packet.ItsDateTime, packetListNode, recentReasonCode: packet.ItsReasonCode));
                        }
                    }
                    break;

                case FrameSubType.Deauthentication:
                    if (client != null && client.ItsBssid != null)
                    {
                        var macAddressUlong = client.ItsBssid.ItsMacAddress.ItsUlongValue;

                        client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsRoamingFlag = false;
                        client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsAuthenticationFlag = false;
                    }
                    packet.ItsReasonCode = GetReasonCode(packet.ItsPacketBytes);
                    client.AddClientAction(ClientWiFiEvents.FailedConnection, packet.ItsDateTime);
                    _packetListService.AddEvent(new WiFiEventMetaData(client, ClientWiFiEvents.FailedConnection, packet.ItsDateTime, packetListNode, recentReasonCode: packet.ItsReasonCode));
                    break;

                case FrameSubType.EapolHandshake1:
                    client.AddConnectionActivity(ClientConnectionActivities.HandshakeKey1);
                    break;

                case FrameSubType.EapolHandshake2:
                    client.AddConnectionActivity(ClientConnectionActivities.HandshakeKey2);
                    break;

                case FrameSubType.EapolHandshake3:
                    client.AddConnectionActivity(ClientConnectionActivities.HandshakeKey3);
                    break;

                case FrameSubType.EapolHandshake4:
                    client.AddConnectionActivity(ClientConnectionActivities.HandshakeKey4);
                    break;
                case FrameSubType.QosNull:
                    packet.ItsPowerManagementFlag = GetPowerManagementFlag(packet.ItsPacketBytes);
                    client.AddConnectionActivity(ClientConnectionActivities.Connected);
                    client.ItsConnectionDateTime = packet.ItsDateTime;
                    break;
                case FrameSubType.Data:
                case FrameSubType.QosData:
                case FrameSubType.RequestToSend:
                case FrameSubType.ClearToSend:
                case FrameSubType.InferredData:
                    client.AddConnectionActivity(ClientConnectionActivities.Connected);
                    client.ItsConnectionDateTime = packet.ItsDateTime;
                    break;
            }
        }

        private void AddSuccessfulWpaEvent(IClientDetails client, DateTime eventDateTime, LinkedListNode<PacketMetaData> startingPacket)
        {
            var bssidAuthentication = client.ItsBssid.ItsSecurityInfo.ItsAuthentication;
            switch (bssidAuthentication)
            {
                case AuthenticationTypes.WPA2_PRE_SHARED_KEY:
                case AuthenticationTypes.WPA3_PRE_SHARED_KEY:
                    client.AddClientAction(ClientWiFiEvents.SuccessfulWPA, eventDateTime);
                    _packetListService.AddEvent(new WiFiEventMetaData(client, ClientWiFiEvents.SuccessfulWPA, eventDateTime, startingPacket));
                    break;
                case AuthenticationTypes.WPA2_ENTERPRISE:
                case AuthenticationTypes.WPA3_ENTERPRISE:
                    client.AddClientAction(ClientWiFiEvents.Successful8021X, eventDateTime);
                    _packetListService.AddEvent(new WiFiEventMetaData(client, ClientWiFiEvents.Successful8021X, eventDateTime, startingPacket));
                    break;
            }
        }

        private bool? GetPowerManagementFlag(byte[] packetBytes)
        {
            if (packetBytes.Length < POWER_MANAGEMENT_FLAG_OFFSET)
                return null;

            int frameControlField = packetBytes[POWER_MANAGEMENT_FLAG_OFFSET];

            return (frameControlField & POWER_MANAGEMENT_FLAG_BITMASK) == POWER_MANAGEMENT_FLAG_BITMASK;
        }

        private int? GetReasonCode(byte[] packetBytes)
        {
            var startIndex = GetPacketStartIndex(packetBytes);

            // CHECK FOR MALFORMED PACKETS
            if (packetBytes.Length < startIndex + REASON_CODE_LENGTH)
                return null;

            return BitConverter.ToInt16(packetBytes, startIndex);
        }

        private int? GetAuthenticationStatusCode(byte[] packetBytes)
        {
            var startIndex = GetPacketStartIndex(packetBytes);

            // CHECK FOR MALFORMED PACKETS
            if (packetBytes.Length < startIndex + AUTHENTICATION_FIXED_PARAMETERS_SIZE)
                return null;

            return BitConverter.ToInt16(packetBytes, startIndex + AUTHENTICATION_STATUS_CODE_OFFSET);
        }

        private int? GetAssociationResponseStatusCode(byte[] packetBytes)
        {
            var startIndex = GetPacketStartIndex(packetBytes);

            // CHECK FOR MALFORMED PACKETS
            if (packetBytes.Length < startIndex + ASSOCIATION_RESPONSE_FIXED_PARAMETERS_SIZE)
                return null;

            return BitConverter.ToInt16(packetBytes, startIndex + ASSOCIATION_RESPONSE_STATUS_CODE_OFFSET);
        }

        private void CheckForClientActionEvents(PacketMetaData packet, IClientDetails client, LinkedListNode<PacketMetaData> packetListNode)
        {
            WiFiEventMetaData clientAction = null;

            switch (packet.ItsFrameType)
            {
                case FrameSubType.ActionSpectrumPowerReport:
                    client.AddClientAction(ClientWiFiEvents.SpectrumPowerReport, packet.ItsDateTime);
                    clientAction = new WiFiEventMetaData(client, ClientWiFiEvents.SpectrumPowerReport, packet.ItsDateTime);
                    break;

                case FrameSubType.ActionNeighborReportRequest:
                    client.AddClientAction(ClientWiFiEvents.NeighborReport, packet.ItsDateTime);
                    clientAction = new WiFiEventMetaData(client, ClientWiFiEvents.NeighborReport, packet.ItsDateTime);
                    break;

                case FrameSubType.ActionNeighborReportResponse:
                    client.AddClientAction(ClientWiFiEvents.NeighborReport, packet.ItsDateTime);
                    clientAction = new WiFiEventMetaData(client, ClientWiFiEvents.NeighborReport, packet.ItsDateTime);
                    break;
            }

            if (clientAction != null)
            {
                _packetListService.AddEvent(new WiFiEventMetaData(client, clientAction.ItsWiFiEvent, packet.ItsDateTime, packetListNode));
                _eventAggregator.GetEvent<ClientWiFiEvent>().Publish(clientAction);
            }
        }

        private void AdjustPacketTypeForEapFrame(PacketMetaData packet)
        {
            const int START_INDEX_802_1X = QOS_DATA_HEADER_LENGTH + LLC_HEADER_LENGTH;
            const int EIGHT02_1X_VERSION_FIELD = 0;
            const int EIGHT02_1X_TYPE_FIELD = 1;
            const int EIGHT02_1X_LENGTH_FIELD = 2;
            const int EIGHT02_1X_KEY_DESC_FIELD = 4;
            const int EIGHT02_1X_KEY_INFO_FIELD = 5;
            const int EIGHT02_1X_EAP_TYPE_FIELD = 8;

            const uint EIGHT02_1X_VERSION_2001 = 1;
            const uint EIGHT02_1X_VERSION_2004 = 2;
            const uint EIGHT02_1X_EAP_TYPE_KEY = 0;
            const uint EIGHT02_1X_EAPOL_TYPE_KEY = 3;

            if (!IsEapHeaderExists(packet.ItsPacketBytes) || packet.ItsPacketBytes.Length <= START_INDEX_802_1X + EIGHT02_1X_KEY_DESC_FIELD) return;

            if (packet.ItsPacketBytes[START_INDEX_802_1X + EIGHT02_1X_VERSION_FIELD] != EIGHT02_1X_VERSION_2001 && packet.ItsPacketBytes[START_INDEX_802_1X + EIGHT02_1X_VERSION_FIELD] != EIGHT02_1X_VERSION_2004) return;

            var typeKey = packet.ItsPacketBytes[START_INDEX_802_1X + EIGHT02_1X_TYPE_FIELD];

            if (typeKey == EIGHT02_1X_EAP_TYPE_KEY)
            {
                packet.ItsFrameType = FrameSubType.Eap;
                packet.ItsEapResponseCode = GetEapResponseCode(packet.ItsPacketBytes);

                var eapTypeIndex = START_INDEX_802_1X + EIGHT02_1X_EAP_TYPE_FIELD;

                if (eapTypeIndex < packet.ItsLength)
                {
                    var eapType = packet.ItsPacketBytes[eapTypeIndex];

                    if (eapType == 0x0D)
                    {
                        packet.ItsFrameType = FrameSubType.EapTls;
                    }
                    else if (eapType == 0x19)
                    {
                        packet.ItsFrameType = FrameSubType.EapPeap;
                    }
                }
            }
            else if (typeKey == EIGHT02_1X_EAPOL_TYPE_KEY)
            {
                // flip endianness
                var keyLength = (uint)(packet.ItsPacketBytes[START_INDEX_802_1X + EIGHT02_1X_LENGTH_FIELD] << 8) | packet.ItsPacketBytes[START_INDEX_802_1X + EIGHT02_1X_LENGTH_FIELD + 1];
                var keyDescriptor = packet.ItsPacketBytes[START_INDEX_802_1X + EIGHT02_1X_KEY_DESC_FIELD];
                var keyInformation = (uint)(packet.ItsPacketBytes[START_INDEX_802_1X + EIGHT02_1X_KEY_INFO_FIELD] << 8) | packet.ItsPacketBytes[START_INDEX_802_1X + EIGHT02_1X_KEY_INFO_FIELD + 1];
                keyInformation &= 0xFFF8;   // cipher and hash types vary and don't impact key #, so just clear those bit fields

                switch (keyInformation)
                {
                    case 0x0088:
                        packet.ItsFrameType = FrameSubType.EapolHandshake1;
                        break;

                    case 0x0108:
                        packet.ItsFrameType = FrameSubType.EapolHandshake2;
                        break;

                    case 0x13C8:
                        packet.ItsFrameType = FrameSubType.EapolHandshake3;
                        break;

                    case 0x0308:
                        packet.ItsFrameType = FrameSubType.EapolHandshake4;
                        break;
                }
            }
        }

        private void AttachInferredDataPacketToClient(double durationTimeUsec, IMacAddress transmitAddress, IMacAddress receiveAddress)
        {
            if (transmitAddress.ItsType != MacAddressType.Normal ||
                receiveAddress.ItsType != MacAddressType.Normal) return;

            var client = _wiFiCollections.GetClient(receiveAddress, false);
            if (client == null)
            {
                client = _wiFiCollections.GetClient(transmitAddress, false);
            }

            if (client != null)
            {
                client.ProcessInferredDataPacket(durationTimeUsec);
            }
        }

        private void AttachClientToBssid(IBssidDetails bssid, IClientDetails client, PacketMetaData packet, LinkedListNode<PacketMetaData> packetListNode)
        {
            client.ItsPreviousBssid = client.ItsBssid;
            var bssidOfClientChangedFlag = client.ItsBssid != bssid;

            bssid.AttachClient(client);

            if (bssidOfClientChangedFlag)
            {
                var macAddressUlong = client.ItsBssid.ItsMacAddress.ItsUlongValue;

                client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsRoamingFlag = false;
                client.ItsAllBssidsAuthInfoMap[macAddressUlong].ItsStartingPacket = null;
                _eventAggregator.GetEvent<BssidUpdatedOnClientEvent>().Publish(client);
            }
        }

        public bool ChannelHasBeacons(uint channel)
        {
            if (channel >= _beaconDetectedArray.Length) return false;
            return _beaconDetectedArray[channel];
        }

        internal void CalculatePacketAirTime(PacketMetaData packet)
        {
            //if (packet.ItsUSecSinceEpoch < _arbitrationTimestampUsec) // TODO && same channel!!
            //{
            //    return;
            //}

            var duration = ((uint)packet.ItsPacketBytes[3] << 8) + packet.ItsPacketBytes[2];

            var plcpTime = 0.0;
            var sifs = 10.0; // default for 2.4 GHz using DSSS
            var difs = 50; // default for 2.4 GHz using DSSS

            if (packet.ItsChannel <= 14)
            {
                // Long PLCP Preamble 
                // CWAP Official Study Guide Exam PW0-270 p. 41
                if (packet.ItsRate < 2)
                {
                    // 144-bit preamble + 48-bit header sent at 1 Mbps
                    plcpTime = 192;
                }
                // Short PLCP Preamble
                else if (packet.ItsRate < 6)
                {
                    // 72-bit preamble + 48-bit header sent at 2 Mbps
                    plcpTime = 96;
                }
                // OFDM PLCP Preamble
                else if (packet.ItsRate >= 6)
                {
                    // 16 us training + 24-bit PLCP header sent at 6 Mbps
                    plcpTime = 20;
                    difs = 28;
                }
            }

            if (packet.ItsChannel >= 36)
            {
                plcpTime = 20;
                sifs = 16;
                difs = 34;
            }

            // airtime is LengthInBits / bitsPerMicroSecond
            var airtimeUsec = (packet.ItsLength * 8) / packet.ItsRate;
            var interframe = (duration == 0 || duration > 0x8000) ? difs : sifs;
            packet.ItsAirTimeUsec = airtimeUsec + plcpTime + interframe;
            packet.ItsDurationTimeUsec = duration;
        }

        private void DetermineFrameType(PacketMetaData packet)
        {
            var packetBytes = packet.ItsPacketBytes;
            var basicFrameType = packetBytes[0];
            packet.ItsFrameType = basicFrameType;

            if (basicFrameType != FrameSubType.Action && basicFrameType != FrameSubType.ActionNoAck)
            {
                return;
            }

            var startIndex = GetPacketStartIndex(packetBytes);
            if (packet.ItsLength < startIndex + 2) return;

            var categoryCode = (uint)packetBytes[startIndex];
            if (categoryCode == ActionFrameCategory.VendorSpecific) return;

            var actionCode = (uint)packetBytes[startIndex + 1];
            packet.ItsFrameType = (categoryCode << 16) | (actionCode << 8) | basicFrameType;
        }

        public static int GetPacketStartIndex(byte[] metaDataBytes)
        {
            var orderBit = metaDataBytes[1] & 0x80;
            // if the order bit is 1 then the header has an HT-Control element in the MAC header
            if (orderBit == 0x80)
            {
                return MANAGEMENT_FRAME_MAC_HEADER_EXCLUDING_HT_CONTROL + HT_CONTROL_SIZE;
            }

            return MANAGEMENT_FRAME_MAC_HEADER_EXCLUDING_HT_CONTROL;
        }

        public static ushort GetBeaconInterval(byte[] metaDataBytes, int beaconStartIndex)
        {
            return BitConverter.ToUInt16(metaDataBytes, beaconStartIndex + BEACON_INTERVAL_INDEX);
        }

        public static ushort GetBeaconCapabilities(byte[] metaDataBytes, int beaconStartIndex)
        {
            return BitConverter.ToUInt16(metaDataBytes, beaconStartIndex + BEACON_CAPABILITIES_INDEX);
        }

        private void ProcessAssociationRequest(IClientDetails client, PacketMetaData packet)
        {
            if (string.IsNullOrEmpty(client.ItsTaxonomySignature))
            {
                var packetBytes = packet.ItsPacketBytes;
                var startIndex = GetPacketStartIndex(packetBytes);
                // CHECK FOR MALFORMED PACKETS
                if (packetBytes.Length < startIndex + ASSOC_REQ_FIXED_PARAMETERS_SIZE) return;

                var capabilities = BitConverter.ToUInt16(packetBytes, startIndex);
                var interval = BitConverter.ToUInt16(packetBytes, startIndex + 2);
                var ieBytes = ExtractIeBytes(packetBytes, startIndex + ASSOC_REQ_FIXED_PARAMETERS_SIZE);

                client.SaveAssociationRequestDetails(packet.ItsDateTime, capabilities, interval, ieBytes);

                var band = packet.ItsChannel <= 30 ? ChannelBand.TwoGhz : ChannelBand.FiveGhz;
                IEParser.ParseClientInformationElements(client, ieBytes, false, band);
                client.ItsHasKnownCapabilities = true;

                _eventAggregator.GetEvent<ClientDetailsEvent>().Publish(new ClientMetaData(client.ItsMacAddress, client.ItsMaxMcsIndex, client.ItsSpacialStreamCount, client.ItsMaxDataRate));
            }
        }

        private void ProcessProbeRequest(IClientDetails client, PacketMetaData packet)
        {
            var band = packet.ItsChannel < 30 ? ChannelBand.TwoGhz : ChannelBand.FiveGhz;
            if (band == ChannelBand.TwoGhz && string.IsNullOrEmpty(client.Its24GhzProbeSignature) || band == ChannelBand.FiveGhz && string.IsNullOrEmpty(client.Its5GhzProbeSignature))
            {
                var packetBytes = packet.ItsPacketBytes;
                var startIndex = GetPacketStartIndex(packetBytes);
                var ieBytes = ExtractIeBytes(packetBytes, startIndex);
                IEParser.ParseClientInformationElements(client, ieBytes, true, band);
                packet.ItsSSID = IEParser.GetPacketSsid(ieBytes);
                client.ItsHasKnownCapabilities = true;
            }
        }

        public static byte[] ExtractIeBytes(byte[] metaDataBytes, int index)
        {
            var ieLength = metaDataBytes.Length - index;
            if (ieLength <= 0) return new byte[0];

            var ieBytes = new byte[ieLength];
            Array.Copy(metaDataBytes, index, ieBytes, 0, ieLength);

            return ieBytes;
        }

        public static byte[] ExtractBytes(byte[] metaDataBytes, int index, int len)
        {
            if (metaDataBytes.Length - index < len) return new byte[0];

            var newBytesArray = new byte[len];
            Array.Copy(metaDataBytes, index, newBytesArray, 0, len);

            return newBytesArray;
        }

        public byte[] AnonymizePacketBytes(PacketMetaData packet)
        {
            var anonymizedPacketByteList = new List<byte>();
            var realPacketBytesPointer = 0;

            // Add all the bytes until MAC addresses starts
            for (; realPacketBytesPointer < MAC_ADDRESS_START_INDEX; realPacketBytesPointer++)
            {
                anonymizedPacketByteList.Add(packet.ItsPacketBytes[realPacketBytesPointer]);
            }

            AnonymizeMacAddresses(packet, anonymizedPacketByteList);

            // Move pointer to the next byte position of the MAC addresses
            realPacketBytesPointer = anonymizedPacketByteList.Count;

            if (IsSsidAvailable(packet))
            {
                var ssidTagIndex = GetSsidTagIndex(packet);
                var ssidLengthIndex = ssidTagIndex + 1;
                var ssidIndex = ssidTagIndex + 2;

                // Add bytes until SSID length data position
                for (; realPacketBytesPointer < ssidLengthIndex; realPacketBytesPointer++)
                {
                    anonymizedPacketByteList.Add(packet.ItsPacketBytes[realPacketBytesPointer]);
                }

                var ssidLength = packet.ItsPacketBytes[ssidLengthIndex];
                var ssid = Encoding.UTF8.GetString(packet.ItsPacketBytes, ssidIndex, ssidLength);

                var anonymizedSsid = GetAnonymousSsid(ssid);
                var anonymizedSsidBytes = Encoding.UTF8.GetBytes(anonymizedSsid);
                var anonymizedSsidBytesLength = anonymizedSsid.Length;

                // Add anonymized ssid length
                anonymizedPacketByteList.Add(Convert.ToByte(anonymizedSsidBytesLength));

                // Add anonymous ssid
                foreach (var ssidBytes in anonymizedSsidBytes)
                {
                    anonymizedPacketByteList.Add(ssidBytes);
                }

                // Move pointer to the next byte position of the SSID data of reat packet bytes
                realPacketBytesPointer = ssidIndex + packet.ItsPacketBytes[ssidLengthIndex];
            }

            if (IsEapHeaderExists(packet.ItsPacketBytes))
            {
                int startIndex802_1X = QOS_DATA_HEADER_LENGTH + LLC_HEADER_LENGTH;

                for (; realPacketBytesPointer < startIndex802_1X + BASIC_INFO_LENGTH_802_1X; realPacketBytesPointer++)
                {
                    anonymizedPacketByteList.Add(packet.ItsPacketBytes[realPacketBytesPointer]);
                }

                anonymizedPacketByteList[startIndex802_1X + 2] = anonymizedPacketByteList[startIndex802_1X + 3] = 0;

                return anonymizedPacketByteList.ToArray();
            }

            // Add the remaining data from main packet bytes
            for (; realPacketBytesPointer < packet.ItsPacketBytes.Length; realPacketBytesPointer++)
            {
                anonymizedPacketByteList.Add(packet.ItsPacketBytes[realPacketBytesPointer]);
            }

            return anonymizedPacketByteList.ToArray();
        }

        private void AnonymizeMacAddresses(PacketMetaData packet, List<byte> outputBytes)
        {
            var currentIndex = MAC_ADDRESS_START_INDEX;

            var availableAddressCount = GetAvailableMacAddressCount(packet);

            for (int i = 1; i <= availableAddressCount; i++)
            {
                // For WDS packet, there are 2 bytes present between 3rd and 4th address for Sequence Control
                if (i == WDS_ADDRESS_COUNT)
                {
                    outputBytes.Add(packet.ItsPacketBytes[currentIndex++]);
                    outputBytes.Add(packet.ItsPacketBytes[currentIndex++]);
                }

                if (packet.ItsPacketBytes.Length < currentIndex + 6) return;

                var addressBytes = ExtractBytes(packet.ItsPacketBytes, currentIndex, 6);
                var randomizedAddress = GetAnonymousMacAddress(addressBytes);

                for (int j = 0; j < randomizedAddress.Length; j++)
                {
                    outputBytes.Add(randomizedAddress[j]);
                }

                currentIndex += 6;
            }
        }

        private uint GetAvailableMacAddressCount(PacketMetaData packet)
        {
            var packetBytes = packet.ItsPacketBytes;
            var basicFrameType = (packetBytes[0] & FRAME_TYPE_BIT) >> 2; // LSB 3rd and 4th
            var subFrameType = packet.ItsFrameType;

            if (basicFrameType == MANAGEMENT_FRAME_TYPE_CODE)
            {
                return MANAGEMENT_FRAME_ADDRESS_COUNT;
            }
            else if (basicFrameType == CONTROL_FRAME_TYPE_CODE)
            {
                // CTS, ACK, and Control Wrapper(0x74) contains one address
                if (subFrameType == FrameSubType.ClearToSend || subFrameType == FrameSubType.Ack || subFrameType == 0x74)
                {
                    return 1;
                }

                return CONTROL_FRAME_ADDRESS_COUNT;
            }
            else
            {
                // WDS available when both To-DS and DS bit is 1
                var wdsAvailableFlag = ((packet.ItsPacketBytes[1] & 0x01) & (packet.ItsPacketBytes[1] & 0x02)) > 0;

                if (wdsAvailableFlag) return WDS_ADDRESS_COUNT;
                else return DATA_FRAME_ADDRESS_COUNT;
            }
        }

        private byte[] GetAnonymousMacAddress(byte[] currentAddressBytes)
        {
            _anonymousInfoService.AnonymizeLastThreeAddressBytes(currentAddressBytes);

            return currentAddressBytes;
        }

        private string GetAnonymousSsid(string currentSsid)
        {
            var anonymousSsid = string.Empty;
            if (!currentSsid.IsNullOrEmpty())
            {
                anonymousSsid = _anonymousInfoService.GetAnonymousSSID(currentSsid);
            }

            return anonymousSsid;
        }

        private bool IsSsidAvailable(PacketMetaData packet)
        {
            var packetBytes = packet.ItsPacketBytes;
            var basicFrameType = (packetBytes[0] & FRAME_TYPE_BIT) >> 2; // LSB 3rd and 4th bit
            if (basicFrameType != MANAGEMENT_FRAME_TYPE_CODE) return false;

            int ssidTagIndex = GetSsidTagIndex(packet);
            if (ssidTagIndex < 0) return false;

            var byteLength = packetBytes.Length;
            if (byteLength > ssidTagIndex)
            {
                if ((uint)packetBytes[ssidTagIndex] == (uint)InformationElementId.Ssid)
                {
                    if (byteLength < ssidTagIndex + 1) return false;

                    var ssidLength = packetBytes[ssidTagIndex + 1];

                    var ssidIndex = ssidTagIndex + 2;
                    if (byteLength < ssidIndex + ssidLength) return false;

                    var ssid = Encoding.UTF8.GetString(packetBytes, ssidIndex, ssidLength);

                    if (!ssid.IsNullOrEmpty())
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private int GetSsidTagIndex(PacketMetaData packet)
        {
            var ssidTagIndex = -1;

            if (packet.ItsFrameType == FrameSubType.ActionNeighborReportRequest)
            {
                ssidTagIndex = GetPacketStartIndex(packet.ItsPacketBytes) + NEIGHBOR_REPORT_FIXED_PARAMETER_SIZE;
            }
            else if (packet.ItsFrameType == FrameSubType.ProbeRequest)
            {
                ssidTagIndex = GetPacketStartIndex(packet.ItsPacketBytes);
            }
            else if (packet.ItsFrameType == FrameSubType.Beacon || packet.ItsFrameType == FrameSubType.ProbeResponse)
            {
                ssidTagIndex = GetPacketStartIndex(packet.ItsPacketBytes) + BEACON_FIXED_PARAMETERS_SIZE;
            }
            else if (packet.ItsFrameType == FrameSubType.AssociationRequest)
            {
                ssidTagIndex = GetPacketStartIndex(packet.ItsPacketBytes) + ASSOC_REQ_FIXED_PARAMETERS_SIZE;
            }
            else if (packet.ItsFrameType == FrameSubType.ReassociationRequest)
            {
                ssidTagIndex = GetPacketStartIndex(packet.ItsPacketBytes) + REASSOC_REQ_FIXED_PARAMETERS_SIZE;
            }

            return ssidTagIndex;
        }

        private bool IsEapIdentityAvailable(byte[] packetBytes)
        {
            int startIndex802_1X = QOS_DATA_HEADER_LENGTH + LLC_HEADER_LENGTH;

            if (!IsEapHeaderExists(packetBytes)) return false;

            var responseCodeIndx = startIndex802_1X + 4;
            if (responseCodeIndx >= packetBytes.Length) return false;
            var responseCode = (uint)packetBytes[responseCodeIndx];

            if (responseCode != EAP_RESPONSE_CODE) return false;

            var identityCodeIdx = startIndex802_1X + 8;
            var identityCode = (uint)packetBytes[identityCodeIdx];

            if (identityCode != EAP_IDENTITY_TYPE) return false;

            var identity = Encoding.UTF8.GetString(packetBytes, identityCodeIdx, packetBytes.Length - identityCodeIdx);

            if (identity != null) return true;

            return false;
        }

        private bool IsEapHeaderExists(byte[] packetBytes)
        {
            if (packetBytes.Length < 37) return false;
            var llcHeader = BitConverter.ToUInt64(packetBytes, QOS_DATA_HEADER_LENGTH);

            if (llcHeader != EAPOL_LLC_HEADER) return false;

            return true;
        }

        private EapResponseCodes GetEapResponseCode(byte[] packetBytes)
        {
            int startIndex802_1X = QOS_DATA_HEADER_LENGTH + LLC_HEADER_LENGTH;
            var responseCodeIndx = startIndex802_1X + 4;
            var responseCode = (EapResponseCodes)packetBytes[responseCodeIndx];

            return responseCode;
        }

        #endregion
    }
}
