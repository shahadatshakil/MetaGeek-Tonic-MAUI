using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using System;

namespace MetaGeek.Tonic.MAUI.MVP_template_
{
    public class Program : MauiApplication
    {
        protected override MauiApp CreatePrismApp() => MauiProgram.CreatePrismApp();

        static void Main(string[] args)
        {
            var app = new Program();
            app.Run(args);
        }
    }
}