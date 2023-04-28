namespace MetaGeek.WiFi.Core.Interfaces
{
    public interface IAnonymousInfoService
    {
        void AnonymizeLastThreeAddressBytes(byte[] macAddressBytes);
        string GetAnonymousSSID(string ssid);
        string GetRandomUser();
        string GetRandomOrg();
    }
}
