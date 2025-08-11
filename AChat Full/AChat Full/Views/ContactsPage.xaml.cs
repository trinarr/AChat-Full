using Xamarin.Forms;
using AChatFull.ViewModels;
using System.Diagnostics;

namespace AChatFull.Views
{
    public partial class ContactsPage : ContentPage
    {
        public ContactsViewModel VM => BindingContext as ContactsViewModel;

        public ContactsPage(ChatRepository repo)
        {
            InitializeComponent();
            BindingContext = new ContactsViewModel(repo);
        }

        protected override async void OnAppearing()
        {
            Debug.WriteLine("TESTLOG OnAppearing");

            base.OnAppearing();
            VM.IsSearchMode = false;   // гарантируем обычный режим при входе
            await VM.LoadAsync();
        }

        private void OnSearchIconClicked(object sender, System.EventArgs e)
        {
            VM.IsSearchMode = true;
            Device.BeginInvokeOnMainThread(() => SearchEntry?.Focus());
        }

        private void OnSearchBackClicked(object sender, System.EventArgs e)
        {
            VM.SearchText = string.Empty; // очистим фильтр
            VM.IsSearchMode = false;
        }
    }
}