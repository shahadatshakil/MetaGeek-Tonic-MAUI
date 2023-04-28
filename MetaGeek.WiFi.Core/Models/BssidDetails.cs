using System;
using System.CodeDom;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MetaGeek.WiFi.Core.Enums;
using MetaGeek.WiFi.Core.Interfaces;
using MetaGeek.WiFi.Core.Models;
using MetaGeek.WiFi.Core.Resources;
using Prism.Mvvm;

namespace MetaGeek.WiFi.Core.Models
{
    public class BssidDetails : BindableBase, IBssidDetails
    {
        #region Fields

        private const double FLOATING_POINT_TOLERANCE = 0.001;
        private const ulong BROADCAST_MAC_ULONG = 0xFFFFFFFFFFFF;
        private const int MAX_SECONDS_TO_USE_MONITOR_RSSI = 5;

        private string _ssid;
        private ConcurrentDictionary<ulong, IClientDetails> _clientCollection;
        private string _broadcastName;
        private int _bssClientCount;
        private int _clientCount;
        private int? _rssi;
        private uint _spatialStreams;
        private uint _mcsIndex;
        private double _airTimePercentage;
        private double _channelAirTimePercentage;
        private DeviceTimeSegment _currentTimeSegment;
        private SortedList<DateTime, DeviceTimeSegment> _allTimeSegments;

        private IClientDetails _broadcastClient;
        private IEssidDetails _essidDetails;
        private string _name;
        private string _make;
        private string _taxonomySignature;
        private ClientWiFiEvents _clientActions;
        private string _alias;
        private string _vendor;
        private bool _connectedFlag;
        private List<double> _basicRates;
        private string _basicRatesString;
        private IApRadioDetails _radioGroup;
        private readonly object _allTimeSegmentsLock;
        private DateTime _firstSeenDateTime;
        private DateTime _lastSeenDateTime;
        private string _displaySsid;
        private DateTime _lastFullBeaconParseTime;
        private double? _retryRate;
        private int? _signalToNoiseRatio;
        private string _lastSeenTimeSpanString;

        private bool _hasMonitorTimeSegmentFlag;
        private DateTime _lastScannedDateTime = DateTime.MinValue;
        private DateTime _lastMonitoredDateTime = DateTime.MinValue;
        private bool _firstSessionSeenFlag;
        private bool _supportsBssTransitionFlag;
        private bool _neighborReportCapabilityFlag;
        private bool _protectedManagementFramesCapabilityFlag;
        private bool _fastRoamingCapabilityFlag;

        #endregion

        #region Properties
        public string ItsName
        {
            get { return _name; }
            set
            {
                if (value == _name) return;
                _name = value;
                SetProperty(ref _name, value);
            }
        }

        public string ItsMakeModel
        {
            get { return _make; }
            set
            {
                if (value == _make) return;
                _make = value;
                SetProperty(ref _make, value);  
            }
        }

        public IMacAddress ItsMacAddress { get; }

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

        public IEssidDetails ItsEssid
        {
            get { return _essidDetails; }
            set { _essidDetails = value; }
        }

        public IApRadioDetails ItsRadioGroup
        {
            get { return _radioGroup; }
            set { _radioGroup = value; }
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

        public NetworkType ItsNetworkType { get; set; }
        public string ItsCountryCode { get; set; }
        public ChannelInfo ItsChannelInfo { get; set; }
        public PhyTypeInfo ItsPhyTypeInfo { get; set; }
        public SecurityInfo ItsSecurityInfo { get; set; }
        public double ItsMaxDataRate { get; set; }
        public double ItsMaxConnectedRate { get; set; }

        public List<double> ItsBasicRates
        {
            get { return _basicRates; }
            set
            {
                _basicRates = value;
            }
        }

        public string ItsBasicRatesString
        {
            get
            {
                if (string.IsNullOrEmpty(_basicRatesString))
                {
                    BuildBasicRatesString();
                }

                return _basicRatesString;
            }
        }

        public ushort ItsCapabilitiesInformation { get; set; }

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

        public DateTime ItsFirstSeenDateTime
        {
            get { return _firstSeenDateTime; }
        }

        public DateTime ItsLastSeenDateTime
        {
            get { return _lastSeenDateTime; }
            set
            {
                _lastSeenDateTime = value;
                if (_firstSeenDateTime == DateTime.MinValue)
                {
                    _firstSeenDateTime = value;
                }
                SetProperty(ref _firstSeenDateTime, value);
            }
        }

        private DateTime ItsLastScannedDateTime
        {
            get { return _lastScannedDateTime; }
            set
            {
                _lastScannedDateTime = value;
                if (_lastScannedDateTime > ItsLastSeenDateTime)
                {
                    ItsLastSeenDateTime = _lastScannedDateTime;
                }
            }
        }
        private DateTime ItsLastMonitoredDateTime
        {
            get { return _lastMonitoredDateTime; }
            set
            {
                _lastMonitoredDateTime = value;
                if (_lastMonitoredDateTime > ItsLastSeenDateTime)
                {
                    ItsLastSeenDateTime = _lastMonitoredDateTime;
                }
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

        public byte[] ItsInformationElementBytes { get; set; }
        public int? ItsNoise { get; private set; }
        public ushort ItsInterval { get; set; }
            
        public string ItsTaxonomySignature
        {
            get { return _taxonomySignature; }
            set { _taxonomySignature = value; }
        }

        public int ItsPacketCount { get; set; }

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

        public double? ItsRetryRate
        {
            get { return _retryRate; }
            set
            {
                if (value == _retryRate) return;
                _retryRate = value;
                SetProperty(ref _retryRate, value);
            }
        }

        public double ItsChannelAirTimePercentage
        {
            get { return _channelAirTimePercentage; }
            set
            {
                if (Math.Abs(value - _channelAirTimePercentage) < FLOATING_POINT_TOLERANCE) return;
                _channelAirTimePercentage = value;
                SetProperty(ref _channelAirTimePercentage, value);
            }
        }

        public uint ItsMaxMcsIndex
        {
            get { return _mcsIndex; }
            set
            {
                if (value == _mcsIndex) return;
                _mcsIndex = value;
                SetProperty(ref _mcsIndex, value);
            }
        }

        public uint ItsSpacialStreamCount
        {
            get { return _spatialStreams; }
            set
            {
                if (value == _spatialStreams) return;
                _spatialStreams = value;
                SetProperty(ref _spatialStreams, value);
            }
        }

        public int? ItsRssi
        {
            get { return _rssi; }
            set
            {
                if (value == _rssi) return;
                _rssi = value;
                SetProperty(ref _rssi, value);
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

        public DateTime ItsLastFullBeaconParseTime
        {
            get { return _lastFullBeaconParseTime; }
            set { _lastFullBeaconParseTime = value; }
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
                return _displaySsid;
            }
            set
            {
                if (value == _displaySsid) return;
                _displaySsid = value;
                SetProperty(ref _displaySsid, value);
            }
        }

        public string ItsSsid
        {
            get
            {
                return _ssid;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    ItsDisplaySsid = value;
                }

                if (value == _ssid) return;
                _ssid = value;
                SetProperty(ref _ssid, value);
            }
        }

        public ConcurrentDictionary<ulong, IClientDetails> ItsClientCollection
        {
            get { return _clientCollection; }
        }

        public int ItsClientCount
        {
            get { return _clientCount; }
            private set
            {
                if (value == _clientCount) return;
                _clientCount = value;
                SetProperty(ref _clientCount, value);
            }
        }

        public int ItsBssClientCount
        {
            get { return _bssClientCount; }
            set
            {
                if (value == _bssClientCount) return;
                _bssClientCount = value;
                if (_clientCollection == null || _clientCollection.Count == 0)
                {
                    ItsClientCount = (int) value;
                }
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

        public bool ItsHasRSNElement { get; set; }

        public List<IMacAddress> ItsNeighborBssidMacList { get; set; }

        public IClientDetails ItsBroadcastClient
        {
            get { return _broadcastClient; }
        }

        #endregion

        #region Constructor

        public BssidDetails(IMacAddress macAddress, string vendor=null)
        {
            ItsMacAddress = macAddress;
            ItsVendor = vendor;
            if (vendor == null)
            {
                ItsName = GetBestName();
            }

            ItsPhyTypeInfo = new PhyTypeInfo();
            ItsBasicRates = new List<double>();
            ItsChannelInfo = new ChannelInfo();
            ItsSecurityInfo = new SecurityInfo();
            _allTimeSegments = new SortedList<DateTime, DeviceTimeSegment>();
            _currentTimeSegment = new DeviceTimeSegment();
            _allTimeSegmentsLock = new object();

            _broadcastClient = new ClientDetails(MacAddressCollection.Broadcast(), "Broadcast");
            _broadcastClient.AttachToBssid(this);
            _clientCollection = new ConcurrentDictionary<ulong, IClientDetails>();
        }

        #endregion

        #region Methods

        public void UpdateBroadcastClientTimeSpan(TimeSpan selectedTimeSpan)
        {
            _broadcastClient.UpdateSelectedTimeSpan(selectedTimeSpan);
        }

        public void UpdateBroadcastClientBasedOnTimeRange(DateTime startTime, DateTime endTime)
        {
            _broadcastClient.UpdateClientStatsBasedOnTimeRange(startTime, endTime);
        }

        /// <summary>
        /// Attaches client to this BSSID after detaching it from previous BSSID if required
        /// </summary>
        /// <param name="clientDetails"></param>
        public void AttachClient(IClientDetails clientDetails)
        {
            if (clientDetails != null)
            {
                clientDetails.AttachToBssid(this);
                if (!_clientCollection.ContainsKey(clientDetails.ItsMacAddress.ItsUlongValue))
                {
                    _clientCollection.TryAdd(clientDetails.ItsMacAddress.ItsUlongValue, clientDetails);
                    ItsClientCount = ItsClientCollection.Count;
                }
            }
        }

        /// <summary>
        /// Detaches client from this BSSID - happens after client roamed
        /// </summary>
        /// <param name="clientDetails"></param>
        public void DetachClient(IClientDetails clientDetails)
        {
            if (clientDetails != null)
            {
                IClientDetails removedClient = null;
                _clientCollection.TryRemove(clientDetails.ItsMacAddress.ItsUlongValue, out removedClient);
            }
        }

        /// <summary>
        /// Called if the BSSID may have clients attached BEFORE we received a beacon containing the SSID
        /// </summary>
        public void UpdateClientsSsid()
        {
            if (string.IsNullOrEmpty(ItsSsid)) return;

            foreach (var client in ItsClientCollection.Values)
            {
                client.ItsDisplaySsid = ItsSsid;
            }
        }

        /// <summary>
        /// Process probe response received via active scanning
        /// </summary>
        /// <param name="scanTime"></param>
        /// <param name="rssi"></param>
        public void ProcessScanResponse(DateTime scanTime, int rssi)
        {
            ItsLastScannedDateTime = scanTime;

            if (scanTime > ItsLastMonitoredDateTime.AddSeconds(MAX_SECONDS_TO_USE_MONITOR_RSSI))
            {
                ItsRssi = rssi;
                ItsAirTimePercentage = 0;
            }

            if (!_hasMonitorTimeSegmentFlag)
            {
                var timeSegment = new DeviceTimeSegment() {ItsRssi = rssi, ItsTimestamp = scanTime};
                try
                {
                    lock (_allTimeSegmentsLock)
                    {
                        _allTimeSegments.Add(timeSegment.ItsTimestamp, timeSegment);
                    }
                }
                catch (ArgumentException)
                {
                }
            }
        }

        /// <summary>
        /// Process beacon received via packet capture
        /// </summary>
        /// <param name="packet"></param>
        public void ProcessBeacon(PacketMetaData packet)
        {
            ItsLastMonitoredDateTime = packet.ItsDateTime;
            ProcessBroadcastPacket(packet, true);
        }

        /// <summary>
        /// We use the RSSI of the "broadcast" for the RSSI of the BSSID
        /// So we invert the clientTransmitted field to track the RSSI of the BSSID
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="bssidTransmitted"></param>
        public void ProcessBroadcastPacket(PacketMetaData packet, bool bssidTransmitted)
        {
            _broadcastClient.ProcessPacket(packet, bssidTransmitted);
        }

        /// <summary>
        /// Called after a channel scan is complete.
        /// Calculates total airtime for the BSSID, updates RSSI, etc.
        /// Adds time segment to list of time segments and starts a new time segment
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="scanTimeSpan"></param>
        public void FinalizeChannelScan(DateTime startTime, TimeSpan scanTimeSpan)
        {
            var currentAirtimePercentage = 0.0;
            var airTimePercentage = 0.0;
            var clientActions = ClientWiFiEvents.None;
            var currentDataCount = 0;
            var currentRetryCount = 0;
            var activeClientCount = 0;

            _hasMonitorTimeSegmentFlag = true;

            _currentTimeSegment.ItsTimestamp = startTime;

            foreach (var client in ItsClientCollection.Values)
            {
                var clientTimeSegment = client.FinalizeAirtimeSegment(startTime, scanTimeSpan);
                currentAirtimePercentage += client.ItsCurrentAirtimePercentage;
                airTimePercentage += client.ItsAirTimePercentage;
                currentDataCount += client.ItsCurrentDataCount;
                currentRetryCount += client.ItsCurrentRetryCount;
                clientActions |= client.ItsClientActions;

                _currentTimeSegment.ItsPacketCount += clientTimeSegment.ItsPacketCount;
                if (clientTimeSegment.ItsMaxClientRate > _currentTimeSegment.ItsMaxClientRate)
                {
                    _currentTimeSegment.ItsMaxClientRate = clientTimeSegment.ItsMaxClientRate;
                }
                if (clientTimeSegment.ItsMaxBssidRate > _currentTimeSegment.ItsMaxBssidRate)
                {
                    _currentTimeSegment.ItsMaxBssidRate = clientTimeSegment.ItsMaxBssidRate;
                }
                if (clientTimeSegment.ItsMaxClientMcs > _currentTimeSegment.ItsMaxClientMcs)
                {
                    _currentTimeSegment.ItsMaxClientMcs = clientTimeSegment.ItsMaxClientMcs;
                }
                if (clientTimeSegment.ItsMaxBssidMcs > _currentTimeSegment.ItsMaxBssidMcs)
                {
                    _currentTimeSegment.ItsMaxBssidMcs = clientTimeSegment.ItsMaxBssidMcs;
                }
                if (client.ItsPacketCount > 0)
                {
                    activeClientCount++;
                }
            }

            _broadcastClient.FinalizeAirtimeSegment(startTime, scanTimeSpan);
            currentAirtimePercentage += _broadcastClient.ItsCurrentAirtimePercentage;
            airTimePercentage += _broadcastClient.ItsAirTimePercentage;
            _currentTimeSegment.ItsAirTimePercentage = currentAirtimePercentage;
            _currentTimeSegment.ItsRetryPercentage = currentDataCount > WiFIConstants.MIN_DATA_COUNT_FOR_RETRY_CALC
                ? (currentRetryCount / (double)currentDataCount) : (double?)null;

            ItsAirTimePercentage = airTimePercentage;
            ItsClientCount = activeClientCount;

            foreach (var client in ItsClientCollection.Values)
            {
                client.ItsPercentageOfBss = ItsAirTimePercentage == 0.0 ? 0.0 : client.ItsAirTimePercentage / ItsAirTimePercentage;
            }

            _broadcastClient.ItsPercentageOfBss = ItsAirTimePercentage == 0.0 ? 0.0 : _broadcastClient.ItsAirTimePercentage / ItsAirTimePercentage;

            ItsClientActions = clientActions;
            if (_broadcastClient.ItsRssi.HasValue)
            {
                ItsRssi = _currentTimeSegment.ItsRssi = _broadcastClient.ItsRssi;
                ItsNoise = _currentTimeSegment.ItsNoise = _broadcastClient.ItsNoise;
            }
            // Getting sum of all client packets for the entire BSSID
            ItsPacketCount += _currentTimeSegment.ItsPacketCount;
            //ItsMaxConnectedRate = _currentTimeSegment.ItsMaxRate;

            FinalizeCurrentTimeSegment();
        }

        private void FinalizeCurrentTimeSegment()
        {
            if (_currentTimeSegment.ItsRssi != null)
            {
                lock (_allTimeSegmentsLock)
                {
                    if (!_allTimeSegments.ContainsKey(_currentTimeSegment.ItsTimestamp))
                    {
                        _allTimeSegments.Add(_currentTimeSegment.ItsTimestamp, _currentTimeSegment);
                    }
                }
            }

            _currentTimeSegment = new DeviceTimeSegment();
        }

        public void UpdateValuesOnTimeRangeChanged()
        {
            var airTimePercentage = 0.0;
            var clientActions = ClientWiFiEvents.None;
            var activeClientCount = 0;

            foreach (var client in ItsClientCollection.Values)
            {
                airTimePercentage += client.ItsAirTimePercentage;
                clientActions |= client.ItsClientActions;
                activeClientCount += client.ItsPacketCount > 0 ? 1 : 0;
            }

            airTimePercentage += _broadcastClient.ItsAirTimePercentage;

            ItsClientActions = clientActions;
            ItsAirTimePercentage = airTimePercentage;
            ItsRssi = _broadcastClient.ItsRssi;
            ItsNoise = _broadcastClient.ItsNoise;
            ItsClientCount = activeClientCount;
        }

        /// <summary>
        /// Returns list of ALL time segments
        /// </summary>
        /// <returns></returns>
        public List<DeviceTimeSegment> GetAllTimeSegments()
        {
            lock (_allTimeSegmentsLock)
            {
                return new List<DeviceTimeSegment>(_allTimeSegments.Values);
            }
        }

        /// <summary>
        /// Trims its own TimeSegment collection and then calls
        /// TrimTimeSegmentCollection() on each client
        /// </summary>
        /// <param name="trimDateTime"></param>
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

        /// <summary>
        /// Returns the best name for BSSID in order:
        /// Alias, Broadcast Name, Vendor_MAC, MAC
        /// </summary>
        /// <returns></returns>
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

        private void BuildBasicRatesString()
        {
            _basicRatesString = String.Join(", ", ItsBasicRates);
        }

        public override string ToString()
        {
            return $"{ItsName}";
        }

        #endregion
    }
}
