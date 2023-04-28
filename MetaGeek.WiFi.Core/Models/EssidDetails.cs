using MetaGeek.WiFi.Core.Enums;
using MetaGeek.WiFi.Core.Helpers;
using MetaGeek.WiFi.Core.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Prism.Mvvm;

namespace MetaGeek.WiFi.Core.Models
{
    public class EssidDetails : BindableBase, IEssidDetails
    {
        #region Fields

        private const double FLOATING_POINT_TOLERANCE = 0.001;

        private string _channelsString;
        private int? _maxRssi;
        private int _clientCount;
        private int _packetCount;
        private int _bssCount;
        private ClientWiFiEvents _clientActions;
        private bool _connectedFlag;
        private Color _color;
        private double _maxAirTimePercentage;
        private DateTime _lastSeenDateTime;
        private string _lastSeenTimeSpanString;
        private string _basicRatesString;
        private bool _firstSessionSeenFlag;
        private bool _hasNewBssidFlag;
        private bool _isHiddenFlag;
        private TimeSpan _lastSeenTimeSpan;
        private object _essidDetailsTimeSegmentsLock;
        private SortedList<DateTime, DeviceTimeSegment> _essidDetailsTimeSegments;

        #endregion

        #region Properties

        public string ItsSsid { get; }

        public Color ItsColor
        {
            get { return _color; }
            set { _color = value; }
        }

        public ConcurrentDictionary<ulong, IBssidDetails> ItsBssidCollection { get; }
        public ConcurrentDictionary<ulong, IClientDetails> ItsClientCollection { get; }

        public PhyTypeInfo ItsAggregatePhyTypeInfo { get; }
        public SecurityInfo ItsAggregateSecurityInfo { get; }
        public SortedList<uint, ChannelInfo> ItsChannels { get; }
        public double ItsMaxDataRate { get; private set; }
        public List<double> ItsBasicRates { get; set; }

        public string ItsBasicRatesString
        {
            get { return _basicRatesString; }
            set
            {
                if (value == _basicRatesString) return;
                _basicRatesString = value;
                SetProperty(ref _basicRatesString, value);
            }
        }

        public int ItsBssCount
        {
            get { return _bssCount; }
            set
            {
                if (value == _bssCount) return;
                _bssCount = value;
                SetProperty(ref _bssCount, value);
            }
        }

        public string ItsChannelsString
        {
            get { return _channelsString; }
            private set
            {
                if (value == _channelsString) return;
                _channelsString = value;
                SetProperty(ref _channelsString, value);
            }
        }

        public int ItsPacketCount
        {
            get { return _packetCount; }
            set
            {
                if (value == _packetCount) return;
                _packetCount = value;
                SetProperty(ref _packetCount, value);
            }
        }

        public bool ItsFirstSessionSeenFlag
        {
            get { return _firstSessionSeenFlag; }
            set
            {
                if (value == _firstSessionSeenFlag) return;
                _firstSessionSeenFlag = value;
                SetProperty(ref _firstSessionSeenFlag, value);
            }
        }

        public bool ItsHasNewBssidFlag
        {
            get { return _hasNewBssidFlag; }
            set
            {
                if (value == _hasNewBssidFlag) return;
                _hasNewBssidFlag = value;
                SetProperty(ref _hasNewBssidFlag, value);
            }
        }

        public int ItsTotalClientCount
        {
            get { return _clientCount; }
            set
            {
                if (value == _clientCount) return;
                _clientCount = value;
                SetProperty(ref _clientCount, value);
            }
        }

        public int? ItsMaxRssi
        {
            get { return _maxRssi; }
            set
            {
                if (value == _maxRssi) return;
                _maxRssi = value;
                SetProperty(ref _maxRssi, value);
            }
        }

        public double ItsMaxAirTimePercentage
        {
            get { return _maxAirTimePercentage; }
            set
            {
                if (Math.Abs(value - _maxAirTimePercentage) < FLOATING_POINT_TOLERANCE) return;
                _maxAirTimePercentage = value;
                SetProperty(ref _maxAirTimePercentage, value);
            }
        }

        public ClientWiFiEvents ItsClientActions
        {
            get { return _clientActions; }
            set
            {
                if (value == _clientActions) return;
                _clientActions = value;
                SetProperty(ref _clientActions, value);
            }
        }

        public bool ItsConnectedFlag
        {
            get { return _connectedFlag; }
            set
            {
                if (value == _connectedFlag) return;
                _connectedFlag = value;
                SetProperty(ref _connectedFlag, value);
            }
        }

        public DateTime ItsLastSeenDateTime
        {
            get { return _lastSeenDateTime; }
            set
            {
                if (value == _lastSeenDateTime) return;
                _lastSeenDateTime = value;
                SetProperty(ref _lastSeenDateTime, value);
            }
        }

        public TimeSpan ItsLastSeenTimeSpan
        {
            get
            {
                return _lastSeenTimeSpan;
            }
            set
            {
                if (value == _lastSeenTimeSpan) return;
                _lastSeenTimeSpan = value;
                SetProperty(ref _lastSeenTimeSpan, value);
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

        public string ItsVendorsString
        {
            get
            {
                var vendors = new List<string>();

                foreach (var bssid in ItsBssidCollection.Values)
                {
                    if (!vendors.Contains(bssid.ItsVendor))
                    {
                        vendors.Add(bssid.ItsVendor);
                    }
                }

                return vendors.Count == 0 ? string.Empty : string.Join(",", vendors);
            }
        }

        public bool ItsHiddenFlag
        {
            get { return _isHiddenFlag; }
            set
            {
                if (value == _isHiddenFlag) return;
                _isHiddenFlag = value;
                SetProperty(ref _isHiddenFlag, value);
            }
        }

        #endregion

        #region Constructors

        public EssidDetails(string ssid)
        {
            ItsSsid = ssid;
            ItsBssidCollection = new ConcurrentDictionary<ulong, IBssidDetails>();
            ItsClientCollection = new ConcurrentDictionary<ulong, IClientDetails>();

            ItsAggregatePhyTypeInfo = new PhyTypeInfo();
            ItsAggregateSecurityInfo = new SecurityInfo();
            ItsChannels = new SortedList<uint, ChannelInfo>();
            ItsBasicRates = new List<double>();

            _essidDetailsTimeSegmentsLock = new object();
            _essidDetailsTimeSegments = new SortedList<DateTime, DeviceTimeSegment>();
        }

        #endregion

        #region Methods

        public void AddBssid(IBssidDetails bssidDetails)
        {
            var key = bssidDetails.ItsMacAddress.ItsUlongValue;
            if (ItsBssidCollection.ContainsKey(key)) return;

            var rebuildBasicRateString = false;
            ItsBssidCollection.TryAdd(bssidDetails.ItsMacAddress.ItsUlongValue, bssidDetails);

            // Update ALL "static" values with information from this new BSSID
            ItsAggregatePhyTypeInfo.Merge(bssidDetails.ItsPhyTypeInfo);
            ItsAggregateSecurityInfo.Merge(bssidDetails.ItsSecurityInfo);
            foreach (var bssidBasicRate in bssidDetails.ItsBasicRates)
            {
                if (!ItsBasicRates.Contains(bssidBasicRate))
                {
                    ItsBasicRates.Add(bssidBasicRate);
                    rebuildBasicRateString = true;
                }
            }

            if (!ItsChannels.ContainsKey(bssidDetails.ItsChannelInfo.ItsChannel))
            {
                ItsChannels.Add(bssidDetails.ItsChannelInfo.ItsChannel, bssidDetails.ItsChannelInfo);
                ItsChannelsString = TranslateChannelsToString();
            }

            if (rebuildBasicRateString)
            {
                ItsBasicRatesString = BuildBasicRatesString();
            }
        }

        public void Update(DateTime scanDateTime)
        {
            var clientCount = 0;
            int? maxRssi = null;
            var maxAirTime = 0.0;
            var essidPacketCount = 0;
            var clientActions = ItsClientActions;
            var connected = false;
            var lastSeen = DateTime.MinValue;

            ItsBssCount = ItsBssidCollection.Count;

            foreach (var bssid in ItsBssidCollection.Values)
            {
                if (bssid.ItsMaxDataRate > ItsMaxDataRate)
                {
                    ItsMaxDataRate = bssid.ItsMaxDataRate;
                }
                if (maxRssi == null || bssid.ItsRssi > maxRssi)
                {
                    maxRssi = bssid.ItsRssi;
                }

                if (bssid.ItsAirTimePercentage > maxAirTime)
                {
                    maxAirTime = bssid.ItsAirTimePercentage;
                }

                if (bssid.ItsConnectedFlag)
                {
                    connected = true;
                }

                var bssidLastSeenTimeSpan = scanDateTime - bssid.ItsLastSeenDateTime;
                bssid.ItsLastSeenTimeSpanString = Utils.TimeSpanToString(bssidLastSeenTimeSpan);
                if (bssid.ItsLastSeenDateTime > lastSeen)
                {
                    lastSeen = bssid.ItsLastSeenDateTime;
                }

                foreach (var client in bssid.ItsClientCollection.Values)
                {
                    if (!ItsClientCollection.ContainsKey(client.ItsMacAddress.ItsUlongValue))
                    {
                        ItsClientCollection.TryAdd(client.ItsMacAddress.ItsUlongValue, client);
                    }

                    if (client.ItsLastSeenDateTime > DateTime.MinValue)
                    {
                        var clientLastSeenTimeSpan = scanDateTime - client.ItsLastSeenDateTime;
                        client.ItsLastSeenTimeSpanString = Utils.TimeSpanToString(clientLastSeenTimeSpan);
                    }
                }

                essidPacketCount += bssid.ItsPacketCount;
                clientActions |= bssid.ItsClientActions;
                clientCount += bssid.ItsClientCount;
            }

            ItsPacketCount = essidPacketCount;
            ItsTotalClientCount = clientCount;
            ItsMaxRssi = maxRssi;
            ItsMaxAirTimePercentage = maxAirTime;
            ItsClientActions = clientActions;
            ItsConnectedFlag = connected;
            ItsLastSeenDateTime = lastSeen;
            ItsLastSeenTimeSpan = scanDateTime - ItsLastSeenDateTime;
            ItsLastSeenTimeSpanString = Utils.TimeSpanToString(ItsLastSeenTimeSpan);

            ItsChannelsString = TranslateChannelsToString();

            var currentEssidTimeSegment = new DeviceTimeSegment();
            currentEssidTimeSegment.ItsTimestamp = ItsLastSeenDateTime;
            currentEssidTimeSegment.ItsRssi = maxRssi;

            lock (_essidDetailsTimeSegmentsLock)
            {
                if (!_essidDetailsTimeSegments.ContainsKey(currentEssidTimeSegment.ItsTimestamp))
                {
                    _essidDetailsTimeSegments.Add(ItsLastSeenDateTime, currentEssidTimeSegment);
                }
            }
        }

        public void TrimTimeSegmentCollection(DateTime trimDateTime)
        {
            lock (_essidDetailsTimeSegmentsLock)
            {
                while (_essidDetailsTimeSegments.Count > 0 && _essidDetailsTimeSegments.First().Value.ItsTimestamp < trimDateTime)
                {
                    _essidDetailsTimeSegments.RemoveAt(0);
                }
            }
        }

        public List<DeviceTimeSegment> GetAllTimeSegments()
        {
            lock (_essidDetailsTimeSegmentsLock)
            {
                return new List<DeviceTimeSegment>(_essidDetailsTimeSegments.Values);
            }
        }

        public void UpdateValuesOnTimeRangeChanged()
        {
            var clientCount = 0;
            int? maxRssi = null;
            var maxAirTime = 0.0;
            var essidPacketCount = 0;
            var clientActions = ItsClientActions;
            var connected = false;

            foreach (var bssid in ItsBssidCollection.Values)
            {
                if (bssid.ItsMaxDataRate > ItsMaxDataRate)
                {
                    ItsMaxDataRate = bssid.ItsMaxDataRate;
                }
                if (maxRssi == null || bssid.ItsRssi > maxRssi)
                {
                    maxRssi = bssid.ItsRssi;
                }

                if (bssid.ItsAirTimePercentage > maxAirTime)
                {
                    maxAirTime = bssid.ItsAirTimePercentage;
                }

                if (bssid.ItsConnectedFlag)
                {
                    connected = true;
                }

                essidPacketCount += bssid.ItsPacketCount;
                clientActions |= bssid.ItsClientActions;
                clientCount += bssid.ItsClientCount;
            }

            ItsPacketCount = essidPacketCount;
            ItsTotalClientCount = clientCount;
            ItsMaxRssi = maxRssi;
            ItsMaxAirTimePercentage = maxAirTime;
            ItsClientActions = clientActions;
            ItsConnectedFlag = connected;
        }
        

        private string TranslateChannelsToString()
        {
            var builder = new StringBuilder(20);

            var channels = ItsChannels.Values.ToList();
            foreach (var channel in channels)
            {
                if (channel == null) continue;

                if (builder.Length > 0) builder.Append(", ");
                builder.Append(channel.ToString());
            }

            return builder.ToString();
        }

        private string BuildBasicRatesString()
        {
            ItsBasicRates.Sort();
            return "Basic Rates: " + String.Join(", ", ItsBasicRates);
        }

        public override string ToString()
        {
            return $"{ItsSsid} - {ItsBssCount} BSSIDs";
        }

        #endregion

    }
}
