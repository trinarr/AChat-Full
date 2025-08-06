using System;
using System.Linq;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AChatFull.Views
{
    public partial class ChatPage : ContentPage
    {
        //private readonly SignalRChatClient _chatClient;
        private readonly ChatViewModel _vm;

        private readonly string _chatId;
        private readonly ChatRepository _repo;

        private double _singleLineHeight;
        private double _maxHeight;

        private const int MaxVisibleLines = 6;

        public ChatPage(string chatId, string userToken, ChatRepository repo, string peerName)
        {
            InitializeComponent();

            _vm = new ChatViewModel(repo, chatId, userToken, peerName);
            BindingContext = _vm;

            _ = _vm.LoadMessagesAsync();

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
                MessagesView.ScrollTo(
                MessagesView.ItemsSource.Cast<ChatMessage>().Last(),
                position: ScrollToPosition.End,
                animate: false);

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
                    MessagesView.ScrollTo(
                        item: Messages.Last(),
                        position: ScrollToPosition.End,
                        animate: true);
                });
            };

            // Запуск подключения с токеном авторизации
            _ = _chatClient.ConnectAsync(userToken);*/
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ScrollToTop(false);
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

            // Прокрутить к последнему элементу
            MessagesView.ScrollTo(
                MessagesView.ItemsSource.Cast<ChatMessage>().Last(),
                position: ScrollToPosition.End,
                animate: animate);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            //_chatClient.Dispose();
        }
    }
}