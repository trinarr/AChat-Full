using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using AChatFull.Views;

namespace AChatFull
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new ChatsList("Test");
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
