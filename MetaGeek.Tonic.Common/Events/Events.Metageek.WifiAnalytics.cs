using System;
using System.Collections.Generic;
using MetaGeek.Tonic.Common.Models;
using MetaGeek.WiFi.Core.Enums;
using MetaGeek.WiFi.Core.Interfaces;
using MetaGeek.WiFi.Core.Models;
using Prism.Events;

namespace MetaGeek.Tonic.Common.Events
{
    public class ChannelUpdatedEvent : PubSubEvent<IChannelDetails>
    {
    }

    public class AccessPointMetaDataChangedEvent : PubSubEvent<IBssidDetails>
    {
    }

    public class ClientMetaDataChangedEvent : PubSubEvent<ClientMetaData>
    {
    }


    /// <summary> 
    /// Percentage of packets that have been saved to pcap
    /// </summary>
    public class PcapSaveProgressEvent : PubSubEvent<double>
    {
    }

    public class PcapOpenProgressEvent : PubSubEvent<double>
    {
    }

    public class MaxRssiIncreasedEvent : PubSubEvent<int>
    {
    }

    public class ClientActionProcessedEvent : PubSubEvent<WiFiEventMetaData>
    {
    }

    public class PossibleSiteChangedEvent : PubSubEvent<string>
    {
    }

    public class SiteTopologyInitializedEvent : PubSubEvent<bool>
    {
    }

    public class NetworkObservationCountChangedEvent : PubSubEvent<int>
    {
    }

    // EXPERIMENTAL FOR PCAP OPEN FAILURES - might become general failure event
    public class ScannerFailureEvent : PubSubEvent<string>
    {
    }

    public class PreEssidCollectionUpdatedEvent : PubSubEvent<EventArgs>
    {
    }
}
