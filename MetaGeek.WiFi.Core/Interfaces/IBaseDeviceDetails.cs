using System;
using System.Collections.Generic;
using MetaGeek.WiFi.Core.Models;

namespace MetaGeek.WiFi.Core.Interfaces
{
    public interface IBaseDeviceDetails
    {
        string ItsName { get; }
        string ItsBroadcastName { get; set; }
        string ItsAlias { get; set; }
        string ItsVendor { get; set; }
        string ItsMakeModel { get; set; }
        IMacAddress ItsMacAddress { get; }
        int? ItsRssi { get; }
        int? ItsNoise { get; }
        int? ItsSignalToNoiseRatio { get; }
        byte[] ItsInformationElementBytes { get; set; }
        PhyTypeInfo ItsPhyTypeInfo { get; set; }
        double ItsMaxDataRate { get; set; }
        double ItsAirTimePercentage { get; set; }
        double? ItsRetryRate { get; set; }
        uint ItsMaxMcsIndex { get; set; }
        uint ItsSpacialStreamCount { get; set; }
        DateTime ItsFirstSeenDateTime { get; }
        DateTime ItsLastSeenDateTime { get; set; }
        string ItsLastSeenTimeSpanString { get; set; }
        List<double> ItsBasicRates { get; }
        string ItsBasicRatesString { get; }
        ushort ItsCapabilitiesInformation { get; set; }
        bool ItsSupportsBssTransitionFlag { get; set; }
        bool ItsNeighborReportCapabilityFlag { get; set; }
        bool ItsProtectedManagementFramesCapabilityFlag { get; set; }
        bool ItsFastRoamingCapabilityFlag { get; set; }
        ushort ItsInterval { get; set; }
        string ItsTaxonomySignature { get; set; }
        SecurityInfo ItsSecurityInfo { get; set; }

        List<DeviceTimeSegment> GetAllTimeSegments();
        void TrimTimeSegmentCollection(DateTime trimDateTime);
    }
}
