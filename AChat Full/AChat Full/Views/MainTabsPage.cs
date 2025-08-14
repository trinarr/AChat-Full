using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using AChatFull.Views;

namespace AChatFull
{
    public class MainTabsPage : Xamarin.Forms.TabbedPage
    {
        public MainTabsPage(string userToken, ChatRepository repo)
        {
            Title = "AChat";

            var chats = new NavigationPage(new ChatsListPage(userToken, repo))
            {
                Title = "Chats",
                IconImageSource = "tab_chat.png"
            };

            var contacts = new NavigationPage(new ContactsPage(repo))
            {
                Title = "Contacts",
                IconImageSource = "tab_contacts.png"
            };

            var settings = new NavigationPage(new SettingsPage())
            {
                Title = "Settings",
                IconImageSource = "tab_settings.png"
            };

            Children.Add(chats);
            Children.Add(contacts);
            Children.Add(settings);

            // Android: вкладки внизу
            this.On<Android>()
                .SetToolbarPlacement(ToolbarPlacement.Bottom)
                .SetIsSwipePagingEnabled(true)
                .SetIsSmoothScrollEnabled(true);
        }
    }
}