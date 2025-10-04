using System.Collections.Generic;

namespace Utilities.Collections
{
    public class SwapBackListIndexed<T> : SwapBack<T> where T : IIndexed
    {
        public SwapBackListIndexed(int capacity) : base(capacity) { }

        public override void Add(T item)
        {
            if (_lastIndex < _items.Count)
                _items[_lastIndex] = item;
            else
                _items.Add(item);

            item.Index = _lastIndex++;
        }

        public override void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
                Add(item);
        }

        public override void RemoveAtSwapBack(T item)
        {
            int index = item.Index;

#if UNITY_EDITOR
            if (index < 0 || index >= _lastIndex)
                throw new System.IndexOutOfRangeException();
#endif

            _lastIndex--;

            T lastItem = _items[_lastIndex];
            _items[index] = lastItem;
            lastItem.Index = index;
            item.Index = -1;
        }

        public override void RemoveAtSwapBack(int index)
        {
#if UNITY_EDITOR
            if (index < 0 || index >= _lastIndex)
                throw new System.IndexOutOfRangeException();
#endif

            _lastIndex--;

            T lastItem = _items[_lastIndex];
            _items[index] = lastItem;
            lastItem.Index = index;
            _items[index].Index = -1;
        }

        public override void RemoveRange(IEnumerable<T> items)
        {
            foreach (var item in items)
                RemoveAtSwapBack(item);
        }

        public override bool Contains(T item)
        {
            int index = item.Index;
            return index >= 0 && index < _lastIndex && _items[index].Equals(item);
        }

        public override int IndexOf(T item)
        {
            int index = item.Index;
            if (index >= 0 && index < _lastIndex && _items[index].Equals(item))
                return index;
            return -1;
        }
    }
}