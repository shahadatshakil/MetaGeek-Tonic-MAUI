using MetaGeek.Tonic.Common.Enums;
using MetaGeek.Tonic.Common.Models;
using MetaGeek.WiFi.Core.Interfaces;
using Prism.Events;
using System;

namespace MetaGeek.Tonic.Common.Events
{
    public class RestartAppEvent : PubSubEvent<bool>
    {
    }

    public class WiSpyDeviceStateChangedEvent : PubSubEvent<bool>
    {
    }

    public class CaptureFileParseRequestEvent : PubSubEvent<CaptureFileParseRequestEventArgs>
    {
    }

    public class ChangeNetworkUtilizationGraphRequestEvent : PubSubEvent<NetworkUtilizationGraphType>
    {
    }

    public class IncompatibleFileLoadingEvent : PubSubEvent<EventArgs>
    {
    }

    public class IncompatibleFileWarningViewClosedEvent : PubSubEvent<EventArgs>
    {
    }

    public class RequestDefaultAppViewEvent : PubSubEvent<EventArgs>
    {
    }

    public class CaptureSelectorTimeRangeChangedEvent : PubSubEvent<CaptureSelectorTimeRangeChangedEventArgs>
    {
    }

    public class CoverageThresholdsViewEssidSelectionChangedEvent : PubSubEvent<IEssidDetails>
    {
    }
}
