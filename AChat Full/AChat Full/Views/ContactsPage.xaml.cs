using Xamarin.Forms;
using AChatFull.ViewModels;
using System;
using AChatFull.Services;
using Xamarin.Essentials;
using System.Collections.Generic;

namespace AChatFull.Views
{
    public partial class ContactsPage : ContentPage
    {
        private bool _isGrid = false;  // false = список, true = сетка

        private readonly ChatRepository _repo;
        public ContactsViewModel VM => BindingContext as ContactsViewModel;

        // BindableProperty, чтобы триггер в XAML реагировал
        public static readonly BindableProperty ShowGroupsProperty =
        BindableProperty.Create(nameof(ShowGroups), typeof(bool), typeof(ContactsPage), true,
        propertyChanged: (b, o, n) => ((ContactsPage)b).ApplyGroupsMode());


        public bool ShowGroups
        {
            get => (bool)GetValue(ShowGroupsProperty);
            set => SetValue(ShowGroupsProperty, value);
        }

        List<object> _flatContacts;

        public ContactsPage(ChatRepository repo)
        {
            InitializeComponent();
            _repo = repo;
            BindingContext = new ContactsViewModel(repo);

            // бридж поиска: при наборе текста обновляем VM.SearchText
            SearchEntry.TextChanged += (s, e) => VM.SearchText = e.NewTextValue;

            // Навигация на чат из VM
            MessagingCenter.Subscribe<ContactsViewModel, string>(this, "OpenChat", async (_, chatId) =>
            {
                var chatPage = new ChatPage(chatId, App.USER_TOKEN_TEST, _repo);
                await Application.Current.MainPage.Navigation.PushModalAsync(chatPage, animated: false);
            });
        }

        // 2) Применение настройки: изменяем ТОЛЬКО ContactsList
        void ApplyGroupsMode()
        {
            var vm = BindingContext;
            if (ShowGroups)
            {
                ContactsList.IsGrouped = true;

                // вернуть биндинг на группы
                ContactsList.ClearValue(ItemsView.ItemsSourceProperty);
                ContactsList.SetBinding(ItemsView.ItemsSourceProperty, new Binding("ContactsGroups", source: vm));
            }
            else
            {
                ContactsList.IsGrouped = false;

                // сформировать плоский источник из ContactsGroups
                _flatContacts = Flatten(BindingContext, "ContactsGroups");

                ContactsList.ClearValue(ItemsView.ItemsSourceProperty);
                ContactsList.ItemsSource = _flatContacts;
            }
        }

        // 3) flatten-утилита — без изменений
        List<object> Flatten(object vm, string groupsPropName)
        {
            if (vm == null) return new List<object>();
            var prop = vm.GetType().GetProperty(groupsPropName);
            var groupsObj = prop?.GetValue(vm);
            if (groupsObj is System.Collections.IEnumerable groups)
            {
                var result = new List<object>();
                foreach (var g in groups)
                    if (g is System.Collections.IEnumerable items)
                        foreach (var it in items) result.Add(it);
                return result;
            }
            return new List<object>();
        }

        void ApplyLayoutMode()
        {
            if (_isGrid)
            {
                // сетка 2 столбца
                ContactsList.ItemsLayout = new GridItemsLayout(2, ItemsLayoutOrientation.Vertical)
                { HorizontalItemSpacing = 0, VerticalItemSpacing = 0 };

                ResultsList.ItemsLayout = new GridItemsLayout(2, ItemsLayoutOrientation.Vertical)
                { HorizontalItemSpacing = 0, VerticalItemSpacing = 0 };

                // уменьшенные размеры для компактного вида
                Resources["Size.PresenceIcon"] = 24.0;  // было 30
                Resources["Size.NameFont"] = 14.0;  // было 18
                Resources["Size.StatusFont"] = 11.0;  // было 12
            }
            else
            {
                // обычный список
                ContactsList.ItemsLayout = LinearItemsLayout.Vertical;
                ResultsList.ItemsLayout = LinearItemsLayout.Vertical;

                // размеры по умолчанию (список)
                Resources["Size.PresenceIcon"] = 30.0;
                Resources["Size.NameFont"] = 18.0;
                Resources["Size.StatusFont"] = 12.0;
            }
        }

        void OnGroupHeaderTapped(object sender, EventArgs e)
        {
            if (sender is BindableObject bo && bo.BindingContext is UserGroup group)
            {
                group.IsExpanded = !group.IsExpanded;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // 1) Прочитать настройку колонок (SharedPreferences, fallback — Essentials)
            var svc = DependencyService.Get<ISettingsService>();
            var cols = svc?.GetInt(AppearanceSettingsPage.ColumnsKey, 1)
                       ?? Preferences.Get(AppearanceSettingsPage.ColumnsKey, 1);

            _isGrid = cols >= 2;

            // 2) Применяем ShowGroups
            var showGroups = svc?.GetBool(AppearanceSettingsPage.ShowGroupsKey, true)
                           ?? Preferences.Get(AppearanceSettingsPage.ShowGroupsKey, true);

            VM.IsSearchMode = false;   // обычный режим при входе
            await VM.LoadContactsAsync(); // полный список контактов

            ApplyLayoutMode();
            ShowGroups = showGroups; // триггер в XAML сам спрячет/покажет хедеры

            // 3) Live-обновления из экрана настроек
            MessagingCenter.Subscribe<AppearanceSettingsPage, int>(this, "Contacts.ColumnsChanged",
                (sender, newCols) =>
                {
                    _isGrid = newCols >= 2;
                    ApplyLayoutMode();
                });

            MessagingCenter.Subscribe<AppearanceSettingsPage, bool>(this, "Contacts.ShowGroupsChanged",
                (sender, value) =>
                {
                    ShowGroups = value; // хедеры мгновенно скрываются/появляются
                });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            MessagingCenter.Unsubscribe<AppearanceSettingsPage, int>(this, "Contacts.ColumnsChanged");
            MessagingCenter.Unsubscribe<AppearanceSettingsPage, bool>(this, "Contacts.ShowGroupsChanged");
        }

        private void OnSearchBackClicked(object sender, EventArgs e)
        {
            SearchEntry?.Unfocus();
            VM.SearchText = string.Empty; // очистить фильтр
            VM.IsSearchMode = false;
        }

        private void OnSearchIconClicked(object sender, EventArgs e)
        {
            VM.IsSearchMode = true;
            Device.BeginInvokeOnMainThread(() => SearchEntry?.Focus());
        }
    }
}