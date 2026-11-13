// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Bounds the silo's retained compiled graphs, independently of the number of namespaces.
/// Eviction drops only the cache reference: a live pipeline can continue using its graph.
/// </summary>
/// <param name="capacity">The maximum number of retained graphs.</param>
/// <param name="countChanged">Records the count after a cache mutation.</param>
internal sealed class CompiledProjectionCache(int capacity, Action<int> countChanged)
{
    readonly Lock _lock = new();
    readonly Dictionary<(EventStoreName Store, EventStoreNamespaceName Namespace, ProjectionId Id), LinkedListNode<Entry>> _entries = [];
    readonly LinkedList<Entry> _recency = new();

    /// <summary>Gets the number of retained graphs.</summary>
    internal int Count
    {
        get
        {
            lock (_lock) return _entries.Count;
        }
    }

    /// <summary>Tries to get a graph and marks a cache hit as recently used.</summary>
    /// <param name="store">The event store.</param>
    /// <param name="namespace">The event store namespace.</param>
    /// <param name="id">The projection identifier.</param>
    /// <param name="projection">The graph, if cached.</param>
    /// <returns>Whether the graph was cached.</returns>
    internal bool TryGet(EventStoreName store, EventStoreNamespaceName @namespace, ProjectionId id, [NotNullWhen(true)] out IProjection? projection)
    {
        lock (_lock)
        {
            if (!_entries.TryGetValue((store, @namespace, id), out var node))
            {
                projection = null;
                return false;
            }

            _recency.Remove(node);
            _recency.AddFirst(node);
            projection = node.Value.Projection;
            return true;
        }
    }

    /// <summary>Retains a graph, dropping the least recently used reference at capacity.</summary>
    /// <param name="store">The event store.</param>
    /// <param name="namespace">The event store namespace.</param>
    /// <param name="id">The projection identifier.</param>
    /// <param name="projection">The compiled graph.</param>
    internal void Store(EventStoreName store, EventStoreNamespaceName @namespace, ProjectionId id, IProjection projection)
    {
        lock (_lock)
        {
            var key = (store, @namespace, id);
            if (_entries.Remove(key, out var existing)) _recency.Remove(existing);
            _entries[key] = _recency.AddFirst(new Entry(store, @namespace, id, projection));
            while (_entries.Count > capacity)
            {
                var oldest = _recency.Last!;
                _entries.Remove((oldest.Value.Store, oldest.Value.Namespace, oldest.Value.Id));
                _recency.RemoveLast();
            }

            countChanged(_entries.Count);
        }
    }

    /// <summary>Drops all namespace graphs for a definition.</summary>
    /// <param name="store">The event store.</param>
    /// <param name="id">The projection identifier.</param>
    internal void Remove(EventStoreName store, ProjectionId id)
    {
        lock (_lock)
        {
            foreach (var key in _entries.Keys.Where(key => key.Store == store && key.Id == id).ToArray())
            {
                _recency.Remove(_entries[key]);
                _entries.Remove(key);
            }

            countChanged(_entries.Count);
        }
    }

    sealed record Entry(EventStoreName Store, EventStoreNamespaceName Namespace, ProjectionId Id, IProjection Projection);
}
