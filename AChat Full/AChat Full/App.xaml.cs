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
            _ = InitAsync();
        }
        private async Task InitAsync()
        {
            var dbPath = await PreloadDatabase.GetDatabasePathAsync();
            // например, сохраняем в DependencyService или сразу передаём в ViewModel:
            var repo = new ChatRepository(dbPath, USER_TOKEN_TEST);
            MainPage = new NavigationPage(new ChatsListPage(USER_TOKEN_TEST, repo));
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
