﻿using Xamarin.Forms;
using System;
using System.Diagnostics;

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