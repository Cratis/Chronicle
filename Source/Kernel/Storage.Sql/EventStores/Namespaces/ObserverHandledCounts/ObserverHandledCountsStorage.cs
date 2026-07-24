// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverHandledCounts;

/// <summary>
/// Represents a transitional in-memory implementation of <see cref="IObserverHandledCountsStorage"/> for the SQL backend.
/// </summary>
/// <param name="eventStore">The <see cref="EventStoreName"/> the storage is for.</param>
/// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the storage is for.</param>
/// <remarks>
/// The SQL backend is still under construction in several areas. This implementation keeps handled-event
/// counts in process memory only, scoped to the event store and namespace, so the observer code path compiles
/// and runs. A migration-backed table can replace this once the SQL backend reaches feature parity with MongoDB.
/// </remarks>
public class ObserverHandledCountsStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace) : IObserverHandledCountsStorage
{
    static readonly ConcurrentDictionary<string, ConcurrentDictionary<(ObserverId ObserverId, string Partition), ConcurrentDictionary<EventTypeId, EventCount>>> _entries = new();
    readonly string _scope = $"{eventStore}/{@namespace}";

    /// <inheritdoc/>
    public Task Increment(ObserverId observerId, Key partition, IReadOnlyDictionary<EventTypeId, EventCount> countsPerEventType)
    {
        if (countsPerEventType.Count == 0)
        {
            return Task.CompletedTask;
        }

        var bucket = _entries.GetOrAdd(_scope, _ => new());
        var partitionCounts = bucket.GetOrAdd((observerId, partition.ToString()), _ => new());
        foreach (var (eventTypeId, count) in countsPerEventType)
        {
            partitionCounts.AddOrUpdate(
                eventTypeId,
                static (_, addend) => addend,
                static (_, existing, addend) => existing + addend.Value,
                count);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyDictionary<EventTypeId, EventCount>> GetFor(ObserverId observerId, Key partition)
    {
        if (_entries.TryGetValue(_scope, out var bucket) &&
            bucket.TryGetValue((observerId, partition.ToString()), out var partitionCounts))
        {
            return Task.FromResult<IReadOnlyDictionary<EventTypeId, EventCount>>(
                partitionCounts.ToDictionary(_ => _.Key, _ => _.Value));
        }

        return Task.FromResult<IReadOnlyDictionary<EventTypeId, EventCount>>(ImmutableDictionary<EventTypeId, EventCount>.Empty);
    }

    /// <inheritdoc/>
    public Task RemoveFor(ObserverId observerId, Key partition)
    {
        if (_entries.TryGetValue(_scope, out var bucket))
        {
            bucket.TryRemove((observerId, partition.ToString()), out _);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemoveAllFor(ObserverId observerId)
    {
        if (_entries.TryGetValue(_scope, out var bucket))
        {
            foreach (var key in bucket.Keys.Where(_ => _.ObserverId == observerId).ToArray())
            {
                bucket.TryRemove(key, out _);
            }
        }

        return Task.CompletedTask;
    }
}
