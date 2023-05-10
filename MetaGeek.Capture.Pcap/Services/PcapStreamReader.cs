using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetaGeek.Capture.Pcap.Services;

namespace MetaGeek.Capture.Pcap.Services
{
    public class PcapStreamReader
    {
        #region Fields
        private FileStream _fileStream;
        private BinaryReader _reader;
        private int _byteCounter;
        private FileInfo _fileInfo;

        private int _naturalAlignmentOffset;
        #endregion

        #region Properties

        public double ItsFilePositionPercentage
        {
            get { return _fileStream.Position / (double)ItsFileLength; }
        }

        public long ItsFileLength { get; private set; }

        public long ItsFilePosition
        {
            get { return _fileStream.Position; }
        }

        public DateTime ItsFileTimeStamp { get; private set; }

        public long ItsByteCounter
        {
            get { return _byteCounter; }
        }
        #endregion

        #region Constructor

        public PcapStreamReader()
        {
        }

        #endregion

        #region Methods

        public bool OpenFile(string fileName)
        {
            _fileInfo = new FileInfo(fileName);
            if (!_fileInfo.Exists) return false;

            ItsFileLength = _fileInfo.Length;
            ItsFileTimeStamp = _fileInfo.LastWriteTime;

            _fileStream = File.OpenRead(fileName);
            _reader = new BinaryReader(_fileStream);

            ResetByteCounter();

            return true;
        }

        public void CloseFile()
        {
            if (_reader != null)
            {
                _reader.Close();
                _reader = null;
            }
        }

        public static ushort GetLittleEndianUShort(byte[] bytes, int startIndex)
        {
            return (ushort)((ushort)bytes[startIndex] << 8 | bytes[startIndex + 1]);
        }

        public static uint GetLittleEndianUint(byte[] bytes, int startIndex)
        {
            return (uint)((uint)bytes[startIndex] << 24 | (uint)bytes[startIndex + 1] << 16 | (uint)bytes[startIndex + 2] << 8 | bytes[startIndex + 3]);
        }

        public void StartNaturalAlignment()
        {
            _naturalAlignmentOffset = _byteCounter;
        }

        public byte[] ReadNaturallyAlignedFieldBytes(int i)
        {
            // handle natural alignment padding 
            while ((_byteCounter - _naturalAlignmentOffset) % i != 0)
            {
                ReadByte();
            }
            _byteCounter += i;
            return _reader.ReadBytes(i);
        }

        public byte ReadByte()
        {
            _byteCounter++;
            return _reader.ReadByte();
        }

        public byte[] ReadBytes(int count)
        {
            _byteCounter += count;
            return _reader.ReadBytes(count);
        }

        public ushort ReadUshort(bool reverse = false)
        {
            _byteCounter += 2;
            var bytes = _reader.ReadBytes(2);
            if (reverse)
            {
                Array.Reverse(bytes);
            }
            return BitConverter.ToUInt16(bytes, 0);
        }

        public uint ReadUint(bool reverse = false)
        {
            _byteCounter += 4;
            var bytes = _reader.ReadBytes(4);
            if (reverse)
            {
                Array.Reverse(bytes);
            }
            return BitConverter.ToUInt32(bytes, 0);
        }

        public ulong ReadUlong(bool reverse = false)
        {
            _byteCounter += 8;
            var bytes = _reader.ReadBytes(8);
            if (reverse)
            {
                Array.Reverse(bytes);
            }

            return BitConverter.ToUInt64(bytes, 0);
        }

        public void SkipBytes(int count)
        {
            _fileStream.Seek(count, SeekOrigin.Current);
            _byteCounter += count;
        }

        public byte[] PeekBytes(int count)
        {
            var bytes = _reader.ReadBytes(count);
            _fileStream.Seek(-count, SeekOrigin.Current);

            return bytes;
        }

        public void ResetByteCounter()
        {
            _byteCounter = 0;
        }

        public void SkipToLocation(uint count)
        {
            SkipBytes((int)count - _byteCounter);
        }

        #endregion
    }
}
