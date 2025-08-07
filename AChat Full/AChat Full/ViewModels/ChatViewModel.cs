using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using System.Globalization;
using System.Diagnostics;
using AChatFull.Views;

namespace AChatFull.ViewModels
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        private readonly ChatRepository _repo;
        private readonly string _chatId;
        private readonly string _currentUserId;

        public ObservableCollection<ChatMessage> Messages { get; }
            = new ObservableCollection<ChatMessage>();

        private string _peerName;
        public string PeerName
        {
            get => _peerName;
            set => SetProperty(ref _peerName, value);
        }

        private string _peerStatus;
        public string PeerStatus
        {
            get => _peerStatus;
            set => SetProperty(ref _peerStatus, value);
        }

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

        }

        public ChatViewModel(ChatRepository repo, string chatId, string currentUserId, string peerName)
        {
            _repo = repo;
            _chatId = chatId;
            _currentUserId = currentUserId;

            PeerName = peerName;
            PeerStatus = "Не в сети";

            SendCommand = new Command(async () => await SendMessageAsync(),
                                  () => !string.IsNullOrWhiteSpace(MessageText));
            LoadMessagesCommand = new Command(async () => await LoadMessagesAsync());

            // Чтобы автоматически обновлять доступность команды Send
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MessageText))
                    ((Command)SendCommand).ChangeCanExecute();
            };
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
            Debug.WriteLine("TESTLOG SendMessageAsync VM");

            var text = MessageText?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            // 1) Формируем новую запись Message
            var newMsg = new Message
            {
                ChatId = _chatId,
                SenderId = _currentUserId,
                Text = text,
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                //IsRead = true
            };

            // 2) Сохраняем её в локальную SQLite-базу
            await _repo.InsertMessageAsync(newMsg);

            // 3) Добавляем в коллекцию ObservableCollection<ChatMessage>,
            //    к которой привязан CollectionView
            Messages.Add(new ChatMessage
            {
                Text = text,
                IsIncoming = false,
                Timestamp = newMsg.CreatedAtDate
            });

            // 4) Очищаем поле ввода
            MessageText = string.Empty;
            MessagingCenter.Send(this, "ScrollToEnd");
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