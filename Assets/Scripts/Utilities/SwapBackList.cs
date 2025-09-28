using System.Collections.Generic;

public class SwapBackList<T> : SwapBack<T>
{
    public SwapBackList(int capacity) : base(capacity) { }

    public override void Add(T addItem)
    {
        if (_lastIndex < _items.Count)
            _items[_lastIndex] = addItem;
        else
            _items.Add(addItem);

        _lastIndex++;
    }

    public override void AddRange(IEnumerable<T> items)
    {
        foreach (var item in items)
            Add(item);
    }

    public override void RemoveAtSwapBack(T item)
    {
        int index = IndexOf(item);

#if UNITY_EDITOR
        if (index < 0 || index >= _lastIndex)
            throw new System.IndexOutOfRangeException();
#endif

        _lastIndex--;

        T lastItem = _items[_lastIndex];
        _items[index] = lastItem;
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
    }

    public override void RemoveRange(IEnumerable<T> items)
    {
        foreach (var item in items)
            RemoveAtSwapBack(item);
    }

    public override bool Contains(T item) => _items.Contains(item);
    public override int IndexOf(T item) => _items.IndexOf(item);
}