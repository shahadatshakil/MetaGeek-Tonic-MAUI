using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaGeek.WiFi.Core.Interfaces
{
    public interface IIpNetworkInfo
    {
        bool ItsHasDynamicAddressFlag { get; set; }

        Dictionary<string, Dictionary<string, string>> ItsServices { get; set; }
    }
}
