using System.Collections;
using System.Collections.Generic;

public abstract class SwapBack<T> : IEnumerable<T>
{
    protected readonly List<T> _items;
    protected int _lastIndex;

    public int Count => _lastIndex;

    public SwapBack(int capacity = 10)
    {
        _items = new List<T>(capacity);
        _lastIndex = 0;
    }

    public T this[int index]
    {
        get
        {
#if UNITY_EDITOR
            if (index < 0 || index >= _lastIndex)
                throw new System.IndexOutOfRangeException();
#endif
            return _items[index];
        }
        set
        {
#if UNITY_EDITOR
            if (index < 0 || index >= _lastIndex)
                throw new System.IndexOutOfRangeException();
#endif
            _items[index] = value;
        }
    }

    public void Clear() => _lastIndex = 0;

    public abstract void Add(T item);
    public abstract void AddRange(IEnumerable<T> items);
    public abstract void RemoveAtSwapBack(T item);
    public abstract void RemoveAtSwapBack(int index);
    public abstract void RemoveRange(IEnumerable<T> items);
    public abstract bool Contains(T item);
    public abstract int IndexOf(T item);

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < _lastIndex; i++)
        {
            yield return _items[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
