using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Forms;
using AChatFull.Views; 

namespace AChatFull.ViewModels
{
    public class ChatsListViewModel : INotifyPropertyChanged
    {
        //private readonly SignalRChatClient _chatClient;
        public ObservableCollection<ChatSummary> Chats { get; } = new ObservableCollection<ChatSummary>();
        private readonly ChatRepository _repo;
        private readonly string _currentUserId;

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        //public ICommand LoadChatsCommand { get; }

        public ChatsListViewModel(ChatRepository repo, string currentUserId)
        {
            _repo = repo;
            _currentUserId = currentUserId;

            // ПОДПИСКА: когда ChatPage закрывается — подождём чуть-чуть и обновим список
            Xamarin.Forms.MessagingCenter.Subscribe<ChatPage, string>(this, "ChatClosed", (_, __) =>
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    // небольшая пауза, чтобы закрытие завершилось и UI «успокоился»
                    await Task.Delay(150);
                    await LoadChatsAsync();
                });
            });

            /*LoadMessagesCommand = new Command(async () => await LoadMessagesAsync());
            SendCommand = new Command(async () => await SendMessageAsync(),
                                              () => !string.IsNullOrWhiteSpace(MessageText));

            // Чтобы автоматически обновлять доступность команды Send
            this.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MessageText))
                    ((Command)SendCommand).ChangeCanExecute();
            };*/
        }

        public async Task LoadChatsAsync()
        {
            IsBusy = true;

            try
            {
                var data = await _repo.GetChatSummariesAsync();
                Chats.Clear();
                foreach (var chat in data)
                    Chats.Add(chat);
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