namespace MetaGeek.WiFi.Core.Models
{
    public class ConnectionQualityDetails
    {
        public double ItsRxRate { get; set; }

        public double ItsTxRate { get; set; }

        public uint ItsSignalQuality { get; set; }

        public int ItsRssi { get; set; }        	
    }
}
