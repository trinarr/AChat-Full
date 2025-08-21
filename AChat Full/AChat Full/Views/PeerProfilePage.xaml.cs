using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using AChatFull.ViewModels;

namespace AChatFull.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PeerProfilePage : ContentPage
    {
        private readonly PeerProfileViewModel _vm;
        private bool _initialized;

        public PeerProfilePage(string userId, ChatRepository repo)
        {
            InitializeComponent();
            BindingContext = _vm = new PeerProfileViewModel(repo, userId);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_initialized) return;
            await _vm.InitializeAsync();
            _initialized = true;
        }

        private async void OnBackTapped(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync(false);
        }

        private void OnMessageTapped(object sender, EventArgs e)
        {
            // Сообщаем вверх (ChatPage/Router) открыть переписку
            //MessagingCenter.Send(this, "PeerProfile_Message", _vm.UserId);
        }

        private void OnVoiceTapped(object sender, EventArgs e)
        {
            // Совпадает с ранее добавленным событием для меню More
            //MessagingCenter.Send(this, "PeerProfile_Call", _vm.UserId);
        }

        private void OnVideoTapped(object sender, EventArgs e)
        {
            //MessagingCenter.Send(this, "PeerProfile_Video", _vm.UserId);
        }

        private async void OnMoreTapped(object sender, EventArgs e)
        {
            var action = await DisplayActionSheet(
                title: null,
                cancel: "Cancel",
                destruction: null,
                buttons: new[] { "Remove contact"});

            switch (action)
            {
                case "Remove contact":
                    var yes = await DisplayAlert("Remove contact",
                        "This will remove the contact from your list. Continue?",
                        "Remove", "Cancel");
                    if (!yes) return;

                    /*var ok = await _vm.RemoveContactAsync();
                    if (ok)
                    {
                        await DisplayAlert("Removed", "Contact has been removed.", "OK");
                        await Navigation.PopModalAsync(false);
                    }
                    else
                    {
                        await DisplayAlert("Error", "Couldn't remove contact. Try again later.", "OK");
                    }*/
                    break;
            }
        }
    }
}
