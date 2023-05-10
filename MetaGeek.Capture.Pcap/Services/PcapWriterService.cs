using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using MetaGeek.Capture.Pcap.Interfaces;
using MetaGeek.WiFi.Core.Enums;
using MetaGeek.WiFi.Core.Interfaces;
using MetaGeek.WiFi.Core.Events;
using MetaGeek.WiFi.Core.Models;
using MetaGeek.Tonic.Common.Events;
using MetaGeek.WiFi.Core.Helpers;
using MetaGeek.WiFi.Core.Services;
using System.Diagnostics;
using MetaGeek.Tonic.Common.Resources;
using System.Linq;
using MetaGeek.Tonic.Common.Interfaces;
using Prism.Events;

namespace MetaGeek.Capture.Pcap.Services
{
    /// <summary>
    /// Exports two types of pcap files - summary files containing:
    /// Beacon for each known BSSID
    /// Association Request for each client that had an Association Request
    /// Full pcap containing ALL packets in a buffer
    /// </summary>
    public class PcapWriterService : IPcapWriterService, IDisposable
    {
        #region Fields

        private const ushort PPI_HEADER_LENGTH = 32;
        private const ushort RADIOTAP_SHORT_HEADER_LENGTH = 24;
        private const ushort RADIOTAP_LONG_HEADER_LENGTH = 36;
        private const ushort RADIOTAP_VERSION = 0;
        private const byte FLAGS_NO_FCS = 0x0;

        private const int ENHANCED_BLOCK_HEADER_LENGTH = 32;
        private const uint INTERFACE_DESC_BLOCK_LENGTH = 20;
        private const int OPTION_TYPE_IF_NAME = 2;
        private const uint CUSTOM_BLOCK_TYPE = 0x00000BAD;
        private const uint METAGEEK_PRIVATE_ENT_NUMBER = 57862;
        private const uint METAGEEK_BLOCK_TYPE_CHANNEL_SCAN = 0;
        private const uint CHANNEL_SCAN_INFO_BLOCK_BASIC_LENGTH = 44;

        private const int BEACON_FIXED_PARAMETERS_LENGTH = 12;
        private const int ASSOC_REQ_FIXED_PARAMETERS_LENGTH = 4;
        private const int MANAGEMENT_FRAME_MAC_HEADER_EXCLUDING_HT_CONTROL_LENGTH = 24;
        private const uint BEACON_TYPE_CONTROL_DURATION = 0x00000080;
        private const uint ASSOC_REQ_TYPE_CONTROL_DURATION = 0x00000000;

        private DateTime _epochTime;
        private IEventAggregator _eventAggregator;
        private readonly IPacketListService _packetListService;
        private readonly IPcapBinaryWriterProvider _pcapBinaryWriterProvider;
        private PacketMetaDataProcessor _packetMetaDataProcessor;
        private IMacAddress _broadcastMacAddress;
        private IBinaryWriter _openFileBinaryWriter;
        private bool _disposed = false;
        private SortedList<int, AdapterInfo> _liveModeAdapterList;
        private SortedList<int, AdapterInfo> _fileModeAdapterList;
        private bool _isLiveData;

        #endregion

        #region Properties

        public string ItsPacketCaptureFolder { get; set; }

        #endregion

        #region Constructors

        public PcapWriterService(IEventAggregator eventAggregator, PacketMetaDataProcessor packetProcessor,
            IPacketListService packetListService, IPcapBinaryWriterProvider pcapBinaryWriterProvider)
        {
            _eventAggregator = eventAggregator;
            _packetMetaDataProcessor = packetProcessor;
            _packetListService = packetListService;
            _pcapBinaryWriterProvider = pcapBinaryWriterProvider;

            _epochTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            _broadcastMacAddress = MacAddressCollection.Broadcast();
            _liveModeAdapterList = new SortedList<int, AdapterInfo>();
            _fileModeAdapterList = new SortedList<int, AdapterInfo>();
            _isLiveData = true;

            HookEvents();
        }

        #endregion

        #region Methods

        private void HookEvents()
        {
            _eventAggregator.GetEvent<AdapterInfoListUpdatedEvent>().Subscribe(AdapterInfoListUpdatedEventHandler);
            _eventAggregator.GetEvent<LiveStatusChangedEvent>().Subscribe(LiveStatusChangedEventHandler);
        }

        private void LiveStatusChangedEventHandler(bool isLiveData)
        {
            _isLiveData = isLiveData;
        }

        public string CreateCaptureFilename(string description)
        {
            var now = DateTime.UtcNow;
            var day = now.ToShortDateString().Replace("/", ".");
            var time = now.ToLongTimeString().Replace(":", ".").Replace(" ", "_");
            var path = Path.Combine(ItsPacketCaptureFolder, description);
            const string ext = ".pcap";
            return string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}{3}", path, day, time, ext);
        }

        public string GetSavedPcapFolderPath()
        {
            return CaptureMagicStrings.CaptureFilePath.SavedPcapFolderPath;
        }

        public void WriteAllPacketsToCaptureFile(string fileName, bool anonymizeFlag = false, bool isPcapng = true)
        {
            var packets = _packetListService.GetAllPackets();
            WritePacketCaptureFile(packets, fileName, anonymizeFlag, isPcapng);
        }

        /// <summary>
        /// Writes auto-capture file from triggered event
        /// </summary>
        /// <param name="packets"></param>
        /// <param name="fileName">The desired location of the created pcap file</param>
        /// <returns></returns>
        public bool WritePacketCaptureFile(PacketMetaData[] packets, string fileName, bool anonymizeFlag = false, bool isPcapng = true)
        {
            if (packets == null) return false;

            IBinaryWriter writer;
            try
            {
                writer = _pcapBinaryWriterProvider.GetBinaryWriter(fileName);

                if (isPcapng)
                {
                    WritePcapngFileHeader(writer);
                }
                else
                {
                    WritePcapFileHeader(writer);
                }

                var totalCount = (double)packets.Length;
                var counter = 0;

                ChannelScanInfo currentChannelScanInfo = null;

                foreach (var packet in packets)
                {
                    var currentPacket = packet;

                    if (anonymizeFlag)
                    {
                        // Create a copy of existing packet
                        currentPacket = packet.Clone();

                        var anonymizedPacketBytes = _packetMetaDataProcessor.AnonymizePacketBytes(currentPacket);

                        // Update length of the copy packet based on anonymized data
                        currentPacket.ItsLength = anonymizedPacketBytes.Length;
                        currentPacket.ItsOriginalLength = anonymizedPacketBytes.Length;
                        currentPacket.ItsPacketBytes = anonymizedPacketBytes;
                    }

                    if (isPcapng && currentPacket.ItsChannelScanInfo != currentChannelScanInfo)
                    {
                        currentChannelScanInfo = currentPacket.ItsChannelScanInfo;
                        WriteChannelScanInfoBlock(writer, currentChannelScanInfo);
                    }

                    var headerLength = GetRadioTapHeaderLength(currentPacket);
                    var blockLength = Get32BitAlignedLength(ENHANCED_BLOCK_HEADER_LENGTH + headerLength + currentPacket.ItsLength);

                    if (isPcapng)
                    {
                        WriteEnhancedPacketBlock(writer, currentPacket, headerLength, blockLength);
                    }
                    else
                    {
                        WriteRecordHeader(writer, packet.ItsDateTime, packet.ItsLength, packet.ItsOriginalLength, headerLength);
                    }

                    WriteRadioTapHeader(writer, currentPacket);
                    WritePacketBytes(writer, currentPacket, isPcapng);

                    if (isPcapng)
                    {
                        WriteEnhancedPacketClosingLength(writer, blockLength);
                    }

                    if (++counter % 1000 == 0)
                    {
                        _eventAggregator.GetEvent<PcapSaveProgressEvent>().Publish(counter / totalCount);
                    }
                }

                writer.Close();
            }
            catch (System.IO.IOException)
            {
                // Unable to create file...
                return false;
            }

            // Clear the progress bar when file is complete.
            _eventAggregator.GetEvent<PcapSaveProgressEvent>().Publish(0);

            return true;
        }

        public bool StartPacketCaptureFile(string fileName)
        {
            try
            {
                ClosePacketCaptureFile();
                _openFileBinaryWriter = _pcapBinaryWriterProvider.GetBinaryWriter(fileName);
                WritePcapngFileHeader(_openFileBinaryWriter);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return true;
        }

        public bool AppendPacketCaptureFile(PacketMetaData[] packets)
        {
            if (packets == null || _openFileBinaryWriter == null) return false;

            try
            {
                ChannelScanInfo currentChannelScanInfo = null;

                foreach (var packet in packets)
                {
                    if (packet.ItsChannelScanInfo != currentChannelScanInfo)
                    {
                        currentChannelScanInfo = packet.ItsChannelScanInfo;
                        WriteChannelScanInfoBlock(_openFileBinaryWriter, currentChannelScanInfo);
                    }

                    var headerLength = GetRadioTapHeaderLength(packet);
                    var blockLength = Get32BitAlignedLength(ENHANCED_BLOCK_HEADER_LENGTH + headerLength + packet.ItsLength);

                    WriteEnhancedPacketBlock(_openFileBinaryWriter, packet, headerLength, blockLength);
                    WriteRadioTapHeader(_openFileBinaryWriter, packet);
                    WritePacketBytes(_openFileBinaryWriter, packet);
                    WriteEnhancedPacketClosingLength(_openFileBinaryWriter, blockLength);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Unable to write packets to current file. {0}", ex.Message);
                return false;
            }

            return true;
        }

        public bool ClosePacketCaptureFile()
        {
            if (_openFileBinaryWriter == null) return false;

            _openFileBinaryWriter.Close();
            _openFileBinaryWriter.Dispose();
            _openFileBinaryWriter = null;

            return true;
        }

        private void WritePacketBytes(IBinaryWriter writer, PacketMetaData packet, bool isPcapng = true)
        {
            writer.Write(packet.ItsPacketBytes);

            if (isPcapng)
            {
                AddPaddingFor32BitBoundary(writer, packet.ItsPacketBytes.Length);
            }
        }

        private int GetRadioTapHeaderLength(PacketMetaData packet)
        {
            return packet.ItsMcsIndex > 0 ? RADIOTAP_LONG_HEADER_LENGTH : RADIOTAP_SHORT_HEADER_LENGTH;
        }

        private void WriteRadioTapHeader(IBinaryWriter writer, PacketMetaData packet)
        {
            if (packet.ItsMcsIndex > 0)
            {
                WriteAcRadioTapHeader(writer, packet);
            }
            else
            {
                WriteLegacyRadioTapHeader(writer, packet.ItsDateTime, packet.ItsChannel, packet.ItsSignal, packet.ItsNoise, packet.ItsRate);
            }
        }

        private void WriteAcRadioTapHeader(IBinaryWriter writer, PacketMetaData packet)
        {
            const uint FIELDS_PRESENT = 0x0020006B;
            const ushort KNOWN_VHT_INFO = 0x0044;
            //header to radiotap
            writer.Write(RADIOTAP_VERSION);
            writer.Write(RADIOTAP_LONG_HEADER_LENGTH);
            writer.Write(FIELDS_PRESENT);

            // timestamp
            WriteTimestamp(writer, packet.ItsDateTime);

            // flags
            // TODO MAY NEED TO WRITE Short/Long GI BIT!
            writer.Write(FLAGS_NO_FCS);
            writer.Write((byte)0);   // padding for natural alignment

            // channel
            WriteRadioTapChannel(writer, packet.ItsChannel, packet.ItsRate);

            // antenna signal
            writer.Write((sbyte)packet.ItsSignal);

            // antenna noise
            writer.Write((sbyte)packet.ItsNoise);

            // VHT information
            writer.Write(KNOWN_VHT_INFO);

            var flags = packet.ItsShortGuardFlag ? 0x04 : 0x00;
            writer.Write((byte)flags);

            writer.Write(TranslateChannelToBandwidthCode(packet.ItsChannel, packet.ItsChannelWidth));

            var mcsNss = packet.ItsMcsIndex << 4 | packet.ItsSpatialStreams;
            writer.Write((byte)mcsNss);
            // Write 0's for: mcsNss 1-3
            writer.Write((byte)0);
            writer.Write((ushort)0);
            // write 0's for coding, group ID, partial AID
            writer.Write((uint)0);
        }

        /// <summary>
        /// Writes short version of RadioTap header for 802.11a/b/g
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="timestamp"></param>
        /// <param name="channel"></param>
        /// <param name="rssi"></param>
        /// <param name="noise"></param>
        /// <param name="rateMbps"></param>
        public void WriteLegacyRadioTapHeader(IBinaryWriter writer, DateTime dateTime, uint channel, int rssi, int noise, double rateMbps = 0)
        {
            const uint FIELDS_PRESENT = 0x0000006F;

            //header to radiotap
            writer.Write(RADIOTAP_VERSION);
            writer.Write(RADIOTAP_SHORT_HEADER_LENGTH);
            writer.Write(FIELDS_PRESENT);

            // timestamp
            WriteTimestamp(writer, dateTime);

            // flags
            writer.Write(FLAGS_NO_FCS);

            // data rate
            var rate = rateMbps == 0 ? (channel < 36 ? 2 : 12) : (int)(rateMbps * 2);
            writer.Write((byte)rate);

            // channel
            WriteRadioTapChannel(writer, channel, rateMbps);

            // antenna signal
            writer.Write((sbyte)rssi);

            // antenna noise
            writer.Write((sbyte)noise);
        }

        private void WriteRadioTapChannel(IBinaryWriter writer, uint channel, double rateMbps)
        {
            const ushort FIVE_GHZ_CH_FLAGS = 0x0140;

            writer.Write(DetermineChannelFrequency(channel));
            if (channel <= 14)
            {
                // check for CCK
                var twoGigChannelFlags = 0x00c0; // Preset to OFDM
                switch (rateMbps)
                {
                    case 2:
                    case 4:
                    case 11:
                    case 22:
                        twoGigChannelFlags = 0x0080;
                        break;
                }

                writer.Write((ushort)twoGigChannelFlags);
            }
            else
            {
                writer.Write(FIVE_GHZ_CH_FLAGS);
            }
        }

        /// <summary>
        /// https://www.radiotap.org/fields/VHT.html
        /// We don't have enough information to know if the packet was a 20 MHz packet
        /// just sent on the primary channel - unless we dig into the connection context of the packet.
        /// </summary>
        /// <param name="primary"></param>
        /// <param name="channelWidth"></param>
        /// <returns></returns>
        private byte TranslateChannelToBandwidthCode(uint primary, ChannelWidth channelWidth)
        {
            switch (channelWidth)
            {
                case ChannelWidth.Twenty:
                    return 0;

                case ChannelWidth.Forty:
                    return 1;

                case ChannelWidth.Eighty:
                    return 4;

                case ChannelWidth.OneSixty:
                case ChannelWidth.EightyPlusEighty:
                    return 11;

                default:
                    return 0;
            }
        }

        private void WriteTimestamp(IBinaryWriter writer, DateTime dateTime)
        {
            var timeSpan = dateTime - _epochTime;
            var seconds = (uint)Math.Floor(timeSpan.TotalSeconds);
            var microseconds = (uint)(timeSpan.Milliseconds * 1000);

            writer.Write(seconds);
            writer.Write(microseconds);
        }

        private void Write64BitTimestamp(IBinaryWriter writer, DateTime dateTime)
        {
            var timeSpan = dateTime - _epochTime;
            var microSeconds = (ulong)(timeSpan.TotalMilliseconds * 1000);
            var upper = (uint)(microSeconds >> 32);
            var lower = (uint)(microSeconds & 0xFFFFFFFF);
            writer.Write(upper);
            writer.Write(lower);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="timeSpan"></param>
        private void WriteTimeSpan(IBinaryWriter writer, TimeSpan timeSpan)
        {
            var microSeconds = (ulong)(timeSpan.TotalMilliseconds * 1000);
            writer.Write((uint)(microSeconds & 0xFFFFFFFF));
        }

        private void WritePcapngFileHeader(IBinaryWriter writer)
        {
            WriteSectionHeaderBlock(writer);

            WriteInterfaceDescriptionBlocks(writer);
        }

        private void WritePcapFileHeader(IBinaryWriter binaryWriter)
        {
            // Sources: https://wiki.wireshark.org/Development/LibpcapFileFormat
            byte[] header = {
                0xd4, 0xc3, 0xb2, 0xa1, // magic number for pcap files
                0x02, 0x00, 0x04, 0x00, // major, minor version
                0x00, 0x00, 0x00, 0x00, // use gmt time
                0x00, 0x00, 0x00, 0x00, // accuracy of timestamps (use default)
                0x00, 0x00, 0x01, 0x00,  // max length of capture packets (65535)
                0x7F, 0x00, 0x00, 0x00  // data link type ppi = C0, Radiotap = 7F
            };

            binaryWriter.Write(header);
        }


        private void WriteRecordHeader(IBinaryWriter writer, DateTime dateTime, int includedLength, int originalLength, int headerLength)
        {
            // Record (Packet) Header (not the radiotap header)
            // timestamp seconds 
            // timestamp microseconds
            // number of octets of packet saved in file
            // actual length of packet
            WriteTimestamp(writer, dateTime);
            writer.Write((uint)(includedLength + headerLength));
            // don't write 0 to original length; Wireshark treats it as a malformed packet
            var adjustedOriginal = originalLength >= includedLength ? originalLength : includedLength;
            writer.Write((uint)(adjustedOriginal + headerLength));
        }

        /// <summary>
        /// pcapng section header block
        /// source: https://datatracker.ietf.org/doc/html/draft-tuexen-opsawg-pcapng
        /// </summary>
        private void WriteSectionHeaderBlock(IBinaryWriter writer)
        {
            writer.Write(0x0A0D0D0A);
            writer.Write(28); // section header block
            writer.Write(0x1A2B3C4D); // byte-order magic
            writer.Write((ushort)0x01); // major version
            writer.Write((ushort)0x00); // minor version
            writer.Write(0xFFFFFFFFFFFFFFFF);
            writer.Write(28); // section header block
        }

        /// <summary>
        /// Writing an interface description for each adapter discovered during this app session.
        /// They may not all have packets associated with them
        /// IT IS CRITICAL THAT ADAPTERS HAVE THE CORRECT INDEX IN THE LIST OF INTERFACE DESCRIPTORS
        /// </summary>
        /// <param name="writer"></param>
        private void WriteInterfaceDescriptionBlocks(IBinaryWriter writer)
        {
            var nextExpectedIndex = 0;
            var adapters = _isLiveData ? new List<AdapterInfo>(_liveModeAdapterList.Values) :
                           new List<AdapterInfo>(_fileModeAdapterList.Values);

            if (!adapters.Any())
            {
                WriteEmptyInterfaceDescriptionBlock();
                return;
            }

            foreach (var adapter in adapters)
            {
                // Fill in any missing interfaces, so that each interface is at the correct array index
                // in the list of Interface Description Blocks
                while (nextExpectedIndex < adapter.ItsDeviceIndex)
                {
                    WriteEmptyInterfaceDescriptionBlock();
                    nextExpectedIndex++;
                }

                WriteInterfaceDescriptionBlockForAdapter(adapter);
                nextExpectedIndex = adapter.ItsDeviceIndex + 1;
            }

            void WriteEmptyInterfaceDescriptionBlock()
            {
                writer.Write((uint)0x01);   // block type 
                writer.Write((uint)INTERFACE_DESC_BLOCK_LENGTH);     // block length with no options
                writer.Write((ushort)0x7F); // Link type radiotap
                writer.Write((ushort)0);    // reserved two bytes
                writer.Write((uint)0);      // no snap length limit
                writer.Write((uint)INTERFACE_DESC_BLOCK_LENGTH);     // block length
            }

            void WriteInterfaceDescriptionBlockForAdapter(AdapterInfo adapter)
            {
                var blockLength = INTERFACE_DESC_BLOCK_LENGTH + GetOptionLength(adapter.ItsName.Length);

                writer.Write((uint)0x01);   // block type 
                writer.Write((uint)blockLength);     // block length with no options
                writer.Write((ushort)0x7F); // Link type radiotap
                writer.Write((ushort)0);    // reserved two bytes
                writer.Write((uint)0);      // no snap length limit
                WriteOption(writer, OPTION_TYPE_IF_NAME, adapter.ItsName);
                writer.Write((uint)blockLength);     // block length
            }
        }

        private void WriteChannelScanInfoBlock(IBinaryWriter writer, ChannelScanInfo channelScanInfo)
        {
            if (channelScanInfo == null) return;

            var blockLength = CHANNEL_SCAN_INFO_BLOCK_BASIC_LENGTH + channelScanInfo.ItsChannelAdapterMap.Count * 4;
            writer.Write(CUSTOM_BLOCK_TYPE);
            writer.Write((uint)blockLength);
            writer.Write(METAGEEK_PRIVATE_ENT_NUMBER);
            writer.Write(METAGEEK_BLOCK_TYPE_CHANNEL_SCAN);
            Write64BitTimestamp(writer, channelScanInfo.ItsStartTime);
            WriteTimeSpan(writer, channelScanInfo.ItsTimeSpan);

            writer.Write((ulong)0x00); // Scanning state AND MAC Address placeholders

            writer.Write((uint)channelScanInfo.ItsChannelAdapterMap.Count);

            foreach (var adapterKVP in channelScanInfo.ItsChannelAdapterMap)
            {
                writer.Write((byte)adapterKVP.Value);   // Adapter ID
                var band = adapterKVP.Key <= 14 ? 0 : 1;
                writer.Write((byte)band);               // band
                writer.Write((ushort)adapterKVP.Key);   // channel
            }
            writer.Write((uint)blockLength);
        }

        private void WriteEnhancedPacketBlock(IBinaryWriter writer, PacketMetaData packet, int headerLength, int blockLength)
        {
            writer.Write(0x00000006);   // block type
            writer.Write((uint)blockLength);
            writer.Write((uint)packet.ItsAdapterId);      // interface ID

            Write64BitTimestamp(writer, packet.ItsDateTime);

            // Captured packet length
            writer.Write((uint)(packet.ItsLength + headerLength));
            // don't write 0 to original length; Wireshark treats it as a malformed packet
            var adjustedOriginal = packet.ItsOriginalLength >= packet.ItsLength ? packet.ItsOriginalLength : packet.ItsLength;
            // original packet length
            writer.Write((uint)(adjustedOriginal + headerLength));
        }

        /// <summary>
        /// Option Type is 2 bytes
        /// Option Length is 2 bytes
        /// Option Value must end on 32-bit bounary
        /// </summary>
        /// <param name="optionValueLength"></param>
        /// <returns></returns>
        private int GetOptionLength(int optionValueLength)
        {
            return 4 + Get32BitAlignedLength(optionValueLength);
        }

        private int Get32BitAlignedLength(int rawLength)
        {
            var mod = rawLength % 4;

            return mod == 0 ? rawLength : rawLength + (4 - mod);
        }

        private void AddPaddingFor32BitBoundary(IBinaryWriter writer, int currentLength)
        {
            // add padding for 32-bit boundary
            var mod = currentLength % 4;
            if (mod != 0)
            {
                writer.Seek(4 - mod, SeekOrigin.Current);
            }
        }

        private void WriteOption(IBinaryWriter writer, int optionType, string optionValue)
        {
            var chars = optionValue.ToCharArray();
            writer.Write((ushort)optionType);
            writer.Write((ushort)Get32BitAlignedLength(optionValue.Length));
            writer.Write(chars);
            AddPaddingFor32BitBoundary(writer, chars.Length);
        }

        private void WriteEnhancedPacketClosingLength(IBinaryWriter writer, int blockLength)
        {
            writer.Write((uint)blockLength);
        }

        private ushort DetermineChannelFrequency(uint channelNumber)
        {
            if (channelNumber < 14)
                return (ushort)(channelNumber * 5 + 2407);

            if (channelNumber == 14)
                return 2484;
            return (ushort)(channelNumber * 5 + 5000);
        }

        /// <summary>
        /// If an adapter is removed, PcapWriter still needs to remember it.
        /// So adapters are never removed from the list in PcapWriterService
        /// </summary>
        /// <param name="adapterInfoList"></param>
        private void AdapterInfoListUpdatedEventHandler(AdapterInfoList adapterInfoList)
        {
            var scannerTypes = (ScannerTypes)(adapterInfoList?.ItsAdapterType);

            if (scannerTypes == ScannerTypes.Pcap)
            {
                _fileModeAdapterList.Clear();
                adapterInfoList.ItsAdapters.Do(x => _fileModeAdapterList.Add(x.ItsDeviceIndex, x));
            }
            else if (scannerTypes == ScannerTypes.MonitorMode)
            {
                _liveModeAdapterList.Clear();
                adapterInfoList.ItsAdapters.Do(x => _liveModeAdapterList.Add(x.ItsDeviceIndex, x));
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                ClosePacketCaptureFile();
            }

            _disposed = true;
        }

        #endregion
    }
}
