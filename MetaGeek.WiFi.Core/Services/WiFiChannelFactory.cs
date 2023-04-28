#region usings

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MetaGeek.WiFi.Core.Enums;
using MetaGeek.WiFi.Core.Models;

#endregion

namespace MetaGeek.WiFi.Core.Services
{
    public class WiFiChannelFactory
    {
        #region Fields

        private const float FIVEGHZ_END_FREQUENCY_HZ_PADDED = 5837000000;
        private const float FIVEGHZ_START_FREQUENCY_HZ_PADDED = 5140000000;
        private const float TWOGHZ_END_FREQUENCY_HZ_PADDED = 2497000000;
        private const float TWOGHZ_START_FREQUENCY_HZ_PADDED = 2390000000;

        private static float _maxFreq24GhzMhz;
        private static float _minFreq24GhzMhz;
        private static float _maxFreq5GhzMhz;

        private static List<Channel> _24GHzchannels;
        private static List<Channel> _5GHzchannels;
        private static List<Channel> _acChannels;
        private static List<Channel> _unfiltered24GHzchannels;
        private static List<Channel> _unfiltered5GHzchannels;
        private static List<Channel> _zigbeeChannels;
        private static string _locale;

        // channels table based upon
        // http://en.wikipedia.org/wiki/List_of_WLAN_channels#5.C2.A0GHz_.28802.11a.2Fh.2Fj.2Fn.29.5B13.5D
        private static List<uint> _validChannelsByRegulatoryRegion;
        private static IEnumerable<Channel> _5GHzUNII1Channels;
        private static IEnumerable<Channel> _5GHzUNII2Channels;
        private static IEnumerable<Channel> _5GHzUNII3Channels;
        private static IEnumerable<Channel> _5GHzISMChannels;

        #endregion Fields

        #region Enumerations

        private enum CountryFilters
        {
            None = 0,
            NorthAmerica,
            Europe,
            Japan,
            Singapore,
            China,
            Israel,
            Korea,
            Turkey,
            Australia,
            Africa,
        }

        #endregion Enumerations

        #region Constructors

        static WiFiChannelFactory()
        {
            InitializeWithTwoLetterISORegionName(RegionInfo.CurrentRegion.TwoLetterISORegionName);
        }

        #endregion Constructors

        #region Methods

        public static void InitializeWithTwoLetterISORegionName(string locale)
        {
            var updated = UpdateRegionIfNeeded(locale);

            if (updated)
            {
                BuildChannelCollection();
            }
        }

        private static void BuildChannelCollection()
        {
            Build24GHzChannelCollections();
            Build5GHzChannelCollections();
            BuildAcChannelCollection();
            BuildZigbeeChannelCollections();
        }

        private static void BuildAcChannelCollection()
        {
            _acChannels = new List<Channel>();

            AddChannel(_acChannels, (new Channel(36, 5180, 20)));
            AddChannel(_acChannels, (new Channel(40, 5200, 20)));
            AddChannel(_acChannels, (new Channel(44, 5220, 20)));
            AddChannel(_acChannels, (new Channel(48, 5240, 20)));
            AddChannel(_acChannels, (new Channel(52, 5260, 20)));
            AddChannel(_acChannels, (new Channel(56, 5280, 20)));
            AddChannel(_acChannels, (new Channel(60, 5300, 20)));
            AddChannel(_acChannels, (new Channel(64, 5320, 20)));
            AddChannel(_acChannels, (new Channel(100, 5500, 20)));
            AddChannel(_acChannels, (new Channel(104, 5520, 20)));
            AddChannel(_acChannels, (new Channel(108, 5540, 20)));
            AddChannel(_acChannels, (new Channel(112, 5560, 20)));

            AddChannel(_acChannels, (new Channel(149, 5745, 20)));
            AddChannel(_acChannels, (new Channel(153, 5765, 20)));
            AddChannel(_acChannels, (new Channel(157, 5785, 20)));
            AddChannel(_acChannels, (new Channel(161, 5805, 20)));
            AddChannel(_acChannels, (new Channel(165, 5825, 20)));
        }

        private static void Build5GHzChannelCollections()
        {
            _unfiltered5GHzchannels = new List<Channel>
                {
                    new Channel(34, 5170, 20),
                    new Channel(36, 5180, 20),
                    new Channel(38, 5190, 20),
                    new Channel(40, 5200, 20),
                    new Channel(42, 5210, 20),
                    new Channel(44, 5220, 20),
                    new Channel(46, 5230, 20),
                    new Channel(48, 5240, 20),
                    new Channel(50, 5250, 20),
                    new Channel(52, 5260, 20),
                    new Channel(56, 5280, 20),
                    new Channel(60, 5300, 20),
                    new Channel(64, 5320, 20),

                    new Channel(100, 5500, 20),
                    new Channel(104, 5520, 20),
                    new Channel(108, 5540, 20),
                    new Channel(112, 5560, 20),
                    new Channel(114, 5570, 20),
                    new Channel(116, 5580, 20),
                    new Channel(120, 5600, 20),
                    new Channel(124, 5620, 20),
                    new Channel(128, 5640, 20),
                    new Channel(132, 5660, 20),
                    new Channel(136, 5680, 20),
                    new Channel(140, 5700, 20),
                    new Channel(144, 5720, 20),

                    new Channel(149, 5745, 20),
                    new Channel(153, 5765, 20),
                    new Channel(157, 5785, 20),
                    new Channel(161, 5805, 20),
                    new Channel(165, 5825, 20)
                };

            BuildFiltered5GhzCollection();
            BuildUnfiltered5GhzCollection();
        }

        private static void BuildFiltered5GhzCollection()
        {
            _5GHzchannels = new List<Channel>();
            Channel addedChannel = null;
            foreach (var channel in _unfiltered5GHzchannels)
            {
                var added = AddChannel(_5GHzchannels, channel);
                if (added)
                {
                    addedChannel = channel;
                }
            }

            _maxFreq5GhzMhz = addedChannel.ItsMaxFreqMhz;
        }

        private static void BuildUnfiltered5GhzCollection()
        {
            RemoveUnfilteredChannel(34);
            RemoveUnfilteredChannel(38);
            RemoveUnfilteredChannel(42);
            RemoveUnfilteredChannel(46);
        }

        private static void Build24GHzChannelCollections()
        {
            _unfiltered24GHzchannels = new List<Channel>
                {
                    new Channel(1, 2412, 22),
                    new Channel(2, 2417, 22),
                    new Channel(3, 2422, 22),
                    new Channel(4, 2427, 22),
                    new Channel(5, 2432, 22),
                    new Channel(6, 2437, 22),
                    new Channel(7, 2442, 22),
                    new Channel(8, 2447, 22),
                    new Channel(9, 2452, 22),
                    new Channel(10, 2457, 22),
                    new Channel(11, 2462, 22),
                    new Channel(12, 2467, 22),
                    new Channel(13, 2472, 22),
                    new Channel(14, 2484, 22)
                };

            _24GHzchannels = new List<Channel>();
            Channel addedChannel = null;
            foreach (var channel in _unfiltered24GHzchannels)
            {
                var added = AddChannel(_24GHzchannels, channel);
                if (added)
                {
                    addedChannel = channel;
                }
            }

            _maxFreq24GhzMhz = addedChannel.ItsMaxFreqMhz;
            _minFreq24GhzMhz = _24GHzchannels[0].ItsMinFreqMhz;
        }

        private static void BuildZigbeeChannelCollections()
        {
            _zigbeeChannels = new List<Channel>
                {
                    // Sub GHz channels
                    new Channel(0, 868.3f, 2),
                    new Channel(1, 906, 2),
                    new Channel(2, 908, 2),
                    new Channel(3, 910, 2),
                    new Channel(4, 912, 2),
                    new Channel(5, 914, 2),
                    new Channel(6, 916, 2),
                    new Channel(7, 918, 2),
                    new Channel(8, 920, 2),
                    new Channel(9, 922, 2),
                    new Channel(10, 924, 2),

                    // 2.4 GHz channels
                    new Channel(11, 2405, 5),
                    new Channel(12, 2410, 5),
                    new Channel(13, 2415, 5),
                    new Channel(14, 2420, 5),
                    new Channel(15, 2425, 5),
                    new Channel(16, 2430, 5),
                    new Channel(17, 2435, 5),
                    new Channel(18, 2440, 5),
                    new Channel(19, 2445, 5),
                    new Channel(20, 2450, 5),
                    new Channel(21, 2455, 5),
                    new Channel(22, 2460, 5),
                    new Channel(23, 2465, 5),
                    new Channel(24, 2470, 5),
                    new Channel(25, 2475, 5),
                    new Channel(26, 2480, 5)
                };
        }

        private static bool UpdateRegionIfNeeded(string locale)
        {
            if (_validChannelsByRegulatoryRegion != null && locale.Equals(_locale)) return false;

            _locale = locale;
            CreateValidChannelsFilters();

            return true;
        }

        private static bool AddChannel(List<Channel> collection, Channel channel)
        {
            if (!IsChannelValidForLocale(channel.ItsChannelNumber)) return false;
            collection.Add(channel);

            return true;
        }

        private static void CreateBaseList()
        {
            // add all channels
            _validChannelsByRegulatoryRegion = new List<uint>();
            for (uint channel = 1; channel <= 14; channel++)
                _validChannelsByRegulatoryRegion.Add(channel);
            for (uint channel = 34; channel <= 48; channel += 2)
                _validChannelsByRegulatoryRegion.Add(channel);

            _validChannelsByRegulatoryRegion.Add(50);
            _validChannelsByRegulatoryRegion.Add(52);
            _validChannelsByRegulatoryRegion.Add(56);
            _validChannelsByRegulatoryRegion.Add(60);
            _validChannelsByRegulatoryRegion.Add(64);

            for (uint channel = 100; channel <= 112; channel += 4)
                _validChannelsByRegulatoryRegion.Add(channel);

            _validChannelsByRegulatoryRegion.Add(114);

            for (uint channel = 116; channel <= 144; channel += 4)
                _validChannelsByRegulatoryRegion.Add(channel);

            for (uint channel = 149; channel <= 165; channel += 4)
                _validChannelsByRegulatoryRegion.Add(channel);
        }

        // _locale is set to the two letter ISO 3166 code for the country/region
        // http://nationsonline.org/oneworld/country_code_list.htm
        private static void CreateValidChannelsFilters()
        {
            CreateBaseList();

            if (_locale.Equals("US") || _locale.Equals("CA") || _locale.Equals("CB") || _locale.Equals("MX") || _locale.Equals("JM"))
                FilterInvalidChannels(CountryFilters.NorthAmerica);
            else if (_locale.Equals("AU") || _locale.Equals("NZ"))
                FilterInvalidChannels(CountryFilters.Australia);
            else if (_locale.Equals("IL"))
                FilterInvalidChannels(CountryFilters.Israel);
            else if (_locale.Equals("TR"))
                FilterInvalidChannels(CountryFilters.Turkey);
            else if (_locale.Equals("JP"))
                FilterInvalidChannels(CountryFilters.Japan);
            else if (_locale.Equals("KR"))
                FilterInvalidChannels(CountryFilters.Korea);
            else if (_locale.Equals("ZA") || _locale.Equals("ZW") || _locale.Equals("KE"))
                FilterInvalidChannels(CountryFilters.Africa);
            else if (_locale.Equals("SG"))
                FilterInvalidChannels(CountryFilters.Singapore);
            else if (_locale.Equals("CN"))
                FilterInvalidChannels(CountryFilters.China);
            else
                // everyone else must be European <-- um... no!
                FilterInvalidChannels(CountryFilters.Europe);
        }

        private static void FilterInvalidChannels(CountryFilters country)
        {
            switch (country)
            {
                case CountryFilters.NorthAmerica:
                    RemoveNonAcLow5GhzChannels();
                    RemoveChannel(12);
                    RemoveChannel(13);
                    RemoveChannel(14);
                    break;

                case CountryFilters.Europe:
                    RemoveNonAcLow5GhzChannels();
                    //RemoveChannel(144);
                    RemoveChannel(14);
                    break;

                case CountryFilters.Japan:
                    RemoveHigh5GhzChannels();
                    RemoveChannel(144);
                    break;

                case CountryFilters.Singapore:
                    RemoveMiddle5GhzChannels();
                    RemoveChannel(14);
                    break;

                case CountryFilters.China:
                    RemoveMiddle5GhzChannels();
                    RemoveChannel(14);
                    RemoveChannel(34);
                    break;

                case CountryFilters.Israel:
                    RemoveMiddle5GhzChannels();
                    RemoveHigh5GhzChannels();
                    RemoveChannel(14);
                    break;

                case CountryFilters.Korea:
                    RemoveChannel(132);
                    RemoveChannel(136);
                    RemoveChannel(140);
                    RemoveChannel(144);
                    RemoveChannel(14);
                    break;

                case CountryFilters.Africa:
                case CountryFilters.Turkey:
                    RemoveHigh5GhzChannels();
                    RemoveChannel(144);
                    RemoveChannel(14);
                    break;

                case CountryFilters.Australia:
                    RemoveNonAcLow5GhzChannels();
                    RemoveChannel(120);
                    RemoveChannel(124);
                    RemoveChannel(128);
                    RemoveChannel(144);
                    RemoveChannel(14);
                    break;

                case CountryFilters.None:
                    break;
            }
        }

        private static bool IsChannelValidForLocale(uint channel)
        {
            return (_validChannelsByRegulatoryRegion.BinarySearch(channel) >= 0);
        }

        private static void RemoveChannel(uint channel)
        {
            var index = _validChannelsByRegulatoryRegion.BinarySearch(channel);
            if (index < 0) return;
            _validChannelsByRegulatoryRegion.RemoveAt(index);
        }

        private static void RemoveUnfilteredChannel(uint channelNumber)
        {
            for (var i = 0; i < _unfiltered5GHzchannels.Count; i++)
            {
                var channel = _unfiltered5GHzchannels[i];
                if (channel.ItsChannelNumber != channelNumber) continue;
                _unfiltered5GHzchannels.RemoveAt(i);
                return;
            }
        }

        private static void RemoveHigh5GhzChannels()
        {
            RemoveChannel(149);
            RemoveChannel(153);
            RemoveChannel(157);
            RemoveChannel(161);
            RemoveChannel(165);
        }

        private static void RemoveNonAcLow5GhzChannels()
        {
            RemoveChannel(34);
            RemoveChannel(38);
            RemoveChannel(42);
            RemoveChannel(46);
        }

        private static void RemoveMiddle5GhzChannels()
        {
            RemoveChannel(100);
            RemoveChannel(104);
            RemoveChannel(108);
            RemoveChannel(112);
            RemoveChannel(116);
            RemoveChannel(120);
            RemoveChannel(124);
            RemoveChannel(128);
            RemoveChannel(132);
            RemoveChannel(136);
            RemoveChannel(140);
            RemoveChannel(144);
        }

        public static Channel GetChannel(uint channelNumber)
        {
            Channel toReturn = null;

            if (channelNumber >= 1 && channelNumber <= 14)
            {
                toReturn = _24GHzchannels.FirstOrDefault(c => c.ItsChannelNumber == channelNumber);
            }
            else if (channelNumber >= 34 && channelNumber <= 165)
            {
                toReturn = _5GHzchannels.FirstOrDefault(c => c.ItsChannelNumber == channelNumber);
            }

            return toReturn;
        }

        public static List<Channel> GetChannelsByBand(ChannelBand band, bool unfiltered = false)
        {
            var toReturn = new List<Channel>();
            switch (band)
            {
                case ChannelBand.TwoGhz:
                    toReturn.AddRange(unfiltered ? _unfiltered24GHzchannels : _24GHzchannels);
                    break;

                case ChannelBand.FiveGhz:
                    toReturn.AddRange(unfiltered ? _unfiltered5GHzchannels : _5GHzchannels);
                    break;

                case ChannelBand.Both:
                    toReturn.AddRange(unfiltered ? _unfiltered24GHzchannels : _24GHzchannels);
                    toReturn.AddRange(unfiltered ? _unfiltered5GHzchannels : _5GHzchannels);
                    break;

                case ChannelBand.FiveGhzUnii1:
                    toReturn.AddRange(_5GHzUNII1Channels ?? (_5GHzUNII1Channels = _5GHzchannels.Where(c => c.ItsChannelNumber >= 36 && c.ItsChannelNumber <= 48)));
                    break;
                case ChannelBand.FiveGhzUnii2:
                    toReturn.AddRange(_5GHzUNII2Channels ?? (_5GHzUNII2Channels = _5GHzchannels.Where(c => c.ItsChannelNumber >= 52 && c.ItsChannelNumber <= 144)));
                    break;
                case ChannelBand.FiveGhzUnii3:
                    toReturn.AddRange(_5GHzUNII3Channels ?? (_5GHzUNII3Channels = _5GHzchannels.Where(c => c.ItsChannelNumber >= 149 && c.ItsChannelNumber <= 161)));
                    break;
                case ChannelBand.FiveGhzIsm:
                    toReturn.AddRange(_5GHzISMChannels ?? (_5GHzISMChannels = _5GHzchannels.Where(c => c.ItsChannelNumber == 165)));
                    break;
                case ChannelBand.Zigbee:
                    toReturn.AddRange(_zigbeeChannels);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("band");
            }
            return toReturn;
        }

        public static float GetMaxFreqMhzForBand(ChannelBand band)
        {
            return band == ChannelBand.TwoGhz ? _maxFreq24GhzMhz : _maxFreq5GhzMhz;
        }

        public static ChannelBand GetChannelBandByCenterFreqMhz(float channelCenterFreq)
        {
            return (channelCenterFreq >= _minFreq24GhzMhz && channelCenterFreq <= _maxFreq24GhzMhz) ? ChannelBand.TwoGhz : ChannelBand.FiveGhz;
        }

        public static ChannelBand DetermineChannelBandBasedOnFreqHzRange(float startFrequencyHz, float endFrequencyHz)
        {
            if (startFrequencyHz >= TWOGHZ_START_FREQUENCY_HZ_PADDED && endFrequencyHz <= TWOGHZ_END_FREQUENCY_HZ_PADDED)
                return ChannelBand.TwoGhz;
            if (startFrequencyHz >= FIVEGHZ_START_FREQUENCY_HZ_PADDED && endFrequencyHz <= FIVEGHZ_END_FREQUENCY_HZ_PADDED)
                return ChannelBand.FiveGhz;
            if (endFrequencyHz <= TWOGHZ_START_FREQUENCY_HZ_PADDED)
                return ChannelBand.NineHundredMhzIsm;

            throw new ArgumentException(String.Format("The starting and ending frequencies specified do not fit within either the 2.4 GHz or 5 GHz bands. Starting frequency: {0} Ending Frequency:{1}", startFrequencyHz, endFrequencyHz));
        }

        public static ChannelBand DetermineChannelBand(uint channel)
        {
            if (channel <= 14) return ChannelBand.TwoGhz;
            return channel <= 200 ? ChannelBand.FiveGhz : ChannelBand.NineHundredMhzIsm;
        }

        public static uint[] GetStandardChannelsForBand(ChannelBand channelBand)
        {
            if (channelBand == ChannelBand.TwoGhz)
            {
                // TODO Resolve "standard channels" for Europe and Japan
                return new uint[] { 1, 6, 11 };
            }
            return GetChannelsByBand(channelBand).Select(e => e.ItsChannelNumber).ToArray();
        }

        public static uint[] GetStandardAcChannels()
        {
            return _acChannels.Select(e => e.ItsChannelNumber).ToArray();
        }

        public static IList<Channel> GetPartialChannelsWithRange(ChannelBand channelBand, float minMhz, float maxMhz)
        {
            var channelsByBand = GetChannelsByBand(channelBand, true);
            return channelsByBand.Where(channel => ChannelIsPartiallyInsideRange(channel, minMhz, maxMhz)).ToList();
        }

        private static bool ChannelIsPartiallyInsideRange(Channel channel, float minMhz, float maxMhz)
        {
            return channel.ItsCenterFreqMhz >= minMhz && channel.ItsCenterFreqMhz <= maxMhz;
        }

        public static IList<Channel> GetCompleteChannelsWithRange(ChannelBand channelBand, float minMhz, float maxMhz)
        {
            var channelsByBand = GetChannelsByBand(channelBand, true);
            return channelsByBand.Where(channel => ChannelIsCompletelyInsideRange(channel, minMhz, maxMhz)).ToList();
        }

        private static bool ChannelIsCompletelyInsideRange(Channel channel, float minMhz, float maxMhz)
        {
            var roundedMinMhz = Math.Round(minMhz - 1);
            var roundedMaxMhz = Math.Round(maxMhz + 1);
            return channel.ItsMinFreqMhz >= roundedMinMhz && channel.ItsMaxFreqMhz <= roundedMaxMhz;
        }

        #endregion Methods
    }
}
