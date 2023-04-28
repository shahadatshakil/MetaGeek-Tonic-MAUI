using MetaGeek.WiFi.Core.Enums;
using System;
using System.Text;

namespace MetaGeek.WiFi.Core.Models
{
    public class PhyTypeInfo
    {
        #region Fields
        private string _phyTypeString;
        private string _highestPhyTypeString;
        private string _wifiGenString;
        private ulong _extendedCapabilitiesInfo;

        #endregion

        #region Properties

        public uint ItsPhyTypesEnum { get; private set; }

        public ushort ItsHtCapabilitiesInfo { get; set; }

        public bool ItsNonErpPresentFlag { get; set; }
        public bool ItsProtectionEnabledFlag { get; set; }

        public bool ItsHtNonGreenfieldPresentFlag { get; set; }

        public uint ItsVhtCapabilitiesInfo { get; set; }

        public ulong ItsExtendedCapabilitiesInfo
        {
            get { return _extendedCapabilitiesInfo; }
            set { _extendedCapabilitiesInfo = value; }
        }

        public ushort ItsHeCapabilitiesInfo { get; set; }
        
        public string ItsPhyTypesString
        {
            get
            {
                if (string.IsNullOrEmpty(_phyTypeString))
                {
                    BuildPhyTypeString();
                }
                return _phyTypeString;
            }
        }

        public string ItsHighestPhyTypeString
        {
            get
            {
                if (string.IsNullOrEmpty(_highestPhyTypeString))
                {
                    BuildHighestPhyTypeString();
                }
                return _highestPhyTypeString;
            }
        }

        public string ItsWiFiGenerationString
        {
            get
            {
                if (string.IsNullOrEmpty(_wifiGenString))
                {
                    BuildWiFiGenString();
                }
                return _wifiGenString;
            }
        }

        #endregion

        #region Constructors

        #endregion

        #region Methods

        public void AddPhyType(PhyTypes phyType)
        {
            ItsPhyTypesEnum |= (uint) phyType;
        }

        private void BuildPhyTypeString()
        {
            var builder = new StringBuilder();
            var separator = "/";

            if ((ItsPhyTypesEnum & (uint)PhyTypes.A) > 0)
            {
                builder.Append("a");
            }
            if ((ItsPhyTypesEnum & (uint)PhyTypes.B) > 0)
            {
                if (builder.Length > 0) builder.Append(separator);
                builder.Append("b");
            }
            if ((ItsPhyTypesEnum & (uint)PhyTypes.G) > 0)
            {
                if (builder.Length > 0) builder.Append(separator);
                builder.Append("g");
            }
            if ((ItsPhyTypesEnum & (uint)PhyTypes.N) > 0)
            {
                if (builder.Length > 0) builder.Append(separator);
                builder.Append("n");
            }
            if ((ItsPhyTypesEnum & (uint)PhyTypes.Ac) > 0)
            {
                if (builder.Length > 0) builder.Append(separator);
                builder.Append("ac");
            }
            if ((ItsPhyTypesEnum & (uint)PhyTypes.Ax) > 0)
            {
                if (builder.Length > 0) builder.Append(separator);
                builder.Append("ax");
            }

            _phyTypeString = builder.ToString();
        }

        private void BuildHighestPhyTypeString()
        {
            var phyEnums = Enum.GetValues(typeof(PhyTypes));
            Array.Reverse(phyEnums);

            foreach (PhyTypes phyEnum in phyEnums)
            {
                if ((ItsPhyTypesEnum & (uint)phyEnum) > 0)
                {
                    _highestPhyTypeString = phyEnum.ToString().ToLower();
                    break;
                }
            }
        }

        private void BuildWiFiGenString()
        {

            // Generations 1-3 are not named, so just return PHY Types
            if (ItsPhyTypesEnum < 0x08)
            {
                BuildPhyTypeString();
                _wifiGenString = _phyTypeString;
                return;
            }

            var builder = new StringBuilder("Wi-Fi ");
            var generation = 0;


            if (ItsPhyTypesEnum >= (uint) PhyTypes.Ax)
            {
                generation = 6;
            }
            else if (ItsPhyTypesEnum >= (uint) PhyTypes.Ac)
            {
                generation = 5;
            }
            else if (ItsPhyTypesEnum >= (uint) PhyTypes.N)
            {
                generation = 4;
            }

            builder.Append(generation);

            _wifiGenString = builder.ToString();
        }

        public void Merge(PhyTypeInfo phyTypeInfo)
        {
            if (phyTypeInfo == null) return;

            ItsPhyTypesEnum |= phyTypeInfo.ItsPhyTypesEnum;
        }

        #endregion

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            var other = (PhyTypeInfo) obj;
            if (ItsHtCapabilitiesInfo != other.ItsHtCapabilitiesInfo) return false;
            if (ItsVhtCapabilitiesInfo != other.ItsVhtCapabilitiesInfo) return false;

            return true;
        }
        public override int GetHashCode()
        {
            unchecked
            {
                return (int) (ItsVhtCapabilitiesInfo * 419) ^ (int) (ItsHtCapabilitiesInfo * 397) ^ (int)ItsPhyTypesEnum;
            }
        }
    }
}
