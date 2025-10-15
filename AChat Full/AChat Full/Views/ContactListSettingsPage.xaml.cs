using Xamarin.Forms;
using Xamarin.Essentials;

namespace AChatFull.Views
{
    public partial class ContactListSettingsPage : ContentPage
    {
        const string ColumnsKey = "contacts.columns";     // int: 1 или 2
        const string ShowGroupsKey = "contacts.showgroups"; // bool

        public ContactListSettingsPage()
        {
            InitializeComponent();

            // загрузим сохранённые настройки (дефолты: 1 колонка, группы видны)
            var cols = Preferences.Get(ColumnsKey, 1);
            ColumnsValueLabel.Text = cols.ToString();

            var showGroups = Preferences.Get(ShowGroupsKey, true);
            ShowGroupsSwitch.IsToggled = showGroups;
        }

        async void OnBackClicked(object sender, System.EventArgs e)
        {
            if (Navigation.ModalStack?.Count > 0)
                await Navigation.PopModalAsync();
            else
                await Navigation.PopAsync();
        }

        async void OnColumnsTapped(object sender, System.EventArgs e)
        {
            var choice = await DisplayActionSheet("Number of columns", "Cancel", null, "1", "2");
            if (choice == "1" || choice == "2")
            {
                Preferences.Set(ColumnsKey, int.Parse(choice));
                ColumnsValueLabel.Text = choice;

                // TODO: при желании — уведомить ContactsPage через MessagingCenter
                // MessagingCenter.Send(this, "Contacts.ColumnsChanged", int.Parse(choice));
            }
        }

        void OnShowGroupsToggled(object sender, ToggledEventArgs e)
        {
            Preferences.Set(ShowGroupsKey, e.Value);
            // TODO: при желании — уведомить ContactsPage через MessagingCenter
            // MessagingCenter.Send(this, "Contacts.ShowGroupsChanged", e.Value);
        }
    }
}
