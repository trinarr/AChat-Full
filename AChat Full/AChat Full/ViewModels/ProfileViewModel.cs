using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Forms;
using AChatFull.Utils;
using AChatFull.Views;

namespace AChatFull.ViewModels
{
    public class ProfileViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private readonly ChatRepository _repo;

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
        string _presence; // "Online"/"Away"/"Busy"/"Offline"

        public string DisplayName { get { return _displayName; } set { Set(ref _displayName, value); } }
        public string AvatarSource { get { return _avatarSource; } set { Set(ref _avatarSource, value); OnPropertyChanged("AvatarImage"); } }
        public string Initials { get { return _initials; } set { Set(ref _initials, value); } }
        public string Presence { get { return _presence; } set { Set(ref _presence, value); OnPropertyChanged("PresenceDisplay"); } }

        // Привязка для <Image Source=...> (если пусто — покажем инициалы)
        public ImageSource AvatarImage
        {
            get
            {
                if (string.IsNullOrWhiteSpace(AvatarSource)) return null;
                return ImageSource.FromFile(AvatarSource);
            }
        }

        public string PresenceDisplay
        {
            get
            {
                var p = (Presence ?? "").ToLowerInvariant();
                switch (p)
                {
                    case "online": return "Active";
                    case "away": return "Idle";
                    case "busy": return "Do Not Disturb";
                    case "offline": return "Invisible";
                    default: return "Status";
                }
            }
        }

        public async Task InitializeAsync(INavigation nav)
        {
            _nav = nav;
            await LoadAsync().ConfigureAwait(false);
            // Возврат на UI-поток для обновления bound-свойств
            Device.BeginInvokeOnMainThread(() =>
            {
                // дергаем PropertyChanged для уже выставленных значений, если нужно
                OnPropertyChanged(nameof(AvatarImage));
                OnPropertyChanged(nameof(PresenceDisplay));
            });
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
            // стандартные + расширенный
            var selection = await Application.Current.MainPage.DisplayActionSheet(
                "Change Online Status", "Cancel", null,
                "Online", "Idle", "Do Not Disturb", "Invisible", "Set a custom status");

            if (string.IsNullOrEmpty(selection) || selection == "Cancel")
                return;

            if (selection == "Set a custom status")
            {
                await _nav.PushAsync(new CustomStatusPage());
                return;
            }

            // map и сохранить
            if (selection == "Online") await SetPresenceAsync("Online");
            else if (selection == "Idle") await SetPresenceAsync("Away");
            else if (selection == "Do Not Disturb") await SetPresenceAsync("Busy");
            else if (selection == "Invisible") await SetPresenceAsync("Offline");
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