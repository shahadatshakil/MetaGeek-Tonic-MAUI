using Prism.Commands;
using Prism.Mvvm;
using MetaGeek.Tonic.MAUI.MVP.Model;
using System.Collections.ObjectModel;

namespace MetaGeek.Tonic.MAUI.MVP.ViewModel
{
    class MainPageViewModel : BindableBase
    {
        public DelegateCommand LoadBtnCommand { get; }

        private double _count;

        private ObservableCollection<NetworkAttributes> myList;

        public ObservableCollection<NetworkAttributes> MyList
        {
            get { return myList; }
            set
            {
                myList = value;
                SetProperty(ref myList, value);
            }
        }

        public MainPageViewModel()
        {
            LoadBtnCommand = new DelegateCommand(OnLoadBtnClicked);
            myList = new ObservableCollection<NetworkAttributes>();
            _count = 0;
        }

        void OnLoadBtnClicked()
        {
            _count = (_count + 1) % 10;
            MyList.Add(new NetworkAttributes
            {
                SSID = "Test" + _count.ToString(),
                AirtimeUsage = _count/10,
                AirtimeUsagePercantage = _count * 10,
                Signal = "Test" + _count.ToString(),
                Radios = "Test" + _count.ToString(),
                Clients = "Test" + _count.ToString(),
                Events = "Test" + _count.ToString(),
                LastSeen = "Test" + _count.ToString()
            });
        }
    }
}
