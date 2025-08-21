using Xamarin.Forms;
using System;
using System.Threading.Tasks;
using AChatFull.ViewModels;

namespace AChatFull.Views
{
    public partial class CustomStatusPage : ContentPage
    {
        private readonly ChatRepository _repo;
        private readonly CustomStatusViewModel _vm;
        private bool _isSaving;

        public CustomStatusPage(ChatRepository repo)
        {
            InitializeComponent();
            _repo = repo;
            _vm = new CustomStatusViewModel(repo);
            BindingContext = _vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.ReloadStatusAsync();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync(true);
        }

        private async void OnEmojiTapped(object sender, EventArgs e)
        {
            // опционально: открыть ваш StatusBottomSheetPage
            // await Navigation.PushModalAsync(new StatusBottomSheetPage(_repo), true);
            // await _vm.ReloadStatusAsync();
        }

        private async void OnStatusTextCompleted(object sender, EventArgs e)
        {
            await SaveStatusAsync();
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (_isSaving) return;
            _isSaving = true;
            try
            {
                await _repo.UpdateCustomStatusAsync(new CustomStatusModel
                {
                    Emoji = _vm.StatusEmoji,
                    Text = _vm.StatusText
                });

                // уведомим другие экраны (как вы уже делаете)
                MessagingCenter.Send<object>(this, "ProfileChanged");

                // закрыть модалку
                await Navigation.PopModalAsync(true);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            finally { _isSaving = false; }
        }

        private async Task SaveStatusAsync()
        {
            await _repo.UpdateCustomStatusAsync(new CustomStatusModel
            {
                Emoji = _vm.StatusEmoji,
                Text = _vm.StatusText
            });

            // уведомляем остальные экраны (вы уже используете это событие)
            MessagingCenter.Send<object>(this, "ProfileChanged");
        }
    }
}
