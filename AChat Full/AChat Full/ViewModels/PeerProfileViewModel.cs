using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Forms;
using AChatFull.Views; 
using System.Diagnostics;

namespace AChatFull.ViewModels
{
    public class PeerProfileViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly ChatRepository _repo;
        private readonly string _userId;

        public PeerProfileViewModel(ChatRepository repo, string userId)
        {
            _repo = repo;
            _userId = userId;
        }

        public async Task InitializeAsync()
        {
            var u = await _repo.GetUserAsync(_userId);
            if (u == null) return;

            // если в данных есть First/Last, соберём из них; иначе берём DisplayName
            var fn = (u.FirstName ?? "").Trim();
            var ln = (u.LastName ?? "").Trim();

            var composed = $"{fn} {ln}".Trim();
            FullName = string.IsNullOrWhiteSpace(composed) ? (u.DisplayName ?? "") : composed;

            //DisplayName = u.DisplayName;
            About = u.About;
            StatusCustom = u.StatusCustom;
            Presence = u.Presence;
            AvatarSource = u.AvatarUrl;

            Debug.WriteLine("PeerProfileViewModel InitializeAsync "+ About+" | "+ StatusCustom+" | "+ Presence);

            // В UserDto дата уже нормализована к "dd.MM.yyyy" или null
            BirthdateFormatted = string.IsNullOrWhiteSpace(u.Birthdate) ? null : u.Birthdate;

            // Запустить обновление видимости
            OnPropertyChanged(nameof(ShowBirthdate));
            OnPropertyChanged(nameof(ShowAbout));
            OnPropertyChanged(nameof(ShowCustomStatus));
            OnPropertyChanged(nameof(HasAvatar));
            OnPropertyChanged(nameof(AvatarImage));
            OnPropertyChanged(nameof(Initials));
            OnPropertyChanged(nameof(PresenceText));
        }

        // --- Отображаемые свойства ---
        string _displayName;
        public string DisplayName { get => _displayName; set => Set(ref _displayName, value); }

        string _firstName;
        public string FirstName { get => _firstName; set => Set(ref _firstName, value); }

        string _lastName;
        public string LastName { get => _lastName; set => Set(ref _lastName, value); }

        string _about;
        public string About
        {
            get => _about;
            set
            {
                Set(ref _about, value);
                OnPropertyChanged(nameof(ShowAbout));
                OnPropertyChanged(nameof(ShowAboutBirthdateDivider)); // <- добавили
            }
        }

        string _statusCustom;
        public string StatusCustom { get => _statusCustom; set => Set(ref _statusCustom, value); }

        string _birthdateFormatted;
        public string BirthdateFormatted
        {
            get => _birthdateFormatted;
            set
            {
                Set(ref _birthdateFormatted, value);
                OnPropertyChanged(nameof(ShowBirthdate));
                OnPropertyChanged(nameof(ShowAboutBirthdateDivider)); // <- добавили
            }
        }

        Presence _presence;
        public Presence Presence { get => _presence; set => Set(ref _presence, value); }

        string _avatarSource;
        public string AvatarSource
        {
            get => _avatarSource;
            set
            {
                if (_avatarSource == value) return;
                _avatarSource = value;
                OnPropertyChanged(nameof(AvatarSource));
                OnPropertyChanged(nameof(AvatarImage));
                OnPropertyChanged(nameof(HasAvatar));
                OnPropertyChanged(nameof(Initials));
            }
        }

        public bool HasAvatar => !string.IsNullOrWhiteSpace(_avatarSource);
        public ImageSource AvatarImage => string.IsNullOrWhiteSpace(_avatarSource) ? null : ImageSource.FromFile(_avatarSource);

        public string Initials
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(FirstName)) return FirstName.Substring(0, 1).ToUpperInvariant();
                if (!string.IsNullOrWhiteSpace(LastName)) return LastName.Substring(0, 1).ToUpperInvariant();
                if (!string.IsNullOrWhiteSpace(DisplayName)) return DisplayName.Substring(0, 1).ToUpperInvariant();
                return "?";
            }
        }

        public string PresenceText
        {
            get
            {
                switch (Presence)
                {
                    case Presence.Online: return "Online";
                    case Presence.Idle: return "Away";   // Idle == Away
                    case Presence.DoNotDisturb: return "Busy";   // DND == Busy
                    default: return "Offline";
                }
            }
        }

        string _fullName;
        public string FullName
        {
            get => _fullName;
            set => Set(ref _fullName, value);
        }
        public bool ShowAbout => !string.IsNullOrWhiteSpace(About);
        public bool ShowBirthdate => !string.IsNullOrWhiteSpace(BirthdateFormatted);
        public bool ShowCustomStatus => !string.IsNullOrWhiteSpace(StatusCustom);
        public bool ShowAboutBirthdateDivider => ShowAbout && ShowBirthdate;

        // --- boilerplate ---
        void Set<T>(ref T backing, T value, [CallerMemberName] string prop = null)
        {
            if (!object.Equals(backing, value))
            {
                backing = value;
                OnPropertyChanged(prop);
            }
        }
        void OnPropertyChanged([CallerMemberName] string prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
