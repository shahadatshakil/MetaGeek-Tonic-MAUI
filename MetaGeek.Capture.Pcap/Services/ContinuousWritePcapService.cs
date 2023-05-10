using MetaGeek.Capture.Pcap.Interfaces; // later investigate
using MetaGeek.Infrastructure.Events;
using MetaGeek.Infrastructure.Interfaces;
using MetaGeek.Tonic.Common.Events;
using MetaGeek.WiFi.Core.Events;
using MetaGeek.WiFi.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Prism.Events;

namespace MetaGeek.Capture.Pcap.Services
{

    public class ContinuousWritePcapService : IDisposable
    {
        #region Fields

        private int WRITE_PACKET_COLLECTION_CHECK_INTERVAL_MSEC = 60 * 1000; // 1 minute
        private int FILE_SESSION_MINUTE = 60;
        private int CAPTURE_SAVE_PROMPT_MIN_TIME_MINUTES = 5;

        private int _writeLapseCount;

        private IEventAggregator _eventAggregator;
        private IPcapWriterService _pcapWriterService;
        private IAppSettingsAccessor _settingsAccessor;
        private Thread _continuousPacketWritingThread;
        // private readonly IRecentCaptureFileProvider _recentCaptureFileProvider;

        private List<PacketMetaData> _packetes;

        private object _packetesLock;

        private DateTime _lastPcapFileStartedAt;
        private string _latestPcapFilePath;
        private string _latestPcapFileName;
        private bool _defaultCaptureDirectoryChangedFlag;
        private bool _liveCaptureModeFlag;
        private bool _captureSavingInitializedFlag;

        #endregion

        #region Constructor

        internal ContinuousWritePcapService(IEventAggregator eventAggregator, IPcapWriterService pcapWriterService,
            IAppSettingsAccessor settingsAccessor, IRecentCaptureFileProvider recentCaptureFileProvider)
        {
            _eventAggregator = eventAggregator;
            _pcapWriterService = pcapWriterService;
            _settingsAccessor = settingsAccessor;
            _recentCaptureFileProvider = recentCaptureFileProvider;

            HookEvents();
            Initialize();
        }

        #endregion

        #region Methods

        private void Initialize()
        {
            _packetes = new List<PacketMetaData>();
            _packetesLock = new object();
            _continuousPacketWritingThread = new Thread(WritePacketCollection);
            _continuousPacketWritingThread.Start();

            _lastPcapFileStartedAt = DateTime.UtcNow;
            _latestPcapFilePath = String.Empty;
            _latestPcapFileName = String.Empty;
            _defaultCaptureDirectoryChangedFlag = false;
            _liveCaptureModeFlag = true;
            _captureSavingInitializedFlag = false;
        }

        private void HookEvents()
        {
            _eventAggregator.GetEvent<LivePacketCaptureEvent>().Subscribe(WritePacketRequestEventHandler);
            _eventAggregator.GetEvent<DefaultCaptureDirectoryChangedEvent>().Subscribe(DefaultCaptureDirectoryChangedEventHandler);
            _eventAggregator.GetEvent<CaptureFileParseRequestEvent>().Subscribe(CaptureFileParseRequestEventHandler);
            _eventAggregator.GetEvent<RestartPacketScannerRequestEvent>().Subscribe(RestartPacketScannerRequestEventHandler);
        }

        private void CaptureFileParseRequestEventHandler(CaptureFileParseRequestEventArgs eventArgs)
        {
            if (_liveCaptureModeFlag)
            {
                DumpPacketsToFile();
                SaveOrDeleteFile();
            }

            _liveCaptureModeFlag = false;
        }

        private void WritePacketRequestEventHandler(PacketMetaData packet)
        {
            lock (_packetesLock)
            {
                _packetes.Add(packet);
            }
        }

        private void DefaultCaptureDirectoryChangedEventHandler(EventArgs obj)
        {
            _defaultCaptureDirectoryChangedFlag = true;
        }

        private void RestartPacketScannerRequestEventHandler(EventArgs args)
        {
            _liveCaptureModeFlag = true;
            DumpPacketsToFile();
        }

        private void WritePacketCollection()
        {
            while (true)
            {
                Thread.Sleep(WRITE_PACKET_COLLECTION_CHECK_INTERVAL_MSEC);

                if (_liveCaptureModeFlag)
                {
                    DumpPacketsToFile();

                    _writeLapseCount++;
                }
            }
        }

        private void DumpPacketsToFile()
        {
            if (_defaultCaptureDirectoryChangedFlag || _lastPcapFileStartedAt.AddMinutes(FILE_SESSION_MINUTE) < DateTime.UtcNow || string.IsNullOrEmpty(_latestPcapFilePath))
            {
                SaveOrDeleteFile();

                _lastPcapFileStartedAt = DateTime.UtcNow;
                _defaultCaptureDirectoryChangedFlag = false;
                _writeLapseCount = 0;

                CreateNewFileName();
                _captureSavingInitializedFlag = false;
            }

            if (_packetes.Count > 0)
            {
                if (!_captureSavingInitializedFlag)
                {
                    _pcapWriterService.StartPacketCaptureFile(_latestPcapFilePath);
                    _captureSavingInitializedFlag = true;
                }

                lock (_packetesLock)
                {
                    _pcapWriterService.AppendPacketCaptureFile(_packetes.ToArray());
                    _packetes.Clear();
                }
            }
        }

        private void SaveOrDeleteFile()
        {
            if (string.IsNullOrEmpty(_latestPcapFilePath) || !_captureSavingInitializedFlag) return;

            _pcapWriterService.ClosePacketCaptureFile();
            _recentCaptureFileProvider.UpdateRecentFilesCollection(_latestPcapFilePath);

            if (_writeLapseCount >= CAPTURE_SAVE_PROMPT_MIN_TIME_MINUTES)
            {
                _eventAggregator.GetEvent<ContinuoiusPcapWriteIterationCompletedEvent>().Publish(_latestPcapFileName);
            }
            else
            {
                DeleteCurrentFile();
            }

            _latestPcapFilePath = "";
        }

        private void CreateNewFileName()
        {
            string pcapFolderFilePath = _settingsAccessor.ReadDefaultSetting<string>("defaultCaptureDirectory");

            if (string.IsNullOrEmpty(pcapFolderFilePath))
            {
                pcapFolderFilePath = _pcapWriterService.GetSavedPcapFolderPath();

                try
                {
                    if (!Directory.Exists(pcapFolderFilePath))
                    {
                        Directory.CreateDirectory(pcapFolderFilePath);
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Exception while creating directory: {0}. {1}", pcapFolderFilePath, ex.Message);
                    return;
                }

                _settingsAccessor.WriteDefaultSetting("defaultCaptureDirectory", pcapFolderFilePath);
            }

            _latestPcapFileName = _lastPcapFileStartedAt.ToString("yyyy-MM-dd-HH-mm-ss") + "__" + _lastPcapFileStartedAt.AddMinutes(FILE_SESSION_MINUTE).ToString("HH-mm-ss") + ".pcapng";

            _latestPcapFilePath = Path.Combine(pcapFolderFilePath, _latestPcapFileName);
        }

        private void KillThreads()
        {
            if (_continuousPacketWritingThread != null)
            {
                if (!_continuousPacketWritingThread.Join(100))
                    _continuousPacketWritingThread.Abort();

                _continuousPacketWritingThread = null;
            }
        }

        private void DeleteCurrentFile()
        {
            if (!string.IsNullOrEmpty(_latestPcapFilePath))
            {
                try
                {
                    File.Delete(_latestPcapFilePath);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Couldn't remove the file: {0}. {1}", _latestPcapFilePath, ex.Message);
                }
            }
        }

        public void Dispose()
        {
            KillThreads();

            DumpPacketsToFile();

            DeleteCurrentFile();
        }

        #endregion
    }
}
