using MetaGeek.Tonic.MAUI.MVP.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaGeek.Tonic.MAUI.MVP_template_.ViewModels
{
    // for investigating data binding

    class ViewWithListViewModel : BindableBase
    {
        public DelegateCommand LoadBtnCommand { get; }
        private ObservableCollection<NetworkAttributes> myList;
        public string Test { get; set; }

        public ObservableCollection<NetworkAttributes> MyList
        {
            get { return myList; }
            set
            {
                myList = value;
                SetProperty(ref myList, value);
            }
        }

        public ViewWithListViewModel()
        {
            LoadBtnCommand = new DelegateCommand(OnLoadBtnClicked);
            myList = new ObservableCollection<NetworkAttributes>();
            Test = "My Test Button";
            MyList.Add(new NetworkAttributes
            {
                SSID = "Test",
                AirtimeUsage = "Test",
                Signal = "Test",
                Radios = "Test",
                Clients = "Test",
                Events = "Test",
                LastSeen = "Test"
            });
        }

        void OnLoadBtnClicked()
        {
            MyList.Add(new NetworkAttributes
            {
                SSID = "Test1",
                AirtimeUsage = "Test1",
                Signal = "Test1",
                Radios = "Test1",
                Clients = "Test1",
                Events = "Test1",
                LastSeen = "Test1"
            });
        }
    }

}
