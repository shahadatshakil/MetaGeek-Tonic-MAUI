using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MetaGeek.WiFi.Core.Enums;
using MetaGeek.WiFi.Core.Events;
using MetaGeek.WiFi.Core.Interfaces;
using MetaGeek.WiFi.Core.Models;
using MetaGeek.WiFi.Core.Resources;
using Prism.Events;

namespace MetaGeek.WiFi.Core.Services
{
    public class WiFiCollectionsService : IWiFiCollectionsService
    {
        #region Fields
        public const int QUEUE_FLUSH_DELAY_MSEC = 100;
        public const string HIDDEN_SSID_NAME_PREFIX = "[HIDDEN] on";
        private const bool OKAY_TO_SWITCH_ESSIDS = true;
        private const bool HIDDEN_ESSID = true;

        private readonly IEventAggregator _eventAggregator;
        private readonly IVendorGenerator _vendorGenerator;
        private ConcurrentDictionary<ulong, IBssidDetails> _hiddenBssids;
        private readonly object _radioLock;

        #endregion

        #region Properties
        /// <summary>
        /// Collection of all clients seen in this session
        /// </summary>
        public ConcurrentDictionary<ulong, IClientDetails> ItsClientCollection { get; private set; }

        /// <summary>
        /// Collection of all BSSIDs seen in this session
        /// </summary>
        public ConcurrentDictionary<ulong, IBssidDetails> ItsBssidCollection { get; private set; }

        /// <summary>
        /// Collection of all ESSIDs seen in this session
        /// </summary>
        public ConcurrentDictionary<string, IEssidDetails> ItsEssidCollection { get; private set; }

        /// <summary>
        /// Collection of all channels available at this locale.
        /// Uses ChannelFactory to build collection
        /// </summary>
        public ConcurrentDictionary<uint, IChannelDetails> ItsChannelCollection { get; private set; }

        /// <summary>
        /// Collection of all AP radios build in this session
        /// </summary>
        public List<IApRadioDetails> ItsRadioCollection { get; private set; }

        /// <summary>
        /// MAC address of connected AP radio
        /// </summary>
        public IMacAddress ItsConnectedMacAddress { get; private set; }

        #endregion

        #region Constructor
        public WiFiCollectionsService(IEventAggregator eventAggregator, IVendorGenerator vendorGenerator)
        {
            _eventAggregator = eventAggregator;
            _vendorGenerator = vendorGenerator;

            _radioLock = new object();

            Initialize();
            HookEvents();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Returns the associated BSSID if the MAC address belongs to a known BSSID
        /// Returns null if no match
        /// </summary>
        /// <param name="macAddress"></param>
        /// <returns></returns>
        public IBssidDetails GetBssid(IMacAddress macAddress)
        {
            IBssidDetails bssidDetails;
            ItsBssidCollection.TryGetValue(macAddress.ItsUlongValue, out bssidDetails);
            return bssidDetails;
        }

        /// <summary>
        /// Returns the associated Client if the MAC address belongs to a known Client
        /// Returns null if no match AND addIfNotFound is false
        /// Returns new client if no match AND addIfNotFound is true
        /// </summary>
        /// <param name="macAddress"></param>
        /// <returns></returns>
        public IClientDetails GetClient(IMacAddress macAddress, bool addIfNotFound)
        {
            IClientDetails clientDetails;
            ItsClientCollection.TryGetValue(macAddress.ItsUlongValue, out clientDetails);
            if (clientDetails != null)
            {
                return clientDetails;
            }

            if (addIfNotFound)
            {
                var vendor = _vendorGenerator.GetVendor(macAddress);
                clientDetails = new ClientDetails(macAddress, vendor);
                ItsClientCollection.TryAdd(macAddress.ItsUlongValue, clientDetails);
                _eventAggregator.GetEvent<ClientAddedEvent>().Publish(clientDetails);
            }

            return clientDetails;
        }

        /// <summary>
        /// Returns the associated ChannelDetails 
        /// Returns null if no match
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public IChannelDetails GetChannel(uint channel)
        {
            IChannelDetails channelDetails;
            ItsChannelCollection.TryGetValue(channel, out channelDetails);
            return channelDetails;
        }

        public void AddRadio(IApRadioDetails radio)
        {
            lock (_radioLock)
            {
                if (!ItsRadioCollection.Contains(radio))
                {
                    ItsRadioCollection.Add(radio);
                }
            }
        }

        public List<IApRadioDetails> GetRadiosOnChannel(uint channel)
        {
            lock (_radioLock)
            {
                return ItsRadioCollection.Where(r => r.ItsChannelInfo.ItsPrimaryChannel == channel && r.ItsMaxRssi != null).ToList();
            }
        }

        /// <summary>
        /// Adds new BSSID to the collection, checks if it matches the connected MAC address
        /// </summary>
        /// <param name="bssid"></param>
        public void AddBssid(IBssidDetails bssid)
        {
            ItsBssidCollection.TryAdd(bssid.ItsMacAddress.ItsUlongValue, bssid);

            if (string.IsNullOrEmpty(bssid.ItsVendor))
            {
                bssid.ItsVendor = _vendorGenerator.GetVendor(bssid.ItsMacAddress);
            }

            if (bssid.ItsSsid != null)
            {
                AttachBssidToEssid(bssid);
            }

            // Remove BSSID from Clients table if there was a client with this MAC Address
            if (ItsClientCollection.ContainsKey(bssid.ItsMacAddress.ItsUlongValue))
            {
                IClientDetails outClient;
                ItsClientCollection.TryRemove(bssid.ItsMacAddress.ItsUlongValue, out outClient);
            }

            _eventAggregator.GetEvent<BssidAddedEvent>().Publish(bssid);
        }

        /// <summary>
        /// Attempts to provide a useful SSID to display for hidden networks that are attached to
        /// known networks.
        /// </summary>
        public void AttachHiddenSsidsToKnownSsids()
        {
            if (_hiddenBssids.Count == 0) return;

            List<ulong> removableFromHiddenNetworks = new List<ulong>();

            var hiddenBssids = _hiddenBssids.Values.ToList();
            foreach (var bssid in hiddenBssids)
            {
                if (bssid.ItsRadioGroup == null) continue;

                foreach (var bssidOnRadio in bssid.ItsRadioGroup.ItsBssidCollection.Values)
                {
                    // Is hidden on a radio with another SSID?
                    if (bssidOnRadio != bssid && !string.IsNullOrEmpty(bssidOnRadio.ItsSsid))
                    {
                        bssid.ItsDisplaySsid = $"{HIDDEN_SSID_NAME_PREFIX} {bssidOnRadio.ItsSsid}";
                        AttachBssidToEssid(bssid, OKAY_TO_SWITCH_ESSIDS, HIDDEN_ESSID);
                        removableFromHiddenNetworks.Add(bssid.ItsMacAddress.ItsUlongValue);
                        break;
                    }
                }
            }

            // Remove the bssid from hidden items if it has been attached to an essid
            IBssidDetails bssidDetails;
            foreach (var removableItem in removableFromHiddenNetworks)
            {
                _hiddenBssids.TryRemove(removableItem, out bssidDetails);
            }
        }

        /// <summary>
        /// Calls TrimTimeSegmentCollection for each BssidDetails in its collection
        /// Runs as an asynchronous task so that it doesn't block the calling thread
        /// </summary>
        /// <param name="trimDateTime"></param>
        public void TrimOldData(DateTime trimDateTime)
        {
            var bssids = ItsBssidCollection.Values;
            foreach (var bssid in bssids)
            {
                bssid.TrimTimeSegmentCollection(trimDateTime);
            }

            var essids = ItsEssidCollection.Values;
            foreach (var essid in essids)
            {
                essid.TrimTimeSegmentCollection(trimDateTime);
            }

            var clients = ItsClientCollection.Values;
            foreach (var client in clients)
            {
                client.TrimTimeSegmentCollection(trimDateTime);
            }

            var channels = ItsChannelCollection.Values;
            foreach (var channel in channels)
            {
                channel.TrimTimeSegmentCollection(trimDateTime);
            }
        }

        public void TrimOldCollection(DateTime trimDateTime)
        {
            foreach (var client in ItsClientCollection)
            {
                if (client.Value.ItsLastSeenDateTime < trimDateTime)
                {
                    ItsClientCollection.TryRemove(client.Key, out IClientDetails clientDetails);
                }
            }

            foreach (var bssid in ItsBssidCollection)
            {
                if (bssid.Value.ItsLastSeenDateTime < trimDateTime)
                {
                    ItsBssidCollection.TryRemove(bssid.Key, out IBssidDetails bssidDetails);
                }
                else
                {
                    foreach (var client in bssid.Value.ItsClientCollection)
                    {
                        if (client.Value.ItsLastSeenDateTime < trimDateTime)
                        {
                            bssid.Value.ItsClientCollection.TryRemove(client.Key, out IClientDetails clientDetail);
                        }
                    }
                }
            }

            foreach (var essid in ItsEssidCollection)
            {
                if (essid.Value.ItsLastSeenDateTime < trimDateTime)
                {
                    ItsEssidCollection.TryRemove(essid.Key, out IEssidDetails essidDetails);
                }
                else
                {
                    foreach (var bssid in essid.Value.ItsBssidCollection)
                    {
                        if (bssid.Value.ItsLastSeenDateTime < trimDateTime)
                        {
                            essid.Value.ItsBssidCollection.TryRemove(bssid.Key, out IBssidDetails bssidDetail);
                        }
                    }

                    foreach (var client in essid.Value.ItsClientCollection)
                    {
                        if (client.Value.ItsLastSeenDateTime < trimDateTime)
                        {
                            essid.Value.ItsClientCollection.TryRemove(client.Key, out IClientDetails clientDetail);
                        }
                    }
                }
            }

            lock (_radioLock)
            {
                foreach (var radio in ItsRadioCollection.ToList())
                {
                    foreach (var bssid in radio.ItsBssidCollection)
                    {
                        if (bssid.Value.ItsLastSeenDateTime < trimDateTime)
                        {
                            radio.ItsBssidCollection.TryRemove(bssid.Key, out IBssidDetails radioBssidDetails);
                        }
                    }

                    if (!radio.ItsBssidCollection.Any())
                    {
                        ItsRadioCollection.Remove(radio);
                    }
                }
            }

            UpdateAllChannelsDetails();
            _eventAggregator.GetEvent<WiFiCollectionsUpdatedEvent>().Publish(EventArgs.Empty);
        }

        public void UpdateWiFiCollectionOnTimeRangeChanged(DateTime startTime, DateTime endTime)
        {
            foreach (var client in ItsClientCollection.Values)
            {
                client.UpdateClientStatsBasedOnTimeRange(startTime, endTime);
            }

            foreach (var bssid in ItsBssidCollection.Values)
            {
                bssid.UpdateBroadcastClientBasedOnTimeRange(startTime, endTime);
                bssid.UpdateValuesOnTimeRangeChanged();
            }

            foreach (var essid in ItsEssidCollection.Values)
            {
                essid.UpdateValuesOnTimeRangeChanged();
            }

            foreach (var channel in ItsChannelCollection.Values)
            {
                channel.UpdateChannelStatsBasedOnTimeRange(startTime, endTime);
            }

            lock (_radioLock)
            {
                foreach (var radio in ItsRadioCollection)
                {
                    radio.UpdateRadio();
                }
            }

            UpdateAllChannelsDetails();

            _eventAggregator.GetEvent<WiFiCollectionsUpdatedOnTimeFrameChangedEvent>().Publish(EventArgs.Empty);
        }

        public void UpdateWiFiCollectionsOnTimeSpanChanged(TimeSpan timeSpan)
        {
            var clients = ItsClientCollection.Values;
            var bssids = ItsBssidCollection.Values;
            var essids = ItsEssidCollection.Values;
            var channels = ItsChannelCollection.Values;

            foreach (var client in clients)
            {
                client.UpdateSelectedTimeSpan(timeSpan);
            }

            foreach (var bssid in bssids)
            {
                bssid.UpdateBroadcastClientTimeSpan(timeSpan);
                bssid.UpdateValuesOnTimeRangeChanged();
            }

            foreach (var essid in essids)
            {
                essid.UpdateValuesOnTimeRangeChanged();
            }

            foreach (var channel in channels)
            {
                channel.UpdateSelectedTimeSpan(timeSpan);
            }

            lock (_radioLock)
            {
                foreach (var radio in ItsRadioCollection)
                {
                    radio.UpdateRadio();
                }
            }

            UpdateAllChannelsDetails();

            _eventAggregator.GetEvent<WiFiCollectionsUpdatedOnTimeFrameChangedEvent>().Publish(EventArgs.Empty);
        }

        private void UpdateAllChannelsDetails()
        {
            foreach (var channel in ItsChannelCollection.Keys)
            {
                var erpPresent = false;
                int maxRssi = WiFIConstants.RSSI_FLOOR;
                IBssidDetails loudestBssid = null;
                var maxAirTime = 0.0;
                IBssidDetails maxAirTimeBssid = null;

                ItsChannelCollection.TryGetValue(channel, out IChannelDetails channelDetails);
                var bssidsOnChannel = ItsBssidCollection.Values.Where(b => b.ItsChannelInfo.ItsPrimaryChannel == channel);

                foreach (var bssidDetails in bssidsOnChannel)
                {
                    bssidDetails.ItsChannelAirTimePercentage = channelDetails.ItsAirTimePercentage;

                    if (bssidDetails.ItsPhyTypeInfo.ItsNonErpPresentFlag)
                    {
                        erpPresent = true;
                    }

                    if (bssidDetails.ItsAirTimePercentage > maxAirTime)
                    {
                        maxAirTime = bssidDetails.ItsAirTimePercentage;
                        maxAirTimeBssid = bssidDetails;
                    }

                    if (bssidDetails.ItsRssi != null && bssidDetails.ItsRssi.Value > maxRssi)
                    {
                        maxRssi = bssidDetails.ItsRssi.Value;
                        loudestBssid = bssidDetails;
                    }
                }

                if (channelDetails != null)
                {
                    channelDetails.ItsNonErpPresentFlag = erpPresent;

                    if (maxRssi > WiFIConstants.RSSI_FLOOR)
                    {
                        channelDetails.ItsMaxRssi = maxRssi;
                        channelDetails.ItsLoudestBssid = loudestBssid;
                    }
                    else
                    {
                        channelDetails.ItsMaxRssi = null;
                        channelDetails.ItsLoudestBssid = null;
                    }

                    if (maxAirTime > 0)
                    {
                        channelDetails.ItsMaxAirTimePercentage = maxAirTime;
                        channelDetails.ItsMaxAirTimeBssid = maxAirTimeBssid;
                    }
                    else
                    {
                        channelDetails.ItsMaxAirTimePercentage = null;
                        channelDetails.ItsMaxAirTimeBssid = null;
                    }
                }
            }
        }

        public void AttachBssidToEssid(IBssidDetails bssid, bool okayToSwitchEssids = false, bool isHiddenEssid = false)
        {
            if (bssid.ItsEssid != null && !okayToSwitchEssids) return;

            // If this is a hidden network, attempt to attach it to a known SSID
            if (string.IsNullOrEmpty(bssid.ItsDisplaySsid))
            {
                if (_hiddenBssids.ContainsKey(bssid.ItsMacAddress.ItsUlongValue))
                    return;

                _hiddenBssids.TryAdd(bssid.ItsMacAddress.ItsUlongValue, bssid);
                bssid.ItsDisplaySsid = $"{HIDDEN_SSID_NAME_PREFIX} {bssid.ItsMacAddress}";
                isHiddenEssid = true;
            }

            IEssidDetails essid;
            var ssid = bssid.ItsDisplaySsid;

            if (!ItsEssidCollection.TryGetValue(ssid, out essid))
            {
                essid = new EssidDetails(ssid);
                ItsEssidCollection.TryAdd(ssid, essid);
            }

            essid.AddBssid(bssid);
            essid.ItsHiddenFlag = isHiddenEssid;

            // Remove from previous ESSID before attaching to new ESSID
            if (okayToSwitchEssids && bssid.ItsEssid != null)
            {
                var formerEssid = bssid.ItsEssid;
                IBssidDetails removedBssid;
                formerEssid.ItsBssidCollection.TryRemove(bssid.ItsMacAddress.ItsUlongValue, out removedBssid);

                // If the previous essid is hidden, remove it from the essid collection
                if (formerEssid.ItsHiddenFlag)
                {
                    IEssidDetails removedEssid;
                    ItsEssidCollection.TryRemove(bssid.ItsEssid.ItsSsid, out removedEssid);
                }
            }

            bssid.ItsEssid = essid;

            if (Equals(bssid.ItsMacAddress, ItsConnectedMacAddress))
            {
                bssid.ItsConnectedFlag = true;

                // Reset ItsConnectedMacAddress and fire the event again now that we have the BSSID
                ItsConnectedMacAddress = null;
                UpdateConnectedMacAddress(bssid.ItsMacAddress);
            }
        }


        #endregion

        #region Private Methods
        private void HookEvents()
        {
            _eventAggregator.GetEvent<ClearDataRequestEvent>().Subscribe(ClearDataRequestEventHandler);
            _eventAggregator.GetEvent<ConnectedMacAddressEvent>().Subscribe(ConnectedMacAddressEventHandler);
            _eventAggregator.GetEvent<WiFiConnectionChangedEvent>().Subscribe(WiFiConnectionChangedEventHandler);
        }

        private void Initialize()
        {
            ItsBssidCollection = new ConcurrentDictionary<ulong, IBssidDetails>();
            ItsEssidCollection = new ConcurrentDictionary<string, IEssidDetails>();
            ItsClientCollection = new ConcurrentDictionary<ulong, IClientDetails>();
            ItsRadioCollection = new List<IApRadioDetails>();
            _hiddenBssids = new ConcurrentDictionary<ulong, IBssidDetails>();

            ItsChannelCollection = new ConcurrentDictionary<uint, IChannelDetails>();
            BuildChannelCollection();

            Task.Run(() => { _vendorGenerator.Initialize(); });
        }

        private void BuildChannelCollection()
        {
            var validChannels = WiFiChannelFactory.GetChannelsByBand(ChannelBand.Both);
            foreach (var channel in validChannels)
            {
                var channelDetails = new ChannelDetails(channel.ItsChannelNumber, channel.ItsCenterFreqMhz, channel.ItsChannelWidthMhz);
                ItsChannelCollection.TryAdd(channelDetails.ItsChannelNumber, channelDetails);
            }
        }

        private void ClearDataRequestEventHandler(EventArgs eventArgs)
        {
            ItsBssidCollection.Clear();
            ItsEssidCollection.Clear();
            ItsClientCollection.Clear();
            lock (_radioLock)
            {
                ItsRadioCollection.Clear();
            }

            ItsChannelCollection.Clear();
            BuildChannelCollection();

            // clear views
            _eventAggregator.GetEvent<ClearViewsRequestEvent>().Publish(EventArgs.Empty);
        }

        private void ConnectedMacAddressEventHandler(IMacAddress macAddress)
        {
            UpdateConnectedMacAddress(macAddress);
        }

        private void WiFiConnectionChangedEventHandler(ConnectionAttributes attributes)
        {
            UpdateConnectedMacAddress(attributes?.ItsConnectedMacAddress);
        }

        private void UpdateConnectedMacAddress(IMacAddress macAddress)
        {
            if (!Equals(ItsConnectedMacAddress, macAddress))
            {
                IBssidDetails previousConnectedBssid = null;
                IBssidDetails currrentConnectedBssid = null;

                if (ItsConnectedMacAddress != null && ItsBssidCollection.ContainsKey(ItsConnectedMacAddress.ItsUlongValue))
                {
                    previousConnectedBssid = ItsBssidCollection[ItsConnectedMacAddress.ItsUlongValue];
                    previousConnectedBssid.ItsConnectedFlag = false;
                }

                ItsConnectedMacAddress = macAddress;
                if (ItsConnectedMacAddress != null && ItsBssidCollection.ContainsKey(ItsConnectedMacAddress.ItsUlongValue))
                {
                    currrentConnectedBssid = ItsBssidCollection[ItsConnectedMacAddress.ItsUlongValue];
                    currrentConnectedBssid.ItsConnectedFlag = true;
                }
            }
        }

        #endregion
    }
}
