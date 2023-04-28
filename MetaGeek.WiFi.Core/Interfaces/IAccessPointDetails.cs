using System;

namespace MetaGeek.WiFi.Core.Interfaces
{
    public interface IAccessPointDetails
    {
        int ItsId { get; set; }
        string ItsAlias { get; set; }
        string ItsVendor { get; set; }
        DateTime ItsFirstSeenDateTime { get; }
        IApRadioDetails ItsTwoFourGhzRadio { get; set; }
        IApRadioDetails ItsFiveGhzRadio { get; set; }
    }
}
