using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using AChatFull.Views;
using System.Threading.Tasks;
using System;
using AChatFull.Utils;

namespace AChatFull
{
    public class MainTabsPage : Xamarin.Forms.TabbedPage
    {
        NavigationPage _youPlaceholderNav;
        bool _youReplaced;
        ChatRepository _repo;

        public MainTabsPage(string userToken, ChatRepository repo)
        {
            _repo = repo;

            NavigationPage chats = new NavigationPage(new ChatsListPage(userToken, _repo)) { Title = "Chats", IconImageSource = "tab_chat.png" };
            NavigationPage contacts = new NavigationPage(new ContactsPage(_repo)) { Title = "Contacts", IconImageSource = "tab_contacts.png" };

            _youPlaceholderNav = new NavigationPage(new ContentPage { Title = "You" })
            {
                Title = "You",
            };

            //Children.Add(chats);
            Children.Add(contacts);
            Children.Add(_youPlaceholderNav);

            // Android: вкладки внизу
            On<Android>()
                .SetToolbarPlacement(ToolbarPlacement.Bottom)
                .SetIsSwipePagingEnabled(true)
                .SetIsSmoothScrollEnabled(true);

            // инициализируем профиль при первом показе нужной вкладки
            this.CurrentPageChanged += async (s, e) => await TryInitCurrentLazyPageAsync();
            // на всякий случай — если вкладка профиля выбрана по умолчанию
            Device.BeginInvokeOnMainThread(async () => await TryInitCurrentLazyPageAsync());

        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (!_youReplaced)
            {
                _youReplaced = true;
                // Деферим подмену вкладки до момента, когда таббар уже показан (PIN закрыт)
                Device.BeginInvokeOnMainThread(async () => await ReplaceYouTabAsync().ConfigureAwait(false));
            }
        }

        private async Task ReplaceYouTabAsync()
        {
            try
            {
                // 2) Создаём настоящую страницу профиля только сейчас
                var profilePage = new ProfilePage(_repo);
                var youNav = new NavigationPage(profilePage)
                {
                    Title = "Settings",
                    IconImageSource = _youPlaceholderNav.IconImageSource
                };

                // 3) Подменяем плейсхолдер на реальную вкладку без мерцания
                var idx = Children.IndexOf(_youPlaceholderNav);
                if (idx >= 0)
                {
                    Children.RemoveAt(idx);
                    Children.Insert(idx, youNav);
                }

                await InitializeProfileAsync(profilePage, youNav.Navigation).ConfigureAwait(false);

                _ = RefreshProfileTabIconSafeAsync(youNav);

                MessagingCenter.Subscribe<object>(this, "ProfileChanged", async _ =>
                {
                    await RefreshProfileTabIconSafeAsync(youNav).ConfigureAwait(false);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ReplaceYouTabAsync error: " + ex);
                // В худшем случае останется плейсхолдер — табы всё равно работают, PIN закроется.
            }
        }

        private async Task InitializeProfileAsync(Page page, INavigation nav)
        {
            try
            {
                // 1) Если страница поддерживает ленивую инициализацию — используем её
                if (page is ILazyInitPage lazy && !lazy.IsInitialized)
                {
                    await lazy.EnsureInitAsync(nav).ConfigureAwait(false);
                    return;
                }

                // 2) Иначе — пробуем вызвать у уже заданной VM
                var vm = page.BindingContext as ILazyInitViewModel;
                if (vm != null)
                {
                    await vm.InitializeAsync(nav).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("InitializeProfileAsync error: " + ex);
            }
        }

        private async Task RefreshProfileTabIconSafeAsync(NavigationPage targetNav)
        {
            try
            {
                // грузим профиль НЕ на UI-потоке
                var user = await _repo.GetCurrentUserProfileAsync().ConfigureAwait(false);
                if (user == null) return;

                var initials = AvatarIconBuilder.MakeInitials($"{user.FirstName} {user.LastName}");
                var dotKey = user.Presence.ToDotKey();
                var icon = await AvatarIconBuilder.BuildAsync(
                    user.AvatarUrl,   // локальный путь/ресурс
                    initials,
                    dotKey,    // "Online"/"Away"/"Busy"/"Offline"
                    28
                ).ConfigureAwait(false);

                if (icon != null)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        try { targetNav.IconImageSource = icon; } catch { /* ignore */ }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("RefreshProfileTabIconSafeAsync error: " + ex);
            }
        }

        private async Task TryInitCurrentLazyPageAsync()
        {
            try
            {
                if (!(CurrentPage is NavigationPage nav)) return;
                // Текущая отображаемая страница в стеке навигации
                var page = nav.CurrentPage;
                if (page is ILazyInit lazy && !lazy.IsInitialized)
                {
                    await lazy.EnsureInitAsync(nav.Navigation).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("TryInitCurrentLazyPageAsync error: " + ex);
            }
        }

private async Task SetProfileTabIconAsync(NavigationPage targetPage, ChatRepository repo)
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
                    targetPage.IconImageSource = icon;
            }
            catch
            {
                // fail-safe: оставляем стандартную иконку
            }
        }
    }
}