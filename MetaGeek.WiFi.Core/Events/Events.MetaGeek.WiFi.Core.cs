using MetaGeek.WiFi.Core.Enums;
using MetaGeek.WiFi.Core.Interfaces;
using MetaGeek.WiFi.Core.Models;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaGeek.WiFi.Core.Events
{
    public class ScanCompletedEventData
    {
        public DateTime ItsScanDateTime { get; }
        public ScannerTypes ItsScannerType { get; }
        public IEssidDetails ItsEssidDetails { get; }
        public bool ItsScanFoundNewBssidFlag { get; }
        public DataSourceInfo ItsDataSourceInfo { get; }

        public ScanCompletedEventData(DateTime scanDateTime, ScannerTypes scannerType, DataSourceInfo dataSourceInfo, bool newBssidFoundFlag = true)
        {
            ItsScanDateTime = scanDateTime;
            ItsScannerType = scannerType;
            ItsDataSourceInfo = dataSourceInfo;
            ItsScanFoundNewBssidFlag = newBssidFoundFlag;
        }

        public ScanCompletedEventData(DateTime scanDateTime, ScannerTypes scannerType, DataSourceInfo dataSourceInfo, IEssidDetails essidDetails)
        {
            ItsScanDateTime = scanDateTime;
            ItsScannerType = scannerType;
            ItsDataSourceInfo = dataSourceInfo;
            ItsEssidDetails = essidDetails;
        }
    }

    public class AdapterInfoListUpdatedEvent : PubSubEvent<AdapterInfoList>
    {
    }

    public class ClientAddedEvent : PubSubEvent<IClientDetails>
    {
    }

    public class BssidAddedEvent : PubSubEvent<IBssidDetails>
    {
    }

    public class ClearDataRequestEvent : PubSubEvent<EventArgs>
    {
    }

    public class ConnectedMacAddressEvent : PubSubEvent<IMacAddress>
    {
    }

    public class ClearViewsRequestEvent : PubSubEvent<EventArgs>
    {
    }

    public class ScannerServiceReadyEvent : PubSubEvent<ScannerTypes>
    {
    }

    public class ScannerServiceNotReadyEvent : PubSubEvent<ScannerTypes>
    {
    }

    /// <summary>
    /// True is Airplane Mode ON, False is Airplane Mode OFF
    /// </summary>
    public class AirplaneModeEvent : PubSubEvent<bool>
    {
    }

    public class AllChannelsScanCompletedEvent : PubSubEvent<ScanCompletedEventData>
    {
    }

    public class ChannelScanCompletedEvent : PubSubEvent<ChannelScanInfo>
    {
    }

    public class EssidDetailsScanCompletedEvent : PubSubEvent<ScanCompletedEventData>
    {
    }

    public class WiFiConnectionChangedEvent : PubSubEvent<ConnectionAttributes>
    {
    }

    public class WiFiConnectionQualityEvent : PubSubEvent<ConnectionQualityDetails>
    {
    }

    public class ConnectedLinkSpeedChangedEvent : PubSubEvent<double?>
    {
    }

    public class ScanningStateChangedEvent : PubSubEvent<ScanningState>
    {
    }

    public class ScannerControllerInitializedEvent : PubSubEvent<EventArgs>
    {
    }

    public class RestartPacketScannerRequestEvent : PubSubEvent<EventArgs>
    {
    }

    public class RequestScanningStartedStateEvent : PubSubEvent<EventArgs>
    {
    }

    public class StateChangeRequestScanAllNetworksEvent : PubSubEvent<EventArgs>
    {
    }

    public class StateChangeRequestScanNetworkEvent : PubSubEvent<IEssidDetails>
    {
    }

    public class StateChangeRequestScanBssidEvent : PubSubEvent<IBssidDetails>
    {
    }

    public class StateChangeRequestScanClientEvent : PubSubEvent<IClientDetails>
    {
    }

    public class StopScanningRequestEvent : PubSubEvent<EventArgs>
    {
    }

    public class ConnectWlanPiRequestEvent : PubSubEvent<string>
    {
    }

    public class ResumeScanningRequestEvent : PubSubEvent<EventArgs>
    {
    }

    public class SiteChangedEvent : PubSubEvent<EventArgs>
    {
    }

    public class ChooseScanningModeRequestEvent : PubSubEvent<bool>
    {
    }

    /// <summary>
    /// True if Live, False if recording (pcap)
    /// </summary>
    public class LiveStatusChangedEvent : PubSubEvent<bool>
    {
    }

    public class CaptureChannelChangedEvent : PubSubEvent<List<uint>>
    {
    }

    public class WiFiEventAddedEvent : PubSubEvent<WiFiEventMetaData>
    {
    }

    public class WiFiEventUpdatedEvent : PubSubEvent<WiFiEventMetaData>
    {
    }

    public class WiFiEventsTrimmedEvent : PubSubEvent<List<WiFiEventMetaData>>
    {
    }

    public class PacketListUpdatedEvent : PubSubEvent<EventArgs>
    {
    }

    public class WiFiCollectionsUpdatedOnTimeFrameChangedEvent : PubSubEvent<EventArgs>
    {
    }

    public class WiFiCollectionsUpdatedEvent : PubSubEvent<EventArgs>
    {
    }

    public class ChannelsToScanListUpdatedEvent : PubSubEvent<List<uint>>
    {
    }

    public class LivePacketCaptureEvent : PubSubEvent<PacketMetaData>
    {
    }

    public class DefaultCaptureDirectoryChangedEvent : PubSubEvent<EventArgs>
    {
    }

    public class ContinuoiusPcapWriteIterationCompletedEvent : PubSubEvent<string>
    {
    }

    public class ClientWiFiEvent : PubSubEvent<WiFiEventMetaData>
    {
    }

    public class BssidUpdatedOnClientEvent : PubSubEvent<IClientDetails>
    {
    }

    public class ClientDetailsEvent : PubSubEvent<ClientMetaData>
    {
    }

}
