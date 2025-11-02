using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace AChatFull.Views.Sheets
{
    public partial class StatusBottomSheetPage : ContentPage
    {
        readonly INavigation _mainNav;
        readonly ChatRepository _repo;

        public Command CloseCommand { get; }
        public Command<string> SetPresenceCommand { get; }

        bool _shown;
        bool _initializing;  // чтобы RadioButton не триггерил сохранение при загрузке

        public StatusBottomSheetPage(INavigation mainNav)
        {
            InitializeComponent();

            _mainNav = mainNav ?? Application.Current.MainPage?.Navigation;
            _repo = DependencyService.Get<ChatRepository>() ?? new ChatRepository(App.DBPATH, App.USER_TOKEN_TEST);

            // команды создаём ДО назначения BindingContext
            CloseCommand = new Command(async () => await CloseAsync());
            SetPresenceCommand = new Command<string>(async p => await SetPresenceAsync(p));

            BindingContext = this;

            // стартовая позиция (на всякий случай)
            SheetPanel.TranslationY = 600;

            // свайп вниз
            var pan = new PanGestureRecognizer();
            pan.PanUpdated += OnPanUpdated;
            //SheetPanel.GestureRecognizers.Add(pan);
            DragArea.GestureRecognizers.Add(pan);
        }


        string _selectedPresence; // "Online" | "Away" | "Busy" | "Offline"
        public string SelectedPresence
        {
            get => _selectedPresence;
            set
            {
                if (_selectedPresence == value) return;
                _selectedPresence = value;
                // уведомляем биндинги
                Device.BeginInvokeOnMainThread(() =>
                    OnPropertyChanged(nameof(SelectedPresence)));
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await AnimateInAsync();
        }

        async void OnRadioCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (_initializing || !e.Value) return; // игнорим снятие галочки и стартовую установку
            var rb = (RadioButton)sender;
            var presence = rb?.Value?.ToString();
            if (string.IsNullOrEmpty(presence)) return;

            // обновим SelectedPresence, сохраним в репозиторий и закроем
            SelectedPresence = presence;
            await SetPresenceAsync(presence);
        }

        static string MapPresence(string p)
        {
            p = (p ?? "").Trim().ToLowerInvariant();

            if (p == "online" || p == "available")
                return "Online";

            if (p == "away" || p == "idle")
                return "Away";

            // все варианты DND/Do Not Disturb
            if (p == "busy" ||
                p == "dnd" ||
                p == "do not disturb" ||
                p == "donotdisturb" ||
                p == "do_not_disturb" ||
                p == "do-not-disturb")
                return "Busy";

            if (p == "offline" || p == "invisible")
                return "Offline";

            // на всякий случай
            return "Offline";
        }

        async Task SetPresenceAsync(string presence)
        {
            try
            {
                SelectedPresence = presence;                 // сразу подсветим radio
                await _repo.UpdatePresenceAsync(presence);   // запись в БД/модель

                // 👉 сообщаем иконке таба, как и раньше
                MessagingCenter.Send<object>(this, "ProfileChanged");
                // 👉 сообщаем странице профиля НОВОЕ значение статуса
                MessagingCenter.Send<object, string>(this, "PresenceChanged", presence);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("UpdatePresence failed: " + ex);
                await DisplayAlert("Error", "Failed to update status.", "OK");
            }
            finally
            {
                await CloseAsync();
            }
        }

        async Task AnimateInAsync()
        {
            if (_shown) return;
            _shown = true;

            // ждём, пока панель получит реальную высоту
            for (int i = 0; i < 20 && SheetPanel.Height <= 0; i++)
                await Task.Delay(16);

            var h = SheetPanel.Height > 0 ? SheetPanel.Height : 600;
            SheetPanel.TranslationY = h + 24; // чуть ниже видимой области
            await SheetPanel.TranslateTo(0, 0, 220, Easing.CubicOut);
        }

        async Task CloseAsync()
        {
            var h = SheetPanel.Height > 0 ? SheetPanel.Height : 600;
            await SheetPanel.TranslateTo(0, h + 24, 180, Easing.CubicIn);
            await Navigation.PopModalAsync(false);
        }

        double _startY, _drag;
        void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    _startY = SheetPanel.TranslationY;
                    _drag = 0;
                    break;
                case GestureStatus.Running:
                    _drag = e.TotalY;
                    var ny = Math.Max(0, _startY + e.TotalY);
                    SheetPanel.TranslationY = ny;
                    break;
                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                    if (_drag > 80)
                        Device.BeginInvokeOnMainThread(async () => await CloseAsync());
                    else
                        Device.BeginInvokeOnMainThread(async () => await SheetPanel.TranslateTo(0, 0, 160, Easing.CubicOut));
                    break;
            }
        }
    }
}
