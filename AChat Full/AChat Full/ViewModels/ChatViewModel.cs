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
using System.Threading;
using System.IO;
using Xamarin.Essentials;

namespace AChatFull.ViewModels
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        private readonly ChatRepository _repo;
        private readonly string _chatId;
        private readonly string _currentUserId;
        private readonly bool _useDocumentPlaceholders = true;

        private readonly IChatTransport _transport;
        private readonly IFileService _files;

        public interface IFileService
        {
            Task<string> UploadAsync(Stream content, string fileName, string mime, IProgress<double> progress, CancellationToken ct);
            Task<string> DownloadAsync(string url, string fileName, IProgress<double> progress, CancellationToken ct);
        }

        public interface IChatTransport  
        {
            Task SendTextAsync(string chatId, string text);
            Task SendDocumentAsync(string chatId, string remoteUrl, string fileName, long size, string mime);
        }

        public ICommand SendCommand { get; }
        public ICommand AttachDocumentCommand { get; }
        public ICommand DownloadDocumentCommand { get; }
        public ICommand OpenDocumentCommand { get; }
        public ICommand LoadMessagesCommand { get; }

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

            AttachDocumentCommand = new Command(async () => await PickAndSendDocumentAsync());
            DownloadDocumentCommand = new Command<ChatMessage>(async msg => await DownloadDocumentAsync(msg));
            OpenDocumentCommand = new Command<ChatMessage>(async msg => await OpenDocumentAsync(msg));

            // Чтобы автоматически обновлять доступность команды Send
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MessageText))
                    ((Command)SendCommand).ChangeCanExecute();
            };
        }

        public async Task OnIncomingDocumentAsync(string remoteUrl, string fileName, long fileSize, string mime, string ts, string senderId)
        {
            var m = new Message
            {
                ChatId = _chatId,
                SenderId = senderId,
                CreatedAt = ts,
                //IsRead = false,
                Type = (int)MessageKind.Document,
                FileName = fileName,
                FileSize = fileSize,
                MimeType = mime,
                RemoteUrl = remoteUrl
            };
            await _repo.InsertMessageAsync(m);

            Messages.Add(new ChatMessage
            {
                Kind = MessageKind.Document,
                IsIncoming = true,
                Timestamp = DateTime.ParseExact(
                    ts,
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture),
                Document = new DocumentInfo
                {
                    FileName = fileName,
                    FileSize = fileSize,
                    MimeType = mime,
                    RemoteUrl = remoteUrl
                }
            });
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

        private async Task PickAndSendDocumentAsync()
        {
            if (_repo == null) throw new InvalidOperationException("_repo is null");
            if (Messages == null) throw new InvalidOperationException("Messages is null");

            // 1) выбрать файл (только чтобы взять имя; поток не открываем)
            var picked = await FilePicker.PickAsync(new PickOptions { PickerTitle = "Выберите документ" });
            if (picked == null) return;

            var fileName = string.IsNullOrWhiteSpace(picked.FileName) ? "Документ" : picked.FileName;

            if (_useDocumentPlaceholders)
            {
                // 2) сохраняем «пустой» документ в БД (без размера/URL)
                var msg = new Message
                {
                    ChatId = _chatId,
                    SenderId = _currentUserId,
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    //IsRead = true,
                    Type = (int)MessageKind.Document,
                    FileName = fileName,
                    FileSize = 0,                       // неизвестно
                    MimeType = null,
                    RemoteUrl = null,                    // нет ссылки
                    LocalPath = null
                };
                await _repo.InsertMessageAsync(msg);

                // 3) добавляем плейсхолдер в UI
                var vmMsg = new ChatMessage
                {
                    Kind = MessageKind.Document,
                    IsIncoming = false,
                    Timestamp = msg.CreatedAtDate,
                    Document = new DocumentInfo
                    {
                        FileName = fileName,
                        FileSize = 0,               // для конвертера «размер неизвестен»
                        MimeType = null,
                        RemoteUrl = null,
                        LocalPath = null,
                        IsDownloaded = false,
                        IsDownloading = false,
                        Progress = 0
                    }
                };

                Device.BeginInvokeOnMainThread(() =>
                {
                    Messages.Add(vmMsg);
                    MessagingCenter.Send(this, "ScrollToEnd");
                });

                // 4) НИЧЕГО НЕ ОТПРАВЛЯЕМ на сервер и НЕ ЧИТАЕМ поток
                return;

                /*try
                {
                    var picked = await FilePicker.PickAsync(new PickOptions { PickerTitle = "Выберите документ" });
                    if (picked == null) return;

                    // метаданные
                    var fileName = picked.FileName;
                    var contentType = picked.ContentType; // может быть null на части устройств
                    long size = 0;
                    string remoteUrl = null;

                    using (var s = await picked.OpenReadAsync())
                    {
                        // Если поток поддерживает Length — возьмём размер
                        if (s.CanSeek)
                            size = s.Length;

                        // при необходимости можно сбросить позицию:
                        // if (s.CanSeek) s.Position = 0;

                        var progress = new Progress<double>(p =>
                        {
                            // обновляйте прогресс активного сообщения при желании
                        });

                        remoteUrl = await _files.UploadAsync(
                            s,              // сам поток файла
                            fileName,
                            contentType,
                            progress,
                            CancellationToken.None);
                    }

                    // сохраняем как сообщение-документ
                    var msg = new Message
                    {
                        ChatId = _chatId,
                        SenderId = _currentUserId,
                        CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                        //IsRead = true,
                        Kind = (int)MessageKind.Document,
                        FileName = fileName,
                        FileSize = size,
                        MimeType = contentType,
                        RemoteUrl = remoteUrl,
                        LocalPath = null
                    };
                    await _repo.InsertMessageAsync(msg);

                    var vmMsg = new ChatMessage
                    {
                        Kind = MessageKind.Document,
                        IsIncoming = false,
                        Timestamp = msg.CreatedAtDate,
                        Document = new DocumentInfo
                        {
                            FileName = fileName,
                            FileSize = size,
                            MimeType = contentType,
                            RemoteUrl = remoteUrl,
                            LocalPath = null,
                            IsDownloaded = false
                        }
                    };
                    Messages.Add(vmMsg);

                    // оповестим собеседника (SignalR)
                    _ = _transport.SendDocumentAsync(_chatId, remoteUrl, fileName, size, contentType);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("TESTLOG PickAndSendDocumentAsync "+ex.Message+" " + ex.StackTrace);
                    // TODO: показать alert/log
                }*/
            }
        }

        private async Task DownloadDocumentAsync(ChatMessage msg)
        {
            if (msg == null || msg.Document == null)
                return;

            if (string.IsNullOrEmpty(msg.Document.RemoteUrl))
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Файл недоступен",
                    "Этот документ пока как заглушка и ещё не загружен на сервер.",
                    "ОК");
                return;
            }

            //if (msg?.Document == null || string.IsNullOrEmpty(msg.Document.RemoteUrl)) return;

            if (msg.Document.IsDownloaded) { await OpenDocumentAsync(msg); return; }

            msg.Document.IsDownloading = true;
            var progress = new Progress<double>(p => msg.Document.Progress = p);
            try
            {
                var localPath = await _files.DownloadAsync(msg.Document.RemoteUrl, msg.Document.FileName, progress, CancellationToken.None);
                msg.Document.LocalPath = localPath;
                msg.Document.IsDownloaded = true;
            }
            finally
            {
                msg.Document.IsDownloading = false;
            }
        }

        private async Task OpenDocumentAsync(ChatMessage msg)
        {
            if (msg?.Document == null) return;
            if (string.IsNullOrEmpty(msg.Document.LocalPath))
            {
                await DownloadDocumentAsync(msg);
                if (string.IsNullOrEmpty(msg.Document.LocalPath)) return;
            }

            await Launcher.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(msg.Document.LocalPath)
            });
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