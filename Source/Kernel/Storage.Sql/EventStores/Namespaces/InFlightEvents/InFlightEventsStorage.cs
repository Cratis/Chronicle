// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.InFlightEvents;

/// <summary>
/// Represents a transitional in-memory implementation of <see cref="IInFlightEventsStorage"/> for the SQL backend.
/// </summary>
/// <param name="eventStore">The <see cref="EventStoreName"/> the storage is for.</param>
/// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the storage is for.</param>
/// <remarks>
/// The SQL backend is still under construction in several areas. This implementation persists in-flight entries
/// in process memory only, scoped to the event store and namespace, so the observer code path compiles and runs.
/// A migration-backed table can replace this once the SQL backend reaches feature parity with MongoDB.
/// </remarks>
public class InFlightEventsStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace) : IInFlightEventsStorage
{
    static readonly ConcurrentDictionary<string, ConcurrentDictionary<InFlightEventId, InFlightEvent>> _entries = new();
    readonly string _scope = $"{eventStore}/{@namespace}";

    /// <inheritdoc/>
    public Task Add(ObserverId observerId, Key partition, EventSequenceNumber eventSequenceNumber)
    {
        var entry = new InFlightEvent
        {
            Id = InFlightEventId.New(),
            ObserverId = observerId,
            Partition = partition,
            EventSequenceNumber = eventSequenceNumber
        };

        var bucket = _entries.GetOrAdd(_scope, _ => new ConcurrentDictionary<InFlightEventId, InFlightEvent>());
        bucket[entry.Id] = entry;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Remove(ObserverId observerId, Key partition, EventSequenceNumber eventSequenceNumber)
    {
        if (_entries.TryGetValue(_scope, out var bucket))
        {
            foreach (var matching in bucket
                .Where(_ =>
                    _.Value.ObserverId == observerId &&
                    _.Value.Partition == partition &&
                    _.Value.EventSequenceNumber == eventSequenceNumber)
                .ToArray())
            {
                bucket.TryRemove(matching.Key, out _);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemoveUpTo(ObserverId observerId, Key partition, EventSequenceNumber upToInclusive)
    {
        if (_entries.TryGetValue(_scope, out var bucket))
        {
            foreach (var matching in bucket
                .Where(_ =>
                    _.Value.ObserverId == observerId &&
                    _.Value.Partition == partition &&
                    _.Value.EventSequenceNumber <= upToInclusive)
                .ToArray())
            {
                bucket.TryRemove(matching.Key, out _);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<InFlightEvent>> GetFor(ObserverId observerId)
    {
        if (!_entries.TryGetValue(_scope, out var bucket))
        {
            return Task.FromResult<IEnumerable<InFlightEvent>>([]);
        }

        return Task.FromResult<IEnumerable<InFlightEvent>>(
            bucket.Values.Where(_ => _.ObserverId == observerId).ToArray());
    }
}
