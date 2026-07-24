// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation;

/// <summary>
/// Represents an in-memory implementation of <see cref="IObserverHandledCountsStorage"/>.
/// </summary>
public sealed class ObserverHandledCountsStorage : IObserverHandledCountsStorage
{
    readonly ConcurrentDictionary<(ObserverId ObserverId, string Partition), ConcurrentDictionary<EventTypeId, EventCount>> _counts = new();

    /// <inheritdoc/>
    public Task Increment(ObserverId observerId, Key partition, IReadOnlyDictionary<EventTypeId, EventCount> countsPerEventType)
    {
        if (countsPerEventType.Count == 0)
        {
            return Task.CompletedTask;
        }

        var partitionCounts = _counts.GetOrAdd((observerId, partition.ToString()), _ => new());
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
    public Task<IReadOnlyDictionary<EventTypeId, EventCount>> GetFor(ObserverId observerId, Key partition) =>
        Task.FromResult<IReadOnlyDictionary<EventTypeId, EventCount>>(
            _counts.TryGetValue((observerId, partition.ToString()), out var partitionCounts)
                ? partitionCounts.ToDictionary(_ => _.Key, _ => _.Value)
                : ImmutableDictionary<EventTypeId, EventCount>.Empty);

    /// <inheritdoc/>
    public Task RemoveFor(ObserverId observerId, Key partition)
    {
        _counts.TryRemove((observerId, partition.ToString()), out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemoveAllFor(ObserverId observerId)
    {
        foreach (var key in _counts.Keys.Where(_ => _.ObserverId == observerId).ToArray())
        {
            _counts.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }
}
