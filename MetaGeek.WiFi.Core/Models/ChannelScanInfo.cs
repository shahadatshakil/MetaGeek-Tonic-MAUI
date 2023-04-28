using MetaGeek.WiFi.Core.Enums;
using MetaGeek.WiFi.Core.Models;
using System;
using System.Collections.Generic;

namespace MetaGeek.WiFi.Core.Models
{
    public class ChannelScanInfo
    {
        #region Fields

        #endregion

        #region Properties
        public SortedList<uint, int> ItsChannelAdapterMap { get; set; }

        public DateTime ItsStartTime { get; }

        public TimeSpan ItsTimeSpan { get; }

        public ScanningState ItsScanningState { get; }

        public DataSourceInfo ItsDataSourceInfo { get; }
        #endregion

        #region Constructor

        /// <summary>
        /// MetaData related to a single channel scan. There can be multiple channels listed
        /// one for EACH adapter available.
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="startTime"></param>
        /// <param name="scanTimeSpan"></param>
        public ChannelScanInfo(SortedList<uint, int> channelAdapterMap, DateTime startTime, TimeSpan scanTimeSpan, ScanningState scanningState, DataSourceInfo dataSourceInfo)
        {
            ItsChannelAdapterMap = channelAdapterMap;
            ItsStartTime = startTime;
            ItsTimeSpan = scanTimeSpan;
            ItsScanningState = scanningState;
            ItsDataSourceInfo = dataSourceInfo;
        }
        #endregion
    }
}
