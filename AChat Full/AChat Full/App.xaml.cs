using Xamarin.Forms;
using AChatFull.Views;

namespace AChatFull
{
    public partial class App : Application
    {
        public static string USER_TOKEN_TEST = "user1";

        public App()
        {
            InitializeComponent();

            InitAsync();

        }

        private async void InitAsync()
        {
            var dbPath = await Utils.PreloadDatabase.GetDatabasePathAsync();

            var repo = new ChatRepository(dbPath, USER_TOKEN_TEST);
            Current.MainPage = new MainTabsPage(USER_TOKEN_TEST, repo);
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
