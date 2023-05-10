using MetaGeek.Capture.Pcap.Interfaces;
using MetaGeek.Tonic.Common.Interfaces;
using System.IO;

namespace MetaGeek.Capture.Pcap.Services
{
    public class PcapBinaryWriterProvider : IPcapBinaryWriterProvider
    {
        public IBinaryWriter GetBinaryWriter(string fileName)
        {
            return new PcapBinaryWriter(File.Open(fileName, FileMode.Create));
        }
    }
}
