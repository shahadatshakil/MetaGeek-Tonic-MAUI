using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaGeek.Tonic.MAUI.MVP_template_.ViewModels
{
    public class RegionView1ViewModel: BindableBase
    {
        public string Message { get; }

        public RegionView1ViewModel()
        {
            Message = "Welcome to .NET MAUI from View 1!";
        }
    }
}
