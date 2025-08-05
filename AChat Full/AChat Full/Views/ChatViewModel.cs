using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace AChatFull.Views
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        private readonly ChatRepository _repo;
        private readonly string _chatId;
        private readonly string _currentUserId;

        public ObservableCollection<ChatMessage> Messages { get; }
            = new ObservableCollection<ChatMessage>();

        private string _messageText;
        public string MessageText
        {
            get => _messageText;
            set => SetProperty(ref _messageText, value);
        }

        public ICommand LoadMessagesCommand { get; }
        public ICommand SendCommand { get; }

        public ChatViewModel()
        {
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

        public ChatViewModel(ChatRepository repo, string chatId, string currentUserId)
        {
            _repo = repo;
            _chatId = chatId;
            _currentUserId = currentUserId;

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

        /// <summary>
        /// Загружает историю сообщений из базы.
        /// </summary>
        public async Task LoadMessagesAsync()
        {
            Messages.Clear();
            var list = await _repo.GetMessagesForChatAsync(_chatId);
            foreach (var m in list)
            {
                Messages.Add(new ChatMessage
                {
                    Text = m.Text,
                    IsIncoming = m.SenderId != _currentUserId,
                    Timestamp = m.CreatedAtDate
                });
            }
        }

        /// <summary>
        /// Отправляет новое сообщение: сохраняет в БД и добавляет в коллекцию.
        /// </summary>
        private async Task SendMessageAsync()
        {
            var text = MessageText?.Trim();
            if (string.IsNullOrEmpty(text))
                return;

            // 1) Сохраняем в БД
            /*var newMsg = new Message
            {
                ChatId = _chatId,
                SenderId = _currentUserId,
                Text = text,
                CreatedAt = DateTime.UtcNow,
                IsRead = true
            };
            await _repo.InsertMessageAsync(newMsg);*/

            // 2) Добавляем в UI
            /*Messages.Add(new ChatMessage
            {
                Text = text,
                IsIncoming = false,
                Timestamp = newMsg
            });

            MessageText = string.Empty;*/
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }

        #endregion
    }
}