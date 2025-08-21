using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using AChatFull.ViewModels;
using System.Threading.Tasks;

namespace AChatFull.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PeerProfilePage : ContentPage
    {
        private readonly PeerProfileViewModel _vm;
        private readonly ChatRepository _repo;
        private bool _initialized;

        public PeerProfilePage(string userId, ChatRepository repo)
        {
            InitializeComponent();

            _repo = repo ?? throw new ArgumentNullException(nameof(repo));

            BindingContext = _vm = new PeerProfileViewModel(_repo, userId);
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

        async Task CloseProfileAndUnderlyingChatIfAnyAsync()
        {
            var nav = Application.Current.MainPage?.Navigation;
            if (nav == null) { await Navigation.PopModalAsync(false); return; }

            // Смотрим страницу ПОД текущим модальным профилем
            Page chatBelow = null;
            if (nav.ModalStack != null && nav.ModalStack.Count >= 2)
                chatBelow = nav.ModalStack[nav.ModalStack.Count - 2];

            // 1) закрываем профиль
            if (nav.ModalStack?.Count > 0)
                await nav.PopModalAsync(false);

            // 2) если под ним ChatPage — закрываем и его
            if (chatBelow is ChatPage)
                await nav.PopModalAsync(false);
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
                    {
                        var confirm = await DisplayAlert("Remove contact",
                            $"Remove {_vm.DisplayName ?? "this user"} from contacts and delete your direct chat?",
                            "Remove", "Cancel");
                        if (!confirm) return;

                        await _repo.UnmarkUserAsContactAsync(_vm.UserId);

                        var chatId = await _repo.FindDirectChatIdAsync(_vm.UserId);
                        if (!string.IsNullOrEmpty(chatId))
                            await _repo.DeleteChatAsync(chatId);

                        MessagingCenter.Send<object, string>(this, "ChatClosed", _vm.UserId);

                        await CloseProfileAndUnderlyingChatIfAnyAsync();
                        break;
                    }
            }
        }
    }
}
