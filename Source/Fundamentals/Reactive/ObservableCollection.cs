// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Concurrent;
using System.Reactive.Subjects;

namespace Cratis.Reactive
{
    /// <summary>
    /// Represents an implementation of <see cref="IObservableCollection{TItem}"/>.
    /// </summary>
    /// <typeparam name="TItem">Type of item.</typeparam>
    public class ObservableCollection<TItem> : IObservableCollection<TItem>
    {
        readonly ReplaySubject<TItem> _added = new();
        readonly ReplaySubject<TItem> _removed = new();
        ConcurrentBag<TItem> _internalCollection = new();

        /// <inheritdoc/>
        public event ObservableCollectionCleared Cleared = () => { };

        /// <inheritdoc/>
        public IObservable<TItem> Added => _added;

        /// <inheritdoc/>
        public IObservable<TItem> Removed => _removed;

        /// <inheritdoc/>
        public int Count => _internalCollection.Count;

        /// <inheritdoc/>
        public bool IsReadOnly => false;

        /// <inheritdoc/>
        public void Add(TItem item)
        {
            lock (_internalCollection)
            {
                _internalCollection.Add(item);
            }
            _added.OnNext(item);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            lock (_internalCollection)
            {
                _internalCollection.Clear();
            }
            Cleared();
        }

        /// <inheritdoc/>
        public bool Contains(TItem item) => _internalCollection.Contains(item);

        /// <inheritdoc/>
        public void CopyTo(TItem[] array, int arrayIndex) => _internalCollection.CopyTo(array, arrayIndex);

        /// <inheritdoc/>
        public void Dispose()
        {
            _added.Dispose();
            _removed.Dispose();
            _internalCollection.Clear();
        }

        /// <inheritdoc/>
        public IEnumerator<TItem> GetEnumerator() => _internalCollection.GetEnumerator();

        /// <inheritdoc/>
        public bool Remove(TItem item)
        {
            lock (_internalCollection)
            {
                _internalCollection = new(_internalCollection.Except(new[] { item }));
            }
            _removed.OnNext(item);

            return true;
        }

        IEnumerator IEnumerable.GetEnumerator() => _internalCollection.GetEnumerator();
    }
}
