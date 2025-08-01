using Xamarin.Forms;

namespace AChatFull.Views
{
    public partial class ChatsList : ContentPage
    {
        private ChatsViewModel Vm => BindingContext as ChatsViewModel;

        public ChatsList(string userToken)
        {
            InitializeComponent();
            // Инициализируем SignalR и загружаем чаты
            _ = Vm.InitializeAsync(userToken);
        }

        private async void OnChatSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.Count == 0) return;
            var chat = e.CurrentSelection[0] as ChatSummary;
            ((CollectionView)sender).SelectedItem = null;
            await Navigation.PushAsync(new ChatsList(chat.ChatId));
        }
    }
}