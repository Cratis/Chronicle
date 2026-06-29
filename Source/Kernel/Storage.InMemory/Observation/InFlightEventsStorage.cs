// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation;

/// <summary>
/// Represents an in-memory implementation of <see cref="IInFlightEventsStorage"/>.
/// </summary>
public sealed class InFlightEventsStorage : IInFlightEventsStorage
{
    readonly ConcurrentDictionary<InFlightEventId, InFlightEvent> _entries = new();

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

        _entries[entry.Id] = entry;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Remove(ObserverId observerId, Key partition, EventSequenceNumber eventSequenceNumber)
    {
        foreach (var entry in Matching(observerId, partition).Where(_ => _.EventSequenceNumber == eventSequenceNumber))
        {
            _entries.TryRemove(entry.Id, out _);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemoveUpTo(ObserverId observerId, Key partition, EventSequenceNumber upToInclusive)
    {
        foreach (var entry in Matching(observerId, partition).Where(_ => _.EventSequenceNumber <= upToInclusive))
        {
            _entries.TryRemove(entry.Id, out _);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<InFlightEvent>> GetFor(ObserverId observerId) =>
        Task.FromResult<IEnumerable<InFlightEvent>>(_entries.Values.Where(_ => _.ObserverId == observerId).ToArray());

    IEnumerable<InFlightEvent> Matching(ObserverId observerId, Key partition) =>
        _entries.Values.Where(_ => _.ObserverId == observerId && _.Partition == partition).ToArray();
}
