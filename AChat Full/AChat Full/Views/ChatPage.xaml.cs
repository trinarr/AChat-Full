using System;
using System.Linq;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Diagnostics;
using AChatFull.ViewModels;
using AChatFull.Views;

namespace AChatFull.Views
{
    public partial class ChatPage : ContentPage
    {
        //private readonly SignalRChatClient _chatClient;   
        private readonly ChatViewModel _vm;

        private readonly string _chatId;
        private readonly ChatRepository _repo;
        private bool _messagesViewCreated;

        private double _singleLineHeight;
        private double _maxHeight;

        private const int MaxVisibleLines = 6;

        public ChatPage(string chatId, string userToken, ChatRepository repo, string peerName)
        {
            InitializeComponent();

            _chatId = chatId;
            _vm = new ChatViewModel(repo, chatId, userToken, peerName);
            BindingContext = _vm;
            // initial load moved to OnAppearing for faster first paint
MessagingCenter.Subscribe<ChatViewModel>(this, "ScrollToEnd", async sender =>
            {
                await ScrollToTop(true);
            });

            // Получаем приближённую высоту строки
            _singleLineHeight = MessageEditor.FontSize * 1.2;
            _maxHeight = MaxVisibleLines * _singleLineHeight;

            // Подписываемся на изменение размера
            MessageEditor.SizeChanged += (s, e) =>
            {
                if (_vm.Messages.Count > 0)
            {
                var host = this.FindByName<ContentView>("MessagesHost");
                if (host?.Content is MessagesListView mlv)
                    mlv.ScrollToBottom(true);
            }

                // когда фактическая высота больше максимума —
                // жёстко фиксим максимальную
                /*if (MessageEditor.Height > _maxHeight)
                {
                    MessageEditor.HeightRequest = _maxHeight;
                }
                else
                {
                    // иначе даём редактору подгоняться сам
                    MessageEditor.HeightRequest = -1;
                }*/
            };

            /*_chatClient = new SignalRChatClient("https://yourserver.com/chathub");
            _chatClient.Connected += () => Device.BeginInvokeOnMainThread(() =>
                System.Diagnostics.Debug.WriteLine("SignalR Connected"));
            _chatClient.Disconnected += () => Device.BeginInvokeOnMainThread(() =>
                System.Diagnostics.Debug.WriteLine("SignalR Disconnected"));
            _chatClient.Error += ex => Device.BeginInvokeOnMainThread(async () =>
                await DisplayAlert("Error", ex.Message, "OK"));
            _chatClient.MessageReceived += (chatIdReceived, userId, text, timestamp) =>
            {
                if (chatIdReceived != _chatId) return;
                Device.BeginInvokeOnMainThread(() =>
                {
                    Messages.Add(new ChatMessage
                    {
                        Text = text,
                        IsIncoming = userId != "78977",
                        Timestamp = timestamp
                    });
                    var host2 = this.FindByName<ContentView>("MessagesHost");
                    if (host2?.Content is MessagesListView mlv2)
                        mlv2.ScrollToBottom(true);
                });
            };

            // Запуск подключения с токеном авторизации
            _ = _chatClient.ConnectAsync(userToken);*/
        }

        private async void OnAudioCallClicked(object sender, EventArgs e)
        {
            bool answer = await DisplayAlert("Аудио-звонок", "Начать аудио-звонок?", "Да", "Отмена");
            if (answer)
            {
                // Логика старта аудио
            }
        }

        private async void OnVideoCallClicked(object sender, EventArgs e)
        {
            bool answer = await DisplayAlert("Видео-звонок", "Начать видео-звонок?", "Да", "Отмена");
            if (answer)
            {
                // Логика старта видео
            }
        }

        private async void OnMoreClicked(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet("Меню", "Отмена", null,
                "Просмотреть контакт", "Заблокировать", "Удалить чат");

            switch (action)
            {
                case "Просмотреть контакт":
                    // открыть инфо
                    break;
                case "Заблокировать":
                    // блок
                    break;
                case "Удалить чат":
                    // удалить
                    break;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (!_messagesViewCreated)
            {
                // Yield to let header & input render
                await Task.Yield();

                var view = new MessagesListView { BindingContext = _vm };
                var host = this.FindByName<ContentView>("MessagesHost");
                if (host != null) host.Content = view;
                _messagesViewCreated = true;
            }

            // Initial load
            if (_vm.Messages.Count == 0)
            {
                await _vm.LoadMessagesAsync();
                await ScrollToTop(false);
            }
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnSendClicked(object sender, EventArgs e)
        {
            Debug.WriteLine("TESTLOG OnSendClicked");

            await ScrollToTop(true);
        }

        private async Task ScrollToTop(bool animate)
        {
            await Task.Delay(50);
            var host = this.FindByName<ContentView>("MessagesHost");
            if (host?.Content is MessagesListView mlv)
            {
                mlv.ScrollToBottom(animate);
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            try
            {
                // Спрятать клавиатуру, чтобы не было перестроений во время закрытия
                MessageEditor?.Unfocus();

                // Отцепить тяжёлый список перед навигацией — так GPU/GC не мешают анимации
                var host = this.FindByName<ContentView>("MessagesHost");
                if (host?.Content is MessagesListView mlv)
                {
                    mlv.Detach();          // см. пункт 2
                    host.Content = null;   // убираем из визуального дерева
                }

                // Разорвать биндинги (ускоряет финализацию)
                BindingContext = null;

                // Закрываем БЕЗ анимации — исчезновение мгновенное и без дропов кадров
                await Application.Current.MainPage.Navigation.PopModalAsync(animated: false);
            }
            finally
            {
                // Сообщаем о закрытии уже ПОСЛЕ выхода со страницы
                Device.BeginInvokeOnMainThread(() =>
                    Xamarin.Forms.MessagingCenter.Send(this, "ChatClosed", _chatId));
            }
        }

        private async void OnSearchClicked(object sender, EventArgs e)
        {
            //
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            MessagingCenter.Unsubscribe<ChatViewModel>(this, "ScrollToEnd");

            //_chatClient.Dispose();
        }
    }
}