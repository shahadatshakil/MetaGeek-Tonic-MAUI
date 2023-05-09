using DryIoc;
using MetaGeek.Tonic.MAUI.MVP;
using MetaGeek.Tonic.MAUI.MVP.ViewModel;
using MetaGeek.Tonic.MAUI.MVP_template_.ViewModels;
using MetaGeek.Tonic.MAUI.MVP_template_.Views;
using Prism.Ioc;
using PrismMauiRegionNavigation.Views;

namespace MetaGeek.Tonic.MAUI.MVP_template_
{
    internal static class PrismStartup
    {
        public static void Configure(PrismAppBuilder builder)
        {
            builder.RegisterTypes(RegisterTypes)
                    .OnAppStart("NavigationPage/MainPage");
        }

        private static void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<MainPage,MainPageViewModel>()
                         .RegisterForRegionNavigation<RegionView1,RegionView1ViewModel>()
                         .RegisterForRegionNavigation<RegionView2,RegionView2ViewModel>()
                         .RegisterForRegionNavigation<ViewWithList,ViewWithListViewModel>()
                         .RegisterInstance(SemanticScreenReader.Default);
        }
    }
}
