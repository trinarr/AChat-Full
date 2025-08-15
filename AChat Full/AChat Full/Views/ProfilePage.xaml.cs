using System;
using Xamarin.Forms;
using AChatFull.ViewModels;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AChatFull.Views
{
    public partial class ProfilePage : ContentPage
    {
        private readonly ChatRepository _repo;
        ProfileViewModel _vm;

        public bool IsInitialized { get; private set; }

        //public ProfileViewModel VM => BindingContext as ProfileViewModel;

        public ProfilePage(ChatRepository repo)
        {
            Debug.WriteLine("ProfilePage");

            InitializeComponent();
            _repo = repo;
            //BindingContext = new ProfileViewModel(repo);
        }

        public async Task EnsureInitAsync(INavigation nav)
        {
            if (IsInitialized) return;

            _vm = new ProfileViewModel(_repo);
            BindingContext = _vm;

            try
            {
                // инициализация VM и загрузка данных
                await _vm.InitializeAsync(nav).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Profile init failed: " + ex);
            }

            // Обновить биндинги на UI-потоке
            Device.BeginInvokeOnMainThread(() =>
            {
                IsInitialized = true;
            });
        }
    }
}