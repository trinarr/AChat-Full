using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using System.Diagnostics;

namespace AChatFull.ViewModels
{
    public class ContactsViewModel : INotifyPropertyChanged
    {
        private readonly Views.ChatRepository _repo;
        private string _searchText;
        private bool _isSearching;
        private string _sortMode = "name"; // name | lastseen
        private bool _isSearchMode;
        private bool _isBusy;

        public ObservableCollection<Views.User> Items { get; } = new ObservableCollection<Views.User>();
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); }
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
                    _ = LoadAsync(); // обновляем список по мере ввода
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public bool IsSearching
        {
            get => _isSearching;
            set { if (_isSearching != value) { _isSearching = value; OnPropertyChanged(); } }
        }

        public string SortMode
        {
            get => _sortMode;
            set { if (_sortMode != value) { _sortMode = value; OnPropertyChanged(); _ = LoadAsync(); } }
        }

        public ICommand RefreshCommand { get; }
        public ICommand ToggleSearchCommand { get; }
        public ICommand SortByNameCommand { get; }
        public ICommand SortByLastSeenCommand { get; }

        public ContactsViewModel(Views.ChatRepository repo)
        {
            _repo = repo;
            ToggleSearchCommand = new Command(() => IsSearching = !IsSearching);
            SortByNameCommand = new Command(() => SortMode = "name");
            SortByLastSeenCommand = new Command(() => SortMode = "lastseen");

            RefreshCommand = new Command(async () => await LoadAsync(force: true));
        }

        public async Task LoadAsync()
        {
            Debug.WriteLine("TESTLOG ContactsViewModel LoadAsync");

            var data = await _repo.GetContactsAsync(SearchText, SortMode);
            Debug.WriteLine($"TESTLOG repo returned: {data?.Count}");
            Items.Clear();
            foreach (var u in data)
                Items.Add(u);

            Debug.WriteLine($"TESTLOG Items after fill: {Items.Count}");
        }

        private bool _loadedOnce;

        public async Task LoadAsync(bool force = false)
        {
            if (IsBusy) return;
            if (!force && _loadedOnce) return; // чтобы не дёргать БД каждый раз, если не нужно

            try
            {
                IsBusy = true;

                var list = await _repo.GetContactsAsync(
                    search: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                    sort: "name" // или по-другому, если нужно
                );

                Items.Clear();
                foreach (var u in list)
                    Items.Add(u);

                _loadedOnce = true;
            }
            finally { IsBusy = false; }
        }

        public async Task SearchAsync(string text)
        {
            SearchText = text;
            _loadedOnce = false; // чтобы перезагрузить с фильтром
            await LoadAsync(force: true);
        }
    }
}
