using System;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Diagnostics;
using AChatFull.ViewModels;

namespace AChatFull.Views
{
    public partial class ChatPage : ContentPage
    {  
        private readonly ChatViewModel _vm;
        private readonly ChatRepository _repo;

        private readonly string _chatId;
        private bool _messagesViewCreated;

        private double _singleLineHeight;
        private double _maxHeight;

        private const int MaxVisibleLines = 6;

        private bool _closedNotified;

        public ChatPage(string chatId, string userToken, ChatRepository repo)
        {
            InitializeComponent();

            _chatId = chatId;
            _repo = repo;
            _vm = new ChatViewModel(repo, chatId, userToken);
            BindingContext = _vm;

            MessagingCenter.Subscribe<ChatViewModel>(this, "ScrollToEnd", async sender =>
            {
                await ScrollToTop(true);
            });

            _singleLineHeight = MessageEditor.FontSize * 1.2;
            _maxHeight = MaxVisibleLines * _singleLineHeight;

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
            bool answer = await DisplayAlert("Start voice call?", "", "Call", "Cancel");
            if (answer)
            {
                // Логика старта аудио
            }
        }

        private async void OnVideoCallClicked(object sender, EventArgs e)
        {
            bool answer = await DisplayAlert("Start video call?", "", "Call", "Cancel");
            if (answer)
            {
                // Логика старта видео
            }
        }

        private async void OnMoreClicked(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet("", "Cancel", null,
                "Mute", "Block", "Delete chat");

            switch (action)
            {
                case "Mute":
                    break;
                case "Block":
                    break;
                case "Delete chat":
                    // Подтвердим удаление
                    bool confirm = await DisplayAlert("Delete chat",
                        "This will remove the conversation and all its messages from this device.",
                        "Delete", "Cancel");

                    if (confirm)
                    {
                        await _repo.DeleteChatAsync(_chatId);
                        await CloseModalAsync(animated: false); // закроем страницу
                                                                // CloseModalAsync вызовет NotifyClosedOnce() => ChatsList перезагрузится
                    }
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

            if (_vm.Messages.Count == 0)
            {
                await _vm.LoadMessagesAsync();
                await ScrollToTop(false);
            }

            await _vm.SetHeaderAsync();
        }

        private async Task CloseModalAsync(bool animated = false)
        {
            try
            {
                MessageEditor?.Unfocus();

                var host = this.FindByName<ContentView>("MessagesHost");
                if (host?.Content is MessagesListView mlv)
                {
                    mlv.Detach();      
                    host.Content = null;
                }

                BindingContext = null;
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
            await Task.Yield();
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
                MessageEditor?.Unfocus();

                var host = this.FindByName<ContentView>("MessagesHost");
                if (host?.Content is MessagesListView mlv)
                {
                    mlv.Detach();          
                    host.Content = null;   
                }

                BindingContext = null;

                await Application.Current.MainPage.Navigation.PopModalAsync(animated: false);
            }
            finally
            {
                Device.BeginInvokeOnMainThread(() => MessagingCenter.Send(this, "ChatClosed", _chatId));
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            NotifyClosedOnce();
        }
    }
}