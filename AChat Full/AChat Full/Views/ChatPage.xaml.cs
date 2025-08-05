using System;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace AChatFull.Views
{
    public partial class ChatPage : ContentPage
    {
        //private readonly SignalRChatClient _chatClient;
        //private readonly ChatViewModel _vm;

        private readonly string _chatId;
        private readonly ChatRepository _repo;

        public ChatPage()
        {
            InitializeComponent();
        }

        public ChatPage(string chatId, string userToken, ChatRepository repo, string peerName)
        {
            InitializeComponent();

            var vm = new ChatViewModel(repo, chatId, userToken, peerName);
            BindingContext = vm;

            _ = vm.LoadMessagesAsync();

            //Messages = new ObservableCollection<ChatMessage>();
            //BindingContext = this;

            /*_chatId = chatId;
            _repo = repo;
            _ = LoadMessagesAsync();*/

            //_vm = new ChatViewModel(_repo, chatId);
            //BindingContext = _vm;

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
            /*base.OnAppearing();
            await LoadMessagesAsync();
            // прокрутка вниз, если нужно:

            if (Messages.Count > 0)
                MessagesView.ScrollTo(
                    Messages[Messages.Count - 1],
                    position: ScrollToPosition.End,
                    animate: false);*/
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnSendClicked(object sender, EventArgs e)
        {
            var text = MessageEntry.Text?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            // сохраняем в базу
            /*var newMsg = new Message
            {
                ChatId = _vm.ChatId,
                SenderId = currentUserId,
                Text = text,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            await repo.InsertAsync(newMsg);*/

            // обновляем UI
            /*_vm.Messages.Add(new ChatMessage
            {
                Text = text,
                IsIncoming = false,
                Timestamp = newMsg.CreatedAt
            });
            MessageEntry.Text = string.Empty;
            MessagesView.ScrollTo(_vm.Messages.Last(), ScrollToPosition.End, true);*/

            // и при желании: послать через вебсокет/SignalR

            // Посылаем на сервер
            //await _chatClient.SendMessageAsync(_chatId, text);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            //_chatClient.Dispose();
        }
    }
}