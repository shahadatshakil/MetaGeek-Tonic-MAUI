using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaGeek.WiFi.Core.Enums
{
    #region Enumerations

    public static class ActionFrameCategory
    {
        public const int SpectrumManagement = 0;
        public const int BlockAck = 3;
        public const int HighThroughput = 7;
        public const int Vht = 21;
        public const int VendorSpecific = 127;
    }


    public enum ObservationType
    {
        SecurityMismatch,
        ConfigurationMismatch,
        No24GhzClients,
        Wide24GhzChannel,
        NonStandard24GhzChannel,
        Overlapping24GhzChannel,
        OnQuietest24GhzChannel,
        RecommendQuiet24GhzChannel,
        OnLeastCrowdedChannel,
        RecommendLeastCrowdedChannel,
        WepSecurity,
        WpaSecurity,
        NotEnoughBandCoverage,
        TooMuchBandCoverage,
        TopTalker,
        CrowdedBssid,
        BusyBssid,
        BusyNeighborsOnChannel,
        HpPrinterNetwork,
        StrongSecurity,
        HighRetryRate,
    }

    public enum ObservationPriority
    {
        Kudos,
        Informational,
        Warning,
        Issue,
    }

    #endregion Enumerations
}
