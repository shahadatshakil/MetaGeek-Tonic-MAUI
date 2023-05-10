using MetaGeek.WiFi.Core.Models;
using System.Threading.Tasks;

namespace MetaGeek.Capture.Pcap.Interfaces
{
    public interface IPcapWriterService
    {
        string ItsPacketCaptureFolder { get; set; }
        
        void WriteAllPacketsToCaptureFile(string fileName, bool anonymizeFlag = false, bool isPcapng = true);

        bool WritePacketCaptureFile(PacketMetaData[] packets, string fileName, bool anonymizeFlag = false, bool isPcapng = true);

        bool StartPacketCaptureFile(string fileName);
        
        bool AppendPacketCaptureFile(PacketMetaData[] packets);

        bool ClosePacketCaptureFile();

        string GetSavedPcapFolderPath();
    }

}
