using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace AChatFull.Utils
{
    public class ObservableRangeCollection<T> : ObservableCollection<T>
    {
        public void AddRange(IEnumerable<T> range)
        {
            if (range == null) return;
            foreach (var item in range) Items.Add(item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void InsertRange(int index, IEnumerable<T> range)
        {
            if (range == null) return;
            var i = index;
            foreach (var item in range)
            {
                Items.Insert(i, item);
                i++;
            }
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void ReplaceRange(IEnumerable<T> range)
        {
            Items.Clear();
            AddRange(range);
        }
    }
}