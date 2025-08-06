using Xamarin.Forms;
using System;
using System.Diagnostics;

namespace AChatFull.Views
{
    public partial class ChatsListPage : ContentPage
    {
        private ChatRepository _repo;

        public ChatsListPage(string userToken, ChatRepository repo)
        {
            InitializeComponent();

            var vm = new ChatsListViewModel(repo, userToken);
            BindingContext = vm;

            _repo = repo;
            _ = vm.LoadChatsAsync();

            // Инициализируем SignalR и загружаем чаты
            //_ = Vm.InitializeAsync(userToken);
        }

        private async void OnChatSelected(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine("TESTLOG OnChatSelected");

            if (e.CurrentSelection.Count == 0) return;
            var chat = e.CurrentSelection[0] as ChatSummary;
            ((CollectionView)sender).SelectedItem = null;

            try
            {
                await Navigation.PushAsync(new ChatPage(chat.ChatId, App.USER_TOKEN_TEST, _repo, chat.Title));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("TESTLOG OnChatSelected Exception " + ex.Message + " " + ex.StackTrace);
            }
        }
    }
}