using Xamarin.Forms;
using AChatFull.Views;

namespace AChatFull
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new ChatsList(ChatsViewModel.USER_TOKEN_TEST);
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
