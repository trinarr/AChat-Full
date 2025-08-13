using System;
using System.Collections;
using Xamarin.Forms;
using AChatFull.ViewModels;

namespace AChatFull.Views
{
    public partial class MessagesListView : ContentView
    {
        public MessagesListView()
        {
            InitializeComponent();
        }

        public void ScrollToBottom(bool animate)
        {
            if (List?.ItemsSource is IList items && items.Count > 0)
            {
                var last = items[items.Count - 1];
                List.ScrollTo(last, position: ScrollToPosition.End, animate: animate);
            }
        }

        public void Detach()
        {
            // Отключаем ItemsSource, чтобы CollectionView мгновенно «облегчился»
            List.ItemsSource = null;

            // Дополнительно можно сбросить BindingContext — быстрее освобождается
            this.BindingContext = null;
        }

        async void OnScrolled(object sender, ItemsViewScrolledEventArgs e)
        {
            // When close to top, request older messages
            if (e.FirstVisibleItemIndex <= 2)
            {
                if (BindingContext is ChatViewModel vm && vm.CanLoadMore && !vm.IsMessagesLoading)
                {
                    var items = vm.Messages;
                    if (items == null || items.Count == 0) return;

                    // Anchor the first visible item to preserve scroll position
                    var anchorIndex = Math.Max(e.FirstVisibleItemIndex, 0);
                    object anchor = null;
                    if (anchorIndex < items.Count)
                        anchor = items[anchorIndex];

                    var added = await vm.LoadMoreOlderAsync();
                    if (added > 0 && anchor != null)
                    {
                        List.ScrollTo(anchor, position: ScrollToPosition.Start, animate: false);
                    }
                }
            }
        }
    }
}
