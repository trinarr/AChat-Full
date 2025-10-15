using Xamarin.Forms;
using Xamarin.Essentials;
using AChatFull.Services;

namespace AChatFull.Views
{
    public partial class ContactListSettingsPage : ContentPage
    {
        public const string ColumnsKey = "contacts.columns";    // int: 1 или 2
        public const string ShowGroupsKey = "contacts.showgroups"; // bool

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

        public ContactListSettingsPage()
        {
            InitializeComponent();

            // init: читаем из SharedPreferences, если доступен; иначе из Essentials
            var svc = DependencyService.Get<ISettingsService>();
            var initCols = svc?.GetInt(ColumnsKey, 1) ?? Preferences.Get(ColumnsKey, 1);
            SelectedColumns = initCols;

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

        void OnOneColumnTapped(object sender, System.EventArgs e) => SelectedColumns = 1;
        void OnTwoColumnsTapped(object sender, System.EventArgs e) => SelectedColumns = 2;

        void OnShowGroupsTapped(object sender, System.EventArgs e)
            => ShowGroupsSwitch.IsToggled = !ShowGroupsSwitch.IsToggled;

        void OnShowGroupsToggled(object sender, ToggledEventArgs e)
        {
            var svc = DependencyService.Get<ISettingsService>();
            if (svc != null) svc.SetBool(ShowGroupsKey, e.Value);
            Preferences.Set(ShowGroupsKey, e.Value);

            // ⬇️ уведомляем ContactsPage
            MessagingCenter.Send(this, "Contacts.ShowGroupsChanged", e.Value);
        }
    }
}
