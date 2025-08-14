using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using System.Globalization;
using System.Diagnostics;
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

        private User _peer;
        public User Peer
        {
            get => _peer;
            set
            {
                if (SetProperty(ref _peer, value))
                {
                    OnPropertyChanged(nameof(PeerDisplayName));
                    OnPropertyChanged(nameof(PeerAvatarUrl));
                    OnPropertyChanged(nameof(PeerHasAvatar));
                    OnPropertyChanged(nameof(PeerNoAvatar));
                    OnPropertyChanged(nameof(PeerInitials));
                }
            }
        }

        // Прокси под биндинг
        public string PeerDisplayName => Peer?.DisplayName;
        public string PeerAvatarUrl => Peer?.AvatarUrl;
        public bool PeerHasAvatar => Peer?.HasAvatar ?? false;
        public bool PeerNoAvatar => !PeerHasAvatar;
        public string PeerInitials => Peer?.Initials;

        public ObservableRangeCollection<ChatMessage> Messages { get; } = new ObservableRangeCollection<ChatMessage>();

        private string _peerStatus;
        public string PeerStatus
        {
            get => _peerStatus;
            set { if (_peerStatus != value) { _peerStatus = value; OnPropertyChanged(); } }
        }

        private string _peerName;
        public string PeerName
        {
            get => _peerName;
            set { if (_peerName != value) { _peerName = value; OnPropertyChanged(); } }
        }


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

        public ChatViewModel(ChatRepository repo, string chatId, string currentUserId)
        {
            _repo = repo;
            _chatId = chatId;
            _currentUserId = currentUserId;

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

        private async Task<User> ResolvePeerAsync()
        {
            if (string.IsNullOrWhiteSpace(_chatId)) return null;

            var userId = await _repo.GetPeerUserIdFromParticipantsAsync(_chatId);

            var u = await _repo.GetUserAsync(userId.ToString());
            return u;
        }

        public async Task SetHeaderAsync()
        {
            var user = await ResolvePeerAsync();
            if (user == null) return;

            Device.BeginInvokeOnMainThread(() => Peer = user);

            string title = user.DisplayName;
            string status = user.HasStatus ? user.DisplayStatus : "Online";

            if (!string.IsNullOrWhiteSpace(title)) Device.BeginInvokeOnMainThread(() => PeerName = title);
            if (!string.IsNullOrWhiteSpace(title)) Device.BeginInvokeOnMainThread(() => PeerStatus = status);
        }

        public async Task LoadMessagesAsync()
        {
            IsMessagesLoading = true;
            try
            {
                Messages.Clear();
                _beforeCursor = null;
                CanLoadMore = true;

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

            Device.BeginInvokeOnMainThread(() =>
            {
                MessagingCenter.Send(this, "ScrollToEnd");
            });

            MessageText = string.Empty;
        }

        private async Task PickAndSendDocumentAsync()
        {
            try
            {
                var picked = await FilePicker.PickAsync(new PickOptions { PickerTitle = "Выберите документ" });
                if (picked == null) return;

                var fileName = string.IsNullOrWhiteSpace(picked.FileName) ? "Document" : picked.FileName;

                /*var now = DateTime.UtcNow;
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
                });*/

                Device.BeginInvokeOnMainThread(() =>
                {
                    MessagingCenter.Send(this, "ScrollToEnd");
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