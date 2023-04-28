using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.Platform;

namespace MetaGeek.Tonic.MAUI.MVP.ViewModel
{
    class NetworksViewModel: BindableBase
    {
        public DelegateCommand LoadBtnCommand { get; private set;}

        private string _SSID;
        private string _AirtimeUsage;
        public string SSID 
        {
            get { return _SSID; }
            set
            {
                _SSID = value;
                SetProperty(ref _SSID, value);
            }
        }
        public string AirtimeUsage
        {
            get { return _AirtimeUsage; }
            set
            {
                _AirtimeUsage = value;
                SetProperty(ref _AirtimeUsage, value);
            }
        }

        public NetworksViewModel() 
        {
            LoadBtnCommand = new DelegateCommand(OnLoadBtnClicked);
            _SSID = "14:AC:12";
            _AirtimeUsage = "2 Hours";
        }

        void OnLoadBtnClicked()
        {
            _SSID = "14:AC:12";
            _AirtimeUsage = "2 Hours";
        }
    }
}
