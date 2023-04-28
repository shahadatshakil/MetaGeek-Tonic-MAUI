using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetaGeek.WiFi.Core.Interfaces;


namespace MetaGeek.WiFi.Core.Models
{
    public class IpNetworkInfo : IIpNetworkInfo
    {
        #region Fields

        private bool _hasDynamicAddressFlag;
        private Dictionary<string, Dictionary<string, string>> _services;

        #endregion

        #region Properties

        public bool ItsHasDynamicAddressFlag
        {
            get { return _hasDynamicAddressFlag; }
            set { _hasDynamicAddressFlag = value; }
        }

        public Dictionary<string, Dictionary<string, string>> ItsServices
        {
            get { return _services; }
            set { _services = value; }
        }

        #endregion

        #region Constructor

        public IpNetworkInfo()
        {
            ItsServices = new Dictionary<string, Dictionary<string, string>>();
        }

        #endregion
    }
}
