using MetaGeek.Capture.Pcap.Enums;
using MetaGeek.Tonic.Common.Events; 
using MetaGeek.WiFi.Core;
using MetaGeek.WiFi.Core.Enums;
using MetaGeek.WiFi.Core.Events;
using MetaGeek.WiFi.Core.Interfaces;
using MetaGeek.WiFi.Core.Models;
using MetaGeek.WiFi.Core.Services;
using MetaGeek.Tonic.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Prism.Events;

namespace MetaGeek.Capture.Pcap.Services
{
    public class PcapReaderService : IPcapReaderService, IDisposable
    {
        #region Fields

        private const uint MAX_VALID_CHANNEL = 165;
        private const double MIN_VALID_RATE = 1.0;
        private const int MIN_RECORD_LENGTH = 20;
        private const int MAX_MSEC_TIMESEGMENT_TO_LOAD_PCAP = 500;

        private const uint PCAP_MAGIC_NUMBER = 0xA1B2C3D4;
        private const uint PCAP_MAGIC_NUMBER_REVERSE = 0xD4C3B2A1;
        private const uint PCAPNG_SECTION_HEADER_BLOCK_TYPE = 0x0A0D0D0A;
        private const uint PCAPNG_MAGIC_NUMBER = 0x1A2B3C4D;
        private const uint PCAPNG_MAGIC_REVERSED = 0x4D3C2B1A;
        private const uint TIMESTAMP_MS_RES = 6;

        private const uint PPI_LINK_TYPE = 0xC0;
        private const uint RADIOTAP_LINK_TYPE = 0x7F;
        private const uint ETHERNET_LINK_TYPE = 1;
        private const int PCAP_RECORD_HEADER_LENGTH = 16;
        private const int RECORD_HEADER_LENGTH_IN_FILE_INDEX = 8;
        private const uint METAGEEK_PRIVATE_ENT_NUMBER = 57862;
        private const uint METAGEEK_BLOCK_TYPE_CHANNEL_SCAN_INFO = 0;

        private const int PPI_PACKET_HEADER_LENGTH = 8;
        private const int PPI_LENGTH_INDEX = 2;
        private const int PPI_DATA_LINK_TYPE_INDEX = 4;
        private const int PPI_DATA_LINK_TYPE_802_11 = 105;
        private readonly TimeSpan SCAN_INFO_TIMESPAN = new TimeSpan(0, 0, 0, 0, 300);
        private IEventAggregator _eventAggregator;
        private IWiFiCollectionsService _wiFiCollections;
        private IAdapterRankProviderService _adapterRankProviderService;
        //private SnapshotReaderService _snapshotReaderService;

        private string _fileName;
        private static bool _endOfFileReached;
        private DateTime _packetStartTime;
        private DateTime _packetEndTime;

        private PacketMetaDataProcessor _packetProcessor;
        private PcapStreamReader _reader;

        private long _fileLength;
        private DataSourceInfo _dataSourceInfo;

        private Dictionary<uint, InterfaceDescription> _interfaceDescriptions;
        private List<AdapterInfo> _adapterInfos;

        private bool _reverseBytes;
        private Thread _pcapLoaderThread;

        private ChannelScanInfo _currentChannelScanInfo;

        #endregion

        #region Properties

        public string ItsFileName
        {
            get { return _fileName; }
            set { _fileName = value; }
        }

        public bool ItsInitializedFlag
        {
            get { return true; }
        }

        public ScannerTypes ItsScannerType
        {
            get { return ScannerTypes.Pcap; }
        }

        public DateTime ItsPacketStartTime
        {
            get { return _packetStartTime; }
        }

        public DateTime ItsPacketEndTime
        {
            get { return _packetEndTime; }
        }

        #endregion

        #region Constructor

        public PcapReaderService(IEventAggregator eventAggregator, IWiFiCollectionsService wiFiCollections, PacketMetaDataProcessor packetMetaDataProcessor,
            IAdapterRankProviderService adapterRankProviderService)
        {
            _eventAggregator = eventAggregator;
            _wiFiCollections = wiFiCollections;
            _packetProcessor = packetMetaDataProcessor;
            _adapterRankProviderService = adapterRankProviderService;

            Initialize();
        }

        #endregion

        #region Methods

        private void Initialize()
        {
        }

        public void SetDataSourceInfo(DataSourceInfo dataSourceInfo)
        {
            _dataSourceInfo = dataSourceInfo;
        }

        public void ReadFile(string fileName)
        {
            if (_pcapLoaderThread == null)
            {
                _pcapLoaderThread = new Thread(() => SelectFileReader(fileName));
            }

            _pcapLoaderThread.Start();
        }

        public void StopParsingFile()
        {
            StopPcapLoaderThread();
        }

        private void StopPcapLoaderThread()
        {
            if (_pcapLoaderThread != null)
            {
                if (!_pcapLoaderThread.Join(100))
                    _pcapLoaderThread.Abort();
                _pcapLoaderThread = null;
            }
        }

        private void SelectFileReader(string fileName)
        {
            try
            {
                Thread.Sleep(MAX_MSEC_TIMESEGMENT_TO_LOAD_PCAP);

                var fileExtension = Path.GetExtension(fileName).ToLower();
                if (fileExtension == ".rampart" || fileExtension == ".json")
                {
                    //_snapshotReaderService.OpenSnapshotFile(fileName);

                    //_eventAggregator.GetEvent<AllChannelsScanCompletedEvent>().Publish(new ScanCompletedEventData(_snapshotReaderService.ItsDateTime, ScannerTypes.Pcap, _dataSourceInfo));
                }
                else
                {
                    ParseCaptureFile(fileName);
                }
            }
            catch (ArgumentException ex)
            {
                Trace.TraceError("Exception while determining extension of file: {0}. {1}", fileName, ex.Message);
            }
            catch (NullReferenceException ex)
            {
                Trace.TraceError("Null Reference Exception while determining extension of file: {0}. {1}", fileName, ex.Message);
            }
            catch (ThreadInterruptedException x)
            {
                Trace.TraceWarning("{0}", x.Message);
            }
            catch (ThreadAbortException x)
            {
                Trace.TraceWarning("{0}", x.Message);
            }
        }

        // Making this public for now for testing purposes... 
        internal void ParseCaptureFile(string fileName)
        {
            if (_reader == null)
            {
                _reader = new Pcap.Services.PcapStreamReader();
            }

            var exists = _reader.OpenFile(fileName);

            if (!exists) return;

            _endOfFileReached = false;

            _packetStartTime = DateTime.MinValue;
            _packetEndTime = DateTime.MinValue;
            uint linkType = 0;
            PacketMetaData packet = null;

            var magicNumber = _reader.ReadUint();
            _reader.SkipBytes(-4); // reset before the magic number

            switch (magicNumber)
            {
                case PCAP_MAGIC_NUMBER:
                    _packetStartTime = ParsePcapOriginalFile(false);
                    break;

                case PCAP_MAGIC_NUMBER_REVERSE:
                    _packetStartTime = ParsePcapOriginalFile(true);
                    break;

                case PCAPNG_SECTION_HEADER_BLOCK_TYPE:
                    _packetStartTime = ParsePcapNextGenFile();
                    break;
            }

            _reader.CloseFile();

            _eventAggregator.GetEvent<PcapOpenProgressEvent>().Publish(1.0);
        }

        private DateTime ParsePcapOriginalFile(bool isLittleEndian)
        {
            var recordStartTime = DateTime.MinValue;
            var scanInfoStartTime = DateTime.MinValue;

            PacketMetaData packet = null;
            var channelAdapterMap = new SortedList<uint, int>();
            var packetCount = 0;
            var recordDateTime = DateTime.MinValue;
            uint originalLength = 0;

            var fileLength = _reader.ItsFileLength;

            // read pcap header
            var pcapHeader = _reader.ReadBytes(24);
            var linkType = ParseUInt32(isLittleEndian ,pcapHeader, 20);


            if (linkType != PPI_LINK_TYPE && linkType != RADIOTAP_LINK_TYPE && linkType != ETHERNET_LINK_TYPE)
            {
                // Throw exception??
                _eventAggregator.GetEvent<ScannerFailureEvent>()
                    .Publish($"Invalid Pcap Magic Number or Unknown Pcap Link Type of {linkType}");
                return recordStartTime;
            }

            //Publishing event with dummy adapter information as there exists no adapter information in the .pcap file
            _eventAggregator.GetEvent<AdapterInfoListUpdatedEvent>().Publish(new AdapterInfoList(ScannerTypes.Pcap, new List<AdapterInfo>() { new AdapterInfo(0, string.Empty, 0) }));

            try
            {
                while (!_endOfFileReached && _reader.ItsFilePosition + MIN_RECORD_LENGTH < fileLength)
                {
                    _reader.ResetByteCounter();
                    var packetLength = ReadPcapRecordHeader(isLittleEndian, out recordDateTime, out originalLength);

                    try
                    {
                        if (packetLength > 0)
                        {
                            packet = null;

                            switch (linkType)
                            {
                                case PPI_LINK_TYPE:
                                    packet = ReadPpiPacket(packetLength);
                                    break;

                                case RADIOTAP_LINK_TYPE:
                                    packet = ReadRadiotapPacket(packetLength);
                                    if (packet != null)
                                    {
                                        packet.ItsDateTime = recordDateTime;
                                    }

                                    break;

                                case ETHERNET_LINK_TYPE:
                                    packet = ReadEthernetPacket(packetLength);
                                    if (packet != null)
                                    {
                                        packet.ItsDateTime = recordDateTime;
                                    }

                                    break;

                                default:
                                    // TODO handle unrecognizable packet type by skipping it
                                    throw new ArgumentException(
                                        $"Unrecognizable link type of {linkType} at packet #{packetCount}");
                                    break;
                            }

                            if (packet != null && packet.ItsChannel <= MAX_VALID_CHANNEL)
                            {
                                // we save original length and packet length WITHOUT the radiotap header, which varies in size... 
                                // this removes the radiotap header length used in the packet from the originalLength
                                packet.ItsOriginalLength = (int)(originalLength - (packetLength - packet.ItsLength));

                                if (packet.ItsRate < MIN_VALID_RATE)
                                {
                                    packet.ItsRate = MIN_VALID_RATE;
                                }

                                if (packetCount == 0)
                                {
                                    recordStartTime = packet.ItsDateTime;
                                    scanInfoStartTime = packet.ItsDateTime;
                                }

                                _packetEndTime = packet.ItsDateTime;

                                _packetProcessor.ProcessPacket(packet);
                                var channel = packet.ItsChannel;
                                if (!channelAdapterMap.Keys.Contains(channel))
                                {
                                    // We don't have interface descriptions in pcap file, so just set everything to 0
                                    channelAdapterMap.Add(channel, 0);
                                }

                                packetCount++;

                                // TODO Delay until the correct time has past for a "real-time" packet reading...
                                // We only need to delay once per update event
                                if (packet.ItsDateTime - scanInfoStartTime > SCAN_INFO_TIMESPAN)
                                {
                                    UpdateStatus(channelAdapterMap, scanInfoStartTime, packet.ItsDateTime);
                                    scanInfoStartTime = packet.ItsDateTime;
                                }
                            }
                            else
                            {
                                _eventAggregator.GetEvent<IncompatibleFileLoadingEvent>().Publish(EventArgs.Empty);
                                StopPcapLoaderThread();
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // most problems within a single record can be skipped and the rest of the file is still okay
                        _reader.SkipToLocation(PCAP_RECORD_HEADER_LENGTH + packetLength);
                    }
                }
            }
            catch (ArgumentException ex)
            {
                _eventAggregator.GetEvent<ScannerFailureEvent>().Publish(ex.Message);
            }

            UpdateStatus(channelAdapterMap, scanInfoStartTime, packet.ItsDateTime);


            return recordStartTime;
        }

        private DateTime ParsePcapNextGenFile()
        {
            const uint INTERFACE_DESCRIPTION_BLOCK = 0x00000001;
            const uint ENHANCED_PACKET_BLOCK = 0x00000006;
            const uint CUSTOM_BLOCK = 0x00000BAD;

            var startTime = DateTime.MinValue;
            PacketMetaData packet = null;
            var packetCount = 0;
            var recordDateTime = DateTime.MinValue;
            var scanInfoStartTime = DateTime.MinValue;
            var channelAdapterMap = new SortedList<uint, int>();
            var customBlockHasScanInfo = false;
            _reverseBytes = false;

            var fileLength = _reader.ItsFileLength;
            _adapterInfos = new List<AdapterInfo>();

            try
            {
                while (!_endOfFileReached && _reader.ItsFilePosition + MIN_RECORD_LENGTH < fileLength)
                {
                    _reader.ResetByteCounter();
                    var blockType = _reader.ReadUint(_reverseBytes);

                    switch (blockType)
                    {
                        case PCAPNG_SECTION_HEADER_BLOCK_TYPE:
                            ReadSectionHeaderBlock();
                            break;

                        case INTERFACE_DESCRIPTION_BLOCK:
                            ReadInterfaceDescriptionBlock();
                            break;

                        case CUSTOM_BLOCK:
                            ReadCustomBlock(ref customBlockHasScanInfo);
                            break;

                        case ENHANCED_PACKET_BLOCK:
                            packet = ReadEnhancedPacketBlock();
                            if (packet != null && packet.ItsChannel <= MAX_VALID_CHANNEL)
                            {
                                if (packetCount == 0)
                                {
                                    startTime = packet.ItsDateTime;
                                    scanInfoStartTime = packet.ItsDateTime;
                                }

                                _packetEndTime = packet.ItsDateTime;

                                packetCount++;

                                if (!customBlockHasScanInfo)
                                {
                                    var channel = packet.ItsChannel;
                                    var currentChannelList = new List<uint>() { channel };

                                    if (!channelAdapterMap.Keys.Contains(channel))
                                    {
                                        channelAdapterMap.Add(channel, 0);
                                    }

                                    if (packet.ItsDateTime - scanInfoStartTime > SCAN_INFO_TIMESPAN)
                                    {
                                        UpdateStatus(channelAdapterMap, scanInfoStartTime, packet.ItsDateTime);
                                        scanInfoStartTime = packet.ItsDateTime;
                                    }
                                }

                                if (_currentChannelScanInfo != null)
                                {
                                    packet.ItsChannelScanInfo = _currentChannelScanInfo;
                                }

                                _packetProcessor.ProcessPacket(packet);
                            }
                            else
                            {
                                _eventAggregator.GetEvent<IncompatibleFileLoadingEvent>().Publish(EventArgs.Empty);
                                StopPcapLoaderThread();
                            }
                            break;

                        default:
                            break;
                    }
                }

                if (!customBlockHasScanInfo)
                {
                    UpdateStatus(channelAdapterMap, scanInfoStartTime, packet.ItsDateTime);
                }
                else
                {
                    // Publish last ChannelScanInfo
                    _eventAggregator.GetEvent<ChannelScanCompletedEvent>().Publish(_currentChannelScanInfo);
                    _currentChannelScanInfo = null;
                    _eventAggregator.GetEvent<PcapOpenProgressEvent>().Publish(1.0);
                }
                
            }
            catch (ArgumentException ex)
            {
                _eventAggregator.GetEvent<ScannerFailureEvent>().Publish(ex.Message);
            }

            return startTime;
        }

        private void UpdateStatus(SortedList<uint, int> channelAdapterMap, DateTime startTime, DateTime endTime)
        {
            var totalTime = endTime - startTime;
            var channelScanInfo = new ChannelScanInfo(channelAdapterMap, startTime, totalTime,
                ScanningState.ScanAllChannels, _dataSourceInfo);

            _eventAggregator.GetEvent<ChannelScanCompletedEvent>().Publish(channelScanInfo);
            _eventAggregator.GetEvent<PcapOpenProgressEvent>().Publish(_reader.ItsFilePositionPercentage);
        }

        private void ReadSectionHeaderBlock()
        {
            uint magicNumber;
            var blockLength = _reader.ReadUint();
            magicNumber = _reader.ReadUint();
            _reverseBytes = magicNumber == PCAPNG_MAGIC_REVERSED;

            var major = _reader.ReadUshort(_reverseBytes);
            var minor = _reader.ReadUshort(_reverseBytes);
            var sectionLength = (long) _reader.ReadUlong(_reverseBytes);
            var sectionLengthUnknown = sectionLength == -1;
            // skip optional fields
            _reader.SkipToLocation(blockLength - 4);
            var blockLengthRepeat = _reader.ReadUint(_reverseBytes);

            // clear interface descriptor dictionary
            _interfaceDescriptions = new Dictionary<uint, InterfaceDescription>();
            _adapterInfos = new List<AdapterInfo>();
        }

        private void ReadInterfaceDescriptionBlock()
        {
            const ushort TIME_RES_OPTION = 9;
            const ushort END_OPTION = 0;
            const ushort IF_NAME_OPTION = 2;
            const ushort IF_DESC_OPTION = 3;

            var blockLength = _reader.ReadUint(_reverseBytes);
            var linkType = _reader.ReadUshort(_reverseBytes);
            _reader.SkipBytes(2); // reserved
            var snapLength = _reader.ReadUint(_reverseBytes);
            var inDesc = new InterfaceDescription(linkType, snapLength);

            var index = _interfaceDescriptions.Count;
            _interfaceDescriptions.Add((uint)index, inDesc);

            var endOfOptions = false;
            while (!endOfOptions && _reader.ItsByteCounter < blockLength - 4)
            {
                var optionType = _reader.ReadUshort(_reverseBytes);
                var optionLength = _reader.ReadUshort(_reverseBytes);
                var padding = optionLength % 4 == 0 ? 0 : 4 - (optionLength % 4);

                switch (optionType)
                {
                    case TIME_RES_OPTION:
                        inDesc.ItsTimestampResolution = _reader.ReadByte();
                        break;

                    case IF_NAME_OPTION:
                        var nameBytes = _reader.ReadBytes(optionLength);
                        inDesc.ItsInterfaceName = Encoding.UTF8.GetString(nameBytes).TrimEnd('\0');
                        break;

                    case END_OPTION:
                        endOfOptions = true;
                        break;

                    default:
                        // read the length with 32-bit boundary
                        _reader.SkipBytes(optionLength + padding);
                        break;
                }
            }

            _adapterInfos.Add(new AdapterInfo(index, inDesc.ItsInterfaceName, 
                _adapterRankProviderService.GetAdapterRankBasedOnName(inDesc.ItsInterfaceName)));
            _adapterInfos.Sort();

            _eventAggregator.GetEvent<AdapterInfoListUpdatedEvent>().Publish(new AdapterInfoList(ScannerTypes.Pcap, _adapterInfos));

            // This probably isn't necessary and it helps verify we are at the right spot after
            // skipping options, etc.
            _reader.SkipToLocation(blockLength-4);
            var blockLengthRepeat = _reader.ReadUint(_reverseBytes);
        }

        /// <summary>
        /// The only custom block we care about is the ChannelScanInfoBlock
        /// </summary>
        private void ReadCustomBlock(ref bool customBlockHasScanInfo)
        {
            var blockLength = _reader.ReadUint(_reverseBytes);
            var pen = _reader.ReadUint(_reverseBytes);
            if (pen == METAGEEK_PRIVATE_ENT_NUMBER)
            {

                var metaGeekBlockType = _reader.ReadUint(_reverseBytes);

                if (metaGeekBlockType == METAGEEK_BLOCK_TYPE_CHANNEL_SCAN_INFO)
                {
                    customBlockHasScanInfo = true;
                    var timeHigh = _reader.ReadUint(_reverseBytes);
                    var timeLow = _reader.ReadUint(_reverseBytes);
                    var startTime = PacketHelpers.BuildDateWithTimeResolution(timeHigh, timeLow, PacketHelpers.MICROSECOND_RESOLUTION);

                    var microSeconds = _reader.ReadUint(_reverseBytes);
                    var scanTimeSpan = PacketHelpers.BuildTimeSpanFromMicroSeconds(microSeconds);

                    _reader.ReadUlong(_reverseBytes); // ignore Scanning state and MAC address for now


                    var adapterCount = _reader.ReadUint(_reverseBytes);
                    var channelAdapterMap = new SortedList<uint, int>();
                    var currentChannelList = new List<uint>();

                    for (int i = 0; i < adapterCount; i++)
                    {
                        var adapterId = _reader.ReadByte();
                        var band = _reader.ReadByte();
                        var channel = _reader.ReadUshort(_reverseBytes);
                        channelAdapterMap.Add(channel, adapterId);

                        currentChannelList.Add(channel);

                        // set the channel in the AdapterInfo
                        var adapterInfo = _adapterInfos.Find(a => a.ItsDeviceIndex == adapterId);
                        if (adapterInfo != null)
                        {
                            adapterInfo.ItsChannel = channel;
                        }
                    }

                    // TODO The ScanningState is NOT being saved in the pcapng file yet
                    var channelScaninfo = new ChannelScanInfo(channelAdapterMap, startTime, scanTimeSpan, ScanningState.ScanAllChannels, _dataSourceInfo);

                    // The curent ChannelScanInfo is completed, so publish the event 
                    if (_currentChannelScanInfo != null)
                    {
                        _eventAggregator.GetEvent<ChannelScanCompletedEvent>().Publish(_currentChannelScanInfo);
                    }
                    _currentChannelScanInfo = channelScaninfo;

                    // This updates the activity dots on channels
                    // TODO - Publish this event when we need scanned channels over time while loading a file 
                    //_eventAggregator.GetEvent<CaptureChannelChangedEvent>().Publish(currentChannelList);
                    
                    _eventAggregator.GetEvent<PcapOpenProgressEvent>().Publish(_reader.ItsFilePositionPercentage);
                }
            }

            // skip to end of block
            _reader.SkipToLocation(blockLength - 4);
            var blockLengthRepeat = _reader.ReadUint(_reverseBytes);
        }

        private PacketMetaData ReadEnhancedPacketBlock()
        {
            var blockLength = _reader.ReadUint(_reverseBytes);
            var interfaceId = _reader.ReadUint(_reverseBytes);

            var timeHigh = _reader.ReadUint(_reverseBytes);
            var timeLow = _reader.ReadUint(_reverseBytes);

            var capturedLength = _reader.ReadUint(_reverseBytes);
            var originalLength = _reader.ReadUint(_reverseBytes);

            var inDesc = _interfaceDescriptions[interfaceId];
            var dateTime = PacketHelpers.BuildDateWithTimeResolution(timeHigh, timeLow, inDesc.ItsTimestampResolution);

            PacketMetaData packet = null;

            switch (inDesc.ItsLinkType)
            {
                case PPI_LINK_TYPE:
                    packet = ReadPpiPacket(capturedLength);
                    break;

                case RADIOTAP_LINK_TYPE:
                    packet = ReadRadiotapPacket(capturedLength);
                    break;

                case ETHERNET_LINK_TYPE:
                    packet = ReadEthernetPacket(capturedLength);
                    break;
            }

            if (packet != null)
            {
                // we save original length and packet length WITHOUT the radiotap header, which varies in size... 
                // this removes the radiotap header length used in the packet from the originalLength
                packet.ItsOriginalLength = (int)(originalLength - (capturedLength - packet.ItsLength)); 
                packet.ItsDateTime = dateTime;

                if (packet.ItsRate < MIN_VALID_RATE)
                {
                    packet.ItsRate = MIN_VALID_RATE;
                }
            }

            _reader.SkipToLocation(blockLength);
            return packet;
        }


        private uint ReadPcapRecordHeader(bool isLittleEndian, out DateTime dateTime, out uint originalLength)
        {
            dateTime = DateTime.MinValue;
            originalLength = 0;

            // pcap record header is: seconds, microseconds, length in file, original length
            var bytes = _reader.ReadBytes(PCAP_RECORD_HEADER_LENGTH);
            if (bytes.Length < PCAP_RECORD_HEADER_LENGTH)
            {
                _endOfFileReached = true;
                return 0;
            }

            var seconds = ParseUInt32(isLittleEndian, bytes, 0);
            var microSeconds = ParseUInt32(isLittleEndian, bytes, 4);
            var lengthInFile = ParseUInt32(isLittleEndian, bytes, RECORD_HEADER_LENGTH_IN_FILE_INDEX);
            originalLength = ParseUInt32(isLittleEndian, bytes, 12);
            dateTime = PacketHelpers.BuildDateTimeFromTsft(seconds, microSeconds);

            return lengthInFile;
        }

        private uint ParseUInt32(bool isLittleEndian, byte[] bytes, int index)
        {
            return isLittleEndian ? PcapStreamReader.GetLittleEndianUint(bytes, index) : BitConverter.ToUInt32(bytes, index);
        }

        private PacketMetaData ReadPpiPacket(uint packetLength)
        {
            if (packetLength < PPI_PACKET_HEADER_LENGTH)
            {
                // Throw exception
            }

            // READ PPI PACKET HEADER STRUCT
            var ppiPacketHeaderBytes = _reader.ReadBytes(PPI_PACKET_HEADER_LENGTH);

            var ppiHeaderTotalLength = BitConverter.ToUInt16(ppiPacketHeaderBytes, PPI_LENGTH_INDEX);
            var dataLinkType = BitConverter.ToUInt32(ppiPacketHeaderBytes, PPI_DATA_LINK_TYPE_INDEX);
            // dataLinkType must be 802.11
            if (dataLinkType != PPI_DATA_LINK_TYPE_802_11)
            {
                // throw exception
            }

            // READ REST OF PPI HEADER
            var ppiBytes = _reader.ReadBytes(ppiHeaderTotalLength - PPI_PACKET_HEADER_LENGTH);


            var packet = new PacketMetaData();

            var index = 0;
            while (index < ppiBytes.Length)
            {
                var type = BitConverter.ToUInt16(ppiBytes, index);
                var dataLength = BitConverter.ToUInt16(ppiBytes, index+2);
                index += 4;

                switch (type)
                {
                    case (ushort)PpiDataType.EightOTwoCommon:
                        ParseEightOTwoCommon(packet, ppiBytes, index);
                        break;
                }

                index += dataLength;
            }

            packet.ItsPacketBytes = _reader.ReadBytes((int)(packetLength - ppiHeaderTotalLength));
            packet.ItsLength = packet.ItsPacketBytes.Length;

            return packet;
        }

        private void ParseEightOTwoCommon(PacketMetaData packet, byte[] ppiHeaderBytes, int offset)
        {
            const ushort PPI_FLAGS_TSF_MSEC = 0x02;

            var seconds = BitConverter.ToUInt32(ppiHeaderBytes, offset);
            offset += 4;
            var microSeconds = BitConverter.ToUInt32(ppiHeaderBytes, offset);
            offset += 4;

            var flags = BitConverter.ToUInt16(ppiHeaderBytes, offset);
            offset += 2;

            var rate = BitConverter.ToUInt16(ppiHeaderBytes, offset);
            offset += 2;

            var channelFreq = BitConverter.ToUInt16(ppiHeaderBytes, offset);
            offset += 2;

            var channelFlags = BitConverter.ToUInt16(ppiHeaderBytes, offset);
            offset += 2;

            // skipping FHSS-Hopset and FHSS-Pattern
            offset += 2;
            var signal = (sbyte)ppiHeaderBytes[offset];
            offset += 1;
            var noise = (sbyte)ppiHeaderBytes[offset];

            if ((flags & PPI_FLAGS_TSF_MSEC) > 0)
            {
                microSeconds *= 1000;   // convert millisecond input to regular microsecond value
            }

            packet.ItsDateTime = PacketHelpers.BuildDateTimeFromTsft(seconds, microSeconds);

            packet.ItsSignal = (int)signal;
            packet.ItsNoise = (int)noise;
            packet.ItsRate = (int)rate / 2.0;
            packet.ItsChannel = DetermineChannelFromFrequency(channelFreq);
        }

        private uint DetermineChannelFromFrequency(ushort frequency)
        {
            if (frequency <= 165)
            {
                return frequency;
            }

            if (frequency < 3000)
            {
                return (uint)((frequency - 2407) / 5);
            }

            return (uint)((frequency - 5000) / 5);
        }

        private PacketMetaData ReadEthernetPacket(uint packetLength)
        {
            const uint PEEK_V2_MAGIC_NUMBER = 0xCDABFF00;
            const uint AIRMAGNET_MAGIC_NUMBER = 0x46435241;
            const uint PPI_MAGIC_NUMBER = 0x00000069;
            const uint MIN_80211_LENGTH = 10;
            const uint ETHERNET_LENGTH = 42;
            const int TYPE_0_HEADER_LENGTH = 16;
            const int TYPE_3_HEADER_LENGTH = 20;
            const uint AIRMAGNET_HEADER_LENGTH = 24;
            const uint ARUBA_PPI_HEADER_LENGTH = 16;
            const uint MIN_TOTAL_LENGTH = 68;
            const uint PEEK_V1_HEADER_LENGTH = 20;
            const uint IP_V4_VERSION = 0x08;
            const byte UDP_PROTOCOL = 0x11;
            const bool BIG_ENDIAN = true;

            var packet = new PacketMetaData();
            var ethernetStartByteCounter = (uint)_reader.ItsByteCounter;

            // packet is too short - bail out
            if (packetLength < MIN_TOTAL_LENGTH)
            {
                _reader.SkipToLocation(ethernetStartByteCounter + packetLength);
                return null;
            }

            _reader.SkipBytes(12); // jump to IP VERSION FIELD
            var ipVersion = _reader.ReadUshort();
            if (ipVersion != IP_V4_VERSION)
            {
                _reader.SkipToLocation(ethernetStartByteCounter + packetLength);
                return null;
            }

            _reader.SkipBytes(9); // Jump to Protocol Field
            var protocol = _reader.ReadByte();
            if (protocol != UDP_PROTOCOL)
            {
                _reader.SkipToLocation(ethernetStartByteCounter + packetLength);
                return null;
            }
            _reader.SkipBytes(18); // SKIP REST OF IP AND UDP HEADERS

            var peekedBytes = _reader.PeekBytes(24);
            var peekV2MagicNumber = BitConverter.ToUInt32(peekedBytes, 0);
            var ppiDlt = BitConverter.ToUInt32(peekedBytes, 20);

            var peekV1PacketLength = PcapStreamReader.GetLittleEndianUShort(peekedBytes, 2);
            var peekV1SliceLength = PcapStreamReader.GetLittleEndianUShort(peekedBytes, 4);
            var type0SliceLength = PcapStreamReader.GetLittleEndianUint(peekedBytes, 8);
            var type0PacketLength = PcapStreamReader.GetLittleEndianUint(peekedBytes, 12);

            // PEEK V2
            if (peekV2MagicNumber == PEEK_V2_MAGIC_NUMBER && peekedBytes[4] == 0x02)
            {
                _reader.SkipBytes(5); // already read the magic number and peek version
                var headerLength = _reader.ReadUint(BIG_ENDIAN);
                var peekType = _reader.ReadUint(BIG_ENDIAN);
                var mcs = _reader.ReadUshort(BIG_ENDIAN);
                packet.ItsChannel = _reader.ReadUshort(BIG_ENDIAN);
                var frequency = _reader.ReadUint(BIG_ENDIAN);
                var band = _reader.ReadUint(BIG_ENDIAN);
                var extendedFlags = _reader.ReadUint(BIG_ENDIAN);
                _reader.SkipBytes(2); // skipping signal % and noise %
                packet.ItsSignal = (sbyte)_reader.ReadByte();
                packet.ItsNoise = (sbyte) _reader.ReadByte();
                _reader.SkipBytes(8); // skipping signal 1-4 and noise 1-4
                var originalPacketLength = _reader.ReadUshort(BIG_ENDIAN);
                var sliceLength = _reader.ReadUshort(BIG_ENDIAN);
                var flags = _reader.ReadByte();
                var status = _reader.ReadByte();
                _reader.SkipBytes(8); // skipping TSFT timestamp

                var is11Ac = (extendedFlags & 0x80) > 0;
                packet.ItsChannelWidth = (extendedFlags & 0x200) > 0 ? ChannelWidth.Eighty :
                    (extendedFlags & 0x04) > 0 ? ChannelWidth.Forty : ChannelWidth.Twenty;
                packet.ItsShortGuardFlag = (extendedFlags & 0x08) > 0;
                packet.ItsSpatialStreams = (ushort)(((extendedFlags >> 14) & 0x07) + 1);

                if (!is11Ac)
                {
                    // convert .11n MCS index into .11ac style
                    packet.ItsMcsIndex = (ushort)(mcs % 8);
                    packet.ItsSpatialStreams = (ushort)((mcs / 8) + 1);
                }
                else
                {
                    packet.ItsMcsIndex = mcs;
                }
                packet.ItsRate = DataRateCalculator.DataRateFromMcsDetails(packet.ItsMcsIndex, packet.ItsSpatialStreams,
                    packet.ItsChannelWidth, packet.ItsShortGuardFlag);

                packet.ItsPacketBytes = _reader.ReadBytes(sliceLength);
                packet.ItsLength = packet.ItsPacketBytes.Length;
            }
            // AIRMAGNET PROTOCOL
            else if (peekV2MagicNumber == AIRMAGNET_MAGIC_NUMBER)
            {
                _reader.SkipBytes(14);
                var length = _reader.ReadUshort(BIG_ENDIAN);
                _reader.SkipBytes(1);
                packet.ItsChannel = _reader.ReadByte();
                var rate = _reader.ReadByte();
                packet.ItsRate = rate / 10.0;
                _reader.SkipBytes(5);

                packet.ItsPacketBytes = _reader.ReadBytes((int)length);
                packet.ItsLength = packet.ItsPacketBytes.Length;
            }
            else if (ppiDlt == PPI_MAGIC_NUMBER && peekedBytes[16] == 0)
            {
                _reader.SkipBytes(8); // ignoring TSFT timstamp
                var lengthWithHeader = _reader.ReadUint(BIG_ENDIAN);
                var lengthWithoutHeader = _reader.ReadUint(BIG_ENDIAN);

                packet = ReadPpiPacket(lengthWithoutHeader);
            }
            // PEEK V1
            else if (peekV1PacketLength == peekV1SliceLength &&
                     ETHERNET_LENGTH + PEEK_V1_HEADER_LENGTH + peekV1PacketLength <= packetLength)
            {
                var signal = _reader.ReadByte();
                packet.ItsSignal = signal >= 128 ? (sbyte) signal : signal * -1;
                var noise = _reader.ReadByte();
                packet.ItsNoise = noise >= 128 ? (sbyte) noise : noise * -1;
                packet.ItsLength = _reader.ReadUshort(BIG_ENDIAN);
                _reader.SkipBytes(2); // Slice Length
                var flags = _reader.ReadByte();
                var status = _reader.ReadByte();
                _reader.SkipBytes(8); // Skipping timestamp for now...  it isn't the actual timestamp
                //var seconds = ReadUint(REVERSE_ENDIANNESS);
                //var microSeconds = ReadUint(REVERSE_ENDIANNESS);
                //packet.ItsDateTime = PacketServices.BuildDateTimeFromTsft(seconds, microSeconds);
                packet.ItsRate = _reader.ReadByte() / 2.0;
                packet.ItsChannel = _reader.ReadByte();
                _reader.SkipBytes(2); // signal % and noise %

                packet.ItsPacketBytes = _reader.ReadBytes((int)(packetLength - ETHERNET_LENGTH - PEEK_V1_HEADER_LENGTH));
                packet.ItsLength = packet.ItsPacketBytes.Length;
            }
            // TYPE 0
            else if (type0PacketLength == type0SliceLength && ETHERNET_LENGTH + TYPE_0_HEADER_LENGTH + type0PacketLength == packetLength)
            {
                var type3RawRate = PcapStreamReader.GetLittleEndianUShort(peekedBytes, 16);
                var type3Rate = type3RawRate / 10.0;
                var type3Channel = peekedBytes[18];
                var type3Signal = peekedBytes[19];
                var type3ProtocolVersion = peekedBytes[20] & 0x03;
                
                // There is no explicit way to know it is type 3, so we do as much checking as possible
                if (type3Rate > 0 && type3Rate <= 1300 && type3Channel <= 165 && type3Signal <= 100 &&
                    type3ProtocolVersion == 0)
                {
                    packet.ItsChannel = type3Channel;
                    packet.ItsRate = type3Rate;
                    packet.ItsSignal = type3Signal;

                    _reader.SkipBytes(TYPE_3_HEADER_LENGTH);
                }
                else
                {
                    // type 0 does not specify channel or data rate, so just fill in defaults
                    packet.ItsChannel = 1;
                    packet.ItsRate = 1;

                    _reader.SkipBytes(TYPE_0_HEADER_LENGTH);
                }

                packet.ItsPacketBytes = _reader.ReadBytes((int)type0SliceLength);
                packet.ItsLength = packet.ItsPacketBytes.Length;
            }
            else
            {
                _reader.SkipToLocation(ethernetStartByteCounter + packetLength);
                return null;
            }

            return packet;
        }

        private PacketMetaData ReadRadiotapPacket(uint packetLength)
        {
            const int RADIOTAP_HEADER_LENGTH = 8;
            const int LENGTH_INDEX = 2;
            const int PRESENT_INDEX = 4;

            const uint RT_PRESENT_TSFT = 0x00000001;
            const uint RT_PRESENT_FLAGS = 0x00000002;
            const uint RT_PRESENT_RATE = 0x00000004;
            const uint RT_PRESENT_CHANNEL = 0x00000008;
            const uint RT_PRESENT_FHSS = 0x00000010;
            const uint RT_PRESENT_DBM_ANTENNA_SIGNAL = 0x00000020;
            const uint RT_PRESENT_DBM_ANTENNA_NOISE = 0x00000040;
            const uint RT_PRESENT_LOCK_QUALITY = 0x00000080;
            const uint RT_PRESENT_TX_ATTENUATION = 0x00000100;
            const uint RT_PRESENT_DB_TX_ATTENUATION = 0x00000200;
            const uint RT_PRESENT_DBM_TX_POWER = 0x00000400;
            const uint RT_PRESENT_ANTENNA = 0x00000800;
            const uint RT_PRESENT_DB_ANTENNA_SIGNAL = 0x00001000;
            const uint RT_PRESENT_DB_ANTENNA_NOISE = 0x00002000;
            const uint RT_PRESENT_RX_FLAGS = 0x00004000;
            // gap between defined fields
            const uint RT_PRESENT_CHANNEL_PLUS = 0x00040000;
            const uint RT_PRESENT_MCS_INFO = 0x00080000;
            const uint RT_PRESENT_A_MPDU_STATUS = 0x00100000;
            const uint RT_PRESENT_VHT_INFO = 0x00200000;
            const uint RT_PRESENT_FRAME_TIMESTAMP = 0x00400000;
            const uint RT_PRESENT_HE_INFO = 0x00800000;
            const uint RT_PRESENT_HE_MU_INFO = 0x01000000;
            const uint RT_PRESENT_ZERO_LENGTH_PSDU = 0x02000000;
            const uint RT_PRESENT_L_SIG = 0x04000000;
            // reserved
            const uint RT_PRESENT_RADIOTAP_NS_NEXT = 0x20000000;
            const uint RT_PRESENT_VENDOR_NS_NEXT = 0x40000000;
            const uint RT_PRESENT_ANOTHER_PRESENT_FIELD = 0x80000000;

            const byte FLAGS_SHORT_GI = 0x80;

           
            var packet = new PacketMetaData();
            Byte[] fieldBytes = null;

            var radioTapStartByteCounter = (uint)_reader.ItsByteCounter;
            _reader.StartNaturalAlignment();

            var radioTapVersion = _reader.ReadByte();
            _reader.ReadByte(); // pad
            fieldBytes = _reader.ReadNaturallyAlignedFieldBytes(2);
            var radioTapHeaderLength = BitConverter.ToUInt16(fieldBytes, 0);

            fieldBytes = _reader.ReadNaturallyAlignedFieldBytes(4);
            var presentFlags = BitConverter.ToUInt32(fieldBytes, 0);
            
            // read additional present fields before reading fields from original present field
            if ((presentFlags & RT_PRESENT_ANOTHER_PRESENT_FIELD) > 0)
            {
                while (true)
                {
                    fieldBytes = _reader.ReadNaturallyAlignedFieldBytes(4);
                    var additionalPresentFlags = BitConverter.ToUInt32(fieldBytes, 0);
                    if ((additionalPresentFlags & RT_PRESENT_ANOTHER_PRESENT_FIELD) == 0)
                    {
                        break;
                    }
                }
            }
            
            if ((presentFlags & RT_PRESENT_TSFT) > 0)
            {
                fieldBytes = _reader.ReadNaturallyAlignedFieldBytes(8);
                var seconds = BitConverter.ToUInt32(fieldBytes, 0);
                var microSeconds = BitConverter.ToUInt32(fieldBytes, 4);
                //packet.ItsUSecSinceEpoch = (seconds * 1000000) + microSeconds;
                packet.ItsDateTime = PacketHelpers.BuildDateTimeFromTsft(seconds, microSeconds);
            }

            if ((presentFlags & RT_PRESENT_FLAGS) > 0)
            {
                var flags = _reader.ReadByte();
                packet.ItsShortGuardFlag = (flags & FLAGS_SHORT_GI) > 0;
            }

            if ((presentFlags & RT_PRESENT_RATE) > 0)
            {
                var rate = _reader.ReadByte();
                packet.ItsRate = rate / 2f;
            }

            if ((presentFlags & RT_PRESENT_CHANNEL) > 0)
            {
                // channel frequency field
                fieldBytes = _reader.ReadNaturallyAlignedFieldBytes(2);
                packet.ItsChannel = DetermineChannelFromFrequency(BitConverter.ToUInt16(fieldBytes, 0));
                // channel flags field
                fieldBytes = _reader.ReadNaturallyAlignedFieldBytes(2);
            }

            if ((presentFlags & RT_PRESENT_FHSS) > 0)
            {
                _reader.ReadByte(); // hop set
                _reader.ReadByte(); // hop pattern
            }

            if ((presentFlags & RT_PRESENT_DBM_ANTENNA_SIGNAL) > 0)
            {
                packet.ItsSignal = (sbyte)_reader.ReadByte();
            }

            if ((presentFlags & RT_PRESENT_DBM_ANTENNA_NOISE) > 0)
            {
                packet.ItsNoise = (sbyte)_reader.ReadByte();
            }

            if ((presentFlags & RT_PRESENT_LOCK_QUALITY) > 0)
            {
                // has to do with barker code lock... very legacy
                _reader.ReadNaturallyAlignedFieldBytes(2);
            }

            if ((presentFlags & RT_PRESENT_TX_ATTENUATION) > 0)
            {
                _reader.ReadNaturallyAlignedFieldBytes(2);
            }

            if ((presentFlags & RT_PRESENT_DB_TX_ATTENUATION) > 0)
            {
                _reader.ReadNaturallyAlignedFieldBytes(2);
            }

            if ((presentFlags & RT_PRESENT_DBM_TX_POWER) > 0)
            {
                _reader.ReadByte();
            }

            if ((presentFlags & RT_PRESENT_ANTENNA) > 0)
            {
                _reader.ReadByte();
            }

            if ((presentFlags & RT_PRESENT_DB_ANTENNA_SIGNAL) > 0)
            {
                _reader.ReadByte();
            }

            if ((presentFlags & RT_PRESENT_DB_ANTENNA_NOISE) > 0)
            {
                _reader.ReadByte();
            }

            if ((presentFlags & RT_PRESENT_RX_FLAGS) > 0)
            {
                _reader.ReadNaturallyAlignedFieldBytes(2);
            }

            if ((presentFlags & RT_PRESENT_CHANNEL_PLUS) > 0)
            {
                _reader.ReadNaturallyAlignedFieldBytes(4);
                _reader.ReadNaturallyAlignedFieldBytes(2);
                _reader.ReadByte();
                _reader.ReadByte();
            }

            if ((presentFlags & RT_PRESENT_MCS_INFO) > 0)
            {
                const byte KNOWN_FLAG_BANDWIDTH = 0x01;
                const byte KNOWN_FLAG_MCS = 0x01;
                const byte KNOWN_FLAG_GI = 0x04;

                const byte FLAGS_BANDWIDTH_MASK = 0x03;
                const byte FLAGS_GI_MASK = 0x04;

                var known = _reader.ReadByte();
                var flags = _reader.ReadByte();
                var mcs = _reader.ReadByte();

                if ((known & KNOWN_FLAG_BANDWIDTH) > 0)
                {
                    packet.ItsChannelWidth = (flags & FLAGS_BANDWIDTH_MASK) == 1 ? ChannelWidth.Forty : ChannelWidth.Twenty;
                }

                if ((known & KNOWN_FLAG_GI) > 0)
                {
                    packet.ItsShortGuardFlag = (flags & FLAGS_GI_MASK) > 0;
                }

                // .11n style MCS Index that is 0-31 and also indicates number of spatial streams
                if((known & KNOWN_FLAG_MCS) > 0)
                {
                    packet.ItsMcsIndex = (ushort)(mcs & 0x07);
                    packet.ItsSpatialStreams = (ushort)((mcs / 8) + 1);
                }
            }

            if ((presentFlags & RT_PRESENT_A_MPDU_STATUS) > 0)
            {
                _reader.ReadNaturallyAlignedFieldBytes(4);
                _reader.ReadNaturallyAlignedFieldBytes(2);
                _reader.ReadByte();
                _reader.ReadByte();
            }

            if ((presentFlags & RT_PRESENT_VHT_INFO) > 0)
            {
                const ushort KNOWN_FLAG_GI = 0x04;
                const ushort KNOWN_FLAG_BANDWIDTH = 0x40;

                const byte FLAGS_GI_MASK = 0x04;

                var knownBytes = _reader.ReadNaturallyAlignedFieldBytes(2);
                var flags = _reader.ReadByte();
                var bandwidth = _reader.ReadByte();

                var mcsNss0 = _reader.ReadByte();
                var mcsNss1 = _reader.ReadByte();
                var mcsNss2 = _reader.ReadByte();
                var mcsNss3 = _reader.ReadByte();
                
                var coding = _reader.ReadByte();
                var groupId = _reader.ReadByte();
                var partialAid = _reader.ReadNaturallyAlignedFieldBytes(2);

                var known = BitConverter.ToUInt16(knownBytes, 0);
                if ((known & KNOWN_FLAG_GI) > 0)
                {
                    packet.ItsShortGuardFlag = (flags & FLAGS_GI_MASK) > 0;
                }

                if ((known & KNOWN_FLAG_BANDWIDTH) > 0)
                {
                    bandwidth &= 0x1F; // upper bits are not used
                    switch (bandwidth)
                    {
                        case 0:
                        case 2:
                        case 3:
                        case 7:
                        case 8:
                        case 9:
                        case 10:
                        case 18:
                        case 19:
                        case 20:
                        case 21:
                        case 22:
                        case 23:
                        case 24:
                        case 25:
                            packet.ItsChannelWidth = ChannelWidth.Twenty;
                            break;

                        case 1:
                        case 5:
                        case 6:
                        case 14:
                        case 15:
                        case 16:
                        case 17:
                            packet.ItsChannelWidth = ChannelWidth.Forty;
                            break;

                        case 4:
                        case 12:
                        case 13:
                            packet.ItsChannelWidth = ChannelWidth.Eighty;
                            break;

                        case 11:
                            packet.ItsChannelWidth = ChannelWidth.OneSixty;
                            break;
                    }
                }

                packet.ItsMcsIndex = (ushort)(mcsNss0 >> 4);
                packet.ItsSpatialStreams = (ushort)(mcsNss0 & 0x0F);
            }

            if ((presentFlags & RT_PRESENT_FRAME_TIMESTAMP) > 0)
            {
                _reader.ReadNaturallyAlignedFieldBytes(8);
                _reader.ReadNaturallyAlignedFieldBytes(2);
                _reader.ReadByte();
                _reader.ReadByte();
            }

            if ((presentFlags & RT_PRESENT_HE_INFO) > 0)
            {
                _reader.ReadNaturallyAlignedFieldBytes(2);
                _reader.ReadNaturallyAlignedFieldBytes(2);
                _reader.ReadNaturallyAlignedFieldBytes(2);
                _reader.ReadNaturallyAlignedFieldBytes(2);
                _reader.ReadNaturallyAlignedFieldBytes(2);
                _reader.ReadNaturallyAlignedFieldBytes(2);
            }

            if ((presentFlags & RT_PRESENT_HE_MU_INFO) > 0)
            {
                _reader.ReadNaturallyAlignedFieldBytes(2);
                _reader.ReadNaturallyAlignedFieldBytes(2);
                _reader.ReadByte();
                _reader.ReadByte();
            }

            if ((presentFlags & RT_PRESENT_ZERO_LENGTH_PSDU) > 0)
            {
                _reader.ReadByte();
            }

            if ((presentFlags & RT_PRESENT_L_SIG) > 0)
            {
                _reader.ReadNaturallyAlignedFieldBytes(2);
                _reader.ReadNaturallyAlignedFieldBytes(2);
            }

            // read any remaining radiotap header namespaces to get to beginning of 802.11 frame
            _reader.SkipToLocation(radioTapStartByteCounter + radioTapHeaderLength);

            // calculate data rate if it wasn't explicitly stated.
            if (packet.ItsRate < 1)
            {
                packet.ItsRate = DataRateCalculator.DataRateFromMcsDetails(packet.ItsMcsIndex, packet.ItsSpatialStreams,
                    packet.ItsChannelWidth, packet.ItsShortGuardFlag);
            }

            packet.ItsPacketBytes = _reader.ReadBytes((int)(packetLength - radioTapHeaderLength));
            packet.ItsLength = packet.ItsPacketBytes.Length;

            return packet;
        }

        public void Dispose()
        {
            StopPcapLoaderThread();
        }

        #endregion

        #region Data

        internal class InterfaceDescription
        {
            internal uint ItsLinkType { get; }
            internal uint ItsSnapLength { get; }

            internal byte ItsTimestampResolution { get; set; }

            internal string ItsInterfaceName { get; set; }

            internal string ItsInterfaceDescription { get; set; }

            internal InterfaceDescription(uint linkType, uint snapLength)
            {
                ItsLinkType = linkType;
                ItsSnapLength = snapLength;

                // default resolution is microsecond
                ItsTimestampResolution = PacketHelpers.MICROSECOND_RESOLUTION;
            }
        }
        #endregion
    }
}
