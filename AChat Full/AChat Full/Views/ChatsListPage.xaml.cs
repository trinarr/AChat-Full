using Xamarin.Forms;
using System;
using System.Diagnostics;
using AChatFull.ViewModels;

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
                var chatPage = new ChatPage(chat.ChatId, App.USER_TOKEN_TEST, _repo, chat.Title);
                await Application.Current.MainPage.Navigation.PushModalAsync(chatPage, animated: false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("TESTLOG OnChatSelected Exception " + ex.Message + " " + ex.StackTrace);
            }
        }
    }
}