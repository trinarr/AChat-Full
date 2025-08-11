using System;
using Xamarin.Forms;
using AChatFull.ViewModels;

namespace AChatFull.Views
{
    public partial class SettingsPage : ContentPage
    {
        private readonly SettingsViewModel _vm;

        public SettingsPage(SettingsViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
        }

        public SettingsPage() : this(new SettingsViewModel(DependencyService.Get<ChatRepository>()))
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