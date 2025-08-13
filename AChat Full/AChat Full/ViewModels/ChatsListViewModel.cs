using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Forms;
using AChatFull.Views;
using System.Linq;
using System.Collections.Generic;

namespace AChatFull.ViewModels
{
    public class ChatsListViewModel : INotifyPropertyChanged
    {
        // Коллекция, к которой биндится UI
        public ObservableCollection<ChatSummary> Chats { get; } = new ObservableCollection<ChatSummary>();

        // Полный набор данных для фильтрации
        private List<ChatSummary> _allChats = new List<ChatSummary>();

        private readonly ChatRepository _repo;

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        private bool _isSearchMode;
        public bool IsSearchMode
        {
            get => _isSearchMode;
            set { if (_isSearchMode != value) { _isSearchMode = value; OnPropertyChanged(); } }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    ApplyFilter(); // фильтруем при вводе
                }
            }
        }

        private void ApplyFilter()
        {
            var t = _searchText?.Trim();
            IEnumerable<ChatSummary> filtered = _allChats;

            if (!string.IsNullOrEmpty(t))
            {
                var tl = t.ToLowerInvariant();
                filtered = _allChats.Where(c =>
                    (!string.IsNullOrEmpty(c.Title) && c.Title.ToLowerInvariant().Contains(tl)) ||
                    (!string.IsNullOrEmpty(c.LastMessage) && c.LastMessage.ToLowerInvariant().Contains(tl)));
            }

            // Обновляем видимую коллекцию (ObservableCollection сообщит UI об изменениях)
            Chats.Clear();
            foreach (var item in filtered)
                Chats.Add(item);
        }

        public ChatsListViewModel(ChatRepository repo, string currentUserId)
        {
            _repo = repo;

            MessagingCenter.Subscribe<ChatPage, string>(this, "ChatClosed", (_, __) =>
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(150);
                    await LoadChatsAsync();
                });
            });
        }

        public async Task LoadChatsAsync()
        {
            IsBusy = true;
            try
            {
                var data = await _repo.GetChatSummariesAsync();
                _allChats = data?.ToList() ?? new List<ChatSummary>();
                ApplyFilter(); // показать с текущим SearchText (или всё, если пусто)
            }
            finally
            {
                IsBusy = false;
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        #endregion
    }
}