using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Forms;
using AChatFull.Utils;
using AChatFull.Views;
using System.Diagnostics;

namespace AChatFull.ViewModels
{
    public class ProfileViewModel : INotifyPropertyChanged, ILazyInitViewModel
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private readonly ChatRepository _repo;
        bool _presenceSubscribed;

        INavigation _nav;

        // зависимости
        readonly Command _changeStatusCommand;
        readonly Command _setAwayCommand;
        readonly Command _editProfileCommand;
        readonly Command _openSettingsCommand;

        public ProfileViewModel(ChatRepository repo)
        {
            // Получаем репозиторий так же, как в других местах проекта:
            _repo = repo;

            _changeStatusCommand = new Command(async () => await ChangeStatusAsync());
            _setAwayCommand = new Command(async () => await SetPresenceAsync("Away"));
            _editProfileCommand = new Command(async () => await OpenEditProfileAsync());
            _openSettingsCommand = new Command(async () => await OpenSettingsAsync());
        }

        public Command ChangeStatusCommand { get { return _changeStatusCommand; } }
        public Command SetAwayCommand { get { return _setAwayCommand; } }
        public Command EditProfileCommand { get { return _editProfileCommand; } }
        public Command OpenSettingsCommand { get { return _openSettingsCommand; } }

        // UI-модель
        string _displayName;
        string _avatarSource; // локальный путь/ресурс
        string _initials;

        public string DisplayName { get { return _displayName; } set { Set(ref _displayName, value); } }
        public string AvatarSource {
            get { return _avatarSource; } 
            set 
            {
                if (_avatarSource == value) return;
                _avatarSource = value;
                OnPropertyChanged(nameof(AvatarSource));
                OnPropertyChanged(nameof(AvatarImage));
                OnPropertyChanged(nameof(HasAvatar));
            }
        }

        public bool HasAvatar => !string.IsNullOrWhiteSpace(_avatarSource);
        public string Initials { get { return _initials; } set { Set(ref _initials, value); } }

        public ImageSource AvatarImage => string.IsNullOrWhiteSpace(_avatarSource) ? null : ImageSource.FromFile(_avatarSource);

        string _presence;                 // "Online"/"Away"/"Busy"/"Offline"
        public string Presence
        {
            get => _presence;
            set
            {
                if (_presence == value) return;
                _presence = value;
                OnPropertyChanged(nameof(Presence));
                OnPropertyChanged(nameof(PresenceDisplay));
                OnPropertyChanged(nameof(PresenceColor));
            }
        }

        public string PresenceDisplay
        {
            get
            {
                var p = (_presence ?? "").ToLowerInvariant();
                if (p == "online") return "Active";
                if (p == "away" || p == "idle") return "Idle";
                if (p == "busy" || p == "dnd" || p == "do not disturb" || p == "donotdisturb") return "Do Not Disturb";
                if (p == "offline" || p == "invisible") return "Invisible";
                return "Status";
            }
        }

        // Цвет точки статуса — без конвертера
        public Color PresenceColor
        {
            get
            {
                var p = (_presence ?? "").Trim().ToLowerInvariant();

                Debug.WriteLine("ProfileViewModel PresenceColor "+ p);

                switch (p)
                {
                    case "online":
                    case "available": 
                        return Color.FromHex("#34C759");
                    case "away":
                    case "idle": 
                        return Color.FromHex("#FFCC00");
                    case "busy":
                    case "dnd":
                    case "do not disturb":
                    case "donotdisturb":
                        return Color.FromHex("#FF3B30");
                    case "offline":
                    case "invisible":
                    default: 
                        return Color.FromHex("#AEAEB2");
                }
            }
        }

        public async Task InitializeAsync(INavigation nav)
        {
            _nav = nav;
            await LoadAsync().ConfigureAwait(false);

            Device.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(AvatarImage));
                OnPropertyChanged(nameof(PresenceDisplay));

                if (!_presenceSubscribed)
                {
                    _presenceSubscribed = true;

                    // получаем новый статус из шита и обновляем UI
                    MessagingCenter.Subscribe<object, string>(this, "PresenceChanged", (s, newPresence) =>
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            Presence = CanonicalPresence(newPresence); // обновит PresenceDisplay/PresenceColor
                        });
                    });
                }
            });
        }

        private string CanonicalPresence(string p)
        {
            if (string.IsNullOrWhiteSpace(p)) return "Offline";
            p = p.Trim().ToLowerInvariant();
            if (p == "online") return "Online";
            if (p == "away" || p == "idle") return "Away";
            if (p == "busy" || p == "dnd" || p.Contains("do not disturb")) return "Busy";
            return "Offline";
        }

        async Task LoadAsync()
        {
            var user = await _repo.GetCurrentUserProfileAsync();
            if (user == null) return;

            DisplayName = string.Format("{0} {1}", user.FirstName, user.LastName).Trim();
            Initials = AvatarIconBuilder.MakeInitials(DisplayName);
            AvatarSource = user.AvatarUrl;
            Presence = user.Presence.ToString();
        }

        async Task ChangeStatusAsync()
        {
            /*var selection = await Application.Current.MainPage.DisplayActionSheet(
                "Change Online Status", "Cancel", null,
                "Online", "Idle", "Do Not Disturb", "Invisible", "Set a custom status");

            if (string.IsNullOrEmpty(selection) || selection == "Cancel")
                return;

            if (selection == "Set a custom status")
            {
                await _nav.PushAsync(new CustomStatusPage());
                return;
            }

            if (selection == "Online") await SetPresenceAsync("Online");
            else if (selection == "Idle") await SetPresenceAsync("Away");
            else if (selection == "Do Not Disturb") await SetPresenceAsync("Busy");
            else if (selection == "Invisible") await SetPresenceAsync("Offline");*/

            // Показываем айшит как модалку с бэкдропом
            var mainNav = Application.Current.MainPage?.Navigation ?? _nav;
            await mainNav.PushModalAsync(new Views.Sheets.StatusBottomSheetPage(_nav), false);
        }

        async Task SetPresenceAsync(string presence)
        {
            try
            {
                await _repo.UpdatePresenceAsync(presence);
                Presence = presence;

                MessagingCenter.Send<object>(this, "ProfileChanged");
            }
            catch
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to update status.", "OK");
            }
        }

        async Task OpenEditProfileAsync()
        {
            try
            {
                //await _nav.PushAsync(new EditProfilePage()); // если у тебя другой класс — подставь свой
            }
            catch
            {
                await Application.Current.MainPage.DisplayAlert("Info", "Edit profile page is not wired yet.", "OK");
            }
        }

        async Task OpenSettingsAsync()
        {
            try
            {
                //await _nav.PushAsync(new SettingsPage()); // подставь свой
            }
            catch
            {
                await Application.Current.MainPage.DisplayAlert("Info", "Settings page is not wired yet.", "OK");
            }
        }

        // helpers
        void Set<T>(ref T backing, T value, [CallerMemberName] string prop = null)
        {
            if (!object.Equals(backing, value))
            {
                backing = value;
                OnPropertyChanged(prop);
            }
        }
        void OnPropertyChanged([CallerMemberName] string prop = null)
        {
            var ev = PropertyChanged;
            if (ev != null) ev(this, new PropertyChangedEventArgs(prop));
        }
    }
}