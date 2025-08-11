using System;
using System.Collections.Generic;
using System.ComponentModel;
using AChatFull.Views;
using System.Windows.Input;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace AChatFull.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly ChatRepository _repo;

        // Профиль
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string BirthDate { get; set; }
        public string About { get; set; }
        public string Status { get; set; }

        // Предустановки статуса (как в WhatsApp)
        public IList<string> StatusPresets { get; } = new[]
        {
        "Доступен", "Занят", "На встрече", "Сплю", "Только срочно", "На связи", "Учусь", "Работаю"
    };

        // Настройки приложения
        public IList<string> Languages { get; } = new[] { "Русский", "English" };
        public string SelectedLanguage { get; set; }
        public bool VibrateOnMessage { get; set; } = true;
        public bool SoundOnMessage { get; set; } = true;

        public bool IsBusy { get; set; }
        public ICommand SaveCommand { get; }

        public SettingsViewModel(ChatRepository repo)
        {
            _repo = repo;
            SaveCommand = new Command(async () => await SaveAsync(), () => !IsBusy);
        }

        public async Task LoadAsync()
        {
            IsBusy = true; OnPropertyChanged(nameof(IsBusy));

            var u = await _repo.GetCurrentUserProfileAsync() ?? new User();
            FirstName = u.FirstName; OnPropertyChanged(nameof(FirstName));
            LastName = u.LastName; OnPropertyChanged(nameof(LastName));
            About = u.About; OnPropertyChanged(nameof(About));
            Status = u.Status; OnPropertyChanged(nameof(Status));
            BirthDate = u.BirthDate;

            /*var s = await _repo.GetAppSettingsAsync();
            SelectedLanguage = s.Language == "en" ? "English" : "Русский";
            VibrateOnMessage = s.VibrateOnMessage;
            SoundOnMessage = s.SoundOnMessage;
            OnPropertyChanged(nameof(SelectedLanguage));
            OnPropertyChanged(nameof(VibrateOnMessage));
            OnPropertyChanged(nameof(SoundOnMessage));*/

            IsBusy = false; OnPropertyChanged(nameof(IsBusy));
        }

        private async Task SaveAsync()
        {
            /*IsBusy = true; OnPropertyChanged(nameof(IsBusy));
            try
            {
                var u = await _repo.GetCurrentUserProfileAsync() ?? new User { UserId = _repo.CurrentUserId };
                u.FirstName = FirstName;
                u.LastName = LastName;
                u.About = About;
                u.Status = Status;
                u.BirthDateUnix = BirthDate.HasValue
                    ? new DateTimeOffset(BirthDate.Value.Date).ToUnixTimeSeconds()
                    : (long?)null;
                await _repo.SaveCurrentUserProfileAsync(u);

                var s = await _repo.GetAppSettingsAsync();
                s.Language = SelectedLanguage?.StartsWith("Eng") == true ? "en" : "ru";
                s.VibrateOnMessage = VibrateOnMessage;
                s.SoundOnMessage = SoundOnMessage;
                await _repo.SaveAppSettingsAsync(s);

                // (опционально) применить язык немедленно
                // LocalizationManager.SetCulture(s.Language);
            }
            finally { IsBusy = false; OnPropertyChanged(nameof(IsBusy)); }*/
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
