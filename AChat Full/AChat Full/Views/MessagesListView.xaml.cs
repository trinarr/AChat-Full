using System;
using System.Collections;
using System.Collections.Specialized;
using Xamarin.Forms;
using AChatFull.ViewModels;

namespace AChatFull.Views
{
    public partial class MessagesListView : ContentView
    {
        // Порог, до которого считаем «мало» (можно подстроить)
        const int FewThreshold = 10;

        INotifyCollectionChanged _subscribedTo;

        public MessagesListView()
        {
            InitializeComponent();
            this.SizeChanged += (_, __) => UpdateMode();
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            // Отписка от старой коллекции
            if (_subscribedTo != null)
            {
                _subscribedTo.CollectionChanged -= OnMessagesCollectionChanged;
                _subscribedTo = null;
            }

            // Подписка на новую коллекцию
            if (BindingContext is ChatViewModel vm && vm.Messages is INotifyCollectionChanged incc)
            {
                _subscribedTo = incc;
                _subscribedTo.CollectionChanged += OnMessagesCollectionChanged;

                // «Малый» стек смотрит на те же сообщения
                BindableLayout.SetItemsSource(FewStack, vm.Messages);
            }

            UpdateMode();
        }

        void OnMessagesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Device.BeginInvokeOnMainThread(UpdateMode);
        }

        void UpdateMode()
        {
            var vm = BindingContext as ChatViewModel;
            var count = vm?.Messages?.Count ?? 0;
            var few = count > 0 && count <= FewThreshold;

            FewContainer.IsVisible = few;   // стек прибит к низу
            List.IsVisible = !few;          // виртуализированный список

            // В «малом» режиме ничего не скроллим — и так у низа
            // В «большом» режим остаётся как был (скролл к последнему, дозагрузка и т.п.)
        }

        // Прокрутка к нижнему сообщению (используется ChatPage)
        public void ScrollToBottom(bool animate)
        {
            if (FewContainer.IsVisible)
                return; // в «малом» режиме элементы уже прижаты к низу

            if (List?.ItemsSource is IList items && items.Count > 0)
            {
                var last = items[items.Count - 1];
                List.ScrollTo(last, position: ScrollToPosition.End, animate: animate);
            }
        }

        // Дозагрузка истории при прокрутке вверх — только в «большом» режиме
        async void OnScrolled(object sender, ItemsViewScrolledEventArgs e)
        {
            if (FewContainer.IsVisible) return;

            if (e.FirstVisibleItemIndex <= 2)
            {
                if (BindingContext is ChatViewModel vm && vm.CanLoadMore && !vm.IsMessagesLoading)
                {
                    var items = vm.Messages;
                    if (items == null || items.Count == 0) return;

                    var anchorIndex = Math.Max(e.FirstVisibleItemIndex, 0);
                    object anchor = null;
                    if (anchorIndex < items.Count)
                        anchor = items[anchorIndex];

                    var added = await vm.LoadMoreOlderAsync();
                    if (added > 0 && anchor != null)
                        List.ScrollTo(anchor, position: ScrollToPosition.Start, animate: false);
                }
            }
        }

        // Вызов из ChatPage при закрытии — «облегчаем» список
        public void Detach()
        {
            try
            {
                List.ItemsSource = null;
                BindableLayout.SetItemsSource(FewStack, null);

                if (_subscribedTo != null)
                {
                    _subscribedTo.CollectionChanged -= OnMessagesCollectionChanged;
                    _subscribedTo = null;
                }
                this.BindingContext = null;
            }
            catch { /* no-op */ }
        }
    }
}