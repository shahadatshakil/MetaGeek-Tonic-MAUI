using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetaGeek.WiFi.Core.Enums;
using Prism.Mvvm;

namespace MetaGeek.WiFi.Core.Models
{
    public class SecurityInfo: BindableBase
    {
        #region Fields

        private string _securityString;
        private AuthenticationTypes _authentication;
        private AuthenticationKeyManagementTypes _authenticationKeyManagementTypes;

        #endregion

        #region Properties

        public string ItsSecurityString
        {
            get
            {
                return _securityString;
            }
            private set
            {
                if (value == _securityString) return;
                _securityString = value;
                SetProperty(ref _securityString, value);
            }
        }

        public AuthenticationTypes ItsAuthentication
        {
            get { return _authentication; }
            set
            {
                if (value == _authentication) return;
                _authentication = value;
                SetProperty(ref _authentication, value);
                ItsSecurityString = BuildSecurityString();
            }
        }

        public AuthenticationKeyManagementTypes ItsAuthenticationKeyManagementTypes
        {
            get { return _authenticationKeyManagementTypes; }
            set
            {
                if (value == _authenticationKeyManagementTypes) return;
                _authenticationKeyManagementTypes = value;
                SetProperty(ref _authenticationKeyManagementTypes, value);
            }
        }

        public bool ItsWpsFlag { get; set; }

        #endregion

        #region Constructors

        public SecurityInfo()
        {
            ItsSecurityString = "Open";
        }

        #endregion

        #region Methods

        public void Merge(SecurityInfo securityInfo)
        {
            if (securityInfo == null) return;

            ItsAuthenticationKeyManagementTypes |= securityInfo.ItsAuthenticationKeyManagementTypes;
            ItsAuthentication |= securityInfo.ItsAuthentication;
        }

        private string BuildSecurityString()
        {
            if ((ItsAuthentication & AuthenticationTypes.WPA3_ENTERPRISE) == AuthenticationTypes.WPA3_ENTERPRISE)
            {
                return "WPA3-Enterprise";
            }

            if ((ItsAuthentication & AuthenticationTypes.WPA2_ENTERPRISE) == AuthenticationTypes.WPA2_ENTERPRISE)
            {
                return "WPA2-Enterprise";
            }

            if ((ItsAuthentication & AuthenticationTypes.WPA_ENTERPRISE) == AuthenticationTypes.WPA_ENTERPRISE)
            {
                return "WPA-Enterprise";
            }

            if ((ItsAuthentication & AuthenticationTypes.WPA3_PRE_SHARED_KEY) == AuthenticationTypes.WPA3_PRE_SHARED_KEY)
            {
                return "WPA3-Personal";
            }

            if ((ItsAuthentication & AuthenticationTypes.WPA2_PRE_SHARED_KEY) == AuthenticationTypes.WPA2_PRE_SHARED_KEY)
            {
                return "WPA2-Personal";
            }

            if ((ItsAuthentication & AuthenticationTypes.WPA_PRE_SHARED_KEY) == AuthenticationTypes.WPA_PRE_SHARED_KEY)
            {
                return "WPA-Personal";
            }

            if ((ItsAuthentication & AuthenticationTypes.WEP) == AuthenticationTypes.WEP)
            {
                return "WEP";
            }

            if (ItsAuthentication == AuthenticationTypes.OPEN)
            {
                return "Open";
            }

            return "Open";
        }
        #endregion
    }
}
