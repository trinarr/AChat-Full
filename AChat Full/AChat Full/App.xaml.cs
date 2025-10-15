using Xamarin.Forms;
using AChatFull.Views;
using Xamarin.Essentials;
using System;
using AChatFull.Services;
using System.Threading.Tasks;

namespace AChatFull
{
    public partial class App : Application
    {
        public static string USER_TOKEN_TEST = "user1";
        public static string DBPATH;

        const int LockGraceSeconds = 10;

        static bool _lockShown;
        static DateTime _lastSleepUtc;

        public App()
        {
            InitializeComponent();

            MainPage = new ContentPage { Content = new ActivityIndicator { IsRunning = true, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center } };
            _ = InitAsync();
        }

        private async Task InitAsync()
        {
            var pin = await SecureStorage.GetAsync("user_pin");
            var bio = (await SecureStorage.GetAsync("bio_enabled")) == "1";

            _lockShown = true;

            var svc = DependencyService.Get<ISettingsService>();
            if (svc != null) svc.SetBool("contacts.showgroups", true);
            Preferences.Set("contacts.showgroups", true);

            if (string.IsNullOrEmpty(pin))
                MainPage = new NavigationPage(new PinPage(isFirstRun: true, biometricsEnabled: false, OnPinSuccess));
            else
                MainPage = new NavigationPage(new PinPage(isFirstRun: false, biometricsEnabled: bio, OnPinSuccess));
        }

        protected override void OnSleep()
        {
            _lastSleepUtc = DateTime.UtcNow;
        }

        protected override async void OnResume()
        {
            var pin = await SecureStorage.GetAsync("user_pin");
            if (string.IsNullOrEmpty(pin)) return;

            if ((DateTime.UtcNow - _lastSleepUtc).TotalSeconds < LockGraceSeconds) return;

            await ShowLockAsync();
        }

        public static async Task ShowLockAsync()
        {
            if (_lockShown) return;
            _lockShown = true;

            var bio = (await SecureStorage.GetAsync("bio_enabled")) == "1";

            var nav = Current?.MainPage?.Navigation;
            if (nav == null) { _lockShown = false; return; }

            PinPage pinPage = null;

            pinPage = new PinPage(isFirstRun: false, biometricsEnabled: bio, onSuccess: async () =>
            {
                await Device.InvokeOnMainThreadAsync(async () => await nav.PopModalAsync());
                _lockShown = false;
            });

            await nav.PushModalAsync(pinPage, animated: true);
        }

        private async Task OnPinSuccess()
        {
            DBPATH = await Utils.PreloadDatabase.GetDatabasePathAsync();

            _lockShown = false;

            var repo = new ChatRepository(DBPATH, USER_TOKEN_TEST);
            Current.MainPage = new MainTabsPage(USER_TOKEN_TEST, repo);
        }
    }
}
