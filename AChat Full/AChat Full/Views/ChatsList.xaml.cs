using Xamarin.Forms;
using System;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace AChatFull.Views
{
    public partial class ChatsList : ContentPage
    {
        //private ChatsViewModel Vm => BindingContext as ChatsViewModel;

        private readonly ChatRepository _repo;
        public ObservableCollection<ChatSummary> Chats { get; }
            = new ObservableCollection<ChatSummary>();

        public ChatsList(string userToken, ChatRepository repo)
        {
            InitializeComponent();
            BindingContext = this;

            _repo = repo;
            _ = LoadChatsAsync();

            // Инициализируем SignalR и загружаем чаты
            //_ = Vm.InitializeAsync(userToken);
        }
        private async Task LoadChatsAsync()
        {
            var list = await _repo.GetChatSummariesAsync();
            Chats.Clear();
            foreach (var chat in list)
                Chats.Add(chat);
        }

        private async void OnChatSelected(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine("TESTLOG OnChatSelected");

            if (e.CurrentSelection.Count == 0) return;
            var chat = e.CurrentSelection[0] as ChatSummary;
            ((CollectionView)sender).SelectedItem = null;

            try
            {
                await Navigation.PushAsync(new ChatPage(chat.ChatId, ChatsViewModel.USER_TOKEN_TEST));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("TESTLOG OnChatSelected Exception " + ex.Message + " " + ex.StackTrace);
            }
        }
    }
}