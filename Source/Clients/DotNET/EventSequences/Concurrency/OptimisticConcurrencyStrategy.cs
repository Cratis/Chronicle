// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Represents an optimistic concurrency strategy for managing concurrency scopes.
/// </summary>
/// <param name="eventSequence">The <see cref="IEventSequence"/> to use for getting the tail sequence number.</param>
/// <param name="options">
/// The <see cref="ConcurrencyOptions"/> deciding whether the first append into a scope is checked. Null falls back
/// to <see cref="ConcurrencyOptions.CheckFirstAppendIntoAScopeByDefault"/>.
/// </param>
public class OptimisticConcurrencyStrategy(IEventSequence eventSequence, ConcurrencyOptions? options) : IConcurrencyScopeStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OptimisticConcurrencyStrategy"/> class without options.
    /// </summary>
    /// <param name="eventSequence">The <see cref="IEventSequence"/> to use for getting the tail sequence number.</param>
    /// <remarks>
    /// Kept as its own constructor rather than folded into the one above with a default argument, so that code
    /// compiled against a build without <see cref="ConcurrencyOptions"/> keeps binding to a constructor that
    /// exists. Behaves as <see cref="ConcurrencyOptions.CheckFirstAppendIntoAScopeByDefault"/> says.
    /// </remarks>
    public OptimisticConcurrencyStrategy(IEventSequence eventSequence)
        : this(eventSequence, default)
    {
    }

    bool ChecksTheFirstAppend => options?.CheckFirstAppendIntoAScope ?? ConcurrencyOptions.CheckFirstAppendIntoAScopeByDefault;

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// The expected sequence number is read with the same narrowing the scope declares, because that is what the
    /// kernel validates the append against. Reading a broader tail than the kernel compares to would report a
    /// conflict between appends the scope says are independent; reading a narrower one would let a real conflict
    /// through. The two have to be asked the same question.
    /// </para>
    /// <para>
    /// When nothing matches that narrowing the tail read answers <see cref="EventSequenceNumber.Unavailable"/>,
    /// which also means "no sequence number was supplied" and is therefore skipped by the kernel. That is what the
    /// scope carries by default, so the first append into a scope goes through unchecked exactly as it always has.
    /// With <see cref="ConcurrencyOptions.CheckFirstAppendIntoAScope"/> turned on the scope says
    /// <see cref="EventSequenceNumber.BeforeFirst"/> instead - the expectation a first append actually has, and one
    /// the kernel checks.
    /// </para>
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
            tail.IsUnavailable && ChecksTheFirstAppend ? EventSequenceNumber.BeforeFirst : tail,
            eventSourceId,
            eventStreamType,
            eventStreamId,
            eventSourceType,
            eventTypes);
    }
}
