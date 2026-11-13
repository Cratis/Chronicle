// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// A capacity-bounded, oldest-first map, accessed under <see cref="CacheEncryptionKeyStorage"/>'s lock.
/// Removing an entry also removes its ordering node, so invalidations cannot leave an unbounded queue behind.
/// </summary>
/// <typeparam name="TKey">The cache key.</typeparam>
/// <typeparam name="TValue">The cached value.</typeparam>
/// <param name="capacity">The maximum number of retained entries.</param>
internal sealed class BoundedEncryptionKeyCache<TKey, TValue>(int capacity)
    where TKey : notnull
{
    readonly Dictionary<TKey, (TValue Value, LinkedListNode<TKey> Order)> _entries = [];
    readonly LinkedList<TKey> _order = [];

    /// <summary>
    /// Gets the number of retained entries.
    /// </summary>
    internal int Count => _entries.Count;

    /// <summary>
    /// Gets the retained keys.
    /// </summary>
    internal IEnumerable<TKey> Keys => _entries.Keys;

    /// <summary>
    /// Checks whether a key is cached.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <returns>Whether the key is cached.</returns>
    internal bool ContainsKey(TKey key) => _entries.ContainsKey(key);

    /// <summary>
    /// Tries to get a cached value.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The cached value, if present.</param>
    /// <returns>Whether the key is cached.</returns>
    internal bool TryGetValue(TKey key, out TValue value)
    {
        if (_entries.TryGetValue(key, out var entry))
        {
            value = entry.Value;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// Stores a value, evicting the oldest entry if the capacity is exceeded.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    internal void Set(TKey key, TValue value)
    {
        if (_entries.TryGetValue(key, out var entry))
        {
            _entries[key] = (value, entry.Order);
            return;
        }

        _entries.Add(key, (value, _order.AddLast(key)));
        if (_entries.Count > capacity)
        {
            Remove(_order.First!.Value);
        }
    }

    /// <summary>
    /// Removes an entry and its ordering node.
    /// </summary>
    /// <param name="key">The cache key.</param>
    internal void Remove(TKey key)
    {
        if (_entries.Remove(key, out var entry))
        {
            _order.Remove(entry.Order);
        }
    }
}
