using System;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Diagnostics;
using AChatFull.ViewModels;

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

        private bool _closedNotified;

        public ChatPage(string chatId, string userToken, ChatRepository repo, string peerName)
        {
            InitializeComponent();

            _chatId = chatId;
            _vm = new ChatViewModel(repo, chatId, userToken, peerName);
            BindingContext = _vm;

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
            };
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

        private async Task CloseModalAsync(bool animated = false)
        {
            try
            {
                // 1) спрятать клавиатуру
                MessageEditor?.Unfocus();

                // 2) «облегчить» визуальное дерево
                var host = this.FindByName<ContentView>("MessagesHost");
                if (host?.Content is MessagesListView mlv)
                {
                    mlv.Detach();       // см. метод ниже
                    host.Content = null;
                }

                // (опционально) разорвать биндинги для быстрого GC
                BindingContext = null;

                // 3) закрыть модалку
                await Application.Current.MainPage.Navigation.PopModalAsync(animated: animated);
            }
            finally
            {
                NotifyClosedOnce();
            }
        }

        private void NotifyClosedOnce()
        {
            if (_closedNotified) return;
            _closedNotified = true;

            Device.BeginInvokeOnMainThread(() =>
                MessagingCenter.Send(this, "ChatClosed", _chatId));
        }

        protected override bool OnBackButtonPressed()
        {
            Device.BeginInvokeOnMainThread(async () => await CloseModalAsync(animated: false));
            return true; 
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await CloseModalAsync(animated: false); 
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
                Device.BeginInvokeOnMainThread(() => MessagingCenter.Send(this, "ChatClosed", _chatId));
            }
        }

        private async void OnSearchClicked(object sender, EventArgs e)
        {
            //
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            NotifyClosedOnce();

            //_chatClient.Dispose();
        }
    }
}