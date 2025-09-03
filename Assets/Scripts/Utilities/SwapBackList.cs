using System.Collections;
using System.Collections.Generic;

public class SwapBackList<T> : IEnumerable<T>
{
    private List<T> _items;
    private int _lastIndex;

    public int Count => _lastIndex;

    public SwapBackList(int capacity = 10)
    {
        _items = new List<T>(capacity);
        _lastIndex = 0;
    }

    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= _lastIndex)
                throw new System.IndexOutOfRangeException();
            return _items[index];
        }
        set
        {
            if (index < 0 || index >= _lastIndex)
                throw new System.IndexOutOfRangeException();
            _items[index] = value;
        }
    }

    public int GetIndex(T item) => _items.IndexOf(item);
    public void Clear() => _lastIndex = 0;

    public void Add(T item)
    {
        if (_lastIndex < _items.Count)
            _items[_lastIndex] = item;
        else
            _items.Add(item);

        _lastIndex++;
    }

    public bool Remove(T item)
    {
        int index = _items.IndexOf(item);
        if (index < 0 || index >= _lastIndex)
            return false;

        _items[index] = _items[--_lastIndex];
        return true;
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _lastIndex)
            throw new System.IndexOutOfRangeException();

        _items[index] = _items[--_lastIndex];
    }

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