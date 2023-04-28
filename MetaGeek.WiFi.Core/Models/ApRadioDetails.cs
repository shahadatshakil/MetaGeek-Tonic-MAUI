using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using MetaGeek.WiFi.Core.Interfaces;

namespace MetaGeek.WiFi.Core.Models
{
    public class ApRadioDetails : IApRadioDetails
    {
        #region Fields

        private const int MAX_RSSI_DIFF_FOR_SAME_RADIO = 5;

        private string _broadcastName;
        private string _alias;
        private readonly ConcurrentDictionary<ulong, IBssidDetails> _bssidCollection;
        private readonly PhyTypeInfo _phyTypeInfo;
        private readonly ChannelInfo _channelInfo;
        private string _taxonomySignature;
        private ulong _macComparisonMask;
        private int _accessPointId;
        private DateTime _firstSeenDateTime;

        #endregion

        #region Properties

        public int ItsAccessPointId
        {
            get { return _accessPointId; }
            set { _accessPointId = value; }
        }

        public DateTime ItsFirstSeenDateTime
        {
            get { return _firstSeenDateTime; }
        }

        public ConcurrentDictionary<ulong, IBssidDetails> ItsBssidCollection
        {
            get { return _bssidCollection; }
        }

        public PhyTypeInfo ItsPhyTypeInfo
        {
            get { return _phyTypeInfo; }
        }

        public ChannelInfo ItsChannelInfo
        {
            get { return _channelInfo; }
        }

        public uint ItsMaxMcsIndex { get; }
        public uint ItsSpacialStreamCount { get; }
        public int? ItsMaxRssi { get; private set; }

        public double ItsAirTimePercentage { get; private set; }
        public string ItsBroadcastName
        {
            get { return _broadcastName; }
            private set { _broadcastName = value; }
        }

        public string ItsAlias
        {
            get { return _alias; }
            private set { _alias = value; }
        }

        public string ItsName
        {
            get
            {
                if(!string.IsNullOrEmpty(_alias))
                    return _alias;
                if (!string.IsNullOrEmpty(_broadcastName))
                    return _broadcastName;
                return string.Empty;
            }
        }

        public string ItsTaxonomySignature
        {
            get { return _taxonomySignature; }
            set { _taxonomySignature = value; }
        }

        #endregion

        #region Constructors

        public ApRadioDetails(IBssidDetails bssid)
        {
            Contract.Requires(bssid != null);

            _bssidCollection = new ConcurrentDictionary<ulong, IBssidDetails>();
            var result =_bssidCollection.TryAdd(bssid.ItsMacAddress.ItsUlongValue, bssid);
            if (result)
            {
                bssid.ItsRadioGroup = this;

                _phyTypeInfo = bssid.ItsPhyTypeInfo;
                _channelInfo = bssid.ItsChannelInfo;
                _firstSeenDateTime = bssid.ItsFirstSeenDateTime;
                ItsMaxRssi = bssid.ItsRssi;
                ItsMaxMcsIndex = bssid.ItsMaxMcsIndex;
                ItsSpacialStreamCount = bssid.ItsSpacialStreamCount;
                ItsBroadcastName = bssid.ItsBroadcastName;
                ItsAlias = bssid.ItsAlias;
                ItsTaxonomySignature = bssid.ItsTaxonomySignature;
            }
        }

        #endregion

        #region Methods

        public bool IsSameRadio(IBssidDetails bssid)
        {
            Contract.Requires(bssid != null);

            if (ItsMaxRssi == null || bssid.ItsRssi == null) return false;

            // eero radios have hidden SSID with different channel width than the broadcast SSID
            if (ItsChannelInfo.ItsPrimaryChannel != bssid.ItsChannelInfo.ItsPrimaryChannel) return false;
            if (!MacsAreAligned(bssid)) return false;

            // check RSSI 
            if (Math.Abs(ItsMaxRssi.Value - bssid.ItsRssi.Value) > MAX_RSSI_DIFF_FOR_SAME_RADIO) return false;

            if (ItsMaxMcsIndex != bssid.ItsMaxMcsIndex) return false;
            if (ItsSpacialStreamCount != bssid.ItsSpacialStreamCount) return false;

            if (!string.IsNullOrEmpty(ItsBroadcastName) && !string.IsNullOrEmpty(bssid.ItsBroadcastName) &&
                ItsBroadcastName != bssid.ItsBroadcastName) return false;

            // EERO RADIOS ARE BREAKING THIS - VIRTUAL SSIDS WITH DIFFERENT CAPABILITIES... is it different radio???
            //if (!Equals(ItsPhyTypeInfo, bssid.ItsPhyTypeInfo)) return false;
            //if (ItsTaxonomySignature != bssid.ItsTaxonomySignature) return false;

            return true;
        }

        // TODO pass in number of nibbles that need to match? What if we used the comparison mask for other radios in this network? 
        private bool MacsAreAligned(IBssidDetails bssid)
        {
            var bssidCount = _bssidCollection.Count;
            var existingBssid = _bssidCollection.FirstOrDefault().Value;

            // second radio creates comparison mask
            if (bssidCount == 1)
            {
                var matchingNibbleCount =
                    MatchingNibbleCount(existingBssid.ItsMacAddress, bssid.ItsMacAddress);

                if (matchingNibbleCount < 10) return false;

                _macComparisonMask = GetMacComparisonMask(existingBssid.ItsMacAddress, bssid.ItsMacAddress);

                return true;
            }

            // third and higher radios just compare mask
            return (existingBssid.ItsMacAddress.ItsUlongValue & _macComparisonMask) ==
                   (bssid.ItsMacAddress.ItsUlongValue & _macComparisonMask);
        }

        public bool TryAddBssid(IBssidDetails bssid)
        {
            Contract.Requires(bssid != null);

            if (!IsSameRadio(bssid)) return false;

            if (_firstSeenDateTime == DateTime.MinValue || bssid.ItsFirstSeenDateTime < _firstSeenDateTime)
            {
                _firstSeenDateTime = bssid.ItsFirstSeenDateTime;
            }

            // TODO Right now ItsBroadcastName must match in order for it pass
            // TODO IsSameRadio() so this code will never run
            //if (string.IsNullOrEmpty(ItsBroadcastName))
            //{
            //    ItsBroadcastName = bssid.ItsBroadcastName;
            //}

            if (string.IsNullOrEmpty(ItsAlias))
            {
                ItsAlias = bssid.ItsAlias;
            }
            
            var result = _bssidCollection.TryAdd(bssid.ItsMacAddress.ItsUlongValue, bssid);
            if (result)
            {
                bssid.ItsRadioGroup = this;
            }

            return result;
        }

        public void UpdateAlias(string bssidAlias)
        {
            ItsAlias = bssidAlias;

            foreach (var bssid in ItsBssidCollection.Values)
            {
                bssid.ItsAlias = bssidAlias;
            }
        }

        public void UpdateRadio()
        {
            int? maxRssi = null;
            var totalAirTimePercentage = 0.0;
            foreach (var bssid in ItsBssidCollection.Values)
            {
                totalAirTimePercentage += bssid.ItsAirTimePercentage;
                if (maxRssi == null || bssid.ItsRssi > maxRssi)
                {
                    maxRssi = bssid.ItsRssi;
                }
            }

            ItsAirTimePercentage = totalAirTimePercentage;
            ItsMaxRssi = maxRssi;
        }

        private int MatchingNibbleCount(IMacAddress addressA, IMacAddress addressB)
        {
            var macA = addressA.ItsUlongValue;
            var macB = addressB.ItsUlongValue;

            var matchingCount = 0;
            for (var i = 0; i < 12; i++)
            {
                if ((macA & 0xF) == (macB & 0xF))
                    matchingCount++;

                macA = macA >> 4;
                macB = macB >> 4;
            }

            return matchingCount;
        }

        private ulong GetMacComparisonMask(IMacAddress addressA, IMacAddress addressB)
        {
            var macA = addressA.ItsUlongValue;
            var macB = addressB.ItsUlongValue;

            ulong totalMask = 0;
            ulong shiftingMask = 0xF;

            for (var i = 0; i < 12; i++)
            {
                if ((macA & 0xF) == (macB & 0xF))
                {
                    totalMask |= shiftingMask;
                }

                shiftingMask = shiftingMask << 4;
                macA = macA >> 4;
                macB = macB >> 4;
            }

            return totalMask;
        }

    }
    #endregion
}

