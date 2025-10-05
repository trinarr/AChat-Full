using Xamarin.Forms;
using AChatFull.ViewModels;
using System;

namespace AChatFull.Views
{
    public partial class ContactsPage : ContentPage
    {
        private bool _isGrid;  // false = список, true = сетка

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

            ApplyLayoutMode(); // дефолт: список
        }

        private void OnLayoutToggleClicked(object sender, EventArgs e)
        {
            _isGrid = !_isGrid;
            ApplyLayoutMode();
        }

        void ApplyLayoutMode()
        {
            if (_isGrid)
            {
                // сетка 2 столбца
                ContactsList.ItemsLayout = new GridItemsLayout(2, ItemsLayoutOrientation.Vertical)
                { HorizontalItemSpacing = 0, VerticalItemSpacing = 0 };

                ResultsList.ItemsLayout = new GridItemsLayout(2, ItemsLayoutOrientation.Vertical)
                { HorizontalItemSpacing = 0, VerticalItemSpacing = 0 };

                // уменьшенные размеры для компактного вида
                Resources["Size.PresenceIcon"] = 24.0;  // было 30
                Resources["Size.NameFont"] = 14.0;  // было 18
                Resources["Size.StatusFont"] = 11.0;  // было 12

                LayoutToggleButton.Source = "view_column_1.png";
            }
            else
            {
                // обычный список
                ContactsList.ItemsLayout = LinearItemsLayout.Vertical;
                ResultsList.ItemsLayout = LinearItemsLayout.Vertical;

                // размеры по умолчанию (список)
                Resources["Size.PresenceIcon"] = 30.0;
                Resources["Size.NameFont"] = 18.0;
                Resources["Size.StatusFont"] = 12.0;

                LayoutToggleButton.Source = "view_column_2.png";
            }
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

        private void OnSearchIconClicked(object sender, EventArgs e)
        {
            VM.IsSearchMode = true;
            Device.BeginInvokeOnMainThread(() => SearchEntry?.Focus());
        }
    }
}