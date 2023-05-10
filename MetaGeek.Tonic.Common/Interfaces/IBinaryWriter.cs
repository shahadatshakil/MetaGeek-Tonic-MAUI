using System.IO;

namespace MetaGeek.Tonic.Common.Interfaces
{
    public interface IBinaryWriter
    {
        void Close();

        void Write(byte value);

        void Write(ushort value);

        void Write(uint value);

        void Write(int value);

        void Write(sbyte value);

        void Write(byte[] buffer);

        void Write(char[] chars);

        void Write(ulong value);

        void Dispose();

        long Seek(int offset, SeekOrigin origin);
    }
}
