using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;              // для Command/Color
using AChatFull.Views;           // User (если у тебя в другом неймспейсе — поправь using)

namespace AChatFull.ViewModels
{
    public class CallsViewModel : INotifyPropertyChanged
    {
        private readonly ChatRepository _repo;

        public ObservableCollection<CallItem> Calls { get; } = new ObservableCollection<CallItem>();

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand CallCommand { get; }
        public ICommand NewCallCommand { get; }

        public CallsViewModel(ChatRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));

            RefreshCommand = new Command(async () => await LoadAsync());
            CallCommand = new Command<CallItem>(async c => await PlaceCallAsync(c));
            NewCallCommand = new Command(async () => await StartNewCallAsync());

            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                Calls.Clear();

                var history = await _repo.TryGetCallHistoryAsync(); // расширение ниже
                foreach (var h in history.OrderByDescending(x => x.When))
                    Calls.Add(h);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private Task PlaceCallAsync(CallItem item)
        {
            // TODO: вызов твоей логики звонка (VoIP/телефон/навигация)
            // например: return _repo.CallAsync(item.UserId ?? item.Phone);
            return Task.CompletedTask;
        }

        private Task StartNewCallAsync()
        {
            // TODO: открыть экран нового звонка/набора номера
            return Task.CompletedTask;
        }

        #region INotifyPropertyChanged helpers
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string name = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(name);
            return true;
        }
        #endregion
    }

    /// <summary>Модель элемента списка звонков под XAML.</summary>
    public class CallItem
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Title { get; set; }            // Имя/номер
        public string Subtitle { get; set; }         // Текст даты/времени для UI
        public DateTime When { get; set; }           // Для сортировки
        public string AvatarUrl { get; set; }
        public string Initials { get; set; }
        public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarUrl);

        public bool IsOutgoing { get; set; }
        public bool IsMissed { get; set; }
        public bool IsVideo { get; set; }

        public string ArrowGlyph => IsOutgoing ? "↗" : "↙";
        public Color ArrowColor => IsMissed ? Color.FromHex("#EF4444") : Color.FromHex("#22C55E");

        public string Phone { get; set; }

        public static CallItem From(User user, DateTime when, bool outgoing, bool missed, bool video = false, string phone = null)
        {
            var title = !string.IsNullOrWhiteSpace(user?.DisplayName) ? user.DisplayName : phone ?? "Неизвестный";
            return new CallItem
            {
                Id = Guid.NewGuid().ToString("N"),
                UserId = user?.UserId,
                Title = title,
                Subtitle = when.ToString("d MMMM, HH:mm", new CultureInfo("ru-RU")),
                When = when,
                AvatarUrl = user?.AvatarUrl,
                Initials = MakeInitials(title),
                IsOutgoing = outgoing,
                IsMissed = missed,
                IsVideo = video,
                Phone = phone
            };
        }

        static string MakeInitials(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "?";
            var parts = s.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpperInvariant();
            return (parts[0].Substring(0, 1) + parts[1].Substring(0, 1)).ToUpperInvariant();
        }
    }

    /// <summary>
    /// Временная заглушка: берём контакты из репозитория и генерим «недавние».
    /// Заменишь содержимое на свой реальный метод истории звонков, когда будет готов.
    /// </summary>
    public static class ChatRepositoryCallsExtensions
    {
        public static async Task<CallItem[]> TryGetCallHistoryAsync(this ChatRepository repo)
        {
            try
            {
                var users = await repo.GetContactsAsync(); // поменяй на свой метод, если имя другое
                var now = DateTime.Now;
                var rnd = new Random();

                var demo = users.Take(12).Select(u =>
                {
                    var when = now.AddDays(-rnd.Next(0, 120)).AddMinutes(-rnd.Next(0, 1800));
                    var outgoing = rnd.Next(0, 2) == 0;
                    var missed = rnd.Next(0, 4) == 0;
                    return CallItem.From(u, when, outgoing, missed);
                }).OrderByDescending(x => x.When).ToArray();

                return demo;
            }
            catch
            {
                return Array.Empty<CallItem>();
            }
        }
    }
}