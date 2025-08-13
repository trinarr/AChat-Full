using Xamarin.Forms;
using AChatFull.Views;
using Xamarin.Essentials;
using System;
using System.Threading.Tasks;

namespace AChatFull
{
    public partial class App : Application
    {
        public static string USER_TOKEN_TEST = "user1";

        // период (сек), в течение которого не спрашиваем PIN после быстрого свитча
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
            // если нет PIN — ничего не делаем
            var pin = await SecureStorage.GetAsync("user_pin");
            if (string.IsNullOrEmpty(pin)) return;

            // грейс‑период
            if ((DateTime.UtcNow - _lastSleepUtc).TotalSeconds < LockGraceSeconds) return;

            await ShowLockAsync();
        }

        // Унифицированный показ PinPage как МОДАЛКИ
        public static async Task ShowLockAsync()
        {
            if (_lockShown) return;
            _lockShown = true;

            var bio = (await SecureStorage.GetAsync("bio_enabled")) == "1";

            var nav = Current?.MainPage?.Navigation;
            if (nav == null) { _lockShown = false; return; }

            // объявим переменную заранее — понадобится в колбэке
            PinPage pinPage = null;

            pinPage = new PinPage(isFirstRun: false, biometricsEnabled: bio, onSuccess: async () =>
            {
                // закрываем модалку
                await Device.InvokeOnMainThreadAsync(async () => await nav.PopModalAsync());
                _lockShown = false;
            });

            // ПУШИМ ИМЕННО pinPage (без NavigationPage-обёртки)
            await nav.PushModalAsync(pinPage, animated: true);
        }

        // Колбэк для первого запуска (когда MainPage = PinPage)
        private async Task OnPinSuccess()
        {
            var dbPath = await Utils.PreloadDatabase.GetDatabasePathAsync();

            var repo = new ChatRepository(dbPath, App.USER_TOKEN_TEST);
            App.Current.MainPage = new MainTabsPage(App.USER_TOKEN_TEST, repo);
        }
    }
}
