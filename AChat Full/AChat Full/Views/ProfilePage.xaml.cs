using System;
using Xamarin.Forms;
using AChatFull.ViewModels;

namespace AChatFull.Views
{
    public partial class ProfilePage : ContentPage
    {
        private readonly ProfileViewModel _vm;

        public ProfilePage(ProfileViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
        }

        public ProfilePage() : this(new ProfileViewModel(DependencyService.Get<ChatRepository>()))
        {
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            /*if (_vm != null)
                await _vm.LoadAsync();*/
        }
    }
}