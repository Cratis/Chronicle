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
    public bool ShouldBeValidated => this != NotSet && this != None && SequenceNumber.IsActualValue;

    /// <summary>
    /// Gets a value indicating whether this <see cref="ConcurrencyScope"/> narrows an append without saying which
    /// sequence number it expects.
    /// </summary>
    /// <returns>true if the scope carries narrowing metadata but no expected sequence number, false if not.</returns>
    /// <remarks>
    /// This is a programming error on the caller's side rather than a valid way to opt out - a caller that wants no
    /// concurrency check has <see cref="None"/>, and one that wants the strategy to decide has <see cref="NotSet"/>.
    /// A scope in this state asks for a check and gets none, which is indistinguishable from having asked for
    /// nothing. It is skipped, but never silently.
    /// </remarks>
    public bool IsIncomplete => this != NotSet && this != None && !SequenceNumber.IsActualValue;
}
