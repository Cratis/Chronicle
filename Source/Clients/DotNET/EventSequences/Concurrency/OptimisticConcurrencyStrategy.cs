// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Represents an optimistic concurrency strategy for managing concurrency scopes.
/// </summary>
/// <param name="eventSequence">The <see cref="IEventSequence"/> to use for getting the tail sequence number.</param>
public class OptimisticConcurrencyStrategy(IEventSequence eventSequence) : IConcurrencyScopeStrategy
{
    /// <inheritdoc/>
    public async Task<ConcurrencyScope> GetScope(
        EventSourceId eventSourceId,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        IEnumerable<EventType>? eventTypes = default)
    {
        var tail = await eventSequence.GetTailSequenceNumber(eventSourceId);
        return new ConcurrencyScope(
            tail,
            eventSourceId,
            eventStreamType,
            eventStreamId,
            eventSourceType,
            eventTypes);
    }
}
