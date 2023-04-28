using MetaGeek.WiFi.Core.Models;
using System;

namespace MetaGeek.WiFi.Core.Interfaces
{
    /// <summary>
    /// Interface to be implemented by classes that handle scanning
    /// bool return values indicate whether scanner service is able to also scan all channels
    /// </summary>
    public interface IPcapReaderService
    {
        bool ItsInitializedFlag { get; }
        
        DateTime ItsPacketStartTime { get; }
        
        DateTime ItsPacketEndTime { get; }
        
        void SetDataSourceInfo(DataSourceInfo dataSourceInfo);

        void ReadFile(string fileName);

        void StopParsingFile();
    }
}
