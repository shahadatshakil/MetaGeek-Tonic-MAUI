using System;
using Prism.Mvvm;

namespace MetaGeek.WiFi.Core.Models
{
    public class AdapterInfo : BindableBase, IComparable<AdapterInfo>
    {
        #region Fields
        private uint? _channel;
        #endregion

        #region Properties
        public string ItsName { get; private set; }

        public uint? ItsChannel {
            get => _channel;
            set
            {
                if (value == _channel) return;
                _channel = value;
                SetProperty(ref _channel, value);
            }
        }

        public int ItsDeviceIndex
        {
            get; set;
        }

        public int ItsRank { get; set; }

        #endregion Properties

        #region Constructors

        public AdapterInfo(int deviceIndex, string name, int rank)
        {
            ItsName = name;
            ItsDeviceIndex = deviceIndex;
            ItsRank = rank;
        }

        #endregion

        #region Methods

        public int CompareTo(AdapterInfo other)
        {
            if (other == null)
            {
                return -1;
            }

            return ItsRank.CompareTo(other.ItsRank);
        }

        #endregion
    }
}