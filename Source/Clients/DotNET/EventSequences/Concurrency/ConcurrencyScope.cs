// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Represents a concurrency scope for an event sequence append operation.
/// </summary>
/// <param name="SequenceNumber">The expected sequence number.</param>
/// <param name="EventSourceId">Optional <see cref="EventSourceId"/> to scope to. If not set, it will not be used.</param>
/// <param name="EventStreamType">Optional <see cref="EventStreamType"/> to scope to. If not set, it will not be used.</param>
/// <param name="EventStreamId">Optional <see cref="EventStreamId"/> to scope to. If not set, it will not be used.</param>
/// <param name="EventSourceType">Optional <see cref="EventSourceType"/> to scope to. If not set, it will not be used.</param>
/// <param name="EventTypes">Optional collection of <see cref="EventType"/> to scope to. If not set, it will not be used.</param>
public record ConcurrencyScope(
    EventSequenceNumber SequenceNumber,
    EventSourceId? EventSourceId = default,
    EventStreamType? EventStreamType = default,
    EventStreamId? EventStreamId = default,
    EventSourceType? EventSourceType = default,
    IEnumerable<EventType>? EventTypes = default)
{
    /// <summary>
    /// Represents a concurrency scope that has not been specified yet.
    /// </summary>
    public static readonly ConcurrencyScope NotSet = new(EventSequenceNumber.Max);

    /// <summary>
    /// Represents a concurrency scope that does not apply any constraints.
    /// </summary>
    public static readonly ConcurrencyScope None = new(EventSequenceNumber.Unavailable);

    /// <summary>
    /// Gets a value indicating whether the scope expects no event matching its narrowing to exist yet.
    /// </summary>
    /// <remarks>
    /// This is what <see cref="OptimisticConcurrencyStrategy"/> produces for the first append into a scope: it
    /// reads the tail through the scope's own narrowing and finds no event matching it, so it answers
    /// <see cref="EventSequenceNumber.BeforeFirst"/> rather than a number. The kernel validates it as "no event
    /// matching this scope may exist" and rejects the append if one appeared in the meantime.
    /// </remarks>
    public bool ExpectsNoMatchingEvent => this != NotSet && this != None && SequenceNumber.IsBeforeFirst;

    /// <summary>
    /// Gets a value indicating whether the scope narrows an append without saying which sequence number it expects.
    /// </summary>
    /// <remarks>
    /// It is not <see cref="NotSet"/>, so the configured <see cref="IConcurrencyScopeStrategy"/> does not get to
    /// supply the expected sequence number, and it has no sequence number of its own for the kernel to validate
    /// against - so the append is checked against nothing, which looks exactly like never having asked for a check.
    /// Build <see cref="None"/> to append without a check, or <see cref="NotSet"/> to let the strategy resolve the
    /// expected sequence number. A scope <see cref="OptimisticConcurrencyStrategy"/> resolved lands here too, for
    /// the first append into a scope, unless <see cref="ConcurrencyOptions.CheckFirstAppendIntoAScope"/> is turned
    /// on - with it on, an empty narrowing gives <see cref="ExpectsNoMatchingEvent"/>, which the kernel checks.
    /// </remarks>
    public bool IsIncomplete => this != NotSet && this != None && !SequenceNumber.IsActualValue && !SequenceNumber.IsBeforeFirst;
}
