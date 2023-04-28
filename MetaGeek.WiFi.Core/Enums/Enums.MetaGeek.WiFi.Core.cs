using System;

namespace MetaGeek.WiFi.Core.Enums
{
    [Flags]
    public enum ScannerTypes
    {
        Unknown = 0x00,
        Pcap = 0x01,
        WlanPi = 0x02,
        Scanner = 0x04,
        MonitorMode = 0x08,
    }

    public enum ScanningState
    {
        FindingAdapters,
        ScanAllChannels,
        ScanEssid,
        MonitorBssid,
        MonitorClient
    }

    public enum WiFiConnectionState
    {
        Unknown,
        Connected,
        Disconnected,
        ConnectionFailed,
        Roamed,
    }

    public enum WiFiConnectionStatus
    {
        NotConnectedToAdapter,
        AdapterNotConnectedToAp,
        Ok,
        Error
    }

    public enum MacAddressType
    {
        Unknown = 0,
        Normal = 1,
        SpanningTree = 2,
        Broadcast = 3,
        Ipv4Multicast = 4,
        Ipv6Multicast = 5,
        PrecisionTime = 6,
        IecMulticast = 7,
        CiscoProtocols = 8,
        Unresolved = 9,
    }

    [Flags]
    public enum WlanEncryptionTypes
    {
        UNKNOWN = 0x00,
        OPEN = 0x01,
        WEP = 0x02,
        TKIP = 0x04,
        CCMP = 0x08,
    }

    public enum ChannelBand
    {
        TwoGhz,
        FiveGhz,
        SixGhz,
        Both,
        FiveGhzUnii1,
        FiveGhzUnii2,
        FiveGhzUnii3,
        FiveGhzIsm,
        NineHundredMhzIsm,
        Zigbee
    }

    [Flags]
    public enum PhyTypes
    {
        Unknown = 0x0,
        B = 0x01,
        A = 0x02,
        G = 0x04,
        N = 0x08,
        Ac = 0x10,
        Ax = 0x20,
    }

    public enum ChannelWidth
    {
        Twenty = 0,
        Forty = 1,
        Eighty = 2,
        OneSixty = 3,
        EightyPlusEighty = 4
    }

    public static class ChannelWidthExtensions
    {
        public static string ToFriendlyString(this ChannelWidth width)
        {
            switch (width)
            {
                case ChannelWidth.Twenty:
                    return "20";
                case ChannelWidth.Forty:
                    return "40";
                case ChannelWidth.Eighty:
                    return "80";
                case ChannelWidth.EightyPlusEighty:
                    return "80+80";
                case ChannelWidth.OneSixty:
                    return "160";
                default:
                    return "unknown";
            }
        }
    }

    public static class ChannelBandExtensions
    {
        public static string ToFriendlyString(this ChannelBand band)
        {
            switch (band)
            {
                case ChannelBand.TwoGhz:
                    return "2.4 GHz";

                case ChannelBand.FiveGhz:
                    return "5 GHz";

                case ChannelBand.Both:
                    return "2.4 & 5 GHz";

                default:
                    return "unknown";
            }
        }
    }

    public enum NetworkType
    {
        Infrastructure = 0,
        Independent = 1,
        Any
    }

    [Flags]
    public enum AuthenticationTypes
    {
        OPEN = 0x00,
        WEP = 0x01,
        WPA_PRE_SHARED_KEY = 0x02,
        WPA_ENTERPRISE = 0x04,
        WPA2_PRE_SHARED_KEY = 0x08,
        WPA2_ENTERPRISE = 0x10,
        WPA3_PRE_SHARED_KEY = 0x20,
        WPA3_ENTERPRISE = 0x40,
    }

    public static class AkmBroadcastType
    {
        public const byte Unknown = 0;
        public const byte Enterprise = 1;
        public const byte Personal = 2;
        public const byte FtEnterprise = 3;
        public const byte FtPersonal = 4;
        public const byte SaePersonal = 8;
        public const byte FtSaePersonal = 9;
        public const byte SaeEnterprise = 12;
        public const byte FtSaeEnterprise = 13;
    }

    [Flags]
    public enum AuthenticationKeyManagementTypes
    {
        UNKNOWN = 0x00,
        PERSONAL = 0x01,
        FT_PERSONAL = 0x02,
        ENTERPRISE = 0x04,
        FT_ENTERPRISE = 0x08,
        SAE_PERSONAL = 0x10,
        SAE_ENTERPRISE = 0x20,
        FT_SAE_PERSONAL = 0x40,
        FT_SAE_ENTERPRISE = 0x80,
        None
    }

    public static class FrameSubType
    {
        public const uint AssociationRequest = 0x00;
        public const uint AssociationResponse = 0x10;
        public const uint ReassociationRequest = 0x20;
        public const uint ReassociationResponse = 0x30;
        public const uint ProbeRequest = 0x40;
        public const uint ProbeResponse = 0x50;
        public const uint Beacon = 0x80;
        public const uint Disassociation = 0xA0;
        public const uint Authentication = 0xB0;
        public const uint Deauthentication = 0xC0;
        public const uint Action = 0xD0;
        public const uint ActionNoAck = 0xE0;
        public const uint ActionVhtBeamforming = 0x1500E0;
        public const uint ActionSpectrumPowerRequest = 0x0002D0;
        public const uint ActionSpectrumPowerReport = 0x0003D0;
        public const uint ActionChannelSwitchAnnouncement = 0x0004D0;

        public const uint ActionQosAddTsRequest = 0x0100D0;
        public const uint ActionQosAddTsResponse = 0x0101D0;
        public const uint ActionQosDeleteTs = 0x0102D0;
        public const uint ActionQosSchedule = 0x0103D0;
        public const uint ActionQosMapConfigure = 0x0104D0;

        public const uint ActionDlsRequest = 0x0200D0;
        public const uint ActionDlsResponse = 0x0201D0;
        public const uint ActionDlsTeardown = 0x0202D0;

        public const uint ActionAddBlockAckRequest = 0x0300D0;
        public const uint ActionAddBlockAckResponse = 0x0301D0;
        public const uint ActionDeleteBlockAckRequest = 0x0302D0;

        public const uint ActionNeighborReportRequest = 0x0504D0;
        public const uint ActionNeighborReportResponse = 0x0505D0;
        public const uint ActionRadioMeasurementUnknown = 0x0500D0;

        public const uint ActionHtSmPowerSave = 0x0701D0;

        public const uint Trigger = 0x24;
        public const uint BeamformingReportPoll = 0x44;
        public const uint VhtNdpAccouncement = 0x54;
        public const uint BlockAckRequest = 0x84;
        public const uint BlockAck = 0x94;
        public const uint PsPoll = 0xA4;
        public const uint RequestToSend = 0xB4;
        public const uint ClearToSend = 0xC4;
        public const uint Ack = 0xD4;
        public const uint CfEnd = 0xE4;

        public const uint Data = 0x08;
        public const uint Null = 0x48;
        public const uint QosData = 0x88;
        public const uint QosNull = 0xC8;
        public const uint EapolHandshake1 = 0xF188;
        public const uint EapolHandshake2 = 0xF288;
        public const uint EapolHandshake3 = 0xF388;
        public const uint EapolHandshake4 = 0xF488;
        public const uint InferredData = 0xFF08;
        public const uint Eap = 0xFA88;
        public const uint EapTls = 0xFB88;
        public const uint EapPeap = 0xFC88;

        public const uint ExtendedNetwork = 0xFF0C;
        public const uint NeighborsNetwork = 0xFF0D;
    }

    // Other element IDs seen in wild...
    // 47
    public enum InformationElementId
    {
        Ssid = 0,                   // 0x00
        SupportedRates = 1,         // 0x01
        DsParameterSet = 3,        // 0x03
        TrafficIndicationMap = 5,   // 0x05
        CountryInformation = 7,     // 0x07
        BssLoad = 11,              // 0x0B
        PowerCapability = 33,       // 0x21
        SupportedChannels = 36,     // 0x24
        ErpParameter = 42,          // 0x2A
        HtCapabilities = 45,        // 0x2D
        OverlappingBssScan = 47,    // 0x2F
        RobustSecurityNetwork = 48, // 0x30
        ExtendedRates = 50,         // 0x32
        NeighborReport = 52,        // 0x34
        HtInformation = 61,         // 0x3D
        ExtendedCapabilities = 127, // 0x7F
        CiscoDeviceName = 133,      // 0x85
        VHtCapabilities = 191,      // 0xBF
        VHtOperation = 192,         // 0xC0
        VendorSpecific = 221,       // 0xDD
        Extension = 255,            // 0xFF
        RmCapabilities = 70         // 0x46
    }

    public enum InformationElementExtensionId
    {
        HeCapabilities = 0x23,
        HeOperation = 0x24,
    }

    public enum WiFiEventTypes
    {
        None,
        Channel,
        Network,
        Bssid,
        Client,
        ClientDetails
    }

    public enum WiFiEventSeverity
    {
        Unknown,
        Informational,
        Warning,
        Critical
    }

    [Flags]
    public enum ClientWiFiEvents
    {
        None = 0x00,
        Associated = 0x01,
        Roamed = 0x02,
        Beamforming = 0x04,
        SpectrumPowerReport = 0x08,
        NeighborReport = 0x10,
        SecurityHandshake = 0x20,
        ClientDiscovered = 0x40,
        Disassociated = 0x80,
        TargetedProbeRequest = 0x100,
        WildcardProbeRequest = 0x200,
        Reassociated = 0x400,
        Successful8021X = 0x800,
        Failed8021X = 0x1000,
        SuccessfulWPA = 0x2000,
        FailedConnection = 0x4000,
        AssumedRoam = 0x8000,
        AssumedSuccessfullWPA = 0x10000
    }

    [Flags]
    public enum ClientConnectionActivities
    {
        Unknown = 0x00,
        WildcardProbeRequest = 0x01,
        TargetedProbeRequest = 0x02,
        TargetedProbeResponse = 0x04,
        Authenticated = 0x08,
        AssociationRequest = 0x10,
        Associated = 0x20,
        HandshakeKey1 = 0x40,
        HandshakeKey2 = 0x80,
        HandshakeKey3 = 0x100,
        HandshakeKey4 = 0x200,
        Connected = 0x400,
        Disassociated = 0x800,
        Reassociated = 0x1000,
        ReassociationRequest = 0x2000
    }

    public enum DeviceCategories
    {
        Unknown = 0,
        Computer,
        Phone,
        IoT,
        Printer,
        AccessPoint,
        MediaStreaming,
        NetworkStorage,
        Networking,
        Broadcast
    }

    public enum ChannelSweepState
    {
        NoChannel = 1001,
        SingleChannel,
        AllChannels,
        TwoGHzChannels,
        FiveGHzChannels,
        LowerFiveGHzChannels,
        MiddleFiveGHzChannels,
        HigherFiveGHzChannels,
        MixedChannels
    }

    public enum NetworkGraphBandFilters
    {
        TwoGhz,
        FiveGhz,
        SixGhz
    }

    public enum ChannelGraphDisplayToggles
    {
        APs,
        APDensity,
        CurrentTrace,
        WiSpyDensity
    }

    public enum EapResponseCodes
    {
        Request = 0x01,
        Response,
        Success,
        Failure
    }
}
