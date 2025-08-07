using Xamarin.Forms;
using AChatFull.Views;
using System.Threading.Tasks;

namespace AChatFull
{
    public partial class App : Application
    {
        public static string USER_TOKEN_TEST = "user1";

        public App()
        {
            InitializeComponent();
            MainPage = new NavigationPage(new PhonePage());
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
