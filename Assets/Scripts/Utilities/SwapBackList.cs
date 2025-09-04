using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwapBackList<T> : IEnumerable<T> where T : IIndexed
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
    public int GetIndex(T item) => item.Index;

    public void Add(T addItem)
    {
        if (_lastIndex < _items.Count)
            _items[_lastIndex] = addItem;
        else
            _items.Add(addItem);

        addItem.Index = _lastIndex++;
    }

    public void Remove(T removeItem)
    {
        int index = removeItem.Index;

#if UNITY_EDITOR
        if (index < 0 || index >= _lastIndex)
            throw new System.IndexOutOfRangeException();
#endif

        _lastIndex--;

        T lastItem = _items[_lastIndex];
        _items[index] = lastItem;
        lastItem.Index = index;
        removeItem.Index = -1;
    }

    public bool Contains(T item)
    {
        int index = item.Index;
        return index >= 0 && index < _lastIndex && _items[index].Equals(item);
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