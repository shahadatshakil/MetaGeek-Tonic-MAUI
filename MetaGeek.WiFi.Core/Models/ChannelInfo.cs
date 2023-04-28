using System;
using MetaGeek.WiFi.Core.Enums;
using Prism.Mvvm;

namespace MetaGeek.WiFi.Core.Models
{
    public class ChannelInfo : BindableBase, IComparable<ChannelInfo>
    {
        #region Fields

        private const double TOLERANCE = 0.001;
        private uint _channel;
        private uint _primaryChannel;
        private uint _secondaryHtChannel;
        private ChannelWidth _channelWidth;
        private int _hashCode;
        private double _bssChannelUtilization;
        private string _channelString;

        private uint _centerFreq;
        private uint _width;
        private uint _leftEdgeFreq;
        private uint _rightEdgeFreq;
        private bool _bOnlyFlag;

        #endregion

        #region Properties

        public uint ItsChannel
        {
            get { return _channel; }
            set
            {
                if (_channel == value) return;
                _channel = value;
                BuildHashCode();
                UpdateChannelBoundaries();
                UpdateChannelString();
                SetProperty(ref _channel, value);
            }
        }

        public uint ItsPrimaryChannel
        {
            get
            {
                if (_primaryChannel == 0)
                    return ItsChannel;
                return _primaryChannel;
            }
            set
            {
                if (_primaryChannel == value) return;
                _primaryChannel = value;
                BuildHashCode();
                UpdateChannelBoundaries();
                UpdateChannelString();
                SetProperty(ref _channel, value);
            }
        }

        public uint ItsHtSecondaryChannel
        {
            get { return _secondaryHtChannel; }
            set
            {
                if (_secondaryHtChannel == value) return;
                _secondaryHtChannel = value;
                BuildHashCode();
                UpdateChannelBoundaries();
                UpdateChannelString();
                SetProperty(ref _channel, value);
            }
        }

        public string ItsChannelString
        {
            get { return _channelString; }
            set
            {
                if (_channelString == value) return;
                _channelString = value;
                SetProperty(ref _channelString, value);
            }
        }
            
        public uint ItsAcSecondaryChannel { get; set; }

        public ChannelWidth ItsChannelWidth
        {
            get { return _channelWidth; }
            set
            {
                if (_channelWidth == value) return;
                _channelWidth = value;
                BuildHashCode();
                UpdateChannelBoundaries();
                SetProperty(ref _channelWidth, value);
            }
        }

        public double ItsBssRawChannelUtilization
        {
            get { return _bssChannelUtilization; }
            set
            {
                if (Math.Abs(value - _bssChannelUtilization) < TOLERANCE) return;
                _bssChannelUtilization = value;
                SetProperty(ref _bssChannelUtilization, value);
                RaisePropertyChanged(nameof(ItsBssUtilizationFlag));
            }
        }

        public bool ItsBssUtilizationFlag
        {
            get { return ItsBssRawChannelUtilization != 0; }
        }

        public bool ItsBOnlyFlag
        {
            get { return _bOnlyFlag; }
            set
            {
                if (value == _bOnlyFlag) return;
                _bOnlyFlag = value;
                UpdateChannelBoundaries();
            }
        }

        public uint ItsLeftEdgeFrequency
        {
            get
            {
                if(_leftEdgeFreq == 0)
                {
                    UpdateChannelBoundaries();
                }

                return _leftEdgeFreq;
            }
        }

        public uint ItsRightEdgeFrequency
        {
            get
            {
                if (_rightEdgeFreq == 0)
                {
                    UpdateChannelBoundaries();
                }

                return _rightEdgeFreq;
            }
        }

        public ChannelBand ItsBand
        {
            get { return ItsChannel <= 14 ? ChannelBand.TwoGhz : ChannelBand.FiveGhz; }
        }

        #endregion

        #region Methods

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            var other = (ChannelInfo) obj;
            if (ItsChannel != other.ItsChannel) return false;
            if (ItsPrimaryChannel != other.ItsPrimaryChannel) return false;
            if (ItsChannelWidth != other.ItsChannelWidth) return false;
            if (ItsHtSecondaryChannel != other.ItsHtSecondaryChannel) return false;
            if (ItsAcSecondaryChannel != other.ItsAcSecondaryChannel) return false;

            return true;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(ItsChannelString))
            {
                ItsChannelString = BuildChannelString();
            }
            return ItsChannelString;
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public int CompareTo(ChannelInfo other)
        {
            if (other == null)
                return -1;

            return ItsChannel.CompareTo(other.ItsChannel);
        }

        private void BuildHashCode()
        {
            _hashCode = (int)(((uint)_channelWidth << 24) | (_primaryChannel << 16) | (_secondaryHtChannel << 8) | _channel);
        }

        private string BuildChannelString()
        {
            switch (ItsChannelWidth)
            {
                case ChannelWidth.Forty:
                case ChannelWidth.Eighty:
                case ChannelWidth.OneSixty:
                    if (ItsChannelWidth == ChannelWidth.Forty && ItsChannel == ItsPrimaryChannel &&
                        ItsHtSecondaryChannel > 0)
                    {
                        return ItsHtSecondaryChannel > ItsChannel
                            ? $"{ItsChannel}+{ItsHtSecondaryChannel}"
                            : $"{ItsChannel}-{ItsHtSecondaryChannel}";
                    }
                    return $"{ItsChannel} [{ItsPrimaryChannel}]";

                case ChannelWidth.Twenty:
                default:
                    return ItsChannel.ToString();
            }
        }

        private void UpdateChannelString()
        {
            ItsChannelString = BuildChannelString();
        }

        public static ChannelWidth GetAcChannelWidthByChannelName(uint channel)
        {
            // AC does not function in 2.4 GHz
            if (channel < 36) return ChannelWidth.Twenty;

            uint tempChannel = channel;
            if (channel >= 149)
            {
                tempChannel--;
            }
            if (tempChannel % 4 == 0)
            {
                return ChannelWidth.Twenty;
            }

            switch (channel)
            {
                case 42:
                case 58:
                case 106:
                case 122:
                case 138:
                case 155:
                    return ChannelWidth.Eighty;

                case 50:
                case 114:
                    return ChannelWidth.OneSixty;

                default:
                    return ChannelWidth.Forty;
            }

        }

        private void UpdateChannelBoundaries()
        {
            _centerFreq = GetChannelCenterFrequency();
            _width = GetChannelWidth();
            _leftEdgeFreq = _centerFreq - (_width / 2);
            _rightEdgeFreq = _leftEdgeFreq + _width;
        }

        private uint GetChannelCenterFrequency()
        {
            var centerChannel = ItsChannel;
            if (ItsHtSecondaryChannel > 0 && ItsChannel == ItsPrimaryChannel)
            {
                centerChannel = (ItsPrimaryChannel + ItsHtSecondaryChannel) / 2;
            }

            if (centerChannel >= 1 && centerChannel <= 13)
                return centerChannel * 5 + 2407;
            if (centerChannel == 14)
                return 2484;
            if (centerChannel >= 36)
                return centerChannel * 5 + 5000;

            return 0;
        }

        private uint GetChannelWidth()
        {
            if (ItsBOnlyFlag)
                return 22;

            switch (ItsChannelWidth)
            {
                case ChannelWidth.Twenty:
                    return 20;
                case ChannelWidth.Forty:
                    return 40;
                case ChannelWidth.Eighty:
                case ChannelWidth.EightyPlusEighty:
                    return 80;
                case ChannelWidth.OneSixty:
                    return 160;
            }

            return 20;
        }


        #endregion
    }
}
