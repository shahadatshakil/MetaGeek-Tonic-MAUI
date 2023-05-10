using MetaGeek.Tonic.Common.Interfaces;
using System.IO;

namespace MetaGeek.Capture.Pcap.Services
{
    internal class PcapBinaryWriter : BinaryWriter, IBinaryWriter
    {
        public PcapBinaryWriter(FileStream fileStream) : base(fileStream)
        { 
        }
    }
}
