using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using System.Diagnostics;

namespace AChatFull.Views
{
    public class ChatsViewModel : INotifyPropertyChanged
    {
        public static string USER_TOKEN_TEST = "TestUser";

        private readonly SignalRChatClient _chatClient;
        public ObservableCollection<ChatSummary> Chats { get; } = new ObservableCollection<ChatSummary>();

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public ICommand LoadChatsCommand { get; }

        public ChatsViewModel()
        {
            _chatClient = new SignalRChatClient("https://yourserver.com/chathub");
            LoadChatsCommand = new Command(async () => await LoadChatsAsync());
        }

        public async Task InitializeAsync(string token)
        {
            Debug.WriteLine("TESTLOG InitializeAsync: token" + token);

            if (token.Equals(USER_TOKEN_TEST)) {
                await LoadTestChatsAsync();
            }
            else
            {
                await _chatClient.ConnectAsync(token);
                await LoadChatsAsync();
            }
        }

        private async Task LoadTestChatsAsync()
        {
            Debug.WriteLine("TESTLOG LoadTestChatsAsync: IsBusy" + IsBusy);

            if (IsBusy) return;
            IsBusy = true;

            try
            {
                var list = await _chatClient.GetTestChatsAsync();
                Chats.Clear();
                foreach (var c in list)
                    Chats.Add(c);

                Debug.WriteLine("TESTLOG Chats count: " + Chats.Count);
            }
            catch (Exception ex)
            {
                // Обработка ошибки (например, через MessagingCenter)
                Debug.WriteLine("TESTLOG LoadTestChatsAsync Exception " + ex.Message+" "+ ex.StackTrace);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadChatsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var list = await _chatClient.GetChatsAsync();
                Chats.Clear();
                foreach (var c in list)
                    Chats.Add(c);
            }
            catch (Exception ex)
            {
                // Обработка ошибки (например, через MessagingCenter)
            }
            finally
            {
                IsBusy = false;
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        #endregion
    }
}