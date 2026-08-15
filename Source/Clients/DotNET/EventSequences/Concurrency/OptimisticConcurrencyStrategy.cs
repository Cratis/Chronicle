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
    /// <remarks>
    /// The expected sequence number is read with the same narrowing the scope declares, because that is what the
    /// kernel validates the append against. Reading a broader tail than the kernel compares to would report a
    /// conflict between appends the scope says are independent; reading a narrower one would let a real conflict
    /// through. The two have to be asked the same question.
    /// When nothing matches that narrowing the tail read answers <see cref="EventSequenceNumber.Unavailable"/>,
    /// which also means "no sequence number was supplied" and is therefore skipped by the kernel. The scope says
    /// <see cref="EventSequenceNumber.BeforeFirst"/> instead - the expectation the first append into a scope
    /// actually has, and one the kernel can check.
    /// </remarks>
    public async Task<ConcurrencyScope> GetScope(
        EventSourceId eventSourceId,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        IEnumerable<EventType>? eventTypes = default)
    {
        var tail = await eventSequence.GetTailSequenceNumber(
            eventSourceId: eventSourceId,
            eventSourceType: eventSourceType,
            eventStreamType: eventStreamType,
            eventStreamId: eventStreamId,
            filterEventTypes: eventTypes);

        return new ConcurrencyScope(
            tail.IsUnavailable ? EventSequenceNumber.BeforeFirst : tail,
            eventSourceId,
            eventStreamType,
            eventStreamId,
            eventSourceType,
            eventTypes);
    }
}
