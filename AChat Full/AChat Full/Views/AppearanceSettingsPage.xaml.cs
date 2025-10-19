using Xamarin.Forms;
using Xamarin.Essentials;
using System.Threading.Tasks;
using AChatFull.Services;
using System.Diagnostics;

namespace AChatFull.Views
{
    public partial class AppearanceSettingsPage : ContentPage
    {
        public const string ColumnsKey = "contacts.columns";    // int: 1 или 2
        public const string IconsKey = "contacts.icons";    // int: 1 или 2
        public const string ShowGroupsKey = "contacts.showgroups"; // bool

        int _selectedIcons;
        public int SelectedIcons
        {
            get => _selectedIcons;
            set
            {
                if (_selectedIcons == value) return;
                _selectedIcons = value;
                OnPropertyChanged(nameof(SelectedIcons));

                ChangeIconAsync();

                var svc = DependencyService.Get<ISettingsService>();
                if (svc != null) svc.SetInt(IconsKey, _selectedIcons);
                Preferences.Set(IconsKey, _selectedIcons);

                //MessagingCenter.Send(this, "Contacts.IconsChanged", _selectedIcons);
            }
        }

        private async Task ChangeIconAsync()
        {
            //Debug.WriteLine("AppearanceSettingsPage SwitchAppIcon: " + _selectedIcons);

            await DependencyService.Get<IIconSwitchService>().SwitchAppIcon(_selectedIcons);
        }

        int _selectedColumns;
        public int SelectedColumns
        {
            get => _selectedColumns;
            set
            {
                if (_selectedColumns == value) return;
                _selectedColumns = value;
                OnPropertyChanged(nameof(SelectedColumns));

                // ⬇⬇ сохраняем в SharedPreferences (Android), fallback — Essentials
                var svc = DependencyService.Get<ISettingsService>();
                if (svc != null) svc.SetInt(ColumnsKey, _selectedColumns);
                Preferences.Set(ColumnsKey, _selectedColumns); // запасной вариант

                // оповестим ContactsPage (живое обновление)
                MessagingCenter.Send(this, "Contacts.ColumnsChanged", _selectedColumns);
            }
        }

        public AppearanceSettingsPage()
        {
            InitializeComponent();

            var svc = DependencyService.Get<ISettingsService>();
            var initCols = svc?.GetInt(ColumnsKey, 1) ?? Preferences.Get(ColumnsKey, 1);
            SelectedColumns = initCols;

            var initIcon = svc?.GetInt(IconsKey, 2) ?? Preferences.Get(IconsKey, 2);
            SelectedIcons = initIcon;

            var showGroups = svc?.GetBool(ShowGroupsKey, true) ?? Preferences.Get(ShowGroupsKey, true);
            ShowGroupsSwitch.IsToggled = showGroups;
        }

        async void OnBackClicked(object sender, System.EventArgs e)
        {
            if (Navigation.ModalStack?.Count > 0)
                await Navigation.PopModalAsync();
            else
                await Navigation.PopAsync();
        }

        void OnOldIconTapped(object sender, System.EventArgs e) => SelectedIcons = 1;
        void OnNewIconTapped(object sender, System.EventArgs e) => SelectedIcons = 2;

        void OnOneColumnTapped(object sender, System.EventArgs e) => SelectedColumns = 1;
        void OnTwoColumnsTapped(object sender, System.EventArgs e) => SelectedColumns = 2;

        void OnShowGroupsTapped(object sender, System.EventArgs e)
            => ShowGroupsSwitch.IsToggled = !ShowGroupsSwitch.IsToggled;

        void OnShowGroupsToggled(object sender, ToggledEventArgs e)
        {
            var svc = DependencyService.Get<ISettingsService>();
            if (svc != null) svc.SetBool(ShowGroupsKey, e.Value);
            Preferences.Set(ShowGroupsKey, e.Value);

            MessagingCenter.Send(this, "Contacts.ShowGroupsChanged", e.Value);
        }
    }
}
