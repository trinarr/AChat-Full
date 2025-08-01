using System;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Collections.ObjectModel;

namespace AChat_Full
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChatPage : ContentPage
    {
        private readonly SignalRChatClient _chatClient;
        public ObservableCollection<ChatMessage> Messages { get; } = new ObservableCollection<ChatMessage>();
        private readonly string _chatId;

        public ChatPage(string chatId, string userToken)
        {
            InitializeComponent();

            _chatId = chatId;
            BindingContext = this;

            _chatClient = new SignalRChatClient("https://yourserver.com/chathub");
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
            _ = _chatClient.ConnectAsync(userToken);
        }

        private async void OnSendClicked(object sender, EventArgs e)
        {
            var text = MessageEntry.Text?.Trim();
            if (string.IsNullOrEmpty(text))
                return;

            // Добавляем в локальный UI
            Messages.Add(new ChatMessage
            {
                Text = text,
                IsIncoming = false,
                Timestamp = DateTime.Now
            });
            MessagesView.ScrollTo(
                       item: Messages.Last(),
                       position: ScrollToPosition.End,
                       animate: true);

            // Посылаем на сервер
            await _chatClient.SendMessageAsync(_chatId, text);

            MessageEntry.Text = string.Empty;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _chatClient.Dispose();
        }
    }
}