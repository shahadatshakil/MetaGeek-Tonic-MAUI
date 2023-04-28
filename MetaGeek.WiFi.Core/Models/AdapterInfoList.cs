using System.Collections.Generic;
using MetaGeek.WiFi.Core.Enums;
using MetaGeek.WiFi.Core.Interfaces;

namespace MetaGeek.WiFi.Core.Models
{
    public class AdapterInfoList
    {
        #region Properties
        public List<AdapterInfo> ItsAdapters { get; }
        public ScannerTypes ItsAdapterType { get; }
        #endregion

        #region Constructors
        public AdapterInfoList(ScannerTypes scannerType, List<AdapterInfo> adapters)
        {
            ItsAdapterType = scannerType;
            ItsAdapters = adapters;
        }
        #endregion
    }
}