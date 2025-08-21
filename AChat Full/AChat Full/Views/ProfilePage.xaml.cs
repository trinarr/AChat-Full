using System;
using Xamarin.Forms;
using AChatFull.ViewModels;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AChatFull.Views
{
    public partial class ProfilePage : ContentPage, ILazyInitPage
    {
        private readonly ChatRepository _repo;
        private ProfileViewModel _vm;

        public bool IsInitialized { get; private set; }

        public ProfilePage(ChatRepository repo)
        {
            Debug.WriteLine("ProfilePage");

            _repo = repo;
            InitializeComponent();
        }

        async void OnClearCustomStatusTapped(object sender, EventArgs e)
        {
            if (BindingContext is ProfileViewModel vm)
                await (vm?.GetType().GetMethod("ClearCustomStatusAsync",
                      System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                      ?.Invoke(vm, new object[] { }) as Task);
        }

        private async void OnCustomStatusTapped(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new NavigationPage(new CustomStatusPage(_repo)));
        }

        public async Task EnsureInitAsync(INavigation nav)
        {
            if (IsInitialized) return;

            if (BindingContext is ProfileViewModel vmFromXaml)
                _vm = vmFromXaml;
            else
                BindingContext = _vm = _vm ?? new ProfileViewModel(_repo);

            try { await _vm.InitializeAsync(nav).ConfigureAwait(false); }
            catch (Exception ex) { Debug.WriteLine("Profile init failed: " + ex); }

            Device.BeginInvokeOnMainThread(() => IsInitialized = true);
        }
    }
}