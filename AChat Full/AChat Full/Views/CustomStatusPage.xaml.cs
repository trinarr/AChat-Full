using System;
using Xamarin.Forms;

namespace AChatFull.Views
{
    public partial class CustomStatusPage : ContentPage
    {
        public CustomStatusPage()
        {
            InitializeComponent();
            ClearAfterPicker.SelectedIndex = 5; // Don't clear
        }

        async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                var model = new CustomStatusModel
                {
                    Emoji = EmojiEntry.Text,
                    Text = TextEntry.Text,
                    ClearPolicy = ClearAfterPicker.SelectedIndex,
                    DoNotDisturb = DndSwitch.IsToggled
                };

                var repo = DependencyService.Get<ChatRepository>() ?? new ChatRepository(App.DBPATH, App.USER_TOKEN_TEST);
                await repo.UpdateCustomStatusAsync(model);

                if (model.DoNotDisturb)
                    await repo.UpdatePresenceAsync("Busy");

                MessagingCenter.Send<object>(this, "ProfileChanged");
                await Navigation.PopAsync();
            }
            catch
            {
                await DisplayAlert("Error", "Failed to update status.", "OK");
            }
        }

        async void OnClearClicked(object sender, EventArgs e)
        {
            try
            {
                var repo = DependencyService.Get<ChatRepository>() ?? new ChatRepository(App.DBPATH, App.USER_TOKEN_TEST);
                await repo.ClearCustomStatusAsync();
                MessagingCenter.Send<object>(this, "ProfileChanged");
                await Navigation.PopAsync();
            }
            catch
            {
                await DisplayAlert("Error", "Failed to clear status.", "OK");
            }
        }
    }

    // Простейшая модель для кастомного статуса — положи в Models при желании
    public class CustomStatusModel
    {
        public string Emoji { get; set; }
        public string Text { get; set; }
        /// <summary>0:30m, 1:1h, 2:4h, 3:Today, 4:ThisWeek, 5:NoClear</summary>
        public int ClearPolicy { get; set; }
        public bool DoNotDisturb { get; set; }
    }
}
