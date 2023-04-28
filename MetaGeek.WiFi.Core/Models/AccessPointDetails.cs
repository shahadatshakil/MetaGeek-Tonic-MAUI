using System;
using MetaGeek.WiFi.Core.Interfaces;

namespace MetaGeek.WiFi.Core.Models
{
    public class AccessPointDetails : IAccessPointDetails
    {
        private int _itsId;
        private string _itsAlias;
        private IApRadioDetails _twoFourGhzRadio;
        private IApRadioDetails _fiveGhzRadio;
        private string _itsVendor;
        private DateTime _firstSeenDateTime;
        public int ItsId
        {
            get { return _itsId; }
            set { _itsId = value; }
        }

        public string ItsAlias
        {
            get { return _itsAlias; }
            set { _itsAlias = value; }
        }

        public string ItsVendor
        {
            get { return _itsVendor; }
            set { _itsVendor = value; }
        }

        public DateTime ItsFirstSeenDateTime
        {
            get { return _firstSeenDateTime; }
        }

        public IApRadioDetails ItsTwoFourGhzRadio
        {
            get { return _twoFourGhzRadio; }
            set
            {
                _twoFourGhzRadio = value;
                if (_twoFourGhzRadio != null && (_firstSeenDateTime == DateTime.MinValue || _twoFourGhzRadio.ItsFirstSeenDateTime < _firstSeenDateTime))
                {
                    _firstSeenDateTime = _twoFourGhzRadio.ItsFirstSeenDateTime;
                }
            }
        }

        public IApRadioDetails ItsFiveGhzRadio
        {
            get { return _fiveGhzRadio; }
            set
            {
                _fiveGhzRadio = value;
                if (_fiveGhzRadio != null && (_firstSeenDateTime == DateTime.MinValue || _fiveGhzRadio.ItsFirstSeenDateTime < _firstSeenDateTime))
                {
                    _firstSeenDateTime = _fiveGhzRadio.ItsFirstSeenDateTime;
                }
            }
        }
    }
}
