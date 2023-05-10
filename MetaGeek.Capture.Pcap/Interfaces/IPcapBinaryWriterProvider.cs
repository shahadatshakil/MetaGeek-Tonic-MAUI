using MetaGeek.Tonic.Common.Interfaces;

namespace MetaGeek.Capture.Pcap.Interfaces
{
    public interface IPcapBinaryWriterProvider
    {
        IBinaryWriter GetBinaryWriter(string fileName);
    }
}
