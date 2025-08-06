﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace AChatFull.Views
{
    public class ChatsListViewModel : INotifyPropertyChanged
    {
        //private readonly SignalRChatClient _chatClient;
        public ObservableCollection<ChatSummary> Chats { get; } = new ObservableCollection<ChatSummary>();
        private readonly ChatRepository _repo;
        private readonly string _currentUserId;

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        //public ICommand LoadChatsCommand { get; }

        public ChatsListViewModel(ChatRepository repo, string currentUserId)
        {
            _repo = repo;
            _currentUserId = currentUserId;

            /*LoadMessagesCommand = new Command(async () => await LoadMessagesAsync());
            SendCommand = new Command(async () => await SendMessageAsync(),
                                              () => !string.IsNullOrWhiteSpace(MessageText));

            // Чтобы автоматически обновлять доступность команды Send
            this.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MessageText))
                    ((Command)SendCommand).ChangeCanExecute();
            };*/
        }

        public async Task LoadChatsAsync()
        {
            var list = await _repo.GetChatSummariesAsync();
            Chats.Clear();
            foreach (var chat in list)
                Chats.Add(chat);
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        #endregion
    }
}