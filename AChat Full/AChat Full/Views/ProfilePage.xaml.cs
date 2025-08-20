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