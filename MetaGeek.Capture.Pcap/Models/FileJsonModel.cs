using System.Collections.Generic;
using MetaGeek.WiFi.Core.Interfaces;
using MetaGeek.WiFi.Core.Models;
using Newtonsoft.Json;

namespace MetaGeek.Capture.Pcap.Models
{
    public class FileJsonModel
    {
        [JsonProperty("data")]
        public FileData ItsData { get; set; }
    }

    public class FileData
    {
        [JsonProperty("created_at", Required = Required.Always)] 
        public long ItsTimestamp { get; set; }

        [JsonProperty("site_name")]
        public string ItsSiteName { get; set; }

        [JsonProperty("room")]
        public FileRoom ItsRoom { get; set; }

        [JsonProperty("access_points")]
        public List<FileAccessPoint> ItsAccessPoints { get; set; }
        
        [JsonProperty("neighbor_snapshots")]
        public List<FileNeighborSnapshot> ItsNeighborSnapshots { get; set; }

        [JsonProperty("bssids_seen")] 
        public Dictionary<ulong, string> ItsBssidsSeen { get; set; }

        [JsonProperty("channel_utilization")]
        public FileChannelCollection ItsChannelCollection { get; set; }
    }

    public class FileRoom
    {
        [JsonProperty("room_id")]
        public int ItsId { get; set; }

        [JsonProperty("room_name")]
        public string ItsRoomName { get; set; }
    }

    public class FileAccessPoint
    {
        #region Properties

        [JsonProperty("id")] 
        public int? ItsId { get; set; }

        [JsonProperty("alias")]
        public string ItsAlias { get; set; }

        [JsonProperty("vendor")]
        public string ItsVendor { get; set; }

        [JsonProperty("radios")]
        public List<FileRadio> ItsRadios { get; set; }

        #endregion
    }

    public class Connected
    {
        #region Properties
        
        [JsonProperty("data_rate")]
        public int? ItsDataRate { get; set; }

        #endregion
    }
    public class FileRadio
    {
        #region Properties
        [JsonProperty("rssi")]
        public int ItsRssi { get; set; }

        [JsonProperty("utilization", NullValueHandling = NullValueHandling.Ignore)]
        public double? ItsChannelUtilization { get; set; }

        [JsonProperty("channel")]
        public uint ItsChannel { get; set; }

        [JsonProperty("secondary_channel")]
        public uint? ItsSecondaryChannel { get; set; }

        [JsonProperty("channel_width")]
        public string ItsChannelWidth { get; set; }

        [JsonProperty("co_channel_count")]
        public int? ItsCoChannelCount { get; set; }

        [JsonProperty("overlapping_channel_count")]
        public int? ItsOverlappingChannelCount { get; set; }

        [JsonProperty("connected")]
        public Connected ItsConnected { get; set; }

        [JsonProperty("bssids")]
        public List<FileBssid> ItsBssids { get; set; }

        #endregion
    }

    public class FileBssid
    {
        #region Fields
        private ulong _macAddressUlongValue;

        #endregion

        #region Properties
        [JsonProperty("mac_address")]
        public ulong ItsMacAddressUlongValue
        {
            get { return _macAddressUlongValue; }
            set
            {
                _macAddressUlongValue = value;
                ItsMacAddress = MacAddressCollection.GetMacAddress(value);
            }
        }

        [JsonIgnore]
        public IMacAddress ItsMacAddress { get; private set; }

        [JsonProperty("client_count")]
        public int ItsClientCount { get; set; }

        [JsonProperty("ssid")]
        public string ItsSsid { get; set; }

        [JsonProperty("rssi")]
        public int? ItsRssi { get; set; }

        [JsonProperty("min_basic_rate")]
        public double? ItsMinBasicRate { get; set; }

        [JsonProperty("max_data_rate")] 
        public double? ItsMaxDataRate { get; set; }

        [JsonProperty("phy_types")] 
        public List<string> ItsPhyTypes { get; set; }

        [JsonProperty("mcs_support")] 
        public uint? ItsMaxMcsIndex { get; set; }

        [JsonProperty("spatial_streams")] 
        public uint? ItsSpatialStreamCount { get; set; }

        #endregion
    }

    public class FileNeighborSnapshot
    {
        #region Fields
        private ulong _macAddressUlongValue;

        #endregion

        [JsonProperty("mac_address")]
        public ulong ItsMacAddressUlongValue
        {
            get { return _macAddressUlongValue; }
            set
            {
                _macAddressUlongValue = value;
                ItsMacAddress = MacAddressCollection.GetMacAddress(value);
            }
        }

        [JsonIgnore]
        public IMacAddress ItsMacAddress { get; private set; }
        
        [JsonProperty("rssi")]
        public int ItsRssi { get; set; }

        [JsonProperty("airtime", NullValueHandling = NullValueHandling.Ignore)]
        public double? ItsAirtime { get; set; }

        [JsonProperty("channel")]
        public uint ItsChannel { get; set; }

        [JsonProperty("secondary_channel", NullValueHandling = NullValueHandling.Ignore)]
        public uint? ItsSecondaryChannel { get; set; }

        [JsonProperty("channel_width")]
        public string ItsChannelWidth { get; set; }
    }

    public class FileChannelCollection
    {
        [JsonProperty("average")]
        public double? ItsAverage { get; set; }

        [JsonProperty("max")]
        public double? ItsMax { get; set; }

        [JsonProperty("channels", NullValueHandling = NullValueHandling.Ignore)]
        public List<FileChannelUtilization> ItsChannelUtilizations { get; set; }
    }

    public class FileChannelUtilization
    {
        [JsonProperty("channel")]
        public uint ItsChannelNumber { get; set; }

        [JsonProperty("utilization")]
        public double ItsUtilization { get; set; }
    }
}
