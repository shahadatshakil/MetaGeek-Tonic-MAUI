using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MetaGeek.WiFi.Core.Enums;
using MetaGeek.WiFi.Core.Interfaces;
using MetaGeek.WiFi.Core.Resources;
using Prism.Mvvm;

namespace MetaGeek.WiFi.Core.Models
{
    public class ClientDetails : BindableBase, IClientDetails
    {
        #region Fields

        private const double FLOATING_POINT_TOLERANCE = 0.001;
        private const int MAX_SUBFRAME_TYPE = 64;

        internal static uint _nextId = 1;

        private TimeSpan _selectedTimeSpan;
        private int _totalPacketCountInTimespan;
        private TimeSpan _totalAirtimeInTimespan;
        private int _totalDataCount;
        private double _airTimePercentage;
        private double _percentageOfBss;
        private DeviceTimeSegment _currentTimeSegment;
        private DeviceTimeSegment _latestTimeSegment;
        private SortedList<DateTime, DeviceTimeSegment> _allTimeSegments;
        private DateTime _tailOfTimeSpan;
        private readonly object _allTimeSegmentsLock;
        private readonly object _currentTimeSegmentLock;
        private readonly object _updateTimeSegmentDataLock;
        private string _name;
        private string _makeModel;
        private string _capabilitiesString;
        private double _connectedRate;
        private double _clientTransmitRate;
        private double _clientReceiveRate;
        private double _maxDataRate;
        private uint _maxClientMcsIndex;
        private DateTime _lastSeenDateTime;

        private ClientWiFiEvents _clientActions;
        private string _vendor;
        private string _alias;

        private int? _rssi;
        private int? _displayRssi;
        private int? _noise;
        private double? _retryRate;
        private double? _displayRetryRate;
        private double? _snr;
        private double? _displaySnr;
        private int? _signalToNoiseRatio;
        private string _lastSeenTimeSpanString;
        private bool _supportsBssTransitionFlag;
        private bool _neighborReportCapabilityFlag;
        private bool _protectedManagementFramesCapabilityFlag;
        private bool _fastRoamingCapabilityFlag;
        private string _ssid;
        private IBssidDetails _bssid;
        private uint? _channel;
        private ClientConnectionActivities _connectionActivities;
        private List<string> _probedNetworks;
        private string _probedNetworksString;
        private string _channelsString;
        private List<uint> _channels;
        private DateTime _connectionDateTime;
        private int? _recentMaxMcs;
        private int? _displayMaxMcs;
        private int _recentSpatialStreams;
        private ChannelWidth _recentChannelWidth;
        private uint _spacialStreamCount;
        private string _broadcastName;
        private DeviceCategories _deviceCategory;
        private string _ipAddress;
        private IIpNetworkInfo _ipNetworkInfo;

        private bool _isNewClient = true;

        private int _totalRetryCount;
        private TimeSpan _totalScanTimeInTimespan;
        private int _currentDataCount;
        private int _currentRetryCount;


        #endregion

        #region Properties
        public uint ItsId { get; }

        public IMacAddress ItsMacAddress { get; }

        // This allows recreation of the entire frame for export to pcap
        public DateTime ItsAssociationRequestTimestamp { get; private set; }

        public ushort ItsCapabilitiesInformation { get; set; }

        public ushort ItsInterval { get; set; }

        public string ItsTaxonomySignature { get; set; }

        public SecurityInfo ItsSecurityInfo { get; set; }

        public bool ItsSupportsBssTransitionFlag
        {
            get { return _supportsBssTransitionFlag; }
            set
            {
                if (value == _supportsBssTransitionFlag) return;
                _supportsBssTransitionFlag = value;
                SetProperty(ref _supportsBssTransitionFlag, value);
            }
        }

        public bool ItsNeighborReportCapabilityFlag
        {
            get { return _neighborReportCapabilityFlag; }
            set
            {
                if (value == _neighborReportCapabilityFlag) return;
                _neighborReportCapabilityFlag = value;
                SetProperty(ref _neighborReportCapabilityFlag, value);
            }
        }
        
        public bool ItsProtectedManagementFramesCapabilityFlag
        {
            get { return _protectedManagementFramesCapabilityFlag; }
            set
            {
                if (value == _protectedManagementFramesCapabilityFlag) return;
                _protectedManagementFramesCapabilityFlag = value;
                SetProperty(ref _protectedManagementFramesCapabilityFlag, value);
            }
        }

        public bool ItsFastRoamingCapabilityFlag
        {
            get { return _fastRoamingCapabilityFlag; }
            set
            {
                if (value == _fastRoamingCapabilityFlag) return;
                _fastRoamingCapabilityFlag = value;
                SetProperty(ref _fastRoamingCapabilityFlag, value);
            }
        }

        public int? ItsSignalToNoiseRatio
        {
            get { return _signalToNoiseRatio; }
            private set
            {
                if (value == _signalToNoiseRatio) return;
                _signalToNoiseRatio = value;
                SetProperty(ref _signalToNoiseRatio, value);
            }
        }

        public double? ItsSNR
        {
            get { return _snr; }
            set
            {
                if (value == _snr) return;
                _snr = value;
                ItsDisplaySNR = _snr;
                SetProperty(ref _snr, value);   
            }
        }

        public double? ItsDisplaySNR
        {
            get { return _displaySnr; }
            set
            {
                if (value == _displaySnr || (value == null && (DateTime.UtcNow - ItsLastSeenDateTime).TotalSeconds < 5)) return;
                _displaySnr = value;
                SetProperty(ref _snr, value);
            }
        }

        public byte[] ItsInformationElementBytes { get; set; }

        public string ItsName
        {
            get
            {
                return _name;
            }
            private set
            {
                if (value == _name) return;
                _name = value;
                UpdateDeviceCategory();
                SetProperty(ref _name, value);
            }
        }
        public string ItsAlias
        {
            get { return _alias; }
            set
            {
                if (value == _alias) return;
                _alias = value;
                SetProperty(ref _alias, value);
                ItsName = GetBestName();
            }
        }

        public string ItsBroadcastName
        {
            get { return _broadcastName; }
            set
            {
                if (value == _broadcastName) return;
                _broadcastName = value;
                SetProperty(ref _broadcastName, value);
                ItsName = GetBestName();
            }
        }
        
        public string ItsDisplaySsid
        {
            get
            {
                return _ssid;
            }
            set
            {
                if (value == _ssid) return;
                _ssid = value;
                SetProperty(ref _ssid, value);
            }
        }

        public string ItsCapabilitiesString
        {
            get
            {
                if (string.IsNullOrEmpty(_capabilitiesString))
                {
                    BuildCapabilityString();
                }
                return _capabilitiesString;
            }
        }

        public uint? ItsChannel
        {
            get { return _channel; }
            set
            {
                if (value == _channel) return;
                _channel = value;
                if (value != null)
                {
                    AddToChannelsList(value.Value);
                }
                SetProperty(ref _channel, value);
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

        public string ItsVendor
        {
            get { return _vendor; }
            set
            {
                if (value == _vendor) return;
                _vendor = value;
                SetProperty(ref _vendor, value);
                ItsName = GetBestName();
            }
        }

        public string ItsMakeModel
        {
            get { return _makeModel; }
            set
            {
                if (value == _makeModel) return;
                _makeModel = value; 
                SetProperty(ref _makeModel, value);
            }
        }

        public int? ItsRssi
        {
            get { return _rssi; }
            set
            {
                if (value == _rssi) return;
                _rssi = value;
                ItsDisplayRssi = _rssi;
                SetProperty(ref _rssi, value);
            }
        }

        public int? ItsDisplayRssi
        {
            get { return _displayRssi; }
            set
            {
                if (value == _displayRssi || (value == null && (DateTime.UtcNow - ItsLastSeenDateTime).TotalSeconds < 5)) return;
                _displayRssi = value;
                SetProperty(ref _rssi, value);
            }
        }

        public int? ItsNoise
        {
            get { return _noise; }
            set
            {
                if (value == _noise) return;
                _noise = value;
                SetProperty(ref _noise, value);
            }
        }

        public double ItsConnectedRate
        {
            get { return _connectedRate; }
            set
            {
                if (Math.Abs(value - _connectedRate) < FLOATING_POINT_TOLERANCE) return;
                _connectedRate = value; 
                SetProperty(ref _connectedRate, value);
            }
        }

        public double ItsClientTransmitRate
        {
            get { return _clientTransmitRate; }
            set
            {
                if (Math.Abs(value - _clientTransmitRate) < FLOATING_POINT_TOLERANCE) return;
                _clientTransmitRate = value;
                SetProperty(ref _clientTransmitRate, value);
            }
        }

        public double ItsClientReceiveRate
        {
            get { return _clientReceiveRate; }
            set
            {
                if (Math.Abs(value - _clientReceiveRate) < FLOATING_POINT_TOLERANCE) return;
                _clientReceiveRate = value;
                SetProperty(ref _clientReceiveRate, value);
            }
        }

        public List<string> ItsProbedNetworks
        {
            get { return _probedNetworks; }
        }

        public string ItsProbedNetworksString
        {
            get { return _probedNetworksString; }
            set
            {
                if (value == _probedNetworksString) return;
                _probedNetworksString = value;
                SetProperty(ref _probedNetworksString, value);
            }
        }

        public int? ItsRecentMaxClientMcs
        {
            get { return _recentMaxMcs; }
            set
            {
                if (value == _recentMaxMcs) return;
                _recentMaxMcs = value;
                ItsDisplayMaxClientMcs = _recentMaxMcs;
                SetProperty(ref _recentMaxMcs, value);  
            }
        }

        public int? ItsDisplayMaxClientMcs
        {
            get { return _displayMaxMcs; }
            set
            {
                if (value == _displayMaxMcs || (value == null && (DateTime.UtcNow - ItsLastSeenDateTime).TotalSeconds < 5)) return;
                _displayMaxMcs = value;
                SetProperty(ref _displayMaxMcs, value);
            }
        }

        public ChannelWidth ItsRecentChannelWidth
        {
            get { return _recentChannelWidth; }
            set
            {
                if (value == _recentChannelWidth) return;
                _recentChannelWidth = value;
                SetProperty(ref _recentChannelWidth, value);
            }
        }

        public int ItsRecentSpatialStreams
        {
            get { return _recentSpatialStreams; }
            set
            {
                if (value == _recentSpatialStreams) return;
                _recentSpatialStreams = value;
                SetProperty(ref _recentSpatialStreams, value);
            }
        }

        public int ItsPacketCount
        {
            get { return _totalPacketCountInTimespan; }
            private set
            {
                if (value == _totalPacketCountInTimespan) return;
                _totalPacketCountInTimespan = value;
                SetProperty(ref _totalPacketCountInTimespan, value);
            }
        }

        public Dictionary<uint, FrameTypeStats> ItsFrameTypeStats { get; }

        public IIpNetworkInfo ItsIpNetworkInfo
        {
            get { return _ipNetworkInfo; }
            set
            {
                if (value == _ipNetworkInfo) return;
                _ipNetworkInfo = value;
                SetProperty(ref _ipNetworkInfo, value);
            }
        }

        public double ItsAirTimePercentage
        {
            get { return _airTimePercentage; }
            set
            {
                if (Math.Abs(value - _airTimePercentage) < FLOATING_POINT_TOLERANCE) return;
                _airTimePercentage = value;
                SetProperty(ref _airTimePercentage, value);
            }
        }

        public double ItsPercentageOfBss
        {
            get { return _percentageOfBss; }
            set
            {
                if (Math.Abs(value - _percentageOfBss) < FLOATING_POINT_TOLERANCE) return;
                _percentageOfBss = value;
                SetProperty(ref _percentageOfBss, value);
            }
        }

        public double ItsCurrentAirtimePercentage { get; private set; }

        public double? ItsRetryRate
        {
            get { return _retryRate; }
            set
            {
                if (value == _retryRate) return;
                _retryRate = value;
                ItsDisplayRetryRate = _retryRate;
                SetProperty(ref _retryRate, value);
            }
        }

        public double? ItsDisplayRetryRate
        {
            get { return _displayRetryRate; }
            set
            {
                if (value == _displayRetryRate || (value == null && (DateTime.UtcNow - ItsLastSeenDateTime).TotalSeconds < 5)) return;
                _displayRetryRate = value;
                SetProperty(ref _displayRetryRate, value);
            }
        }

        public DeviceCategories ItsDeviceCategory
        {
            get { return _deviceCategory; }
            set
            {
                if (value == _deviceCategory) return;
                _deviceCategory = value;
                SetProperty(ref _deviceCategory, value);
            }
        }

        public string ItsIpAddress
        {
            get { return _ipAddress; }
            set
            {
                if (value == _ipAddress) return;
                _ipAddress = value;
                SetProperty(ref _ipAddress, value);
            }
        }

        public IBssidDetails ItsBssid
        {
            get { return _bssid; }
            private set
            {
                if (value == _bssid) return;
                _bssid = value;
                SetProperty(ref _bssid, value);
            }
        }

        public PhyTypeInfo ItsPhyTypeInfo { get; set; }

        public IBssidDetails ItsPreviousBssid { get; set; }

        public double ItsMaxDataRate
        {
            get { return _maxDataRate; }
            set
            {
                if (value == _maxDataRate) return;
                _maxDataRate = value;
                SetProperty(ref _maxDataRate, value);
            }
        }

        public uint ItsMaxMcsIndex
        {
            get { return _maxClientMcsIndex; }
            set
            {
                if (value == _maxClientMcsIndex) return;
                _maxClientMcsIndex = value;
                SetProperty(ref _maxClientMcsIndex, value);
            }
        }

        public uint ItsSpacialStreamCount
        {
            get { return _spacialStreamCount; }
            set
            {
                if (value == _spacialStreamCount) return;
                _spacialStreamCount = value;
                SetProperty(ref _spacialStreamCount, value);
            }
        }

        public DateTime ItsFirstSeenDateTime { get; private set; }

        public DateTime ItsLastSeenDateTime
        {
            get { return _lastSeenDateTime; }
            set
            {
                _lastSeenDateTime = value;
                if (ItsFirstSeenDateTime == DateTime.MinValue)
                {
                    ItsFirstSeenDateTime = value;
                }
                SetProperty(ref _lastSeenDateTime, value);  
            }
        }

        public DateTime ItsConnectionDateTime
        {
            get { return _connectionDateTime; }
            set
            {
                if (value == _connectionDateTime) return;
                _connectionDateTime = value;
                SetProperty(ref _connectionDateTime, value);
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

        public List<double> ItsBasicRates { get; set; }

        public string ItsBasicRatesString { get; }

        public ClientWiFiEvents ItsClientActions
        {
            get { return _clientActions; }
            private set
            {
                if (value == _clientActions) return;
                _clientActions = value;
                SetProperty(ref _clientActions, value);
            }
        }

        public string Its24GhzProbeSignature { get; set; }
        public string Its5GhzProbeSignature { get; set; }

        public ClientConnectionActivities ItsConnectionActivities
        {
            get
            {
                return _connectionActivities;
            }
            private set
            {
                if (value == _connectionActivities) return;
                _connectionActivities = value;
                SetProperty(ref _connectionActivities, value);
            }
        }

        public bool ItsIsNewClientFlag
        {
            get { return _isNewClient; }
        }

        public bool ItsHasKnownCapabilities { get; set; }

        public int ItsCurrentDataCount
        {
            get => _currentDataCount; 
            private set => _currentDataCount = value;
        }

        public int ItsCurrentRetryCount
        {
            get => _currentRetryCount; 
            private set => _currentRetryCount = value;
        }

        public LinkedListNode<PacketMetaData> ItsLastAssociationRequestPacketNode { get; set; }

        public ConcurrentDictionary<ulong, AuthenticationInfo> ItsAllBssidsAuthInfoMap { get; private set; }

        #endregion

        #region Constructors
        public ClientDetails(IMacAddress macAddress, string vendor=null)
        {
            ItsId = _nextId++;
            ItsAllBssidsAuthInfoMap = new ConcurrentDictionary<ulong, AuthenticationInfo>();
            ItsMacAddress = macAddress;
            ItsVendor = vendor;

            if (vendor == null)
            {
                ItsName = GetBestName();
            }

            _allTimeSegmentsLock = new object();
            _currentTimeSegmentLock = new object();
            _updateTimeSegmentDataLock = new object();

            ItsSecurityInfo = new SecurityInfo();

            _allTimeSegments = new SortedList<DateTime, DeviceTimeSegment>();
            _tailOfTimeSpan = DateTime.MinValue;

            _currentTimeSegment = new DeviceTimeSegment();
            ItsPhyTypeInfo = new PhyTypeInfo();
            ItsBasicRates = new List<double>();

            ItsFrameTypeStats = new Dictionary<uint, FrameTypeStats>(MAX_SUBFRAME_TYPE);

            _probedNetworks = new List<string>();
            _channels = new List<uint>();
        }

        #endregion

        #region Methods
        
        public void AttachToBssid(IBssidDetails bssidDetails)
        {
            if (ItsBssid != bssidDetails)
            {
                ItsBssid?.DetachClient(this);
            }

            ItsBssid = bssidDetails;
            if (bssidDetails == null) return;

            if(!ItsAllBssidsAuthInfoMap.ContainsKey(bssidDetails.ItsMacAddress.ItsUlongValue))
            {
                ItsAllBssidsAuthInfoMap.TryAdd(bssidDetails.ItsMacAddress.ItsUlongValue, new AuthenticationInfo());
            }

            ItsDisplaySsid = bssidDetails.ItsDisplaySsid;
            ItsChannel = bssidDetails.ItsChannelInfo.ItsChannel;
        }

        public void AddProbedNetwork(string ssid)
        {
            if (string.IsNullOrEmpty(ssid))
            {
                ssid = "[WILDCARD]";
            }
            if (!_probedNetworks.Contains(ssid))
            {
                _probedNetworks.Add(ssid);
                ItsProbedNetworksString = string.Join(", ", _probedNetworks);
            }
        }

        private void AddToChannelsList(uint channel)
        {
            if (!_channels.Contains(channel))
            {
                _channels.Add(channel);
                _channels.Sort();
                ItsChannelsString = string.Join(", ", _channels);
            }
        }

        public void ProcessPacket(PacketMetaData packet, bool clientTransmitted = false)
        {
            _isNewClient = false;

            packet.ItsClient = this;
            packet.ItsFromClientFlag = clientTransmitted; //ItsMacAddress.Equals(packet.ItsTxAddress);

            if (_bssid != null)
            {
                // Some packets have bogus channel width resulting in an impossible data rate given the current channel width
                if (packet.ItsChannelWidth > _bssid.ItsChannelInfo.ItsChannelWidth)
                {
                    packet.ItsChannelWidth = _bssid.ItsChannelInfo.ItsChannelWidth;
                    packet.ItsRate = DataRateCalculator.DataRateFromMcsDetails(packet.ItsMcsIndex,
                        packet.ItsSpatialStreams, packet.ItsChannelWidth, packet.ItsShortGuardFlag);
                }

                lock (_currentTimeSegmentLock)
                {
                    _currentTimeSegment.AddPacket(packet, clientTransmitted);
                }
            }
            // Independent Client - No BSSID to process time segments
            else
            {
                lock(_updateTimeSegmentDataLock)
                {
                    ItsPacketCount++;
                }

                ItsChannel = packet.ItsChannel;
                if (clientTransmitted)
                {
                    ItsRssi = packet.ItsSignal;
                    ItsNoise = packet.ItsNoise;
                    ItsLastSeenDateTime = packet.ItsDateTime;
                }
            }
        }
        
        public void ProcessInferredDataPacket(double inferredAirtime)
        {
            lock (_currentTimeSegmentLock)
            {
                _currentTimeSegment.AddInferredDataPacket(inferredAirtime);
            }
        }

        public List<DeviceTimeSegment> GetAllTimeSegments()
        {
            lock (_allTimeSegmentsLock)
            {
                return new List<DeviceTimeSegment>(_allTimeSegments.Values);
            }
        }

        public DeviceTimeSegment FinalizeAirtimeSegment(DateTime startTime, TimeSpan scanTimeSpan)
        {
            DeviceTimeSegment finalizedTimeSegment = null;

            lock (_currentTimeSegmentLock)
            {
                // finalize this segment, add it to list, and start a new segment
                _currentTimeSegment.ItsTimestamp = startTime;
                _currentTimeSegment.FinalizeTimeSegment(scanTimeSpan);
                if (ItsBssid?.ItsRssi != null)
                {
                    _currentTimeSegment.ItsBssidRssi = ItsBssid.ItsRssi;
                }

                finalizedTimeSegment = _currentTimeSegment;
                StartNextTimeSegment(startTime);
            }

            AddTimeSegmentToTimeSpan(_latestTimeSegment);
            RemoveOlderTimeSegmentFromTimeSpan();

            return finalizedTimeSegment;
        }

        private void AddTimeSegmentToTimeSpan(DeviceTimeSegment timeSegment)
        {
            if (timeSegment == null)
                return;

            lock (_updateTimeSegmentDataLock)
            {
                if (timeSegment.ItsFrameTypeStats != null)
                {
                    foreach (var stats in timeSegment.ItsFrameTypeStats)
                    {
                        var frameType = stats.ItsFrameType;
                        if (!ItsFrameTypeStats.ContainsKey(frameType))
                        {
                            ItsFrameTypeStats.Add(stats.ItsFrameType, new FrameTypeStats(frameType));
                        }

                        ItsFrameTypeStats[frameType].ItsPacketCount += stats.ItsPacketCount;
                        ItsFrameTypeStats[frameType].ItsAirTime += stats.ItsAirTime;
                    }
                }

                ItsPacketCount += timeSegment.ItsPacketCount;

                if (timeSegment.ItsDataPacketCount > WiFIConstants.MIN_DATA_COUNT_FOR_RETRY_CALC)
                {
                    _totalDataCount += timeSegment.ItsDataPacketCount;
                    _totalRetryCount += timeSegment.ItsRetryCount;
                }

                _totalAirtimeInTimespan += timeSegment.ItsAirTime;
                _totalScanTimeInTimespan += timeSegment.ItsScanTime;
            }

            ItsCurrentDataCount = timeSegment.ItsDataPacketCount;
            ItsCurrentRetryCount = timeSegment.ItsRetryCount;
            ItsCurrentAirtimePercentage = timeSegment.ItsAirTimePercentage ?? 0.0;
            ItsAirTimePercentage = _totalAirtimeInTimespan.TotalMilliseconds / _totalScanTimeInTimespan.TotalMilliseconds;
            ItsRetryRate = _totalDataCount > WiFIConstants.MIN_DATA_COUNT_FOR_RETRY_CALC ? _totalRetryCount / (double)_totalDataCount : (double?)null;

            // These properties are JUST the most recent time segment
            ItsRssi = timeSegment.ItsRssi;
            ItsNoise = timeSegment.ItsNoise;
            ItsSNR = timeSegment.ItsSNR;
            ItsSignalToNoiseRatio = ItsRssi != null && ItsNoise != null ? ItsRssi - ItsNoise : null;

            ItsConnectedRate = timeSegment.ItsMaxClientRate.HasValue ? timeSegment.ItsMaxClientRate.Value : 0;
            ItsClientTransmitRate = timeSegment.ItsMaxClientRate.HasValue ? timeSegment.ItsMaxClientRate.Value : 0;
            ItsClientReceiveRate = timeSegment.ItsMaxBssidRate;
            ItsRecentMaxClientMcs = timeSegment.ItsMaxClientMcs;
            ItsRecentSpatialStreams = timeSegment.ItsSpatialStreams;
            ItsRecentChannelWidth = timeSegment.ItsChannelWidth;
        }

        private void RemoveOlderTimeSegmentFromTimeSpan()
        {
            List<DeviceTimeSegment> oldTimeSegments = null;
            var newTailOfTimeSpan = _latestTimeSegment.ItsTimestamp.AddSeconds(-_selectedTimeSpan.TotalSeconds);


            lock (_allTimeSegmentsLock)
            {
                oldTimeSegments = _allTimeSegments.Values.Where(t => t != null && t.ItsTimestamp >= _tailOfTimeSpan && t.ItsTimestamp < newTailOfTimeSpan).ToList();
            }
            _tailOfTimeSpan = newTailOfTimeSpan;

            if (oldTimeSegments == null || oldTimeSegments.Count == 0) return;

            lock (_updateTimeSegmentDataLock)
            {
                // Remove old time segments
                foreach (var timeSegment in oldTimeSegments)
                {
                    if (timeSegment.ItsFrameTypeStats != null)
                    {
                        foreach (var stats in timeSegment.ItsFrameTypeStats)
                        {
                            var frameType = stats.ItsFrameType;
                            if (ItsFrameTypeStats.ContainsKey(frameType))
                            {
                                ItsFrameTypeStats[frameType].ItsPacketCount -= stats.ItsPacketCount;
                                ItsFrameTypeStats[frameType].ItsAirTime -= stats.ItsAirTime;
                            }
                        }
                    }

                    ItsPacketCount -= timeSegment.ItsPacketCount;

                    if (timeSegment.ItsDataPacketCount > WiFIConstants.MIN_DATA_COUNT_FOR_RETRY_CALC)
                    {
                        _totalDataCount -= timeSegment.ItsDataPacketCount;
                        _totalRetryCount -= timeSegment.ItsRetryCount;
                    }

                    _totalAirtimeInTimespan -= timeSegment.ItsAirTime;
                    _totalScanTimeInTimespan -= timeSegment.ItsScanTime;
                }
            }

            ItsAirTimePercentage = _totalAirtimeInTimespan.TotalMilliseconds / _totalScanTimeInTimespan.TotalMilliseconds;
            ItsRetryRate = _totalDataCount > WiFIConstants.MIN_DATA_COUNT_FOR_RETRY_CALC ? _totalRetryCount / (double)_totalDataCount : (double?)null;
        }
        
        public void UpdateClientStatsBasedOnTimeRange(DateTime startTime, DateTime endTime)
        {
            var selectedTimeSegments = GetTimeSegmentsBetweenTimeRange(startTime, endTime);
            _latestTimeSegment = selectedTimeSegments.LastOrDefault();

            UpdateClientStatsBasedOnTimeSegments(selectedTimeSegments);
        }

        private void RebuildClientStatsForTimeRange()
        {
            if (_latestTimeSegment == null) return;

            // Update values based on the time segments between selected time span
            var startime = _latestTimeSegment.ItsTimestamp.AddMilliseconds(-_selectedTimeSpan.TotalMilliseconds);
            var endTime = _latestTimeSegment.ItsTimestamp;
            List<DeviceTimeSegment> selectedTimeSegments = GetTimeSegmentsBetweenTimeRange(startime, endTime);

            UpdateClientStatsBasedOnTimeSegments(selectedTimeSegments);
        }

        private void UpdateClientStatsBasedOnTimeSegments(List<DeviceTimeSegment> selectedTimeSegments)
        {
            var totalPackets = 0;
            var totalAirTime = TimeSpan.Zero;
            var totalScanTime = TimeSpan.Zero;

            var dataCount = 0;
            var retryCount = 0;

            lock (_updateTimeSegmentDataLock)
            {
                ItsFrameTypeStats.Clear();

                foreach (var segment in selectedTimeSegments)
                {
                    if (segment == null) continue;

                    totalPackets += segment.ItsPacketCount;
                    totalAirTime += segment.ItsAirTime;
                    totalScanTime += segment.ItsScanTime;

                    if (segment.ItsDataPacketCount > WiFIConstants.MIN_DATA_COUNT_FOR_RETRY_CALC)
                    {
                        dataCount += segment.ItsDataPacketCount;
                        retryCount += segment.ItsRetryCount;
                    }

                    if (segment.ItsFrameTypeStats != null)
                    {
                        foreach (var stats in segment.ItsFrameTypeStats)
                        {
                            var frameType = stats.ItsFrameType;
                            if (!ItsFrameTypeStats.ContainsKey(frameType))
                            {
                                ItsFrameTypeStats.Add(stats.ItsFrameType, new FrameTypeStats(frameType));
                            }

                            ItsFrameTypeStats[frameType].ItsPacketCount += stats.ItsPacketCount;
                            ItsFrameTypeStats[frameType].ItsAirTime += stats.ItsAirTime;
                        }
                    }
                }

                _totalAirtimeInTimespan = totalAirTime;
                _totalScanTimeInTimespan = totalScanTime;
                _totalDataCount = dataCount;
                _totalRetryCount = retryCount;

                _tailOfTimeSpan = _latestTimeSegment != null ? _latestTimeSegment.ItsTimestamp.AddSeconds(-_selectedTimeSpan.TotalSeconds) : DateTime.MinValue;

                // This data is aggregate for the CURRENT TIME SPAN
                ItsPacketCount = totalPackets;
            }

            ItsAirTimePercentage = _totalAirtimeInTimespan.TotalMilliseconds / _totalScanTimeInTimespan.TotalMilliseconds;
            ItsRetryRate = _totalDataCount > WiFIConstants.MIN_DATA_COUNT_FOR_RETRY_CALC ? _totalRetryCount / (double)_totalDataCount : (double?)null;

            // This information is from LATEST TIME SEGMENT
            if (_latestTimeSegment != null)
            {
                ItsRssi = _latestTimeSegment.ItsRssi;
                ItsNoise = _latestTimeSegment.ItsNoise;
                ItsSNR = _latestTimeSegment.ItsSNR;
                ItsSignalToNoiseRatio = ItsRssi != null && ItsNoise != null ? ItsRssi - ItsNoise : null;

                ItsConnectedRate = _latestTimeSegment.ItsMaxClientRate.HasValue ? _latestTimeSegment.ItsMaxClientRate.Value : 0;
                ItsClientTransmitRate = _latestTimeSegment.ItsMaxClientRate.HasValue ? _latestTimeSegment.ItsMaxClientRate.Value : 0;
                ItsClientReceiveRate = _latestTimeSegment.ItsMaxBssidRate;
                ItsRecentMaxClientMcs = _latestTimeSegment.ItsMaxClientMcs.HasValue ? _latestTimeSegment.ItsMaxClientMcs.Value : 0;
                ItsRecentSpatialStreams = _latestTimeSegment.ItsSpatialStreams;
                ItsRecentChannelWidth = _latestTimeSegment.ItsChannelWidth;
            }
        }

        private List<DeviceTimeSegment> GetTimeSegmentsBetweenTimeRange(DateTime startTime, DateTime endTime)
        {
            List<DeviceTimeSegment> selectedSegments = new List<DeviceTimeSegment>();

            if (_allTimeSegments.Count == 0)
            {
                return selectedSegments;
            }

            lock (_allTimeSegmentsLock)
            {
                foreach (var timeSegment in _allTimeSegments.Values)
                {
                    if (timeSegment.ItsTimestamp >= startTime && timeSegment.ItsTimestamp <= endTime)
                    {
                        selectedSegments.Add(timeSegment);
                    }
                }
            }

            return selectedSegments;
        }

        public void AddClientAction(ClientWiFiEvents clientAction, DateTime timestamp)
        {
            ItsClientActions |= clientAction;
        }

        public void SaveAssociationRequestDetails(DateTime dateTime, ushort capabilities, ushort interval, byte[] informationElements)
        {
            ItsAssociationRequestTimestamp = dateTime;
            ItsCapabilitiesInformation = capabilities;
            ItsInterval = interval;
            ItsInformationElementBytes = informationElements;
            ItsClientActions |= ClientWiFiEvents.Associated;
        }

        public void UpdateSelectedTimeSpan(TimeSpan timeSpan)
        {
            _selectedTimeSpan = timeSpan;
            RebuildClientStatsForTimeRange();
        }

        public void TrimTimeSegmentCollection(DateTime trimDateTime)
        {
            lock (_allTimeSegmentsLock)
            {
                while (_allTimeSegments.Count > 0 && _allTimeSegments.First().Value.ItsTimestamp < trimDateTime)
                {
                    _allTimeSegments.RemoveAt(0);
                }
            }
        }

        private void StartNextTimeSegment(DateTime startTime)
        {
            lock (_allTimeSegmentsLock)
            {
                if (!_allTimeSegments.ContainsKey(_currentTimeSegment.ItsTimestamp))
                {
                    _allTimeSegments.Add(_currentTimeSegment.ItsTimestamp, _currentTimeSegment);
                }
            }

            _latestTimeSegment = _currentTimeSegment;

            if (_currentTimeSegment.ItsPacketCount > 0)
            {
                ItsLastSeenDateTime = startTime;
            }

            _currentTimeSegment = new DeviceTimeSegment();
        }
        
        private string GetBestName()
        {
            if (!string.IsNullOrEmpty(ItsAlias))
            {
                return ItsAlias;
            }

            if (!string.IsNullOrEmpty(ItsBroadcastName))
            {
                return ItsBroadcastName;
            }

            if (!string.IsNullOrEmpty(ItsVendor))
            {
                return ItsMacAddress.BuildVendorMacString(ItsVendor);
            }

            return ItsMacAddress.ToString();
        }

        private void UpdateDeviceCategory()
        {
            if (ItsDeviceCategory != DeviceCategories.Unknown) return;

            var lowerName = ItsName.ToLower();
            if (lowerName.Contains("phone"))
            {
                ItsDeviceCategory = DeviceCategories.Phone;
            }
            else if (lowerName.Contains("macbook") || lowerName.Contains("laptop") || lowerName.Contains("computer"))
            {
                ItsDeviceCategory = DeviceCategories.Computer;
            }
            else if (lowerName.Contains("broadcast"))
            {
                ItsDeviceCategory = DeviceCategories.Broadcast;
            }
        }

        public void AddConnectionActivity(ClientConnectionActivities activity)
        {
            ItsConnectionActivities |= activity;
        }

        public override string ToString()
        {
            return ItsMacAddress == null ? string.Empty : ItsMacAddress.ToString();
        }

        private void BuildCapabilityString()
        {
            var builder = new StringBuilder();
            var separator = " / ";

            if(ItsNeighborReportCapabilityFlag)
            {
                builder.Append("k");
            }
            if (ItsSupportsBssTransitionFlag)
            {
                if (builder.Length > 0) builder.Append(separator);
                builder.Append("v");
            }
            if (ItsFastRoamingCapabilityFlag)
            {
                if (builder.Length > 0) builder.Append(separator);
                builder.Append("r");
            }
            if (ItsProtectedManagementFramesCapabilityFlag)
            {
                if (builder.Length > 0) builder.Append(separator);
                builder.Append("w");
            }

            _capabilitiesString = builder.ToString();
        }

        #endregion
    }
}
