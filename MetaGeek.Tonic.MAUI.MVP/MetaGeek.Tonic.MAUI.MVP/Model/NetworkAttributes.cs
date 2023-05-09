using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaGeek.Tonic.MAUI.MVP.Model
{
    public class NetworkAttributes
    {
        public string SSID { get; set; }
        public string AirtimeUsage { get; set; }
        public string Signal { get; set; }
        public string Radios { get; set; } 
        public string Clients { get; set; }
        public string Events { get; set; }
        public string LastSeen { get; set; }
    }
}
