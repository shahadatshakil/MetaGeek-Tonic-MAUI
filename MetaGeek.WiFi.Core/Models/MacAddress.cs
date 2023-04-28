using System;
using System.Text.RegularExpressions;
using MetaGeek.WiFi.Core.Enums;
using MetaGeek.WiFi.Core.Interfaces;

namespace MetaGeek.WiFi.Core.Models
{
    /// <summary>
    /// Represents The Media Access Control Address
    /// </summary>
    public class MacAddress : IMacAddress
    {
        #region Fields

        private MacAddressType _addressType;
        private readonly byte[] _bytes = new byte[6];
        //private readonly ulong _ulongRadioValue;
        private readonly ulong _uLongValue = ~0UL;

        public const ulong UnresolvedUlongValue = 0xFFFFFFFFFFFE;

        private string _friendlyName;
        private string _vendor;
        private string _broadcastName;
        private string _alias;

        #endregion Fields

        #region Properties

        public byte[] ItsBytes
        {
            get
            {
                var bytes = new byte[6];
                Array.Copy(_bytes, bytes, 6);
                return bytes;
            }
        }

        public string ItsStringValue
        {
            get { return ToString(); }
        }

        public MacAddressType ItsType
        {
            get
            {
                return _addressType;
            }
            set
            {
                _addressType = value;
            }
        }

        public ulong ItsUlongValue
        {
            get { return _uLongValue; }
        }

        public string ItsFriendlyName
        {
            get { return _friendlyName; }
        }

        public string ItsVendor
        {
            get { return _vendor; }
            set
            {
                _vendor = value;
                SetFriendlyName();
            }
        }

        public string ItsBroadcastName
        {
            get { return _broadcastName; }
            set
            {
                _broadcastName = value;
                SetFriendlyName();
            }
        }

        public string ItsAlias
        {
            get { return _alias; }
            set
            {
                _alias = value;
                SetFriendlyName();
            }
        }
        #endregion Properties

        #region Constructors

        internal MacAddress(byte b0, byte b1, byte b2, byte b3, byte b4, byte b5)
            : this(new[] { b0, b1, b2, b3, b4, b5 })
        {
        }

        /// <summary>
        /// Builds A MacAddress from a Byte Array
        /// </summary>
        /// <param name="bytes">Must be 6 bytes</param>
        /// <exception cref="ArgumentOutOfRangeException">MAC Addresses Must be 6 Bytes long</exception>
        internal MacAddress(byte[] bytes)
        {
            if (bytes.Length != 6)
                throw new ArgumentOutOfRangeException("bytes", "MAC Addresses Must be 6 Bytes long");

            Array.Copy(bytes, _bytes, 6);
            _uLongValue = ULongFromBytes();
        }

        internal MacAddress(byte[] bytes, int startIndex)
        {
            if ((bytes.Length - startIndex) < 6)
                throw new ArgumentOutOfRangeException("bytes", "MAC Addresses Must be 6 Bytes long");

            Array.Copy(bytes, startIndex, _bytes, 0, 6);
            _uLongValue = ULongFromBytes();
        }

        // Copy constructor for cloning
        internal MacAddress(MacAddress mac)
        {
            Array.Copy(mac._bytes, _bytes, 6);
            _uLongValue = mac._uLongValue;
        }

        //public IMacAddress Clone()
        //{
        //    return new MacAddress(this);
        //}

        public MacAddress(ulong macBytes) : this(BytesFromULong(macBytes))
        {
        }

        #endregion Constructors

        #region Methods

        private static byte[] BytesFromULong(ulong bytesSource)
        {
            var bytes = new byte[6];
            for (var i = 5; i >= 0; i--)
            {
                bytes[i] = (byte)(bytesSource & 0xff);
                bytesSource >>= 8;
            }
            return bytes;
        }

        private ulong ULongFromBytes()
        {
            ulong val = 0;
            foreach (var octet in _bytes)
            {
                val <<= 8;
                val |= octet;
            }
            return val;
        }

        public string BuildVendorMacString(string vendor)
        {
            if (string.IsNullOrEmpty(vendor)) return string.Empty;

            var bytes = ItsBytes;
            var vendorWords = vendor.Split(new char[] { ' ', '_', ',', '-' });

            if (vendorWords.Length < 1 || bytes.Length < 6) return string.Empty;

            return $"{vendorWords[0]}_{bytes[3]:X2}:{bytes[4]:X2}:{bytes[5]:X2}";
        }

        private void SetFriendlyName()
        {
            if (!string.IsNullOrEmpty(_alias))
            {
                _friendlyName = _alias;
            }
            else if (!string.IsNullOrEmpty(_broadcastName))
            {
                _friendlyName = _broadcastName;
            }
            else if (!string.IsNullOrEmpty(_vendor))
            {
                _friendlyName = _vendor;
            }
            else
            {
                _friendlyName = ItsStringValue;
            }
        }


        public override string ToString()
        {
            if (_bytes.Length > 6) return string.Empty;

            var str = String.Format("{0:X2}:{1:X2}:{2:X2}:{3:X2}:{4:X2}:{5:X2}",
                _bytes[0], _bytes[1], _bytes[2], _bytes[3], _bytes[4], _bytes[5]);

            return str;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return _uLongValue == ((MacAddress)obj)._uLongValue;
        }

        public override int GetHashCode()
        {
            return _uLongValue.GetHashCode();
        }

        public int CompareTo(IMacAddress other)
        {
            if (other == null)
                return -1;

            return _uLongValue.CompareTo(other.ItsUlongValue);
        }

        public bool IsEqualTo(byte[] bytes, int checkBytes = 0)
        {
            if (_bytes == bytes) return true;
            if (_bytes == null || bytes == null) return false;
            if (_bytes.Length != bytes.Length) return false;

            var lengthToCompare = checkBytes == 0 ? _bytes.Length : checkBytes;

            for (var i = 0; i < lengthToCompare; i++)
            {
                if (_bytes[i] != bytes[i]) return false;
            }
            return true;
        }

        #endregion Methods
    }
}