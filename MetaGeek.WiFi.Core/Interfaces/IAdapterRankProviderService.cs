using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaGeek.WiFi.Core.Interfaces
{
    public interface IAdapterRankProviderService
    {
        int GetAdapterRankBasedOnName(string name);
    }
}
