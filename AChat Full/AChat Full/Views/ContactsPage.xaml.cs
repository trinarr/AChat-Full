using Xamarin.Forms;
using AChatFull.ViewModels;
using System;

namespace AChatFull.Views
{
    public partial class ContactsPage : ContentPage
    {
        private readonly ChatRepository _repo;
        public ContactsViewModel VM => BindingContext as ContactsViewModel;

        public ContactsPage(ChatRepository repo)
        {
            InitializeComponent();
            _repo = repo;
            BindingContext = new ContactsViewModel(repo);

            // бридж поиска: при наборе текста обновляем VM.SearchText
            this.SearchEntry.TextChanged += (s, e) => VM.SearchText = e.NewTextValue;

            // Навигация на чат из VM
            MessagingCenter.Subscribe<ContactsViewModel, string>(this, "OpenChat", async (_, chatId) =>
            {
                var chatPage = new ChatPage(chatId, App.USER_TOKEN_TEST, _repo);
                await Application.Current.MainPage.Navigation.PushModalAsync(chatPage, animated: false);
            });
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            VM.IsSearchMode = false;   // обычный режим при входе
            await VM.LoadContactsAsync(); // полный список контактов
        }

        private void OnSearchBackClicked(object sender, EventArgs e)
        {
            SearchEntry?.Unfocus();
            VM.SearchText = string.Empty; // очистить фильтр
            VM.IsSearchMode = false;
        }

        // === ВАЖНО: эти сигнатуры должны совпадать с XAML ===
        private void OnSearchIconClicked(object sender, EventArgs e)
        {
            VM.IsSearchMode = true;
            Device.BeginInvokeOnMainThread(() => SearchEntry?.Focus());
        }
    }
}