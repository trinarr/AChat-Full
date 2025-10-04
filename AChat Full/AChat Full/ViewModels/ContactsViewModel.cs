using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using System.Linq;
using AChatFull.Views;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace AChatFull.ViewModels
{
    public class AlphaGroup<T> : ObservableCollection<T>
    {
        public string Key { get; }
        public AlphaGroup(string key) => Key = key;
        public AlphaGroup(string key, IEnumerable<T> items) : base(items) => Key = key;
    }

    public class ContactsViewModel : INotifyPropertyChanged
    {
        private readonly ChatRepository _repo;
        private CancellationTokenSource _cts;
        private bool _isBusy;
        private bool _isSearchMode;
        private string _searchText;

        public event PropertyChangedEventHandler PropertyChanged;

        public ContactsViewModel(ChatRepository repo)
        {
            _repo = repo;
            Contacts = new ObservableCollection<User>();
            SearchGroups = new ObservableCollection<UserGroup>();

            SearchCommand = new Command<string>(async (text) => await SearchAsync(text));
            ClearSearchCommand = new Command(async () => await SearchAsync(string.Empty));
            OpenChatCommand = new Command<User>(async (u) => await OpenChatAsync(u));
            Contacts.CollectionChanged += OnContactsChanged;

            // Первичная загрузка контактов
            _ = LoadContactsAsync();
        }

        private void OnContactsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildContactsGroups();
        }

        public ObservableCollection<User> Contacts { get; }

        // Группированные результаты поиска: [Контакты], [Пользователи]
        public ObservableCollection<UserGroup> SearchGroups { get; }

        public ObservableCollection<UserGroup> ContactsGroups { get; } = new ObservableCollection<UserGroup>();

        private void RebuildContactsGroups(IEnumerable<User> source = null)
        {
            var data = source ?? Contacts;

            var online = data
                .Where(u => u.Presence != Presence.Offline && u.Presence != Presence.Invisible)
                .OrderBy(u => u.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            var offline = data
                .Where(u => u.Presence == Presence.Offline || u.Presence == Presence.Invisible)
                .OrderBy(u => u.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            Device.BeginInvokeOnMainThread(() =>
            {
                ContactsGroups.Clear();
                if (online.Count > 0)
                    ContactsGroups.Add(new UserGroup("Online", online));
                if (offline.Count > 0)
                    ContactsGroups.Add(new UserGroup("Offline", offline));
            });
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { if (_isBusy != value) { _isBusy = value; OnPropertyChanged(); } }
        }

        public bool IsSearchMode
        {
            get => _isSearchMode;
            set { if (_isSearchMode != value) { _isSearchMode = value; OnPropertyChanged(); } }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    // дебаунс по набору
                    _ = DebouncedSearchAsync(_searchText);
                }
            }
        }

        public ICommand SearchCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand OpenChatCommand { get; }

        public ObservableCollection<AlphaGroup<User>> ContactsGrouped { get; } = new ObservableCollection<AlphaGroup<User>>();
        public ObservableCollection<User> SearchResults { get; } = new ObservableCollection<User>();

        public async Task LoadContactsAsync()
        {
            try
            {
                IsBusy = true;

                var contacts = await _repo.GetContactsAsync();

                // алфавитная сортировка по отображаемому имени (без учёта регистра)
                var sorted = contacts
                    .OrderBy(u => u.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

                Contacts.Clear();
                foreach (var c in sorted)
                    Contacts.Add(c);

                // Можно оставить старую группировку в коде, но НЕ использовать её в XAML
                // BuildContactGroups(Contacts);
            }
            finally { 
                IsBusy = false;
                RebuildContactsGroups();
            }
        }

        private async Task DebouncedSearchAsync(string text)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            try
            {
                await Task.Delay(250, _cts.Token);
                await SearchAsync(text);
            }
            catch (TaskCanceledException) { }
        }

        public async Task SearchAsync(string text)
        {
            try
            {
                IsBusy = true;
                SearchGroups.Clear();

                if (text == null)
                {
                    IsSearchMode = false;
                    return;
                }

                var all = await _repo.SearchUsersAsync(text, limit: 200);

                if (!string.IsNullOrWhiteSpace(App.USER_TOKEN_TEST))
                    all = all.Where(u => !string.Equals(u.UserId, App.USER_TOKEN_TEST, StringComparison.OrdinalIgnoreCase))
                             .ToList();

                var contacts = all.Where(u => u.IsContact == 1)
                                  .OrderBy(u => u.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                                  .ToList();

                var others = all.Where(u => u.IsContact == 0)
                                .OrderBy(u => u.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                                .ToList();

                if (contacts.Count > 0)
                    SearchGroups.Add(new UserGroup("Contacts", contacts));

                if (others.Count > 0)
                    SearchGroups.Add(new UserGroup("Global Search", others));
            }
            finally { IsBusy = false; }
        }

        private async Task OpenChatAsync(User user)
        {
            if (user == null) return;
            // Открыть/создать диалог
            var chatId = await _repo.GetOrCreateDirectChatIdAsync(user.UserId);
            MessagingCenter.Send(this, "OpenChat", chatId);
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class UserGroup : ObservableCollection<User>
    {
        public string Title { get; }
        public UserGroup(string title) { Title = title; }
        public UserGroup(string title, IEnumerable<User> items = null)
        : base(items ?? Enumerable.Empty<User>())
        {
            Title = title;
        }
    }
}