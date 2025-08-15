using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using AChatFull.Views;
using System.Threading.Tasks;
using AChatFull.Utils;

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

            var settings = new NavigationPage(new ProfilePage())
            {
                Title = "You",
            };

            Children.Add(chats);
            Children.Add(contacts);
            Children.Add(settings);

            // Android: вкладки внизу
            this.On<Android>()
                .SetToolbarPlacement(ToolbarPlacement.Bottom)
                .SetIsSwipePagingEnabled(true)
                .SetIsSmoothScrollEnabled(true);

            _ = SetProfileTabIconAsync(settings, repo);

        }

        private async Task SetProfileTabIconAsync(NavigationPage settingsPage, ChatRepository repo)
        {
            try
            {
                var user = await repo.GetCurrentUserProfileAsync();
                if (user == null) return;

                var initials = AvatarIconBuilder.MakeInitials(string.Format("{0} {1}", user.FirstName, user.LastName));

                // ВАЖНО: сюда передавайте локальный путь/имя ресурса/URI (content://, file://, bundle/drawable имя)
                var icon = await AvatarIconBuilder.BuildAsync(
                    user.AvatarUrl,   // локальный источник аватарки
                    initials,
                    user.Presence.ToString(),
                    28
                );

                if (icon != null)
                    settingsPage.IconImageSource = icon;
            }
            catch
            {
                // fail-safe: оставляем стандартную иконку
            }
        }
    }
}