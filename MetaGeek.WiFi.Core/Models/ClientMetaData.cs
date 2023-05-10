using MetaGeek.WiFi.Core.Interfaces;

namespace MetaGeek.WiFi.Core.Models
{
    public class ClientMetaData
    {
        #region Properties
        public IMacAddress ItsMacAddress { get; }
        public string ItsName { get; set; }
        public string ItsBroadcastName { get; set; }
        public string ItsDeviceCategory { get; set; }
        public string ItsMakeModel { get; set; }
        public uint ItsMaxMcsIndex { get; set; }
        public uint ItsSpatialStreamCount { get; set; }
        public double ItsMaxDataRate { get; set; }

        #endregion

        #region Constructors

        public ClientMetaData(IMacAddress macAddress)
        {
            ItsMacAddress = macAddress;
        }

        public ClientMetaData(IMacAddress macAddress, string name, string makeModel, uint maxMcsIndex, uint spatialStreamCount, double maxDataRate)
        {
            ItsMacAddress = macAddress;
            ItsName = name;
            ItsMakeModel = makeModel;
            ItsMaxMcsIndex = maxMcsIndex;
            ItsSpatialStreamCount = spatialStreamCount;
            ItsMaxDataRate = maxDataRate;
        }

        public ClientMetaData(IMacAddress macAddress, uint maxMcsIndex, uint spatialStreamCount, double maxDataRate)
        {
            ItsMacAddress = macAddress;
            ItsMaxMcsIndex = maxMcsIndex;
            ItsSpatialStreamCount = spatialStreamCount;
            ItsMaxDataRate = maxDataRate;
        }
        #endregion
    }
}
