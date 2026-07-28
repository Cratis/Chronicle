// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Keys;

namespace Cratis.Chronicle.Storage.InMemory.Keys;

/// <summary>
/// Represents an in-memory implementation of <see cref="IObserverKeyIndex"/>.
/// </summary>
/// <remarks>
/// Backed by an in-memory ordered collection of distinct keys. Unlike the event-store-derived
/// implementations, the in-memory index has no per-key <see cref="EventSequenceNumber"/>, so
/// <see cref="GetKeys"/> returns every added key in insertion order.
/// </remarks>
public sealed class ObserverKeyIndex : IObserverKeyIndex
{
    readonly List<Key> _keys = [];
    readonly HashSet<Key> _seen = [];
    readonly object _lock = new();

    /// <inheritdoc/>
    public IObserverKeys GetKeys(EventSequenceNumber fromEventSequenceNumber)
    {
        lock (_lock)
        {
            return new ObserverKeys(_keys.ToArray());
        }
    }

    /// <inheritdoc/>
    public Task Add(Key key)
    {
        lock (_lock)
        {
            if (_seen.Add(key))
            {
                _keys.Add(key);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Rebuild()
    {
        lock (_lock)
        {
            _keys.Clear();
            _seen.Clear();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Represents an in-memory implementation of <see cref="IObserverKeys"/>.
    /// </summary>
    /// <param name="keys">The snapshot of keys to enumerate.</param>
    sealed class ObserverKeys(IReadOnlyList<Key> keys) : IObserverKeys
    {
        /// <inheritdoc/>
        public async IAsyncEnumerator<Key> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            foreach (var key in keys)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return key;
            }

            await Task.CompletedTask;
        }
    }
}
