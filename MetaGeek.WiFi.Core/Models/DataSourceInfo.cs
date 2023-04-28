using MetaGeek.WiFi.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaGeek.WiFi.Core.Models
{
    public class DataSourceInfo
    {
        #region Properties
        public int ItsId { get; }

        public bool ItsIsLiveDataFlag { get; }

        public ScannerTypes ItsScannerType { get;  }

        private static int _idCounter;
        #endregion

        #region Constructor

        public DataSourceInfo(bool isLive)
        {
            ItsId = ++_idCounter;
            ItsIsLiveDataFlag = isLive;

            ItsScannerType = isLive ? ScannerTypes.MonitorMode : ScannerTypes.Pcap;
        }

        #endregion
    }
}
