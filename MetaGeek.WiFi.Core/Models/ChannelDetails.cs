using System;
using System.Collections.Generic;
using System.Linq;
using MetaGeek.WiFi.Core.Interfaces;
using Prism.Mvvm;

namespace MetaGeek.WiFi.Core.Models
{
    public class ChannelDetails : BindableBase, IChannelDetails
    {
        #region Fields

        private const double FLOATING_POINT_TOLERANCE = 0.001;
        private uint _channel;
        private int _packetCount;
        private bool _beaconDetectedFlag;
        private bool _isScanningFlag;
        private int _eightyChannel;
        private int _fortyChannel;
        private double _airTimePercentage;
        private float _centerFreqMhz;
        private float _minFreqMhz;
        private float _maxFreqMhz;
        private int? _maxRssi;
        private double? _spectrumUtilization;
        private bool _nonErpPresentFlag;
        private IBssidDetails _loudestBssid;
        private double? _maxAirTimePercentage;
        private IBssidDetails _maxAirTimeBssid;
        private bool _itsValidInRegionFlag = true;
        private TimeSpan _selectedTimeSpan;
        private ChannelTimeSegment _currentTimeSegment;
        private ChannelTimeSegment _latestTimeSegment;
        private readonly object _currentTimeSegmentLock;
        private SortedList<DateTime, ChannelTimeSegment> _allTimeSegments;
        private readonly object _allTimeSegmentsLock;
        private readonly object _updateTimeSegmentDataLock;
        private DateTime _tailOfTimeSpan;
        private TimeSpan _totalAirtimeInTimespan;
        private TimeSpan _totalScanTimeInTimespan;
        private int _clientCount;

        #endregion

        #region Properties
        public uint ItsChannelNumber
        {
            get { return _channel; }
            set
            {
                if (value == _channel) return;
                _channel = value;
                SetProperty(ref _channel, value);
            }
        }

        //public bool ItsCanScanChannelFlag
        //{
        //    get { return _supportedChannelFlag; }
        //    set
        //    {
        //        if (value == _supportedChannelFlag) return;
        //        _supportedChannelFlag = value;
        //        RaisePropertyChanged(() => ItsCanScanChannelFlag);
        //    }
        //}

        public bool ItsValidInRegionFlag
        {
            get { return _itsValidInRegionFlag; }
            set { _itsValidInRegionFlag = value; }
        }

        public int ItsNonBeaconPacketCount
        {
            get { return _packetCount; }
            set
            {
                if (value == _packetCount) return;
                _packetCount = value;
                SetProperty(ref _packetCount, value);
            }
        }

        public int ItsClientCount
        {
            get { return _clientCount; }
            set
            {
                if (value == _clientCount) return;
                _clientCount = value;
                SetProperty(ref _clientCount, value);
            }
        }

        public bool ItsIsScanningFlag
        {
            get { return _isScanningFlag; }
            set
            {
                if (value == _isScanningFlag) return;
                _isScanningFlag = value;
                SetProperty(ref _isScanningFlag, value);
            }
        }

        public int Its80MhzChannel
        {
            get { return _eightyChannel; }
            set { _eightyChannel = value; }
        }

        public int Its40MhzChannel
        {
            get
            {
                return _fortyChannel;
            }
            set { _fortyChannel = value; }
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

        public double ItsAirTime { get; set; }

        public float ItsCenterFreqMhz
        {
            get { return _centerFreqMhz; }
            set { _centerFreqMhz = value; }
        }

        public float ItsMinFreqMhz
        {
            get { return _minFreqMhz; }
            set { _minFreqMhz = value; }
        }

        public float ItsMaxFreqMhz
        {
            get { return _maxFreqMhz; }
            set { _maxFreqMhz = value; }
        }

        public int? ItsMaxRssi
        {
            get { return _maxRssi; }
            set
            {
                if (_maxRssi != null && value == _maxRssi) return;
                _maxRssi = value;
                SetProperty(ref _maxRssi, value);
            }
        }

        public string ItsLoudestSsid
        {
            get { return _loudestBssid == null ? null : _loudestBssid.ItsDisplaySsid; }
        }

        public IBssidDetails ItsLoudestBssid
        {
            get { return _loudestBssid; }
            set
            {
                if (value == _loudestBssid) return;
                _loudestBssid = value;
                SetProperty(ref _loudestBssid, value);
               // RaisePropertyChanged(() => ItsLoudestBssid);
                RaisePropertyChanged(ItsLoudestSsid);
            }
        }

        public double? ItsMaxAirTimePercentage
        {
            get { return _maxAirTimePercentage; }
            set
            {
                if (_maxAirTimePercentage != null && value == _maxAirTimePercentage) return;
                _maxAirTimePercentage = value;
                SetProperty(ref _maxAirTimePercentage, value);
            }
        }

        public IBssidDetails ItsMaxAirTimeBssid
        {
            get { return _maxAirTimeBssid; }
            set
            {
                if (value == _maxAirTimeBssid) return;
                _maxAirTimeBssid = value;
                SetProperty(ref _maxAirTimeBssid, value);
                //
                RaisePropertyChanged(ItsMaxAirTimeSsid);
            }
        }

        public string ItsMaxAirTimeSsid
        {
            get { return _maxAirTimeBssid?.ItsDisplaySsid; }
        }

        public double? ItsSpectrumUtilization
        {
            get { return _spectrumUtilization; }
            set
            {
                _spectrumUtilization = value;
                SetProperty(ref _spectrumUtilization, value);
            }
        }

        public bool ItsNonErpPresentFlag
        {
            get { return _nonErpPresentFlag; }
            set
            {
                if (value == _nonErpPresentFlag) return;
                _nonErpPresentFlag = value;
                SetProperty(ref _nonErpPresentFlag, value);
                RaisePropertyChanged(ItsNonErpPresentString);
            }
        }

        public string ItsNonErpPresentString
        {
            get
            {
                return _nonErpPresentFlag ? "YES" : string.Empty;
            }
        }

        public bool ItsBeaconDetectedFlag
        {
            get { return _beaconDetectedFlag; }
            set
            {
                if (value == _beaconDetectedFlag) return;
                _beaconDetectedFlag = value;
                SetProperty(ref _beaconDetectedFlag, value);
                //RaisePropertyChanged(() => ItsBeaconDetectedFlag);
            }
        }

        #endregion

        #region Constructors

        public ChannelDetails(uint channel)
        {
            ItsChannelNumber = channel;

            _currentTimeSegment = new ChannelTimeSegment();
            _currentTimeSegmentLock = new object();
            _allTimeSegments = new SortedList<DateTime, ChannelTimeSegment>();
            _allTimeSegmentsLock = new object();
            _updateTimeSegmentDataLock = new object();
            _tailOfTimeSpan = DateTime.MinValue;
        }

        public ChannelDetails(uint channel, float centerFreqMhz, float widthMhz) : this(channel)
        {
            ItsCenterFreqMhz = centerFreqMhz;
            ItsMinFreqMhz = centerFreqMhz - (widthMhz / 2.0f);
            ItsMaxFreqMhz = centerFreqMhz + (widthMhz / 2.0f);
        }

        public ChannelDetails(uint channel, int fortyChannel, int eightyChannel, float centerFreqMhz, float widthMhz) : this(channel, centerFreqMhz, widthMhz)
        {
            Its40MhzChannel = fortyChannel;
            Its80MhzChannel = eightyChannel;
        }

        #endregion

        #region Methods

        public List<ChannelTimeSegment> GetChannelTimeSegmentsBetweenTime(DateTime startTime, DateTime endTime)
        {
            var timeSegments = new List<ChannelTimeSegment>();

            lock (_allTimeSegmentsLock)
            {
                foreach (var timeSegment in _allTimeSegments.Values)
                {
                    if (timeSegment.ItsTimestamp > endTime) break;
                    if (timeSegment.ItsTimestamp >= startTime)
                    {
                        timeSegments.Add(timeSegment);
                    }
                }
            }

            return timeSegments;
        }


        public void ProcessPacket(PacketMetaData packet)
        {
            lock (_currentTimeSegmentLock)
            {
                _currentTimeSegment.AddPacket(packet);
            }
        }

        public ChannelTimeSegment FinalizeAirtimeSegment(DateTime startTime, TimeSpan scanTimeSpan, int clientCount)
        {
            ChannelTimeSegment finalizedTimeSegment = null;

            lock (_currentTimeSegmentLock)
            {
                // finalize this segment, add it to list, and start a new segment
                _currentTimeSegment.ItsTimestamp = startTime;
                _currentTimeSegment.ItsClientCount = clientCount;
                _currentTimeSegment.FinalizeTimeSegment(scanTimeSpan);

                finalizedTimeSegment = _currentTimeSegment;
                StartNextTimeSegment();
            }

            AddTimeSegmentToTimeSpan(_latestTimeSegment);
            RemoveOlderTimeSegmentFromTimeSpan();

            ItsClientCount = clientCount;
            ItsAirTime = _totalAirtimeInTimespan.TotalMilliseconds;
            ItsAirTimePercentage = ItsAirTime / _totalScanTimeInTimespan.TotalMilliseconds;

            return finalizedTimeSegment;
        }

        private void AddTimeSegmentToTimeSpan(ChannelTimeSegment timeSegment)
        {
            if (timeSegment == null)
                return;

            lock (_updateTimeSegmentDataLock)
            {
                _totalAirtimeInTimespan += timeSegment.ItsAirTime;
                _totalScanTimeInTimespan += timeSegment.ItsScanTime;
                ItsNonBeaconPacketCount += timeSegment.ItsPacketCount;
            }
        }

        private void RemoveOlderTimeSegmentFromTimeSpan()
        {
            List<ChannelTimeSegment> oldTimeSegments = null;
            var newTailOfTimeSpan = _latestTimeSegment.ItsTimestamp.AddSeconds(-_selectedTimeSpan.TotalSeconds);


            lock (_allTimeSegmentsLock)
            {
                oldTimeSegments = _allTimeSegments.Values.Where(t => t != null && t.ItsTimestamp >= _tailOfTimeSpan && t.ItsTimestamp < newTailOfTimeSpan).ToList();
            }
            _tailOfTimeSpan = newTailOfTimeSpan;

            if (oldTimeSegments == null || oldTimeSegments.Count == 0) return;

            lock (_updateTimeSegmentDataLock)
            {
                foreach (var timeSegment in oldTimeSegments)
                {
                    _totalAirtimeInTimespan -= timeSegment.ItsAirTime;
                    _totalScanTimeInTimespan -= timeSegment.ItsScanTime;
                    ItsNonBeaconPacketCount -= timeSegment.ItsPacketCount;
                }
            }
        }

        private void StartNextTimeSegment()
        {
            lock (_allTimeSegmentsLock)
            {
                if (!_allTimeSegments.ContainsKey(_currentTimeSegment.ItsTimestamp))
                {
                    _allTimeSegments.Add(_currentTimeSegment.ItsTimestamp, _currentTimeSegment);
                }
            }

            _latestTimeSegment = _currentTimeSegment;

            _currentTimeSegment = new ChannelTimeSegment();
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

        public void UpdateChannelStatsBasedOnTimeRange(DateTime startTime, DateTime endTime)
        {
            List<ChannelTimeSegment> selectedSegments = GetChannelTimeSegmentsBetweenTime(startTime, endTime);
            _latestTimeSegment = selectedSegments.FirstOrDefault();

            RebuildChannelStatsBasedOnTimeSegments(selectedSegments);
        }

        public void UpdateSelectedTimeSpan(TimeSpan timeSpan)
        {
            _selectedTimeSpan = timeSpan;

            if (_latestTimeSegment == null) return;

            var startTime = _latestTimeSegment.ItsTimestamp.AddMilliseconds(-_selectedTimeSpan.TotalMilliseconds);
            var endTime = _latestTimeSegment.ItsTimestamp;

            // gathering the channel Time Segments in selected timespan
            List<ChannelTimeSegment> selectedSegments = GetChannelTimeSegmentsBetweenTime(startTime, endTime);

            RebuildChannelStatsBasedOnTimeSegments(selectedSegments);
        }

        private void RebuildChannelStatsBasedOnTimeSegments(List<ChannelTimeSegment> selectedSegments)
        {
            if (_allTimeSegments.Count == 0) return;

            var totalPackets = 0;
            var totalAirTime = TimeSpan.Zero;
            var totalScanTime = TimeSpan.Zero;

            lock (_updateTimeSegmentDataLock)
            {
                foreach (var segment in selectedSegments)
                {
                    if (segment == null) continue;

                    totalAirTime += segment.ItsAirTime;
                    totalScanTime += segment.ItsScanTime;
                    totalPackets += segment.ItsPacketCount;
                }

                _totalAirtimeInTimespan = totalAirTime;
                _totalScanTimeInTimespan = totalScanTime;
                _packetCount = totalPackets;
                _tailOfTimeSpan = _latestTimeSegment != null ? _latestTimeSegment.ItsTimestamp.AddSeconds(-_selectedTimeSpan.TotalSeconds) : DateTime.MinValue;

            }

            ItsAirTime = _totalAirtimeInTimespan.TotalMilliseconds;
            ItsAirTimePercentage = ItsAirTime / _totalScanTimeInTimespan.TotalMilliseconds;
        }

        #endregion
    }
}
