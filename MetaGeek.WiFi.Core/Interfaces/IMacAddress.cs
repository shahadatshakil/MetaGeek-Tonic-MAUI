using System;
using MetaGeek.WiFi.Core.Enums;

namespace MetaGeek.WiFi.Core.Interfaces
{
    public interface IMacAddress : IComparable<IMacAddress>
    {

        #region Properties

        byte[] ItsBytes { get; }

        //string ItsRadioStringValue { get; }

        string ItsStringValue { get; }

        //ulong ItsUlongRadioValue { get; }

        ulong ItsUlongValue { get; }

        MacAddressType ItsType { get; }

        string ItsFriendlyName { get; }

        string ItsVendor { get; set; }

        string ItsBroadcastName { get; set; }

        string ItsAlias { get; set; }

        #endregion Properties

        #region Methods

        string ToString();

        string BuildVendorMacString(string vendor);

        bool IsEqualTo(byte[] bytes, int checkBytes = 0);
        //IMacAddress Clone();

        #endregion Methods
    }
}