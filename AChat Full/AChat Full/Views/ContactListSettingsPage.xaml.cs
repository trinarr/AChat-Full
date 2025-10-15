using Xamarin.Forms;
using Xamarin.Essentials;

namespace AChatFull.Views
{
    public partial class ContactListSettingsPage : ContentPage
    {
        const string ColumnsKey = "contacts.columns";     // int: 1 или 2
        const string ShowGroupsKey = "contacts.showgroups";  // bool

        int _selectedColumns;
        public int SelectedColumns
        {
            get => _selectedColumns;
            set
            {
                if (_selectedColumns == value) return;
                _selectedColumns = value;
                OnPropertyChanged(nameof(SelectedColumns));   // обновляет радио-индикаторы
                Preferences.Set(ColumnsKey, _selectedColumns);
                // при желании — уведомить ContactsPage:
                // MessagingCenter.Send(this, "Contacts.ColumnsChanged", _selectedColumns);
            }
        }

        public ContactListSettingsPage()
        {
            InitializeComponent();

            // init
            SelectedColumns = Preferences.Get(ColumnsKey, 1);
            ShowGroupsSwitch.IsToggled = Preferences.Get(ShowGroupsKey, true);
        }

        async void OnBackClicked(object sender, System.EventArgs e)
        {
            if (Navigation.ModalStack?.Count > 0)
                await Navigation.PopModalAsync();
            else
                await Navigation.PopAsync();
        }

        // Тапы по строкам с радиокнопками
        void OnOneColumnTapped(object sender, System.EventArgs e) => SelectedColumns = 1;
        void OnTwoColumnsTapped(object sender, System.EventArgs e) => SelectedColumns = 2;

        // Show groups
        void OnShowGroupsTapped(object sender, System.EventArgs e)
            => ShowGroupsSwitch.IsToggled = !ShowGroupsSwitch.IsToggled;

        void OnShowGroupsToggled(object sender, ToggledEventArgs e)
        {
            Preferences.Set(ShowGroupsKey, e.Value);
            // MessagingCenter.Send(this, "Contacts.ShowGroupsChanged", e.Value);
        }
    }
}
