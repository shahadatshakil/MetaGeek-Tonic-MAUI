using DryIoc;
using MetaGeek.Tonic.MAUI.MVP;
using MetaGeek.Tonic.MAUI.MVP.ViewModel;
using Prism.Ioc;

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
                         .RegisterInstance(SemanticScreenReader.Default);
        }
    }
}
