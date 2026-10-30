// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences;

public partial class EventSequenceStorage
{
    /// <inheritdoc/>
    public Task<IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>>> GetInstancesOfAnyInRange(
        IEnumerable<EventSourceId> eventSourceIds,
        IEnumerable<EventTypeId> eventTypes,
        EventSequenceNumber from,
        EventSequenceNumber upTo,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sources = eventSourceIds.ToHashSet();
        var types = eventTypes.ToHashSet();
        var result = new Dictionary<EventSourceId, IReadOnlyList<AppendedEvent>>();
        lock (_lock)
        {
            foreach (var group in _events.Where(e => sources.Contains(e.Context.EventSourceId) && types.Contains(e.Context.EventType.Id) && e.Context.SequenceNumber <= upTo).GroupBy(e => e.Context.EventSourceId))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var predecessor = group.Where(e => e.Context.SequenceNumber < from).MaxBy(e => e.Context.SequenceNumber);
                result[group.Key] = group.Where(e => e.Context.SequenceNumber >= from || e == predecessor).OrderBy(e => e.Context.SequenceNumber).ToArray();
            }
        }
        return Task.FromResult<IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>>>(result);
    }
}
