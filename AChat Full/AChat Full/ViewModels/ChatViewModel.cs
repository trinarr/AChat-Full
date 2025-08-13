using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using System.Globalization;
using System.Diagnostics;
using System.Threading;
using System.IO;
using Xamarin.Essentials;
using AChatFull.Views;
using AChatFull.Utils;

namespace AChatFull.ViewModels
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        private readonly ChatRepository _repo;
        private readonly string _chatId;
        private readonly string _currentUserId;

        public ObservableRangeCollection<ChatMessage> Messages { get; } = new ObservableRangeCollection<ChatMessage>();

        public string PeerName { get; private set; }
        public string PeerStatus { get; private set; }

        private string _messageText;
        public string MessageText
        {
            get { return _messageText; }
            set { SetProperty(ref _messageText, value); }
        }

        public ICommand SendCommand { get; }
        public ICommand AttachDocumentCommand { get; }
        public ICommand DownloadDocumentCommand { get; }
        public ICommand OpenDocumentCommand { get; }
        public ICommand LoadMessagesCommand { get; }

        public bool IsMessagesLoading
        {
            get { return _isMessagesLoading; }
            set { SetProperty(ref _isMessagesLoading, value); }
        }
        private bool _isMessagesLoading;

        public bool CanLoadMore
        {
            get { return _canLoadMore; }
            set { SetProperty(ref _canLoadMore, value); }
        }
        private bool _canLoadMore = true;

        public int PageSize { get; set; } = 50;

        // Курсор: минимальная дата CreatedAt среди уже загруженных (формат "yyyy-MM-dd HH:mm:ss")
        private string _beforeCursor;

        public ChatViewModel() { }

        public ChatViewModel(ChatRepository repo, string chatId, string currentUserId, string peerName)
        {
            _repo = repo;
            _chatId = chatId;
            _currentUserId = currentUserId;

            PeerName = peerName;
            PeerStatus = "Не в сети";

            SendCommand = new Command(async () => await SendMessageAsync(), () => !string.IsNullOrWhiteSpace(MessageText));
            LoadMessagesCommand = new Command(async () => await LoadMessagesAsync());

            AttachDocumentCommand = new Command(async () => await PickAndSendDocumentAsync());
            DownloadDocumentCommand = new Command<ChatMessage>(async msg => await DownloadDocumentAsync(msg));
            OpenDocumentCommand = new Command<ChatMessage>(async msg => await OpenDocumentAsync(msg));

            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MessageText))
                    ((Command)SendCommand).ChangeCanExecute();
            };
        }

        public async Task LoadMessagesAsync()
        {
            IsMessagesLoading = true;
            try
            {
                Messages.Clear();
                _beforeCursor = null;
                CanLoadMore = true;

                // Берём последние PageSize сообщений (DESC), потом показываем по возрастанию
                var raw = await _repo.GetMessagesForChatAsync(_chatId, PageSize, null);

                var list = new System.Collections.Generic.List<ChatMessage>();
                foreach (var m in raw)
                    list.Add(MapToVm(m));
                list.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
                Messages.ReplaceRange(list);

                if (raw.Count > 0)
                {
                    string minCreatedAt = raw[0].CreatedAt;
                    for (int i = 1; i < raw.Count; i++)
                        if (string.Compare(raw[i].CreatedAt, minCreatedAt, StringComparison.Ordinal) < 0)
                            minCreatedAt = raw[i].CreatedAt;
                    _beforeCursor = minCreatedAt;
                }

                CanLoadMore = raw.Count == PageSize;
            }
            finally
            {
                IsMessagesLoading = false;
            }
        }

        public async Task<int> LoadMoreOlderAsync()
        {
            if (!CanLoadMore || IsMessagesLoading) return 0;

            IsMessagesLoading = true;
            try
            {
                var raw = await _repo.GetMessagesForChatAsync(_chatId, PageSize, _beforeCursor);
                if (raw.Count == 0)
                {
                    CanLoadMore = false;
                    return 0;
                }

                var olderAsc = new System.Collections.Generic.List<ChatMessage>();
                foreach (var m in raw)
                    olderAsc.Add(MapToVm(m));
                olderAsc.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

                Messages.InsertRange(0, olderAsc);

                string minCreatedAt = raw[0].CreatedAt;
                for (int i = 1; i < raw.Count; i++)
                    if (string.Compare(raw[i].CreatedAt, minCreatedAt, StringComparison.Ordinal) < 0)
                        minCreatedAt = raw[i].CreatedAt;
                _beforeCursor = minCreatedAt;

                CanLoadMore = raw.Count == PageSize;
                return olderAsc.Count;
            }
            finally
            {
                IsMessagesLoading = false;
            }
        }

        private ChatMessage MapToVm(Message m)
        {
            var vm = new ChatMessage
            {
                Kind = (MessageKind)m.Type,
                Text = m.Text,
                IsIncoming = m.SenderId != _currentUserId,
                Timestamp = m.CreatedAtDate
            };

            if (m.Type == (int)MessageKind.Document)
            {
                vm.Document = new DocumentInfo
                {
                    FileName = m.FileName,
                    FileSize = m.FileSize,
                    MimeType = m.MimeType,
                    RemoteUrl = m.RemoteUrl,
                    LocalPath = m.LocalPath
                };
            }
            return vm;
        }

        private async Task SendMessageAsync()
        {
            var text = MessageText == null ? null : MessageText.Trim();
            if (string.IsNullOrEmpty(text)) return;

            var now = DateTime.UtcNow;
            var msg = new Message
            {
                ChatId = _chatId,
                SenderId = _currentUserId,
                Text = text,
                CreatedAt = now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                Type = (int)MessageKind.Text
            };

            await _repo.InsertMessageAsync(msg);

            Messages.Add(new ChatMessage
            {
                Kind = MessageKind.Text,
                Text = text,
                IsIncoming = false,
                Timestamp = now
            });

            MessageText = string.Empty;
        }

        private async Task PickAndSendDocumentAsync()
        {
            try
            {
                var picked = await FilePicker.PickAsync(new PickOptions { PickerTitle = "Выберите документ" });
                if (picked == null) return;

                var fileName = string.IsNullOrWhiteSpace(picked.FileName) ? "Документ" : picked.FileName;

                var now = DateTime.UtcNow;
                var msg = new Message
                {
                    ChatId = _chatId,
                    SenderId = _currentUserId,
                    CreatedAt = now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    Type = (int)MessageKind.Document,
                    FileName = fileName,
                    FileSize = 0,
                    MimeType = null,
                    RemoteUrl = null,
                    LocalPath = null
                };
                await _repo.InsertMessageAsync(msg);

                Messages.Add(new ChatMessage
                {
                    Kind = MessageKind.Document,
                    IsIncoming = false,
                    Timestamp = now,
                    Document = new DocumentInfo
                    {
                        FileName = fileName,
                        FileSize = 0,
                        MimeType = null,
                        RemoteUrl = null,
                        LocalPath = null
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("PickAndSendDocumentAsync error: " + ex.Message);
            }
        }

        private async Task DownloadDocumentAsync(ChatMessage msg)
        {
            if (msg == null || msg.Document == null) return;
            await Task.CompletedTask;
        }

        private async Task OpenDocumentAsync(ChatMessage msg)
        {
            if (msg == null || msg.Document == null) return;

            if (!string.IsNullOrEmpty(msg.Document.LocalPath))
            {
                await Launcher.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(msg.Document.LocalPath)
                });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            var h = PropertyChanged;
            if (h != null)
                h(this, new PropertyChangedEventArgs(name));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (object.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }
    }
}