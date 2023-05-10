using System.IO;

namespace MetaGeek.Capture.Pcap.Interfaces
{
    public interface IBinaryReader
    {
        void Close();

        int Read(byte[] buffer, int index, int count);

        byte[] ReadBytes(int count);

        byte ReadByte();

        Stream BaseStream { get; }
    }
}
