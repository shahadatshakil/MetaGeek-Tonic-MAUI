using MetaGeek.WiFi.Core.Enums;
using MetaGeek.WiFi.Core.Interfaces;


namespace MetaGeek.WiFi.Core.Models
{
    public class ConnectionAttributes
    {
        #region Properties

        public WiFiConnectionState ItsConnectionState { get; set; }

        public string ItsProfileName { get; set; }

        public IMacAddress ItsConnectedMacAddress { get; set; }

        public string ItsSsid { get; set; }

        public PhyTypes ItsPhyType { get; set; }

        public double ItsRxRate { get; set; }

        public double ItsTxRate { get; set; }

        public uint ItsSignalQuality { get; set; }

        public WlanEncryptionTypes ItsEncryptionType { get; set; }

        public AuthenticationKeyManagementTypes ItsAuthenticationManagementType { get; set; }

        public bool Its1XEnabledFlag { get; set; }

        public bool ItsSecurityEnabledFlag { get; set; }

        #endregion

        #region Constructor

        public ConnectionAttributes(WiFiConnectionState connectionState)
        {
            ItsConnectionState = connectionState;
        }
        #endregion
    }
}
