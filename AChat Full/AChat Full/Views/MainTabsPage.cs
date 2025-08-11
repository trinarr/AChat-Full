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
                Title = "Чаты",
                IconImageSource = "tab_chat.png"
            };

            var calls = new NavigationPage(new CallsPage())
            {
                Title = "Звонки",
                IconImageSource = "tab_call.png"
            };

            var contacts = new NavigationPage(new ContactsPage())
            {
                Title = "Контакты",
                IconImageSource = "tab_contacts.png"
            };

            var settings = new NavigationPage(new SettingsPage())
            {
                Title = "Настройки",
                IconImageSource = "tab_settings.png"
            };

            Children.Add(chats);
            Children.Add(calls);
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