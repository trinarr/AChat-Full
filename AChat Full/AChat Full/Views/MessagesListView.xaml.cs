using System;
using System.Collections;
using System.Collections.Specialized;
using Xamarin.Forms;
using AChatFull.ViewModels;

namespace AChatFull.Views
{
    public partial class MessagesListView : ContentView
    {
        // �����, �� �������� ������� ����� (����� ����������)
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

            // ������� �� ������ ���������
            if (_subscribedTo != null)
            {
                _subscribedTo.CollectionChanged -= OnMessagesCollectionChanged;
                _subscribedTo = null;
            }

            // �������� �� ����� ���������
            if (BindingContext is ChatViewModel vm && vm.Messages is INotifyCollectionChanged incc)
            {
                _subscribedTo = incc;
                _subscribedTo.CollectionChanged += OnMessagesCollectionChanged;

                // ������ ���� ������� �� �� �� ���������
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

            FewContainer.IsVisible = few;   // ���� ������ � ����
            List.IsVisible = !few;          // ������������������ ������

            // � ������ ������ ������ �� �������� � � ��� � ����
            // � �������� ����� ������� ��� ��� (������ � ����������, ���������� � �.�.)
        }

        // ��������� � ������� ��������� (������������ ChatPage)
        public void ScrollToBottom(bool animate)
        {
            if (FewContainer.IsVisible)
                return; // � ������ ������ �������� ��� ������� � ����

            if (List?.ItemsSource is IList items && items.Count > 0)
            {
                var last = items[items.Count - 1];
                List.ScrollTo(last, position: ScrollToPosition.End, animate: animate);
            }
        }

        // ���������� ������� ��� ��������� ����� � ������ � �������� ������
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

        // ����� �� ChatPage ��� �������� � ���������� ������
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