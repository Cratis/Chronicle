// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.EventSequences.Concurrency;

/// <summary>
/// Represents a concurrency scope for an event sequence append operation.
/// </summary>
/// <param name="SequenceNumber">The expected sequence number.</param>
/// <param name="EventSourceId">The value indicating whether to scope to the associated <see cref="EventSourceId"/>.</param>
/// <param name="EventStreamType">Optional <see cref="EventStreamType"/> to scope to. If not set, it will not be used.</param>
/// <param name="EventStreamId">Optional <see cref="EventStreamId"/> to scope to. If not set, it will not be used.</param>
/// <param name="EventSourceType">Optional <see cref="EventSourceType"/> to scope to. If not set, it will not be used.</param>
/// <param name="EventTypes">Optional collection of <see cref="EventType"/> to scope to. If not set, it will not be used.</param>
public record ConcurrencyScope(
    EventSequenceNumber SequenceNumber,
    bool EventSourceId,
    EventStreamType? EventStreamType,
    EventStreamId? EventStreamId,
    EventSourceType? EventSourceType,
    IEnumerable<EventType>? EventTypes)
{
    /// <summary>
    /// Represents a concurrency scope that has not been specified yet.
    /// </summary>
    public static readonly ConcurrencyScope NotSet = new(
        EventSequenceNumber.Max,
        default,
        default,
        default,
        default,
        default);

    /// <summary>
    /// Represents a concurrency scope that does not apply any constraints.
    /// </summary>
    public static readonly ConcurrencyScope None = new(
        EventSequenceNumber.Unavailable,
        default,
        default,
        default,
        default,
        default);

    /// <summary>
    /// Gets a value indicating whether this <see cref="ConcurrencyScope"/> should be validated.
    /// </summary>
    /// <returns>true if it should be validated, false if not.</returns>
    /// <remarks>
    /// A scope expecting <see cref="EventSequenceNumber.BeforeFirst"/> is validated as well as one expecting an
    /// actual sequence number - it says "no event matching this narrowing may exist", which is a check the kernel
    /// can perform rather than a scope with nothing to compare against.
    /// </remarks>
    public bool ShouldBeValidated => this != NotSet && this != None && (SequenceNumber.IsActualValue || SequenceNumber.IsBeforeFirst);

    /// <summary>
    /// Gets a value indicating whether this <see cref="ConcurrencyScope"/> expects no event matching its narrowing
    /// to exist yet.
    /// </summary>
    /// <returns>true if the scope expects to still be empty, false if not.</returns>
    /// <remarks>
    /// This is what a strategy produces for the first append into a scope: it resolved the expected tail through
    /// the scope's own narrowing and found no event matching it. Validating it means rejecting the append if a
    /// matching event appeared between the moment the scope was resolved and the moment the append arrived.
    /// </remarks>
    public bool ExpectsNoMatchingEvent => this != NotSet && this != None && SequenceNumber.IsBeforeFirst;

    /// <summary>
    /// Gets a value indicating whether this <see cref="ConcurrencyScope"/> narrows an append without saying which
    /// sequence number it expects.
    /// </summary>
    /// <returns>true if the scope carries narrowing metadata but no expected sequence number, false if not.</returns>
    /// <remarks>
    /// A caller built it without resolving an expected sequence number, where <see cref="None"/> (append without a
    /// check) or <see cref="NotSet"/> (let the strategy decide) is what was wanted instead. A strategy no longer
    /// produces this state - one that resolves the expected tail and finds no event matching the scope's narrowing
    /// answers <see cref="EventSequenceNumber.BeforeFirst"/>, which is validated rather than skipped. A scope that
    /// does reach this state asks for a check and gets none, which is indistinguishable from having asked for
    /// nothing. It is skipped, but never silently.
    /// </remarks>
    public bool IsIncomplete => this != NotSet && this != None && !SequenceNumber.IsActualValue && !SequenceNumber.IsBeforeFirst;
}
