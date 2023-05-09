using Prism.Commands;
using Prism.Mvvm;
using MetaGeek.Tonic.MAUI.MVP.Model;
using System.Collections.ObjectModel;

namespace MetaGeek.Tonic.MAUI.MVP.ViewModel
{
    // for investigating region navigation
    public class MainPageViewModel : BindableBase
    {
        private INavigationService _navigationService { get; }
        private IRegionManager _regionManager { get; }
        public MainPageViewModel(INavigationService navigationService, IRegionManager regionManager)
        {
            _navigationService = navigationService;
            _regionManager = regionManager;

            NavigateCommand = new DelegateCommand<string>(OnNavigateCommandExecuted);
        }

        public DelegateCommand<string> NavigateCommand { get; }

        private void OnNavigateCommandExecuted(string uri)
        {
            _regionManager.RequestNavigate("TestRegion", uri);
        }
    }

}
