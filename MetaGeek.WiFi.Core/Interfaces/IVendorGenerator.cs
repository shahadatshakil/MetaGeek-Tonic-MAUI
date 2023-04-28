namespace MetaGeek.WiFi.Core.Interfaces
{
    public interface IVendorGenerator
    {
        void Initialize();
        string GetVendor(IMacAddress mac);
        string GetVendorAppendedMac(IMacAddress address);
    }
}