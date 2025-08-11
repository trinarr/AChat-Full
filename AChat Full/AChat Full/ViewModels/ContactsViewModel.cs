
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using System.Linq;

namespace AChatFull.ViewModels
{
    public class ContactsViewModel : INotifyPropertyChanged
    {
        private readonly AChatFull.Views.ChatRepository _repo;
        private string _searchText;
        private bool _isSearching;
        private string _sortMode = "name"; // name | lastseen
        private bool _isSearchMode;

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

        public ObservableCollection<AChatFull.Views.Contact> Items { get; } = new ObservableCollection<AChatFull.Views.Contact>();

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

        public ICommand ToggleSearchCommand { get; }
        public ICommand SortByNameCommand { get; }
        public ICommand SortByLastSeenCommand { get; }

        public ContactsViewModel(AChatFull.Views.ChatRepository repo)
        {
            _repo = repo;
            ToggleSearchCommand = new Command(() => IsSearching = !IsSearching);
            SortByNameCommand = new Command(() => SortMode = "name");
            SortByLastSeenCommand = new Command(() => SortMode = "lastseen");
        }

        public async Task LoadAsync()
        {
            var data = await _repo.GetContactsAsync(SearchText, SortMode);
            // простой reconcile
            Items.Clear();
            foreach (var c in data)
                Items.Add(c);
        }
    }
}
