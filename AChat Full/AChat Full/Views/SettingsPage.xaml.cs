using Xamarin.Forms;
using Xamarin.Essentials;

namespace AChatFull.Views
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();
            VersionLabel.Text = "Version: " + AppInfo.VersionString;
        }

        async void OnBackClicked(object sender, System.EventArgs e)
        {
            // Закрыть модально, если открыта модально; иначе — обычный PopAsync
            if (Navigation.ModalStack?.Count > 0)
                await Navigation.PopModalAsync();
            else
                await Navigation.PopAsync();
        }

        async void OnNotificationsTapped(object sender, System.EventArgs e)
        {
            // TODO: переход к экрану уведомлений
            //await DisplayAlert("Notifications", "Открыть настройки уведомлений.", "OK");
        }

        async void OnContactListTapped(object sender, System.EventArgs e)
        {
            // TODO: переход к экрану настроек списка контактов

            await Navigation.PushModalAsync(new NavigationPage(new ContactListSettingsPage()));
        }
    }
}
