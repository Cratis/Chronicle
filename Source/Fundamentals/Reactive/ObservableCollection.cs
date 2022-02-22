// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Concurrent;
using System.Reactive.Subjects;

namespace Aksio.Cratis.Reactive;

/// <summary>
/// Represents an implementation of <see cref="IObservableCollection{TItem}"/>.
/// </summary>
/// <typeparam name="TItem">Type of item.</typeparam>
public class ObservableCollection<TItem> : IObservableCollection<TItem>
{
    readonly ReplaySubject<TItem> _added = new();
    readonly ReplaySubject<TItem> _removed = new();
    ConcurrentBag<TItem> _internalCollection = new();
    ConcurrentBag<IObserver<IEnumerable<TItem>>> _observers = new();

    /// <inheritdoc/>
    public IObservable<TItem> Added => _added;

    /// <inheritdoc/>
    public IObservable<TItem> Removed => _removed;

    /// <inheritdoc/>
    public int Count => _internalCollection.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <inheritdoc/>
    public event ObservableCollectionCleared Cleared = () => { };

    /// <inheritdoc/>
    public bool Contains(TItem item) => _internalCollection.Contains(item);

    /// <inheritdoc/>
    public void CopyTo(TItem[] array, int arrayIndex) => _internalCollection.CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public void Add(TItem item)
    {
        lock (_internalCollection)
        {
            _internalCollection.Add(item);
        }
        _added.OnNext(item);
        OnChange();
    }

    /// <inheritdoc/>
    public void Clear()
    {
        lock (_internalCollection)
        {
            _internalCollection.Clear();
        }
        Cleared();
        OnChange();
    }

    /// <inheritdoc/>
    public bool Remove(TItem item)
    {
        lock (_internalCollection)
        {
            _internalCollection = new(_internalCollection.Except(new[] { item }));
        }
        _removed.OnNext(item);
        OnChange();

        return true;
    }

    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<IEnumerable<TItem>> observer)
    {
        _observers.Add(observer);
        observer.OnNext(_internalCollection.ToArray());
        return new ObservableCollectionObserver(() => _observers = new(_observers.Except(new[] { observer })));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _added.Dispose();
        _removed.Dispose();
        _observers.Clear();
        _internalCollection.Clear();
    }

    /// <inheritdoc/>
    public IEnumerator<TItem> GetEnumerator() => _internalCollection.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => _internalCollection.GetEnumerator();

    void OnChange()
    {
        foreach (var observer in _observers)
        {
            observer.OnNext(_internalCollection.ToArray());
        }
    }
}
