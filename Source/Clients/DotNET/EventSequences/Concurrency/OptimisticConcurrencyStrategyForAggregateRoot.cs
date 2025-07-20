// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Represents an optimistic concurrency strategy for managing concurrency scopes.
/// </summary>
/// <param name="aggregateRootContext">The <see cref="IAggregateRoot"/> to use for getting the tail sequence number.</param>
public class OptimisticConcurrencyStrategyForAggregateRoot(IAggregateRootContext aggregateRootContext) : IConcurrencyScopeStrategy
{
    /// <inheritdoc/>
    public Task<ConcurrencyScope> GetScope(
        EventSourceId eventSourceId,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        IEnumerable<EventType>? eventTypes = default)
    {
        return Task.FromResult(new ConcurrencyScope(
            aggregateRootContext.TailEventSequenceNumber,
            eventSourceId,
            eventStreamType,
            eventStreamId,
            eventSourceType,
            eventTypes));
    }
}
