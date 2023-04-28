using MetaGeek.WiFi.Core.Enums;
using MetaGeek.WiFi.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MetaGeek.WiFi.Core.Models
{
    public static class MacAddressCollection
    {
        #region Fields
        private static Dictionary<ulong, IMacAddress> _macAddresses = new Dictionary<ulong, IMacAddress>();

        private const ulong _broadcastValue =       0xffffffffffff;
        private const ulong _ipv4MulticastValue =   0x01005e000000;
        private const ulong _ipv6MulticastValue =   0x333300000000;
        private const ulong _spanningTreeValue =    0x0180c2000000;
        private const ulong _precisionTime =        0x01AB19000000;
        private const ulong _iecMulticast =         0x010CCD000000;
        private const ulong _ciscoProtocols =       0x01000C000000;
        private const ulong _unknownValue =         0x000000000000;
        private const ulong _unresolvedValue =      0xfffffffffffe;

        #endregion

        #region Methods
        public static IMacAddress GetMacAddress(ulong address)
        {
            if(_macAddresses.ContainsKey(address))
            {
                return _macAddresses[address];
            }
            var macAddress = new MacAddress(address);
            _macAddresses.Add(address, macAddress);
            macAddress.ItsType = CalculateType(address);

            return macAddress;
        }

        public static IMacAddress GetMacAddress(byte[] bytes, int startIndex)
        {
            var address = UlongFromBytes(bytes, startIndex);
            if (_macAddresses.ContainsKey(address))
            {
                return _macAddresses[address];
            }
            var macAddress = new MacAddress(bytes, startIndex);
            _macAddresses.Add(address, macAddress);
            macAddress.ItsType = CalculateType(address);

            return macAddress;
        }
        
        public static IMacAddress GetMacAddress(byte[] bytes)
        {
            return GetMacAddress(bytes, 0);
        }

        public static IMacAddress GetMacAddress(string macString)
        {
            var bytes = BytesFromString(macString);
            return GetMacAddress(bytes, 0);
        }

        public static bool ContainsMacAddress(ulong address)
        {
            return _macAddresses.ContainsKey(address);
        }

        public static IMacAddress Broadcast()
        {
            return GetMacAddress(_broadcastValue);
        }

        public static IMacAddress Ipv4Multicast()
        {
            return GetMacAddress(_ipv4MulticastValue);
        }

        public static IMacAddress Ipv6Multicast()
        {
            return GetMacAddress(_ipv6MulticastValue);
        }

        public static IMacAddress SpanningTree()
        {
            return GetMacAddress(_spanningTreeValue);
        }

        public static IMacAddress PrecisionTimeAddress()
        {
            return GetMacAddress(_precisionTime);
        }

        public static IMacAddress IecMulticastAddress()
        {
            return GetMacAddress(_iecMulticast);
        }

        public static IMacAddress CiscoProtocolsAddress()
        {
            return GetMacAddress(_ciscoProtocols);
        }

        public static IMacAddress Unknown()
        {
            return GetMacAddress(_unknownValue);
        }

        public static IMacAddress Unresolved()
        {
            return GetMacAddress(_unresolvedValue);
        }

        private static MacAddressType CalculateType(ulong macAddress)
        {
            if (macAddress == _broadcastValue)
            {
                return MacAddressType.Broadcast;
            }
            if (macAddress == _unknownValue)
            {
                return MacAddressType.Unknown;
            }

            if (macAddress == _unresolvedValue)
            {
                return MacAddressType.Unresolved;
            }

            // All multicast address are in address families based on their OUI
            var macOui = macAddress & 0xFFFFFF000000;

            switch (macOui)
            {
                case _ipv4MulticastValue: return MacAddressType.Ipv4Multicast;
                case _spanningTreeValue: return MacAddressType.SpanningTree;
                case _precisionTime: return MacAddressType.PrecisionTime;
                case _iecMulticast: return MacAddressType.IecMulticast;
                case _ciscoProtocols: return MacAddressType.CiscoProtocols;
            }

            // IPV6 Multicast uses 0x333300 - 0x3333FF OUIs
            if ((macAddress & 0x333300000000) == _ipv6MulticastValue)
            {
                return MacAddressType.Ipv6Multicast;
            }

            return MacAddressType.Normal;
        }

        private static ulong UlongFromBytes(byte[] bytes, int startIndex)
        {
            ulong val = 0;
            for(int counter = 0; counter < 6; counter++)
            {
                val <<= 8;
                val |= bytes[startIndex + counter];
            }
            return val;
        }

        private static byte[] BytesFromString(string addressString)
        {
            // NOTE: This is mainly for taking a RadioMac string but any digit with an "X" will be set to 0
            var upperAddress = addressString.ToUpper().Replace('X', '0');

            var rgx = new Regex("[^a-zA-Z0-9 -]");
            var alphaString = rgx.Replace(upperAddress, "");

            if (alphaString.Length != 12)
                throw new ArgumentOutOfRangeException("addressString", "String must be in the Format 000000000000 or each octect delimited by a non-alphanumeric character");

            var bytes = new byte[6];
            for (int i = 0; i < 6; i++)
            {
                try
                {
                    bytes[i] = Convert.ToByte(alphaString.Substring(i * 2, 2), 16);
                }
                catch (FormatException)
                {
                    throw new FormatException();
                }
            }
            return bytes;
        }

        #endregion
    }
}
